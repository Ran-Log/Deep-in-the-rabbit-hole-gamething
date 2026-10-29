using Godot;
using System.Collections.Generic;

/// <summary>
/// Genera, por código y sin ningún archivo de imagen, las texturas y
/// materiales de cada mineral y del piso de las salas — ver
/// MineralMaterialProfile (paleta/PBR por mineral) y ProceduralNoise (el
/// ruido en sí, testeado aparte porque no depende de GodotSharp). Esta
/// clase es la única parte "Godot" del trío: arma el Image pixel a pixel y
/// lo empaqueta en un StandardMaterial3D.
///
/// Cachea un material por mineral (y uno para el piso) la primera vez que
/// se pide: generar una imagen de TextureSize x TextureSize por cada
/// bloque individual sería carísimo con cientos de bloques por sala, y
/// además todos los bloques del mismo mineral deben verse igual.
/// </summary>
public static class MineralTextureFactory
{
	private const int TextureSize = 64;
	private const int BaseOctaves = 4;
	private const int VeinOctaves = 2;

	/// <summary>
	/// Semilla fija de las texturas — a propósito NO es la semilla del
	/// mundo (WorldState.Seed), para que un mismo mineral se vea siempre
	/// igual sin importar la partida.
	/// </summary>
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

		// Cada mineral usa su propia sub-semilla (vía DeterministicHash) para
		// que, por ejemplo, Iron y Gold no compartan el mismo patrón de
		// ruido con distinto color encima.
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

	/// <summary>Ícono para la UI de inventario: la misma textura que ya se generó para el bloque, sin generar nada aparte ni depender de un .png en disco.</summary>
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

			// El piso es un solo BoxMesh estirado a toda la sala (ver
			// Room.ResizeFloor): con triplanar la textura se repite según el
			// tamaño real en vez de estirarse en una sola pieza gigante.
			Uv1Triplanar = true,
			Uv1Scale = new Vector3(0.25f, 0.25f, 0.25f),
		};

		return floorMaterialCache;
	}

	/// <summary>
	/// Pinta una imagen de TextureSize x TextureSize: ruido fBm de base
	/// (BaseOctaves) para la variación de color, más una segunda capa de
	/// ruido de mayor frecuencia y semilla distinta que decide dónde
	/// aparecen las vetas/motas de color (VeinDensity de la paleta).
	/// </summary>
	private static ImageTexture BuildTexture(MineralMaterialProfile profile, ulong seed, bool tileable)
	{
		var image = Image.CreateEmpty(TextureSize, TextureSize, false, Image.Format.Rgb8);

		// Con tileable, el ruido se muestrea en un rango que se repite cada
		// TextureSize píxeles (mod implícito de ValueNoise sobre enteros),
		// para que el triplanar del piso no muestre una costura entre
		// tiles. Con blocks (tileable=false) no importa: cada cara del
		// cubo usa la textura completa una sola vez.
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
