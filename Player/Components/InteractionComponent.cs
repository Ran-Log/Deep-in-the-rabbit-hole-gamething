using Godot;

/// <summary>
/// Activa con E el Interactable apuntado (cama, cofre, estación...) y le dice
/// al HUD qué texto mostrar (vacío = nada apuntado).
/// </summary>
public partial class InteractionComponent : Node3D
{
	[Signal]
	public delegate void PromptChangedEventHandler(string prompt);

	[ExportCategory("InteractionComponent")]
	[ExportGroup("Referencias")]
	[Export] public Camera3D Camera;
	[Export] public Player Player;

	[ExportGroup("Alcance")]
	// Metros desde el jugador (CrosshairRay suma la distancia de la cámara).
	[Export] public float Range = 4.0f;
	[Export(PropertyHint.Layers3DPhysics)] public uint TargetLayers = 1;

	private Interactable current;
	private string lastPrompt = "";

	public override void _PhysicsProcess(double delta)
	{
		if (Camera == null || Player == null)
		{
			return;
		}

		current = FindTarget();

		// Se recalcula cada frame: el texto puede cambiar sin cambiar de objetivo.
		string prompt = current != null ? current.GetPrompt(Player) : "";

		if (prompt != lastPrompt)
		{
			lastPrompt = prompt;
			EmitSignal(SignalName.PromptChanged, prompt);
		}

		if (current != null
			&& !string.IsNullOrEmpty(prompt)
			&& CrosshairRay.PlayerHasControl()
			&& Input.IsActionJustPressed("interact"))
		{
			current.Interact(Player);
		}
	}

	private Interactable FindTarget()
	{
		if (!CrosshairRay.Cast(Camera, Range, TargetLayers, Player, out CrosshairRay.Hit hit))
		{
			return null;
		}

		return hit.Collider as Interactable;
	}
}
