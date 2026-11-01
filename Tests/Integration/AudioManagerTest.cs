namespace Tests.Integration;

using System.Threading.Tasks;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class AudioManagerTest
{
	[TestCase]
	public void ElAutoloadTieneTodosLosSonidosAsignados()
	{
		AudioManager audio = AudioManager.Instance;

		AssertThat(audio).IsNotNull();

		AudioStream[] sounds =
		{
			audio.MineHit, audio.BlockBreak, audio.Pickup, audio.HitRejected,
			audio.EnemyHit, audio.EnemyDeath, audio.PlayerHurt, audio.PlayerDeath,
			audio.ChestOpen, audio.NoteOpen, audio.Eat, audio.Upgrade, audio.Build,
			audio.NightFalls, audio.Explosion, audio.Victory, audio.UiClick, audio.DayAmbience, audio.NightAmbience,
		};

		foreach (AudioStream sound in sounds)
		{
			AssertThat(sound).IsNotNull();
		}
	}

	[TestCase]
	public async Task UnBotonQueEntraAlArbolSuenaAlPresionarlo()
	{
		var button = new Button();
		ISceneRunner runner = ISceneRunner.Load(button, autoFree: true);
		await runner.AwaitIdleFrame();

		button.EmitSignal(BaseButton.SignalName.Pressed);

		AssertThat(AudioManager.Instance.LastPlayed).IsSame(AudioManager.Instance.UiClick);
	}

	[TestCase]
	public async Task GolpearAUnEnemigoYMatarloSuena()
	{
		ISceneRunner runner = ISceneRunner.Load("res://Enemies/Zombie.tscn", autoFree: false);
		await runner.AwaitIdleFrame();

		var enemy = (Enemy)runner.Scene();
		enemy.TakeHit(999, enemy.GlobalPosition + Vector3.Forward);

		AssertThat(AudioManager.Instance.LastPlayed).IsSame(AudioManager.Instance.EnemyDeath);

		await runner.SimulateFrames(2);
	}
}
