using Godot;

/// <summary>
/// Algo que se puede golpear (enemigos, nidos). CombatComponent golpea
/// cualquier IDamageable sin saber qué es.
/// </summary>
public interface IDamageable
{
	// Resta daño a cada golpe (ver CombatRules). 0 = sin armadura.
	int Armor { get; }

	// fromPosition = desde dónde vino el golpe (para el retroceso).
	void TakeHit(int damage, Vector3 fromPosition);
}
