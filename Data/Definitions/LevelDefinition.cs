using Godot;

/// <summary>
/// Un nivel de la mina (GDD, sección 4): sus salas hechas a mano, qué enemigos aparecen
/// y el color de la luz. Hay un .tres por nivel en Data/Levels.
/// </summary>
[GlobalClass]
public partial class LevelDefinition : Resource
{
	[ExportCategory("LevelDefinition")]
	[ExportGroup("Nivel")]
	[Export] public int Number = 1;
	[Export] public string DisplayName = "";
	// true = su salida termina el juego (victoria).
	[Export] public bool IsFinal = false;
	// Texto al apuntar a la salida, sin la tecla.
	[Export] public string ExitPrompt = "Bajar por la grieta";

	[ExportGroup("Salas hechas a mano")]
	[Export] public Godot.Collections.Array<RoomDesign> FixedRooms = new();

	[ExportGroup("Enemigos")]
	// Los que pueden aparecer en este nivel; se elige uno al azar en cada aparición.
	[Export] public Godot.Collections.Array<PackedScene> EnemyScenes = new();

	[ExportGroup("Luz")]
	[Export] public Color AmbientColor = new Color(1.0f, 1.0f, 1.0f);
	[Export] public Color SunColor = new Color(1.0f, 1.0f, 1.0f);

	public RoomDesign FixedRoomAt(Vector2I coord)
	{
		foreach (RoomDesign room in FixedRooms)
		{
			if (room.Coord == coord)
			{
				return room;
			}
		}

		return null;
	}

	// Dónde está la salida del nivel (para las notas que la señalan).
	public Vector2I ExitCoord()
	{
		foreach (RoomDesign room in FixedRooms)
		{
			if (room.HasExit)
			{
				return room.Coord;
			}
		}

		return Vector2I.Zero;
	}
}
