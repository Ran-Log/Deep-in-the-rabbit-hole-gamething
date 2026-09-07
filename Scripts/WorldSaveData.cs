using System.Collections.Generic;

/// <summary>
/// DTOs planos para el archivo de guardado (JSON vía System.Text.Json).
/// Deliberadamente sin Vector2I/Vector3I/MineralType de Godot: así
/// WorldSaveState se puede testear sin GodotSharp (ver Tests/GameLogic.Tests)
/// y el formato de guardado no queda atado a cómo Godot serializa sus tipos.
/// </summary>
public sealed class WorldSaveData
{
	public ulong Seed { get; set; }
	public float PlayerX { get; set; }
	public float PlayerY { get; set; }
	public float PlayerZ { get; set; }
	public List<MinedRoomDto> MinedRooms { get; set; } = new();
	public List<MineralCountDto> Inventory { get; set; } = new();
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
