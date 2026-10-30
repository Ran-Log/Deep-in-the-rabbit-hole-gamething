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

	// Con tamaño par, el centro queda corrido media celda.
	public static int CenterOffset(int index, int size)
	{
		return index - size / 2;
	}
}
