/// <summary>
/// Nivel de herramienta. Un bloque solo se puede minar con una herramienta
/// de nivel igual o superior al que exige (<see cref="DestructibleBlock.RequiredTool"/>).
/// El orden de los valores importa: se comparan como enteros.
/// </summary>
public enum ToolTier
{
	None = 0,
	Basic = 1,
	Reinforced = 2,
	Advanced = 3
}
