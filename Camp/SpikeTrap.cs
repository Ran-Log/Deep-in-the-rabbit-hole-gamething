using Godot;

/// <summary>
/// Trampa de pinchos del campamento (GDD 2.1.8). Cada HitInterval segundos pincha a los enemigos
/// que están encima (Area3D) y gasta un uso; sin usos se rompe. Los pinchos bajan a medida que se gasta.
/// Los usos que le quedan se guardan en WorldState.
/// </summary>
public partial class SpikeTrap : Node3D
{
	[Signal]
	public delegate void TriggeredEventHandler();

	[Signal]
	public delegate void BrokenEventHandler();

	[ExportCategory("SpikeTrap")]
	[ExportGroup("Referencias")]
	[Export] public Area3D Area;
	[Export] public Node3D Spikes;

	[ExportGroup("Trampa")]
	[Export(PropertyHint.Range, "1,50,1")] public int Damage = 3;
	[Export] public float HitInterval = 1.0f;
	// Pinchazos que aguanta antes de romperse.
	[Export(PropertyHint.Range, "1,100,1")] public int MaxUses = 12;

	// Los pone TrapPlacer antes de agregarla. UsesLeft <= 0 = nueva (usos completos).
	public Vector3I Cell { get; set; }

	public int UsesLeft { get; set; }

	private float timer;

	public override void _Ready()
	{
		if (UsesLeft <= 0)
		{
			UsesLeft = MaxUses;
		}

		// Lista para pinchar apenas alguien la pise.
		timer = HitInterval;
		ShowWear();
	}

	public override void _PhysicsProcess(double delta)
	{
		timer += (float)delta;

		if (timer < HitInterval || Area == null)
		{
			return;
		}

		foreach (Node3D body in Area.GetOverlappingBodies())
		{
			if (body is Enemy enemy && UsesLeft > 0)
			{
				Hit(enemy);
				// Solo espera si pinchó: así el próximo enemigo que la pise recibe el golpe al instante.
				timer = 0.0f;
			}
		}
	}

	private void Hit(Enemy enemy)
	{
		enemy.TakeHit(Damage, GlobalPosition);
		UsesLeft--;
		EmitSignal(SignalName.Triggered);

		if (UsesLeft <= 0)
		{
			Break();
			return;
		}

		WorldState.Instance?.SetTrap(Cell, UsesLeft);
		ShowWear();
	}

	private void Break()
	{
		WorldState.Instance?.RemoveTrap(Cell);
		GameEvents.PostMessage("Una trampa del campamento se rompió");
		EmitSignal(SignalName.Broken);
		QueueFree();
	}

	// Pinchos altos = nueva; bajos = casi rota.
	private void ShowWear()
	{
		if (Spikes != null)
		{
			Spikes.Scale = new Vector3(1.0f, 0.35f + 0.65f * UsesLeft / MaxUses, 1.0f);
		}
	}
}
