using System.Collections.Generic;
using Godot;

/// <summary>
/// Brújula arriba de la pantalla: una tira con los puntos cardinales que se corre al girar la cámara,
/// y marcas hacia el campamento y la salida. El norte es el mismo que "arriba" en el mapa (-Z del mundo).
/// La alimenta WorldMap, que ya sabe dónde está cada cosa.
/// </summary>
public partial class Compass : Control
{
	private static readonly string[] Labels = { "N", "NE", "E", "SE", "S", "SO", "O", "NO" };

	[ExportCategory("Compass")]
	[ExportGroup("Vista")]
	// Grados que entran en la tira (de borde a borde).
	[Export(PropertyHint.Range, "60,360,1")] public float FieldOfView = 180.0f;
	[Export] public Color BackgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.45f);
	[Export] public Color TickColor = new Color(1.0f, 1.0f, 1.0f, 0.7f);
	[Export] public Color NorthColor = new Color(1.0f, 0.4f, 0.35f);
	[Export] public Color CenterColor = new Color(1.0f, 0.9f, 0.5f);

	// Hacia dónde mira la cámara, en grados: 0 = norte, 90 = este.
	public float Heading { get; set; }

	// Marcas extra: (rumbo en grados, color). Las pone WorldMap cada frame.
	public List<(float Bearing, Color Color)> Markers { get; } = new();

	public override void _Ready()
	{
		ClipContents = true;
		MouseFilter = MouseFilterEnum.Ignore;
	}

	// Rumbo (0 = norte, 90 = este) de una dirección en el plano del mundo.
	public static float BearingOf(float worldX, float worldZ)
	{
		return Mathf.PosMod(Mathf.RadToDeg(Mathf.Atan2(worldX, -worldZ)), 360.0f);
	}

	// Cuánto hay que girar desde el rumbo actual (-180 a 180): negativo = a la izquierda.
	public static float DeltaFrom(float heading, float bearing)
	{
		return Mathf.PosMod(bearing - heading + 180.0f, 360.0f) - 180.0f;
	}

	public override void _Draw()
	{
		DrawRect(new Rect2(Vector2.Zero, Size), BackgroundColor);

		float pixelsPerDegree = Size.X / FieldOfView;
		Font font = GetThemeDefaultFont();
		int fontSize = GetThemeDefaultFontSize();

		// Una marca cada 15°; las de 45° llevan letra.
		for (int degrees = 0; degrees < 360; degrees += 15)
		{
			float delta = DeltaFrom(Heading, degrees);

			if (Mathf.Abs(delta) > FieldOfView * 0.5f)
			{
				continue;
			}

			float x = Size.X * 0.5f + delta * pixelsPerDegree;

			if (degrees % 45 == 0)
			{
				string label = Labels[degrees / 45];
				Color color = degrees == 0 ? NorthColor : TickColor;
				Vector2 textSize = font.GetStringSize(label, HorizontalAlignment.Left, -1, fontSize);
				DrawString(font, new Vector2(x - textSize.X * 0.5f, Size.Y * 0.5f + textSize.Y * 0.3f), label, HorizontalAlignment.Left, -1, fontSize, color);
			}
			else
			{
				DrawLine(new Vector2(x, Size.Y * 0.65f), new Vector2(x, Size.Y), TickColor, 1.0f);
			}
		}

		DrawMarkers(pixelsPerDegree);

		// El centro: hacia donde caminas.
		DrawLine(new Vector2(Size.X * 0.5f, 0.0f), new Vector2(Size.X * 0.5f, Size.Y * 0.25f), CenterColor, 2.0f);
	}

	// Fuera de la tira quedan pegadas al borde, para saber hacia qué lado girar.
	private void DrawMarkers(float pixelsPerDegree)
	{
		float halfView = FieldOfView * 0.5f;

		foreach (var (bearing, color) in Markers)
		{
			float delta = Mathf.Clamp(DeltaFrom(Heading, bearing), -halfView, halfView);
			float x = Mathf.Clamp(Size.X * 0.5f + delta * pixelsPerDegree, 4.0f, Size.X - 4.0f);

			DrawColoredPolygon(new[]
			{
				new Vector2(x, Size.Y - 1.0f),
				new Vector2(x - 4.0f, Size.Y - 8.0f),
				new Vector2(x + 4.0f, Size.Y - 8.0f),
			}, color);
		}
	}
}
