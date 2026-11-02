using System.Collections.Generic;
using Godot;

/// <summary>
/// La fogata del campamento, lo que atacan los enemigos (GDD: "el campamento tiene vida, la cual
/// se puede mejorar o recuperar"). Con E se repara gastando piedra; su vida máxima sale del nivel
/// del campamento (CampDefinition), que se mejora en la estación.
/// </summary>
public partial class CampCore : Interactable
{
	[Signal]
	public delegate void RepairedEventHandler();

	private static readonly string StoneId = ItemCatalog.IdOf(MineralType.Stone);

	[ExportCategory("CampCore")]
	[ExportGroup("Referencias")]
	[Export] public HealthComponent Health;

	[ExportGroup("Reparar")]
	[Export(PropertyHint.Range, "1,200,1")] public int RepairAmount = 25;
	[Export(PropertyHint.Range, "1,50,1")] public int RepairStoneCost = 3;

	public IReadOnlyDictionary<string, int> RepairCost => new Dictionary<string, int> { [StoneId] = RepairStoneCost };

	// Antes del _Ready de Health, para que cargue la vida guardada con el máximo correcto.
	public override void _EnterTree()
	{
		Health ??= GetNodeOrNull<HealthComponent>("Health");

		if (Health != null)
		{
			Health.MaxHealth = MaxHealthFor(WorldState.Instance?.CampLevel ?? 1);
		}
	}

	public override void _Ready()
	{
		if (WorldState.Instance != null)
		{
			WorldState.Instance.CampLevelChanged += OnCampLevelChanged;
		}
	}

	public override void _ExitTree()
	{
		if (WorldState.Instance != null)
		{
			WorldState.Instance.CampLevelChanged -= OnCampLevelChanged;
		}
	}

	public override string GetPrompt(Player player)
	{
		if (Health == null)
		{
			return "";
		}

		return Health.IsFull
			? $"Campamento ({Health.Current}/{Health.MaxHealth})"
			: $"Reparar campamento (+{RepairAmount} vida · {RepairStoneCost} Piedra)";
	}

	public override void Interact(Player player)
	{
		TryRepair();
	}

	// Público para los tests.
	public bool TryRepair()
	{
		if (Health == null || WorldState.Instance == null)
		{
			return false;
		}

		if (Health.IsFull)
		{
			GameEvents.PostMessage("El campamento no necesita reparación");
			return false;
		}

		if (!WorldState.Instance.TrySpend(RepairCost))
		{
			GameEvents.PostMessage($"Necesitas {RepairStoneCost} Piedra para reparar");
			return false;
		}

		Health.Heal(RepairAmount);
		GameEvents.PostMessage($"Reparaste el campamento (+{RepairAmount} vida)");
		EmitSignal(SignalName.Repaired);

		return true;
	}

	private void OnCampLevelChanged(int level)
	{
		Health?.SetMaxHealth(MaxHealthFor(level));
	}

	private static int MaxHealthFor(int level)
	{
		return GameData.Camp(level)?.MaxHealth ?? 100;
	}
}
