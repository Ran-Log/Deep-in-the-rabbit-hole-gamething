using System.Collections.Generic;

/// <summary>
/// Los datos de la partida: semilla, celdas minadas por sala, inventario (la mochila
/// del GDD), pico, cofres, defensas, antorchas y salas visitadas. WorldState lo conecta con Godot.
/// </summary>
public sealed class WorldSaveState
{
	public const int DefaultCapacity = 30;

	// Ids de vida guardada (HealthComponent.SaveId en player.tscn y Camp.tscn).
	public const string PlayerHealthId = "player";
	public const string CampHealthId = "camp";

	public ulong Seed { get; set; }

	// Ítems que caben en la mochila. Crece al encontrar mochilas.
	public int Capacity { get; set; } = DefaultCapacity;

	// Nivel del pico (valor de ToolTier).
	public int ToolLevel { get; set; } = 1;

	public int Day { get; set; } = 1;

	// Nivel de la mina (1 = túneles superficiales, 2 = cavernas medias, 3 = profundidades).
	public int CurrentLevel { get; set; } = 1;

	// Vida guardada por id (ej. "player", "camp"). Ver HealthComponent.SaveId.
	private readonly Dictionary<string, int> healthById = new();

	public void SetHealth(string id, int value) => healthById[id] = value;

	public bool TryGetHealth(string id, out int value) => healthById.TryGetValue(id, out value);

	// Bajar al siguiente nivel: el mundo es nuevo (y su campamento también), pero el
	// jugador conserva lo que lleva (inventario, pico, mochila) y el día sigue corriendo.
	public void StartNextLevel()
	{
		CurrentLevel++;

		minedCells.Clear();
		openedChests.Clear();
		placedDefenses.Clear();
		placedTorches.Clear();
		visitedRooms.Clear();
		destroyedNests.Clear();
		healthById.Remove(CampHealthId);
	}

	private readonly HashSet<(int RoomX, int RoomZ)> openedChests = new();
	private readonly HashSet<(int X, int Y, int Z)> placedDefenses = new();
	private readonly HashSet<(int X, int Y, int Z)> placedTorches = new();
	private readonly HashSet<(int RoomX, int RoomZ)> visitedRooms = new();
	private readonly HashSet<(int RoomX, int RoomZ)> destroyedNests = new();

	// GDD: al destruir un nido de hormigas, dejan de aparecer para siempre.
	public bool IsNestDestroyed(int roomX, int roomZ) => destroyedNests.Contains((roomX, roomZ));

	public void MarkNestDestroyed(int roomX, int roomZ) => destroyedNests.Add((roomX, roomZ));

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

	// Todo o nada: si no cabe todo, no agrega nada (así se puede avisar "mochila llena").
	public bool TryAddItem(string itemId, int amount = 1)
	{
		if (amount <= 0 || amount > FreeSpace)
		{
			return false;
		}

		AddMineral(itemId, amount);
		return true;
	}

	// Al llegar a 0 la entrada se borra, para no mostrar "x0".
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

	// Cobra todo el costo, o nada si falta algo.
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

	// Solo en salas visitadas aparecen enemigos de noche.
	public bool IsRoomVisited(int roomX, int roomZ) => visitedRooms.Contains((roomX, roomZ));

	// true solo la primera vez (sala recién descubierta).
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
			Day = Day,
			CurrentLevel = CurrentLevel,
			Health = new Dictionary<string, int>(healthById),
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

		foreach (var nest in destroyedNests)
		{
			dto.DestroyedNests.Add(new RoomCoordDto { RoomX = nest.RoomX, RoomZ = nest.RoomZ });
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
			// Saves viejos no traen estos campos (llegan en 0).
			Capacity = dto.Capacity > 0 ? dto.Capacity : DefaultCapacity,
			ToolLevel = dto.ToolLevel > 0 ? dto.ToolLevel : 1,
			Day = dto.Day > 0 ? dto.Day : 1,
			CurrentLevel = dto.CurrentLevel > 0 ? dto.CurrentLevel : 1,
		};

		foreach (var entry in dto.Health ?? new Dictionary<string, int>())
		{
			state.SetHealth(entry.Key, entry.Value);
		}

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

		foreach (var nest in dto.DestroyedNests ?? new List<RoomCoordDto>())
		{
			state.MarkNestDestroyed(nest.RoomX, nest.RoomZ);
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
