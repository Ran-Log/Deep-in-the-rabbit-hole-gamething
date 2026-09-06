using Godot;

/// <summary>
/// Una habitación: un piso, y un perímetro de bloques generado automáticamente
/// alrededor según <see cref="Width"/> y <see cref="Depth"/>.
///
/// El origen de la Room coincide con el CENTRO de la primera capa de bloques
/// (celda y = 0), así que la superficie del piso queda media celda más abajo.
/// Conviene ubicar las Rooms en múltiplos de BlockGrid.CellSize para que las
/// celdas de habitaciones vecinas calcen entre sí.
/// </summary>
public partial class Room : Node3D
{
	[Export]
	public PackedScene BlockScene;

	/// <summary>Ancho total en celdas, incluyendo las dos paredes.</summary>
	[Export(PropertyHint.Range, "3,64,1")]
	public int Width = 10;

	/// <summary>Profundidad total en celdas, incluyendo las dos paredes.</summary>
	[Export(PropertyHint.Range, "3,64,1")]
	public int Depth = 10;

	/// <summary>Altura del muro perimetral, en celdas.</summary>
	[Export(PropertyHint.Range, "1,16,1")]
	public int WallHeight = 3;

	[Export]
	public MineralType WallMineral = MineralType.Stone;

	[Export]
	public int WallBlockHealth = 5;

	[Export]
	public StaticBody3D FloorBody;

	[Export]
	public MeshInstance3D FloorMesh;

	[Export]
	public CollisionShape3D FloorCollision;

	[Export]
	public Node3D BlockContainer;

	private Vector3I originCell;
	private int spawnedBlocks;

	/// <summary>Cuántos bloques generó esta habitación. Útil para el paso de medición.</summary>
	public int SpawnedBlocks => spawnedBlocks;

	public override void _Ready()
	{
		if (BlockGrid.Instance == null)
		{
			GD.PushError($"{Name}: no hay BlockGrid autoload; la habitación no se puede generar.");
			return;
		}

		SnapOriginToGrid();
		ResizeFloor();
		GenerateWalls();
	}

	private void SnapOriginToGrid()
	{
		BlockGrid grid = BlockGrid.Instance;

		GlobalPosition = grid.GridToWorld(grid.WorldToGrid(GlobalPosition));
	}

	/// <summary>
	/// Estira el piso para que cubra exactamente la huella de la habitación.
	/// Ocupa la capa de celdas y = -1, o sea justo debajo del primer nivel de muro.
	/// </summary>
	private void ResizeFloor()
	{
		float cell = BlockGrid.Instance.CellSize;
		Vector3 size = new Vector3(Width * cell, cell, Depth * cell);
		Vector3 center = new Vector3(0.0f, -cell, 0.0f);

		if (FloorMesh?.Mesh is BoxMesh boxMesh)
		{
			boxMesh.Size = size;
			FloorMesh.Position = center;
		}

		if (FloorCollision?.Shape is BoxShape3D boxShape)
		{
			boxShape.Size = size;
			FloorCollision.Position = center;
		}
	}

	private void GenerateWalls()
	{
		if (BlockScene == null)
		{
			GD.PushWarning($"{Name}: falta asignar BlockScene; no se generan paredes.");
			return;
		}

		// La habitación tiene que estar alineada a los ejes: las celdas se
		// calculan como offsets locales sin rotar.
		originCell = BlockGrid.Instance.WorldToGrid(GlobalPosition);

		for (int x = 0; x < Width; x++)
		{
			for (int z = 0; z < Depth; z++)
			{
				if (!IsPerimeter(x, z))
				{
					continue;
				}

				for (int y = 0; y < WallHeight; y++)
				{
					SpawnBlock(new Vector3I(x - Width / 2, y, z - Depth / 2));
				}
			}
		}
	}

	private bool IsPerimeter(int x, int z)
	{
		return x == 0 || x == Width - 1 || z == 0 || z == Depth - 1;
	}

	private void SpawnBlock(Vector3I localCell)
	{
		BlockGrid grid = BlockGrid.Instance;

		if (grid.IsOccupied(originCell + localCell))
		{
			// Habitaciones vecinas pueden compartir pared: la primera gana.
			return;
		}

		var block = BlockScene.Instantiate<DestructibleBlock>();

		block.Mineral = WallMineral;
		block.MaxHealth = WallBlockHealth;

		// La posición se fija ANTES de AddChild: al entrar al árbol el bloque
		// corre su _Ready, y ahí ya necesita estar donde corresponde para
		// registrarse en la celda correcta.
		block.Position = grid.GridToWorld(localCell);

		(BlockContainer ?? (Node3D)this).AddChild(block);

		spawnedBlocks++;
	}
}
