using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using KCViewer.Database;
using KCViewer.Database.DataMigration;
using KCViewer.Database.KancolleApi;
using KCViewer.Database.Sortie;
using KCViewer.Services;
using KCViewer.Window.Tools.SortieRecordViewer;
using KCViewer.Window.Tools.SortieRecordViewer.SortieCostViewer;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KCViewerCoreTests.SortieCost;

public abstract class SortieCostTestBase
{
	private static string DirectoryName => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

	protected abstract string RelativePath { get; }

	private string BasePath => Path.Join(DirectoryName, RelativePath);

	private async Task<T> GetDataFromFile<T>(string fileName) where T : new()
	{
		string path = Path.Join(BasePath, fileName);

		if (!File.Exists(path)) return new();

		Stream stream = File.OpenRead(path);
		T? data = await JsonSerializer.DeserializeAsync<T>(stream);

		Assert.NotNull(data);

		return data;
	}

	protected async Task<List<SortieCostViewModel>> MakeSortieCosts(string testFilePrefix,
		bool clearFleetAfterBattleData = false)
	{
		ToolService toolService = new(new(), null!);
		SortieRecordMigrationService sortieRecordMigrationService = new(toolService);

		await using KCViewerContext db = new(true);
		await db.Database.EnsureDeletedAsync();
		await db.Database.EnsureCreatedAsync();

		List<ApiFile> apiFiles = await GetDataFromFile<List<ApiFile>>($"{testFilePrefix}ApiFiles.json");
		List<SortieRecord> sortieRecords = await GetDataFromFile<List<SortieRecord>>($"{testFilePrefix}SortieRecords.json");

		if (clearFleetAfterBattleData)
		{
			foreach (SortieRecord sortie in sortieRecords)
			{
				sortie.FleetAfterSortieData = null;
			}

			apiFiles = apiFiles.Where(f => f.SortieRecordId is not null).ToList();
		}

		await db.AddRangeAsync(sortieRecords);
		await db.AddRangeAsync(apiFiles);
		await db.SaveChangesAsync();

		List<SortieRecordViewModel> sorties = await db.Sorties
			.Include(s => s.ApiFiles)
			.Select(s => new SortieRecordViewModel(s, s.ApiFiles.Select(f => f.TimeStamp).Min(), null!))
			.ToListAsync();

		List<SortieCostViewModel> sortieCosts = [];

		foreach (SortieRecordViewModel sortie in sorties)
		{
			sortieCosts.Add(new(db, toolService, sortieRecordMigrationService, sortie, new()));
		}

		return sortieCosts;
	}

	public virtual async Task SortieCostTest0(string testFilePrefix)
	{
		List<SortieCostViewModel> sortieCosts2 = await MakeSortieCosts(testFilePrefix, true);
		List<SortieCostViewModel> sortieCosts1 = await MakeSortieCosts(testFilePrefix);

		Assert.Equal(sortieCosts1.Count, sortieCosts2.Count);

		foreach ((SortieCostViewModel first, SortieCostViewModel second) in sortieCosts1.Zip(sortieCosts2))
		{
			Assert.Equal(first.SortieFleetSupplyCost, second.SortieFleetSupplyCost);
			Assert.Equal(first.SortieFleetRepairCost, second.SortieFleetRepairCost);
			Assert.Equal(first.TotalAirBaseSortieCost, second.TotalAirBaseSortieCost);
			Assert.Equal(first.TotalAirBaseSupplyCost, second.TotalAirBaseSupplyCost);
		}
	}
}
