namespace Tests.Integration;

using System.Threading.Tasks;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

// Primera persona (V) y alcance medido desde el jugador.
[TestSuite]
[RequireGodotRuntime]
public class CameraTest
{
	[TestCase]
	public async Task EnPrimeraPersonaLaCamaraVaALosOjosYNoDibujaElCuerpo()
	{
		ISceneRunner runner = ISceneRunner.Load("res://Player/player.tscn", autoFree: true);
		await runner.AwaitIdleFrame();

		var player = (Player)runner.Scene();
		CameraRig rig = player.CameraRig;
		var camera = rig.GetNode<Camera3D>("Camera3D");

		// El modo se recuerda durante la sesión: se parte siempre desde tercera persona.
		if (rig.IsFirstPerson)
		{
			rig.ToggleView();
		}

		try
		{
			await runner.SimulateFrames(60, 16);
			AssertThat(camera.GetCullMaskValue(rig.BodyRenderLayer)).IsTrue();

			rig.ToggleView();
			await runner.SimulateFrames(60, 16);

			AssertThat(rig.IsFirstPerson).IsTrue();
			AssertThat(camera.GetCullMaskValue(rig.BodyRenderLayer)).IsFalse();
			AssertThat(camera.Position.DistanceTo(new Vector3(0, rig.EyeHeight, 0))).IsLess(0.1f);
		}
		finally
		{
			if (rig.IsFirstPerson)
			{
				rig.ToggleView();
			}
		}
	}

	[TestCase]
	public async Task ElAlcanceSeMideDesdeElJugadorYNoDesdeLaCamara()
	{
		// Un "rig" con la cámara 5 m detrás, y una caja 3 m delante del jugador.
		var root = new Node3D { Position = new Vector3(900, 50, 900) };
		var camera = new Camera3D { Position = new Vector3(0, 0, 5) };
		var box = new StaticBody3D { Position = new Vector3(0, 0, -3) };
		box.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = Vector3.One } });
		root.AddChild(camera);
		root.AddChild(box);

		ISceneRunner runner = ISceneRunner.Load(root, autoFree: true);
		await runner.AwaitIdleFrame();
		await runner.AwaitIdleFrame();

		// La cara de la caja está a 2.5 m del jugador.
		AssertThat(CrosshairRay.Cast(camera, 3.0f, 1, null, out _)).IsTrue();
		AssertThat(CrosshairRay.Cast(camera, 2.0f, 1, null, out _)).IsFalse();
	}
}
