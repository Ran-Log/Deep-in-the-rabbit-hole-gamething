using Godot;

/// <summary>
/// Minar: golpea el bloque apuntado mientras se mantiene "mine", y al romperlo
/// suma el mineral a la mochila (GDD 2.1.2). Avisa al HUD con señales.
/// </summary>
public partial class MiningComponent : Node3D
{
	[Signal]
	public delegate void TargetAcquiredEventHandler(DestructibleBlock block);

	[Signal]
	public delegate void TargetLostEventHandler();

	/// <summary>El golpe no entró porque la herramienta no alcanza.</summary>
	[Signal]
	public delegate void HitRejectedEventHandler(DestructibleBlock block);

	[Signal]
	public delegate void BlockDestroyedEventHandler(int mineral);

	[ExportCategory("MiningComponent")]
	[ExportGroup("Referencias")]
	[Export] public Camera3D Camera;
	// El rayo lo ignora (el propio jugador).
	[Export] public CollisionObject3D ExcludedBody;
	[Export] public PlayerEquipment Equipment;

	[ExportGroup("Minado")]
	[Export] public float Range = 6.0f;
	// Segundos entre golpe y golpe mientras se mantiene presionado.
	[Export] public float HitCooldown = 0.3f;
	[Export(PropertyHint.Layers3DPhysics)] public uint TargetLayers = 1;

	private DestructibleBlock target;
	private float cooldownLeft;
	private bool enabled = true;

	/// <summary>Bloque apuntado ahora mismo, o null.</summary>
	public DestructibleBlock Target => GodotObject.IsInstanceValid(target) ? target : null;

	public ToolTier Tool => Equipment?.Tool ?? ToolTier.Basic;

	// BuildComponent lo apaga mientras se construye, para que el clic no haga las dos cosas.
	public bool Enabled
	{
		get => enabled;
		set
		{
			enabled = value;

			if (!enabled)
			{
				UpdateTarget(null);
			}
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (Camera == null || !Enabled)
		{
			return;
		}

		cooldownLeft = Mathf.Max(0.0f, cooldownLeft - (float)delta);

		UpdateTarget(RaycastFromCrosshair());

		if (ShouldStrike())
		{
			Strike();
		}
	}

	private DestructibleBlock RaycastFromCrosshair()
	{
		if (!CrosshairRay.Cast(Camera, Range, TargetLayers, ExcludedBody, out CrosshairRay.Hit hit))
		{
			return null;
		}

		return hit.Collider as DestructibleBlock;
	}

	private void UpdateTarget(DestructibleBlock next)
	{
		if (next == Target)
		{
			return;
		}

		if (Target != null)
		{
			target.SetHighlighted(false);
			EmitSignal(SignalName.TargetLost);
		}

		target = next;

		if (target != null)
		{
			target.SetHighlighted(true);
			EmitSignal(SignalName.TargetAcquired, target);
		}
	}

	private bool ShouldStrike()
	{
		return Target != null
			&& cooldownLeft <= 0.0f
			&& CrosshairRay.PlayerHasControl()
			&& Input.IsActionPressed("mine");
	}

	private void Strike()
	{
		cooldownLeft = HitCooldown;

		DestructibleBlock block = target;
		int damage = Equipment?.MiningDamage ?? 1;

		if (!block.TakeDamage(damage, Tool))
		{
			EmitSignal(SignalName.HitRejected, block);
			return;
		}

		if (block.Health <= 0)
		{
			CollectMineral(block.Mineral);
			EmitSignal(SignalName.BlockDestroyed, (int)block.Mineral);

			// El bloque ya se liberó: se suelta ya para que el HUD no muestre un nodo muerto.
			target = null;
			EmitSignal(SignalName.TargetLost);
		}
	}

	private static void CollectMineral(MineralType mineral)
	{
		string itemId = ItemCatalog.IdOf(mineral);

		if (WorldState.Instance != null && !WorldState.Instance.TryAddItem(itemId))
		{
			GameEvents.PostMessage($"Mochila llena: se perdió 1 {ItemCatalog.DisplayName(itemId)}");
		}
	}
}
