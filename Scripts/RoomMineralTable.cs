/// <summary>
/// Traduce un roll 0-99 al mineral de una sala generada, ponderado para que
/// Stone sea común y Bedrock casi no aparezca. Separada de RoomGenerator
/// para poder testearla sin GodotSharp (ver Tests/GameLogic.Tests).
/// </summary>
public static class RoomMineralTable
{
	public static MineralType Pick(int roll0To99)
	{
		if (roll0To99 < 55)
		{
			return MineralType.Stone;
		}

		if (roll0To99 < 80)
		{
			return MineralType.Coal;
		}

		if (roll0To99 < 93)
		{
			return MineralType.Iron;
		}

		if (roll0To99 < 99)
		{
			return MineralType.Gold;
		}

		return MineralType.Bedrock;
	}
}
