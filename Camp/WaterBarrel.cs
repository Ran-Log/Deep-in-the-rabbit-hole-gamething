using Godot;

/// <summary>
/// Barril de agua del campamento: beber es gratis y quita toda la sed.
/// Así la sed marca cuánto puede alejarse el jugador antes de volver.
/// </summary>
public partial class WaterBarrel : Interactable
{
	[Signal]
	public delegate void DrankEventHandler();

	public override string GetPrompt(Player player)
	{
		return "Beber agua";
	}

	public override void Interact(Player player)
	{
		if (player.Thirst == null)
		{
			return;
		}

		if (player.Thirst.IsFull)
		{
			GameEvents.PostMessage("No tienes sed");
			return;
		}

		player.Thirst.Restore(player.Thirst.MaxValue);
		GameEvents.PostMessage("Bebiste agua del barril");
		EmitSignal(SignalName.Drank);
	}
}
