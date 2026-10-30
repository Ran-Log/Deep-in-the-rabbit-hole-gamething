/// <summary>
/// Convierte un número 0-99 en el mineral de una sala: piedra es común, bedrock casi no sale.
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
