using Godot;

/// <summary>
/// Datos de un mineral, editables en el inspector (Resource, clase 4).
/// Hay un .tres por mineral en Data/Minerals.
/// </summary>
[GlobalClass]
public partial class MineralDefinition : Resource
{
	[ExportCategory("MineralDefinition")]
	[ExportGroup("Juego")]
	[Export] public MineralType Mineral = MineralType.Stone;
	// Pico mínimo para poder romperlo.
	[Export] public ToolTier RequiredTool = ToolTier.Basic;

	[ExportGroup("Aspecto")]
	[Export] public Color BaseColor = new Color(0.5f, 0.5f, 0.5f);
	[Export] public Color VeinColor = new Color(0.6f, 0.6f, 0.6f);
	// 0 = sin vetas, 1 = cubierto de vetas.
	[Export(PropertyHint.Range, "0,1,0.01")] public float VeinDensity = 0.0f;
	[Export(PropertyHint.Range, "0,1,0.01")] public float Metallic = 0.0f;
	[Export(PropertyHint.Range, "0,1,0.01")] public float Roughness = 1.0f;
}
