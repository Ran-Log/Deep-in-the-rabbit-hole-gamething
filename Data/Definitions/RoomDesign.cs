using Godot;

/// <summary>
/// Una sala hecha a mano de un nivel (beat chart del GDD). Se crea dentro de un LevelDefinition.
/// </summary>
[GlobalClass]
public partial class RoomDesign : Resource
{
	[ExportCategory("RoomDesign")]
	[ExportGroup("Ubicación")]
	// Coordenada en la grilla de salas. (0,0) es el campamento; -Y es el norte.
	[Export] public Vector2I Coord = Vector2I.Zero;

	[ExportGroup("Contenido")]
	[Export] public MineralType Mineral = MineralType.Stone;
	[Export] public bool HasGas = false;
	[Export] public ChestLoot Loot = ChestLoot.None;
	// Solo si Loot es Note.
	[Export(PropertyHint.MultilineText)] public string NoteText = "";
	// La grieta que baja al siguiente nivel (o la salida final).
	[Export] public bool HasExit = false;
	// Un nido de hormigas gigantes (GDD: "solo en habitaciones especiales").
	[Export] public bool HasNest = false;
}
