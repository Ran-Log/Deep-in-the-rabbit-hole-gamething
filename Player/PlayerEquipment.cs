using Godot;

/// <summary>
/// Equipamiento del jugador (hoy, el pico). El nivel se guarda en WorldState;
/// minar y pelear le preguntan el daño a este nodo. El mismo pico sirve para las dos cosas (GDD 2.1.7).
/// </summary>
public partial class PlayerEquipment : Node
{
	[Signal]
	public delegate void ToolChangedEventHandler(int tier);

	[ExportCategory("PlayerEquipment")]
	[ExportGroup("Combate")]
	// Daño extra contra enemigos, sumado al daño del pico.
	[Export] public int CombatBonusDamage = 1;

	public ToolTier Tool => WorldState.Instance?.Tool ?? ToolTier.Basic;

	public int MiningDamage => MineralRules.ToolDamage(Tool);

	public int CombatDamage => MineralRules.ToolDamage(Tool) + CombatBonusDamage;

	public override void _Ready()
	{
		if (WorldState.Instance != null)
		{
			WorldState.Instance.ToolChanged += OnWorldToolChanged;
		}
	}

	public override void _ExitTree()
	{
		if (WorldState.Instance != null)
		{
			WorldState.Instance.ToolChanged -= OnWorldToolChanged;
		}
	}

	/// <summary>Sube el pico a <paramref name="tier"/> solo si es mejor que el actual. Devuelve si cambió.</summary>
	public bool UpgradeTo(ToolTier tier)
	{
		if (WorldState.Instance == null || tier <= Tool)
		{
			return false;
		}

		WorldState.Instance.Tool = tier;
		return true;
	}

	private void OnWorldToolChanged(int tier)
	{
		EmitSignal(SignalName.ToolChanged, tier);
	}
}
