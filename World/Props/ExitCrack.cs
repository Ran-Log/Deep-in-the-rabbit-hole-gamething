using Godot;

/// <summary>
/// La salida del nivel (beat chart del GDD: "encontrar el acceso al siguiente nivel").
/// En los niveles 1 y 2 baja al siguiente; en el último, es la salida de la mina (victoria).
/// </summary>
public partial class ExitCrack : Interactable
{
	public override string GetPrompt(Player player)
	{
		return GameData.CurrentLevel?.ExitPrompt ?? "Bajar por la grieta";
	}

	public override void Interact(Player player)
	{
		LevelDefinition level = GameData.CurrentLevel;

		if (level == null || level.IsFinal || WorldState.Instance == null)
		{
			GameEvents.CompleteLevel();
			return;
		}

		WorldState.Instance.AdvanceLevel();
		GetTree().ReloadCurrentScene();
	}
}
