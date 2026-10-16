using Godot;
using System.Linq;

/// <summary>
/// Hace aparecer zombies de noche (GDD: "durante la noche aparecerán
/// amenazas, las cuales seguirán al jugador o irán hacia el campamento").
///
/// Dos fuentes:
/// 1. Oleada al campamento: al caer la noche aparecen en el borde interior
///    de la sala del campamento ("salen de los túneles"). Cada noche vienen
///    más.
/// 2. Salas exploradas: solo en salas que el jugador YA visitó (WorldState,
///    lo marca WorldStreamer) y que están cargadas. Al caer la noche cada una
///    tiene una probabilidad de traer un zombie, y mientras dura la noche
///    sigue apareciendo uno cada <see cref="TrickleInterval"/> segundos.
///    Así excavar mucho tiene costo: más salas abiertas = más amenazas
///    (regla del GDD sobre los túneles cerca del campamento).
///
/// PENDIENTE (diseño, para después de la alfa): de DÍA los enemigos solo
/// deberían poder estar en salas CON TECHO. El campamento tiene un agujero
/// al cielo (por donde cayó el jugador), igual que algunas otras salas, así
/// que ahí la luz los espanta. Hoy los zombies que quedan al amanecer
/// siguen vivos donde estén.
///
/// No decide cómo se comportan (eso es Zombie) ni cuándo es de noche (eso
/// es DayNightCycle).
/// </summary>
public partial class EnemySpawner : Node3D
{
	[Export]
	public PackedScene ZombieScene;

	[Export]
	public DayNightCycle DayNight;

	[Export]
	public WorldStreamer Streamer;

	[Export]
	public int BaseZombiesPerNight = 2;

	[Export]
	public int ExtraZombiesPerDay = 1;

	[Export]
	public int MaxAlive = 10;

	/// <summary>Probabilidad (0-100) de que cada sala explorada traiga un zombie al caer la noche.</summary>
	[Export]
	public int RoomSpawnChancePercent = 50;

	/// <summary>Cada cuántos segundos de noche aparece un zombie extra en alguna sala explorada.</summary>
	[Export]
	public float TrickleInterval = 30.0f;

	/// <summary>Esquina mínima (x,z en metros) del rectángulo de la oleada al campamento, por defecto el interior de la sala (0,0).</summary>
	[Export]
	public Vector2 AreaMin = new Vector2(-10.0f, -10.0f);

	[Export]
	public Vector2 AreaMax = new Vector2(8.0f, 8.0f);

	[Export]
	public float SpawnHeight = 0.5f;

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
