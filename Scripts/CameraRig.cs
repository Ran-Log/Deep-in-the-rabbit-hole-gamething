using Godot;

public partial class CameraRig : Node3D
{
	[Export]
	public float MouseSensitivity = 0.01f;

	// Límites de la cámara
	[Export]
	public float MinPitch = -80.0f;

	[Export]
	public float MaxPitch = 80.0f;

	private float pitch = 0.0f;

	private Camera3D camera;

	public override void _Ready()
	{
		camera = GetNode<Camera3D>("Camera3D");

		// Al empezar, capturar el mouse
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _Input(InputEvent @event)
	{
		// ESC libera el mouse
		if (@event.IsActionPressed("ui_cancel"))
		{
			Input.MouseMode = Input.MouseModeEnum.Visible;
			return;
		}

		// Si el mouse está libre, clic izquierdo vuelve a capturarlo
		if (@event is InputEventMouseButton mouseButton)
		{
			if (
				mouseButton.ButtonIndex == MouseButton.Left &&
				mouseButton.Pressed &&
				Input.MouseMode != Input.MouseModeEnum.Captured
			)
			{
				Input.MouseMode = Input.MouseModeEnum.Captured;
				return;
			}
		}

		// No mover cámara si el mouse está libre
		if (Input.MouseMode != Input.MouseModeEnum.Captured)
		{
			return;
		}

		// Movimiento de cámara
		if (@event is InputEventMouseMotion mouseMotion)
		{
			// Izquierda / derecha
			RotateY(
				-mouseMotion.Relative.X * MouseSensitivity
			);

			// Arriba / abajo
			pitch -= mouseMotion.Relative.Y * MouseSensitivity;

			// Limitar rotación vertical
			pitch = Mathf.Clamp(
				pitch,
				Mathf.DegToRad(MinPitch),
				Mathf.DegToRad(MaxPitch)
			);

			camera.Rotation = new Vector3(
				pitch,
				0,
				0
			);
		}
	}

	public Vector3 GetForwardDirection()
	{
		Vector3 forward = -GlobalTransform.Basis.Z;

		// Ignorar inclinación vertical
		forward.Y = 0;

		return forward.Normalized();
	}

	public Vector3 GetRightDirection()
	{
		Vector3 right = GlobalTransform.Basis.X;

		right.Y = 0;

		return right.Normalized();
	}
}
