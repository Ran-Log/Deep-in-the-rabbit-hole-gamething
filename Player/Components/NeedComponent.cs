using Godot;

/// <summary>
/// Hambre o sed (GDD 2.1.11): baja con el tiempo y en 0 quita vida de a poco.
/// El jugador tiene dos (Hunger y Thirst), igual que HealthComponent se reusa en varios lados.
/// </summary>
public partial class NeedComponent : Node
{
	[Signal]
	public delegate void ValueChangedEventHandler(int current, int max);

	[ExportCategory("NeedComponent")]
	[ExportGroup("Referencias")]
	// A quién le quita vida cuando llega a 0.
	[Export] public HealthComponent Health;

	[ExportGroup("Necesidad")]
	[Export] public string DisplayName = "Hambre";
	[Export] public int MaxValue = 100;
	// Cuánto baja por minuto.
	[Export] public float DrainPerMinute = 10.0f;

	[ExportGroup("En cero")]
	[Export] public int DamageWhenEmpty = 2;
	[Export] public float DamageIntervalSeconds = 3.0f;
	[Export] public string EmptyMessage = "Tienes mucha hambre: pierdes vida y no puedes correr";

	[ExportGroup("Guardado")]
	// Se guarda junto con la vida (WorldState lo busca en el mismo grupo).
	[Export] public string SaveId = "";

	private float value;
	private float damageTimer;

	public int Current => Mathf.CeilToInt(value);

	public bool IsEmpty => value <= 0.0f;

	// Por el número que se ve, así "100" cuenta como lleno.
	public bool IsFull => Current >= MaxValue;

	public override void _Ready()
	{
		value = MaxValue;

		if (string.IsNullOrEmpty(SaveId))
		{
			return;
		}

		AddToGroup(HealthComponent.SavedGroup);

		if (WorldState.Instance != null && WorldState.Instance.TryGetSavedHealth(SaveId, out int saved))
		{
			value = Mathf.Clamp(saved, 0, MaxValue);
		}
	}

	public override void _Process(double delta)
	{
		Tick(delta);
	}

	// Público para poder adelantar el tiempo en los tests.
	public void Tick(double delta)
	{
		if (Health != null && Health.IsDead)
		{
			return;
		}

		int before = Current;
		bool wasEmpty = IsEmpty;

		value = Mathf.Max(0.0f, value - DrainPerMinute * (float)delta / 60.0f);

		if (Current != before)
		{
			EmitSignal(SignalName.ValueChanged, Current, MaxValue);
		}

		if (!IsEmpty)
		{
			damageTimer = 0.0f;
			return;
		}

		if (!wasEmpty)
		{
			GameEvents.PostMessage(EmptyMessage);
		}

		damageTimer += (float)delta;

		if (damageTimer >= DamageIntervalSeconds)
		{
			damageTimer -= DamageIntervalSeconds;
			Health?.TakeDamage(DamageWhenEmpty);
		}
	}

	// Comer o beber. No pasa del máximo.
	public void Restore(int amount)
	{
		if (amount <= 0)
		{
			return;
		}

		value = Mathf.Min(MaxValue, value + amount);
		damageTimer = 0.0f;
		EmitSignal(SignalName.ValueChanged, Current, MaxValue);
	}
}
