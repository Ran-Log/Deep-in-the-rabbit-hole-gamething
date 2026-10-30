using Godot;

/// <summary>
/// HUD del jugador: crosshair, bloque apuntado, texto de interacción, mensajes,
/// notas, estado de mochila y pico, y debug. Solo escucha señales; no toca el mundo.
/// </summary>
public partial class Hud : CanvasLayer
{
	[ExportCategory("Hud")]
	[ExportGroup("Componentes del jugador")]
	[Export] public MiningComponent Mining;
	[Export] public HealthComponent Health;
	[Export] public InteractionComponent Interaction;
	[Export] public BuildComponent Build;
	[Export] public PlayerEquipment Equipment;

	[ExportGroup("Nodos de UI")]
	[Export] public Control Crosshair;
	[Export] public Label TargetLabel;
	[Export] public Label PromptLabel;
	[Export] public Label MessageLabel;
	[Export] public Label StatusLabel;
	[Export] public Label HealthLabel;
	[Export] public Label BuildHintLabel;
	[Export] public Control HelpPanel;
	[Export] public Control NotePanel;
	[Export] public Label NoteLabel;
	[Export] public Label DebugLabel;

	[ExportGroup("Tiempos")]
	[Export] public float MessageSeconds = 3.0f;
	[Export] public float NoteSeconds = 14.0f;

	[ExportGroup("Colores del crosshair")]
	[Export] public Color IdleCrosshairColor = new Color(1.0f, 1.0f, 1.0f, 0.6f);
	[Export] public Color TargetCrosshairColor = new Color(1.0f, 0.85f, 0.35f, 1.0f);
	// Cuando el bloque apuntado pide un pico mejor.
	[Export] public Color BlockedCrosshairColor = new Color(1.0f, 0.35f, 0.35f, 1.0f);

	[ExportGroup("Debug")]
	[Export] public bool ShowDebug = true;

	private DestructibleBlock watchedBlock;
	private float messageTimeLeft;
	private float noteTimeLeft;

	public override void _Ready()
	{
		if (Mining == null)
		{
			GD.PushError($"{Name}: falta asignar MiningComponent; el HUD no se va a actualizar.");
			return;
		}

		Mining.TargetAcquired += OnTargetAcquired;
		Mining.TargetLost += OnTargetLost;

		if (Health != null)
		{
			Health.HealthChanged += OnHealthChanged;
			OnHealthChanged(Health.Current, Health.MaxHealth);
		}

		if (Interaction != null)
		{
			Interaction.PromptChanged += OnPromptChanged;
		}

		if (Build != null)
		{
			Build.BuildModeChanged += OnBuildModeChanged;
			Build.SelectionChanged += OnBuildSelectionChanged;
		}

		if (Equipment != null)
		{
			Equipment.ToolChanged += OnToolChanged;
		}

		if (WorldState.Instance != null)
		{
			WorldState.Instance.InventoryChanged += RefreshStatus;
		}

		if (HelpPanel != null)
		{
			HelpPanel.Visible = false;
		}

		if (GameEvents.Instance != null)
		{
			GameEvents.Instance.MessagePosted += OnMessagePosted;
			GameEvents.Instance.NoteOpened += OnNoteOpened;
		}

		OnTargetLost();
		OnPromptChanged("");
		OnBuildModeChanged(false);
		SetNoteVisible(false);
		RefreshStatus();

		if (MessageLabel != null)
		{
			MessageLabel.Text = "";
		}
	}

	public override void _ExitTree()
	{
		if (Mining != null)
		{
			Mining.TargetAcquired -= OnTargetAcquired;
			Mining.TargetLost -= OnTargetLost;
		}

		if (Health != null)
		{
			Health.HealthChanged -= OnHealthChanged;
		}

		if (Interaction != null)
		{
			Interaction.PromptChanged -= OnPromptChanged;
		}

		if (Build != null)
		{
			Build.BuildModeChanged -= OnBuildModeChanged;
			Build.SelectionChanged -= OnBuildSelectionChanged;
		}

		if (Equipment != null)
		{
			Equipment.ToolChanged -= OnToolChanged;
		}

		if (WorldState.Instance != null)
		{
			WorldState.Instance.InventoryChanged -= RefreshStatus;
		}

		if (GameEvents.Instance != null)
		{
			GameEvents.Instance.MessagePosted -= OnMessagePosted;
			GameEvents.Instance.NoteOpened -= OnNoteOpened;
		}

		StopWatching();
	}

	public override void _Process(double delta)
	{
		TickMessage((float)delta);
		TickNote((float)delta);

		if (DebugLabel == null)
		{
			return;
		}

		DebugLabel.Visible = ShowDebug;

		if (ShowDebug)
		{
			int blockCount = BlockGrid.Instance?.BlockCount ?? 0;
			DebugLabel.Text = $"FPS {Engine.GetFramesPerSecond():0}   Bloques {blockCount}";
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("toggle_help") && HelpPanel != null)
		{
			HelpPanel.Visible = !HelpPanel.Visible;
		}

		if (noteTimeLeft > 0.0f && (@event.IsActionPressed("ui_cancel") || @event.IsActionPressed("toggle_inventory")))
		{
			SetNoteVisible(false);
		}
	}

	// Mochila, comida y pico. Se actualiza por señales, no cada frame.
	private void RefreshStatus()
	{
		if (StatusLabel == null || WorldState.Instance == null)
		{
			return;
		}

		WorldState world = WorldState.Instance;
		string tool = ItemCatalog.ToolName(Equipment?.Tool ?? ToolTier.Basic);

		StatusLabel.Text =
			$"Mochila {world.TotalItems}/{world.Capacity}   " +
			$"Comida {world.GetItemCount(ItemCatalog.Food)} [Q]   " +
			$"{tool}";
	}

	private void OnToolChanged(int tier)
	{
		RefreshStatus();
	}

	private void OnPromptChanged(string prompt)
	{
		if (PromptLabel != null)
		{
			PromptLabel.Text = string.IsNullOrEmpty(prompt) ? "" : $"[E] {prompt}";
		}
	}

	private void OnBuildModeChanged(bool active)
	{
		if (BuildHintLabel != null)
		{
			BuildHintLabel.Visible = active;
		}

		RefreshBuildMenu();
	}

	private void OnBuildSelectionChanged(int index)
	{
		RefreshBuildMenu();
	}

	// Sale de BuildComponent.Options: una construcción nueva aparece sola.
	private void RefreshBuildMenu()
	{
		if (BuildHintLabel == null)
		{
			return;
		}

		var text = new System.Text.StringBuilder("CONSTRUIR   [B] cerrar\n");
		int selected = Build?.Selected ?? 0;

		for (int i = 0; i < BuildComponent.Options.Length; i++)
		{
			BuildComponent.BuildOption option = BuildComponent.Options[i];
			string marker = i == selected ? "►" : "  ";

			text.Append($"{marker} [{i + 1}] {option.Name} — {option.CostText}\n");
		}

		text.Append("Clic izquierdo: colocar");
		BuildHintLabel.Text = text.ToString();
	}

	private void OnMessagePosted(string text)
	{
		if (MessageLabel == null)
		{
			return;
		}

		MessageLabel.Text = text;
		MessageLabel.Modulate = Colors.White;
		messageTimeLeft = MessageSeconds;
	}

	// El mensaje se desvanece en el último medio segundo.
	private void TickMessage(float delta)
	{
		if (MessageLabel == null || messageTimeLeft <= 0.0f)
		{
			return;
		}

		messageTimeLeft -= delta;
		MessageLabel.Modulate = new Color(1, 1, 1, Mathf.Clamp(messageTimeLeft / 0.5f, 0.0f, 1.0f));

		if (messageTimeLeft <= 0.0f)
		{
			MessageLabel.Text = "";
		}
	}

	private void OnNoteOpened(string text)
	{
		if (NoteLabel != null)
		{
			NoteLabel.Text = text;
		}

		SetNoteVisible(true);
	}

	private void TickNote(float delta)
	{
		if (noteTimeLeft <= 0.0f)
		{
			return;
		}

		noteTimeLeft -= delta;

		if (noteTimeLeft <= 0.0f)
		{
			SetNoteVisible(false);
		}
	}

	private void SetNoteVisible(bool visible)
	{
		noteTimeLeft = visible ? NoteSeconds : 0.0f;

		if (NotePanel != null)
		{
			NotePanel.Visible = visible;
		}
	}

	private void OnHealthChanged(int current, int max)
	{
		if (HealthLabel != null)
		{
			HealthLabel.Text = $"Vida: {current}/{max}";
		}
	}

	private void OnTargetAcquired(DestructibleBlock block)
	{
		StopWatching();

		watchedBlock = block;
		watchedBlock.Damaged += OnTargetDamaged;

		RefreshTarget(block.Health, block.MaxHealth);
	}

	private void OnTargetLost()
	{
		StopWatching();

		if (TargetLabel != null)
		{
			TargetLabel.Text = string.Empty;
		}

		if (Crosshair != null)
		{
			Crosshair.Modulate = IdleCrosshairColor;
		}
	}

	private void OnTargetDamaged(int currentHealth, int maxHealth)
	{
		RefreshTarget(currentHealth, maxHealth);
	}

	private void RefreshTarget(int currentHealth, int maxHealth)
	{
		bool minable = watchedBlock.CanBeMinedWith(Mining.Tool);

		if (TargetLabel != null)
		{
			TargetLabel.Text = minable
				? $"{watchedBlock.Label}   {currentHealth}/{maxHealth}"
				: $"{watchedBlock.Label}   requiere {ItemCatalog.ToolName(watchedBlock.RequiredTool)}";
		}

		if (Crosshair != null)
		{
			Crosshair.Modulate = minable ? TargetCrosshairColor : BlockedCrosshairColor;
		}
	}

	private void StopWatching()
	{
		if (watchedBlock != null && GodotObject.IsInstanceValid(watchedBlock))
		{
			watchedBlock.Damaged -= OnTargetDamaged;
		}

		watchedBlock = null;
	}
}
