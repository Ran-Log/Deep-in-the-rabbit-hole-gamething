using Godot;

/// <summary>
/// Fog of war del mapa (GDD: el mapa "va desbloqueando zonas no antes vistas"). Cada poco tiempo
/// mira las celdas cerca del jugador y anota en WorldState si son piso o pared. Así quedan
/// dibujados los túneles que cavó. El mapa (WorldMap) escucha CellsRevealed para pintarlas.
/// </summary>
public partial class MapRevealer : Node
{
	// Cada celda va como (x, z, tipo de MapCell).
	[Signal]
	public delegate void CellsRevealedEventHandler(Godot.Collections.Array<Vector3I> cells);

	[ExportCategory("MapRevealer")]
	[ExportGroup("Referencias")]
	[Export] public Node3D Body;

	[ExportGroup("Niebla")]
	// Radio en celdas (una celda = 2 m).
	[Export(PropertyHint.Range, "1,20,1")] public int RevealRadius = 5;
	[Export] public float RevealInterval = 0.2f;

	private float timer;

	public override void _Process(double delta)
	{
		timer += (float)delta;

		if (timer < RevealInterval || Body == null)
		{
			return;
		}

		timer = 0.0f;
		RevealAround(Body.GlobalPosition);
	}

	// Devuelve cuántas celdas cambiaron. Público para los tests.
	public int RevealAround(Vector3 worldPosition)
	{
		BlockGrid grid = BlockGrid.Instance;

		if (grid == null || WorldState.Instance == null)
		{
			return 0;
		}

		Vector3I center = grid.WorldToGrid(worldPosition);
		var changed = new Godot.Collections.Array<Vector3I>();

		foreach (var (x, z) in MapMemory.CellsInRadius(center.X, center.Z, RevealRadius))
		{
			MapCell kind = Classify(grid, x, z);

			if (WorldState.Instance.RevealMapCell(new Vector2I(x, z), kind))
			{
				changed.Add(new Vector3I(x, z, (int)kind));
			}
		}

		if (changed.Count > 0)
		{
			EmitSignal(SignalName.CellsRevealed, changed);
		}

		return changed.Count;
	}

	// Pared si hay bloque a la altura del jugador (capas 0 o 1); si no, se puede pasar.
	private static MapCell Classify(BlockGrid grid, int x, int z)
	{
		bool blocked = grid.IsOccupied(new Vector3I(x, 0, z)) || grid.IsOccupied(new Vector3I(x, 1, z));
		return blocked ? MapCell.Wall : MapCell.Floor;
	}
}
