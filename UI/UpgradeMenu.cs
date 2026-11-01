using Godot;

/// <summary>
/// Menú de la estación de mejora (GDD 2.1.4: "interacción sobre la estación, luego selección
/// en un menú con mouse"). Una fila por línea de mejora; las filas salen de UpgradeTable.
/// Como la mochila, no pausa el juego.
/// </summary>
public partial class UpgradeMenu : CanvasLayer
{
	public const string Group = "upgrade_menu";

	[ExportCategory("UpgradeMenu")]
	[ExportGroup("Nodos de UI")]
	[Export] public Control Panel;
	[Export] public VBoxContainer RowsContainer;
	[Export] public Button CloseButton;

	[ExportGroup("Colores")]
	[Export] public Color MissingColor = new Color(1.0f, 0.45f, 0.4f);
	[Export] public Color EnoughColor = new Color(0.65f, 1.0f, 0.65f);

	private PlayerEquipment equipment;

	public bool IsOpen => Panel != null && Panel.Visible;

	public override void _EnterTree()
	{
		AddToGroup(Group);
	}

	public override void _Ready()
	{
		if (Panel == null || RowsContainer == null || CloseButton == null)
		{
			GD.PushError($"{Name}: faltan nodos por asignar.");
			return;
		}

		CloseButton.Pressed += Close;

		if (WorldState.Instance != null)
		{
			WorldState.Instance.InventoryChanged += RefreshIfOpen;
		}

		Panel.Visible = false;
	}

	public override void _ExitTree()
	{
		if (CloseButton != null)
		{
			CloseButton.Pressed -= Close;
		}

		if (WorldState.Instance != null)
		{
			WorldState.Instance.InventoryChanged -= RefreshIfOpen;
		}
	}

	// Esc cierra este menú antes que abrir la pausa (este nodo va después de PauseMenu en main.tscn).
	public override void _UnhandledInput(InputEvent @event)
	{
		if (IsOpen && @event.IsActionPressed("ui_cancel"))
		{
			Close();
			GetViewport().SetInputAsHandled();
		}
	}

	public void Open(PlayerEquipment playerEquipment)
	{
		if (Panel == null || playerEquipment == null)
		{
			return;
		}

		equipment = playerEquipment;
		Panel.Visible = true;
		Input.MouseMode = Input.MouseModeEnum.Visible;
		Refresh();
	}

	public void Close()
	{
		if (!IsOpen)
		{
			return;
		}

		Panel.Visible = false;
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	// Público para los tests: lo mismo que apretar "Mejorar" en esa fila.
	public bool Buy(int rowIndex)
	{
		if (equipment == null)
		{
			return false;
		}

		var options = UpgradeTable.Options(equipment);

		if (rowIndex < 0 || rowIndex >= options.Count)
		{
			return false;
		}

		bool bought = UpgradeTable.TryBuy(options[rowIndex], equipment, out string message);
		GameEvents.PostMessage(message);
		Refresh();

		return bought;
	}

	private void RefreshIfOpen()
	{
		if (IsOpen)
		{
			Refresh();
		}
	}

	private void Refresh()
	{
		foreach (Node child in RowsContainer.GetChildren())
		{
			RowsContainer.RemoveChild(child);
			child.QueueFree();
		}

		var options = UpgradeTable.Options(equipment);

		for (int i = 0; i < options.Count; i++)
		{
			RowsContainer.AddChild(BuildRow(options[i], i));
		}
	}

	/// <summary>Título, "actual → siguiente", costo (en rojo lo que falta) y el botón.</summary>
	private Control BuildRow(UpgradeOption option, int index)
	{
		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 16);

		var info = new VBoxContainer { CustomMinimumSize = new Vector2(380, 0) };
		row.AddChild(info);

		var title = new Label { Text = option.Title };
		title.AddThemeFontSizeOverride("font_size", 18);
		info.AddChild(title);

		info.AddChild(new Label
		{
			Text = option.IsMax ? $"{option.Current} (máximo)" : $"{option.Current}  →  {option.Next}",
		});

		if (!option.IsMax)
		{
			info.AddChild(BuildCostLine(option));
		}

		var button = new Button
		{
			Text = option.IsMax ? "Al máximo" : "Mejorar",
			Disabled = option.IsMax || WorldState.Instance?.HasItems(option.Cost) != true,
			CustomMinimumSize = new Vector2(110, 0),
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
		};
		button.Pressed += () => Buy(index);
		row.AddChild(button);

		return row;
	}

	private Control BuildCostLine(UpgradeOption option)
	{
		var line = new HBoxContainer();
		line.AddThemeConstantOverride("separation", 12);

		foreach (var (itemId, amount) in option.Cost)
		{
			int have = WorldState.Instance?.GetItemCount(itemId) ?? 0;
			var label = new Label { Text = $"{ItemCatalog.DisplayName(itemId)} {have}/{amount}" };
			label.AddThemeColorOverride("font_color", have >= amount ? EnoughColor : MissingColor);
			line.AddChild(label);
		}

		return line;
	}
}
