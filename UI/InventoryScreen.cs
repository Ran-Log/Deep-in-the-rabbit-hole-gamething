using Godot;

/// <summary>
/// Mochila (GDD 2.1.3), con Tab: muestra el espacio libre y permite usar la comida
/// o soltar ítems. Mientras está abierta el jugador no se mueve (el GDD quiere ese riesgo).
/// Las filas se arman por código, así un ítem nuevo aparece sin tocar la escena.
/// </summary>
public partial class InventoryScreen : CanvasLayer
{
	[ExportCategory("InventoryScreen")]
	[ExportGroup("Referencias")]
	// Para el botón "Usar" de la comida.
	[Export] public EatComponent Eat;

	[ExportGroup("Nodos de UI")]
	[Export] public Control Panel;
	[Export] public VBoxContainer EntriesContainer;
	[Export] public Label CapacityLabel;

	private bool open;
	private static Texture2D foodIcon;
	private static Texture2D waterIcon;

	public override void _Ready()
	{
		if (WorldState.Instance != null)
		{
			WorldState.Instance.InventoryChanged += OnInventoryChanged;
		}

		SetOpen(false);
	}

	public override void _ExitTree()
	{
		if (WorldState.Instance != null)
		{
			WorldState.Instance.InventoryChanged -= OnInventoryChanged;
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("toggle_inventory"))
		{
			SetOpen(!open);
		}
	}

	private void SetOpen(bool value)
	{
		open = value;

		if (Panel != null)
		{
			Panel.Visible = open;
		}

		Input.MouseMode = open ? Input.MouseModeEnum.Visible : Input.MouseModeEnum.Captured;

		if (open)
		{
			RefreshEntries();
		}
	}

	private void OnInventoryChanged()
	{
		if (open)
		{
			RefreshEntries();
		}
	}

	private void RefreshEntries()
	{
		if (EntriesContainer == null || WorldState.Instance == null)
		{
			return;
		}

		WorldState world = WorldState.Instance;

		if (CapacityLabel != null)
		{
			CapacityLabel.Text = $"Espacio: {world.TotalItems}/{world.Capacity}     Herramienta: {ItemCatalog.ToolName(world.Tool)}";
		}

		foreach (Node child in EntriesContainer.GetChildren())
		{
			child.QueueFree();
		}

		bool any = false;

		foreach (var (itemId, count) in world.GetItems())
		{
			any = true;
			EntriesContainer.AddChild(BuildRow(itemId, count));
		}

		if (!any)
		{
			EntriesContainer.AddChild(new Label { Text = "(vacía — todavía no recogiste nada)" });
		}
	}

	/// <summary>Ícono + nombre + cantidad + botones de acción de un ítem.</summary>
	private Control BuildRow(string itemId, int count)
	{
		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 12);

		row.AddChild(new TextureRect
		{
			Texture = GetIcon(itemId),
			CustomMinimumSize = new Vector2(32, 32),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.Scale,
		});

		row.AddChild(new Label
		{
			Text = $"{ItemCatalog.DisplayName(itemId)}   x{count}",
			CustomMinimumSize = new Vector2(220, 0),
		});

		if (ItemCatalog.IsConsumable(itemId))
		{
			var useButton = new Button { Text = "Usar" };
			useButton.Pressed += () => Eat?.Use(itemId);
			row.AddChild(useButton);
		}

		var dropButton = new Button { Text = "Soltar 1" };
		dropButton.Pressed += () => WorldState.Instance?.RemoveItem(itemId);
		row.AddChild(dropButton);

		return row;
	}

	/// <summary>
	/// Los minerales usan la MISMA textura procedural del bloque 3D (ver
	/// MineralTextureFactory), así que siempre hay ícono sin ningún .png en
	/// disco. La comida, que no es un bloque, usa un cuadro de color plano.
	/// </summary>
	private static Texture2D GetIcon(string itemId)
	{
		if (System.Enum.TryParse(itemId, out MineralType mineral))
		{
			return MineralTextureFactory.GetBlockIcon(mineral);
		}

		if (itemId == ItemCatalog.Water)
		{
			waterIcon ??= FlatIcon(new Color(0.25f, 0.5f, 0.85f));
			return waterIcon;
		}

		foodIcon ??= FlatIcon(new Color(0.85f, 0.55f, 0.25f));
		return foodIcon;
	}

	private static Texture2D FlatIcon(Color color)
	{
		Image image = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8);
		image.Fill(color);
		return ImageTexture.CreateFromImage(image);
	}
}
