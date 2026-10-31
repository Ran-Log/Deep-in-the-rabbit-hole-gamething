using Godot;

/// <summary>
/// Menú de pausa (Esc): detiene el juego y ofrece continuar, guardar y volver al menú,
/// o guardar y salir. Sigue procesando con el juego pausado (ProcessMode Always).
/// </summary>
public partial class PauseMenu : CanvasLayer
{
	[ExportCategory("PauseMenu")]
	[ExportGroup("Nodos de UI")]
	[Export] public Control Panel;
	[Export] public Button ResumeButton;
	[Export] public Button MenuButton;
	[Export] public Button QuitButton;

	[ExportGroup("Escenas")]
	[Export(PropertyHint.File, "*.tscn")] public string MainMenuScene = "res://UI/MainMenu.tscn";

	public bool IsOpen => Panel != null && Panel.Visible;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;

		if (Panel == null || ResumeButton == null || MenuButton == null || QuitButton == null)
		{
			GD.PushError($"{Name}: faltan nodos por asignar.");
			return;
		}

		ResumeButton.Pressed += OnResumePressed;
		MenuButton.Pressed += OnMenuPressed;
		QuitButton.Pressed += OnQuitPressed;

		Panel.Visible = false;
	}

	public override void _ExitTree()
	{
		if (ResumeButton != null)
		{
			ResumeButton.Pressed -= OnResumePressed;
			MenuButton.Pressed -= OnMenuPressed;
			QuitButton.Pressed -= OnQuitPressed;
		}
	}

	// Solo lee el input (patrón de la clase 8: ProcessInput separado de UpdateState).
	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_cancel"))
		{
			HandleCancel();
			GetViewport().SetInputAsHandled();
		}
	}

	// Qué pasa al presionar Esc.
	public void HandleCancel()
	{
		if (IsOpen)
		{
			SetOpen(false);
		}
		else if (!GetTree().Paused)
		{
			// Si ya está pausado es por otra pantalla (ej. game over): no se abre encima.
			SetOpen(true);
		}
	}

	public void SetOpen(bool open)
	{
		Panel.Visible = open;
		GetTree().Paused = open;
		Input.MouseMode = open ? Input.MouseModeEnum.Visible : Input.MouseModeEnum.Captured;

		if (open)
		{
			ResumeButton.GrabFocus();
		}
	}

	private void OnResumePressed()
	{
		SetOpen(false);
	}

	private void OnMenuPressed()
	{
		SaveGame();
		GetTree().Paused = false;
		GetTree().ChangeSceneToFile(MainMenuScene);
	}

	private void OnQuitPressed()
	{
		SaveGame();
		GetTree().Quit();
	}

	private void SaveGame()
	{
		if (GetTree().GetFirstNodeInGroup("player") is Node3D player)
		{
			WorldState.Instance?.Save(player.GlobalPosition);
		}
	}
}
