using Godot;

/// <summary>
/// Cama (GDD 2.1.6): recupera toda la vida, de noche salta al amanecer y guarda
/// la partida, pero solo si no hay enemigos cerca ("si está protegido").
/// </summary>
public partial class Bed : Interactable
{
	[ExportCategory("Bed")]
	[ExportGroup("Descanso")]
	// Un zombie a menos de esta distancia (metros) impide dormir.
	[Export] public float SafeRadius = 14.0f;

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

		// Después de saltar al amanecer, para que se guarde el día nuevo.
		WorldState.Instance?.Save(player.GlobalPosition);
		GameEvents.PostMessage("Descansaste. Vida recuperada y partida guardada");
	}

	private DayNightCycle FindDayNight()
	{
		return GetTree().GetFirstNodeInGroup(DayNightCycle.Group) as DayNightCycle;
	}
}
