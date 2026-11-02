using System.Collections.Generic;

/// <summary>Qué se sabe de una celda del mapa (vista desde arriba).</summary>
public enum MapCell
{
	Unknown = 0,
	Floor = 1,
	Wall = 2
}

/// <summary>
/// Lo que el jugador ya vio de la mina (GDD: el mapa "va desbloqueando zonas no antes vistas").
/// Una celda por columna (x, z) de la grilla. C# puro, para probarlo en el harness; se guarda con la partida.
/// </summary>
public sealed class MapMemory
{
	private readonly Dictionary<(int X, int Z), MapCell> cells = new();

	public int Count => cells.Count;

	public IReadOnlyDictionary<(int X, int Z), MapCell> Cells => cells;

	public MapCell Get(int x, int z) => cells.TryGetValue((x, z), out MapCell kind) ? kind : MapCell.Unknown;

	// true si cambió algo: una celda nueva, o una pared que ahora es túnel.
	public bool Reveal(int x, int z, MapCell kind)
	{
		if (kind == MapCell.Unknown || Get(x, z) == kind)
		{
			return false;
		}

		cells[(x, z)] = kind;
		return true;
	}

	public void Clear() => cells.Clear();

	// Para el JSON: x, z, tipo, x, z, tipo... (mucho más corto que un objeto por celda).
	public List<int> ToFlatList()
	{
		var flat = new List<int>(cells.Count * 3);

		foreach (var entry in cells)
		{
			flat.Add(entry.Key.X);
			flat.Add(entry.Key.Z);
			flat.Add((int)entry.Value);
		}

		return flat;
	}

	public void LoadFlatList(List<int> flat)
	{
		cells.Clear();

		if (flat == null)
		{
			return;
		}

		for (int i = 0; i + 2 < flat.Count; i += 3)
		{
			Reveal(flat[i], flat[i + 1], (MapCell)flat[i + 2]);
		}
	}

	// Las celdas dentro de un círculo de radio r (en celdas) alrededor de (cx, cz).
	public static IEnumerable<(int X, int Z)> CellsInRadius(int cx, int cz, int r)
	{
		for (int dx = -r; dx <= r; dx++)
		{
			for (int dz = -r; dz <= r; dz++)
			{
				if (dx * dx + dz * dz <= r * r)
				{
					yield return (cx + dx, cz + dz);
				}
			}
		}
	}
}
