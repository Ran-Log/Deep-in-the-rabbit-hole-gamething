using System.Collections.Generic;
using Godot;

/// <summary>
/// Una mejora del pico en la estación del campamento: de qué nivel a cuál, y cuánto cuesta.
/// Hay un .tres por mejora en Data/Upgrades.
/// </summary>
[GlobalClass]
public partial class UpgradeRecipe : Resource
{
	[ExportCategory("UpgradeRecipe")]
	[ExportGroup("Mejora")]
	[Export] public ToolTier From = ToolTier.Basic;
	[Export] public ToolTier To = ToolTier.Reinforced;

	[ExportGroup("Costo")]
	[Export] public Godot.Collections.Array<MineralCost> Cost = new();

	public Dictionary<string, int> CostByItemId() => MineralCost.ToItemCost(Cost);
}
