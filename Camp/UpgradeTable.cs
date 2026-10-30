using System.Collections.Generic;

/// <summary>
/// Costos de mejora del pico (Básico → Reforzado → Avanzado) y de construir.
/// </summary>
public static class UpgradeTable
{
	public const int DefenseStoneCost = 2;

	public const int TorchCoalCost = 1;

	// Siguiente nivel y su costo (ítem → cantidad). false si ya está al máximo.
	public static bool TryGetNext(ToolTier current, out ToolTier next, out IReadOnlyDictionary<string, int> cost)
	{
		switch (current)
		{
			case ToolTier.None:
			case ToolTier.Basic:
				next = ToolTier.Reinforced;
				cost = new Dictionary<string, int> { ["Stone"] = 8, ["Coal"] = 4 };
				return true;

			case ToolTier.Reinforced:
				next = ToolTier.Advanced;
				cost = new Dictionary<string, int> { ["Iron"] = 6, ["Coal"] = 4 };
				return true;

			default:
				next = current;
				cost = new Dictionary<string, int>();
				return false;
		}
	}
}
