namespace Tests.Integration;

using System.Threading.Tasks;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class EnemyTest
{
	private static readonly string[] EnemyScenes =
	{
		"res://Enemies/Zombie.tscn",
		"res://Enemies/Mole.tscn",
		"res://Enemies/Spider.tscn",
		"res://Enemies/Rabbit.tscn",
		"res://Enemies/Ant.tscn",
	};

	[TestCase]
	public async Task CadaEnemigoCargaConSuComportamiento()
	{
		foreach (string path in EnemyScenes)
		{
			ISceneRunner runner = ISceneRunner.Load(path, autoFree: true);
			await runner.AwaitIdleFrame();

			var enemy = runner.Scene() as Enemy;

			AssertThat(enemy).IsNotNull();
			AssertThat(enemy.Behavior).IsNotNull();
			AssertThat(enemy.Health).IsNotNull();
			AssertThat(enemy.IsInGroup(Enemy.Group)).IsTrue();
		}
	}

	[TestCase]
	public async Task UnGolpeBajaLaVida()
	{
		ISceneRunner runner = ISceneRunner.Load("res://Enemies/Zombie.tscn", autoFree: true);
		await runner.AwaitIdleFrame();

		var enemy = (Enemy)runner.Scene();
		int before = enemy.Health.Current;

		enemy.TakeHit(2, enemy.GlobalPosition + Vector3.Forward);

		AssertThat(enemy.Health.Current).IsEqual(before - 2);
	}

	[TestCase]
	public async Task UnGolpeMortalLoElimina()
	{
		ISceneRunner runner = ISceneRunner.Load("res://Enemies/Rabbit.tscn", autoFree: false);
		await runner.AwaitIdleFrame();

		var enemy = (Enemy)runner.Scene();
		enemy.TakeHit(99, enemy.GlobalPosition + Vector3.Forward);
		await runner.SimulateFrames(2);

		AssertThat(GodotObject.IsInstanceValid(enemy)).IsFalse();
	}

	[TestCase]
	public async Task ElConejoHuyeDespuesDeUnGolpe()
	{
		ISceneRunner runner = ISceneRunner.Load("res://Enemies/Rabbit.tscn", autoFree: true);
		await runner.AwaitIdleFrame();

		var enemy = (Enemy)runner.Scene();
		var rabbit = (RabbitBehavior)enemy.Behavior;

		AssertThat(rabbit.IsFleeing).IsFalse();

		enemy.TakeHit(1, enemy.GlobalPosition + Vector3.Forward);

		AssertThat(rabbit.IsFleeing).IsTrue();
	}

	[TestCase]
	public async Task ElConejoAguantaUnGolpeDelPicoBasico()
	{
		// Si muriera de un golpe, nunca alcanzaría a huir.
		ISceneRunner runner = ISceneRunner.Load("res://Enemies/Rabbit.tscn", autoFree: true);
		await runner.AwaitIdleFrame();

		var enemy = (Enemy)runner.Scene();
		int basicCombatDamage = MineralRules.ToolDamage(ToolTier.Basic) + 1;

		enemy.TakeHit(basicCombatDamage, enemy.GlobalPosition + Vector3.Forward);

		AssertThat(enemy.Health.IsDead).IsFalse();
	}

	[TestCase]
	public async Task GolpearAUnConejoAsustaALosCercanos()
	{
		ISceneRunner runner = ISceneRunner.Load("res://Enemies/Rabbit.tscn", autoFree: true);
		var hit = (Enemy)runner.Scene();

		var nearby = GD.Load<PackedScene>("res://Enemies/Rabbit.tscn").Instantiate<Enemy>();
		nearby.Position = hit.Position + new Vector3(2.0f, 0.0f, 0.0f);
		hit.GetParent().AddChild(nearby);
		await runner.AwaitIdleFrame();

		hit.TakeHit(1, hit.GlobalPosition + Vector3.Forward);

		AssertThat(((RabbitBehavior)nearby.Behavior).IsFleeing).IsTrue();

		nearby.QueueFree();
	}

	[TestCase]
	public void LosConejosAparecenEnGrupo()
	{
		var rabbit = GD.Load<PackedScene>("res://Enemies/Rabbit.tscn").Instantiate<Enemy>();

		AssertThat(rabbit.SpawnGroupSize).IsGreater(1);

		rabbit.Free();
	}

	[TestCase]
	public async Task DestruirElNidoLoMarcaComoDestruido()
	{
		var coord = new Vector2I(-77, 77);
		var nest = GD.Load<PackedScene>("res://Enemies/AntNest.tscn").Instantiate<AntNest>();
		nest.RoomCoord = coord;

		ISceneRunner runner = ISceneRunner.Load(nest, autoFree: false);
		await runner.AwaitIdleFrame();

		AssertThat(WorldState.Instance.IsNestDestroyed(coord)).IsFalse();

		nest.TakeHit(999, Vector3.Zero);
		await runner.SimulateFrames(2);

		AssertThat(WorldState.Instance.IsNestDestroyed(coord)).IsTrue();
	}
}
