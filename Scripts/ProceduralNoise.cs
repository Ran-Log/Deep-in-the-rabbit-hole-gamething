using System;

/// <summary>
/// Ruido procedural determinístico y sin GodotSharp: "value noise" con
/// interpolación suave, más una suma fractal (fBm) sobre él. Reutiliza
/// DeterministicHash para que la misma semilla + coordenada siempre den el
/// mismo resultado — igual que la generación de salas — así esta parte se
/// puede probar de verdad en Tests/GameLogic.Tests, algo que no se puede
/// hacer con Godot.Image (ver MineralTextureFactory, que sí usa GodotSharp
/// y consume esta clase para pintar las texturas de cada mineral).
/// </summary>
public static class ProceduralNoise
{
	/// <summary>Hash determinístico a un double en [0,1).</summary>
	public static double Hash01(ulong seed, int x, int y)
	{
		ulong h = DeterministicHash.Combine(seed, x, y);

		// Los 53 bits altos entran justos en la mantisa de un double, así el
		// resultado cubre [0,1) con la resolución que da un double normal.
		return (h >> 11) * (1.0 / (1UL << 53));
	}

	/// <summary>Curva de suavizado tipo Perlin (6t^5 - 15t^4 + 10t^3): evita el quiebre visible que deja una interpolación lineal simple.</summary>
	private static double Fade(double t)
	{
		return t * t * t * (t * (t * 6.0 - 15.0) + 10.0);
	}

	private static double Lerp(double a, double b, double t)
	{
		return a + (b - a) * t;
	}

	/// <summary>
	/// "Value noise" 2D: interpola suavemente entre valores hasheados en las
	/// cuatro esquinas de la celda de grilla que contiene a (x, y). Resultado
	/// en [0,1).
	/// </summary>
	public static double ValueNoise(ulong seed, double x, double y)
	{
		int x0 = (int)Math.Floor(x);
		int y0 = (int)Math.Floor(y);
		int x1 = x0 + 1;
		int y1 = y0 + 1;

		double tx = Fade(x - x0);
		double ty = Fade(y - y0);

		double v00 = Hash01(seed, x0, y0);
		double v10 = Hash01(seed, x1, y0);
		double v01 = Hash01(seed, x0, y1);
		double v11 = Hash01(seed, x1, y1);

		double top = Lerp(v00, v10, tx);
		double bottom = Lerp(v01, v11, tx);

		return Lerp(top, bottom, ty);
	}

	/// <summary>
	/// Suma fractal (fBm) de varias octavas de <see cref="ValueNoise"/>,
	/// normalizada de vuelta a [0,1). Cada octava dobla la frecuencia
	/// (lacunarity=2) y pesa la mitad que la anterior (persistence=0.5) —
	/// los valores clásicos, de sobra para el detalle de una textura de
	/// bloque. Cada octava usa su propia sub-semilla para no repetir el
	/// mismo patrón de ValueNoise a distintas frecuencias.
	/// </summary>
	public static double Fbm(ulong seed, double x, double y, int octaves)
	{
		if (octaves < 1)
		{
			octaves = 1;
		}

		double amplitude = 1.0;
		double frequency = 1.0;
		double sum = 0.0;
		double maxSum = 0.0;

		for (int i = 0; i < octaves; i++)
		{
			ulong octaveSeed = seed + (ulong)i * 0x9E3779B97F4A7C15UL;

			sum += ValueNoise(octaveSeed, x * frequency, y * frequency) * amplitude;
			maxSum += amplitude;

			amplitude *= 0.5;
			frequency *= 2.0;
		}

		return sum / maxSum;
	}
}
