using Godot;

/// <summary>
/// Ataque (GDD 2.1.7). Usa el mismo botón que minar: si la mira apunta a algo que se puede
/// golpear (IDamageable: enemigos, nidos) le pega con el arma o el pico (lo que pegue más fuerte);
/// si es un bloque, mina MiningComponent.
/// </summary>
public partial class CombatComponent : Node3D
{
	[Signal]
	public delegate void EnemyHitEventHandler(Node3D target);

	// La armadura del objetivo se comió el golpe: hace falta un arma mejor.
	[Signal]
	public delegate void HitResistedEventHandler(Node3D target);

	[ExportCategory("CombatComponent")]
	[ExportGroup("Referencias")]
	[Export] public Camera3D Camera;
	[Export] public CollisionObject3D ExcludedBody;
	[Export] public PlayerEquipment Equipment;

	[ExportGroup("Combate")]
	// Se mide desde la cámara, que está ~2 m detrás del jugador.
	[Export] public float Range = 5.0f;
	[Export] public float AttackCooldown = 0.45f;
	[Export(PropertyHint.Layers3DPhysics)] public uint TargetLayers = 1;
	// Cada cuánto se puede repetir el aviso de "arma muy débil".
	[Export] public float ResistedMessageSeconds = 4.0f;

	private float cooldownLeft;
	private float resistedMessageLeft;

	public override void _PhysicsProcess(double delta)
	{
		cooldownLeft = Mathf.Max(0.0f, cooldownLeft - (float)delta);
		resistedMessageLeft = Mathf.Max(0.0f, resistedMessageLeft - (float)delta);

		if (cooldownLeft > 0.0f
			|| !CrosshairRay.PlayerHasControl()
			|| !Input.IsActionPressed("mine"))
		{
			return;
		}

		if (!CrosshairRay.Cast(Camera, Range, TargetLayers, ExcludedBody, out CrosshairRay.Hit hit)
			|| hit.Collider is not IDamageable target)
		{
			return;
		}

		cooldownLeft = AttackCooldown;
		Vector3 from = ExcludedBody != null ? ExcludedBody.GlobalPosition : Camera.GlobalPosition;

		Strike(target, (Node3D)hit.Collider, from);
	}

	// Público para probarlo sin apuntar con el mouse.
	public void Strike(IDamageable target, Node3D targetNode, Vector3 from)
	{
		int damage = Equipment?.CombatDamage ?? 1;

		target.TakeHit(damage, from);

		if (CombatRules.IsResisted(damage, target.Armor))
		{
			WarnResisted();
			EmitSignal(SignalName.HitResisted, targetNode);
		}
		else
		{
			EmitSignal(SignalName.EnemyHit, targetNode);
		}
	}

	private void WarnResisted()
	{
		if (resistedMessageLeft > 0.0f)
		{
			return;
		}

		resistedMessageLeft = ResistedMessageSeconds;
		GameEvents.PostMessage("Casi no le haces daño: forja un arma mejor en la estación");
	}
}
