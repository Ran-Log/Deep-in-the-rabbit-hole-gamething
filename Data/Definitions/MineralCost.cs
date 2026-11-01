using System.Collections.Generic;
using Godot;

/// <summary>
/// Una línea de un costo: cuánto de un mineral. Se crea dentro de una mejora (pico, arma, antorchas).
/// </summary>
[GlobalClass]
public partial class MineralCost : Resource
{
	[Export] public MineralType Mineral = MineralType.Stone;
	[Export(PropertyHint.Range, "1,99,1")] public int Amount = 1;

	// El costo en el formato del inventario (id de ítem → cantidad).
	public static Dictionary<string, int> ToItemCost(Godot.Collections.Array<MineralCost> lines)
	{
		var result = new Dictionary<string, int>();

		foreach (MineralCost line in lines)
		{
			string itemId = ItemCatalog.IdOf(line.Mineral);
			result[itemId] = result.GetValueOrDefault(itemId) + line.Amount;
		}

		return result;
	}
}
