using Godot;

/// <summary>
/// Toda la lógica de minar, separada del movimiento. Lanza un rayo por el
/// centro de la pantalla, mantiene cuál es el bloque apuntado y lo golpea
/// mientras se mantenga presionada la acción "mine".
///
/// No sabe nada del HUD: avisa hacia arriba con señales.
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

	[Export]
	public Camera3D Camera;

	/// <summary>Cuerpo a ignorar en el rayo (el propio jugador).</summary>
	[Export]
	public CollisionObject3D ExcludedBody;

	[Export]
	public float Range = 6.0f;

	[Export]
	public int Damage = 1;

	/// <summary>Segundos entre golpe y golpe mientras se mantiene presionado.</summary>
	[Export]
	public float HitCooldown = 0.3f;

	[Export]
	public ToolTier Tool = ToolTier.Basic;

	[Export(PropertyHint.Layers3DPhysics)]
	public uint TargetLayers = 1;

	private DestructibleBlock target;
	private float cooldownLeft;

	/// <summary>Bloque apuntado ahora mismo, o null.</summary>
	public DestructibleBlock Target => GodotObject.IsInstanceValid(target) ? target : null;

	public override void _PhysicsProcess(double delta)
	{
		if (Camera == null)
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
		// Se proyecta desde el centro exacto del viewport, que es donde está
		// dibujado el crosshair. Así el rayo y la mira no se pueden desfasar,
		// aunque la cámara tenga offset respecto del jugador.
		Vector2 screenCenter = GetViewport().GetVisibleRect().Size * 0.5f;

		Vector3 from = Camera.ProjectRayOrigin(screenCenter);
		Vector3 to = from + Camera.ProjectRayNormal(screenCenter) * Range;

		PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(from, to);

		query.CollisionMask = TargetLayers;

		if (ExcludedBody != null)
		{
			query.Exclude = new Godot.Collections.Array<Rid> { ExcludedBody.GetRid() };
		}

		Godot.Collections.Dictionary hit = GetWorld3D().DirectSpaceState.IntersectRay(query);

		if (hit.Count == 0)
		{
			return null;
		}

		return hit["collider"].As<GodotObject>() as DestructibleBlock;
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
			&& Input.MouseMode == Input.MouseModeEnum.Captured
			&& Input.IsActionPressed("mine");
	}

	private void Strike()
	{
		cooldownLeft = HitCooldown;

		DestructibleBlock block = target;

		if (!block.TakeDamage(Damage, Tool))
		{
			EmitSignal(SignalName.HitRejected, block);
			return;
		}

		if (block.Health <= 0)
		{
			EmitSignal(SignalName.BlockDestroyed, (int)block.Mineral);

			// El bloque ya se liberó: soltar el target ahora evita que el HUD
			// siga mostrando datos de un nodo muerto durante un frame.
			target = null;
			EmitSignal(SignalName.TargetLost);
		}
	}
}
