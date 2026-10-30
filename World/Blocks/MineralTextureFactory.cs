using Godot;
using System.Collections.Generic;

/// <summary>
/// Genera por código la textura y el material de cada mineral y del piso, usando el
/// ruido FastNoiseLite de Godot. Guarda uno por mineral (Flyweight): todos los
/// bloques de piedra comparten el mismo.
/// </summary>
public static class MineralTextureFactory
{
	private const int TextureSize = 64;

	// Semilla fija (no la del mundo), para que cada mineral se vea igual en todas las partidas.
	private const int TextureSeed = 9182;
	private const int FloorSeed = TextureSeed - 100;

	// Frecuencia del ruido: más alta = manchas más chicas.
	private const float BaseFrequency = 0.3f;
	private const float VeinFrequency = 0.9f;

	private static readonly Dictionary<MineralType, StandardMaterial3D> blockMaterialCache = new();
	private static StandardMaterial3D floorMaterialCache;

	public static StandardMaterial3D GetBlockMaterial(MineralType mineral)
	{
		if (blockMaterialCache.TryGetValue(mineral, out StandardMaterial3D cached))
		{
			return cached;
		}

		MineralDefinition look = GameData.Mineral(mineral);

		// Semilla distinta por mineral, para que no compartan el mismo patrón.
		int seed = TextureSeed + (int)mineral * 10;

		var material = new StandardMaterial3D
		{
			AlbedoTexture = BuildTexture(look, seed, tileable: false),
			Metallic = look.Metallic,
			Roughness = look.Roughness,
		};

		blockMaterialCache[mineral] = material;

		return material;
	}

	// El inventario usa la misma textura del bloque como ícono.
	public static Texture2D GetBlockIcon(MineralType mineral)
	{
		return GetBlockMaterial(mineral).AlbedoTexture;
	}

	public static StandardMaterial3D GetFloorMaterial()
	{
		if (floorMaterialCache != null)
		{
			return floorMaterialCache;
		}

		MineralDefinition look = GameData.Floor;

		floorMaterialCache = new StandardMaterial3D
		{
			AlbedoTexture = BuildTexture(look, FloorSeed, tileable: true),
			Metallic = look.Metallic,
			Roughness = look.Roughness,

			// El piso es una sola caja estirada: con triplanar la textura se repite en vez de estirarse.
			Uv1Triplanar = true,
			Uv1Scale = new Vector3(0.25f, 0.25f, 0.25f),
		};

		return floorMaterialCache;
	}

	// Una imagen de ruido para el color base y otra para las vetas; se mezclan pixel a pixel.
	private static ImageTexture BuildTexture(MineralDefinition look, int seed, bool tileable)
	{
		Image baseNoise = MakeNoiseImage(seed, BaseFrequency, octaves: 4, tileable);
		Image veinNoise = MakeNoiseImage(seed + 1, VeinFrequency, octaves: 2, tileable);

		var image = Image.CreateEmpty(TextureSize, TextureSize, false, Image.Format.Rgb8);

		for (int y = 0; y < TextureSize; y++)
		{
			for (int x = 0; x < TextureSize; x++)
			{
				// Las imágenes de ruido son en escala de grises: R va de 0 a 1.
				Color color = BaseColor(look, baseNoise.GetPixel(x, y).R);
				color = AddVein(look, color, veinNoise.GetPixel(x, y).R);

				image.SetPixel(x, y, color);
			}
		}

		return ImageTexture.CreateFromImage(image);
	}

	private static Image MakeNoiseImage(int seed, float frequency, int octaves, bool tileable)
	{
		var noise = new FastNoiseLite
		{
			NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin,
			Seed = seed,
			Frequency = frequency,
			FractalType = FastNoiseLite.FractalTypeEnum.Fbm,
			FractalOctaves = octaves,
		};

		// GetSeamlessImage se repite sin costura, que es lo que necesita el piso.
		return tileable
			? noise.GetSeamlessImage(TextureSize, TextureSize)
			: noise.GetImage(TextureSize, TextureSize);
	}

	// Varía el color base entre 70% y 115% según el ruido.
	private static Color BaseColor(MineralDefinition look, float noise)
	{
		Color dark = look.BaseColor * 0.7f;
		Color light = look.BaseColor * 1.15f;

		return dark.Lerp(light, noise);
	}

	// Donde el ruido de vetas supera el umbral, se pinta hacia el color de la veta.
	private static Color AddVein(MineralDefinition look, Color color, float noise)
	{
		float threshold = 1.0f - look.VeinDensity;

		if (look.VeinDensity <= 0.0f || noise <= threshold)
		{
			return color;
		}

		float t = (noise - threshold) / look.VeinDensity;

		return color.Lerp(look.VeinColor, Mathf.Clamp(t, 0.0f, 1.0f));
	}
}
