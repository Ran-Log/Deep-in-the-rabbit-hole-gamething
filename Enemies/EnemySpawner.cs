using Godot;
using System.Linq;

/// <summary>
/// Hace aparecer enemigos de noche, de dos formas, y nunca dentro del campamento:
/// 1. Una oleada en las salas vecinas al campamento, más grande cada noche: tienen que
///    entrar rompiendo paredes o barricadas.
/// 2. En las salas que el jugador ya visitó: excavar mucho trae más amenazas (regla del GDD).
/// Al amanecer se van todos. (Cuando haya techos: de día solo en salas con techo, regla del GDD.)
/// </summary>
public partial class EnemySpawner : Node3D
{
	[ExportCategory("EnemySpawner")]
	[ExportGroup("Referencias")]
	// Respaldo: si el nivel no define enemigos (LevelDefinition.EnemyScenes), aparece este.
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

	[ExportGroup("Posición")]
	[Export] public float SpawnHeight = 0.5f;

	// Las 4 salas pegadas al campamento (0,0): de ahí sale la oleada.
	private static readonly Vector2I[] CampNeighbors =
	{
		new Vector2I(1, 0), new Vector2I(-1, 0), new Vector2I(0, 1), new Vector2I(0, -1),
	};

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
		DayNight.DayStarted += OnDayStarted;
	}

	public override void _ExitTree()
	{
		if (DayNight != null)
		{
			DayNight.NightStarted -= OnNightStarted;
			DayNight.DayStarted -= OnDayStarted;
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

	// Al amanecer los enemigos se van (el GDD: de día se explora con libertad).
	private void OnDayStarted(int day)
	{
		var enemies = GetTree().GetNodesInGroup(Enemy.Group);

		foreach (Node enemy in enemies)
		{
			enemy.QueueFree();
		}

		if (enemies.Count > 0)
		{
			GameEvents.PostMessage("Con la luz del día, las criaturas huyen a la oscuridad");
		}
	}

	private void OnNightStarted(int day)
	{
		trickleTimer = 0.0f;

		int campWave = BaseZombiesPerNight + (day - 1) * ExtraZombiesPerDay;
		Room[] neighbors = LoadedCampNeighbors().ToArray();

		// Si el jugador está lejos, esas salas no están cargadas: esa noche no hay oleada al campamento.
		for (int i = 0; i < campWave && neighbors.Length > 0; i++)
		{
			Room room = neighbors[rng.RandiRange(0, neighbors.Length - 1)];
			TrySpawn(RoomSpawnPoint(room));
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

	// Las salas pegadas al campamento que están cargadas ahora.
	private System.Collections.Generic.IEnumerable<Room> LoadedCampNeighbors()
	{
		if (Streamer == null)
		{
			yield break;
		}

		foreach (Room room in Streamer.LoadedRooms)
		{
			if (GodotObject.IsInstanceValid(room) && CampNeighbors.Contains(room.RoomGridCoord))
			{
				yield return room;
			}
		}
	}

	// Salas cargadas que el jugador ya visitó, sin contar el campamento (ahí nunca aparecen enemigos).
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
		PackedScene scene = PickEnemyScene();
		var first = scene.Instantiate<Enemy>();

		// Algunos aparecen en grupo (ej. los conejos): SpawnGroupSize en su escena.
		int groupSize = Mathf.Max(1, first.SpawnGroupSize);

		for (int i = 0; i < groupSize; i++)
		{
			if (GetTree().GetNodesInGroup(Enemy.Group).Count >= MaxAlive)
			{
				break;
			}

			Enemy enemy = i == 0 ? first : scene.Instantiate<Enemy>();
			enemy.Position = ToLocal(globalPosition + new Vector3(i * 1.2f, 0.0f, (i % 2) * 1.2f));
			AddChild(enemy);
		}

		// Si no cupo ninguno (MaxAlive), el primero nunca entró al árbol: se libera a mano.
		if (!first.IsInsideTree())
		{
			first.Free();
		}
	}

	// Uno al azar de los enemigos del nivel actual.
	private PackedScene PickEnemyScene()
	{
		var scenes = GameData.CurrentLevel?.EnemyScenes;

		if (scenes == null || scenes.Count == 0)
		{
			return ZombieScene;
		}

		return scenes[rng.RandiRange(0, scenes.Count - 1)];
	}
}
