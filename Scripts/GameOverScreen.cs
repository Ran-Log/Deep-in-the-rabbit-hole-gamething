using Godot;

/// <summary>
/// Pantalla de "moriste": pausa el juego y ofrece reiniciar. Reiniciar
/// recarga main.tscn — como WorldState es autoload, no se destruye al
/// recargar la escena, así que el jugador reaparece en la última posición
/// GUARDADA (ver Player._Ready) con la vida llena, pero el mundo conserva
/// lo que se minó desde el último guardado (esos datos también viven en
/// WorldState, no en la escena).
///
/// Solo escucha PlayerHealth.Died; no sabe nada más del jugador.
/// </summary>
public partial class GameOverScreen : CanvasLayer
{
	[Export]
	public PlayerHealth Health;

	[Export]
	public Control Panel;

	[Export]
	public Button RestartButton;

	public override void _Ready()
	{
		// Tiene que seguir procesando input mientras el árbol está pausado,
		// si no el botón de reiniciar nunca respondería al click.
		ProcessMode = ProcessModeEnum.Always;

		if (Health == null)
		{
			GD.PushError($"{Name}: falta asignar PlayerHealth; el Game Over nunca se va a mostrar.");
		}
		else
		{
			Health.Died += OnPlayerDied;
		}

		if (RestartButton != null)
		{
			RestartButton.Pressed += OnRestartPressed;
		}

		SetPanelVisible(false);
	}

	public override void _ExitTree()
	{
		if (Health != null)
		{
			Health.Died -= OnPlayerDied;
		}

		if (RestartButton != null)
		{
			RestartButton.Pressed -= OnRestartPressed;
		}
	}

	private void OnPlayerDied()
	{
		SetPanelVisible(true);
		GetTree().Paused = true;
		Input.MouseMode = Input.MouseModeEnum.Visible;
	}

	private void OnRestartPressed()
	{
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
