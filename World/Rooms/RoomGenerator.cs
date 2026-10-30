using Godot;

/// <summary>
/// Decide cómo es cada sala a partir de la semilla y su coordenada. La misma
/// coordenada siempre da la misma sala, así se puede descargar y volver a cargar.
/// Las salas cercanas al campamento salen fijas de LevelOneLayout.
/// </summary>
public static class RoomGenerator
{
	public const int RoomWidth = 12;
	public const int RoomDepth = 12;
	public const int RoomWallHeight = 3;

	// Probabilidades (0-100).
	public const int ChestChancePercent = 20;
	public const int GasChancePercent = 30;
	public const int NoteChancePercent = 40;

	public static RoomSpec Generate(ulong seed, Vector2I gridCoord, float cellSize)
	{
		float spacing = RoomWidth * cellSize;
		Vector3 position = new Vector3(gridCoord.X * spacing, 0.0f, gridCoord.Y * spacing);
		string name = $"Room_{gridCoord.X}_{gridCoord.Y}";

		// Un generador con semilla propia por sala: la misma semilla del mundo y la misma
		// coordenada dan siempre los mismos números, en el mismo orden (clase 3).
		var rng = new RandomNumberGenerator { Seed = (uint)GD.Hash($"{seed}:{gridCoord.X}:{gridCoord.Y}") };

		int health = rng.RandiRange(4, 8);

		// Salas del nivel 1 hechas a mano: iguales con cualquier semilla.
		if (LevelOneLayout.TryGet(gridCoord.X, gridCoord.Y, out LevelOneLayout.RoomDesign design))
		{
			return new RoomSpec(name, position, RoomWidth, RoomDepth, RoomWallHeight, design.Mineral, health, gridCoord,
				design.HasGas, design.Loot, design.NoteText, design.HasExit);
		}

		MineralType mineral = RoomMineralTable.Pick(rng.RandiRange(0, 99));

		// Gas solo en algunas salas de carbón.
		bool hasGas = mineral == MineralType.Coal && rng.RandiRange(0, 99) < GasChancePercent;

		// Cofres al azar: comida o una nota que orienta hacia la salida (GDD).
		ChestLoot loot = ChestLoot.None;
		string noteText = "";

		if (rng.RandiRange(0, 99) < ChestChancePercent)
		{
			bool isNote = rng.RandiRange(0, 99) < NoteChancePercent;

			loot = isNote ? ChestLoot.Note : ChestLoot.Food;
			noteText = isNote ? LevelOneLayout.HintTowardsExit(gridCoord.X, gridCoord.Y) : "";
		}

		return new RoomSpec(name, position, RoomWidth, RoomDepth, RoomWallHeight, mineral, health, gridCoord, hasGas, loot, noteText);
	}
}
