using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Mejoras de la estación (GDD 2.1.4: picos, armas y antorchas) y costos de construir.
/// Los datos salen de Data/Upgrades, Data/Weapons y Data/Lights.
/// </summary>
public static class UpgradeTable
{
	public const int DefenseStoneCost = 2;

	public const int TorchCoalCost = 1;

	private static readonly Dictionary<string, int> NoCost = new();

	// Siguiente nivel del pico y su costo (ítem → cantidad). false si ya está al máximo.
	public static bool TryGetNext(ToolTier current, out ToolTier next, out IReadOnlyDictionary<string, int> cost)
	{
		// Sin pico se mejora igual que desde el básico.
		UpgradeRecipe recipe = GameData.UpgradeFrom(current == ToolTier.None ? ToolTier.Basic : current);

		if (recipe == null)
		{
			next = current;
			cost = NoCost;
			return false;
		}

		next = recipe.To;
		cost = recipe.CostByItemId();
		return true;
	}

	// Las filas del menú, en orden: pico, arma, antorchas.
	public static List<UpgradeOption> Options(PlayerEquipment equipment)
	{
		return new List<UpgradeOption>
		{
			PickaxeOption(equipment.Tool),
			WeaponOption(equipment.Weapon),
			LightOption(equipment.LightLevel),
		};
	}

	// Gasta los minerales y aplica la mejora. El mensaje dice qué pasó.
	public static bool TryBuy(UpgradeOption option, PlayerEquipment equipment, out string message)
	{
		if (option.IsMax)
		{
			message = $"{option.Current} ya está al máximo";
			return false;
		}

		if (WorldState.Instance == null || !WorldState.Instance.TrySpend(option.Cost))
		{
			message = $"Te faltan materiales: {FormatMissing(option.Cost)}";
			return false;
		}

		option.Apply(equipment);
		message = $"¡Mejoraste a {option.Next}!";
		return true;
	}

	public static string FormatCost(IReadOnlyDictionary<string, int> cost)
	{
		return string.Join(", ", cost.Select(entry => $"{entry.Value} {ItemCatalog.DisplayName(entry.Key)}"));
	}

	public static string FormatMissing(IReadOnlyDictionary<string, int> cost)
	{
		var missing = cost
			.Select(entry => (entry.Key, Missing: entry.Value - (WorldState.Instance?.GetItemCount(entry.Key) ?? 0)))
			.Where(entry => entry.Missing > 0)
			.Select(entry => $"{entry.Missing} {ItemCatalog.DisplayName(entry.Key)}");

		return string.Join(", ", missing);
	}

	private static UpgradeOption PickaxeOption(ToolTier current)
	{
		string currentName = ItemCatalog.ToolName(current);

		if (!TryGetNext(current, out ToolTier next, out var cost))
		{
			return new UpgradeOption("Pico", currentName, "", NoCost, _ => false);
		}

		return new UpgradeOption("Pico", currentName, ItemCatalog.ToolName(next), cost, equipment => equipment.UpgradeTo(next));
	}

	private static UpgradeOption WeaponOption(WeaponTier current)
	{
		string currentName = GameData.Weapon(current)?.DisplayName ?? "Sin arma (peleas con el pico)";
		WeaponDefinition next = GameData.NextWeapon(current);

		if (next == null)
		{
			return new UpgradeOption("Arma", currentName, "", NoCost, _ => false);
		}

		return new UpgradeOption("Arma", currentName, next.DisplayName, next.CostByItemId(), equipment => equipment.UpgradeWeaponTo(next.Tier));
	}

	private static UpgradeOption LightOption(int currentLevel)
	{
		string currentName = GameData.Light(currentLevel)?.DisplayName ?? "Antorchas";
		LightDefinition next = GameData.Light(currentLevel + 1);

		if (next == null)
		{
			return new UpgradeOption("Antorchas", currentName, "", NoCost, _ => false);
		}

		return new UpgradeOption("Antorchas", currentName, next.DisplayName, next.CostByItemId(), equipment => equipment.UpgradeLightTo(next.Level));
	}
}
