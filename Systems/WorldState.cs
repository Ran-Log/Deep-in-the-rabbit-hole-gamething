using Godot;
using System.Text.Json;

/// <summary>
/// Autoload con el estado guardable del mundo (semilla, bloques minados, inventario,
/// pico, cofres, defensas) y el guardado en user://savegame.json. Los datos viven en
/// WorldSaveState; esta clase los conecta con Godot (tipos, archivos y señales).
/// Carga sola al iniciar; se guarda con F5 o al dormir. Al morir, "Reintentar" vuelve al último guardado.
/// </summary>
public partial class WorldState : Node
{
	public static WorldState Instance { get; private set; }

	// Cambió algo del inventario (cantidad o capacidad).
	[Signal]
	public delegate void InventoryChangedEventHandler();

	[Signal]
	public delegate void ToolChangedEventHandler(int tier);

	private static readonly string SavePath = "user://savegame.json";

	// Raciones con las que empieza una partida nueva.
	private const int StartingFood = 2;

	private WorldSaveState state = new();

	public ulong Seed
	{
		get => state.Seed;
		private set => state.Seed = value;
	}

	// Para el botón "Continuar" del menú principal.
	public bool HasSaveFile => FileAccess.FileExists(SavePath);

	// null si es partida nueva.
	public Vector3? LoadedPlayerPosition { get; private set; }

	public override void _EnterTree()
	{
		Instance = this;
	}

	public override void _ExitTree()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	public override void _Ready()
	{
		if (!LoadFromDisk())
		{
			StartNewGame();
		}
	}

	// Borra todo el progreso (en memoria y en disco). Sin semilla, usa una nueva al azar.
	public void StartNewGame(ulong? seed = null)
	{
		state = new WorldSaveState
		{
			Seed = seed ?? (((ulong)GD.Randi() << 32) | GD.Randi()),
		};

		state.TryAddItem(ItemCatalog.Food, StartingFood);
		LoadedPlayerPosition = null;

		if (FileAccess.FileExists(SavePath))
		{
			DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(SavePath));
		}

		GD.Print($"WorldState: partida nueva, semilla {Seed}.");

		EmitSignal(SignalName.InventoryChanged);
		EmitSignal(SignalName.ToolChanged, (int)Tool);
	}

	// "Reintentar" al morir: descarta lo no guardado. Si nunca se guardó, empieza de cero con la misma semilla.
	public void RevertToLastSave()
	{
		if (!LoadFromDisk())
		{
			StartNewGame(Seed);
		}
	}

	// --- Niveles ---

	public int CurrentLevel => state.CurrentLevel;

	// Mensaje para mostrar al llegar a un nivel nuevo (el HUD lo muestra una vez y lo borra).
	public string PendingArrivalMessage { get; set; } = "";

	// Baja al siguiente nivel: mundo y campamento nuevos, el jugador conserva lo que lleva.
	// Se guarda enseguida, para que no se pierda el avance si el juego se cierra.
	public void AdvanceLevel()
	{
		state.StartNextLevel();
		LoadedPlayerPosition = null;

		LevelDefinition level = GameData.Level(state.CurrentLevel);
		PendingArrivalMessage = $"Nivel {state.CurrentLevel}: {level?.DisplayName}";

		// La posición se ignora: al cargar, LoadedPlayerPosition queda null y se empieza en el campamento.
		SaveWithoutPosition();
	}

	// --- Vida y día guardados ---

	public int SavedDay => state.Day;

	public bool TryGetSavedHealth(string id, out int value) => state.TryGetHealth(id, out value);

	// --- Bloques minados ---

	public bool IsCellMined(Vector2I room, Vector3I localCell)
	{
		return state.IsCellMined(room.X, room.Y, localCell.X, localCell.Y, localCell.Z);
	}

	// Para que la sala no regenere el bloque al volver a cargarse. Lo llama Room.
	public void MarkCellMined(Vector2I room, Vector3I localCell)
	{
		state.MarkCellMined(room.X, room.Y, localCell.X, localCell.Y, localCell.Z);
	}

	// --- Inventario ---

	public int Capacity => state.Capacity;

	public int TotalItems => state.TotalItems;

	public int GetItemCount(string itemId)
	{
		return state.GetMineralCount(itemId);
	}

	// Ítems con cantidad > 0, para la UI.
	public System.Collections.Generic.IEnumerable<(string ItemId, int Count)> GetItems()
	{
		foreach (var entry in state.MineralCounts)
		{
			if (entry.Value > 0)
			{
				yield return (entry.Key, entry.Value);
			}
		}
	}

	// Agrega solo si cabe todo.
	public bool TryAddItem(string itemId, int amount = 1)
	{
		if (!state.TryAddItem(itemId, amount))
		{
			return false;
		}

		EmitSignal(SignalName.InventoryChanged);
		return true;
	}

	public bool RemoveItem(string itemId, int amount = 1)
	{
		if (!state.RemoveItem(itemId, amount))
		{
			return false;
		}

		EmitSignal(SignalName.InventoryChanged);
		return true;
	}

	public bool HasItems(System.Collections.Generic.IReadOnlyDictionary<string, int> cost)
	{
		return state.HasItems(cost);
	}

	public bool TrySpend(System.Collections.Generic.IReadOnlyDictionary<string, int> cost)
	{
		if (!state.TrySpend(cost))
		{
			return false;
		}

		EmitSignal(SignalName.InventoryChanged);
		return true;
	}

	public void IncreaseCapacity(int extraSlots)
	{
		state.Capacity += extraSlots;
		EmitSignal(SignalName.InventoryChanged);
	}

	// --- Equipamiento ---

	public ToolTier Tool
	{
		get => (ToolTier)state.ToolLevel;
		set
		{
			if ((int)value == state.ToolLevel)
			{
				return;
			}

			state.ToolLevel = (int)value;
			EmitSignal(SignalName.ToolChanged, (int)value);
		}
	}

	// --- Cofres ---

	public bool IsChestOpened(Vector2I room) => state.IsChestOpened(room.X, room.Y);

	public void MarkChestOpened(Vector2I room) => state.MarkChestOpened(room.X, room.Y);

	// --- Defensas del campamento (celdas globales de BlockGrid) ---

	public System.Collections.Generic.IEnumerable<Vector3I> GetPlacedDefenses()
	{
		foreach (var cell in state.PlacedDefenses)
		{
			yield return new Vector3I(cell.X, cell.Y, cell.Z);
		}
	}

	public void AddDefense(Vector3I cell) => state.AddDefense(cell.X, cell.Y, cell.Z);

	public void RemoveDefense(Vector3I cell) => state.RemoveDefense(cell.X, cell.Y, cell.Z);

	// --- Antorchas puestas por el jugador (celdas globales) ---

	public System.Collections.Generic.IEnumerable<Vector3I> GetPlacedTorches()
	{
		foreach (var cell in state.PlacedTorches)
		{
			yield return new Vector3I(cell.X, cell.Y, cell.Z);
		}
	}

	public void AddTorch(Vector3I cell) => state.AddTorch(cell.X, cell.Y, cell.Z);

	// --- Salas visitadas ---

	public bool IsRoomVisited(Vector2I room) => state.IsRoomVisited(room.X, room.Y);

	// true solo la primera vez que se entra a la sala.
	public bool MarkRoomVisited(Vector2I room) => state.MarkRoomVisited(room.X, room.Y);

	// --- Disco ---

	public void Save(Vector3 playerPosition)
	{
		CollectHealthAndDay(includeCamp: true);

		if (WriteToDisk(state.ToDto(playerPosition.X, playerPosition.Y, playerPosition.Z)))
		{
			// Para que "Reintentar" reaparezca aquí y no donde empezó la sesión.
			LoadedPlayerPosition = playerPosition;
		}
	}

	// Al empezar un nivel: sin posición (se aparece en el campamento) y sin la vida del campamento anterior.
	private void SaveWithoutPosition()
	{
		CollectHealthAndDay(includeCamp: false);

		WorldSaveData dto = state.ToDto(0.0f, 0.0f, 0.0f);
		dto.StartAtCamp = true;
		WriteToDisk(dto);
	}

	private bool WriteToDisk(WorldSaveData dto)
	{
		string json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });

		using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);

		if (file == null)
		{
			GD.PushError($"WorldState: no se pudo abrir {SavePath} para guardar (error {FileAccess.GetOpenError()}).");
			return false;
		}

		file.StoreString(json);
		GD.Print($"WorldState: partida guardada en {SavePath}.");
		return true;
	}

	// Junta la vida de cada HealthComponent con SaveId y el día actual.
	private void CollectHealthAndDay(bool includeCamp)
	{
		foreach (Node node in GetTree().GetNodesInGroup(HealthComponent.SavedGroup))
		{
			if (node is not HealthComponent health || string.IsNullOrEmpty(health.SaveId))
			{
				continue;
			}

			if (includeCamp || health.SaveId != WorldSaveState.CampHealthId)
			{
				state.SetHealth(health.SaveId, health.Current);
			}
		}

		if (GetTree().GetFirstNodeInGroup(DayNightCycle.Group) is DayNightCycle dayNight)
		{
			state.Day = dayNight.Day;
		}
	}

	private bool LoadFromDisk()
	{
		if (!FileAccess.FileExists(SavePath))
		{
			return false;
		}

		using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);

		if (file == null)
		{
			GD.PushWarning($"WorldState: no se pudo abrir {SavePath} para cargar (error {FileAccess.GetOpenError()}).");
			return false;
		}

		string json = file.GetAsText();
		WorldSaveData dto;

		try
		{
			dto = JsonSerializer.Deserialize<WorldSaveData>(json);
		}
		catch (JsonException e)
		{
			GD.PushError($"WorldState: {SavePath} está corrupto, se ignora ({e.Message}).");
			return false;
		}

		if (dto == null)
		{
			return false;
		}

		state = WorldSaveState.FromDto(dto);

		// Guardado al empezar un nivel: se aparece en el campamento, no en una posición guardada.
		LoadedPlayerPosition = dto.StartAtCamp ? null : new Vector3(dto.PlayerX, dto.PlayerY, dto.PlayerZ);

		GD.Print($"WorldState: partida cargada desde {SavePath} (semilla {Seed}).");
		return true;
	}
}
