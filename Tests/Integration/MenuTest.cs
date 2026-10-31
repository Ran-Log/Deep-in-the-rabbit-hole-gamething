namespace Tests.Integration;

using System.Threading.Tasks;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

// Los botones que cambian de escena o borran la partida no se presionan acá:
// romperían la partida guardada de quien corre los tests. Se prueban a mano.
[TestSuite]
[RequireGodotRuntime]
public class MenuTest
{
	[TestCase]
	public async Task ElMenuPrincipalTieneSusTresBotones()
	{
		ISceneRunner runner = ISceneRunner.Load("res://UI/MainMenu.tscn", autoFree: true);
		await runner.AwaitIdleFrame();

		var menu = (MainMenu)runner.Scene();

		AssertThat(menu.ContinueButton).IsNotNull();
		AssertThat(menu.NewGameButton.Text).IsEqual("Nueva partida");
		AssertThat(menu.QuitButton.Text).IsEqual("Salir");
	}

	// Se llama a HandleCancel (lo que hace Esc) directamente: la simulación de teclas
	// de GdUnit4 no llega a _UnhandledInput en modo headless.
	[TestCase]
	public async Task EscPausaYDespausa()
	{
		ISceneRunner runner = ISceneRunner.Load("res://UI/PauseMenu.tscn", autoFree: true);
		await runner.AwaitIdleFrame();

		var pause = (PauseMenu)runner.Scene();
		SceneTree tree = pause.GetTree();

		pause.HandleCancel();

		AssertThat(pause.IsOpen).IsTrue();
		AssertThat(tree.Paused).IsTrue();

		pause.HandleCancel();

		AssertThat(pause.IsOpen).IsFalse();
		AssertThat(tree.Paused).IsFalse();
	}

	[TestCase]
	public async Task NoSeAbreSobreOtraPantallaQueYaPauso()
	{
		ISceneRunner runner = ISceneRunner.Load("res://UI/PauseMenu.tscn", autoFree: true);
		await runner.AwaitIdleFrame();

		var pause = (PauseMenu)runner.Scene();
		SceneTree tree = pause.GetTree();

		// Simula el game over, que pausa el juego por su cuenta.
		tree.Paused = true;

		pause.HandleCancel();

		AssertThat(pause.IsOpen).IsFalse();

		tree.Paused = false;
	}
}
