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

	[ExportCategory("DestructibleBlock")]
	[ExportGroup("Referencias")]
	[Export] public MeshInstance3D MeshInstance;

	[ExportGroup("Bloque")]
	[Export] public MineralType Mineral = MineralType.Stone;
	[Export] public int MaxHealth = 5;
	// Nivel mínimo de pico para poder dañarlo.
	[Export] public ToolTier RequiredTool = ToolTier.Basic;
	// false = indestructible (ej. bedrock del borde).
	[Export] public bool IsDestructible = true;
	// Nombre para el HUD; vacío usa el del mineral (ej. "Barricada" en las defensas).
	[Export] public string DisplayName = "";

	[ExportGroup("Grilla")]
	// Se puede apagar para bloques decorativos fuera de la grilla.
	[Export] public bool AlignToGrid = true;

	private int health;
	private Vector3I cell;
	private bool registered;
	private Material highlightOverlay;

	public Vector3I Cell => cell;

	public int Health => health;

	public string Label => string.IsNullOrEmpty(DisplayName)
		? ItemCatalog.DisplayName(ItemCatalog.IdOf(Mineral))
		: DisplayName;

	public override void _Ready()
	{
		health = MaxHealth;

		ApplyMineralMaterial();

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

	// Devuelve false si el pico no alcanza, para mostrar que el golpe rebotó.
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

	private void ApplyMineralMaterial()
	{
		if (MeshInstance != null)
		{
			MeshInstance.MaterialOverride = MineralTextureFactory.GetBlockMaterial(Mineral);
		}
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

		// El mesh es un cubo de 1x1x1, así que la escala es el tamaño de celda.
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

		// _ExitTree lo saca de la grilla.
		QueueFree();
	}
}
