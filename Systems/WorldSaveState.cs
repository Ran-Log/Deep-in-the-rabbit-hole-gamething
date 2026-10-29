using System.Collections.Generic;

/// <summary>
/// Estado guardable del mundo: semilla, qué celdas ya se minaron (por sala) y
/// cuántos minerales tiene el jugador. Sin dependencia de Godot para poder
/// testearlo (ver Tests/GameLogic.Tests) — <see cref="WorldState"/> es el
/// autoload que lo conecta con el juego (Vector2I/Vector3I, disco, señales).
///
/// Los minerales se guardan por nombre (string) y no como el enum
/// MineralType de Godot, por la misma razón: para que este archivo no
/// dependa de GodotSharp. El mismo diccionario guarda también ítems que no
/// son minerales (ej. comida, ver ItemCatalog), con un límite total de
/// <see cref="Capacity"/> (la mochila del GDD).
/// </summary>
public sealed class WorldSaveState
{
	public const int DefaultCapacity = 30;

	public ulong Seed { get; set; }

	/// <summary>Cuántos ítems caben en la mochila en total. Se amplía encontrando mochilas.</summary>
	public int Capacity { get; set; } = DefaultCapacity;

	/// <summary>Nivel del pico (valor de ToolTier), guardado como int igual que el resto del DTO.</summary>
	public int ToolLevel { get; set; } = 1;

	private readonly HashSet<(int RoomX, int RoomZ)> openedChests = new();
	private readonly HashSet<(int X, int Y, int Z)> placedDefenses = new();
	private readonly HashSet<(int X, int Y, int Z)> placedTorches = new();
	private readonly HashSet<(int RoomX, int RoomZ)> visitedRooms = new();

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

	public int TotalItems
	{
		get
		{
			int total = 0;

			foreach (int count in mineralCounts.Values)
			{
				total += count;
			}

			return total;
		}
	}

	public int FreeSpace => System.Math.Max(0, Capacity - TotalItems);

	/// <summary>
	/// Agrega solo si cabe TODO (todo o nada): así la mochila nunca queda
	/// por encima de su capacidad y quien llama puede avisar "mochila llena".
	/// </summary>
	public bool TryAddItem(string itemId, int amount = 1)
	{
		if (amount <= 0 || amount > FreeSpace)
		{
			return false;
		}

		AddMineral(itemId, amount);
		return true;
	}

	/// <summary>Quita ítems si hay suficientes. Al llegar a 0 la entrada desaparece (no se muestra "x0").</summary>
	public bool RemoveItem(string itemId, int amount = 1)
	{
		int current = GetMineralCount(itemId);

		if (amount <= 0 || current < amount)
		{
			return false;
		}

		if (current == amount)
		{
			mineralCounts.Remove(itemId);
		}
		else
		{
			mineralCounts[itemId] = current - amount;
		}

		return true;
	}

	public bool HasItems(IReadOnlyDictionary<string, int> cost)
	{
		foreach (var entry in cost)
		{
			if (GetMineralCount(entry.Key) < entry.Value)
			{
				return false;
			}
		}

		return true;
	}

	/// <summary>Cobra un costo completo, o nada si falta algo.</summary>
	public bool TrySpend(IReadOnlyDictionary<string, int> cost)
	{
		if (!HasItems(cost))
		{
			return false;
		}

		foreach (var entry in cost)
		{
			RemoveItem(entry.Key, entry.Value);
		}

		return true;
	}

	public bool IsChestOpened(int roomX, int roomZ) => openedChests.Contains((roomX, roomZ));

	public void MarkChestOpened(int roomX, int roomZ) => openedChests.Add((roomX, roomZ));

	public IEnumerable<(int X, int Y, int Z)> PlacedDefenses => placedDefenses;

	public void AddDefense(int x, int y, int z) => placedDefenses.Add((x, y, z));

	public void RemoveDefense(int x, int y, int z) => placedDefenses.Remove((x, y, z));

	public IEnumerable<(int X, int Y, int Z)> PlacedTorches => placedTorches;

	public void AddTorch(int x, int y, int z) => placedTorches.Add((x, y, z));

	/// <summary>Salas por las que ya pasó el jugador: solo ahí pueden aparecer enemigos de noche.</summary>
	public bool IsRoomVisited(int roomX, int roomZ) => visitedRooms.Contains((roomX, roomZ));

	/// <summary>Devuelve true solo la primera vez (sala recién descubierta).</summary>
	public bool MarkRoomVisited(int roomX, int roomZ) => visitedRooms.Add((roomX, roomZ));

	public WorldSaveData ToDto(float playerX, float playerY, float playerZ)
	{
		var dto = new WorldSaveData
		{
			Seed = Seed,
			PlayerX = playerX,
			PlayerY = playerY,
			PlayerZ = playerZ,
			Capacity = Capacity,
			ToolLevel = ToolLevel,
		};

		foreach (var chest in openedChests)
		{
			dto.OpenedChests.Add(new RoomCoordDto { RoomX = chest.RoomX, RoomZ = chest.RoomZ });
		}

		foreach (var cell in placedDefenses)
		{
			dto.PlacedDefenses.Add(new MinedCellDto { X = cell.X, Y = cell.Y, Z = cell.Z });
		}

		foreach (var cell in placedTorches)
		{
			dto.PlacedTorches.Add(new MinedCellDto { X = cell.X, Y = cell.Y, Z = cell.Z });
		}

		foreach (var room in visitedRooms)
		{
			dto.VisitedRooms.Add(new RoomCoordDto { RoomX = room.RoomX, RoomZ = room.RoomZ });
		}

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
		var state = new WorldSaveState
		{
			Seed = dto.Seed,
			// Saves anteriores a la mochila/al pico no traen estos campos (llegan en 0).
			Capacity = dto.Capacity > 0 ? dto.Capacity : DefaultCapacity,
			ToolLevel = dto.ToolLevel > 0 ? dto.ToolLevel : 1,
		};

		foreach (var chest in dto.OpenedChests)
		{
			state.MarkChestOpened(chest.RoomX, chest.RoomZ);
		}

		foreach (var cell in dto.PlacedDefenses)
		{
			state.AddDefense(cell.X, cell.Y, cell.Z);
		}

		foreach (var cell in dto.PlacedTorches)
		{
			state.AddTorch(cell.X, cell.Y, cell.Z);
		}

		foreach (var room in dto.VisitedRooms)
		{
			state.MarkRoomVisited(room.RoomX, room.RoomZ);
		}

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
