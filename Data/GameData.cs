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

	public static LevelDefinition Level(int number)
	{
		foreach (LevelDefinition level in Catalog.Levels)
		{
			if (level.Number == number)
			{
				return level;
			}
		}

		GD.PushError($"GameData: falta el nivel {number} en {CatalogPath}.");
		return null;
	}

	// El nivel en que está el jugador ahora (se guarda en WorldState).
	public static LevelDefinition CurrentLevel => Level(WorldState.Instance?.CurrentLevel ?? 1);

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

	// El arma de ese nivel, o null (sin arma o no existe).
	public static WeaponDefinition Weapon(WeaponTier tier)
	{
		foreach (WeaponDefinition weapon in Catalog.Weapons)
		{
			if (weapon.Tier == tier)
			{
				return weapon;
			}
		}

		return null;
	}

	// La siguiente arma a forjar, o null si ya tiene la mejor.
	public static WeaponDefinition NextWeapon(WeaponTier current) => Weapon(current + 1);

	public static CampDefinition Camp(int level)
	{
		foreach (CampDefinition camp in Catalog.CampLevels)
		{
			if (camp.Level == level)
			{
				return camp;
			}
		}

		return null;
	}

	public static LightDefinition Light(int level)
	{
		foreach (LightDefinition light in Catalog.Lights)
		{
			if (light.Level == level)
			{
				return light;
			}
		}

		return null;
	}
}
