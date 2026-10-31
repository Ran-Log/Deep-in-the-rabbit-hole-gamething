using Godot;

/// <summary>
/// Menú principal: la primera escena del juego. Continuar (si hay partida guardada),
/// Nueva partida o Salir.
/// </summary>
public partial class MainMenu : Control
{
	[ExportCategory("MainMenu")]
	[ExportGroup("Nodos de UI")]
	[Export] public Button ContinueButton;
	[Export] public Button NewGameButton;
	[Export] public Button QuitButton;

	[ExportGroup("Escenas")]
	[Export(PropertyHint.File, "*.tscn")] public string GameScene = "res://main.tscn";

	public override void _Ready()
	{
		if (ContinueButton == null || NewGameButton == null || QuitButton == null)
		{
			GD.PushError($"{Name}: faltan botones por asignar.");
			return;
		}

		// Si se llega desde el juego, puede venir con el árbol pausado y el mouse capturado.
		GetTree().Paused = false;
		Input.MouseMode = Input.MouseModeEnum.Visible;

		ContinueButton.Disabled = !(WorldState.Instance?.HasSaveFile ?? false);

		ContinueButton.Pressed += OnContinuePressed;
		NewGameButton.Pressed += OnNewGamePressed;
		QuitButton.Pressed += OnQuitPressed;

		// Para poder usarlo también con el teclado.
		(ContinueButton.Disabled ? NewGameButton : ContinueButton).GrabFocus();
	}

	public override void _ExitTree()
	{
		if (ContinueButton != null)
		{
			ContinueButton.Pressed -= OnContinuePressed;
			NewGameButton.Pressed -= OnNewGamePressed;
			QuitButton.Pressed -= OnQuitPressed;
		}
	}

	private void OnContinuePressed()
	{
		// WorldState ya cargó la partida guardada al iniciar (es autoload).
		GetTree().ChangeSceneToFile(GameScene);
	}

	private void OnNewGamePressed()
	{
		WorldState.Instance?.StartNewGame();
		GetTree().ChangeSceneToFile(GameScene);
	}

	private void OnQuitPressed()
	{
		GetTree().Quit();
	}
}
