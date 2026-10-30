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

	// Probabilidad (0-100) de que una sala procedural tenga cofre.
	public const int ChestChancePercent = 20;

	public static RoomSpec Generate(ulong seed, Vector2I gridCoord, float cellSize)
	{
		float spacing = RoomWidth * cellSize;
		Vector3 position = new Vector3(gridCoord.X * spacing, 0.0f, gridCoord.Y * spacing);
		string name = $"Room_{gridCoord.X}_{gridCoord.Y}";
		ulong hash = DeterministicHash.Combine(seed, gridCoord.X, gridCoord.Y);
		int health = 4 + (int)((hash >> 8) % 5); // 4..8

		// Salas del nivel 1 hechas a mano: iguales con cualquier semilla.
		if (LevelOneLayout.TryGet(gridCoord.X, gridCoord.Y, out LevelOneLayout.RoomDesign design))
		{
			return new RoomSpec(name, position, RoomWidth, RoomDepth, RoomWallHeight, design.Mineral, health, gridCoord,
				design.HasGas, design.Loot, design.NoteText, design.HasExit);
		}

		MineralType mineral = RoomMineralTable.Pick((int)(hash % 100));

		// Gas en ~30% de las salas de carbón. Usa otros bits del hash para no depender del mineral.
		bool hasGas = mineral == MineralType.Coal && (hash >> 16) % 100 < 30;

		// Cofres al azar: comida o una nota que orienta hacia la salida (GDD).
		ChestLoot loot = ChestLoot.None;
		string noteText = "";

		if ((hash >> 24) % 100 < ChestChancePercent)
		{
			bool isNote = (hash >> 40) % 100 < 40;

			loot = isNote ? ChestLoot.Note : ChestLoot.Food;
			noteText = isNote ? LevelOneLayout.HintTowardsExit(gridCoord.X, gridCoord.Y) : "";
		}

		return new RoomSpec(name, position, RoomWidth, RoomDepth, RoomWallHeight, mineral, health, gridCoord, hasGas, loot, noteText);
	}
}
