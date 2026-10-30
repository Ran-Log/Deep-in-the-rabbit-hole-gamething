using Godot;

/// <summary>
/// Cámara en tercera persona sobre el hombro: mouse para girar, rueda para zoom,
/// R para cambiar de hombro. Se acerca sola si una pared la tapa (túneles angostos).
/// </summary>
public partial class CameraRig : Node3D
{
	[ExportCategory("CameraRig")]
	[ExportGroup("Mouse")]
	[Export] public float MouseSensitivity = 0.01f;
	[Export] public float MinPitch = -80.0f;
	[Export] public float MaxPitch = 80.0f;

	[ExportGroup("Posición")]
	// Positivo = hombro derecho.
	[Export] public float ShoulderOffset = 0.84f;
	[Export] public float HeightOffset = -0.25f;
	// Más alto = la cámara llega más brusco a su nueva posición.
	[Export] public float SmoothSpeed = 10.0f;

	[ExportGroup("Zoom")]
	[Export] public float Distance = 2.0f;
	[Export] public float MinDistance = 0.8f;
	[Export] public float MaxDistance = 7.0f;
	[Export] public float ZoomStep = 0.5f;

	[ExportGroup("Colisión")]
	// Separación mínima con la pared que la tapa.
	[Export] public float CollisionMargin = 0.25f;
	[Export(PropertyHint.Layers3DPhysics)] public uint CollisionMask = 1;

	private float pitch = 0.0f;
	private float side = 1.0f;
	private float currentSide = 1.0f;
	private float targetDistance;
	private float currentDistance;

	private Camera3D camera;

	public override void _Ready()
	{
		camera = GetNode<Camera3D>("Camera3D");

		targetDistance = Distance;
		currentDistance = Distance;

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

		if (@event.IsActionPressed("zoom_in"))
		{
			targetDistance = Mathf.Clamp(targetDistance - ZoomStep, MinDistance, MaxDistance);
		}
		else if (@event.IsActionPressed("zoom_out"))
		{
			targetDistance = Mathf.Clamp(targetDistance + ZoomStep, MinDistance, MaxDistance);
		}
		else if (@event.IsActionPressed("swap_shoulder"))
		{
			side = -side;
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

	public override void _Process(double delta)
	{
		// Interpolación exponencial: igual de suave a cualquier framerate.
		float t = 1.0f - Mathf.Exp(-SmoothSpeed * (float)delta);

		currentSide = Mathf.Lerp(currentSide, side, t);
		currentDistance = Mathf.Lerp(currentDistance, targetDistance, t);

		PlaceCamera();
	}

	/// <summary>Dirección hacia adelante en el plano (para caminar).</summary>
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

	/// <summary>Dirección hacia donde mira la cámara, CON inclinación (para volar en modo debug).</summary>
	public Vector3 GetLookDirection()
	{
		return -camera.GlobalTransform.Basis.Z;
	}

	// Orbita alrededor del jugador según el pitch, y se acerca si una pared se interpone.
	private void PlaceCamera()
	{
		Vector3 pivotLocal = new Vector3(0.0f, HeightOffset, 0.0f);
		Vector3 offset = new Basis(Vector3.Right, pitch) * new Vector3(ShoulderOffset * currentSide, 0.0f, currentDistance);
		Vector3 desiredLocal = pivotLocal + offset;

		Vector3 from = ToGlobal(pivotLocal);
		Vector3 to = ToGlobal(desiredLocal);

		var query = PhysicsRayQueryParameters3D.Create(from, to, CollisionMask);

		if (GetParent() is CollisionObject3D body)
		{
			query.Exclude = new Godot.Collections.Array<Rid> { body.GetRid() };
		}

		Godot.Collections.Dictionary hit = GetWorld3D().DirectSpaceState.IntersectRay(query);

		if (hit.Count > 0)
		{
			Vector3 hitPoint = hit["position"].AsVector3();
			float safeLength = Mathf.Max(0.0f, from.DistanceTo(hitPoint) - CollisionMargin);

			desiredLocal = pivotLocal + offset.Normalized() * safeLength;
		}

		camera.Position = desiredLocal;
	}
}
