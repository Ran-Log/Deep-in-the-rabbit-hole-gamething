using Godot;

/// <summary>
/// Pantalla de inventario: se abre/cierra con la acción "toggle_inventory"
/// (Tab por defecto). Mientras está abierta libera el mouse — CameraRig ya
/// ignora el movimiento de cámara cuando el mouse no está capturado (ver
/// CameraRig._Input), así que no hace falta coordinar nada más con él.
///
/// Lee directo de WorldState (autoload), igual que Hud. Las filas se arman
/// en código (no a mano en el .tscn) para no depender de una lista fija de
/// nodos — un mineral nuevo aparece solo, sin tocar la escena.
/// </summary>
public partial class InventoryScreen : CanvasLayer
{
	[Export]
	public Control Panel;

	[Export]
	public VBoxContainer EntriesContainer;

	private bool open;

	public override void _Ready()
	{
		SetOpen(false);
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

	private void RefreshEntries()
	{
		if (EntriesContainer == null || WorldState.Instance == null)
		{
			return;
		}

		foreach (Node child in EntriesContainer.GetChildren())
		{
			child.QueueFree();
		}

		bool any = false;

		foreach (var (mineral, count) in WorldState.Instance.GetMineralCounts())
		{
			any = true;
			EntriesContainer.AddChild(BuildRow(mineral, count));
		}

		if (!any)
		{
			EntriesContainer.AddChild(new Label { Text = "(vacío — todavía no minaste nada)" });
		}
	}

	/// <summary>
	/// Ícono + nombre + cantidad de un mineral. El ícono usa la MISMA
	/// textura procedural del material 3D del bloque (ver
	/// MineralTextureFactory) — no depende de ningún .png en disco, así
	/// que siempre hay ícono, para cualquier mineral.
	/// </summary>
	private static Control BuildRow(MineralType mineral, int count)
	{
		var row = new HBoxContainer();

		row.AddChild(new TextureRect
		{
			Texture = MineralTextureFactory.GetBlockIcon(mineral),
			CustomMinimumSize = new Vector2(32, 32),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.Scale,
		});

		row.AddChild(new Label { Text = $"{mineral}   x{count}" });

		return row;
	}
}
