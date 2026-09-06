using Godot;

/// <summary>
/// HUD del jugador: crosshair, información del bloque apuntado y overlay de
/// debug. Solo escucha señales del <see cref="MiningComponent"/>; no toca el
/// mundo ni consulta bloques por su cuenta.
/// </summary>
public partial class Hud : CanvasLayer
{
	[Export]
	public MiningComponent Mining;

	[Export]
	public Control Crosshair;

	[Export]
	public Label TargetLabel;

	[Export]
	public Label DebugLabel;

	[Export]
	public Color IdleCrosshairColor = new Color(1.0f, 1.0f, 1.0f, 0.6f);

	[Export]
	public Color TargetCrosshairColor = new Color(1.0f, 0.85f, 0.35f, 1.0f);

	/// <summary>Crosshair cuando el bloque apuntado exige mejor herramienta.</summary>
	[Export]
	public Color BlockedCrosshairColor = new Color(1.0f, 0.35f, 0.35f, 1.0f);

	[Export]
	public bool ShowDebug = true;

	private DestructibleBlock watchedBlock;

	public override void _Ready()
	{
		if (Mining == null)
		{
			GD.PushError($"{Name}: falta asignar MiningComponent; el HUD no se va a actualizar.");
			return;
		}

		Mining.TargetAcquired += OnTargetAcquired;
		Mining.TargetLost += OnTargetLost;

		OnTargetLost();
	}

	public override void _ExitTree()
	{
		if (Mining != null)
		{
			Mining.TargetAcquired -= OnTargetAcquired;
			Mining.TargetLost -= OnTargetLost;
		}

		StopWatching();
	}

	public override void _Process(double delta)
	{
		if (DebugLabel == null)
		{
			return;
		}

		DebugLabel.Visible = ShowDebug;

		if (!ShowDebug)
		{
			return;
		}

		int blockCount = BlockGrid.Instance?.BlockCount ?? 0;

		DebugLabel.Text = $"FPS {Engine.GetFramesPerSecond():0}   Bloques {blockCount}";
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
				? $"{watchedBlock.Mineral}   {currentHealth}/{maxHealth}"
				: $"{watchedBlock.Mineral}   requiere {watchedBlock.RequiredTool}";
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
