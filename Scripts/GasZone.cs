using Godot;

/// <summary>
/// Zona de gas venenoso (grisú): mientras el jugador esté dentro del
/// Area3D, le hace daño por segundo a través de su PlayerHealth. El tamaño
/// y el visual (niebla verde translúcida con un pulso suave) se arman en
/// código, con los mismos enums fuertemente tipados que usa el highlight de
/// DestructibleBlock (GetHighlightOverlay) — evita valores mágicos escritos
/// a mano en el .tscn que no puedo verificar sin abrir el editor de Godot.
///
/// Detecta al jugador por el grupo "player" (ver Player en main.tscn) y
/// busca un hijo llamado "PlayerHealth" — no depende de Room ni de
/// WorldStreamer para nada más que su propia posición, que se la da quien
/// la instancia (ver Room.SpawnGasZoneIfNeeded).
/// </summary>
public partial class GasZone : Area3D
{
	[Export]
	public int DamagePerSecond = 8;

	[Export]
	public Vector3 Size = new Vector3(4.0f, 3.0f, 4.0f);

	[Export]
	public MeshInstance3D Fog;

	private PlayerHealth playerInside;
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

	/// <summary>Sube y baja la opacidad lentamente, para que la niebla se sienta "viva" en vez de un bloque de color fijo.</summary>
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
			playerInside = characterBody.GetNodeOrNull<PlayerHealth>("PlayerHealth");
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
