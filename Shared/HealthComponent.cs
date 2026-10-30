using Godot;

/// <summary>
/// Componente de vida del jugador, el campamento y los enemigos. Quien hace daño
/// llama TakeDamage; el HUD y quien reacciona a la muerte escuchan sus señales.
/// </summary>
public partial class HealthComponent : Node
{
	[Signal]
	public delegate void HealthChangedEventHandler(int current, int max);

	[Signal]
	public delegate void DiedEventHandler();

	[ExportCategory("HealthComponent")]
	[ExportGroup("Vida")]
	[Export] public int MaxHealth = 100;

	private int current;
	private bool dead;

	public int Current => current;

	public bool IsDead => dead;

	public bool IsFull => current >= MaxHealth;

	public override void _Ready()
	{
		current = MaxHealth;
	}

	// Si ya está muerto no hace nada, para no emitir Died dos veces.
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

	// Comer o descansar. No pasa del máximo.
	public void Heal(int amount)
	{
		if (dead || amount <= 0)
		{
			return;
		}

		current = Mathf.Min(MaxHealth, current + amount);

		EmitSignal(SignalName.HealthChanged, current, MaxHealth);
	}

	public void FullHeal()
	{
		current = MaxHealth;
		dead = false;
		EmitSignal(SignalName.HealthChanged, current, MaxHealth);
	}
}
