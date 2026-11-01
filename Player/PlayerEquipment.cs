using Godot;

/// <summary>
/// Equipamiento del jugador: pico, arma y nivel de las antorchas (GDD 2.1.4). Los niveles
/// se guardan en WorldState; minar y pelear le preguntan el daño a este nodo.
/// Sin arma se pelea con el pico (GDD 2.1.7: "usa su herramienta o arma equipada").
/// </summary>
public partial class PlayerEquipment : Node
{
	[Signal]
	public delegate void ToolChangedEventHandler(int tier);

	[Signal]
	public delegate void WeaponChangedEventHandler(int tier);

	[Signal]
	public delegate void LightChangedEventHandler(int level);

	[ExportCategory("PlayerEquipment")]
	[ExportGroup("Combate")]
	// Daño extra del pico contra enemigos, sumado a su daño de minado.
	[Export] public int CombatBonusDamage = 1;

	public ToolTier Tool => WorldState.Instance?.Tool ?? ToolTier.Basic;

	public WeaponTier Weapon => WorldState.Instance?.Weapon ?? WeaponTier.None;

	public int LightLevel => WorldState.Instance?.LightLevel ?? 1;

	public int MiningDamage => MineralRules.ToolDamage(Tool);

	public int PickaxeCombatDamage => MineralRules.ToolDamage(Tool) + CombatBonusDamage;

	public int WeaponDamage => GameData.Weapon(Weapon)?.Damage ?? 0;

	// Se pelea con lo que pegue más fuerte: el arma o el pico.
	public int CombatDamage => Mathf.Max(PickaxeCombatDamage, WeaponDamage);

	public override void _Ready()
	{
		if (WorldState.Instance == null)
		{
			return;
		}

		WorldState.Instance.ToolChanged += OnWorldToolChanged;
		WorldState.Instance.WeaponChanged += OnWorldWeaponChanged;
		WorldState.Instance.LightChanged += OnWorldLightChanged;
	}

	public override void _ExitTree()
	{
		if (WorldState.Instance == null)
		{
			return;
		}

		WorldState.Instance.ToolChanged -= OnWorldToolChanged;
		WorldState.Instance.WeaponChanged -= OnWorldWeaponChanged;
		WorldState.Instance.LightChanged -= OnWorldLightChanged;
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

	public bool UpgradeWeaponTo(WeaponTier tier)
	{
		if (WorldState.Instance == null || tier <= Weapon)
		{
			return false;
		}

		WorldState.Instance.Weapon = tier;
		return true;
	}

	public bool UpgradeLightTo(int level)
	{
		if (WorldState.Instance == null || level <= LightLevel)
		{
			return false;
		}

		WorldState.Instance.LightLevel = level;
		return true;
	}

	private void OnWorldToolChanged(int tier)
	{
		EmitSignal(SignalName.ToolChanged, tier);
	}

	private void OnWorldWeaponChanged(int tier)
	{
		EmitSignal(SignalName.WeaponChanged, tier);
	}

	private void OnWorldLightChanged(int level)
	{
		EmitSignal(SignalName.LightChanged, level);
	}
}
