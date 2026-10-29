using Godot;

/// <summary>
/// Zombie minero (GDD: "se mueven directamente hacia el campamento del
/// jugador aunque él no esté ahí, tienen más daño hacia estructuras").
///
/// Comportamiento, a propósito simple (la IA no se evalúa en la alfa):
/// 1. Si el jugador está a menos de <see cref="AggroRange"/>, lo persigue.
/// 2. Si no, camina en línea recta al núcleo del campamento (grupo
///    "camp_core") y le pega al llegar.
/// 3. Si choca con un bloque (una barricada o una pared), lo golpea hasta
///    romperlo — por eso construir defensas lo frena.
/// </summary>
public partial class Zombie : CharacterBody3D
{
	public const string Group = "enemies";
	public const string CampCoreGroup = "camp_core";

	[Export]
	public HealthComponent Health;

	[Export]
	public MeshInstance3D Body;

	[Export]
	public float Speed = 2.6f;

	[Export]
	public float Gravity = 20.0f;

	[Export]
	public float AggroRange = 9.0f;

	[Export]
	public float AttackRange = 2.0f;

	[Export]
	public float CampAttackRange = 2.8f;

	[Export]
	public int PlayerDamage = 10;

	[Export]
	public int CampDamage = 8;

	/// <summary>Daño por golpe a bloques. Alto a propósito: según el GDD los zombies son buenos rompiendo estructuras.</summary>
	[Export]
	public int BlockDamage = 3;

	[Export]
	public float AttackCooldown = 1.2f;

	[Export]
	public float KnockbackStrength = 7.0f;

	private const float FallLimitY = -40.0f;

	private float cooldownLeft;
	private Vector3 knockback;
	private StandardMaterial3D bodyMaterial;
	private Color baseColor;

	public override void _EnterTree()
	{
		AddToGroup(Group);
	}

	public override void _Ready()
	{
		if (Health != null)
		{
			Health.Died += OnDied;
		}

		bodyMaterial = Body?.MaterialOverride as StandardMaterial3D;

		if (bodyMaterial != null)
		{
			// Copia propia: si no, el flash rojo pintaría a TODOS los zombies.
			bodyMaterial = (StandardMaterial3D)bodyMaterial.Duplicate();
			Body.MaterialOverride = bodyMaterial;
			baseColor = bodyMaterial.AlbedoColor;
		}
	}

	public override void _ExitTree()
	{
		if (Health != null)
		{
			Health.Died -= OnDied;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;

		// Si la sala donde estaba se descargó (streaming), se queda sin piso
		// y cae para siempre: se elimina en vez de seguir procesándolo.
		if (GlobalPosition.Y < FallLimitY)
		{
			QueueFree();
			return;
		}

		cooldownLeft = Mathf.Max(0.0f, cooldownLeft - dt);

		Node3D player = GetTree().GetFirstNodeInGroup("player") as Node3D;
		Node3D campCore = GetTree().GetFirstNodeInGroup(CampCoreGroup) as Node3D;

		bool chasingPlayer = player != null && HorizontalDistanceTo(player) < AggroRange;
		Node3D target = chasingPlayer ? player : campCore;

		Move(target, dt);

		if (target != null)
		{
			TryAttack(target, chasingPlayer);
		}

		TryBreakBlockingBlock();
	}

	/// <summary>Lo llama CombatComponent. <paramref name="fromPosition"/> es desde dónde vino el golpe (para el empujón).</summary>
	public void TakeHit(int damage, Vector3 fromPosition)
	{
		Vector3 away = GlobalPosition - fromPosition;
		away.Y = 0.0f;

		if (away.LengthSquared() > 0.001f)
		{
			knockback = away.Normalized() * KnockbackStrength;
		}

		FlashHit();
		Health?.TakeDamage(damage);
	}

	/// <summary>Cuántos zombies hay a menos de <paramref name="radius"/> metros (la cama lo usa para "estás protegido").</summary>
	public static int CountNear(SceneTree tree, Vector3 position, float radius)
	{
		int count = 0;

		foreach (Node node in tree.GetNodesInGroup(Group))
		{
			if (node is Node3D enemy && enemy.GlobalPosition.DistanceTo(position) < radius)
			{
				count++;
			}
		}

		return count;
	}

	private void Move(Node3D target, float dt)
	{
		Vector3 velocity = Velocity;
		Vector3 horizontal = Vector3.Zero;

		if (target != null)
		{
			Vector3 toTarget = target.GlobalPosition - GlobalPosition;
			toTarget.Y = 0.0f;

			if (toTarget.Length() > AttackRange * 0.8f)
			{
				horizontal = toTarget.Normalized() * Speed;
				LookAt(GlobalPosition + toTarget, Vector3.Up);
			}
		}

		// El empujón de un golpe se suma una vez y se va apagando, en vez
		// de re-sumarse cada frame (eso lo haría acumularse sin control).
		horizontal += knockback;
		knockback = knockback.MoveToward(Vector3.Zero, KnockbackStrength * 4.0f * dt);

		velocity.X = horizontal.X;
		velocity.Z = horizontal.Z;
		velocity.Y = IsOnFloor() ? 0.0f : velocity.Y - Gravity * dt;

		Velocity = velocity;
		MoveAndSlide();
	}

	private void TryAttack(Node3D target, bool targetIsPlayer)
	{
		if (cooldownLeft > 0.0f)
		{
			return;
		}

		float range = targetIsPlayer ? AttackRange : CampAttackRange;

		if (HorizontalDistanceTo(target) > range)
		{
			return;
		}

		HealthComponent targetHealth = target.GetNodeOrNull<HealthComponent>("Health");

		if (targetHealth == null)
		{
			return;
		}

		targetHealth.TakeDamage(targetIsPlayer ? PlayerDamage : CampDamage);
		cooldownLeft = AttackCooldown;
	}

	private void TryBreakBlockingBlock()
	{
		if (cooldownLeft > 0.0f)
		{
			return;
		}

		for (int i = 0; i < GetSlideCollisionCount(); i++)
		{
			if (GetSlideCollision(i).GetCollider() is DestructibleBlock block)
			{
				// Advanced: un zombie no "necesita" la herramienta adecuada,
				// solo es lento rompiendo cosas duras.
				block.TakeDamage(BlockDamage, ToolTier.Advanced);
				cooldownLeft = AttackCooldown;
				return;
			}
		}
	}

	private float HorizontalDistanceTo(Node3D other)
	{
		Vector3 diff = other.GlobalPosition - GlobalPosition;
		diff.Y = 0.0f;
		return diff.Length();
	}

	private void FlashHit()
	{
		if (bodyMaterial == null)
		{
			return;
		}

		bodyMaterial.AlbedoColor = new Color(1.0f, 0.25f, 0.25f);

		Tween tween = CreateTween();
		tween.TweenProperty(bodyMaterial, "albedo_color", baseColor, 0.2f);
	}

	private void OnDied()
	{
		GameEvents.PostMessage("Derrotaste a un zombie");
		QueueFree();
	}
}
