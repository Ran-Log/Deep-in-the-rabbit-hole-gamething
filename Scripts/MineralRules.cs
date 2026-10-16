/// <summary>
/// Reglas de juego de cada mineral: qué herramienta hace falta para romperlo
/// y cuánto daño hace cada nivel de pico. Separado de los nodos para que el
/// balance se ajuste en un solo lugar y se pueda testear sin GodotSharp.
///
/// Regla del GDD: "ciertos bloques solo pueden ser destruidos por la
/// herramienta indicada" y "el nivel de las herramientas determina qué
/// bloques se pueden destruir y qué tan rápido".
/// </summary>
public static class MineralRules
{
	public static ToolTier RequiredTool(MineralType mineral)
	{
		return mineral switch
		{
			MineralType.Iron => ToolTier.Reinforced,
			MineralType.Gold => ToolTier.Advanced,
			MineralType.Bedrock => ToolTier.Advanced,
			_ => ToolTier.Basic,
		};
	}

	/// <summary>Daño por golpe de cada pico, tanto a bloques como a enemigos.</summary>
	public static int ToolDamage(ToolTier tier)
	{
		return tier switch
		{
			ToolTier.Basic => 1,
			ToolTier.Reinforced => 2,
			ToolTier.Advanced => 3,
			_ => 0,
		};
	}
}
