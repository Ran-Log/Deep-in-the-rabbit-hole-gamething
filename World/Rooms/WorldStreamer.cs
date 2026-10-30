using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Carga las salas cercanas al jugador y descarga las lejanas, para que el mundo
/// sea infinito sin pasarse del límite de cuerpos físicos.
/// </summary>
public partial class WorldStreamer : Node3D
{
	[ExportCategory("WorldStreamer")]
	[ExportGroup("Referencias")]
	[Export] public PackedScene RoomScene;
	[Export] public Node3D Player;

	[ExportGroup("Carga")]
	// Radios en salas. UnloadRadius es mayor a propósito, para no cargar y
	// descargar la misma sala si el jugador camina justo en el borde.
	[Export] public int LoadRadius = 2;
	[Export] public int UnloadRadius = 4;
	// Cada cuántos segundos se revisa qué cargar o descargar.
	[Export] public float UpdateInterval = 0.5f;

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

		// Para que la primera carga sea inmediata y no después de UpdateInterval.
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

		// Solo las salas visitadas pueden tener enemigos de noche (EnemySpawner).
		WorldState.Instance?.MarkRoomVisited(center);

		LoadNearbyRooms(center);
		UnloadFarRooms(center);
	}

	// EnemySpawner elige entre estas dónde hacer aparecer zombies.
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

		// Antes de AddChild: en su _Ready la sala ya necesita estar en su lugar.
		room.Position = spec.Position;

		AddChild(room);

		loadedRooms[coord] = room;
	}
}
