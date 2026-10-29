using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Genera y descarga Rooms alrededor del jugador para simular un mundo
/// infinito sin que la cantidad de nodos/cuerpos físicos crezca sin límite
/// (el problema de escalabilidad de la revisión original: Jolt se quedaba
/// sin cuerpos físicos apenas se cavaba un poco). Reemplaza a LevelBuilder,
/// que generaba un layout fijo una sola vez — ver LevelLayout.cs, que ya no
/// se usa desde main.tscn pero se dejó por si en algún momento quieren un
/// modo de nivel fijo además del infinito.
///
/// Cada Room vive en una celda de una grilla infinita de salas (no de
/// bloques), separada RoomGenerator.RoomWidth*CellSize del resto — el mismo
/// espaciado que usaba el layout fijo, así que las salas vecinas quedan
/// adyacentes igual que antes.
///
/// LoadRadius/UnloadRadius con histéresis (Unload &gt; Load): una sala se
/// descarga solo cuando queda claramente lejos, no apenas sale del radio de
/// carga — evita crear y destruir la misma sala en cada actualización
/// cuando el jugador camina justo en el borde.
/// </summary>
public partial class WorldStreamer : Node3D
{
	[Export]
	public PackedScene RoomScene;

	[Export]
	public Node3D Player;

	/// <summary>Radio, en salas, que se mantiene siempre generado alrededor del jugador.</summary>
	[Export]
	public int LoadRadius = 2;

	/// <summary>Radio, en salas, a partir del cual una sala se descarga. Debe ser mayor que LoadRadius.</summary>
	[Export]
	public int UnloadRadius = 4;

	/// <summary>Cada cuántos segundos se revisa si hay que cargar/descargar salas.</summary>
	[Export]
	public float UpdateInterval = 0.5f;

	private readonly Dictionary<Vector2I, Room> loadedRooms = new();
	private float timeSinceUpdate;

	public override void _Ready()
	{
		if (RoomScene == null)
		{
			GD.PushError($"{Name}: falta asignar RoomScene; no se genera ningún mundo.");
			return;
		}

		if (Player == null)
		{
			GD.PushError($"{Name}: falta asignar Player; el streaming no sabe alrededor de quién generar.");
			return;
		}

		// Fuerza una primera pasada inmediata en el próximo _Process en vez
		// de esperar UpdateInterval completo (si no, el jugador arranca sin
		// nada generado alrededor por medio segundo).
		timeSinceUpdate = UpdateInterval;
	}

	public override void _Process(double delta)
	{
		if (RoomScene == null || Player == null)
		{
			return;
		}

		timeSinceUpdate += (float)delta;

		if (timeSinceUpdate < UpdateInterval)
		{
			return;
		}

		timeSinceUpdate = 0.0f;

		Vector2I center = WorldToRoomGrid(Player.GlobalPosition);

		// "Un jugador ya pasó por aquí": solo las salas visitadas pueden
		// tener enemigos de noche (ver EnemySpawner).
		WorldState.Instance?.MarkRoomVisited(center);

		LoadNearbyRooms(center);
		UnloadFarRooms(center);
	}

	/// <summary>Salas cargadas ahora mismo (EnemySpawner elige entre estas dónde hacer aparecer zombies).</summary>
	public IEnumerable<Room> LoadedRooms => loadedRooms.Values;

	private float CellSize => BlockGrid.Instance?.CellSize ?? 2.0f;

	private Vector2I WorldToRoomGrid(Vector3 worldPosition)
	{
		float spacing = RoomGenerator.RoomWidth * CellSize;

		return new Vector2I(
			Mathf.RoundToInt(worldPosition.X / spacing),
			Mathf.RoundToInt(worldPosition.Z / spacing)
		);
	}

	private void LoadNearbyRooms(Vector2I center)
	{
		for (int dx = -LoadRadius; dx <= LoadRadius; dx++)
		{
			for (int dz = -LoadRadius; dz <= LoadRadius; dz++)
			{
				var coord = new Vector2I(center.X + dx, center.Y + dz);

				if (!loadedRooms.ContainsKey(coord))
				{
					SpawnRoom(coord);
				}
			}
		}
	}

	private void UnloadFarRooms(Vector2I center)
	{
		var toRemove = new List<Vector2I>();

		foreach (var entry in loadedRooms)
		{
			Vector2I coord = entry.Key;
			int distance = Math.Max(Math.Abs(coord.X - center.X), Math.Abs(coord.Y - center.Y));

			if (distance > UnloadRadius)
			{
				toRemove.Add(coord);
			}
		}

		foreach (Vector2I coord in toRemove)
		{
			loadedRooms[coord].QueueFree();
			loadedRooms.Remove(coord);
		}
	}

	private void SpawnRoom(Vector2I coord)
	{
		RoomSpec spec = RoomGenerator.Generate(WorldState.Instance?.Seed ?? 0, coord, CellSize);

		var room = RoomScene.Instantiate<Room>();

		room.Name = spec.Name;
		room.Width = spec.Width;
		room.Depth = spec.Depth;
		room.WallHeight = spec.WallHeight;
		room.WallMineral = spec.WallMineral;
		room.WallBlockHealth = spec.WallBlockHealth;
		room.RoomGridCoord = spec.GridCoord;
		room.HasGas = spec.HasGas;
		room.Loot = spec.Loot;
		room.NoteText = spec.NoteText;
		room.HasExit = spec.HasExit;

		// La posición se fija ANTES de AddChild, mismo criterio que en todo
		// el resto del proyecto (ver Room.SpawnBlock/LevelBuilder.SpawnRoom):
		// al entrar al árbol la Room corre su _Ready y ahí ya necesita estar
		// en su lugar.
		room.Position = spec.Position;

		AddChild(room);

		loadedRooms[coord] = room;
	}
}
