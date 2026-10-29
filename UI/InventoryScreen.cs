using Godot;

/// <summary>
/// Pantalla de mochila (GDD 2.1.3): se abre/cierra con "toggle_inventory"
/// (Tab). Muestra cuánto espacio queda y, por cada ítem, permite USAR los
/// consumibles (comida) y SOLTAR uno para hacer espacio. Mientras está
/// abierta libera el mouse, y el jugador no puede caminar ni minar (todos
/// los componentes miran CrosshairRay.PlayerHasControl), así que revisar
/// la mochila lejos del campamento es un riesgo, como pide el GDD.
///
/// Lee directo de WorldState (autoload). Las filas se arman en código (no
/// a mano en el .tscn) para no depender de una lista fija de nodos — un
/// ítem nuevo aparece solo, sin tocar la escena.
/// </summary>
public partial class InventoryScreen : CanvasLayer
{
	[Export]
	public Control Panel;

	[Export]
	public VBoxContainer EntriesContainer;

	[Export]
	public Label CapacityLabel;

	/// <summary>Para el botón "Usar" de la comida (vive en el jugador).</summary>
	[Export]
	public EatComponent Eat;

	private bool open;
	private static Texture2D foodIcon;

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
			useButton.Pressed += () => Eat?.TryEat();
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

		if (foodIcon == null)
		{
			Image image = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8);
			image.Fill(new Color(0.85f, 0.55f, 0.25f));
			foodIcon = ImageTexture.CreateFromImage(image);
		}

		return foodIcon;
	}
}
