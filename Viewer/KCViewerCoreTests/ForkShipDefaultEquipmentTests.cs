using System.Collections.Generic;
using System.Globalization;
using KCViewer.Fork;
using Xunit;

namespace KCViewerCoreTests;

/// <summary>
/// Fork: the comparison of the default equipment data with the ship record (KCViewer/Fork/ForkShipDefaultEquipment.cs,
/// handoff ship-default-equipment-data-20261125.md section 5 ②: 갱신, 일치, 불일치).
/// </summary>
public class ForkShipDefaultEquipmentTests
{
	private static readonly Dictionary<int, int[]> Data = new()
	{
		[1] = [1, 37, -1, -1, -1],
		[2] = [1, -1, -1, -1, -1],
		[3] = [2, 14, -1, -1, -1],
		[4] = [48, 14, -1, -1, -1],
		[5] = [48, 39, 49, -1, -1],
		[6] = [10, -1, -1, -1, -1],
	};

	/// <summary>The data compared before (or the default record): ship 4's old equipment.</summary>
	private static readonly Dictionary<int, int[]> Previous = new()
	{
		[1] = [1, 37, -1, -1, -1],
		[4] = [2, 14, -1, -1, -1],
		[5] = [48, 39, 39, -1, -1],
	};

	private static readonly Dictionary<int, int[]?> Record = new()
	{
		[1] = [1, 37, -1, -1, -1], // as the data: 일치
		[2] = null, // unknown: 갱신
		// 3 has no row: 갱신
		[4] = [2, 14, -1, -1, -1], // the previous data: 갱신
		[5] = [48, 39, 25, -1, -1], // recorded in play: 불일치
		[6] = [11, -1, -1, -1, -1], // not in the master: left out
	};

	[Fact]
	public void ClassifiesEachShip()
	{
		(List<int> updated, int matched, List<int> conflicts) = ForkShipDefaultEquipment.Classify(
			Data, Previous, id => Record.GetValueOrDefault(id), id => id != 6);

		Assert.Equal([2, 3, 4], updated);
		Assert.Equal(1, matched);
		Assert.Equal([5], conflicts);
	}

	[Fact]
	public void TheFirstComparisonOfAProfileTakesTheDefaultRecordAsThePreviousVersion()
	{
		// a profile still holding 74식's old default record, as 文月改 and 平安丸改 before 2026-11-24: not ours, so asked
		Dictionary<int, int[]> defaultRecord = new() { [4] = [48, 14, -1, -1, -1], [5] = [48, 39, 49, -1, -1] };
		Dictionary<int, int[]?> oldRecord = new() { [4] = [2, 14, -1, -1, -1], [5] = [48, 39, 39, -1, -1] };

		(List<int> updated, int matched, List<int> conflicts) = ForkShipDefaultEquipment.Classify(
			Data, defaultRecord, id => oldRecord.GetValueOrDefault(id), id => id is 4 or 5);

		Assert.Empty(updated);
		Assert.Equal(0, matched);
		Assert.Equal([4, 5], conflicts);
	}

	[Fact]
	public void ARecordOfFewerSlotsIsReadWithTheRestEmpty()
	{
		(List<int> updated, int matched, List<int> conflicts) = ForkShipDefaultEquipment.Classify(
			new Dictionary<int, int[]> { [2] = [1, -1, -1, -1, -1] }, new Dictionary<int, int[]>(), _ => [1, -1, -1, -1], _ => true);

		Assert.Empty(updated);
		Assert.Equal(1, matched);
		Assert.Empty(conflicts);
	}

	[Theory]
	[InlineData(null, new[] { 1, 37, -1, -1, -1 }, false)] // as the data
	[InlineData(null, new[] { 1, 13, -1, -1, -1 }, true)] // unknown before, other than the data
	[InlineData(new[] { 1, 37, -1, -1, -1 }, new[] { 1, 13, -1, -1, -1 }, true)] // the data before, other now
	[InlineData(new[] { 1, 13, -1, -1, -1 }, new[] { 1, 13, -1, -1, -1 }, false)] // recorded already: logged once
	[InlineData(null, new[] { 1, 37, -1, -1 }, false)] // four slots as five
	public void EquipmentSeenInPlayIsLoggedWhenItIsNewAndNotTheData(int[]? before, int[] observed, bool logged)
	{
		Assert.Equal(logged, ForkShipDefaultEquipment.ObservedDiffers(before, observed, [1, 37, -1, -1, -1]));
	}

	[Theory]
	[InlineData(1, "기본 장비 자료 버전 1을 받았습니다.")]
	[InlineData(2, "기본 장비 자료 버전 2를 받았습니다.")]
	[InlineData(4, "기본 장비 자료 버전 4를 받았습니다.")]
	[InlineData(10, "기본 장비 자료 버전 10을 받았습니다.")]
	[InlineData(19, "기본 장비 자료 버전 19를 받았습니다.")]
	public void TheFetchedLineReadsAsKorean(int version, string expected)
	{
		CultureInfo culture = CultureInfo.CurrentUICulture;
		CultureInfo.CurrentUICulture = new CultureInfo("ko-KR");
		try
		{
			Assert.Equal(expected, ForkStrings.DefaultEquipmentFetched(version));
		}
		finally
		{
			CultureInfo.CurrentUICulture = culture;
		}
	}
}
