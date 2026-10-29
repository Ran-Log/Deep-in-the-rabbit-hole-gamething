using System.Collections.Generic;

/// <summary>
/// Costos de mejora del pico en la estación del campamento. Una mejora por
/// nivel: Básico → Reforzado → Avanzado. Sin GodotSharp (testeable).
/// </summary>
public static class UpgradeTable
{
	public const int DefenseStoneCost = 2;

	public const int TorchCoalCost = 1;

	/// <summary>
	/// Siguiente nivel y su costo (id de ítem → cantidad), o false si ya está
	/// al máximo.
	/// </summary>
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
