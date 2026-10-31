namespace Tests.Integration;

using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class RoomGeneratorTest
{
	private const ulong Seed = 12345;
	private const float CellSize = 2.0f;

	private static LevelDefinition Level1 => GameData.Level(1);

	private static RoomSpec Room(int x, int z, LevelDefinition level = null)
	{
		return RoomGenerator.Generate(Seed, new Vector2I(x, z), CellSize, level ?? Level1);
	}

	[TestCase]
	public void MismaSemillaYCoordenadaDanLaMismaSala()
	{
		RoomSpec a = Room(7, -5);
		RoomSpec b = Room(7, -5);

		AssertThat(b.WallMineral).IsEqual(a.WallMineral);
		AssertThat(b.WallBlockHealth).IsEqual(a.WallBlockHealth);
		AssertThat(b.HasGas).IsEqual(a.HasGas);
		AssertThat(b.Loot).IsEqual(a.Loot);
	}

	[TestCase]
	public void CoordenadasDistintasNoSonTodasIguales()
	{
		// Con 30 salas, alguna tiene que tener otro mineral u otra vida que la primera.
		RoomSpec first = Room(10, 10);
		bool foundDifferent = false;

		for (int i = 1; i < 30 && !foundDifferent; i++)
		{
			RoomSpec other = Room(10 + i, 10 - i);
			foundDifferent = other.WallMineral != first.WallMineral || other.WallBlockHealth != first.WallBlockHealth;
		}

		AssertThat(foundDifferent).IsTrue();
	}

	[TestCase]
	public void OtroNivelConLaMismaSemillaDaOtrasSalas()
	{
		bool foundDifferent = false;

		for (int i = 0; i < 30 && !foundDifferent; i++)
		{
			RoomSpec inLevel1 = Room(10 + i, 10, GameData.Level(1));
			RoomSpec inLevel2 = Room(10 + i, 10, GameData.Level(2));
			foundDifferent = inLevel1.WallMineral != inLevel2.WallMineral || inLevel1.WallBlockHealth != inLevel2.WallBlockHealth;
		}

		AssertThat(foundDifferent).IsTrue();
	}

	[TestCase]
	public void LaVidaDeLasParedesEstaEntre4y8()
	{
		for (int i = 0; i < 50; i++)
		{
			AssertThat(Room(i, i * 3).WallBlockHealth).IsBetween(4, 8);
		}
	}

	[TestCase]
	public void ElCampamentoEsDePiedraYSinGas()
	{
		RoomSpec camp = Room(0, 0);

		AssertThat(camp.WallMineral).IsEqual(MineralType.Stone);
		AssertThat(camp.HasGas).IsFalse();
		AssertThat(camp.Loot).IsEqual(ChestLoot.None);
	}

	[TestCase]
	public void SoloHayGasEnSalasDeCarbonProcedurales()
	{
		for (int i = 0; i < 200; i++)
		{
			RoomSpec room = Room(20 + i, -20 - i);

			if (room.HasGas)
			{
				AssertThat(room.WallMineral).IsEqual(MineralType.Coal);
			}
		}
	}

	[TestCase]
	public void UnaSalaFijaDelNivelSeRespeta()
	{
		// Nivel 1: en (1,-1) está el pico viejo abandonado.
		AssertThat(Room(1, -1).Loot).IsEqual(ChestLoot.Pickaxe);
	}
}
