using KCViewer.Avalonia.Translation.Destination;
using KCViewer.Avalonia.Translation.Equipment;
using KCViewer.Avalonia.Translation.EquipmentUpgrade;
using KCViewer.Avalonia.Translation.FitBonus;
using KCViewer.Avalonia.Translation.Lock;
using KCViewer.Avalonia.Translation.Mission;
using KCViewer.Avalonia.Translation.Operation;
using KCViewer.Avalonia.Translation.Quest;
using KCViewer.Avalonia.Translation.QuestMetadata;
using KCViewer.Avalonia.Translation.Ship;
using KCViewer.Core.Services;

namespace KCViewer.Avalonia.Translation;

public class DataService
{
	private IConfigurationUi ConfigurationUi { get; }
	private ISoftwareUpdaterService SoftwareUpdaterService { get; }
	private IEoLogger EoLogger { get; }

	public DestinationDataService Destination { get; }
	public QuestDataService Quest { get; }
	public QuestMetadataDataService QuestsMetadata { get; }
	public EquipmentDataService Equipment { get; }
	public MissionDataService Mission { get; }
	public ShipDataService Ship { get; }
	public OperationDataService Operation { get; }
	public LockDataService Lock { get; }
	public FitBonusDataService FitBonus { get; }
	public EquipmentUpgradeDataService EquipmentUpgrade { get; }

	public DataService(
		IConfigurationUi configurationUi,
		ISoftwareUpdaterService softwareUpdaterService,
		IEoLogger eoLogger)
	{
		ConfigurationUi = configurationUi;
		SoftwareUpdaterService = softwareUpdaterService;
		EoLogger = eoLogger;

		Destination = new(ConfigurationUi, SoftwareUpdaterService, EoLogger);
		Quest = new(ConfigurationUi, SoftwareUpdaterService, EoLogger);
		QuestsMetadata = new(ConfigurationUi, SoftwareUpdaterService, EoLogger);
		Equipment = new(ConfigurationUi, SoftwareUpdaterService, EoLogger);
		Mission = new(ConfigurationUi, SoftwareUpdaterService, EoLogger);
		Ship = new(ConfigurationUi, SoftwareUpdaterService, EoLogger);
		Operation = new(ConfigurationUi, SoftwareUpdaterService, EoLogger);
		Lock = new(ConfigurationUi, SoftwareUpdaterService, EoLogger);
		FitBonus = new(ConfigurationUi, SoftwareUpdaterService, EoLogger);
		EquipmentUpgrade = new(ConfigurationUi, SoftwareUpdaterService, EoLogger);
	}

	public async Task Initialize()
	{
		await Destination.Initialize();
		await Quest.Initialize();
		await QuestsMetadata.Initialize();
		await Equipment.Initialize();
		await Mission.Initialize();
		await Ship.Initialize();
		await Operation.Initialize();
		await Lock.Initialize();
		await FitBonus.Initialize();
		await EquipmentUpgrade.Initialize();
	}
}
