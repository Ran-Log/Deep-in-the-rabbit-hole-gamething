using Godot;

/// <summary>
/// Ciclo día y noche (GDD): maneja el reloj y la luz. Quien reacciona a la noche
/// (como EnemySpawner) escucha NightStarted y DayStarted.
/// </summary>
public partial class DayNightCycle : Node
{
	public const string Group = "day_night";

	[Signal]
	public delegate void NightStartedEventHandler(int day);

	[Signal]
	public delegate void DayStartedEventHandler(int day);

	[ExportCategory("DayNightCycle")]
	[ExportGroup("Referencias")]
	[Export] public DirectionalLight3D Sun;
	[Export] public WorldEnvironment WorldEnvironment;

	[ExportGroup("Tiempos")]
	// En segundos reales.
	[Export] public float DayLength = 150.0f;
	[Export] public float NightLength = 90.0f;
	// Cuánto tarda en oscurecer o aclarar.
	[Export] public float TransitionSeconds = 8.0f;

	[ExportGroup("Luz")]
	// El sol solo entra por los tragaluces (las salas tienen techo).
	[Export] public float DaySunEnergy = 1.4f;
	[Export] public float NightSunEnergy = 0.02f;
	// Ambiente bajo a propósito: la mina es oscura (GDD) y hace falta luz para ver.
	[Export] public float DayAmbientEnergy = 0.15f;
	[Export] public float NightAmbientEnergy = 0.04f;

	private bool isNight;
	private float phaseElapsed;
	private int day = 1;

	// 0 = día pleno, 1 = noche plena.
	private float darkness;

	public bool IsNight => isNight;

	public int Day => day;

	public float SecondsLeftInPhase => Mathf.Max(0.0f, (isNight ? NightLength : DayLength) - phaseElapsed);

	public override void _EnterTree()
	{
		AddToGroup(Group);
	}

	public override void _Ready()
	{
		// Una partida cargada sigue en su día (siempre desde el comienzo de ese día).
		day = WorldState.Instance?.SavedDay ?? 1;

		ApplyLevelColors(GameData.CurrentLevel);
		ApplyLighting();
	}

	public override void _Process(double delta)
	{
		phaseElapsed += (float)delta;

		if (phaseElapsed >= (isNight ? NightLength : DayLength))
		{
			if (isNight)
			{
				StartDay();
			}
			else
			{
				StartNight();
			}
		}

		float target = isNight ? 1.0f : 0.0f;
		darkness = Mathf.MoveToward(darkness, target, (float)delta / Mathf.Max(0.01f, TransitionSeconds));

		ApplyLighting();
	}

	// Al dormir en la cama. De día no hace nada.
	public void SkipToMorning()
	{
		if (isNight)
		{
			StartDay();
			darkness = 0.0f;
			ApplyLighting();
		}
	}

	// Debug (F2): cambia de día a noche (con oleada) o al revés, al instante.
	public void TogglePhaseDebug()
	{
		if (isNight)
		{
			SkipToMorning();
			return;
		}

		StartNight();
		darkness = 1.0f;
		ApplyLighting();
	}

	private void StartNight()
	{
		isNight = true;
		phaseElapsed = 0.0f;

		GameEvents.PostMessage("Cae la noche... algo se mueve en los túneles");
		EmitSignal(SignalName.NightStarted, day);
	}

	private void StartDay()
	{
		isNight = false;
		phaseElapsed = 0.0f;
		day++;

		GameEvents.PostMessage($"Amanece. Día {day}");
		EmitSignal(SignalName.DayStarted, day);
	}

	// Cada nivel tiene su color de luz (VISUALS.md: cálido arriba, frío y oscuro al bajar).
	private void ApplyLevelColors(LevelDefinition level)
	{
		if (level == null)
		{
			return;
		}

		if (Sun != null)
		{
			Sun.LightColor = level.SunColor;
		}

		Godot.Environment env = WorldEnvironment?.Environment;

		if (env != null)
		{
			env.AmbientLightColor = level.AmbientColor;
			// Mezcla: 25% la luz del cielo y 75% el color del nivel.
			env.AmbientLightSkyContribution = 0.25f;
		}
	}

	private void ApplyLighting()
	{
		if (Sun != null)
		{
			Sun.LightEnergy = Mathf.Lerp(DaySunEnergy, NightSunEnergy, darkness);
		}

		Godot.Environment env = WorldEnvironment?.Environment;

		if (env != null)
		{
			env.AmbientLightEnergy = Mathf.Lerp(DayAmbientEnergy, NightAmbientEnergy, darkness);
			env.BackgroundEnergyMultiplier = Mathf.Lerp(1.0f, 0.05f, darkness);
		}
	}
}
