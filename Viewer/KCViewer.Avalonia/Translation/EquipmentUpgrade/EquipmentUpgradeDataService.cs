using KCViewer.Core.Services;
using KCViewer.Core.Types.Serialization.EquipmentUpgrade;

namespace KCViewer.Avalonia.Translation.EquipmentUpgrade;

public sealed class EquipmentUpgradeDataService(
	IConfigurationUi configurationUi,
	ISoftwareUpdaterService softwareUpdaterService,
	IEoLogger logger)
	: DataServiceBase(configurationUi, softwareUpdaterService, logger)
{
	protected override string FileName => "EquipmentUpgrades.json";
	protected override DataType DataType => DataType.Data;

	public List<EquipmentUpgradeDataModel> UpgradeList { get; private set; } = [];

	/// <inheritdoc />
	public override async Task Initialize()
	{
		await LoadDictionary(FilePath);
	}

	private async Task LoadDictionary(string path)
	{
		UpgradeList = await Load<List<EquipmentUpgradeDataModel>>(path) ?? [];
	}
}
