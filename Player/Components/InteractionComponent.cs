using Godot;

/// <summary>
/// Detecta el <see cref="Interactable"/> apuntado con la mira y lo activa
/// con la acción "interact" (E). Avisa al HUD qué texto mostrar con
/// <see cref="PromptChanged"/> (vacío = nada apuntado).
/// </summary>
public partial class InteractionComponent : Node3D
{
	[Signal]
	public delegate void PromptChangedEventHandler(string prompt);

	[Export]
	public Camera3D Camera;

	[Export]
	public Player Player;

	[Export]
	public float Range = 5.0f;

	[Export(PropertyHint.Layers3DPhysics)]
	public uint TargetLayers = 1;

	private Interactable current;
	private string lastPrompt = "";

	public override void _PhysicsProcess(double delta)
	{
		if (Camera == null || Player == null)
		{
			return;
		}

		current = FindTarget();

		// El texto se recalcula cada frame: puede cambiar sin cambiar de
		// objetivo (ej. la estación pasa a "puedes mejorar" al juntar mineral).
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
