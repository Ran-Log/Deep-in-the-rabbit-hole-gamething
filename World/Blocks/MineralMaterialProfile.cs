/// <summary>
/// Paleta y "look" PBR de cada mineral: color base, color de sus vetas o
/// brillos puntuales, y qué tan metálico/rugoso se ve. Es solo datos (sin
/// GodotSharp) para poder ajustarlo y testearlo sin abrir el editor; quien
/// realmente pinta los píxeles con esto es MineralTextureFactory.
///
/// No hay generador de imágenes por IA ni un banco de assets licenciados
/// disponible en este entorno (decisión tomada con el usuario), así que el
/// "realismo" acá es ruido procedural coloreado con estos parámetros, no
/// fotorrealismo — pero cada mineral queda con un color y brillo propios,
/// en vez de todos los bloques compartiendo una sola imagen pegada.
/// </summary>
public readonly struct MineralMaterialProfile
{
	/// <summary>Color base (RGB, 0..1) sobre el que varía el ruido de fondo.</summary>
	public readonly float BaseR;
	public readonly float BaseG;
	public readonly float BaseB;

	/// <summary>Color de las vetas o brillos puntuales (ej. las motas de oro).</summary>
	public readonly float VeinR;
	public readonly float VeinG;
	public readonly float VeinB;

	public readonly float Metallic;
	public readonly float Roughness;

	/// <summary>De 0 (sin vetas) a 1 (cubierto de vetas): qué tan seguido aparece una veta sobre el ruido base.</summary>
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
			// Piedra: gris apagado, sin brillo metálico, casi sin vetas.
			MineralType.Stone => new MineralMaterialProfile(
				0.45f, 0.44f, 0.42f,
				0.55f, 0.54f, 0.51f,
				0.05f, 0.9f, 0.0f),

			// Carbón: casi negro, con un leve brillo antracita azulado en
			// las vetas y algo más de metallic/menos roughness que la piedra.
			MineralType.Coal => new MineralMaterialProfile(
				0.045f, 0.045f, 0.055f,
				0.22f, 0.24f, 0.28f,
				0.25f, 0.55f, 0.08f),

			// Hierro: pardo-óxido, vetas más claras/rojizas, metallic medio.
			MineralType.Iron => new MineralMaterialProfile(
				0.42f, 0.24f, 0.16f,
				0.75f, 0.45f, 0.30f,
				0.6f, 0.45f, 0.12f),

			// Oro: base amarilla cálida con motas brillantes densas y muy
			// metálicas — el mineral más "llamativo" a propósito.
			MineralType.Gold => new MineralMaterialProfile(
				0.55f, 0.42f, 0.10f,
				1.0f, 0.85f, 0.25f,
				0.9f, 0.2f, 0.18f),

			// Bedrock: casi negro azulado, muy rugoso, casi sin vetas — se
			// ve "duro" e indestructible.
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
