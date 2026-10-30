/// <summary>
/// Qué pico hace falta para cada mineral y cuánto daño hace cada nivel de pico
/// (GDD: ciertos bloques solo se rompen con la herramienta indicada).
/// </summary>
public static class MineralRules
{
	// Sale del MineralDefinition de cada mineral (Data/Minerals).
	public static ToolTier RequiredTool(MineralType mineral)
	{
		return GameData.Mineral(mineral)?.RequiredTool ?? ToolTier.Basic;
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
