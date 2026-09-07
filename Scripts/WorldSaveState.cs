using System.Collections.Generic;

/// <summary>
/// Estado guardable del mundo: semilla, qué celdas ya se minaron (por sala) y
/// cuántos minerales tiene el jugador. Sin dependencia de Godot para poder
/// testearlo (ver Tests/GameLogic.Tests) — <see cref="WorldState"/> es el
/// autoload que lo conecta con el juego (Vector2I/Vector3I, disco, señales).
///
/// Los minerales se guardan por nombre (string) y no como el enum
/// MineralType de Godot, por la misma razón: para que este archivo no
/// dependa de GodotSharp.
/// </summary>
public sealed class WorldSaveState
{
	public ulong Seed { get; set; }

	private readonly Dictionary<(int RoomX, int RoomZ), HashSet<(int X, int Y, int Z)>> minedCells = new();
	private readonly Dictionary<string, int> mineralCounts = new();

	public IReadOnlyDictionary<string, int> MineralCounts => mineralCounts;

	public bool IsCellMined(int roomX, int roomZ, int cellX, int cellY, int cellZ)
	{
		return minedCells.TryGetValue((roomX, roomZ), out var cells)
			&& cells.Contains((cellX, cellY, cellZ));
	}

	public void MarkCellMined(int roomX, int roomZ, int cellX, int cellY, int cellZ)
	{
		if (!minedCells.TryGetValue((roomX, roomZ), out var cells))
		{
			cells = new HashSet<(int, int, int)>();
			minedCells[(roomX, roomZ)] = cells;
		}

		cells.Add((cellX, cellY, cellZ));
	}

	public int GetMineralCount(string mineral)
	{
		return mineralCounts.TryGetValue(mineral, out int count) ? count : 0;
	}

	public int AddMineral(string mineral, int amount = 1)
	{
		int newTotal = GetMineralCount(mineral) + amount;

		mineralCounts[mineral] = newTotal;

		return newTotal;
	}

	public WorldSaveData ToDto(float playerX, float playerY, float playerZ)
	{
		var dto = new WorldSaveData
		{
			Seed = Seed,
			PlayerX = playerX,
			PlayerY = playerY,
			PlayerZ = playerZ,
		};

		foreach (var entry in minedCells)
		{
			var roomDto = new MinedRoomDto { RoomX = entry.Key.RoomX, RoomZ = entry.Key.RoomZ };

			foreach (var cell in entry.Value)
			{
				roomDto.Cells.Add(new MinedCellDto { X = cell.X, Y = cell.Y, Z = cell.Z });
			}

			dto.MinedRooms.Add(roomDto);
		}

		foreach (var entry in mineralCounts)
		{
			dto.Inventory.Add(new MineralCountDto { Mineral = entry.Key, Count = entry.Value });
		}

		return dto;
	}

	public static WorldSaveState FromDto(WorldSaveData dto)
	{
		var state = new WorldSaveState { Seed = dto.Seed };

		foreach (var roomDto in dto.MinedRooms)
		{
			foreach (var cell in roomDto.Cells)
			{
				state.MarkCellMined(roomDto.RoomX, roomDto.RoomZ, cell.X, cell.Y, cell.Z);
			}
		}

		foreach (var entry in dto.Inventory)
		{
			state.AddMineral(entry.Mineral, entry.Count);
		}

		return state;
	}
}
