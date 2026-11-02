using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;

/// <summary>
/// Pantalla del diario (J). Arriba se elige un objetivo (sección, qué, cuánto); al medio la lista,
/// que se tacha sola y se pone verde si te pasas; abajo, todo lo de la sesión. Los datos están en
/// SessionJournal; esta pantalla solo los muestra. Como la mochila, no pausa el juego.
/// </summary>
public partial class JournalScreen : CanvasLayer
{
	private static readonly (GoalKind Kind, string Label)[] Sections =
	{
		(GoalKind.Collect, "Recolección"),
		(GoalKind.Upgrade, "Mejora"),
		(GoalKind.Build, "Construcción"),
		(GoalKind.Explore, "Exploración"),
		(GoalKind.Defeat, "Combate"),
	};

	private static readonly MineralType[] CollectableMinerals =
	{
		MineralType.Stone, MineralType.Coal, MineralType.Iron, MineralType.Gold,
	};

	[ExportCategory("JournalScreen")]
	[ExportGroup("Nodos de UI")]
	[Export] public Control Panel;
	[Export] public OptionButton SectionOption;
	[Export] public OptionButton SubjectOption;
	[Export] public SpinBox AmountSpin;
	[Export] public Button AddButton;
	[Export] public VBoxContainer GoalsContainer;
	[Export] public RichTextLabel SummaryLabel;
	[Export] public Button CloseButton;

	[ExportGroup("Colores")]
	[Export] public Color DoneColor = new Color(0.6f, 0.6f, 0.6f);
	[Export] public Color ExceededColor = new Color(0.45f, 1.0f, 0.5f);

	public bool IsOpen => Panel != null && Panel.Visible;

	public override void _Ready()
	{
		if (Panel == null || SectionOption == null || SubjectOption == null || AmountSpin == null
			|| AddButton == null || GoalsContainer == null || SummaryLabel == null || CloseButton == null)
		{
			GD.PushError($"{Name}: faltan nodos por asignar.");
			return;
		}

		foreach (var (_, label) in Sections)
		{
			SectionOption.AddItem(label);
		}

		SectionOption.ItemSelected += OnSectionSelected;
		AddButton.Pressed += OnAddPressed;
		CloseButton.Pressed += Close;

		if (SessionJournal.Instance != null)
		{
			SessionJournal.Instance.Changed += RefreshIfOpen;
		}

		OnSectionSelected(0);
		Panel.Visible = false;
	}

	public override void _ExitTree()
	{
		if (SessionJournal.Instance != null)
		{
			SessionJournal.Instance.Changed -= RefreshIfOpen;
		}
	}

	// Esc cierra el diario antes que abrir la pausa (este nodo va después de PauseMenu en main.tscn).
	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("toggle_journal"))
		{
			if (IsOpen)
			{
				Close();
			}
			else if (CrosshairRay.PlayerHasControl())
			{
				Open();
			}

			GetViewport().SetInputAsHandled();
		}
		else if (IsOpen && @event.IsActionPressed("ui_cancel"))
		{
			Close();
			GetViewport().SetInputAsHandled();
		}
	}

	public void Open()
	{
		Panel.Visible = true;
		Input.MouseMode = Input.MouseModeEnum.Visible;
		Refresh();
	}

	public void Close()
	{
		if (!IsOpen)
		{
			return;
		}

		Panel.Visible = false;
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	// Público para los tests: lo mismo que el botón "Agregar".
	public void AddGoal(GoalKind kind, string subject, int amount)
	{
		SessionJournal.Instance?.AddGoal(kind, subject, amount);
		Refresh();
	}

	// Qué se puede elegir en cada sección: (id que usa el juego, texto que se muestra).
	public static List<(string Id, string Label)> SubjectsFor(GoalKind kind)
	{
		return kind switch
		{
			GoalKind.Collect => CollectableMinerals
				.Select(mineral => (ItemCatalog.IdOf(mineral), ItemCatalog.DisplayName(ItemCatalog.IdOf(mineral))))
				.ToList(),
			GoalKind.Upgrade => UpgradeTable.Lines.Select(line => (line, line)).ToList(),
			GoalKind.Build => BuildComponent.Options.Select(option => (option.Name, option.Name)).ToList(),
			GoalKind.Explore => new List<(string, string)> { (SessionJournal.RoomsSubject, "Descubrir salas") },
			_ => new List<(string, string)> { (SessionJournal.EnemiesSubject, "Derrotar enemigos") },
		};
	}

	private void OnSectionSelected(long index)
	{
		SubjectOption.Clear();

		var subjects = SubjectsFor(Sections[index].Kind);

		for (int i = 0; i < subjects.Count; i++)
		{
			SubjectOption.AddItem(subjects[i].Label);
			SubjectOption.SetItemMetadata(i, subjects[i].Id);
		}

		// Exploración y combate tienen una sola opción.
		SubjectOption.Disabled = subjects.Count == 1;
	}

	private void OnAddPressed()
	{
		GoalKind kind = Sections[SectionOption.Selected].Kind;
		string subject = SubjectOption.GetItemMetadata(SubjectOption.Selected).AsString();

		AddGoal(kind, subject, (int)AmountSpin.Value);
	}

	private void RefreshIfOpen()
	{
		if (IsOpen)
		{
			Refresh();
		}
	}

	private void Refresh()
	{
		Journal journal = SessionJournal.Instance?.Journal;

		if (journal == null || GoalsContainer == null)
		{
			return;
		}

		foreach (Node child in GoalsContainer.GetChildren())
		{
			GoalsContainer.RemoveChild(child);
			child.QueueFree();
		}

		foreach (JournalGoal goal in journal.Goals)
		{
			GoalsContainer.AddChild(BuildRow(goal));
		}

		if (journal.Goals.Count == 0)
		{
			GoalsContainer.AddChild(new Label { Text = "(sin objetivos: elige uno arriba)" });
		}

		SummaryLabel.Text = BuildSummary(journal);
	}

	/// <summary>Texto con el progreso, meta editable y botón para quitarlo.</summary>
	private Control BuildRow(JournalGoal goal)
	{
		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 10);

		var text = new RichTextLabel
		{
			BbcodeEnabled = true,
			FitContent = true,
			ScrollActive = false,
			CustomMinimumSize = new Vector2(420, 0),
			Text = GoalText(goal),
		};
		row.AddChild(text);

		row.AddChild(new Label { Text = "Meta" });

		var target = new SpinBox { MinValue = 1, MaxValue = 999, Value = goal.Target };
		target.ValueChanged += value =>
		{
			SessionJournal.Instance?.SetTarget(goal, (int)value);
			text.Text = GoalText(goal);
		};
		row.AddChild(target);

		var remove = new Button { Text = "Quitar" };
		remove.Pressed += () => SessionJournal.Instance?.RemoveGoal(goal);
		row.AddChild(remove);

		return row;
	}

	// GDD: se tacha al cumplirse; en verde si se hizo más de lo planeado.
	private string GoalText(JournalGoal goal)
	{
		string text = $"{SessionJournal.Describe(goal)}   {goal.Progress}/{goal.Target}";

		if (goal.IsExceeded)
		{
			return $"[color=#{ExceededColor.ToHtml(false)}][s]{text}[/s]  (+{goal.Progress - goal.Target})[/color]";
		}

		if (goal.IsDone)
		{
			return $"[color=#{DoneColor.ToHtml(false)}][s]{text}[/s][/color]";
		}

		return $"{text}   (faltan {goal.Remaining})";
	}

	// Todo lo de la sesión, por sección.
	private static string BuildSummary(Journal journal)
	{
		var text = new StringBuilder();

		foreach (var (kind, label) in Sections)
		{
			var entries = journal.SessionTotals
				.Where(entry => entry.Key.Kind == kind)
				.Select(entry => $"{entry.Value} {SessionJournal.SubjectName(kind, entry.Key.Subject)}")
				.ToList();

			if (entries.Count > 0)
			{
				text.AppendLine($"[b]{label}:[/b] {string.Join(", ", entries)}");
			}
		}

		return text.Length > 0 ? text.ToString() : "Todavía nada en esta sesión.";
	}
}
