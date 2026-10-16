using Godot;

/// <summary>
/// Ataque cuerpo a cuerpo con el pico (GDD 2.1.7). Usa el mismo botón que
/// minar ("mine"): si la mira apunta a un enemigo, le pega; si apunta a un
/// bloque, MiningComponent se encarga. Los dos usan el mismo rayo
/// (CrosshairRay), así que nunca reaccionan los dos al mismo objetivo.
/// </summary>
public partial class CombatComponent : Node3D
{
	[Signal]
	public delegate void EnemyHitEventHandler(Zombie enemy);

	[Export]
	public Camera3D Camera;

	[Export]
	public CollisionObject3D ExcludedBody;

	[Export]
	public PlayerEquipment Equipment;

	/// <summary>Alcance medido desde la cámara (que está ~2 m detrás del jugador).</summary>
	[Export]
	public float Range = 5.0f;

	[Export]
	public float AttackCooldown = 0.45f;

	[Export(PropertyHint.Layers3DPhysics)]
	public uint TargetLayers = 1;

	private float cooldownLeft;

	public override void _PhysicsProcess(double delta)
	{
		cooldownLeft = Mathf.Max(0.0f, cooldownLeft - (float)delta);

		if (cooldownLeft > 0.0f
			|| !CrosshairRay.PlayerHasControl()
			|| !Input.IsActionPressed("mine"))
		{
			return;
		}

		if (!CrosshairRay.Cast(Camera, Range, TargetLayers, ExcludedBody, out CrosshairRay.Hit hit)
			|| hit.Collider is not Zombie zombie)
		{
			return;
		}

		cooldownLeft = AttackCooldown;

		int damage = Equipment?.CombatDamage ?? 1;
		Vector3 from = ExcludedBody != null ? ExcludedBody.GlobalPosition : Camera.GlobalPosition;

		zombie.TakeHit(damage, from);
		EmitSignal(SignalName.EnemyHit, zombie);
	}
}
