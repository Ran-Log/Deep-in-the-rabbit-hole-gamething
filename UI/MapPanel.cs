using Godot;

/// <summary>
/// Dibuja el mapa centrado en el jugador: la textura de WorldMap (un píxel por celda) y encima
/// los marcadores (jugador, campamento, salida). Se usa dos veces: el minimapa y el mapa grande.
/// Todo en un solo _Draw; ClipContents recorta lo que queda fuera del panel.
/// </summary>
public partial class MapPanel : Control
{
	[ExportCategory("MapPanel")]
	[ExportGroup("Vista")]
	[Export] public float PixelsPerCell = 3.0f;
	[Export] public Color BackgroundColor = new Color(0.03f, 0.03f, 0.04f, 0.85f);
	[Export] public Color BorderColor = new Color(0.6f, 0.55f, 0.45f, 0.9f);

	[ExportGroup("Marcadores")]
	[Export] public Color PlayerColor = new Color(1.0f, 1.0f, 1.0f);
	[Export] public Color CampColor = new Color(1.0f, 0.6f, 0.2f);
	[Export] public Color ExitColor = new Color(0.4f, 1.0f, 0.5f);

	// Los pone WorldMap cada frame. Las celdas son continuas (posición / tamaño de celda).
	public Texture2D MapTexture { get; set; }

	// La celda (0, 0) está en el píxel (ImageHalfSize, ImageHalfSize) de la textura.
	public int ImageHalfSize { get; set; }

	public Vector2 PlayerCell { get; set; }

	public Vector2 PlayerFacing { get; set; } = Vector2.Up;

	public Vector2? CampCell { get; set; }

	public Vector2? ExitCell { get; set; }

	public override void _Ready()
	{
		ClipContents = true;
		// Píxeles nítidos al agrandar (estilo voxel).
		TextureFilter = TextureFilterEnum.Nearest;
		MouseFilter = MouseFilterEnum.Ignore;
	}

	public override void _Draw()
	{
		DrawRect(new Rect2(Vector2.Zero, Size), BackgroundColor);

		if (MapTexture != null)
		{
			Vector2 topLeft = ToScreen(new Vector2(-ImageHalfSize - 0.5f, -ImageHalfSize - 0.5f));
			DrawTextureRect(MapTexture, new Rect2(topLeft, MapTexture.GetSize() * PixelsPerCell), false);
		}

		if (CampCell is Vector2 camp)
		{
			float half = PixelsPerCell * 1.5f;
			DrawRect(new Rect2(ToScreen(camp) - new Vector2(half, half), new Vector2(half, half) * 2.0f), CampColor);
		}

		if (ExitCell is Vector2 exit)
		{
			DrawDiamond(ToScreen(exit), PixelsPerCell * 2.0f, ExitColor);
		}

		DrawPlayerArrow();
		DrawRect(new Rect2(Vector2.Zero, Size), BorderColor, false, 2.0f);
	}

	// El jugador siempre queda en el centro del panel.
	private Vector2 ToScreen(Vector2 cell)
	{
		return Size * 0.5f + (cell - PlayerCell) * PixelsPerCell;
	}

	private void DrawPlayerArrow()
	{
		Vector2 center = Size * 0.5f;
		Vector2 forward = PlayerFacing.LengthSquared() > 0.001f ? PlayerFacing.Normalized() : Vector2.Up;
		Vector2 side = new Vector2(-forward.Y, forward.X);
		float size = Mathf.Max(5.0f, PixelsPerCell * 2.0f);

		DrawColoredPolygon(new[]
		{
			center + forward * size,
			center - forward * size * 0.6f + side * size * 0.6f,
			center - forward * size * 0.6f - side * size * 0.6f,
		}, PlayerColor);
	}

	private void DrawDiamond(Vector2 center, float radius, Color color)
	{
		DrawColoredPolygon(new[]
		{
			center + new Vector2(0, -radius),
			center + new Vector2(radius, 0),
			center + new Vector2(0, radius),
			center + new Vector2(-radius, 0),
		}, color);
	}
}
