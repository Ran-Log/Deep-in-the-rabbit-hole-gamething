using Godot;

/// <summary>
/// Borde invisible del mapa: una caja de colisión hueca, para no salir del mundo
/// minando hacia afuera. Sin mesh, solo colisión. (Hoy no se usa; ver el plan.)
/// </summary>
public partial class BoundaryWall : Node3D
{
	[ExportCategory("BoundaryWall")]
	[ExportGroup("Tamaño")]
	// Medidas interiores del área jugable, en metros.
	[Export] public Vector3 InnerSize = new Vector3(200.0f, 60.0f, 200.0f);
	[Export] public float Thickness = 4.0f;

	[ExportGroup("Caras")]
	[Export] public bool IncludeCeiling = true;
	[Export] public bool IncludeFloor = true;

	[ExportGroup("Colisión")]
	[Export(PropertyHint.Layers3DPhysics)] public uint CollisionLayer3D = 1;

	public override void _Ready()
	{
		Vector3 half = InnerSize * 0.5f;
		float offset = Thickness * 0.5f;

		BuildSide("BoundaryLeft", new Vector3(-half.X - offset, 0.0f, 0.0f), SideSize(Vector3.Axis.X));
		BuildSide("BoundaryRight", new Vector3(half.X + offset, 0.0f, 0.0f), SideSize(Vector3.Axis.X));
		BuildSide("BoundaryBack", new Vector3(0.0f, 0.0f, -half.Z - offset), SideSize(Vector3.Axis.Z));
		BuildSide("BoundaryFront", new Vector3(0.0f, 0.0f, half.Z + offset), SideSize(Vector3.Axis.Z));

		if (IncludeCeiling)
		{
			BuildSide("BoundaryCeiling", new Vector3(0.0f, half.Y + offset, 0.0f), SideSize(Vector3.Axis.Y));
		}

		if (IncludeFloor)
		{
			BuildSide("BoundaryFloor", new Vector3(0.0f, -half.Y - offset, 0.0f), SideSize(Vector3.Axis.Y));
		}
	}

	// Fino en el eje que tapa y más grande en los otros, para sellar las esquinas.
	private Vector3 SideSize(Vector3.Axis normal)
	{
		Vector3 outer = InnerSize + Vector3.One * (Thickness * 2.0f);

		return normal switch
		{
			Vector3.Axis.X => new Vector3(Thickness, outer.Y, outer.Z),
			Vector3.Axis.Y => new Vector3(outer.X, Thickness, outer.Z),
			_ => new Vector3(outer.X, outer.Y, Thickness)
		};
	}

	private void BuildSide(string name, Vector3 position, Vector3 size)
	{
		var body = new StaticBody3D
		{
			Name = name,
			Position = position,
			CollisionLayer = CollisionLayer3D
		};

		var collision = new CollisionShape3D
		{
			Name = "CollisionShape3D",
			Shape = new BoxShape3D { Size = size }
		};

		body.AddChild(collision);
		AddChild(body);
	}
}
