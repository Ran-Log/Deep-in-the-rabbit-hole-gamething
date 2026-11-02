using Godot;

/// <summary>
/// Modo construcción (GDD 2.1.8): B abre o cierra, 1/2 eligen qué construir,
/// un fantasma verde o rojo muestra dónde iría y el clic lo coloca.
/// Dónde se puede y cuánto cuesta lo decide cada IStructurePlacer.
/// </summary>
public partial class BuildComponent : Node3D
{
	// Una entrada del menú: nombre, costo (texto) y el grupo del nodo que la coloca.
	public readonly struct BuildOption
	{
		public readonly string Name;
		public readonly string CostText;
		public readonly string PlacerGroup;
		public readonly Vector3 GhostSize;

		public BuildOption(string name, string costText, string placerGroup, Vector3 ghostSize)
		{
			Name = name;
			CostText = costText;
			PlacerGroup = placerGroup;
			GhostSize = ghostSize;
		}
	}

	public static readonly BuildOption[] Options =
	{
		new BuildOption("Barricada", $"{UpgradeTable.DefenseStoneCost} Piedra · solo en el campamento", CampDefenses.Group, new Vector3(2.02f, 2.02f, 2.02f)),
		new BuildOption("Antorcha", $"{UpgradeTable.TorchCoalCost} Carbón · en cualquier suelo", TorchPlacer.Group, new Vector3(0.5f, 2.0f, 0.5f)),
		new BuildOption("Trampa de pinchos", $"{UpgradeTable.FormatCost(UpgradeTable.TrapCost)} · suelo del campamento", TrapPlacer.Group, new Vector3(1.8f, 0.4f, 1.8f)),
	};

	[Signal]
	public delegate void BuildModeChangedEventHandler(bool active);

	[Signal]
	public delegate void SelectionChangedEventHandler(int index);

	// name = BuildOption.Name ("Barricada", "Antorcha"...).
	[Signal]
	public delegate void StructureBuiltEventHandler(string name);

	[ExportCategory("BuildComponent")]
	[ExportGroup("Referencias")]
	[Export] public Camera3D Camera;
	[Export] public CollisionObject3D ExcludedBody;
	[Export] public MiningComponent Mining;

	[ExportGroup("Construcción")]
	[Export] public float Range = 8.0f;

	[ExportGroup("Visual")]
	[Export] public Color ValidColor = new Color(0.3f, 1.0f, 0.4f, 0.35f);
	[Export] public Color InvalidColor = new Color(1.0f, 0.3f, 0.3f, 0.35f);

	private bool active;
	private int selected;
	private MeshInstance3D ghost;
	private BoxMesh ghostMesh;
	private StandardMaterial3D ghostMaterial;

	public bool IsActive => active;

	public int Selected => selected;

	public override void _Ready()
	{
		CreateGhost();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!CrosshairRay.PlayerHasControl())
		{
			return;
		}

		if (@event.IsActionPressed("build"))
		{
			SetActive(!active);
			return;
		}

		if (!active)
		{
			return;
		}

		// build_option_1, build_option_2... una acción por opción del menú.
		for (int i = 0; i < Options.Length; i++)
		{
			if (@event.IsActionPressed($"build_option_{i + 1}"))
			{
				Select(i);
			}
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!active)
		{
			return;
		}

		IStructurePlacer placer = GetTree().GetFirstNodeInGroup(Options[selected].PlacerGroup) as IStructurePlacer;

		if (placer == null || !FindTarget(out Vector3I cell, out bool onTop))
		{
			ghost.Visible = false;
			return;
		}

		bool valid = placer.CanPlace(cell, onTop, out _);

		// Apoyado en el fondo de la celda (la trampa es baja; la barricada la llena entera).
		float cellSize = BlockGrid.Instance.CellSize;
		float lift = (cellSize - Options[selected].GhostSize.Y) * 0.5f;
		ghost.GlobalPosition = BlockGrid.Instance.GridToWorld(cell) - new Vector3(0.0f, lift, 0.0f);
		ghost.Visible = true;
		ghostMaterial.AlbedoColor = valid ? ValidColor : InvalidColor;

		if (CrosshairRay.PlayerHasControl() && Input.IsActionJustPressed("mine") && placer.TryPlace(cell, onTop))
		{
			EmitSignal(SignalName.StructureBuilt, Options[selected].Name);
		}
	}

	private void SetActive(bool value)
	{
		active = value;
		ghost.Visible = false;

		if (Mining != null)
		{
			Mining.Enabled = !active;
		}

		EmitSignal(SignalName.BuildModeChanged, active);
	}

	private void Select(int index)
	{
		selected = Mathf.Clamp(index, 0, Options.Length - 1);
		ghostMesh.Size = Options[selected].GhostSize;

		EmitSignal(SignalName.SelectionChanged, selected);
	}

	// La celda vacía pegada a la cara apuntada. onTop = se apuntó a la cara de
	// arriba de algo, que es donde puede ir una antorcha.
	private bool FindTarget(out Vector3I cell, out bool onTop)
	{
		cell = default;
		onTop = false;

		BlockGrid grid = BlockGrid.Instance;

		if (grid == null || !CrosshairRay.Cast(Camera, Range, 1, ExcludedBody, out CrosshairRay.Hit hit))
		{
			return false;
		}

		cell = grid.WorldToGrid(hit.Position + hit.Normal * grid.CellSize * 0.5f);
		onTop = hit.Normal.Y > 0.7f;
		return true;
	}

	private void CreateGhost()
	{
		ghostMaterial = new StandardMaterial3D
		{
			AlbedoColor = ValidColor,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
		};

		ghostMesh = new BoxMesh { Size = Options[0].GhostSize };

		ghost = new MeshInstance3D
		{
			Mesh = ghostMesh,
			MaterialOverride = ghostMaterial,
			TopLevel = true,
			Visible = false,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		};

		AddChild(ghost);
	}
}
