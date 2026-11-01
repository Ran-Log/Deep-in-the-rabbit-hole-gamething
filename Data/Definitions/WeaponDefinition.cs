using System.Collections.Generic;
using Godot;

/// <summary>
/// Un arma que se forja en la estación de mejora (GDD 2.1.4): cuánto daño hace y cuánto cuesta.
/// Hay un .tres por arma en Data/Weapons. Se forjan en orden (hierro, después oro).
/// </summary>
[GlobalClass]
public partial class WeaponDefinition : Resource
{
	[ExportCategory("WeaponDefinition")]
	[ExportGroup("Arma")]
	[Export] public WeaponTier Tier = WeaponTier.Iron;
	[Export] public string DisplayName = "Espada";
	// Daño por golpe a enemigos (antes de la armadura del enemigo).
	[Export(PropertyHint.Range, "1,50,1")] public int Damage = 4;

	[ExportGroup("Costo")]
	[Export] public Godot.Collections.Array<MineralCost> Cost = new();

	public Dictionary<string, int> CostByItemId() => MineralCost.ToItemCost(Cost);
}
