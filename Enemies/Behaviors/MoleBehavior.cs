using Godot;

/// <summary>
/// Topo (GDD): cava túneles hacia el jugador. Seek al jugador desde lejos; las paredes no
/// lo detienen porque rompe bloques rápido (su BlockDamage es alto en Mole.tscn).
/// </summary>
public partial class MoleBehavior : EnemyBehavior
{
	[ExportCategory("MoleBehavior")]
	[ExportGroup("Detección")]
	// Lo "huele" desde lejos, a través de las paredes.
	[Export] public float DetectRange = 40.0f;

	public override Node3D ChooseTarget(Enemy enemy)
	{
		return IsWithin(enemy, enemy.Player, DetectRange) ? enemy.Player : null;
	}

	public override Vector3 ChooseDirection(Enemy enemy, Node3D target, float delta)
	{
		if (target == null)
		{
			return Vector3.Zero;
		}

		return Steering.Seek(enemy.GlobalPosition, target.GlobalPosition, enemy.AttackRange * 0.8f);
	}
}
