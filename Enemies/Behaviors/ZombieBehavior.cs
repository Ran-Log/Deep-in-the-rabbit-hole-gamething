using Godot;

/// <summary>
/// Zombie (GDD): Seek al jugador si está cerca; si no, Seek al campamento "aunque él no esté ahí".
/// </summary>
public partial class ZombieBehavior : EnemyBehavior
{
	[ExportCategory("ZombieBehavior")]
	[ExportGroup("Detección")]
	[Export] public float AggroRange = 9.0f;

	public override Node3D ChooseTarget(Enemy enemy)
	{
		return IsWithin(enemy, enemy.Player, AggroRange) ? enemy.Player : enemy.CampCore;
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
