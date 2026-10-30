using Godot;

/// <summary>
/// Atajos para probar y para la demo: F1 volar, F2 día/noche, F3 materiales.
/// Solo funcionan jugando desde el editor (en un build exportado se apagan solos).
/// </summary>
public partial class DebugTools : Node
{
	[ExportCategory("DebugTools")]
	[ExportGroup("Referencias")]
	[Export] public Player Player;

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
