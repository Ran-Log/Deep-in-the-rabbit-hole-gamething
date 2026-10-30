using Godot;
using System.Collections.Generic;

/// <summary>
/// Genera por código la textura y el material de cada mineral y del piso.
/// Guarda uno por mineral (Flyweight): todos los bloques de piedra comparten el mismo.
/// </summary>
public static class MineralTextureFactory
{
	private const int TextureSize = 64;
	private const int BaseOctaves = 4;
	private const int VeinOctaves = 2;

	// Semilla fija (no la del mundo), para que cada mineral se vea igual en todas las partidas.
	private const ulong TextureSeed = 918273645UL;

	private static readonly Dictionary<MineralType, StandardMaterial3D> blockMaterialCache = new();
	private static StandardMaterial3D floorMaterialCache;

	public static StandardMaterial3D GetBlockMaterial(MineralType mineral)
	{
		if (blockMaterialCache.TryGetValue(mineral, out StandardMaterial3D cached))
		{
			return cached;
		}

		MineralMaterialProfile profile = MineralMaterialProfile.For(mineral);

		// Semilla distinta por mineral, para que no compartan el mismo patrón.
		ulong seed = DeterministicHash.Combine(TextureSeed, (int)mineral, 0);

		var material = new StandardMaterial3D
		{
			AlbedoTexture = BuildTexture(profile, seed, tileable: false),
			Metallic = profile.Metallic,
			Roughness = profile.Roughness,
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

		MineralMaterialProfile profile = MineralMaterialProfile.Floor();

		// -1 no choca con ningún valor de MineralType (Stone=0 .. Bedrock=4).
		ulong seed = DeterministicHash.Combine(TextureSeed, -1, 0);

		floorMaterialCache = new StandardMaterial3D
		{
			AlbedoTexture = BuildTexture(profile, seed, tileable: true),
			Metallic = profile.Metallic,
			Roughness = profile.Roughness,

			// El piso es una sola caja estirada: con triplanar la textura se repite en vez de estirarse.
			Uv1Triplanar = true,
			Uv1Scale = new Vector3(0.25f, 0.25f, 0.25f),
		};

		return floorMaterialCache;
	}

	// Una capa de ruido para el color base y otra para las vetas.
	private static ImageTexture BuildTexture(MineralMaterialProfile profile, ulong seed, bool tileable)
	{
		var image = Image.CreateEmpty(TextureSize, TextureSize, false, Image.Format.Rgb8);

		double uvScale = tileable ? TextureSize / 16.0 : TextureSize / 20.0;

		for (int y = 0; y < TextureSize; y++)
		{
			for (int x = 0; x < TextureSize; x++)
			{
				double nx = x / uvScale;
				double ny = y / uvScale;

				double baseNoise = ProceduralNoise.Fbm(seed, nx, ny, BaseOctaves);

				float r = Mathf.Lerp(profile.BaseR * 0.7f, profile.BaseR * 1.15f, (float)baseNoise);
				float g = Mathf.Lerp(profile.BaseG * 0.7f, profile.BaseG * 1.15f, (float)baseNoise);
				float b = Mathf.Lerp(profile.BaseB * 0.7f, profile.BaseB * 1.15f, (float)baseNoise);

				if (profile.VeinDensity > 0.0f)
				{
					double veinNoise = ProceduralNoise.Fbm(seed + 1UL, nx * 3.0, ny * 3.0, VeinOctaves);
					double veinThreshold = 1.0 - profile.VeinDensity;

					if (veinNoise > veinThreshold)
					{
						float t = (float)((veinNoise - veinThreshold) / profile.VeinDensity);
						r = Mathf.Lerp(r, profile.VeinR, t);
						g = Mathf.Lerp(g, profile.VeinG, t);
						b = Mathf.Lerp(b, profile.VeinB, t);
					}
				}

				image.SetPixel(x, y, new Color(
					Mathf.Clamp(r, 0.0f, 1.0f),
					Mathf.Clamp(g, 0.0f, 1.0f),
					Mathf.Clamp(b, 0.0f, 1.0f)));
			}
		}

		return ImageTexture.CreateFromImage(image);
	}
}
