/// <summary>
/// Cuentas de la forma de una sala: qué celdas son pared y dónde cae cada celda respecto al centro.
/// </summary>
public static class RoomMath
{
	// True si la celda (x, z) está en el borde, donde van las paredes.
	public static bool IsPerimeterCell(int x, int z, int width, int depth)
	{
		return x == 0 || x == width - 1 || z == 0 || z == depth - 1;
	}

	// Rectángulos (en X y Z) que forman el techo. Con tragaluz: 4 franjas alrededor del agujero.
	public static System.Collections.Generic.List<(float MinX, float MinZ, float MaxX, float MaxZ)> CeilingPieces(
		(float MinX, float MinZ, float MaxX, float MaxZ) full,
		(float MinX, float MinZ, float MaxX, float MaxZ) hole,
		bool hasHole)
	{
		if (!hasHole)
		{
			return new() { full };
		}

		return new()
		{
			(full.MinX, full.MinZ, full.MaxX, hole.MinZ), // norte
			(full.MinX, hole.MaxZ, full.MaxX, full.MaxZ), // sur
			(full.MinX, hole.MinZ, hole.MinX, hole.MaxZ), // oeste
			(hole.MaxX, hole.MinZ, full.MaxX, hole.MaxZ), // este
		};
	}

	// Con tamaño par, el centro queda corrido media celda.
	public static int CenterOffset(int index, int size)
	{
		return index - size / 2;
	}
}
