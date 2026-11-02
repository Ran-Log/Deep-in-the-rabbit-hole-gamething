using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Secciones del diario (GDD: "recolección, mejora, construcción, etc.").</summary>
public enum GoalKind
{
	Collect,
	Upgrade,
	Build,
	Explore,
	Defeat
}

/// <summary>Un objetivo que eligió el jugador, ej. "recolectar 5 de oro".</summary>
public sealed class JournalGoal
{
	public JournalGoal(GoalKind kind, string subject, int target)
	{
		Kind = kind;
		Subject = subject;
		Target = target;
	}

	public GoalKind Kind { get; }

	// Qué: el id del mineral, la línea de mejora ("Pico"), la construcción ("Barricada")...
	public string Subject { get; }

	public int Target { get; set; }

	// Cuenta desde que se agregó el objetivo.
	public int Progress { get; set; }

	public bool IsDone => Progress >= Target;

	// GDD: "se muestra en verde si realiza más de lo que había planeado".
	public bool IsExceeded => Progress > Target;

	public int Remaining => Math.Max(0, Target - Progress);
}

/// <summary>
/// El diario de la sesión (GDD, Desafíos): el jugador elige objetivos, se tachan solos al cumplirlos,
/// puede cambiar la meta (cuenta lo que ya hizo) y ve un resumen de todo lo de la sesión.
/// C# puro, para probarlo en el harness. No se guarda: es por sesión.
/// </summary>
public sealed class Journal
{
	private readonly List<JournalGoal> goals = new();
	private readonly Dictionary<(GoalKind Kind, string Subject), int> sessionTotals = new();

	public IReadOnlyList<JournalGoal> Goals => goals;

	public IReadOnlyDictionary<(GoalKind Kind, string Subject), int> SessionTotals => sessionTotals;

	// Si ya hay un objetivo igual, solo se le cambia la meta (no se duplica).
	public JournalGoal AddGoal(GoalKind kind, string subject, int target)
	{
		JournalGoal existing = goals.FirstOrDefault(goal => goal.Kind == kind && goal.Subject == subject);

		if (existing != null)
		{
			SetTarget(existing, target);
			return existing;
		}

		var created = new JournalGoal(kind, subject, Math.Max(1, target));
		goals.Add(created);
		return created;
	}

	// Cambiar la meta no borra lo hecho: se descuenta la diferencia.
	public void SetTarget(JournalGoal goal, int target)
	{
		goal.Target = Math.Max(1, target);
	}

	public bool RemoveGoal(JournalGoal goal) => goals.Remove(goal);

	// Algo pasó en el juego. Devuelve los objetivos que se cumplieron justo ahora (para avisar una vez).
	public List<JournalGoal> Record(GoalKind kind, string subject, int amount = 1)
	{
		var justDone = new List<JournalGoal>();

		if (amount <= 0)
		{
			return justDone;
		}

		sessionTotals[(kind, subject)] = SessionTotal(kind, subject) + amount;

		foreach (JournalGoal goal in goals.Where(goal => goal.Kind == kind && goal.Subject == subject))
		{
			bool wasDone = goal.IsDone;
			goal.Progress += amount;

			if (!wasDone && goal.IsDone)
			{
				justDone.Add(goal);
			}
		}

		return justDone;
	}

	public int SessionTotal(GoalKind kind, string subject) => sessionTotals.TryGetValue((kind, subject), out int total) ? total : 0;

	public void Clear()
	{
		goals.Clear();
		sessionTotals.Clear();
	}
}
