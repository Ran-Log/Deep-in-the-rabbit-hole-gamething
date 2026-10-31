namespace Tests.Integration;

using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

// Prueba los datos de Data/Levels (salas hechas a mano de cada nivel).
[TestSuite]
[RequireGodotRuntime]
public class LevelDataTest
{
	[TestCase]
	public void HayTresNiveles()
	{
		AssertThat(GameData.Catalog.Levels.Count).IsEqual(3);

		for (int n = 1; n <= 3; n++)
		{
			AssertThat(GameData.Level(n)).IsNotNull();
		}
	}

	[TestCase]
	public void CadaNivelTieneCampamentoSeguroEnElOrigen()
	{
		for (int n = 1; n <= 3; n++)
		{
			RoomDesign camp = GameData.Level(n).FixedRoomAt(Vector2I.Zero);

			AssertThat(camp).IsNotNull();
			AssertThat(camp.HasGas).IsFalse();
			AssertThat(camp.Loot).IsEqual(ChestLoot.None);
			AssertThat(camp.HasExit).IsFalse();
		}
	}

	[TestCase]
	public void CadaNivelTieneExactamenteUnaSalida()
	{
		for (int n = 1; n <= 3; n++)
		{
			int exits = 0;

			foreach (RoomDesign room in GameData.Level(n).FixedRooms)
			{
				exits += room.HasExit ? 1 : 0;
			}

			AssertThat(exits).IsEqual(1);
		}
	}

	[TestCase]
	public void SoloElNivel3EsElFinal()
	{
		AssertThat(GameData.Level(1).IsFinal).IsFalse();
		AssertThat(GameData.Level(2).IsFinal).IsFalse();
		AssertThat(GameData.Level(3).IsFinal).IsTrue();
	}

	[TestCase]
	public void ElNivel1SeMantieneComoAntes()
	{
		LevelDefinition level = GameData.Level(1);

		AssertThat(level.ExitCoord()).IsEqual(new Vector2I(3, -1));
		AssertThat(level.FixedRoomAt(new Vector2I(3, -1)).Mineral).IsEqual(MineralType.Iron);
		AssertThat(level.FixedRoomAt(new Vector2I(1, 0)).Loot).IsEqual(ChestLoot.Backpack);
		AssertThat(level.FixedRoomAt(new Vector2I(1, -1)).Loot).IsEqual(ChestLoot.Pickaxe);
		AssertThat(level.FixedRoomAt(new Vector2I(2, 0)).HasGas).IsTrue();
		AssertThat(level.FixedRoomAt(new Vector2I(2, -1)).Loot).IsEqual(ChestLoot.Note);
	}

	[TestCase]
	public void LaSalidaDelNivel2EsDeOro()
	{
		// Obliga a mejorar el pico a Avanzado antes de bajar al nivel 3.
		LevelDefinition level = GameData.Level(2);

		AssertThat(level.FixedRoomAt(level.ExitCoord()).Mineral).IsEqual(MineralType.Gold);
	}

	[TestCase]
	public void CadaNivelTieneAlMenosUnEnemigo()
	{
		for (int n = 1; n <= 3; n++)
		{
			AssertThat(GameData.Level(n).EnemyScenes.Count).IsGreater(0);
		}
	}
}
