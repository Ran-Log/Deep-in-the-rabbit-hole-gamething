using Godot;

/// <summary>
/// Algo que se puede golpear con el pico (enemigos, nidos). CombatComponent golpea
/// cualquier IDamageable sin saber qué es.
/// </summary>
public interface IDamageable
{
	// fromPosition = desde dónde vino el golpe (para el retroceso).
	void TakeHit(int damage, Vector3 fromPosition);
}
