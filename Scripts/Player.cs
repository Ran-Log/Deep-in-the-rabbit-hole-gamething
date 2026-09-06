using Godot;

/// <summary>
/// Movimiento del jugador. El minado vive en <see cref="MiningComponent"/>.
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

	public override void _Ready()
	{
		if (CameraRig == null)
		{
			GD.PushError($"{Name}: falta asignar CameraRig; el movimiento va a ignorar la cámara.");
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector3 horizontal = ReadHorizontalVelocity();
		float vertical = ReadVerticalVelocity((float)delta);

		Velocity = new Vector3(horizontal.X, vertical, horizontal.Z);

		MoveAndSlide();
	}

	private Vector3 ReadHorizontalVelocity()
	{
		if (CameraRig == null)
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

	private float ReadVerticalVelocity(float delta)
	{
		if (IsOnFloor())
		{
			if (Input.IsActionJustPressed("jump"))
			{
				return JumpVelocity;
			}

			return Mathf.Max(0.0f, Velocity.Y);
		}

		return Velocity.Y - Gravity * delta;
	}
}
