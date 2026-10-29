using Godot;

/// <summary>
/// Pantalla de fin de partida: pausa el juego y muestra por qué terminó.
/// - Derrota (GDD): la vida del jugador llega a 0, o el campamento es
///   destruido.
/// - Victoria: el jugador bajó por la grieta al final del Nivel 1
///   (GameEvents.LevelCompleted).
///
/// "Reintentar" recarga main.tscn — como WorldState es autoload, no se
/// destruye al recargar: el jugador reaparece en la última posición
/// GUARDADA (F5) con la vida llena, y el mundo conserva lo minado.
/// "Nueva partida" además borra todo el progreso (WorldState.StartNewGame).
///
/// Solo escucha señales; no sabe nada del jugador ni del campamento.
/// </summary>
public partial class GameOverScreen : CanvasLayer
{
	[Export]
	public HealthComponent PlayerHealth;

	[Export]
	public HealthComponent CampHealth;

	[Export]
	public Control Panel;

	[Export]
	public Label TitleLabel;

	[Export]
	public Label SubtitleLabel;

	[Export]
	public Button RestartButton;

	[Export]
	public Button NewGameButton;

	public override void _Ready()
	{
		// Tiene que seguir procesando input mientras el árbol está pausado,
		// si no los botones nunca responderían al click.
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
		ShowEnd("Moriste", "Las minas no perdonan. Puedes volver al último guardado (F5).", canRetry: true);
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
