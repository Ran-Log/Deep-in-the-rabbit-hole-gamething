using Godot;

/// <summary>
/// Grieta al nivel 2, el cierre del nivel 1 (beat chart del GDD). Interactuar
/// avisa por GameEvents.LevelCompleted.
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
