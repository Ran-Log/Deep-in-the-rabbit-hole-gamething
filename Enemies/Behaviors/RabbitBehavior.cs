using System.Collections.Generic;
using Godot;

/// <summary>
/// Conejo rabioso (GDD): van en grupo, persiguen al jugador (Seek + Separation para no
/// amontonarse) y huyen unos segundos al recibir daño (Flee). Si golpean a uno, los cercanos
/// también huyen (pánico de grupo). Como la "Coward Rat" de Axy 10.
/// </summary>
public partial class RabbitBehavior : EnemyBehavior
{
	public const string Group = "rabbits";

	[ExportCategory("RabbitBehavior")]
	[ExportGroup("Detección")]
	[Export] public float AggroRange = 14.0f;

	[ExportGroup("Huida")]
	[Export] public float FleeSeconds = 2.5f;
	// Si golpean a un conejo, los que estén a esta distancia también huyen. 0 = solo el golpeado.
	[Export] public float PanicRadius = 6.0f;

	[ExportGroup("Grupo")]
	// Distancia a la que empieza a alejarse de los otros conejos.
	[Export] public float SeparationRadius = 1.6f;
	[Export] public float SeparationWeight = 1.2f;

	private float fleeLeft;

	public bool IsFleeing => fleeLeft > 0.0f;

	public override void _Ready()
	{
		GetParent().AddToGroup(Group);
	}

	public override Node3D ChooseTarget(Enemy enemy)
	{
		// Huyendo no ataca.
		return !IsFleeing && IsWithin(enemy, enemy.Player, AggroRange) ? enemy.Player : null;
	}

	public override Vector3 ChooseDirection(Enemy enemy, Node3D target, float delta)
	{
		fleeLeft = Mathf.Max(0.0f, fleeLeft - delta);

		if (IsFleeing && enemy.Player != null)
		{
			return Steering.Flee(enemy.GlobalPosition, enemy.Player.GlobalPosition);
		}

		Vector3 seek = target != null
			? Steering.Seek(enemy.GlobalPosition, target.GlobalPosition, enemy.AttackRange * 0.8f)
			: Vector3.Zero;

		Vector3 separation = Steering.Separation(enemy.GlobalPosition, OtherRabbits(enemy), SeparationRadius);

		// Combinar comportamientos con pesos (clase 12).
		return (seek + separation * SeparationWeight).LimitLength(1.0f);
	}

	public override void OnHit(Enemy enemy, Vector3 fromPosition)
	{
		Scare();

		// Pánico de grupo: avisa a los conejos cercanos.
		foreach (Node node in GetTree().GetNodesInGroup(Group))
		{
			if (node != enemy
				&& node is Enemy other
				&& other.Behavior is RabbitBehavior otherRabbit
				&& other.GlobalPosition.DistanceTo(enemy.GlobalPosition) < PanicRadius)
			{
				otherRabbit.Scare();
			}
		}
	}

	public void Scare()
	{
		fleeLeft = FleeSeconds;
	}

	private IEnumerable<Vector3> OtherRabbits(Enemy self)
	{
		foreach (Node node in GetTree().GetNodesInGroup(Group))
		{
			if (node != self && node is Node3D other)
			{
				yield return other.GlobalPosition;
			}
		}
	}
}
