using Godot;

/// <summary>
/// Vida del jugador. Solo lleva el número y avisa por señal: quien inflige
/// daño (ej. GasZone) llama <see cref="TakeDamage"/>; quien la muestra
/// (Hud) escucha <see cref="HealthChanged"/>; quien reacciona a la muerte
/// (GameOverScreen) escucha <see cref="Died"/>. No sabe nada de gas, HUD ni
/// pantallas — mismo criterio de componentes desacoplados que el resto del
/// proyecto (ver MiningComponent/Hud).
/// </summary>
public partial class PlayerHealth : Node
{
	[Signal]
	public delegate void HealthChangedEventHandler(int current, int max);

	[Signal]
	public delegate void DiedEventHandler();

	[Export]
	public int MaxHealth = 100;

	private int current;
	private bool dead;

	public int Current => current;

	public override void _Ready()
	{
		current = MaxHealth;
	}

	/// <summary>No hace nada si ya está muerto, para no re-emitir Died mientras se procesa el Game Over.</summary>
	public void TakeDamage(int amount)
	{
		if (dead || amount <= 0)
		{
			return;
		}

		current = Mathf.Max(0, current - amount);

		EmitSignal(SignalName.HealthChanged, current, MaxHealth);

		if (current <= 0)
		{
			dead = true;
			EmitSignal(SignalName.Died);
		}
	}

	/// <summary>Restaura la vida completa (ej. al reaparecer después de un Game Over).</summary>
	public void FullHeal()
	{
		current = MaxHealth;
		dead = false;
		EmitSignal(SignalName.HealthChanged, current, MaxHealth);
	}
}
