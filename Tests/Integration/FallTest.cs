namespace Tests.Integration;

using System.Threading.Tasks;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

// Feature 1.8: daño por caída y salto de un bloque.
[TestSuite]
[RequireGodotRuntime]
public class FallTest
{
	[TestCase]
	public async Task AterrizarDesdeMuyAltoQuitaVida()
	{
		var health = new HealthComponent();
		var player = new Player { Health = health };
		player.AddChild(health);

		ISceneRunner runner = ISceneRunner.Load(player, autoFree: true);
		await runner.AwaitIdleFrame();

		player.Land(4.3f);
		AssertThat(health.Current).IsEqual(100);

		player.Land(8.0f);
		AssertThat(health.Current).IsEqual(70);
	}

	[TestCase]
	public void ElSaltoAlcanzaParaSubirUnBloque()
	{
		var player = GD.Load<PackedScene>("res://Player/player.tscn").Instantiate<Player>();
		float cellSize = BlockGrid.Instance.CellSize;

		// Altura máxima de un tiro vertical: v² / (2g).
		float jumpHeight = player.JumpVelocity * player.JumpVelocity / (2.0f * player.Gravity);

		AssertThat(jumpHeight).IsGreater(cellSize);
		player.Free();
	}
}
