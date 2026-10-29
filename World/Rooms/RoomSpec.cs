using Godot;

/// <summary>
/// Definición de una Room del nivel: nombre, posición y configuración de sus
/// paredes. Es el "dato" que LevelBuilder lee para instanciar Room.tscn —
/// agregar o ajustar una sala pasa a ser una línea en <see cref="LevelLayout"/>
/// en vez de crear y configurar un nodo a mano en main.tscn.
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

	/// <summary>
	/// Coordenada de la sala en la grilla infinita de salas (no en celdas de
	/// bloque). La usa WorldStreamer para decidir qué cargar/descargar y
	/// Room para llevar el registro de qué celdas ya se minaron ahí. Datos
	/// del layout fijo antiguo (LevelLayout) la dejan en (0,0) por defecto,
	/// ya que ese layout nunca la necesitó.
	/// </summary>
	public readonly Vector2I GridCoord;

	/// <summary>Si es true, la sala sale con una GasZone en el centro (ver RoomGenerator/Room.SpawnGasZoneIfNeeded).</summary>
	public readonly bool HasGas;

	/// <summary>Qué hay en el cofre de la sala (None = sin cofre).</summary>
	public readonly ChestLoot Loot;

	/// <summary>Texto de la nota si <see cref="Loot"/> es Note.</summary>
	public readonly string NoteText;

	/// <summary>Si es true, la sala tiene la grieta de salida del nivel.</summary>
	public readonly bool HasExit;

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
		bool hasExit = false)
	{
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
