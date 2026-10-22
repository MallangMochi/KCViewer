using System.Collections.Generic;
using KCViewer.Core.Types;
using KCViewer.Core.Types.Attacks;
using KCViewer.Core.Types.Extensions;
using KCViewer.Core.Types.Mocks;
using KCViewer.Utility.Data;
using Xunit;

namespace KCViewerCoreTests;

[Collection(DatabaseCollection.Name)]
public class AccuracyTests
{
	private DatabaseFixture Db { get; }

	public AccuracyTests(DatabaseFixture db)
	{
		Db = db;
	}

	[Fact]
	public void AccuracyTest1()
	{
		IShipData bismarck = new ShipDataMock(Db.MasterShips[ShipId.BismarckDrei])
		{
			Level = 175,
			Condition = 85,
			LuckBase = 84,
			SlotInstance = new List<IEquipmentData?>
			{
				new EquipmentDataMock(Db.MasterEquipment[EquipmentId.MainGunLarge_46cmTripleGunKai])
				{
					Level = 7
				},
			},
		};

		IFleetData fleet = new FleetDataMock
		{
			FleetType = FleetType.Single,
			MembersInstance = new(new List<IShipData?>
			{
				bismarck
			}),
		};

		double expected = 160;

		Assert.Equal(expected, bismarck.GetDayAttackAccuracy(DayAttackKind.NormalAttack, fleet));
	}

	[Fact]
	public void AccuracyTest2()
	{
		ShipDataMock kamikaze = new(Db.MasterShips[ShipId.KamikazeKai])
		{
			Level = 180,
			LuckBase = 99,
		};

		Assert.Equal(184, kamikaze.NextAccuracyLevel());
	}
}
