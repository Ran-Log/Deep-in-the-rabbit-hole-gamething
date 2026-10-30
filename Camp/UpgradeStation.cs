using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Estación de mejora (GDD 2.1.4): gasta minerales para subir el pico un nivel.
/// Los costos están en UpgradeTable.
/// </summary>
public partial class UpgradeStation : Interactable
{
	public override string GetPrompt(Player player)
	{
		ToolTier current = player.Equipment?.Tool ?? ToolTier.Basic;

		if (!UpgradeTable.TryGetNext(current, out ToolTier next, out var cost))
		{
			return $"{ItemCatalog.ToolName(current)} (nivel máximo)";
		}

		bool affordable = WorldState.Instance?.HasItems(cost) == true;
		string costText = FormatCost(cost);

		return affordable
			? $"Mejorar a {ItemCatalog.ToolName(next)} ({costText})"
			: $"Mejorar a {ItemCatalog.ToolName(next)}: faltan materiales ({costText})";
	}

	public override void Interact(Player player)
	{
		if (player.Equipment == null || WorldState.Instance == null)
		{
			return;
		}

		if (!UpgradeTable.TryGetNext(player.Equipment.Tool, out ToolTier next, out var cost))
		{
			GameEvents.PostMessage("Tu pico ya está al máximo");
			return;
		}

		if (!WorldState.Instance.TrySpend(cost))
		{
			GameEvents.PostMessage($"Te faltan materiales: {FormatMissing(cost)}");
			return;
		}

		player.Equipment.UpgradeTo(next);
		GameEvents.PostMessage($"¡Mejoraste tu pico a {ItemCatalog.ToolName(next)}!");
	}

	private static string FormatCost(IReadOnlyDictionary<string, int> cost)
	{
		return string.Join(", ", cost.Select(entry => $"{entry.Value} {ItemCatalog.DisplayName(entry.Key)}"));
	}

	private static string FormatMissing(IReadOnlyDictionary<string, int> cost)
	{
		var missing = cost
			.Select(entry => (entry.Key, Missing: entry.Value - (WorldState.Instance?.GetItemCount(entry.Key) ?? 0)))
			.Where(entry => entry.Missing > 0)
			.Select(entry => $"{entry.Missing} {ItemCatalog.DisplayName(entry.Key)}");

		return string.Join(", ", missing);
	}
}
