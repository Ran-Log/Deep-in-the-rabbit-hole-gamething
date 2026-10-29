/// <summary>
/// Catálogo de ítems del inventario. Los minerales se identifican por el
/// nombre de su <see cref="MineralType"/> ("Stone", "Coal"...), así el
/// inventario guardado sigue siendo compatible con partidas anteriores; los
/// ítems que no son minerales tienen su propio id acá.
///
/// Sin dependencia de GodotSharp para poder testearlo (ver
/// Tests/GameLogic.Tests).
/// </summary>
public static class ItemCatalog
{
	public const string Food = "Food";

	/// <summary>Vida que recupera comer una ración.</summary>
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
