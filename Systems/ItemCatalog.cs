/// <summary>
/// Ítems del inventario. Los minerales usan el nombre de su MineralType como id
/// ("Stone", "Coal"...); los demás tienen su id acá.
/// </summary>
public static class ItemCatalog
{
	public const string Food = "Food";
	public const string Water = "Water";

	// Lo que hace una ración: baja el hambre y cura un poco (GDD: "en algunos casos recuperan vida").
	public const int FoodHunger = 40;
	public const int FoodHeal = 10;

	// Lo que quita una cantimplora de sed.
	public const int WaterThirst = 50;

	public static string IdOf(MineralType mineral)
	{
		return mineral.ToString();
	}

	public static bool IsConsumable(string itemId)
	{
		return itemId == Food || itemId == Water;
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
			Water => "Cantimplora de agua",
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
