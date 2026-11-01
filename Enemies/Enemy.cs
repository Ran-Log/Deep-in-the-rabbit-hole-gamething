using Godot;

/// <summary>
/// Un enemigo, como contenedor de componentes (clase 10): vida (HealthComponent) e IA
/// (un EnemyBehavior que decide a quién atacar y hacia dónde ir). Esta clase hace lo común
/// a todos: moverse con la física, atacar, romper bloques y destellar al recibir daño.
/// </summary>
public partial class Enemy : CharacterBody3D, IDamageable
{
	public const string Group = "enemies";
	public const string CampCoreGroup = "camp_core";
	public const string PlayerGroup = "player";

	[ExportCategory("Enemy")]
	[ExportGroup("Referencias")]
	[Export] public HealthComponent Health;
	[Export] public MeshInstance3D Body;
	[Export] public EnemyBehavior Behavior;

	[ExportGroup("Datos")]
	// Para el mensaje al derrotarlo ("Derrotaste a un zombie").
	[Export] public string DisplayName = "un enemigo";

	[ExportGroup("Movimiento")]
	[Export] public float Speed = 2.6f;
	[Export] public float Gravity = 20.0f;
	[Export] public float KnockbackStrength = 7.0f;

	[ExportGroup("Combate")]
	[Export] public float AttackRange = 2.0f;
	[Export] public float CampAttackRange = 2.8f;
	[Export] public int PlayerDamage = 10;
	[Export] public int CampDamage = 8;
	// Daño a los bloques que lo tapan; 0 = no rompe bloques.
	[Export] public int BlockDamage = 3;
	[Export] public float AttackCooldown = 1.2f;
	// Resta daño a cada golpe que recibe (GDD 2.1.7: hace falta un arma mejor).
	[Export(PropertyHint.Range, "0,10,1")] public int Armor { get; set; } = 0;

	[ExportGroup("Aparición")]
	// Cuántos aparecen juntos (ej. los conejos van en grupo).
	[Export] public int SpawnGroupSize = 1;
	// GDD: las arañas solo aparecen en salas oscuras (sin tragaluz).
	[Export] public bool OnlyInDarkRooms = false;

	private const float FallLimitY = -40.0f;

	private float cooldownLeft;
	private Vector3 knockback;
	private StandardMaterial3D bodyMaterial;
	private Color baseColor;
	private Node3D player;
	private Node3D campCore;

	// Se buscan una vez en _Ready (antes se buscaban en cada frame).
	public Node3D Player => IsInstanceValid(player) ? player : null;

	public Node3D CampCore => IsInstanceValid(campCore) ? campCore : null;

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

		if (Behavior == null)
		{
			GD.PushError($"{Name}: falta asignar Behavior; el enemigo no se va a mover.");
		}

		player = GetTree().GetFirstNodeInGroup(PlayerGroup) as Node3D;
		campCore = GetTree().GetFirstNodeInGroup(CampCoreGroup) as Node3D;

		bodyMaterial = Body?.MaterialOverride as StandardMaterial3D;

		if (bodyMaterial != null)
		{
			// Copia propia: si no, el destello rojo pintaría a TODOS los enemigos de este tipo.
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

		// Si la sala donde estaba se descargó, se queda sin piso y cae para siempre.
		if (GlobalPosition.Y < FallLimitY)
		{
			QueueFree();
			return;
		}

		cooldownLeft = Mathf.Max(0.0f, cooldownLeft - dt);

		// La IA decide (Behavior); la física mueve (acá).
		Node3D target = Behavior?.ChooseTarget(this);
		Vector3 direction = Behavior?.ChooseDirection(this, target, dt) ?? Vector3.Zero;

		Move(direction, dt);

		if (target != null)
		{
			TryAttack(target);
		}

		if (BlockDamage > 0)
		{
			TryBreakBlockingBlock();
		}
	}

	// Lo llama CombatComponent. fromPosition es desde dónde vino el golpe (para el retroceso).
	public void TakeHit(int damage, Vector3 fromPosition)
	{
		Vector3 away = GlobalPosition - fromPosition;
		away.Y = 0.0f;

		if (away.LengthSquared() > 0.001f)
		{
			knockback = away.Normalized() * KnockbackStrength;
		}

		FlashHit();
		Behavior?.OnHit(this, fromPosition);
		Health?.TakeDamage(CombatRules.DamageAfterArmor(damage, Armor));
	}

	public float HorizontalDistanceTo(Node3D other)
	{
		Vector3 diff = other.GlobalPosition - GlobalPosition;
		diff.Y = 0.0f;
		return diff.Length();
	}

	// Cuántos enemigos hay a menos de radius metros (la cama lo usa para "estás protegido").
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

	// Vf = v * Df (clase 11), más el retroceso de un golpe y la gravedad.
	private void Move(Vector3 direction, float dt)
	{
		Vector3 horizontal = direction * Speed;

		if (direction.LengthSquared() > 0.0001f)
		{
			LookAt(GlobalPosition + direction, Vector3.Up);
		}

		// El retroceso se suma una vez y se va apagando (si se sumara cada frame, se acumularía).
		horizontal += knockback;
		knockback = knockback.MoveToward(Vector3.Zero, KnockbackStrength * 4.0f * dt);

		Vector3 velocity = Velocity;
		velocity.X = horizontal.X;
		velocity.Z = horizontal.Z;
		velocity.Y = IsOnFloor() ? 0.0f : velocity.Y - Gravity * dt;

		Velocity = velocity;
		MoveAndSlide();
	}

	private void TryAttack(Node3D target)
	{
		if (cooldownLeft > 0.0f)
		{
			return;
		}

		bool targetIsPlayer = target.IsInGroup(PlayerGroup);
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
				// Advanced: un enemigo no "necesita" el pico adecuado, solo es lento con lo duro.
				block.TakeDamage(BlockDamage, ToolTier.Advanced);
				cooldownLeft = AttackCooldown;
				return;
			}
		}
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
		GameEvents.PostMessage($"Derrotaste a {DisplayName}");
		QueueFree();
	}
}
