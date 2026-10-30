using System.Collections.Generic;

/// <summary>
/// Costos de mejora del pico (salen de Data/Upgrades) y de construir.
/// </summary>
public static class UpgradeTable
{
	public const int DefenseStoneCost = 2;

	public const int TorchCoalCost = 1;

	// Siguiente nivel y su costo (ítem → cantidad). false si ya está al máximo.
	public static bool TryGetNext(ToolTier current, out ToolTier next, out IReadOnlyDictionary<string, int> cost)
	{
		// Sin pico se mejora igual que desde el básico.
		UpgradeRecipe recipe = GameData.UpgradeFrom(current == ToolTier.None ? ToolTier.Basic : current);

		if (recipe == null)
		{
			next = current;
			cost = new Dictionary<string, int>();
			return false;
		}

		next = recipe.To;
		cost = recipe.CostByItemId();
		return true;
	}
}
