using Godot;

/// <summary>
/// Cofre / restos de alguien que quedó atrapado antes (GDD 2.1.5:
/// "encontrar equipamiento abandonado"). Lo genera Room según su RoomSpec;
/// guarda una sola cosa (<see cref="ChestLoot"/>).
///
/// Que ya se abrió se guarda en WorldState por coordenada de sala: si el
/// streaming descarga la sala y la vuelve a cargar, el cofre aparece
/// abierto y vacío en vez de regalar el botín de nuevo.
/// </summary>
public partial class Chest : Interactable
{
	[Export]
	public Node3D LidPivot;

	/// <summary>Raciones que trae un cofre de comida.</summary>
	[Export]
	public int FoodAmount = 2;

	/// <summary>Espacio extra que da una mochila encontrada.</summary>
	[Export]
	public int BackpackSlots = 15;

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
			// No se pudo (ej. mochila llena): el cofre queda cerrado con su
			// contenido, para volver más tarde.
			return;
		}

		WorldState.Instance?.MarkChestOpened(RoomCoord);
		ShowOpened();
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
