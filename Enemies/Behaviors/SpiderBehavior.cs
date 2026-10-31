using Godot;

/// <summary>
/// Araña (GDD): deambula (Wander) hasta ver al jugador cerca, y entonces lo persigue (Seek).
/// </summary>
public partial class SpiderBehavior : EnemyBehavior
{
	[ExportCategory("SpiderBehavior")]
	[ExportGroup("Detección")]
	[Export] public float AggroRange = 12.0f;

	[ExportGroup("Deambular")]
	// Cuánto puede girar por segundo al deambular (radianes).
	[Export] public float WanderTurnSpeed = 2.5f;
	// Deambulando va más lento que persiguiendo (0 a 1).
	[Export(PropertyHint.Range, "0,1,0.05")] public float WanderSpeedFactor = 0.4f;

	private readonly RandomNumberGenerator rng = new();
	private Vector3 wanderDirection;

	public override void _Ready()
	{
		rng.Randomize();
	}

	public override Node3D ChooseTarget(Enemy enemy)
	{
		return IsWithin(enemy, enemy.Player, AggroRange) ? enemy.Player : null;
	}

	public override Vector3 ChooseDirection(Enemy enemy, Node3D target, float delta)
	{
		if (target != null)
		{
			return Steering.Seek(enemy.GlobalPosition, target.GlobalPosition, enemy.AttackRange * 0.8f);
		}

		wanderDirection = Steering.Wander(wanderDirection, WanderTurnSpeed * delta, rng);
		return wanderDirection * WanderSpeedFactor;
	}
}
