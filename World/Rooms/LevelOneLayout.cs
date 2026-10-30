/// <summary>
/// Salas fijas del nivel 1 cerca del campamento (beat chart del GDD), para que
/// la primera partida siempre enseñe lo mismo. Coordenadas de sala; -Z es el norte.
///
///            x=0          x=1           x=2            x=3
///   z=-1                Pico viejo    Nota          GRIETA (hierro)
///   z= 0   Campamento   Mochila       Gas + comida
/// </summary>
public static class LevelOneLayout
{
	public const int ExitRoomX = 3;
	public const int ExitRoomZ = -1;

	public readonly struct RoomDesign
	{
		public readonly MineralType Mineral;
		public readonly bool HasGas;
		public readonly ChestLoot Loot;
		public readonly bool HasExit;
		public readonly string NoteText;

		public RoomDesign(MineralType mineral, bool hasGas = false, ChestLoot loot = ChestLoot.None, bool hasExit = false, string noteText = "")
		{
			Mineral = mineral;
			HasGas = hasGas;
			Loot = loot;
			HasExit = hasExit;
			NoteText = noteText;
		}
	}

	public static bool TryGet(int roomX, int roomZ, out RoomDesign design)
	{
		switch ((roomX, roomZ))
		{
			case (0, 0):
				// Campamento: siempre piedra, sin gas ni cofre.
				design = new RoomDesign(MineralType.Stone);
				return true;

			case (1, 0):
				design = new RoomDesign(MineralType.Coal, loot: ChestLoot.Backpack);
				return true;

			case (-1, 0):
				design = new RoomDesign(MineralType.Stone, loot: ChestLoot.Food);
				return true;

			case (1, -1):
				design = new RoomDesign(MineralType.Stone, loot: ChestLoot.Pickaxe);
				return true;

			case (2, 0):
				design = new RoomDesign(MineralType.Coal, hasGas: true, loot: ChestLoot.Food);
				return true;

			case (2, -1):
				design = new RoomDesign(
					MineralType.Stone,
					loot: ChestLoot.Note,
					noteText: "\"Día 14. Las paredes al ESTE de esta sala son de hierro; " +
						"detrás se siente una corriente de aire que sube. Con un pico " +
						"reforzado debería poder pasar. Si alguien lee esto: no te quedes " +
						"lejos del campamento cuando oscurezca.\""
				);
				return true;

			case (ExitRoomX, ExitRoomZ):
				design = new RoomDesign(MineralType.Iron, hasExit: true);
				return true;

			default:
				design = default;
				return false;
		}
	}

	// Nota para los cofres de salas procedurales: orienta hacia la salida sin dar un mapa.
	public static string HintTowardsExit(int roomX, int roomZ)
	{
		int dx = ExitRoomX - roomX;
		int dz = ExitRoomZ - roomZ;

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
