using Godot;

/// <summary>
/// El layout del nivel, como datos. Hoy es el mismo trío de salas
/// (RoomStart/RoomNorth/RoomEast) que antes vivía como 3 nodos Room
/// configurados a mano en main.tscn — mismos valores, misma posición.
/// <see cref="LevelBuilder"/> lee esta lista y genera las Rooms en
/// <c>_Ready</c>.
/// </summary>
public static class LevelLayout
{
	public static readonly RoomSpec[] Default =
	{
		new RoomSpec("RoomStart", new Vector3(0, 0, 0), 12, 12, 3, MineralType.Stone, 5),
		new RoomSpec("RoomNorth", new Vector3(0, 0, 24), 12, 12, 3, MineralType.Coal, 7),
		new RoomSpec("RoomEast", new Vector3(24, 0, 0), 12, 12, 3, MineralType.Iron, 9),
	};
}
