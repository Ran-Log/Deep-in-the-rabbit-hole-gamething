using Godot;

/// <summary>
/// Autoload con el diario de la sesión (GDD: "se reinicia cada vez que entra y sale del juego").
/// Es un autoload para que sobreviva a morir, bajar de nivel o volver al menú, pero no se guarda.
/// Como AudioManager (Observer): escucha las señales del juego y anota en el Journal.
/// </summary>
public partial class SessionJournal : Node
{
	public static SessionJournal Instance { get; private set; }

	public const string RoomsSubject = "Salas";
	public const string EnemiesSubject = "Enemigos";

	// Cambió algo (objetivo nuevo, progreso...): la pantalla del diario se redibuja.
	[Signal]
	public delegate void ChangedEventHandler();

	[Signal]
	public delegate void GoalCompletedEventHandler(string description);

	public Journal Journal { get; } = new();

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
		GetTree().NodeAdded += OnNodeAdded;

		if (WorldState.Instance != null)
		{
			WorldState.Instance.Upgraded += (line, levels) => Record(GoalKind.Upgrade, line, levels);
			WorldState.Instance.RoomDiscovered += OnRoomDiscovered;
		}
	}

	public JournalGoal AddGoal(GoalKind kind, string subject, int target)
	{
		JournalGoal goal = Journal.AddGoal(kind, subject, target);
		EmitSignal(SignalName.Changed);
		return goal;
	}

	// No emite Changed: la fila que se está editando se actualiza sola (si no, perdería el foco).
	public void SetTarget(JournalGoal goal, int target)
	{
		Journal.SetTarget(goal, target);
	}

	public void RemoveGoal(JournalGoal goal)
	{
		Journal.RemoveGoal(goal);
		EmitSignal(SignalName.Changed);
	}

	public void Record(GoalKind kind, string subject, int amount = 1)
	{
		foreach (JournalGoal goal in Journal.Record(kind, subject, amount))
		{
			string description = Describe(goal);
			GameEvents.PostMessage($"Objetivo cumplido: {description}");
			EmitSignal(SignalName.GoalCompleted, description);
		}

		EmitSignal(SignalName.Changed);
	}

	// "Recolectar 5 Oro", "Mejorar Pico 1 nivel", "Construir 2 Barricada"...
	public static string Describe(JournalGoal goal)
	{
		string subject = SubjectName(goal.Kind, goal.Subject);

		return goal.Kind switch
		{
			GoalKind.Collect => $"Recolectar {goal.Target} {subject}",
			GoalKind.Upgrade => $"Mejorar {subject} {goal.Target} {(goal.Target == 1 ? "nivel" : "niveles")}",
			GoalKind.Build => $"Construir {goal.Target} {subject}",
			GoalKind.Explore => $"Descubrir {goal.Target} {(goal.Target == 1 ? "sala" : "salas")}",
			GoalKind.Defeat => $"Derrotar {goal.Target} {(goal.Target == 1 ? "enemigo" : "enemigos")}",
			_ => subject,
		};
	}

	// Los minerales se guardan por id ("Gold"); se muestran en castellano.
	public static string SubjectName(GoalKind kind, string subject)
	{
		return kind == GoalKind.Collect ? ItemCatalog.DisplayName(subject) : subject;
	}

	private void OnRoomDiscovered(Vector2I room)
	{
		// La sala del campamento no cuenta: ahí se empieza.
		if (room != Vector2I.Zero)
		{
			Record(GoalKind.Explore, RoomsSubject);
		}
	}

	private void OnNodeAdded(Node node)
	{
		switch (node)
		{
			case MiningComponent mining:
				mining.MineralCollected += itemId => Record(GoalKind.Collect, itemId);
				break;

			case BuildComponent build:
				build.StructureBuilt += name => Record(GoalKind.Build, name);
				break;

			case Enemy enemy when enemy.Health != null:
				enemy.Health.Died += () => Record(GoalKind.Defeat, EnemiesSubject);
				break;
		}
	}
}
