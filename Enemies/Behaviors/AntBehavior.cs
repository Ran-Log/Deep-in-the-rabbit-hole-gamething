using Godot;

/// <summary>
/// Hormiga gigante (GDD): sale de un nido. Deambula cerca de él (Wander), vuelve si se aleja
/// mucho (Seek al nido) y persigue al jugador si se acerca.
/// </summary>
public partial class AntBehavior : EnemyBehavior
{
	[ExportCategory("AntBehavior")]
	[ExportGroup("Detección")]
	[Export] public float AggroRange = 8.0f;

	[ExportGroup("Nido")]
	// Si se aleja más que esto del nido, vuelve.
	[Export] public float LeashRadius = 7.0f;
	[Export] public float WanderTurnSpeed = 2.0f;
	[Export(PropertyHint.Range, "0,1,0.05")] public float WanderSpeedFactor = 0.5f;

	private readonly RandomNumberGenerator rng = new();
	private Vector3 wanderDirection;
	private bool hasHome;
	private Vector3 home;

	// Lo pone el nido al crearla.
	public Vector3 Home
	{
		get => home;
		set
		{
			home = value;
			hasHome = true;
		}
	}

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
		if (!hasHome)
		{
			Home = enemy.GlobalPosition;
		}

		if (target != null)
		{
			return Steering.Seek(enemy.GlobalPosition, target.GlobalPosition, enemy.AttackRange * 0.8f);
		}

		if (enemy.GlobalPosition.DistanceTo(home) > LeashRadius)
		{
			return Steering.Seek(enemy.GlobalPosition, home);
		}

		wanderDirection = Steering.Wander(wanderDirection, WanderTurnSpeed * delta, rng);
		return wanderDirection * WanderSpeedFactor;
	}
}
