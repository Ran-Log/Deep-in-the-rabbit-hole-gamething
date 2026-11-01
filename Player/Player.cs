using Godot;

/// <summary>
/// Movimiento del jugador. Minar, pelear, interactuar, construir y comer
/// viven en componentes hijos (Player/Components).
/// </summary>
public partial class Player : CharacterBody3D
{
	[ExportCategory("Player")]
	[ExportGroup("Referencias")]
	[Export] public CameraRig CameraRig;
	// Expuestos acá para que la cama, los cofres, etc. reciban solo el Player.
	[Export] public HealthComponent Health;
	[Export] public PlayerEquipment Equipment;
	[Export] public NeedComponent Hunger;
	[Export] public NeedComponent Thirst;

	[ExportGroup("Movimiento")]
	[Export] public float Speed = 5.0f;
	[Export] public float RunMultiplier = 1.5f;
	[Export] public float JumpVelocity = 6.0f;
	[Export] public float Gravity = 20.0f;

	[ExportGroup("Debug")]
	[Export] public float FlySpeed = 14.0f;

	// Modo debug (F1): vuela y atraviesa bloques. Solo se activa en builds de debug.
	public bool NoClip { get; set; }

	// Con hambre o sed en 0 no se puede correr.
	public bool CanRun => !(Hunger?.IsEmpty ?? false) && !(Thirst?.IsEmpty ?? false);

	public override void _Ready()
	{
		if (CameraRig == null)
		{
			GD.PushError($"{Name}: falta asignar CameraRig; el movimiento va a ignorar la cámara.");
		}

		// WorldState es autoload, así que ya cargó el save antes que esta escena.
		if (WorldState.Instance?.LoadedPlayerPosition is Vector3 savedPosition)
		{
			GlobalPosition = savedPosition;
		}
	}

	// F5 guarda la partida.
	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("save"))
		{
			WorldState.Instance?.Save(GlobalPosition);
			GameEvents.PostMessage("Partida guardada");
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
		// Con un menú abierto no se camina: el GDD pide que revisar la mochila deje al jugador vulnerable.
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

		float currentSpeed = Input.IsActionPressed("run") && CanRun
			? Speed * RunMultiplier
			: Speed;

		return direction * currentSpeed;
	}

	// Vuelo debug: W/S hacia donde mira la cámara, Espacio sube, Ctrl baja, Shift más rápido.
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
