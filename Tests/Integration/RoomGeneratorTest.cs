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

	[TestCase]
	public void MismaSemillaYCoordenadaDanLaMismaSala()
	{
		var coord = new Vector2I(7, -5);

		RoomSpec a = RoomGenerator.Generate(Seed, coord, CellSize);
		RoomSpec b = RoomGenerator.Generate(Seed, coord, CellSize);

		AssertThat(b.WallMineral).IsEqual(a.WallMineral);
		AssertThat(b.WallBlockHealth).IsEqual(a.WallBlockHealth);
		AssertThat(b.HasGas).IsEqual(a.HasGas);
		AssertThat(b.Loot).IsEqual(a.Loot);
	}

	[TestCase]
	public void CoordenadasDistintasNoSonTodasIguales()
	{
		// Con 30 salas, alguna tiene que tener otro mineral u otra vida que la primera.
		RoomSpec first = RoomGenerator.Generate(Seed, new Vector2I(10, 10), CellSize);
		bool foundDifferent = false;

		for (int i = 1; i < 30 && !foundDifferent; i++)
		{
			RoomSpec other = RoomGenerator.Generate(Seed, new Vector2I(10 + i, 10 - i), CellSize);
			foundDifferent = other.WallMineral != first.WallMineral || other.WallBlockHealth != first.WallBlockHealth;
		}

		AssertThat(foundDifferent).IsTrue();
	}

	[TestCase]
	public void LaVidaDeLasParedesEstaEntre4y8()
	{
		for (int i = 0; i < 50; i++)
		{
			int health = RoomGenerator.Generate(Seed, new Vector2I(i, i * 3), CellSize).WallBlockHealth;

			AssertThat(health).IsBetween(4, 8);
		}
	}

	[TestCase]
	public void ElCampamentoEsDePiedraYSinGas()
	{
		RoomSpec camp = RoomGenerator.Generate(Seed, Vector2I.Zero, CellSize);

		AssertThat(camp.WallMineral).IsEqual(MineralType.Stone);
		AssertThat(camp.HasGas).IsFalse();
		AssertThat(camp.Loot).IsEqual(ChestLoot.None);
	}

	[TestCase]
	public void SoloHayGasEnSalasDeCarbon()
	{
		for (int i = 0; i < 200; i++)
		{
			RoomSpec room = RoomGenerator.Generate(Seed, new Vector2I(20 + i, -20 - i), CellSize);

			if (room.HasGas)
			{
				AssertThat(room.WallMineral).IsEqual(MineralType.Coal);
			}
		}
	}
}
