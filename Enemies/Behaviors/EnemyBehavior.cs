using Godot;

/// <summary>
/// El componente de IA de un enemigo (clases 10 y 11): decide a quién atacar y hacia dónde
/// moverse. El movimiento en sí (física) lo hace Enemy. Cada tipo de enemigo tiene el suyo.
/// </summary>
public abstract partial class EnemyBehavior : Node
{
	// A quién atacar si queda al alcance, o null.
	public abstract Node3D ChooseTarget(Enemy enemy);

	// Hacia dónde moverse: un vector horizontal de largo 0 a 1 (Enemy lo multiplica por su Speed).
	public abstract Vector3 ChooseDirection(Enemy enemy, Node3D target, float delta);

	// Reacción al recibir un golpe (ej. el conejo huye).
	public virtual void OnHit(Enemy enemy, Vector3 fromPosition)
	{
	}

	protected static bool IsWithin(Enemy enemy, Node3D other, float range)
	{
		return other != null && enemy.HorizontalDistanceTo(other) < range;
	}
}
