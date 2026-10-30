using Godot;
using System.Linq;

/// <summary>
/// Hace aparecer zombies de noche, de dos formas:
/// 1. Una oleada en el borde del campamento, más grande cada noche.
/// 2. En las salas que el jugador ya visitó: excavar mucho trae más amenazas (regla del GDD).
/// TODO: de día deberían desaparecer o quedarse en salas con techo (ver plan, bugs).
/// </summary>
public partial class EnemySpawner : Node3D
{
	[ExportCategory("EnemySpawner")]
	[ExportGroup("Referencias")]
	[Export] public PackedScene ZombieScene;
	[Export] public DayNightCycle DayNight;
	[Export] public WorldStreamer Streamer;

	[ExportGroup("Oleadas")]
	[Export] public int BaseZombiesPerNight = 2;
	[Export] public int ExtraZombiesPerDay = 1;
	[Export] public int MaxAlive = 10;
	// Probabilidad (0-100) de que cada sala explorada traiga un zombie al anochecer.
	[Export] public int RoomSpawnChancePercent = 50;
	// Cada cuántos segundos de noche aparece uno más en una sala explorada.
	[Export] public float TrickleInterval = 30.0f;

	[ExportGroup("Zona del campamento")]
	// Rectángulo (x, z en metros) donde aparece la oleada; por defecto el interior de la sala (0,0).
	[Export] public Vector2 AreaMin = new Vector2(-10.0f, -10.0f);
	[Export] public Vector2 AreaMax = new Vector2(8.0f, 8.0f);
	[Export] public float SpawnHeight = 0.5f;

	private readonly RandomNumberGenerator rng = new();
	private float trickleTimer;

	public override void _Ready()
	{
		if (ZombieScene == null || DayNight == null)
		{
			GD.PushError($"{Name}: falta asignar ZombieScene o DayNight; no van a aparecer enemigos.");
			return;
		}

		rng.Randomize();
		DayNight.NightStarted += OnNightStarted;
	}

	public override void _ExitTree()
	{
		if (DayNight != null)
		{
			DayNight.NightStarted -= OnNightStarted;
		}
	}

	public override void _Process(double delta)
	{
		if (DayNight == null || !DayNight.IsNight)
		{
			return;
		}

		trickleTimer += (float)delta;

		if (trickleTimer >= TrickleInterval)
		{
			trickleTimer = 0.0f;
			SpawnInRandomExploredRoom();
		}
	}

	private void OnNightStarted(int day)
	{
		trickleTimer = 0.0f;

		int campWave = BaseZombiesPerNight + (day - 1) * ExtraZombiesPerDay;

		for (int i = 0; i < campWave; i++)
		{
			TrySpawn(PickCampEdgePosition());
		}

		foreach (Room room in ExploredRooms())
		{
			if (rng.RandiRange(0, 99) < RoomSpawnChancePercent)
			{
				TrySpawn(RoomSpawnPoint(room));
			}
		}
	}

	private void SpawnInRandomExploredRoom()
	{
		Room[] rooms = ExploredRooms().ToArray();

		if (rooms.Length > 0)
		{
			Room room = rooms[rng.RandiRange(0, rooms.Length - 1)];
			TrySpawn(RoomSpawnPoint(room));
		}
	}

	/// <summary>Salas cargadas que el jugador ya visitó, sin contar el campamento (ese tiene su propia oleada).</summary>
	private System.Collections.Generic.IEnumerable<Room> ExploredRooms()
	{
		if (Streamer == null || WorldState.Instance == null)
		{
			yield break;
		}

		foreach (Room room in Streamer.LoadedRooms)
		{
			if (GodotObject.IsInstanceValid(room)
				&& room.RoomGridCoord != Vector2I.Zero
				&& WorldState.Instance.IsRoomVisited(room.RoomGridCoord))
			{
				yield return room;
			}
		}
	}

	/// <summary>Un poco corrido del centro, para no caer encima del cofre o la grieta (que van al medio).</summary>
	private Vector3 RoomSpawnPoint(Room room)
	{
		return room.InteriorCenterGlobal + new Vector3(-3.0f, SpawnHeight + 1.0f, 3.0f);
	}

	private void TrySpawn(Vector3 globalPosition)
	{
		if (GetTree().GetNodesInGroup(Zombie.Group).Count >= MaxAlive)
		{
			return;
		}

		var zombie = ZombieScene.Instantiate<Zombie>();

		zombie.Position = ToLocal(globalPosition);
		AddChild(zombie);
	}

	/// <summary>Un punto al azar en el borde del rectángulo del campamento, lejos del centro donde está el fuego.</summary>
	private Vector3 PickCampEdgePosition()
	{
		float x = rng.RandfRange(AreaMin.X, AreaMax.X);
		float z = rng.RandfRange(AreaMin.Y, AreaMax.Y);

		switch (rng.RandiRange(0, 3))
		{
			case 0: x = AreaMin.X; break;
			case 1: x = AreaMax.X; break;
			case 2: z = AreaMin.Y; break;
			default: z = AreaMax.Y; break;
		}

		return new Vector3(x, SpawnHeight, z);
	}
}
