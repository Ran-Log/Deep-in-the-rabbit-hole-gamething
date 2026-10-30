using Godot;

/// <summary>
/// Antorcha que se enciende y apaga con E (GDD 2.1.9: "encender antorchas").
/// De noche es la diferencia entre ver a los zombies venir o no.
/// </summary>
public partial class Torch : Interactable
{
	[ExportCategory("Torch")]
	[ExportGroup("Referencias")]
	[Export] public OmniLight3D Light;
	[Export] public Node3D Flame;

	[ExportGroup("Estado inicial")]
	[Export] public bool StartLit = false;

	private bool lit;

	public override void _Ready()
	{
		SetLit(StartLit);
	}

	public override string GetPrompt(Player player)
	{
		return lit ? "Apagar antorcha" : "Encender antorcha";
	}

	public override void Interact(Player player)
	{
		SetLit(!lit);
	}

	private void SetLit(bool value)
	{
		lit = value;

		if (Light != null)
		{
			Light.Visible = lit;
		}

		if (Flame != null)
		{
			Flame.Visible = lit;
		}
	}
}
