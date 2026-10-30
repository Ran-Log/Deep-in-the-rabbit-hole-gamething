using Godot;

/// <summary>
/// Raycast desde el centro de la pantalla (donde está el crosshair). Lo usan
/// minar, pelear, interactuar y construir, para apuntar a lo mismo que ve el jugador.
/// </summary>
public static class CrosshairRay
{
	public readonly struct Hit
	{
		public readonly GodotObject Collider;
		public readonly Vector3 Position;
		public readonly Vector3 Normal;

		public Hit(GodotObject collider, Vector3 position, Vector3 normal)
		{
			Collider = collider;
			Position = position;
			Normal = normal;
		}
	}

	/// <summary>Devuelve false si no hay nada dentro de <paramref name="range"/> metros.</summary>
	public static bool Cast(Camera3D camera, float range, uint collisionMask, CollisionObject3D excluded, out Hit hit)
	{
		hit = default;

		if (camera == null || !camera.IsInsideTree())
		{
			return false;
		}

		Vector2 screenCenter = camera.GetViewport().GetVisibleRect().Size * 0.5f;

		Vector3 from = camera.ProjectRayOrigin(screenCenter);
		Vector3 to = from + camera.ProjectRayNormal(screenCenter) * range;

		var query = PhysicsRayQueryParameters3D.Create(from, to);
		query.CollisionMask = collisionMask;

		if (excluded != null)
		{
			query.Exclude = new Godot.Collections.Array<Rid> { excluded.GetRid() };
		}

		Godot.Collections.Dictionary result = camera.GetWorld3D().DirectSpaceState.IntersectRay(query);

		if (result.Count == 0)
		{
			return false;
		}

		hit = new Hit(
			result["collider"].As<GodotObject>(),
			result["position"].AsVector3(),
			result["normal"].AsVector3()
		);

		return true;
	}

	/// <summary>True si el jugador está "en el juego" (mouse capturado), no en un menú.</summary>
	public static bool PlayerHasControl()
	{
		return Input.MouseMode == Input.MouseModeEnum.Captured;
	}
}
