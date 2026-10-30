/// <summary>
/// Ítems del inventario. Los minerales usan el nombre de su MineralType como id
/// ("Stone", "Coal"...); los demás tienen su id acá.
/// </summary>
public static class ItemCatalog
{
	public const string Food = "Food";

	// Vida que recupera una ración.
	public const int FoodHeal = 30;

	public static string IdOf(MineralType mineral)
	{
		return mineral.ToString();
	}

	public static bool IsConsumable(string itemId)
	{
		return itemId == Food;
	}

	public static string DisplayName(string itemId)
	{
		return itemId switch
		{
			"Stone" => "Piedra",
			"Coal" => "Carbón",
			"Iron" => "Hierro",
			"Gold" => "Oro",
			"Bedrock" => "Roca madre",
			Food => "Ración de comida",
			_ => itemId,
		};
	}

	public static string ToolName(ToolTier tier)
	{
		return tier switch
		{
			ToolTier.Basic => "Pico básico",
			ToolTier.Reinforced => "Pico reforzado",
			ToolTier.Advanced => "Pico avanzado",
			_ => "Sin herramienta",
		};
	}
}
