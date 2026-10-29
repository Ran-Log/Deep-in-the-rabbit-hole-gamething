using Godot;

/// <summary>
/// Vida genérica: la usan el jugador, el campamento y los enemigos. Solo
/// lleva el número y avisa por señal: quien inflige daño (GasZone, Zombie,
/// CombatComponent) llama <see cref="TakeDamage"/>; quien la muestra (Hud,
/// WorldHud) escucha <see cref="HealthChanged"/>; quien reacciona a la
/// muerte (GameOverScreen, Zombie) escucha <see cref="Died"/>. No sabe nada
/// de quién es dueño — mismo criterio de componentes desacoplados que el
/// resto del proyecto (ver MiningComponent/Hud).
///
/// Reemplaza a PlayerHealth: era lo mismo pero atado al nombre "jugador",
/// y el campamento y los enemigos también necesitan vida.
/// </summary>
public partial class HealthComponent : Node
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

	public bool IsDead => dead;

	public bool IsFull => current >= MaxHealth;

	public override void _Ready()
	{
		current = MaxHealth;
	}

	/// <summary>No hace nada si ya está muerto, para no re-emitir Died mientras se procesa la muerte.</summary>
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

	/// <summary>Recupera vida sin pasar del máximo (comer, descansar).</summary>
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
