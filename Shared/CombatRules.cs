using System;

/// <summary>
/// Reglas de combate en C# puro (sin Godot), para probarlas en el harness.
/// GDD 2.1.7: las mejoras de arma "aumentan la efectividad contra ciertos enemigos".
/// </summary>
public static class CombatRules
{
	// La armadura resta daño a cada golpe; siempre entra al menos 1, para que ningún enemigo sea imposible.
	public static int DamageAfterArmor(int damage, int armor)
	{
		return Math.Max(1, damage - Math.Max(0, armor));
	}

	// Un golpe "casi no le hace nada" cuando la armadura se come todo el daño.
	public static bool IsResisted(int damage, int armor)
	{
		return damage <= armor;
	}
}
