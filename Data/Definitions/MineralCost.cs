using Godot;

/// <summary>
/// Una línea de un costo: cuánto de un mineral. Se crea dentro de una UpgradeRecipe.
/// </summary>
[GlobalClass]
public partial class MineralCost : Resource
{
	[Export] public MineralType Mineral = MineralType.Stone;
	[Export(PropertyHint.Range, "1,99,1")] public int Amount = 1;
}
