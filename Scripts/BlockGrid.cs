using System.Collections.Generic;
using Godot;

/// <summary>
/// Autoload. Única fuente de verdad de dónde vive cada bloque del mundo.
///
/// Convención de coordenadas: la celda (x, y, z) tiene su CENTRO en
/// (x, y, z) * <see cref="CellSize"/>. O sea, un bloque en la celda (0,0,0)
/// está centrado en el origen del mundo, no apoyado en él.
/// </summary>
public partial class BlockGrid : Node
{
	public static BlockGrid Instance { get; private set; }

	[Export]
	public float CellSize = 2.0f;

	private readonly Dictionary<Vector3I, DestructibleBlock> blocks = new();

	/// <summary>Cuántos bloques hay registrados. Se usa para medir rendimiento.</summary>
	public int BlockCount => blocks.Count;

	public override void _EnterTree()
	{
		// _EnterTree y no _Ready: los autoloads entran al árbol antes que la
		// escena principal, así que Instance ya está lista cuando los bloques
		// corren su propio _Ready.
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

	/// <summary>
	/// Registra un bloque en una celda. Devuelve false si la celda ya estaba
	/// ocupada — el que llama decide qué hacer con el duplicado.
	/// </summary>
	public bool RegisterBlock(Vector3I cell, DestructibleBlock block)
	{
		if (blocks.ContainsKey(cell))
		{
			return false;
		}

		blocks[cell] = block;

		return true;
	}

	/// <summary>
	/// Libera una celda. Solo la libera si el bloque que la ocupa es
	/// exactamente <paramref name="block"/>, para que un duplicado que nunca
	/// llegó a registrarse no borre la entrada del bloque legítimo.
	/// </summary>
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

	/// <summary>
	/// True si algún cuerpo físico (jugador, zombie, cama, antorcha...) ocupa
	/// la celda. Se consulta con una caja un poco más chica que la celda para
	/// que no cuente el piso ni los bloques vecinos, que solo la tocan por el borde.
	/// Lo usan quienes construyen (CampDefenses, TorchPlacer) antes de colocar algo.
	/// </summary>
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
