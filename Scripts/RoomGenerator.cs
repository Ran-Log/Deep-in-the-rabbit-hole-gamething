using Godot;

/// <summary>
/// Genera la configuración de una Room de forma determinística a partir de
/// su coordenada en la grilla infinita y la semilla del mundo: la misma
/// coordenada siempre da la misma sala, sin importar el orden en que se
/// visite — clave para poder cargar y descargar salas libremente mientras
/// el jugador se mueve (ver WorldStreamer). La coordenada (0,0) es especial:
/// siempre es una sala "segura" de Stone, para que el punto de partida no
/// dependa de la semilla.
///
/// La matemática pura (hash, tabla de minerales) vive en DeterministicHash
/// y RoomMineralTable — testeadas aparte, sin GodotSharp, en
/// Tests/GameLogic.Tests.
/// </summary>
public static class RoomGenerator
{
	public const int RoomWidth = 12;
	public const int RoomDepth = 12;
	public const int RoomWallHeight = 3;

	public static RoomSpec Generate(ulong seed, Vector2I gridCoord, float cellSize)
	{
		float spacing = RoomWidth * cellSize;
		Vector3 position = new Vector3(gridCoord.X * spacing, 0.0f, gridCoord.Y * spacing);

		if (gridCoord == Vector2I.Zero)
		{
			// Punto de partida: siempre igual, sin importar la semilla.
			return new RoomSpec("RoomStart", position, RoomWidth, RoomDepth, RoomWallHeight, MineralType.Stone, 5, gridCoord);
		}

		ulong hash = DeterministicHash.Combine(seed, gridCoord.X, gridCoord.Y);

		MineralType mineral = RoomMineralTable.Pick((int)(hash % 100));
		int health = 4 + (int)((hash >> 8) % 5); // 4..8

		// Grisú: solo en salas de Coal, y ni siquiera en todas — un ~30% de
		// ellas. Usa otro "canal" del mismo hash (bits altos) para que no
		// esté correlacionado con qué mineral o salud salió.
		bool hasGas = mineral == MineralType.Coal && (hash >> 16) % 100 < 30;

		string name = $"Room_{gridCoord.X}_{gridCoord.Y}";

		return new RoomSpec(name, position, RoomWidth, RoomDepth, RoomWallHeight, mineral, health, gridCoord, hasGas);
	}
}
