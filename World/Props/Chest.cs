using Godot;

/// <summary>
/// Cofre o restos de alguien atrapado antes (GDD 2.1.5). Guarda una sola cosa.
/// Queda marcado como abierto en WorldState, para no regalar el botín dos veces.
/// </summary>
public partial class Chest : Interactable
{
	[Signal]
	public delegate void OpenedEventHandler();

	[ExportCategory("Chest")]
	[ExportGroup("Referencias")]
	[Export] public Node3D LidPivot;

	[ExportGroup("Botín")]
	// Raciones de un cofre de comida.
	[Export] public int FoodAmount = 2;
	// Cantimploras de un cofre de agua.
	[Export] public int WaterAmount = 2;
	// Espacio extra que da la mochila encontrada.
	[Export] public int BackpackSlots = 15;

	public ChestLoot Loot { get; set; } = ChestLoot.Food;

	public Vector2I RoomCoord { get; set; }

	public string NoteText { get; set; } = "";

	private bool opened;

	public override void _Ready()
	{
		if (WorldState.Instance != null && WorldState.Instance.IsChestOpened(RoomCoord))
		{
			ShowOpened();
		}
	}

	public override string GetPrompt(Player player)
	{
		if (!opened)
		{
			return Loot == ChestLoot.Note ? "Revisar restos" : "Abrir cofre";
		}

		// Una nota se puede releer; un cofre vacío ya no ofrece nada.
		return Loot == ChestLoot.Note ? "Releer nota" : "";
	}

	public override void Interact(Player player)
	{
		if (opened)
		{
			if (Loot == ChestLoot.Note)
			{
				GameEvents.OpenNote(NoteText);
			}

			return;
		}

		if (!GiveLoot(player))
		{
			// Ej. mochila llena: queda cerrado para volver más tarde.
			return;
		}

		WorldState.Instance?.MarkChestOpened(RoomCoord);
		ShowOpened();
		EmitSignal(SignalName.Opened);
	}

	private bool GiveLoot(Player player)
	{
		switch (Loot)
		{
			case ChestLoot.Food:
				if (WorldState.Instance == null || !WorldState.Instance.TryAddItem(ItemCatalog.Food, FoodAmount))
				{
					GameEvents.PostMessage("Mochila llena: no te caben las raciones");
					return false;
				}

				GameEvents.PostMessage($"Encontraste {FoodAmount} raciones de comida");
				return true;

			case ChestLoot.Water:
				if (WorldState.Instance == null || !WorldState.Instance.TryAddItem(ItemCatalog.Water, WaterAmount))
				{
					GameEvents.PostMessage("Mochila llena: no te caben las cantimploras");
					return false;
				}

				GameEvents.PostMessage($"Encontraste {WaterAmount} cantimploras de agua");
				return true;

			case ChestLoot.Pickaxe:
				GameEvents.PostMessage(player.Equipment != null && player.Equipment.UpgradeTo(ToolTier.Reinforced)
					? "Encontraste un Pico reforzado abandonado. Ahora puedes romper hierro"
					: "Un pico viejo y oxidado. El tuyo es mejor");
				return true;

			case ChestLoot.Backpack:
				WorldState.Instance?.IncreaseCapacity(BackpackSlots);
				GameEvents.PostMessage($"Encontraste una mochila más grande (+{BackpackSlots} espacio)");
				return true;

			case ChestLoot.Note:
				GameEvents.OpenNote(NoteText);
				return true;

			default:
				return true;
		}
	}

	private void ShowOpened()
	{
		opened = true;

		if (LidPivot != null)
		{
			LidPivot.RotationDegrees = new Vector3(-100.0f, 0.0f, 0.0f);
		}
	}
}
