using Godot;

/// <summary>
/// Gas de las minas (GDD: "la iluminación hará que explote"). A oscuras solo envenena;
/// si entra la lámpara del casco prendida o hay una antorcha encendida cerca, explota:
/// daña a todos en el área, rompe los bloques cercanos y no vuelve más.
/// </summary>
public partial class GasZone : Area3D
{
	[Signal]
	public delegate void ExplodedEventHandler();

	[ExportCategory("GasZone")]
	[ExportGroup("Referencias")]
	[Export] public MeshInstance3D Fog;
	[Export] public PackedScene ExplosionScene;

	[ExportGroup("Gas")]
	[Export] public int DamagePerSecond = 8;
	[Export] public Vector3 Size = new Vector3(4.0f, 3.0f, 4.0f);

	[ExportGroup("Explosión")]
	// Daño dentro del gas; baja hasta 0 a BlastMargin metros del borde.
	[Export] public int ExplosionDamage = 70;
	[Export] public float BlastMargin = 5.0f;
	// Los bloques a menos de esto (metros) del punto donde se encendió se rompen.
	[Export] public float BreakRadius = 4.0f;
	// Una antorcha encendida a menos de esto del borde lo hace explotar.
	[Export] public float TorchTriggerDistance = 2.0f;
	// Avisa al acercarse con la lámpara prendida.
	[Export] public float WarnDistance = 4.0f;

	// Sala a la que pertenece, para recordar que ya explotó. La pone Room.
	public Vector2I RoomCoord { get; set; }

	private Player playerInside;
	private float damageAccumulator;
	private float pulseTime;
	private StandardMaterial3D fogMaterial;
	private bool exploded;
	private bool warned;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;

		SetupCollision();
		SetupVisual();
	}

	public override void _ExitTree()
	{
		BodyEntered -= OnBodyEntered;
		BodyExited -= OnBodyExited;
	}

	public override void _PhysicsProcess(double delta)
	{
		AnimateFog((float)delta);
		CheckIgnition();

		if (!exploded)
		{
			ApplyPoison((float)delta);
		}
	}

	// Distancia de un punto al borde del gas (0 si está adentro).
	public float DistanceTo(Vector3 globalPoint)
	{
		Vector3 local = ToLocal(globalPoint).Abs() - Size * 0.5f;
		return local.Max(Vector3.Zero).Length();
	}

	public int DamageAt(Vector3 globalPoint)
	{
		float distance = DistanceTo(globalPoint);

		if (distance >= BlastMargin)
		{
			return 0;
		}

		return Mathf.RoundToInt(ExplosionDamage * (1.0f - distance / BlastMargin));
	}

	public void Explode(Vector3 ignitionPoint)
	{
		if (exploded)
		{
			return;
		}

		exploded = true;
		WorldState.Instance?.MarkGasExploded(RoomCoord);

		DamageEveryoneNear();
		BreakBlocksAround(ignitionPoint);
		SpawnExplosionEffect(ignitionPoint);

		GameEvents.PostMessage("¡El gas explotó!");
		EmitSignal(SignalName.Exploded);
		QueueFree();
	}

	// La luz enciende el gas: la lámpara del jugador adentro o una antorcha cerca.
	private void CheckIgnition()
	{
		if (exploded)
		{
			return;
		}

		if (playerInside?.Lamp != null && playerInside.Lamp.IsOn)
		{
			Explode(playerInside.GlobalPosition);
			return;
		}

		foreach (Node node in GetTree().GetNodesInGroup(Torch.Group))
		{
			if (node is Torch torch && torch.IsLit && DistanceTo(torch.GlobalPosition) <= TorchTriggerDistance)
			{
				Explode(torch.GlobalPosition);
				return;
			}
		}

		WarnIfApproachingWithLight();
	}

	private void WarnIfApproachingWithLight()
	{
		if (warned || GetTree().GetFirstNodeInGroup("player") is not Player player)
		{
			return;
		}

		if (player.Lamp != null && player.Lamp.IsOn && DistanceTo(player.GlobalPosition) <= WarnDistance)
		{
			warned = true;
			GameEvents.PostMessage("Huele a gas... apaga la lámpara [L] antes de entrar");
		}
	}

	private void DamageEveryoneNear()
	{
		if (GetTree().GetFirstNodeInGroup("player") is Player player)
		{
			player.Health?.TakeDamage(DamageAt(player.GlobalPosition));
		}

		foreach (Node node in GetTree().GetNodesInGroup("enemies"))
		{
			if (node is Node3D body && node is IDamageable target && DamageAt(body.GlobalPosition) > 0)
			{
				target.TakeHit(DamageAt(body.GlobalPosition), GlobalPosition);
			}
		}
	}

	// Rompe con la herramienta más fuerte: la roca madre (no destructible) queda.
	private void BreakBlocksAround(Vector3 point)
	{
		BlockGrid grid = BlockGrid.Instance;

		if (grid == null)
		{
			return;
		}

		Vector3I center = grid.WorldToGrid(point);
		int reach = Mathf.CeilToInt(BreakRadius / grid.CellSize);

		for (int x = -reach; x <= reach; x++)
		{
			for (int y = -reach; y <= reach; y++)
			{
				for (int z = -reach; z <= reach; z++)
				{
					Vector3I cell = center + new Vector3I(x, y, z);

					if (grid.GridToWorld(cell).DistanceTo(point) <= BreakRadius)
					{
						grid.GetBlock(cell)?.TakeDamage(int.MaxValue, ToolTier.Advanced);
					}
				}
			}
		}
	}

	// El efecto va en la sala (no en el gas), porque el gas se borra ahora.
	private void SpawnExplosionEffect(Vector3 point)
	{
		if (ExplosionScene == null || GetParent() == null)
		{
			return;
		}

		var effect = ExplosionScene.Instantiate<Node3D>();
		GetParent().AddChild(effect);
		effect.GlobalPosition = point;
	}

	private void SetupCollision()
	{
		var shape = new CollisionShape3D
		{
			Shape = new BoxShape3D { Size = Size },
		};

		AddChild(shape);
	}

	private void SetupVisual()
	{
		if (Fog == null)
		{
			return;
		}

		Fog.Mesh = new BoxMesh { Size = Size };

		fogMaterial = new StandardMaterial3D
		{
			AlbedoColor = new Color(0.35f, 0.85f, 0.25f, 0.3f),
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			CullMode = BaseMaterial3D.CullModeEnum.Disabled,
		};

		Fog.MaterialOverride = fogMaterial;
	}

	// Sube y baja la opacidad para que la niebla no sea un bloque fijo.
	private void AnimateFog(float delta)
	{
		if (fogMaterial == null)
		{
			return;
		}

		pulseTime += delta;

		Color color = fogMaterial.AlbedoColor;
		color.A = 0.30f + (0.10f * Mathf.Sin(pulseTime * 1.5f));
		fogMaterial.AlbedoColor = color;
	}

	private void ApplyPoison(float delta)
	{
		if (playerInside?.Health == null)
		{
			return;
		}

		damageAccumulator += DamagePerSecond * delta;

		if (damageAccumulator < 1.0f)
		{
			return;
		}

		int wholeDamage = (int)damageAccumulator;
		damageAccumulator -= wholeDamage;
		playerInside.Health.TakeDamage(wholeDamage);
	}

	private void OnBodyEntered(Node3D body)
	{
		if (body is Player player)
		{
			playerInside = player;
		}
	}

	private void OnBodyExited(Node3D body)
	{
		if (body is Player)
		{
			playerInside = null;
			damageAccumulator = 0.0f;
		}
	}
}
