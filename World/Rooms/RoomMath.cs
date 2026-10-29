/// <summary>
/// Matemática pura de la huella de una Room: qué celdas son pared
/// (perímetro) y a qué offset del centro cae cada índice local. Se separó
/// de Room.cs (que sigue usando estas fórmulas) para poder testearla sin
/// GodotSharp — ver Tests/GameLogic.Tests, que compila este archivo solo,
/// sin el proyecto de Godot completo.
/// </summary>
public static class RoomMath
{
	/// <summary>
	/// True si la celda (x, z) de una habitación de width x depth cae en el
	/// borde exterior (donde van las paredes minables).
	/// </summary>
	public static bool IsPerimeterCell(int x, int z, int width, int depth)
	{
		return x == 0 || x == width - 1 || z == 0 || z == depth - 1;
	}

	/// <summary>
	/// Offset de un índice local (0..size-1) respecto al centro de la Room,
	/// misma cuenta que usa Room.GenerateWalls (`indice - tamaño / 2`). Con
	/// tamaño par el centro queda desplazado media celda; ver el doc-comment
	/// de la clase Room.
	/// </summary>
	public static int CenterOffset(int index, int size)
	{
		return index - size / 2;
	}
}
