using Godot;

/// <summary>
/// Equipamiento del jugador (hoy: el pico). El nivel vive en WorldState
/// para que se guarde con la partida y sobreviva a reiniciar la escena;
/// este nodo es la cara "del jugador" de ese dato: los componentes que
/// pegan (MiningComponent, CombatComponent) le preguntan el nivel y el
/// daño acá, sin saber de WorldState.
///
/// El mismo pico sirve para minar y para pelear (GDD 2.1.7: "usa su
/// herramienta o arma equipada"), así una mejora se nota en las dos cosas.
/// </summary>
public partial class PlayerEquipment : Node
{
	[Signal]
	public delegate void ToolChangedEventHandler(int tier);

	/// <summary>Daño extra contra enemigos sobre el daño del pico a bloques.</summary>
	[Export]
	public int CombatBonusDamage = 1;

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
