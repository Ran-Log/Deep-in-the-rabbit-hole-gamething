using Godot;

/// <summary>
/// Los datos de una sala (los arma RoomGenerator y los usa WorldStreamer para crear la Room).
/// </summary>
public readonly struct RoomSpec
{
	public readonly string Name;
	public readonly Vector3 Position;
	public readonly int Width;
	public readonly int Depth;
	public readonly int WallHeight;
	public readonly MineralType WallMineral;
	public readonly int WallBlockHealth;

	// Posición en la grilla de salas (no de bloques).
	public readonly Vector2I GridCoord;

	public readonly bool HasGas;

	// None = sin cofre.
	public readonly ChestLoot Loot;

	// Solo si Loot es Note.
	public readonly string NoteText;

	// Tiene la grieta de salida del nivel.
	public readonly bool HasExit;

	// Tiene un nido de hormigas.
	public readonly bool HasNest;

	public RoomSpec(
		string name,
		Vector3 position,
		int width,
		int depth,
		int wallHeight,
		MineralType wallMineral,
		int wallBlockHealth,
		Vector2I gridCoord = default,
		bool hasGas = false,
		ChestLoot loot = ChestLoot.None,
		string noteText = "",
		bool hasExit = false,
		bool hasNest = false)
	{
		HasNest = hasNest;
		Name = name;
		Position = position;
		Width = width;
		Depth = depth;
		WallHeight = wallHeight;
		WallMineral = wallMineral;
		WallBlockHealth = wallBlockHealth;
		GridCoord = gridCoord;
		HasGas = hasGas;
		Loot = loot;
		NoteText = noteText ?? "";
		HasExit = hasExit;
	}
}
