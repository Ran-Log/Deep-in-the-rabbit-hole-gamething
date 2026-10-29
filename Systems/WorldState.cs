using Godot;
using System.Text.Json;

/// <summary>
/// Autoload: punto de acceso único al estado guardable del mundo (semilla,
/// bloques minados, inventario, nivel del pico, cofres abiertos, defensas
/// construidas) y a guardar/cargar desde disco (user://savegame.json). La
/// lógica de datos vive en WorldSaveState (testeada aparte, sin GodotSharp,
/// ver Tests/GameLogic.Tests) — esta clase solo la conecta con Godot: tipos
/// (Vector2I/Vector3I/MineralType/ToolTier), FileAccess y señales para quien
/// quiera mostrar el estado (Hud, InventoryScreen, PlayerEquipment).
///
/// Se carga sola al iniciar (_Ready): si hay un savegame.json válido, lo usa
/// (y expone la posición guardada del jugador en LoadedPlayerPosition, que
/// Player.cs aplica); si no, arranca una partida nueva. Guardar es manual —
/// ver la acción de entrada "save" (F5 por defecto) en Player.cs.
///
/// Al ser autoload, sobrevive a ReloadCurrentScene: morir y reiniciar NO
/// borra el progreso desde el último guardado. Para empezar de cero (ej.
/// después de ganar) está <see cref="StartNewGame"/>.
/// </summary>
public partial class WorldState : Node
{
	public static WorldState Instance { get; private set; }

	/// <summary>Cambió cualquier cosa del inventario (cantidad o capacidad).</summary>
	[Signal]
	public delegate void InventoryChangedEventHandler();

	[Signal]
	public delegate void ToolChangedEventHandler(int tier);

	private static readonly string SavePath = "user://savegame.json";

	/// <summary>Raciones con las que arranca una partida nueva.</summary>
	private const int StartingFood = 2;

	private WorldSaveState state = new();

	public ulong Seed
	{
		get => state.Seed;
		private set => state.Seed = value;
	}

	/// <summary>Posición guardada del jugador, o null si no había save (partida nueva).</summary>
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

	/// <summary>Descarta todo el progreso (en memoria y en disco) y arranca con una semilla nueva.</summary>
	public void StartNewGame()
	{
		state = new WorldSaveState
		{
			Seed = ((ulong)GD.Randi() << 32) | GD.Randi(),
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

	// --- Bloques minados ---

	public bool IsCellMined(Vector2I room, Vector3I localCell)
	{
		return state.IsCellMined(room.X, room.Y, localCell.X, localCell.Y, localCell.Z);
	}

	/// <summary>
	/// Registra que se rompió una celda de una sala, para que el streaming
	/// no la regenere. Lo llama Room cuando uno de sus bloques se destruye
	/// (sea el jugador o un enemigo). Sumar el mineral al inventario es
	/// trabajo de quien mina (MiningComponent), no de esta función.
	/// </summary>
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

	/// <summary>Todos los ítems con cantidad &gt; 0 (id, cantidad), para mostrarlos en la UI.</summary>
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

	/// <summary>Agrega si cabe todo. Devuelve false con la mochila llena.</summary>
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

	/// <summary>True solo la primera vez que se pisa la sala.</summary>
	public bool MarkRoomVisited(Vector2I room) => state.MarkRoomVisited(room.X, room.Y);

	// --- Disco ---

	public void Save(Vector3 playerPosition)
	{
		WorldSaveData dto = state.ToDto(playerPosition.X, playerPosition.Y, playerPosition.Z);
		string json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });

		using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);

		if (file == null)
		{
			GD.PushError($"WorldState: no se pudo abrir {SavePath} para guardar (error {FileAccess.GetOpenError()}).");
			return;
		}

		file.StoreString(json);
		GD.Print($"WorldState: partida guardada en {SavePath}.");
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
		LoadedPlayerPosition = new Vector3(dto.PlayerX, dto.PlayerY, dto.PlayerZ);

		GD.Print($"WorldState: partida cargada desde {SavePath} (semilla {Seed}).");
		return true;
	}
}
