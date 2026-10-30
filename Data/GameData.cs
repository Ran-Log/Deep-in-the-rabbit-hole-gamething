using Godot;

/// <summary>
/// Acceso a los datos de diseño. Carga Data/game_data.tres una sola vez con GD.Load
/// (tip de la clase 2) y busca por mineral o por nivel de pico.
/// </summary>
public static class GameData
{
	private const string CatalogPath = "res://Data/game_data.tres";

	private static GameDataCatalog catalog;

	public static GameDataCatalog Catalog => catalog ??= GD.Load<GameDataCatalog>(CatalogPath);

	public static MineralDefinition Mineral(MineralType type)
	{
		foreach (MineralDefinition mineral in Catalog.Minerals)
		{
			if (mineral.Mineral == type)
			{
				return mineral;
			}
		}

		GD.PushError($"GameData: falta el mineral {type} en {CatalogPath}.");
		return null;
	}

	public static MineralDefinition Floor => Catalog.Floor;

	// La mejora que parte desde este nivel de pico, o null si ya está al máximo.
	public static UpgradeRecipe UpgradeFrom(ToolTier current)
	{
		foreach (UpgradeRecipe recipe in Catalog.Upgrades)
		{
			if (recipe.From == current)
			{
				return recipe;
			}
		}

		return null;
	}
}
