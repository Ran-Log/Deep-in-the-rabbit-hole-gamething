using Godot;

/// <summary>
/// HUD del mundo: hora del día y vida del campamento. Está en main.tscn porque
/// necesita nodos que solo existen ahí (DayNightCycle y el campamento).
/// </summary>
public partial class WorldHud : CanvasLayer
{
	[ExportCategory("WorldHud")]
	[ExportGroup("Referencias")]
	[Export] public DayNightCycle DayNight;
	[Export] public HealthComponent CampHealth;

	[ExportGroup("Nodos de UI")]
	[Export] public Label TimeLabel;
	[Export] public Label CampLabel;

	public override void _Ready()
	{
		if (CampHealth != null)
		{
			CampHealth.HealthChanged += OnCampHealthChanged;
			OnCampHealthChanged(CampHealth.MaxHealth, CampHealth.MaxHealth);
		}
	}

	public override void _ExitTree()
	{
		if (CampHealth != null)
		{
			CampHealth.HealthChanged -= OnCampHealthChanged;
		}
	}

	public override void _Process(double delta)
	{
		if (DayNight == null || TimeLabel == null)
		{
			return;
		}

		int secondsLeft = Mathf.CeilToInt(DayNight.SecondsLeftInPhase);
		string phase = DayNight.IsNight ? "Noche" : "Día";
		string next = DayNight.IsNight ? "amanecer" : "anochecer";

		TimeLabel.Text = $"Día {DayNight.Day} — {phase}   ({secondsLeft / 60}:{secondsLeft % 60:00} para el {next})";
	}

	private void OnCampHealthChanged(int current, int max)
	{
		if (CampLabel != null)
		{
			CampLabel.Text = $"Campamento: {current}/{max}";
		}
	}
}
