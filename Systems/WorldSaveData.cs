using System.Collections.Generic;

/// <summary>
/// Clases simples que se guardan como JSON en el archivo de la partida.
/// </summary>
public sealed class WorldSaveData
{
	public ulong Seed { get; set; }
	public float PlayerX { get; set; }
	public float PlayerY { get; set; }
	public float PlayerZ { get; set; }
	// true = guardado al empezar un nivel: ignorar la posición y aparecer en el campamento.
	public bool StartAtCamp { get; set; }
	public List<MinedRoomDto> MinedRooms { get; set; } = new();
	public List<MineralCountDto> Inventory { get; set; } = new();
	public int Capacity { get; set; }
	public int ToolLevel { get; set; }
	public int Day { get; set; }
	public int CurrentLevel { get; set; }
	// Vida por id ("player", "camp").
	public Dictionary<string, int> Health { get; set; } = new();
	public List<RoomCoordDto> OpenedChests { get; set; } = new();

	// Celdas globales (de BlockGrid) con barricadas y antorchas del jugador.
	public List<MinedCellDto> PlacedDefenses { get; set; } = new();

	public List<MinedCellDto> PlacedTorches { get; set; } = new();

	public List<RoomCoordDto> VisitedRooms { get; set; } = new();
	public List<RoomCoordDto> DestroyedNests { get; set; } = new();
}

public sealed class RoomCoordDto
{
	public int RoomX { get; set; }
	public int RoomZ { get; set; }
}

public sealed class MinedRoomDto
{
	public int RoomX { get; set; }
	public int RoomZ { get; set; }
	public List<MinedCellDto> Cells { get; set; } = new();
}

public sealed class MinedCellDto
{
	public int X { get; set; }
	public int Y { get; set; }
	public int Z { get; set; }
}

public sealed class MineralCountDto
{
	public string Mineral { get; set; } = "";
	public int Count { get; set; }
}
