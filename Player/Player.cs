using Godot;

/// <summary>
/// Movimiento del jugador. Lo demás vive en componentes hijos, cada uno con
/// una sola responsabilidad: MiningComponent (minar), CombatComponent
/// (pegarle a enemigos), InteractionComponent (usar objetos con E),
/// BuildComponent (construir defensas), EatComponent (comer),
/// HealthComponent (vida) y PlayerEquipment (nivel del pico).
///
/// Health y Equipment se exponen acá para que los Interactable (cama,
/// cofres, estación) reciban un solo parámetro — el Player — en vez de
/// conocer la estructura interna de la escena del jugador.
/// </summary>
public partial class Player : CharacterBody3D
{
	[Export]
	public float Speed = 5.0f;

	[Export]
	public float RunMultiplier = 1.5f;

	[Export]
	public float JumpVelocity = 6.0f;

	[Export]
	public float Gravity = 20.0f;

	[Export]
	public CameraRig CameraRig;

	[Export]
	public HealthComponent Health;

	[Export]
	public PlayerEquipment Equipment;

	/// <summary>Velocidad de vuelo en modo debug (<see cref="NoClip"/>).</summary>
	[Export]
	public float FlySpeed = 14.0f;

	/// <summary>
	/// Modo debug: vuela hacia donde mira la cámara y atraviesa bloques
	/// (se mueve sin MoveAndSlide, así que no choca con nada). Lo activa
	/// DebugTools; en un build exportado no se puede activar.
	/// </summary>
	public bool NoClip { get; set; }

	public override void _Ready()
	{
		if (CameraRig == null)
		{
			GD.PushError($"{Name}: falta asignar CameraRig; el movimiento va a ignorar la cámara.");
		}

		// WorldState es autoload: ya corrió su _Ready (y por lo tanto ya
		// cargó el save, si había uno) antes que la escena principal.
		if (WorldState.Instance?.LoadedPlayerPosition is Vector3 savedPosition)
		{
			GlobalPosition = savedPosition;
		}
	}

	/// <summary>Tecla "save" (F5 por defecto): guarda semilla, bloques minados e inventario.</summary>
	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("save"))
		{
			WorldState.Instance?.Save(GlobalPosition);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (NoClip)
		{
			Fly((float)delta);
			return;
		}

		Vector3 horizontal = ReadHorizontalVelocity();
		float vertical = ReadVerticalVelocity((float)delta);

		Velocity = new Vector3(horizontal.X, vertical, horizontal.Z);

		MoveAndSlide();
	}

	private Vector3 ReadHorizontalVelocity()
	{
		// Sin control (inventario, nota o menú abiertos) no se camina: el
		// GDD pide que revisar la mochila deje al jugador vulnerable.
		if (CameraRig == null || !CrosshairRay.PlayerHasControl())
		{
			return Vector3.Zero;
		}

		Vector2 inputDir = Input.GetVector(
			"move_left",
			"move_right",
			"move_up",
			"move_down"
		);

		Vector3 direction = (
			CameraRig.GetRightDirection() * inputDir.X +
			CameraRig.GetForwardDirection() * -inputDir.Y
		).Normalized();

		float currentSpeed = Input.IsActionPressed("run")
			? Speed * RunMultiplier
			: Speed;

		return direction * currentSpeed;
	}

	/// <summary>W/S hacia donde mira la cámara, A/D de costado, Espacio sube, Ctrl baja, Shift más rápido.</summary>
	private void Fly(float delta)
	{
		Velocity = Vector3.Zero;

		if (CameraRig == null || !CrosshairRay.PlayerHasControl())
		{
			return;
		}

		Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_up", "move_down");

		Vector3 direction =
			CameraRig.GetRightDirection() * inputDir.X +
			CameraRig.GetLookDirection() * -inputDir.Y;

		if (Input.IsActionPressed("jump"))
		{
			direction += Vector3.Up;
		}

		if (Input.IsKeyPressed(Key.Ctrl))
		{
			direction += Vector3.Down;
		}

		float speed = Input.IsActionPressed("run") ? FlySpeed * 2.5f : FlySpeed;

		GlobalPosition += direction.LimitLength(1.0f) * speed * delta;
	}

	private float ReadVerticalVelocity(float delta)
	{
		if (IsOnFloor())
		{
			if (Input.IsActionJustPressed("jump") && CrosshairRay.PlayerHasControl())
			{
				return JumpVelocity;
			}

			return Mathf.Max(0.0f, Velocity.Y);
		}

		return Velocity.Y - Gravity * delta;
	}
}
