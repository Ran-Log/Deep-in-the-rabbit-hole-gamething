using System.Collections.Generic;
using Godot;

/// <summary>
/// Un nivel del campamento (GDD: "el campamento tiene vida, la cual se puede mejorar"). Se mejora
/// en la estación: sube la vida máxima y las barricadas nuevas aguantan más.
/// Hay un .tres por nivel en Data/Camp. El nivel 1 es el inicial (sin costo).
/// </summary>
[GlobalClass]
public partial class CampDefinition : Resource
{
	[ExportCategory("CampDefinition")]
	[ExportGroup("Nivel")]
	[Export(PropertyHint.Range, "1,10,1")] public int Level = 1;
	[Export] public string DisplayName = "Fogata";

	[ExportGroup("Defensa")]
	[Export(PropertyHint.Range, "1,1000,1")] public int MaxHealth = 100;
	// Golpes que aguanta una barricada construida con este nivel.
	[Export(PropertyHint.Range, "1,100,1")] public int BarricadeHealth = 12;

	[ExportGroup("Costo")]
	// Lo que cuesta llegar a este nivel desde el anterior.
	[Export] public Godot.Collections.Array<MineralCost> Cost = new();

	public Dictionary<string, int> CostByItemId() => MineralCost.ToItemCost(Cost);
}
