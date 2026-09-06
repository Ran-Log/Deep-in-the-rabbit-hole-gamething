using Godot;

/// <summary>
/// Un bloque minable. Se alinea solo a la grilla del <see cref="BlockGrid"/> y
/// se da de baja de ella cuando sale del árbol.
/// </summary>
public partial class DestructibleBlock : StaticBody3D
{
	[Signal]
	public delegate void DamagedEventHandler(int currentHealth, int maxHealth);

	[Signal]
	public delegate void DestroyedEventHandler(int mineral);

	[Export]
	public MineralType Mineral = MineralType.Stone;

	[Export]
	public int MaxHealth = 5;

	/// <summary>Nivel mínimo de herramienta para poder dañar este bloque.</summary>
	[Export]
	public ToolTier RequiredTool = ToolTier.Basic;

	/// <summary>Si es false, el bloque es indestructible (ej. bedrock del borde).</summary>
	[Export]
	public bool IsDestructible = true;

	[Export]
	public MeshInstance3D MeshInstance;

	/// <summary>
	/// Ajusta posición y escala a la grilla en _Ready. Se puede apagar para
	/// bloques decorativos puestos a mano fuera de la grilla.
	/// </summary>
	[Export]
	public bool AlignToGrid = true;

	private int health;
	private Vector3I cell;
	private bool registered;
	private Material highlightOverlay;

	public Vector3I Cell => cell;

	public int Health => health;

	public override void _Ready()
	{
		health = MaxHealth;

		if (AlignToGrid)
		{
			SnapToGrid();
		}

		Register();
	}

	public override void _ExitTree()
	{
		if (registered && BlockGrid.Instance != null)
		{
			BlockGrid.Instance.RemoveBlock(cell, this);
			registered = false;
		}
	}

	/// <summary>
	/// Aplica daño si la herramienta alcanza. Devuelve true si el golpe contó,
	/// para que quien mina pueda dar feedback distinto cuando rebota.
	/// </summary>
	public bool TakeDamage(int amount, ToolTier tool)
	{
		if (!IsDestructible || tool < RequiredTool)
		{
			return false;
		}

		health = Mathf.Max(0, health - amount);

		EmitSignal(SignalName.Damaged, health, MaxHealth);

		if (health <= 0)
		{
			Destroy();
		}

		return true;
	}

	public bool CanBeMinedWith(ToolTier tool)
	{
		return IsDestructible && tool >= RequiredTool;
	}

	/// <summary>Resalta el bloque apuntado, sin duplicar su material base.</summary>
	public void SetHighlighted(bool highlighted)
	{
		if (MeshInstance == null)
		{
			return;
		}

		MeshInstance.MaterialOverlay = highlighted ? GetHighlightOverlay() : null;
	}

	private void SnapToGrid()
	{
		BlockGrid grid = BlockGrid.Instance;

		if (grid == null)
		{
			GD.PushWarning($"{Name}: no hay BlockGrid; el bloque queda fuera de la grilla.");
			return;
		}

		cell = grid.WorldToGrid(GlobalPosition);

		// El bloque se modela como un cubo de 1x1x1, así que la escala ES el
		// tamaño de celda. Un solo lugar define el tamaño del mundo.
		GlobalTransform = new Transform3D(
			Basis.Identity.Scaled(Vector3.One * grid.CellSize),
			grid.GridToWorld(cell)
		);
	}

	private void Register()
	{
		BlockGrid grid = BlockGrid.Instance;

		if (grid == null || !AlignToGrid)
		{
			return;
		}

		registered = grid.RegisterBlock(cell, this);

		if (!registered)
		{
			GD.PushWarning($"{Name}: la celda {cell} ya estaba ocupada; se descarta el duplicado.");
			QueueFree();
		}
	}

	private Material GetHighlightOverlay()
	{
		if (highlightOverlay == null)
		{
			highlightOverlay = new StandardMaterial3D
			{
				AlbedoColor = new Color(1.0f, 1.0f, 1.0f, 0.25f),
				Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
				ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded
			};
		}

		return highlightOverlay;
	}

	private void Destroy()
	{
		EmitSignal(SignalName.Destroyed, (int)Mineral);

		// La baja de la grilla la hace _ExitTree, así no depende de por dónde
		// se libere el nodo.
		QueueFree();
	}
}
