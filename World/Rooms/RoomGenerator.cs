using Godot;

/// <summary>
/// Genera la configuración de una Room de forma determinística a partir de
/// su coordenada en la grilla infinita y la semilla del mundo: la misma
/// coordenada siempre da la misma sala, sin importar el orden en que se
/// visite — clave para poder cargar y descargar salas libremente mientras
/// el jugador se mueve (ver WorldStreamer). La coordenada (0,0) es especial:
/// siempre es una sala "segura" de Stone (el campamento), y las salas del
/// Nivel 1 cercanas a ella vienen fijas desde LevelOneLayout.
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

	/// <summary>Probabilidad (0-100) de que una sala procedural tenga un cofre.</summary>
	public const int ChestChancePercent = 20;

	public static RoomSpec Generate(ulong seed, Vector2I gridCoord, float cellSize)
	{
		float spacing = RoomWidth * cellSize;
		Vector3 position = new Vector3(gridCoord.X * spacing, 0.0f, gridCoord.Y * spacing);
		string name = $"Room_{gridCoord.X}_{gridCoord.Y}";
		ulong hash = DeterministicHash.Combine(seed, gridCoord.X, gridCoord.Y);
		int health = 4 + (int)((hash >> 8) % 5); // 4..8

		// Salas del Nivel 1 diseñadas a mano (campamento, cofres, gas, salida):
		// mismo contenido sin importar la semilla. Ver LevelOneLayout.
		if (LevelOneLayout.TryGet(gridCoord.X, gridCoord.Y, out LevelOneLayout.RoomDesign design))
		{
			return new RoomSpec(name, position, RoomWidth, RoomDepth, RoomWallHeight, design.Mineral, health, gridCoord,
				design.HasGas, design.Loot, design.NoteText, design.HasExit);
		}

		MineralType mineral = RoomMineralTable.Pick((int)(hash % 100));

		// Grisú: solo en salas de Coal, y ni siquiera en todas — un ~30% de
		// ellas. Usa otro "canal" del mismo hash (bits altos) para que no
		// esté correlacionado con qué mineral o salud salió.
		bool hasGas = mineral == MineralType.Coal && (hash >> 16) % 100 < 30;

		// Cofres al azar en el resto del mundo: comida o una nota que orienta
		// hacia la salida (GDD: las notas indican qué hay cerca, no un mapa).
		ChestLoot loot = ChestLoot.None;
		string noteText = "";

		if ((hash >> 24) % 100 < ChestChancePercent)
		{
			bool isNote = (hash >> 40) % 100 < 40;

			loot = isNote ? ChestLoot.Note : ChestLoot.Food;
			noteText = isNote ? LevelOneLayout.HintTowardsExit(gridCoord.X, gridCoord.Y) : "";
		}

		return new RoomSpec(name, position, RoomWidth, RoomDepth, RoomWallHeight, mineral, health, gridCoord, hasGas, loot, noteText);
	}
}
