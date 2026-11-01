using System.Collections.Generic;
using Godot;

/// <summary>
/// Un nivel de las antorchas (GDD 2.1.4): hasta dónde alumbran las antorchas y la lámpara del casco.
/// Hay un .tres por nivel en Data/Lights. El nivel 1 es el inicial (sin costo).
/// </summary>
[GlobalClass]
public partial class LightDefinition : Resource
{
	[ExportCategory("LightDefinition")]
	[ExportGroup("Nivel")]
	[Export(PropertyHint.Range, "1,10,1")] public int Level = 1;
	[Export] public string DisplayName = "Antorchas comunes";

	[ExportGroup("Alcance (metros)")]
	[Export] public float TorchRange = 11.0f;
	[Export] public float LampRange = 5.0f;

	[ExportGroup("Costo")]
	// Lo que cuesta llegar a este nivel desde el anterior.
	[Export] public Godot.Collections.Array<MineralCost> Cost = new();

	public Dictionary<string, int> CostByItemId() => MineralCost.ToItemCost(Cost);
}
