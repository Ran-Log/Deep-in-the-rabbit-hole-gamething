using Godot;

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
	public int MiningDamage = 1;

	public override void _PhysicsProcess(double delta)
	{
		// Get movement input
		Vector2 inputDir = Input.GetVector(
			"move_left",
			"move_right",
			"move_up",
			"move_down"
		);
		
		if (
			Input.MouseMode == Input.MouseModeEnum.Captured &&
			Input.IsActionJustPressed("mine")
		)
		{
			TryMine();
		}

		// Get movement directions relative to the camera
		CameraRig cameraRig = GetNode<CameraRig>("CameraRig");

		Vector3 direction =
			cameraRig.GetRightDirection() * inputDir.X +
			cameraRig.GetForwardDirection() * -inputDir.Y;

		direction = direction.Normalized();

		// Running
		float currentSpeed = Speed;

		if (Input.IsActionPressed("run"))
		{
			currentSpeed *= RunMultiplier;
		}

		// Horizontal movement
		if (direction != Vector3.Zero)
		{
			Velocity = new Vector3(
				direction.X * currentSpeed,
				Velocity.Y,
				direction.Z * currentSpeed
			);
		}
		else
		{
			Velocity = new Vector3(
				0,
				Velocity.Y,
				0
			);
		}

		// Jump
		if (Input.IsActionJustPressed("jump") && IsOnFloor())
		{
			Velocity = new Vector3(
				Velocity.X,
				JumpVelocity,
				Velocity.Z
			);
		}

		// Gravity
		if (!IsOnFloor())
		{
			Velocity += Vector3.Down * Gravity * (float)delta;
		}
		else if (Velocity.Y < 0)
		{
			Velocity = new Vector3(
				Velocity.X,
				0,
				Velocity.Z
			);
		}

		MoveAndSlide();
	}
	
	private void TryMine()
	{
		Camera3D camera = GetNode<Camera3D>("CameraRig/Camera3D");

		Vector3 start = camera.GlobalPosition;
		Vector3 end = start + -camera.GlobalTransform.Basis.Z * 5.0f;

		PhysicsRayQueryParameters3D query =
			PhysicsRayQueryParameters3D.Create(start, end);

		query.Exclude = new Godot.Collections.Array<Rid>
		{
			GetRid()
		};

		var result = GetWorld3D().DirectSpaceState.IntersectRay(query);

		if (result.Count > 0)
		{
			Node collider = result["collider"].AsGodotObject() as Node;

			if (collider is DestructibleWall wall)
			{
				wall.TakeDamage(MiningDamage);
			}
		}
	}
}
