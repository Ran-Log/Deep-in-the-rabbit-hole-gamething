using Godot;

/// <summary>
/// La lámpara del casco de minero: una luz tenue que el jugador enciende o apaga con L.
/// </summary>
public partial class LampComponent : Node
{
	[Signal]
	public delegate void ToggledEventHandler(bool on);

	[ExportCategory("LampComponent")]
	[ExportGroup("Referencias")]
	[Export] public Light3D Lamp;

	[ExportGroup("Estado inicial")]
	[Export] public bool StartOn = true;

	public bool IsOn => Lamp != null && Lamp.Visible;

	public override void _Ready()
	{
		if (Lamp == null)
		{
			GD.PushError($"{Name}: falta asignar Lamp.");
			return;
		}

		Lamp.Visible = StartOn;
		ApplyLightLevel(WorldState.Instance?.LightLevel ?? 1);

		if (WorldState.Instance != null)
		{
			WorldState.Instance.LightChanged += ApplyLightLevel;
		}
	}

	public override void _ExitTree()
	{
		if (WorldState.Instance != null)
		{
			WorldState.Instance.LightChanged -= ApplyLightLevel;
		}
	}

	// Las antorchas mejoradas también alargan la luz de la lámpara.
	private void ApplyLightLevel(int level)
	{
		LightDefinition light = GameData.Light(level);

		if (Lamp is OmniLight3D omni && light != null)
		{
			omni.OmniRange = light.LampRange;
		}
	}

	// Solo lee la tecla (clase 8: ProcessInput separado de UpdateState).
	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("toggle_lamp") && CrosshairRay.PlayerHasControl())
		{
			Toggle();
		}
	}

	public void Toggle()
	{
		if (Lamp == null)
		{
			return;
		}

		Lamp.Visible = !Lamp.Visible;
		EmitSignal(SignalName.Toggled, Lamp.Visible);
	}
}
