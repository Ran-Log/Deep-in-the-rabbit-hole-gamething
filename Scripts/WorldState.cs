using Godot;
using System.Text.Json;

/// <summary>
/// Autoload: punto de acceso único al estado guardable del mundo (semilla,
/// bloques minados, inventario) y a guardar/cargar desde disco
/// (user://savegame.json). La lógica de datos vive en WorldSaveState
/// (testeada aparte, sin GodotSharp, ver Tests/GameLogic.Tests) — esta clase
/// solo la conecta con Godot: tipos (Vector2I/Vector3I/MineralType),
/// FileAccess y la señal MineralCollected para quien quiera mostrar el
/// inventario (ver Hud.cs).
///
/// Se carga sola al iniciar (_Ready): si hay un savegame.json válido, lo usa
/// (y expone la posición guardada del jugador en LoadedPlayerPosition, que
/// Player.cs aplica); si no, arranca una semilla nueva. Guardar es manual —
/// ver la acción de entrada "save" (F5 por defecto) en Player.cs.
/// </summary>
public partial class WorldState : Node
{
	public static WorldState Instance { get; private set; }

	[Signal]
	public delegate void MineralCollectedEventHandler(int mineral, int newTotal);

	private static readonly string SavePath = "user://savegame.json";

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
			Seed = ((ulong)GD.Randi() << 32) | GD.Randi();
			GD.Print($"WorldState: no había partida guardada, nueva semilla {Seed}.");
		}
	}

	public bool IsCellMined(Vector2I room, Vector3I localCell)
	{
		return state.IsCellMined(room.X, room.Y, localCell.X, localCell.Y, localCell.Z);
	}

	/// <summary>
	/// Registra que se minó una celda y suma el mineral correspondiente al
	/// inventario. Lo llama Room cuando uno de sus bloques se destruye (ver
	/// Room.SpawnBlock) — Room es quien sabe en qué sala y celda estaba.
	/// </summary>
	public void MarkCellMined(Vector2I room, Vector3I localCell, MineralType mineral)
	{
		state.MarkCellMined(room.X, room.Y, localCell.X, localCell.Y, localCell.Z);

		int newTotal = state.AddMineral(mineral.ToString(), 1);

		EmitSignal(SignalName.MineralCollected, (int)mineral, newTotal);
	}

	public int GetMineralCount(MineralType mineral)
	{
		return state.GetMineralCount(mineral.ToString());
	}

	/// <summary>Todos los minerales con conteo &gt; 0, para mostrar un resumen (ver Hud.cs).</summary>
	public System.Collections.Generic.IEnumerable<(MineralType Mineral, int Count)> GetMineralCounts()
	{
		foreach (var entry in state.MineralCounts)
		{
			if (System.Enum.TryParse(entry.Key, out MineralType mineral))
			{
				yield return (mineral, entry.Value);
			}
		}
	}

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
