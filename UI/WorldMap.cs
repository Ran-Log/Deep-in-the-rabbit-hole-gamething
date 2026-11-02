using Godot;

/// <summary>
/// El mapa del jugador (GDD: "se va actualizando y desbloqueando zonas no antes vistas"): un minimapa
/// siempre visible y el mapa grande con M. Guarda lo visto en una Image (un píxel por celda) que pinta
/// cuando MapRevealer avisa; los dos MapPanel dibujan esa misma textura.
/// </summary>
public partial class WorldMap : CanvasLayer
{
	[ExportCategory("WorldMap")]
	[ExportGroup("Referencias")]
	[Export] public Player Player;
	[Export] public MapRevealer Revealer;

	[ExportGroup("Nodos de UI")]
	[Export] public MapPanel Minimap;
	[Export] public MapPanel FullMapPanel;
	[Export] public Control FullMap;
	[Export] public Compass Compass;

	[ExportGroup("Mapa")]
	// Lado de la imagen en celdas; el campamento queda en el medio.
	[Export(PropertyHint.Range, "64,1024,32")] public int ImageSize = 512;
	[Export] public Color FloorColor = new Color(0.55f, 0.48f, 0.38f);
	[Export] public Color WallColor = new Color(0.22f, 0.19f, 0.16f);

	private Image image;
	private ImageTexture texture;

	public bool IsFullMapOpen => FullMap != null && FullMap.Visible;

	private int HalfSize => ImageSize / 2;

	private float CellSize => BlockGrid.Instance?.CellSize ?? 2.0f;

	public override void _Ready()
	{
		image = Image.CreateEmpty(ImageSize, ImageSize, false, Image.Format.Rgba8);

		// Lo que ya se había visto (partida guardada).
		if (WorldState.Instance != null)
		{
			foreach (var (cell, kind) in WorldState.Instance.GetMapCells())
			{
				Paint(cell.X, cell.Y, kind);
			}
		}

		texture = ImageTexture.CreateFromImage(image);

		foreach (MapPanel panel in new[] { Minimap, FullMapPanel })
		{
			if (panel != null)
			{
				panel.MapTexture = texture;
				panel.ImageHalfSize = HalfSize;
			}
		}

		if (Revealer != null)
		{
			Revealer.CellsRevealed += OnCellsRevealed;
		}

		if (FullMap != null)
		{
			FullMap.Visible = false;
		}
	}

	public override void _ExitTree()
	{
		if (Revealer != null)
		{
			Revealer.CellsRevealed -= OnCellsRevealed;
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("toggle_map"))
		{
			ToggleFullMap();
		}
	}

	public override void _Process(double delta)
	{
		if (Player == null)
		{
			return;
		}

		// Se calcula una vez y lo usan los dos mapas y la brújula.
		Vector2 playerCell = ToCell(Player.GlobalPosition);
		Vector2 facing = Vector2.Up;

		if (Player.CameraRig != null)
		{
			Vector3 forward = Player.CameraRig.GetForwardDirection();
			facing = new Vector2(forward.X, forward.Z);
		}

		Vector2? campCell = GetTree().GetFirstNodeInGroup(Enemy.CampCoreGroup) is Node3D camp ? ToCell(camp.GlobalPosition) : null;
		Vector2? exitCell = KnownExitCell();

		UpdatePanel(Minimap, playerCell, facing, campCell, exitCell);
		UpdatePanel(FullMapPanel, playerCell, facing, campCell, exitCell);
		UpdateCompass(playerCell, facing, campCell, exitCell);
	}

	// No pausa ni suelta el mouse: como la mochila, mirar el mapa te deja expuesto.
	public void ToggleFullMap()
	{
		if (FullMap == null)
		{
			return;
		}

		FullMap.Visible = !FullMap.Visible;

		if (Minimap != null)
		{
			Minimap.Visible = !FullMap.Visible;
		}
	}

	// Para los tests: el color con que quedó pintada una celda.
	public Color PixelAt(Vector2I cell)
	{
		return image.GetPixel(cell.X + HalfSize, cell.Y + HalfSize);
	}

	private void OnCellsRevealed(Godot.Collections.Array<Vector3I> cells)
	{
		foreach (Vector3I cell in cells)
		{
			Paint(cell.X, cell.Y, (MapCell)cell.Z);
		}

		texture?.Update(image);
	}

	private void Paint(int x, int z, MapCell kind)
	{
		int px = x + HalfSize;
		int pz = z + HalfSize;

		// Muy lejos del campamento queda fuera de la imagen (sigue guardado en WorldState).
		if (px < 0 || pz < 0 || px >= ImageSize || pz >= ImageSize)
		{
			return;
		}

		image.SetPixel(px, pz, kind == MapCell.Wall ? WallColor : FloorColor);
	}

	private static void UpdatePanel(MapPanel panel, Vector2 playerCell, Vector2 facing, Vector2? campCell, Vector2? exitCell)
	{
		if (panel == null || !panel.IsVisibleInTree())
		{
			return;
		}

		panel.PlayerCell = playerCell;
		panel.PlayerFacing = facing;
		panel.CampCell = campCell;
		panel.ExitCell = exitCell;
		panel.QueueRedraw();
	}

	// Mismos colores que los marcadores del mapa, para reconocerlos.
	private void UpdateCompass(Vector2 playerCell, Vector2 facing, Vector2? campCell, Vector2? exitCell)
	{
		if (Compass == null || !Compass.IsVisibleInTree())
		{
			return;
		}

		Compass.Heading = Compass.BearingOf(facing.X, facing.Y);
		Compass.Markers.Clear();

		if (campCell is Vector2 camp)
		{
			Vector2 toCamp = camp - playerCell;
			Compass.Markers.Add((Compass.BearingOf(toCamp.X, toCamp.Y), Minimap?.CampColor ?? Colors.Orange));
		}

		if (exitCell is Vector2 exit)
		{
			Vector2 toExit = exit - playerCell;
			Compass.Markers.Add((Compass.BearingOf(toExit.X, toExit.Y), Minimap?.ExitColor ?? Colors.Green));
		}

		Compass.QueueRedraw();
	}

	// La salida aparece en el mapa solo después de entrar a su sala.
	private Vector2? KnownExitCell()
	{
		LevelDefinition level = GameData.CurrentLevel;

		if (level == null || WorldState.Instance == null)
		{
			return null;
		}

		Vector2I room = level.ExitCoord();

		if (room == Vector2I.Zero || !WorldState.Instance.IsRoomVisited(room))
		{
			return null;
		}

		// Igual que Room: la salida va en el centro interior de la sala (corrido una celda).
		float spacing = RoomGenerator.RoomWidth * CellSize;
		Vector3 exitWorld = new Vector3(room.X * spacing - CellSize * 0.5f, 0.0f, room.Y * spacing - CellSize * 0.5f);
		return ToCell(exitWorld);
	}

	private Vector2 ToCell(Vector3 world)
	{
		return new Vector2(world.X / CellSize, world.Z / CellSize);
	}
}
