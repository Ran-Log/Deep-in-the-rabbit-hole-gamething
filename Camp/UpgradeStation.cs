using Godot;

/// <summary>
/// Estación de mejora (GDD 2.1.4): con E abre el menú para mejorar pico, arma y antorchas.
/// Las mejoras y sus costos están en UpgradeTable; el menú es UpgradeMenu.
/// </summary>
public partial class UpgradeStation : Interactable
{
	public override string GetPrompt(Player player)
	{
		return "Usar estación de mejora";
	}

	public override void Interact(Player player)
	{
		if (GetTree().GetFirstNodeInGroup(UpgradeMenu.Group) is not UpgradeMenu menu)
		{
			GD.PushWarning($"{Name}: no hay UpgradeMenu en la escena.");
			return;
		}

		menu.Open(player.Equipment);
	}
}
