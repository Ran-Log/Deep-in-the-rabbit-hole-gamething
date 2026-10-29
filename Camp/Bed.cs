using Godot;

/// <summary>
/// Cama del campamento (GDD 2.1.6): recupera toda la vida y, si es de
/// noche, salta al amanecer — pero solo si no hay enemigos cerca ("si está
/// protegido"). Así las defensas tienen sentido: con la base bien cerrada
/// se puede dormir antes.
/// </summary>
public partial class Bed : Interactable
{
	/// <summary>Radio en metros dentro del cual un zombie impide dormir.</summary>
	[Export]
	public float SafeRadius = 14.0f;

	public override string GetPrompt(Player player)
	{
		return FindDayNight()?.IsNight == true
			? "Dormir hasta el amanecer"
			: "Descansar (recuperar vida)";
	}

	public override void Interact(Player player)
	{
		if (Zombie.CountNear(GetTree(), GlobalPosition, SafeRadius) > 0)
		{
			GameEvents.PostMessage("No puedes descansar con enemigos cerca");
			return;
		}

		player.Health?.Heal(player.Health.MaxHealth);

		DayNightCycle dayNight = FindDayNight();

		if (dayNight != null && dayNight.IsNight)
		{
			dayNight.SkipToMorning();
		}
		else
		{
			GameEvents.PostMessage("Descansaste. Vida recuperada");
		}
	}

	private DayNightCycle FindDayNight()
	{
		return GetTree().GetFirstNodeInGroup(DayNightCycle.Group) as DayNightCycle;
	}
}
