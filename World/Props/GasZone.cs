using Godot;

/// <summary>
/// Gas venenoso: mientras el jugador está dentro del Area3D, pierde vida por segundo.
/// La niebla verde pulsa suavemente.
/// </summary>
public partial class GasZone : Area3D
{
	[ExportCategory("GasZone")]
	[ExportGroup("Referencias")]
	[Export] public MeshInstance3D Fog;

	[ExportGroup("Gas")]
	[Export] public int DamagePerSecond = 8;
	[Export] public Vector3 Size = new Vector3(4.0f, 3.0f, 4.0f);

	private HealthComponent playerInside;
	private float damageAccumulator;
	private float pulseTime;
	private StandardMaterial3D fogMaterial;

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
		ApplyDamage((float)delta);
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

	private void ApplyDamage(float delta)
	{
		if (playerInside == null)
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
		playerInside.TakeDamage(wholeDamage);
	}

	private void OnBodyEntered(Node3D body)
	{
		if (body is CharacterBody3D characterBody && characterBody.IsInGroup("player"))
		{
			playerInside = characterBody.GetNodeOrNull<HealthComponent>("Health");
		}
	}

	private void OnBodyExited(Node3D body)
	{
		if (body is CharacterBody3D characterBody && characterBody.IsInGroup("player"))
		{
			playerInside = null;
			damageAccumulator = 0.0f;
		}
	}
}
