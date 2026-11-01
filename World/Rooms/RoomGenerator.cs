using Godot;

/// <summary>
/// Fábrica de salas (Factory, clase 11): decide cómo es cada sala del nivel. Si el nivel
/// tiene una sala hecha a mano en esa coordenada, usa esa; si no, la genera al azar.
/// La misma semilla, nivel y coordenada dan siempre la misma sala, así se puede
/// descargar y volver a cargar.
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
	public const int WaterChancePercent = 50;

	public static RoomSpec Generate(ulong seed, Vector2I gridCoord, float cellSize, LevelDefinition level)
	{
		float spacing = RoomWidth * cellSize;
		Vector3 position = new Vector3(gridCoord.X * spacing, 0.0f, gridCoord.Y * spacing);
		string name = $"Room_{gridCoord.X}_{gridCoord.Y}";
		int levelNumber = level?.Number ?? 1;

		// Un generador con semilla propia por sala y nivel: los mismos datos dan siempre
		// los mismos números, en el mismo orden (clase 3).
		var rng = new RandomNumberGenerator { Seed = (uint)GD.Hash($"{seed}:{levelNumber}:{gridCoord.X}:{gridCoord.Y}") };

		int health = rng.RandiRange(4, 8);

		RoomDesign design = level?.FixedRoomAt(gridCoord);

		if (design != null)
		{
			return new RoomSpec(name, position, RoomWidth, RoomDepth, RoomWallHeight, design.Mineral, health, gridCoord,
				design.HasGas, design.Loot, design.NoteText, design.HasExit, design.HasNest, design.HasSkylight);
		}

		MineralType mineral = RoomMineralTable.Pick(rng.RandiRange(0, 99));

		// Gas solo en algunas salas de carbón.
		bool hasGas = mineral == MineralType.Coal && rng.RandiRange(0, 99) < GasChancePercent;

		// Cofres al azar: comida, agua o una nota que orienta hacia la salida (GDD).
		ChestLoot loot = ChestLoot.None;
		string noteText = "";

		if (rng.RandiRange(0, 99) < ChestChancePercent)
		{
			bool isNote = rng.RandiRange(0, 99) < NoteChancePercent;
			Vector2I exit = level?.ExitCoord() ?? Vector2I.Zero;

			loot = isNote ? ChestLoot.Note : ChestLoot.Food;
			noteText = isNote ? ExitHint.Towards(gridCoord.X, gridCoord.Y, exit.X, exit.Y) : "";
		}

		// Al final, para no cambiar los números de antes (mismo mundo con la misma semilla).
		bool hasSkylight = rng.RandiRange(0, 99) < (level?.SkylightChancePercent ?? 30);

		// La mitad de los cofres de comida traen agua (también al final, por lo mismo).
		if (rng.RandiRange(0, 99) < WaterChancePercent && loot == ChestLoot.Food)
		{
			loot = ChestLoot.Water;
		}

		return new RoomSpec(name, position, RoomWidth, RoomDepth, RoomWallHeight, mineral, health, gridCoord, hasGas, loot, noteText,
			hasSkylight: hasSkylight);
	}
}
