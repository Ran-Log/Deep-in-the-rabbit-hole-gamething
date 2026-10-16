using Godot;

/// <summary>
/// Atajos de debug para probar y para mostrar el juego en la presentación
/// sin tener que jugar 20 minutos hasta cada parte:
/// - F1: volar / atravesar bloques (Player.NoClip).
/// - F2: pasar a noche (con su oleada de zombies) o volver al día.
/// - F3: dar materiales y comida (para probar mejoras y defensas).
///
/// Solo funciona en builds de debug (jugando desde el editor): en un
/// build exportado <see cref="OS.IsDebugBuild"/> es false y este nodo se
/// apaga solo, así no hace falta acordarse de borrarlo para entregar.
/// </summary>
public partial class DebugTools : Node
{
	[Export]
	public Player Player;

	public override void _Ready()
	{
		if (!OS.IsDebugBuild())
		{
			SetProcessUnhandledInput(false);
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("debug_fly"))
		{
			ToggleFly();
		}
		else if (@event.IsActionPressed("debug_night"))
		{
			(GetTree().GetFirstNodeInGroup(DayNightCycle.Group) as DayNightCycle)?.TogglePhaseDebug();
		}
		else if (@event.IsActionPressed("debug_items"))
		{
			GiveItems();
		}
	}

	private void ToggleFly()
	{
		if (Player == null)
		{
			return;
		}

		Player.NoClip = !Player.NoClip;
		GameEvents.PostMessage(Player.NoClip
			? "[DEBUG] Vuelo ON — Espacio sube, Ctrl baja, Shift rápido (F1 para salir)"
			: "[DEBUG] Vuelo OFF");
	}

	private static void GiveItems()
	{
		WorldState world = WorldState.Instance;

		if (world == null)
		{
			return;
		}

		// Agranda la mochila si hace falta, para que el atajo siempre funcione.
		const int total = 33;

		if (world.Capacity - world.TotalItems < total)
		{
			world.IncreaseCapacity(total);
		}

		world.TryAddItem("Stone", 10);
		world.TryAddItem("Coal", 10);
		world.TryAddItem("Iron", 10);
		world.TryAddItem(ItemCatalog.Food, 3);

		GameEvents.PostMessage("[DEBUG] +10 Piedra, +10 Carbón, +10 Hierro, +3 Comida");
	}
}
