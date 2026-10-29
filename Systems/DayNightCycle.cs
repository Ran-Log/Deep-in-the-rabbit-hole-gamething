using Godot;

/// <summary>
/// Ciclo día/noche (recurso "Tiempo del Día/Noche" del GDD): de día se
/// explora tranquilo; al caer la noche baja la luz y EnemySpawner manda una
/// oleada de zombies hacia el campamento. Dormir en la cama de noche salta
/// al amanecer (<see cref="SkipToMorning"/>).
///
/// Solo maneja el reloj y la luz; quién reacciona a la noche lo decide cada
/// uno escuchando <see cref="NightStarted"/>/<see cref="DayStarted"/>.
/// Los demás nodos lo encuentran por el grupo "day_night".
/// </summary>
public partial class DayNightCycle : Node
{
	public const string Group = "day_night";

	[Signal]
	public delegate void NightStartedEventHandler(int day);

	[Signal]
	public delegate void DayStartedEventHandler(int day);

	[Export]
	public DirectionalLight3D Sun;

	[Export]
	public WorldEnvironment WorldEnvironment;

	/// <summary>Segundos de día real por día de juego.</summary>
	[Export]
	public float DayLength = 150.0f;

	[Export]
	public float NightLength = 90.0f;

	/// <summary>Segundos que tarda en oscurecer/aclarar, para que la noche no "se prenda" de golpe.</summary>
	[Export]
	public float TransitionSeconds = 8.0f;

	[Export]
	public float DaySunEnergy = 1.0f;

	[Export]
	public float NightSunEnergy = 0.02f;

	[Export]
	public float DayAmbientEnergy = 0.6f;

	[Export]
	public float NightAmbientEnergy = 0.08f;

	private bool isNight;
	private float phaseElapsed;
	private int day = 1;

	/// <summary>0 = día pleno, 1 = noche plena. Se mueve suave hacia el objetivo.</summary>
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

	/// <summary>Termina la noche ya (dormir en la cama). No hace nada de día.</summary>
	public void SkipToMorning()
	{
		if (isNight)
		{
			StartDay();
			darkness = 0.0f;
			ApplyLighting();
		}
	}

	/// <summary>Debug: pasa de día a noche (con su oleada) o de noche a día, al instante.</summary>
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
