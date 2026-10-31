using Godot;

/// <summary>
/// Fin de partida: pausa el juego y muestra por qué terminó. Derrota si el jugador
/// o el campamento llegan a 0 (GDD); victoria al bajar por la grieta.
/// "Reintentar" recarga la escena; "Nueva partida" además borra el progreso.
/// </summary>
public partial class GameOverScreen : CanvasLayer
{
	[ExportCategory("GameOverScreen")]
	[ExportGroup("Referencias")]
	[Export] public HealthComponent PlayerHealth;
	[Export] public HealthComponent CampHealth;

	[ExportGroup("Nodos de UI")]
	[Export] public Control Panel;
	[Export] public Label TitleLabel;
	[Export] public Label SubtitleLabel;
	[Export] public Button RestartButton;
	[Export] public Button NewGameButton;

	public override void _Ready()
	{
		// Always: si no, con el juego pausado los botones no responderían.
		ProcessMode = ProcessModeEnum.Always;

		if (PlayerHealth == null)
		{
			GD.PushError($"{Name}: falta asignar PlayerHealth; el Game Over nunca se va a mostrar.");
		}
		else
		{
			PlayerHealth.Died += OnPlayerDied;
		}

		if (CampHealth != null)
		{
			CampHealth.Died += OnCampDestroyed;
		}

		if (GameEvents.Instance != null)
		{
			GameEvents.Instance.LevelCompleted += OnLevelCompleted;
		}

		if (RestartButton != null)
		{
			RestartButton.Pressed += OnRestartPressed;
		}

		if (NewGameButton != null)
		{
			NewGameButton.Pressed += OnNewGamePressed;
		}

		SetPanelVisible(false);
	}

	public override void _ExitTree()
	{
		if (PlayerHealth != null)
		{
			PlayerHealth.Died -= OnPlayerDied;
		}

		if (CampHealth != null)
		{
			CampHealth.Died -= OnCampDestroyed;
		}

		if (GameEvents.Instance != null)
		{
			GameEvents.Instance.LevelCompleted -= OnLevelCompleted;
		}

		if (RestartButton != null)
		{
			RestartButton.Pressed -= OnRestartPressed;
		}

		if (NewGameButton != null)
		{
			NewGameButton.Pressed -= OnNewGamePressed;
		}
	}

	private void OnPlayerDied()
	{
		ShowEnd("Moriste", "Las minas no perdonan. Puedes volver al último guardado (F5 o dormir en la cama).", canRetry: true);
	}

	private void OnCampDestroyed()
	{
		ShowEnd("Destruyeron tu campamento", "Sin refugio no hay cómo pasar la noche. Construye defensas con [B].", canRetry: true);
	}

	private void OnLevelCompleted()
	{
		ShowEnd("¡Encontraste la salida del Nivel 1!", "La grieta baja hacia las cavernas medias... (fin de la versión alfa)", canRetry: false);
	}

	private void ShowEnd(string title, string subtitle, bool canRetry)
	{
		if (TitleLabel != null)
		{
			TitleLabel.Text = title;
		}

		if (SubtitleLabel != null)
		{
			SubtitleLabel.Text = subtitle;
		}

		if (RestartButton != null)
		{
			RestartButton.Visible = canRetry;
		}

		SetPanelVisible(true);
		GetTree().Paused = true;
		Input.MouseMode = Input.MouseModeEnum.Visible;
	}

	private void OnRestartPressed()
	{
		// Descarta lo que no se guardó (inventario, bloques minados...).
		WorldState.Instance?.RevertToLastSave();
		GetTree().Paused = false;
		GetTree().ReloadCurrentScene();
	}

	private void OnNewGamePressed()
	{
		WorldState.Instance?.StartNewGame();
		GetTree().Paused = false;
		GetTree().ReloadCurrentScene();
	}

	private void SetPanelVisible(bool visible)
	{
		if (Panel != null)
		{
			Panel.Visible = visible;
		}
	}
}
