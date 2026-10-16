using Godot;

/// <summary>
/// Menú de construcción (GDD 2.1.8): con la acción "build" (B) se abre/cierra.
/// Mientras está abierto, 1/2 eligen qué construir (ver <see cref="Options"/>),
/// un fantasma muestra dónde iría (verde = se puede, rojo = no) y "mine"
/// (clic izquierdo) lo coloca. Las reglas de cada construcción (dónde,
/// cuánto cuesta) las decide su IStructurePlacer; este componente solo
/// apunta, muestra y delega.
///
/// Al abrirse apaga MiningComponent, para que el clic no mine y construya
/// al mismo tiempo.
/// </summary>
public partial class BuildComponent : Node3D
{
	/// <summary>Una entrada del menú: qué es, cuánto cuesta (texto) y qué grupo tiene el nodo que la coloca.</summary>
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
	};

	[Signal]
	public delegate void BuildModeChangedEventHandler(bool active);

	[Signal]
	public delegate void SelectionChangedEventHandler(int index);

	[Export]
	public Camera3D Camera;

	[Export]
	public CollisionObject3D ExcludedBody;

	[Export]
	public MiningComponent Mining;

	[Export]
	public float Range = 8.0f;

	[Export]
	public Color ValidColor = new Color(0.3f, 1.0f, 0.4f, 0.35f);

	[Export]
	public Color InvalidColor = new Color(1.0f, 0.3f, 0.3f, 0.35f);

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
		}
		else if (active && @event.IsActionPressed("build_option_1"))
		{
			Select(0);
		}
		else if (active && @event.IsActionPressed("build_option_2"))
		{
			Select(1);
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

		ghost.GlobalPosition = BlockGrid.Instance.GridToWorld(cell);
		ghost.Visible = true;
		ghostMaterial.AlbedoColor = valid ? ValidColor : InvalidColor;

		if (CrosshairRay.PlayerHasControl() && Input.IsActionJustPressed("mine"))
		{
			placer.TryPlace(cell, onTop);
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

	/// <summary>
	/// Celda vacía pegada a la cara apuntada (como en cualquier juego de
	/// voxels). <paramref name="onTop"/> = se apuntó a la cara de arriba de
	/// algo (piso o bloque), que es donde puede ir una antorcha.
	/// </summary>
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
