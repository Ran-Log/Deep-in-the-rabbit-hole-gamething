using Godot;

/// <summary>
/// Junta todos los datos de diseño en un solo Resource (Data/game_data.tres),
/// para verlos y editarlos desde un lugar en el inspector.
/// </summary>
[GlobalClass]
public partial class GameDataCatalog : Resource
{
	[ExportCategory("GameDataCatalog")]
	[ExportGroup("Minerales")]
	[Export] public Godot.Collections.Array<MineralDefinition> Minerals = new();
	// El piso de las salas usa el mismo tipo de datos de aspecto que un mineral.
	[Export] public MineralDefinition Floor;

	[ExportGroup("Mejoras del pico")]
	[Export] public Godot.Collections.Array<UpgradeRecipe> Upgrades = new();
}
