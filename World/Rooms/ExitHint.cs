/// <summary>
/// El texto de las notas al azar: orienta hacia la salida del nivel sin dar un mapa (GDD 2.1.5).
/// </summary>
public static class ExitHint
{
	public static string Towards(int fromX, int fromZ, int exitX, int exitZ)
	{
		int dx = exitX - fromX;
		int dz = exitZ - fromZ;

		// -Z es el norte.
		string eastWest = dx > 0 ? $"{dx} salas al este" : dx < 0 ? $"{-dx} salas al oeste" : "";
		string northSouth = dz < 0 ? $"{-dz} salas al norte" : dz > 0 ? $"{dz} salas al sur" : "";

		string direction = (eastWest, northSouth) switch
		{
			("", "") => "aquí mismo",
			(_, "") => eastWest,
			("", _) => northSouth,
			_ => $"{eastWest} y {northSouth}",
		};

		return $"\"Alguien rayó en la madera: la corriente de aire viene de unas {direction}.\"";
	}
}
