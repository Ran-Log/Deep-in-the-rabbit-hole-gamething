using System.Collections.Generic;
using Godot;

/// <summary>
/// Comportamientos de movimiento cinemático de la clase 12. Devuelven una dirección
/// horizontal (Y = 0); la velocidad la pone quien la usa (Vf = v * Df).
/// </summary>
public static class Steering
{
	// Seek: Df = N(Pf − Pi). Devuelve cero si ya está a menos de stopDistance.
	public static Vector3 Seek(Vector3 from, Vector3 to, float stopDistance = 0.0f)
	{
		Vector3 toTarget = Flat(to - from);

		if (toTarget.Length() <= Mathf.Max(stopDistance, 0.001f))
		{
			return Vector3.Zero;
		}

		return toTarget.Normalized();
	}

	// Flee: Df = −N(Pf − Pi).
	public static Vector3 Flee(Vector3 from, Vector3 threat)
	{
		Vector3 away = Flat(from - threat);

		return away.LengthSquared() < 0.000001f ? Vector3.Zero : away.Normalized();
	}

	// Wander: Df = Di + un pequeño giro al azar.
	public static Vector3 Wander(Vector3 current, float maxTurnRadians, RandomNumberGenerator rng)
	{
		if (current.LengthSquared() < 0.000001f)
		{
			float angle = rng.RandfRange(0.0f, Mathf.Tau);
			return new Vector3(Mathf.Cos(angle), 0.0f, Mathf.Sin(angle));
		}

		float turn = rng.RandfRange(-maxTurnRadians, maxTurnRadians);
		return Flat(current).Normalized().Rotated(Vector3.Up, turn);
	}

	// Separación (comportamiento de grupo): alejarse de los vecinos; más fuerte mientras más cerca.
	public static Vector3 Separation(Vector3 position, IEnumerable<Vector3> neighbors, float radius)
	{
		Vector3 push = Vector3.Zero;

		foreach (Vector3 neighbor in neighbors)
		{
			Vector3 away = Flat(position - neighbor);
			float distance = away.Length();

			if (distance > 0.001f && distance < radius)
			{
				push += away.Normalized() * (1.0f - distance / radius);
			}
		}

		return push;
	}

	private static Vector3 Flat(Vector3 v)
	{
		return new Vector3(v.X, 0.0f, v.Z);
	}
}
