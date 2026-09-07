using System.Collections.Generic;
using Godot;

/// <summary>
/// Cuenta cuántos bloques de cada mineral minó el jugador. Solo escucha la
/// señal <see cref="MiningComponent.BlockDestroyed"/>; no conoce el mundo ni
/// el HUD (mismo criterio que <see cref="Hud"/>: quien necesite los datos los
/// consulta o se suscribe, esto no empuja nada por su cuenta más que la señal
/// propia).
///
/// Sin UI dedicada todavía — el HUD la lee directamente para el overlay de
/// debug (ver Hud.cs). El objetivo de esta clase es dejar el "gancho" de
/// datos listo para cuando exista un inventario visual o un sistema de
/// crafteo que consuma <see cref="MineralCollected"/>.
/// </summary>
public partial class PlayerInventory : Node
{
	[Signal]
	public delegate void MineralCollectedEventHandler(int mineral, int newTotal);

	[Export]
	public MiningComponent Mining;

	private readonly Dictionary<MineralType, int> counts = new();

	public IReadOnlyDictionary<MineralType, int> Counts => counts;

	public override void _Ready()
	{
		if (Mining == null)
		{
			GD.PushError($"{Name}: falta asignar MiningComponent; el inventario no va a recibir minerales.");
			return;
		}

		Mining.BlockDestroyed += OnBlockDestroyed;
	}

	public override void _ExitTree()
	{
		if (Mining != null)
		{
			Mining.BlockDestroyed -= OnBlockDestroyed;
		}
	}

	public int GetCount(MineralType mineral)
	{
		return counts.TryGetValue(mineral, out int count) ? count : 0;
	}

	private void OnBlockDestroyed(int mineral)
	{
		var type = (MineralType)mineral;
		int newTotal = GetCount(type) + 1;

		counts[type] = newTotal;

		EmitSignal(SignalName.MineralCollected, mineral, newTotal);
	}
}
