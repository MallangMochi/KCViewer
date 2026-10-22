using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using KCViewer.Data;
using KCViewer.Resource.Record;
using KCViewer.Window.Dialog.ResourceChartWPF;
using Microsoft.Win32;
using ScottPlot.Plottable;
using Drawing = System.Drawing;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: the resource chart in KanColleViewer's style (see the XAML; mockup v7 and the user's rules, VIEWER-PLAN.md
/// "자원 차트"). The records and the ranges are the viewer's (ResourceChartWPF); the drawing is new:
/// - 자원: the eight items as solid lines. Left axis 0 to min(35만, the shown resources' top × 1.2 cut to 만), five equal
///   steps in 만; right axis 0 to min(3,000, the shown materials' top × 1.2 cut to 백) on the same grid lines. Without
///   shown resources the materials take the left axis; without shown materials there is no right axis.
/// - 자원(차분): the change since the record before, as steps; 0 in the middle, ± the largest change × 1.2 cut to 천
///   (resources) or 1 (materials).
/// - 경험치: a range around the values; 경험치(차분): 0 to the largest gain × 1.2 cut to 백.
/// The time axis keeps the viewer's steps per range, written 10-01 or 10-01 12:00, the year under the first label
/// and where it changes.
/// </summary>
public partial class KcvResourceChartWindow
{
	private enum ChartKind
	{
		Resource,
		ResourceDiff,
		Experience,
		ExperienceDiff,
	}

	/// <summary>The viewer's ranges (ResourceChartWPF.ChartSpan); Custom once the dates are changed by hand.</summary>
	private enum ChartSpan
	{
		Day,
		Week,
		Month,
		Season,
		Year,
		All,
		WeekFirst,
		MonthFirst,
		SeasonFirst,
		YearFirst,
		Custom,
	}

	private sealed class Item(string name, int rgb, bool isMaterial, Func<ResourceRecord.ResourceElement, int> value)
	{
		public string Name { get; } = name;
		public Drawing.Color Color { get; } = Drawing.Color.FromArgb(rgb >> 16 & 0xFF, rgb >> 8 & 0xFF, rgb & 0xFF);
		public bool IsMaterial { get; } = isMaterial;
		public Func<ResourceRecord.ResourceElement, int> Value { get; } = value;
		public bool Visible { get; set; } = true;
		public FrameworkElement? LegendEntry { get; set; }
	}

	/// <summary>A drawn line with its hover marker and the value at each record (or each step).</summary>
	private sealed record DrawnLine(string Name, MarkerPlot Marker, double[] Values);

	private static readonly Drawing.Color ExperienceColor = Drawing.Color.FromArgb(0x3D, 0x8E, 0xF0);

	private Item[] Items { get; } =
	[
		new(ForkStrings.KcvChartFuel, 0x4CAF50, false, r => r.Fuel),
		new(ForkStrings.KcvChartAmmo, 0xC9A43A, false, r => r.Ammo),
		new(ForkStrings.KcvChartSteel, 0xA0A0A8, false, r => r.Steel),
		new(ForkStrings.KcvChartBauxite, 0xD9822B, false, r => r.Bauxite),
		new(ForkStrings.KcvChartInstantRepair, 0x3D8EF0, true, r => r.InstantRepair),
		new(ForkStrings.KcvChartInstantConstruction, 0xF06A4A, true, r => r.InstantConstruction),
		new(ForkStrings.KcvChartDevelopmentMaterial, 0x4FC3D9, true, r => r.DevelopmentMaterial),
		new(ForkStrings.KcvChartImprovementMaterial, 0xB98AE0, true, r => r.ModdingMaterial),
	];

	private ChartKind Kind { get; set; } = ChartKind.Resource;
	private ChartSpan Span { get; set; } = ChartSpan.Month;

	// the range as the window opened, for 초기화
	private ChartSpan OpenSpan { get; set; }
	private DateTime OpenBegin { get; set; }
	private DateTime OpenEnd { get; set; }

	// the dates a range set, to tell them from dates changed by hand
	private DateTime RangeBegin { get; set; }
	private DateTime RangeEnd { get; set; }

	private DateTime[] Times { get; set; } = [];
	private List<DrawnLine> Lines { get; } = [];
	private bool Steps { get; set; }
	private int HoverIndex { get; set; } = -1;

	public KcvResourceChartWindow() : base(new ResourceChartViewModel())
	{
		InitializeComponent();

		Chart.Configuration.Zoom = false;
		Chart.Configuration.Pan = false;
		Chart.Configuration.DoubleClickBenchmark = false;
		Chart.RightClicked -= Chart.DefaultRightClickEvent;

		BuildLegend();
		CheckKind();

		Loaded += Open;
		// the time labels are thinned by the width
		Chart.SizeChanged += (_, e) =>
		{
			if (Math.Abs(e.NewSize.Width - e.PreviousSize.Width) > 40) Draw();
		};
		ViewModel.PropertyChanged += DatesChanged;
		KcvViewState.Current.PropertyChanged += ThemeChanged;
		Closed += (_, _) => KcvViewState.Current.PropertyChanged -= ThemeChanged;
	}

	private void Open(object sender, RoutedEventArgs e)
	{
		Loaded -= Open;

		List<ResourceRecord.ResourceElement> records = RecordManager.Instance.Resource.Record;
		if (records.Count is 0)
		{
			MessageBox.Show(ResourceChartResources.RecordDataDoesNotExist, ResourceChartResources.Error,
				MessageBoxButton.OK, MessageBoxImage.Error);
			Close();
			return;
		}

		ViewModel.MinDate = records.First().Date.Date;
		ViewModel.MaxDate = DateTime.Now.AddDays(1).Date;
		SetSpan(ChartSpan.Month);

		OpenSpan = Span;
		OpenBegin = ViewModel.DateBegin;
		OpenEnd = ViewModel.DateEnd;

		Draw();
	}

	#region Conditions and menus

	private void Search(object sender, RoutedEventArgs e) => Draw();

	private void Redraw(object sender, RoutedEventArgs e) => Draw();

	/// <summary>Back to the range of the opened window; the chart stays until 검색.</summary>
	private void ResetConditions(object sender, RoutedEventArgs e) => SetSpan(OpenSpan, OpenBegin, OpenEnd);

	/// <summary>A range from the menu sets the dates and draws at once (user's choice 2026-10-04).</summary>
	private void SelectSpan(object sender, RoutedEventArgs e)
	{
		if (sender is not MenuItem { Tag: string tag } || !Enum.TryParse(tag, out ChartSpan span)) return;

		SetSpan(span);
		Draw();
	}

	private void SelectKind(object sender, RoutedEventArgs e)
	{
		if (sender is not MenuItem { Tag: string tag } || !Enum.TryParse(tag, out ChartKind kind)) return;

		Kind = kind;
		CheckKind();
		Draw();
	}

	private void CheckKind()
	{
		KindResource.IsChecked = Kind is ChartKind.Resource;
		KindResourceDiff.IsChecked = Kind is ChartKind.ResourceDiff;
		KindExperience.IsChecked = Kind is ChartKind.Experience;
		KindExperienceDiff.IsChecked = Kind is ChartKind.ExperienceDiff;
		Legend.Visibility = Kind is ChartKind.Resource or ChartKind.ResourceDiff ? Visibility.Visible : Visibility.Collapsed;
	}

	/// <summary>The viewer's dates of each range (ResourceChartWPF.SetDateRange).</summary>
	private void SetSpan(ChartSpan span, DateTime? begin = null, DateTime? end = null)
	{
		DateTime now = DateTime.Now;
		DateTime tomorrow = now.AddDays(1).Date;

		begin ??= span switch
		{
			ChartSpan.Day => now.AddDays(-1),
			ChartSpan.Week => now.AddDays(-7),
			ChartSpan.Month => now.AddMonths(-1),
			ChartSpan.Season => now.AddMonths(-3),
			ChartSpan.Year => now.AddYears(-1),
			ChartSpan.WeekFirst => now.AddDays(now.DayOfWeek == DayOfWeek.Sunday ? -6 : 1 - (int)now.DayOfWeek),
			ChartSpan.MonthFirst => new DateTime(now.Year, now.Month, 1),
			ChartSpan.SeasonFirst => SeasonStart(now),
			ChartSpan.YearFirst => new DateTime(now.Year, 1, 1),
			_ => RecordManager.Instance.Resource.Record.FirstOrDefault()?.Date ?? now.AddMonths(-1),
		};
		end ??= tomorrow;

		Span = span;
		RangeBegin = begin.Value;
		RangeEnd = end.Value;
		ViewModel.DateBegin = begin.Value;
		ViewModel.DateEnd = end.Value;

		foreach (MenuItem item in SpanMenu.Items.OfType<MenuItem>())
		{
			item.IsChecked = item.Tag as string == span.ToString();
		}
	}

	private static DateTime SeasonStart(DateTime now)
	{
		int month = now.Month / 3 * 3;
		if (month == 0) month = 12;
		return new DateTime(now.Year - (now.Month < 3 ? 1 : 0), month, 1);
	}

	/// <summary>Dates changed by hand: a range of their own, drawn with 검색.</summary>
	private void DatesChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is not (nameof(ViewModel.DateBegin) or nameof(ViewModel.DateEnd))) return;
		if (ViewModel.DateBegin.Date == RangeBegin.Date && ViewModel.DateEnd.Date == RangeEnd.Date) return;

		Span = ChartSpan.Custom;
		RangeBegin = ViewModel.DateBegin;
		RangeEnd = ViewModel.DateEnd;
		foreach (MenuItem item in SpanMenu.Items.OfType<MenuItem>())
		{
			item.IsChecked = false;
		}
	}

	/// <summary>The theme's colours are set after the state changes (KcvPanels.Apply), so the chart is drawn after that.</summary>
	private void ThemeChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is nameof(KcvViewState.Dark))
		{
			Dispatcher.BeginInvoke(Draw, System.Windows.Threading.DispatcherPriority.Background);
		}
	}

	private void SaveImage(object sender, RoutedEventArgs e)
	{
		SaveFileDialog dialog = new()
		{
			FileName = "ResourceChart.png",
			Filter = ForkStrings.KcvChartSaveFilter,
		};
		if (dialog.ShowDialog(this) is not true) return;

		try
		{
			Chart.Plot.SaveFig(dialog.FileName);
		}
		catch (Exception)
		{
			MessageBox.Show(ResourceChartResources.FailedToSaveResourceChartImage, ResourceChartResources.Error,
				MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}

	#endregion

	#region Legend

	private void BuildLegend()
	{
		Legend.Children.Add(LegendHeader(ForkStrings.KcvChartResources, 0));
		foreach (Item item in Items)
		{
			if (item == Items[4]) Legend.Children.Add(LegendHeader(ForkStrings.KcvChartMaterials, 8));

			StackPanel content = new() { Orientation = Orientation.Horizontal };
			content.Children.Add(new Rectangle
			{
				Width = 9,
				Height = 9,
				RadiusX = 2,
				RadiusY = 2,
				Margin = new Thickness(0, 0, 6, 0),
				VerticalAlignment = VerticalAlignment.Center,
				Fill = new SolidColorBrush(Color.FromRgb(item.Color.R, item.Color.G, item.Color.B)),
			});
			content.Children.Add(new TextBlock { Text = item.Name, FontSize = 13, VerticalAlignment = VerticalAlignment.Center });

			Border entry = new()
			{
				Padding = new Thickness(4, 2, 4, 2),
				Background = Brushes.Transparent,
				Cursor = Cursors.Hand,
				Child = content,
			};
			entry.MouseLeftButtonUp += (_, _) =>
			{
				item.Visible = !item.Visible;
				entry.Opacity = item.Visible ? 1 : 0.35;
				Draw();
			};

			item.LegendEntry = entry;
			Legend.Children.Add(entry);
		}
	}

	private static TextBlock LegendHeader(string text, double top)
	{
		TextBlock header = new()
		{
			Text = text,
			FontSize = 11,
			Margin = new Thickness(4, top, 0, 2),
		};
		header.SetResourceReference(TextBlock.ForegroundProperty, "Kcv.InactiveForeground");
		return header;
	}

	#endregion

	#region Drawing

	/// <summary>The records in the range (the viewer's GetRecords and ShouldSkipRecord), with today's values at the end.</summary>
	private List<ResourceRecord.ResourceElement> Records()
	{
		List<ResourceRecord.ResourceElement> records = RecordManager.Instance.Resource.Record
			.Where(r => r.Date > ViewModel.DateBegin && r.Date < ViewModel.DateEnd)
			.ToList();

		if (DateTime.Now < ViewModel.DateEnd && KCDatabase.Instance.Material.IsAvailable && KCDatabase.Instance.Admiral.IsAvailable)
		{
			var material = KCDatabase.Instance.Material;
			var admiral = KCDatabase.Instance.Admiral;
			records.Add(new ResourceRecord.ResourceElement(
				material.Fuel, material.Ammo, material.Steel, material.Bauxite,
				material.InstantConstruction, material.InstantRepair, material.DevelopmentMaterial, material.ModdingMaterial,
				admiral.Level, admiral.Exp));
		}

		if (ShowAllData.IsChecked) return records;

		TimeSpan gap = Span switch
		{
			ChartSpan.Month or ChartSpan.MonthFirst => TimeSpan.FromHours(12),
			ChartSpan.Season or ChartSpan.SeasonFirst or ChartSpan.Year or ChartSpan.YearFirst or ChartSpan.All => TimeSpan.FromDays(1),
			_ => TimeSpan.Zero,
		};
		if (gap == TimeSpan.Zero) return records;

		List<ResourceRecord.ResourceElement> kept = [];
		foreach (ResourceRecord.ResourceElement record in records)
		{
			if (kept.Count > 0 && record.Date - kept[^1].Date < gap) continue;
			kept.Add(record);
		}

		return kept;
	}

	private void Draw()
	{
		if (!IsLoaded) return;

		ScottPlot.Plot plot = Chart.Plot;
		plot.Clear();
		Lines.Clear();
		HoverIndex = -1;
		Tip.Visibility = Visibility.Collapsed;

		Drawing.Color background = ThemeColor("Kcv.StatusBackground");
		Drawing.Color grid = ThemeColor("Kcv.ChartGrid");
		Drawing.Color tick = ThemeColor("Kcv.ChartTick");
		Drawing.Color frame = ThemeColor("Kcv.ChartFrame");
		Drawing.Color zero = ThemeColor("Kcv.ChartZero");
		string font = CultureInfo.CurrentUICulture.Name.StartsWith("ko", StringComparison.Ordinal) ? "Malgun Gothic" : "Segoe UI";

		plot.Style(background, background, grid, tick, tick, tick);
		foreach (ScottPlot.Renderable.Axis axis in new[] { plot.XAxis, plot.XAxis2, plot.YAxis, plot.YAxis2 })
		{
			axis.Color(frame);
			axis.TickLabelStyle(color: tick, fontName: font, fontSize: 11);
			axis.MinorGrid(false);
			axis.Ticks(true, false, true);
		}
		plot.XAxis2.Ticks(false);
		plot.XAxis.MajorGrid(true, grid);
		plot.YAxis.MajorGrid(true, grid);
		plot.YAxis2.MajorGrid(false);

		List<ResourceRecord.ResourceElement> records = Records();
		Times = records.Select(r => r.Date).ToArray();
		double[] xs = Times.Select(t => t.ToOADate()).ToArray();
		Steps = Kind is ChartKind.ResourceDiff or ChartKind.ExperienceDiff;

		switch (Kind)
		{
			case ChartKind.Resource:
				DrawResources(plot, records, xs);
				break;
			case ChartKind.ResourceDiff:
				DrawResourceChanges(plot, records, xs, zero);
				break;
			case ChartKind.Experience:
				DrawExperience(plot, records, xs);
				break;
			case ChartKind.ExperienceDiff:
				DrawExperienceChanges(plot, records, xs);
				break;
		}

		// The right edge is the last record (today's values while the range reaches now), so the lines always reach it
		// ("자원 차트는 항상 우측을 기록의 마지막지점으로 고정할 것", 2026-10-05); the left stays the range's start
		DateTime right = Times.Length > 0 && Times[^1] > ViewModel.DateBegin ? Times[^1] : ViewModel.DateEnd;
		(double[] positions, string[] labels) = TimeTicks(ViewModel.DateBegin, right);
		plot.XAxis.ManualTickPositions(positions, labels);
		plot.SetAxisLimits(ViewModel.DateBegin.ToOADate(), right.ToOADate(), null, null, 0, 0);

		Chart.Refresh();
	}

	private void DrawResources(ScottPlot.Plot plot, List<ResourceRecord.ResourceElement> records, double[] xs)
	{
		Item[] resources = Items.Where(i => !i.IsMaterial && i.Visible).ToArray();
		Item[] materials = Items.Where(i => i.IsMaterial && i.Visible).ToArray();
		int materialAxis = resources.Length > 0 ? 1 : 0;

		foreach (Item item in resources.Concat(materials))
		{
			double[] ys = records.Select(r => (double)item.Value(r)).ToArray();
			AddLine(plot, item.Name, item.Color, xs, ys, ys, item.IsMaterial ? materialAxis : 0);
		}

		double resourceTop = AxisTop(records, resources, 10000, 350000);
		double materialTop = AxisTop(records, materials, 100, 3000);

		if (resources.Length > 0)
		{
			SetAxis(plot, plot.YAxis, 0, 0, resourceTop, 5, ForkStrings.KcvChartTenThousands);
		}
		else if (materials.Length > 0)
		{
			SetAxis(plot, plot.YAxis, 0, 0, materialTop, 5, Count);
		}
		else
		{
			SetAxis(plot, plot.YAxis, 0, 0, 1, 1, _ => "");
		}

		ShowRightAxis(plot, resources.Length > 0 && materials.Length > 0);
		if (resources.Length > 0 && materials.Length > 0)
		{
			SetAxis(plot, plot.YAxis2, 1, 0, materialTop, 5, Count);
		}
	}

	private void DrawResourceChanges(ScottPlot.Plot plot, List<ResourceRecord.ResourceElement> records, double[] xs, Drawing.Color zero)
	{
		Item[] resources = Items.Where(i => !i.IsMaterial && i.Visible).ToArray();
		Item[] materials = Items.Where(i => i.IsMaterial && i.Visible).ToArray();
		int materialAxis = resources.Length > 0 ? 1 : 0;

		double resourceMax = 0;
		double materialMax = 0;
		foreach (Item item in resources.Concat(materials))
		{
			double[] changes = Changes(records, r => item.Value(r));
			AddSteps(plot, item.Name, item.Color, xs, changes, item.IsMaterial ? materialAxis : 0);

			double largest = changes.Length > 0 ? changes.Max(Math.Abs) : 0;
			if (item.IsMaterial) materialMax = Math.Max(materialMax, largest);
			else resourceMax = Math.Max(resourceMax, largest);
		}

		double resourceRange = Math.Max(1000, Math.Floor(resourceMax * 1.2 / 1000) * 1000);
		double materialRange = Math.Max(1, Math.Floor(materialMax * 1.2));
		string SignedResource(double v) => Math.Abs(v) < 1e-9 ? "0" : (v > 0 ? "+" : "-") + ForkStrings.KcvChartTenThousands(Math.Abs(v));
		string SignedCount(double v) => Math.Abs(v) < 1e-9 ? "0" : (v > 0 ? "+" : "-") + Count(Math.Abs(v));

		if (resources.Length > 0)
		{
			SetAxis(plot, plot.YAxis, 0, -resourceRange, resourceRange, 6, SignedResource);
		}
		else
		{
			SetAxis(plot, plot.YAxis, 0, -materialRange, materialRange, 6, SignedCount);
		}

		ShowRightAxis(plot, resources.Length > 0 && materials.Length > 0);
		if (resources.Length > 0 && materials.Length > 0)
		{
			SetAxis(plot, plot.YAxis2, 1, -materialRange, materialRange, 6, SignedCount);
		}

		var line = plot.AddHorizontalLine(0, zero, 1);
		line.YAxisIndex = 0;
	}

	private void DrawExperience(ScottPlot.Plot plot, List<ResourceRecord.ResourceElement> records, double[] xs)
	{
		double[] ys = records.Select(r => (double)r.HQExp).ToArray();
		AddLine(plot, ForkStrings.KcvChartExperience, ExperienceColor, xs, ys, ys, 0);
		ShowRightAxis(plot, false);

		if (ys.Length is 0)
		{
			SetAxis(plot, plot.YAxis, 0, 0, 1, 1, _ => "");
			return;
		}

		// a range around the values (user's choice 2026-10-04): from 0 the line lay flat at the top
		double min = ys.Min();
		double max = ys.Max();
		double step = NiceStep((max - min) / 5);
		double low = Math.Floor(min / step) * step;
		double high = Math.Max(low + step, Math.Ceiling(max / step) * step);
		SetAxis(plot, plot.YAxis, 0, low, high, (int)Math.Round((high - low) / step), ForkStrings.KcvChartTenThousands);
	}

	private void DrawExperienceChanges(ScottPlot.Plot plot, List<ResourceRecord.ResourceElement> records, double[] xs)
	{
		double[] changes = Changes(records, r => r.HQExp);
		AddSteps(plot, ForkStrings.KcvChartExperience, ExperienceColor, xs, changes, 0);
		ShowRightAxis(plot, false);

		double top = Math.Max(100, Math.Floor((changes.Length > 0 ? changes.Max() : 0) * 1.2 / 100) * 100);
		SetAxis(plot, plot.YAxis, 0, 0, top, 5, Count);
	}

	/// <summary>The change from each record to the next (per day with 일단위로 평균화); one fewer than the records.</summary>
	private double[] Changes(List<ResourceRecord.ResourceElement> records, Func<ResourceRecord.ResourceElement, int> value)
	{
		double[] changes = new double[Math.Max(0, records.Count - 1)];
		for (int i = 1; i < records.Count; i++)
		{
			double change = value(records[i]) - value(records[i - 1]);
			if (DivideByDay.IsChecked)
			{
				change /= Math.Max((records[i].Date - records[i - 1].Date).TotalDays, 1.0 / 1440.0);
			}
			changes[i - 1] = change;
		}
		return changes;
	}

	private void AddLine(ScottPlot.Plot plot, string name, Drawing.Color color, double[] xs, double[] ys, double[] values, int axis)
	{
		if (xs.Length is 0) return;

		ScatterPlot line = plot.AddScatterLines(xs, ys, color, 1.6f);
		line.YAxisIndex = axis;
		Lines.Add(new DrawnLine(name, Marker(plot, color, axis), values));
	}

	/// <summary>A step from each record to the next at the change in between (the viewer's step display).</summary>
	private void AddSteps(ScottPlot.Plot plot, string name, Drawing.Color color, double[] xs, double[] changes, int axis)
	{
		if (changes.Length is 0) return;

		double[] stepXs = new double[changes.Length * 2];
		double[] stepYs = new double[changes.Length * 2];
		for (int i = 0; i < changes.Length; i++)
		{
			stepXs[i * 2] = xs[i];
			stepXs[i * 2 + 1] = xs[i + 1];
			stepYs[i * 2] = changes[i];
			stepYs[i * 2 + 1] = changes[i];
		}

		ScatterPlot line = plot.AddScatterLines(stepXs, stepYs, color, 1.5f);
		line.YAxisIndex = axis;
		Lines.Add(new DrawnLine(name, Marker(plot, color, axis), changes));
	}

	private static MarkerPlot Marker(ScottPlot.Plot plot, Drawing.Color color, int axis)
	{
		MarkerPlot marker = plot.AddPoint(0, 0, color, 6);
		marker.YAxisIndex = axis;
		marker.IsVisible = false;
		return marker;
	}

	/// <summary>0 to the shown items' top × 1.2, cut to the unit, at most the cap ("만단위/백단위에서 절사").</summary>
	private static double AxisTop(List<ResourceRecord.ResourceElement> records, Item[] items, double unit, double cap)
	{
		double max = items.Length is 0 || records.Count is 0 ? 0 : records.Max(r => items.Max(i => i.Value(r)));
		return Math.Min(cap, Math.Max(unit, Math.Floor(max * 1.2 / unit) * unit));
	}

	private static void SetAxis(ScottPlot.Plot plot, ScottPlot.Renderable.Axis axis, int index, double low, double high, int steps, Func<double, string> label)
	{
		double[] positions = Enumerable.Range(0, steps + 1).Select(i => low + (high - low) * i / steps).ToArray();
		axis.ManualTickPositions(positions, positions.Select(label).ToArray());
		plot.SetAxisLimits(null, null, low, high, 0, index);
	}

	private static void ShowRightAxis(ScottPlot.Plot plot, bool show)
	{
		plot.YAxis2.Ticks(show, false, show);
	}

	private static string Count(double value) => value.ToString("N0", CultureInfo.InvariantCulture);

	/// <summary>1, 2 or 5 × 10^n at least the given step.</summary>
	private static double NiceStep(double raw)
	{
		if (raw <= 0) return 1;

		double power = Math.Pow(10, Math.Floor(Math.Log10(raw)));
		double fraction = raw / power;
		return (fraction <= 1 ? 1 : fraction <= 2 ? 2 : fraction <= 5 ? 5 : 10) * power;
	}

	/// <summary>The viewer's time steps per range; 10-01 or 10-01 12:00, the year under the first label and where it changes.</summary>
	private (double[] Positions, string[] Labels) TimeTicks(DateTime begin, DateTime end)
	{
		ChartSpan span = Span is ChartSpan.Custom
			? Math.Floor((end - begin).TotalDays) switch
			{
				<= 1 => ChartSpan.Day,
				<= 7 => ChartSpan.Week,
				<= 31 => ChartSpan.Month,
				<= 93 => ChartSpan.Season,
				_ => ChartSpan.Year,
			}
			: Span;

		(DateTime first, Func<DateTime, DateTime> next, int hours) = span switch
		{
			ChartSpan.Day => (CeilHours(begin, 2), t => t.AddHours(2), 2),
			ChartSpan.Week or ChartSpan.WeekFirst => (CeilHours(begin, 12), t => t.AddHours(12), 12),
			ChartSpan.Month or ChartSpan.MonthFirst => (CeilDay(begin), t => t.AddDays(2), 0),
			ChartSpan.Season or ChartSpan.SeasonFirst => (CeilDay(begin), t => t.AddDays(7), 0),
			_ => (CeilMonth(begin), (Func<DateTime, DateTime>)(t => t.AddMonths(1)), 0),
		};

		List<DateTime> times = [];
		for (DateTime t = first; t <= end && times.Count < 400; t = next(t))
		{
			times.Add(t);
		}

		// Every step keeps its grid line; the labels skip steps where they would run into each other (about 80 px
		// each), keeping midnight in hour steps
		int every = Math.Max(1, (int)Math.Ceiling(times.Count * 80.0 / Math.Max(200, Chart.ActualWidth - 120)));
		bool Labelled(DateTime t, int index) => hours > 0
			? (int)Math.Round((t - t.Date).TotalHours) % (hours * every) == 0
			: index % every == 0;

		string[] labels = new string[times.Count];
		int lastYear = -1;
		for (int i = 0; i < times.Count; i++)
		{
			DateTime t = times[i];
			if (!Labelled(t, i))
			{
				labels[i] = "";
				continue;
			}

			// 10-01, or 10-01 12:00 off midnight; the year under the first label and where it changes
			string label = t.ToString(t.TimeOfDay == TimeSpan.Zero ? "MM-dd" : "MM-dd HH:mm", CultureInfo.InvariantCulture);
			if (t.Year != lastYear) label += $"\n{t.Year}";
			lastYear = t.Year;
			labels[i] = label;
		}

		return (times.Select(t => t.ToOADate()).ToArray(), labels);
	}

	private static DateTime CeilHours(DateTime time, int hours) =>
		time.Date.AddHours(Math.Ceiling((time - time.Date).TotalHours / hours) * hours);

	private static DateTime CeilDay(DateTime time) => time == time.Date ? time : time.Date.AddDays(1);

	private static DateTime CeilMonth(DateTime time)
	{
		DateTime first = new(time.Year, time.Month, 1);
		return first == time ? time : first.AddMonths(1);
	}

	private Drawing.Color ThemeColor(string key) => TryFindResource(key) switch
	{
		SolidColorBrush brush => Drawing.Color.FromArgb(brush.Color.A, brush.Color.R, brush.Color.G, brush.Color.B),
		_ => Drawing.Color.Gray,
	};

	#endregion

	#region Tooltip

	private void ShowPoint(object sender, MouseEventArgs e)
	{
		if (Lines.Count is 0 || Times.Length is 0)
		{
			HidePoint(sender, e);
			return;
		}

		(double x, _) = Chart.GetMouseCoordinates();
		double[] xs = Times.Select(t => t.ToOADate()).ToArray();
		int index = Steps ? StepAt(xs, x) : NearestIndex(xs, x);
		if (index < 0)
		{
			HidePoint(sender, e);
			return;
		}

		if (index != HoverIndex)
		{
			HoverIndex = index;
			double markerX = Steps ? (xs[index] + xs[index + 1]) / 2 : xs[index];
			foreach (DrawnLine line in Lines)
			{
				line.Marker.X = markerX;
				line.Marker.Y = line.Values[index];
				line.Marker.IsVisible = true;
			}
			Chart.Refresh();
			FillTip(index);
		}

		Point mouse = e.GetPosition(Chart);
		Tip.Visibility = Visibility.Visible;
		Tip.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
		double left = mouse.X + 14;
		if (left + Tip.DesiredSize.Width > Chart.ActualWidth) left = Math.Max(0, mouse.X - Tip.DesiredSize.Width - 14);
		double top = Math.Min(mouse.Y + 14, Math.Max(0, Chart.ActualHeight - Tip.DesiredSize.Height));
		Canvas.SetLeft(Tip, left);
		Canvas.SetTop(Tip, top);
	}

	private void HidePoint(object sender, MouseEventArgs e)
	{
		Tip.Visibility = Visibility.Collapsed;
		if (HoverIndex < 0) return;

		HoverIndex = -1;
		foreach (DrawnLine line in Lines)
		{
			line.Marker.IsVisible = false;
		}
		Chart.Refresh();
	}

	/// <summary>The time (or the step's two times) and the shown items, two to a line.</summary>
	private void FillTip(int index)
	{
		TipLines.Children.Clear();

		string title = Steps
			? $"{KcvRecordTimeConverter.Format(Times[index])} ~ {KcvRecordTimeConverter.Format(Times[index + 1])}"
			: KcvRecordTimeConverter.Format(Times[index]);
		TipLines.Children.Add(TipLine(title, "Kcv.Foreground"));

		string perDay = Steps && DivideByDay.IsChecked ? ForkStrings.KcvChartPerDay : "";
		string Value(double v) => Steps
			? v.ToString("+#,0;-#,0;0", CultureInfo.InvariantCulture) + perDay
			: Count(v);

		foreach (DrawnLine[] pair in Lines.Chunk(2))
		{
			TipLines.Children.Add(TipLine(string.Join(" · ", pair.Select(l => $"{l.Name} {Value(l.Values[index])}")), "Kcv.SubForeground"));
		}
	}

	private static TextBlock TipLine(string text, string foreground)
	{
		TextBlock line = new() { Text = text };
		line.SetResourceReference(TextBlock.ForegroundProperty, foreground);
		return line;
	}

	private static int NearestIndex(double[] xs, double x)
	{
		int index = Array.BinarySearch(xs, x);
		if (index >= 0) return index;

		index = ~index;
		if (index == 0) return 0;
		if (index >= xs.Length) return xs.Length - 1;
		return x - xs[index - 1] < xs[index] - x ? index - 1 : index;
	}

	/// <summary>The step under the mouse: between record i and i + 1, or -1 outside.</summary>
	private static int StepAt(double[] xs, double x)
	{
		if (xs.Length < 2 || x < xs[0] || x > xs[^1]) return -1;

		int index = Array.BinarySearch(xs, x);
		if (index < 0) index = ~index - 1;
		return Math.Clamp(index, 0, xs.Length - 2);
	}

	#endregion
}
