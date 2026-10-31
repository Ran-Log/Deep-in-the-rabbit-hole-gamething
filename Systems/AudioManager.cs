using Godot;

/// <summary>
/// Autoload de audio, como Observer (clase 11, Axy 8): escucha las señales del juego
/// (minar, golpes, daño, cofres, noche...) y reproduce el sonido. Ningún script del juego
/// conoce al AudioManager. Engancha a cada nodo cuando entra al árbol (SceneTree.NodeAdded),
/// así también funciona con lo que aparece después (enemigos, cofres, botones).
/// </summary>
public partial class AudioManager : Node
{
	public static AudioManager Instance { get; private set; }

	private const string SfxBus = "SFX";
	private const string MusicBus = "Music";
	private const int SfxVoices = 10;

	[ExportCategory("AudioManager")]
	[ExportGroup("Minado")]
	[Export] public AudioStream MineHit;
	[Export] public AudioStream BlockBreak;
	[Export] public AudioStream Pickup;
	[Export] public AudioStream HitRejected;

	[ExportGroup("Combate")]
	[Export] public AudioStream EnemyHit;
	[Export] public AudioStream EnemyDeath;
	[Export] public AudioStream PlayerHurt;
	[Export] public AudioStream PlayerDeath;

	[ExportGroup("Mundo")]
	[Export] public AudioStream ChestOpen;
	[Export] public AudioStream NoteOpen;
	[Export] public AudioStream Eat;
	[Export] public AudioStream Upgrade;
	[Export] public AudioStream Build;
	[Export] public AudioStream NightFalls;
	[Export] public AudioStream Victory;

	[ExportGroup("Interfaz")]
	[Export] public AudioStream UiClick;

	[ExportGroup("Ambiente")]
	[Export] public AudioStream DayAmbience;
	[Export] public AudioStream NightAmbience;
	[Export] public float AmbienceFadeSeconds = 3.0f;

	private readonly AudioStreamPlayer[] voices = new AudioStreamPlayer[SfxVoices];
	private int nextVoice;
	private AudioStreamPlayer dayPlayer;
	private AudioStreamPlayer nightPlayer;
	private int lastPlayerHealth = -1;

	// El último sonido que se pidió (para los tests).
	public AudioStream LastPlayed { get; private set; }

	public override void _EnterTree()
	{
		Instance = this;
	}

	public override void _ExitTree()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	public override void _Ready()
	{
		// Sigue sonando con el juego en pausa (los clics del menú de pausa).
		ProcessMode = ProcessModeEnum.Always;

		CreateVoices();
		CreateAmbiencePlayers();

		GetTree().NodeAdded += OnNodeAdded;

		if (GameEvents.Instance != null)
		{
			GameEvents.Instance.NoteOpened += _ => Play(NoteOpen);
			GameEvents.Instance.LevelCompleted += () => Play(Victory);
		}

		SetNight(false);
	}

	public void Play(AudioStream stream, float volumeDb = 0.0f)
	{
		if (stream == null)
		{
			return;
		}

		LastPlayed = stream;

		AudioStreamPlayer voice = voices[nextVoice];
		nextVoice = (nextVoice + 1) % voices.Length;

		voice.Stream = stream;
		voice.VolumeDb = volumeDb;
		voice.Play();
	}

	// Cada tipo de nodo avisa cosas distintas; acá se decide qué sonido corresponde a cada aviso.
	private void OnNodeAdded(Node node)
	{
		switch (node)
		{
			case MiningComponent mining:
				mining.BlockHit += _ => Play(MineHit);
				mining.HitRejected += _ => Play(HitRejected);
				mining.BlockDestroyed += _ => { Play(BlockBreak); Play(Pickup, -8.0f); };
				break;

			case CombatComponent combat:
				combat.EnemyHit += _ => Play(EnemyHit);
				break;

			case Enemy enemy when enemy.Health != null:
				enemy.Health.Died += () => Play(EnemyDeath);
				break;

			case HealthComponent health when health.SaveId == WorldSaveState.PlayerHealthId:
				// Después de su _Ready (puede venir de una partida guardada).
				Callable.From(() => lastPlayerHealth = health.Current).CallDeferred();
				health.HealthChanged += OnPlayerHealthChanged;
				health.Died += () => Play(PlayerDeath);
				break;

			case Chest chest:
				chest.Opened += () => Play(ChestOpen);
				break;

			case EatComponent eat:
				eat.Ate += () => Play(Eat);
				break;

			case PlayerEquipment equipment:
				equipment.ToolChanged += _ => Play(Upgrade);
				break;

			case BuildComponent build:
				build.StructureBuilt += () => Play(Build);
				break;

			case DayNightCycle dayNight:
				dayNight.NightStarted += _ => { Play(NightFalls); SetNight(true); };
				dayNight.DayStarted += _ => SetNight(false);
				SetNight(false);
				break;

			case BaseButton button:
				button.Pressed += () => Play(UiClick, -4.0f);
				break;
		}
	}

	// Solo suena cuando la vida baja (no al curarse).
	private void OnPlayerHealthChanged(int current, int max)
	{
		if (lastPlayerHealth >= 0 && current < lastPlayerHealth && current > 0)
		{
			Play(PlayerHurt);
		}

		lastPlayerHealth = current;
	}

	// Cambia el ambiente de día a noche con un fundido (Tween).
	private void SetNight(bool night)
	{
		AudioStreamPlayer fadeIn = night ? nightPlayer : dayPlayer;
		AudioStreamPlayer fadeOut = night ? dayPlayer : nightPlayer;

		if (fadeIn.Stream == null)
		{
			return;
		}

		if (!fadeIn.Playing)
		{
			fadeIn.VolumeDb = -40.0f;
			fadeIn.Play();
		}

		Tween tween = CreateTween().SetParallel();
		tween.TweenProperty(fadeIn, "volume_db", 0.0f, AmbienceFadeSeconds);
		tween.TweenProperty(fadeOut, "volume_db", -40.0f, AmbienceFadeSeconds);
	}

	private void CreateVoices()
	{
		for (int i = 0; i < voices.Length; i++)
		{
			voices[i] = new AudioStreamPlayer { Bus = SfxBus };
			AddChild(voices[i]);
		}
	}

	private void CreateAmbiencePlayers()
	{
		dayPlayer = new AudioStreamPlayer { Bus = MusicBus, Stream = Looping(DayAmbience) };
		nightPlayer = new AudioStreamPlayer { Bus = MusicBus, Stream = Looping(NightAmbience), VolumeDb = -40.0f };
		AddChild(dayPlayer);
		AddChild(nightPlayer);
	}

	private static AudioStream Looping(AudioStream stream)
	{
		if (stream is AudioStreamOggVorbis ogg)
		{
			ogg.Loop = true;
		}

		return stream;
	}
}
