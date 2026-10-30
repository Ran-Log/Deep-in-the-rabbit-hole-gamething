using System.Collections.Generic;
using Godot;

/// <summary>
/// Autoload con la grilla de bloques del mundo. La celda (x, y, z) tiene su
/// centro en (x, y, z) * CellSize.
/// </summary>
public partial class BlockGrid : Node
{
	public static BlockGrid Instance { get; private set; }

	[ExportCategory("BlockGrid")]
	[ExportGroup("Grilla")]
	[Export] public float CellSize = 2.0f;

	private readonly Dictionary<Vector3I, DestructibleBlock> blocks = new();

	// Para el overlay de debug (FPS / cantidad de bloques).
	public int BlockCount => blocks.Count;

	public override void _EnterTree()
	{
		// En _EnterTree para que Instance exista antes del _Ready de los bloques.
		Instance = this;
	}

	public override void _ExitTree()
	{
		blocks.Clear();

		if (Instance == this)
		{
			Instance = null;
		}
	}

	public Vector3I WorldToGrid(Vector3 world)
	{
		return new Vector3I(
			Mathf.RoundToInt(world.X / CellSize),
			Mathf.RoundToInt(world.Y / CellSize),
			Mathf.RoundToInt(world.Z / CellSize)
		);
	}

	public Vector3 GridToWorld(Vector3I cell)
	{
		return new Vector3(cell.X, cell.Y, cell.Z) * CellSize;
	}

	// Devuelve false si la celda ya estaba ocupada.
	public bool RegisterBlock(Vector3I cell, DestructibleBlock block)
	{
		if (blocks.ContainsKey(cell))
		{
			return false;
		}

		blocks[cell] = block;

		return true;
	}

	// Solo la libera si la ocupa ese mismo bloque (un duplicado no debe borrar al original).
	public void RemoveBlock(Vector3I cell, DestructibleBlock block)
	{
		if (blocks.TryGetValue(cell, out DestructibleBlock current) && current == block)
		{
			blocks.Remove(cell);
		}
	}

	public bool IsOccupied(Vector3I cell)
	{
		return blocks.ContainsKey(cell);
	}

	// True si algo físico (jugador, zombie, cama...) ocupa la celda. La caja es un
	// poco más chica que la celda para no contar el piso ni los vecinos.
	public bool HasBodyInCell(World3D world, Vector3I cell)
	{
		var query = new PhysicsShapeQueryParameters3D
		{
			Shape = new BoxShape3D { Size = Vector3.One * CellSize * 0.9f },
			Transform = new Transform3D(Basis.Identity, GridToWorld(cell)),
			CollisionMask = 1,
		};

		return world.DirectSpaceState.IntersectShape(query, 1).Count > 0;
	}

	public DestructibleBlock GetBlock(Vector3I cell)
	{
		return blocks.TryGetValue(cell, out DestructibleBlock block) ? block : null;
	}
}
