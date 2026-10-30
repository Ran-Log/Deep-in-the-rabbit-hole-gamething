/// <summary>
/// Colores y brillo de cada mineral (base, vetas, metálico, rugosidad).
/// Los usa MineralTextureFactory para pintar la textura.
/// </summary>
public readonly struct MineralMaterialProfile
{
	// Color base (RGB, 0..1).
	public readonly float BaseR;
	public readonly float BaseG;
	public readonly float BaseB;

	// Color de las vetas (ej. las motas del oro).
	public readonly float VeinR;
	public readonly float VeinG;
	public readonly float VeinB;

	public readonly float Metallic;
	public readonly float Roughness;

	// 0 = sin vetas, 1 = cubierto de vetas.
	public readonly float VeinDensity;

	public MineralMaterialProfile(
		float baseR, float baseG, float baseB,
		float veinR, float veinG, float veinB,
		float metallic, float roughness, float veinDensity)
	{
		BaseR = baseR;
		BaseG = baseG;
		BaseB = baseB;
		VeinR = veinR;
		VeinG = veinG;
		VeinB = veinB;
		Metallic = metallic;
		Roughness = roughness;
		VeinDensity = veinDensity;
	}

	public static MineralMaterialProfile For(MineralType mineral)
	{
		return mineral switch
		{
			// Piedra: gris apagado, sin vetas.
			MineralType.Stone => new MineralMaterialProfile(
				0.45f, 0.44f, 0.42f,
				0.55f, 0.54f, 0.51f,
				0.05f, 0.9f, 0.0f),

			// Carbón: casi negro, con vetas azuladas.
			MineralType.Coal => new MineralMaterialProfile(
				0.045f, 0.045f, 0.055f,
				0.22f, 0.24f, 0.28f,
				0.25f, 0.55f, 0.08f),

			// Hierro: color óxido, algo metálico.
			MineralType.Iron => new MineralMaterialProfile(
				0.42f, 0.24f, 0.16f,
				0.75f, 0.45f, 0.30f,
				0.6f, 0.45f, 0.12f),

			// Oro: amarillo con motas brillantes, el más llamativo.
			MineralType.Gold => new MineralMaterialProfile(
				0.55f, 0.42f, 0.10f,
				1.0f, 0.85f, 0.25f,
				0.9f, 0.2f, 0.18f),

			// Bedrock: casi negro y muy rugoso, se ve indestructible.
			MineralType.Bedrock => new MineralMaterialProfile(
				0.08f, 0.09f, 0.11f,
				0.16f, 0.17f, 0.20f,
				0.1f, 0.95f, 0.02f),

			_ => new MineralMaterialProfile(
				0.5f, 0.5f, 0.5f,
				0.6f, 0.6f, 0.6f,
				0.0f, 1.0f, 0.0f),
		};
	}

	/// <summary>Piso de las salas: tierra/roca de mina, opaca y sin brillo metálico.</summary>
	public static MineralMaterialProfile Floor()
	{
		return new MineralMaterialProfile(
			0.28f, 0.22f, 0.16f,
			0.16f, 0.13f, 0.10f,
			0.0f, 1.0f, 0.0f);
	}
}
