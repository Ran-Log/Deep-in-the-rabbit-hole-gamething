namespace Tests.Integration;

using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

// Las fórmulas de movimiento de la clase 12.
[TestSuite]
[RequireGodotRuntime]
public class SteeringTest
{
	[TestCase]
	public void SeekApuntaAlObjetivoEnHorizontal()
	{
		Vector3 dir = Steering.Seek(Vector3.Zero, new Vector3(10, 5, 0));

		AssertThat(dir).IsEqual(new Vector3(1, 0, 0));
	}

	[TestCase]
	public void SeekSeDetieneCercaDelObjetivo()
	{
		AssertThat(Steering.Seek(Vector3.Zero, new Vector3(1, 0, 0), stopDistance: 1.5f)).IsEqual(Vector3.Zero);
	}

	[TestCase]
	public void FleeVaAlLadoContrario()
	{
		Vector3 dir = Steering.Flee(Vector3.Zero, new Vector3(0, 0, 4));

		AssertThat(dir).IsEqual(new Vector3(0, 0, -1));
	}

	[TestCase]
	public void WanderGiraPocoYMantieneElLargo()
	{
		var rng = new RandomNumberGenerator { Seed = 7 };
		Vector3 current = new Vector3(1, 0, 0);
		Vector3 next = Steering.Wander(current, 0.2f, rng);

		AssertThat(next.Length()).IsBetween(0.99f, 1.01f);
		AssertThat(current.AngleTo(next)).IsLessEqual(0.2001f);
	}

	[TestCase]
	public void SeparationAlejaDeUnVecinoCercano()
	{
		Vector3 push = Steering.Separation(Vector3.Zero, new[] { new Vector3(0.5f, 0, 0) }, radius: 2.0f);

		AssertThat(push.X).IsLess(0.0f);
	}

	[TestCase]
	public void SeparationIgnoraVecinosLejanos()
	{
		Vector3 push = Steering.Separation(Vector3.Zero, new[] { new Vector3(5, 0, 0) }, radius: 2.0f);

		AssertThat(push).IsEqual(Vector3.Zero);
	}
}
