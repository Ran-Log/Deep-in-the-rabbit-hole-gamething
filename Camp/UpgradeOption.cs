using System;
using System.Collections.Generic;

/// <summary>
/// Una fila del menú de la estación: qué se mejora, a qué, cuánto cuesta y cómo se aplica.
/// Las arma UpgradeTable; el menú solo las muestra.
/// </summary>
public sealed class UpgradeOption
{
	public UpgradeOption(string title, string current, string next, IReadOnlyDictionary<string, int> cost, Func<PlayerEquipment, bool> apply)
	{
		Title = title;
		Current = current;
		Next = next;
		Cost = cost;
		Apply = apply;
	}

	// "Pico", "Arma", "Antorchas".
	public string Title { get; }

	public string Current { get; }

	// Vacío = ya está al máximo.
	public string Next { get; }

	public IReadOnlyDictionary<string, int> Cost { get; }

	public Func<PlayerEquipment, bool> Apply { get; }

	public bool IsMax => string.IsNullOrEmpty(Next);
}
