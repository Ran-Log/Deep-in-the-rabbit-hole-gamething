using Godot;

/// <summary>
/// Grieta que baja al Nivel 2: el final del nivel 1 (Beat Chart del GDD:
/// "Cierre del nivel — encontrar el acceso al Nivel 2"). Interactuar
/// termina el nivel; la pantalla de victoria la muestra GameOverScreen
/// escuchando GameEvents.LevelCompleted.
/// </summary>
public partial class ExitCrack : Interactable
{
	public override string GetPrompt(Player player)
	{
		return "Bajar por la grieta (terminar el nivel)";
	}

	public override void Interact(Player player)
	{
		GameEvents.CompleteLevel();
	}
}
