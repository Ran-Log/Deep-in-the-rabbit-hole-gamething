using System.Collections.Generic;
using Godot;

/// <summary>
/// Nido de hormigas gigantes (GDD): genera hormigas mientras el jugador está cerca.
/// Si se destruye, dejan de aparecer para siempre (queda guardado por sala).
/// </summary>
public partial class AntNest : StaticBody3D, IDamageable
{
	[ExportCategory("AntNest")]
	[ExportGroup("Referencias")]
	[Export] public HealthComponent Health;
	[Export] public PackedScene AntScene;

	[ExportGroup("Hormigas")]
	[Export] public float SpawnInterval = 8.0f;
	[Export] public int MaxAnts = 4;
	// Solo genera hormigas si el jugador está a menos de esta distancia (metros).
	[Export] public float ActivationRadius = 22.0f;

	private readonly List<Node3D> ants = new();
	private readonly RandomNumberGenerator rng = new();
	private float timer;
	private Node3D player;

	// La pone Room al crearlo, para guardar que se destruyó.
	public Vector2I RoomCoord { get; set; }

	public override void _Ready()
	{
		rng.Randomize();
		player = GetTree().GetFirstNodeInGroup(Enemy.PlayerGroup) as Node3D;

		if (Health != null)
		{
			Health.Died += OnDied;
		}
	}

	public override void _ExitTree()
	{
		if (Health != null)
		{
			Health.Died -= OnDied;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		timer += (float)delta;

		if (timer < SpawnInterval)
		{
			return;
		}

		timer = 0.0f;
		ants.RemoveAll(ant => !IsInstanceValid(ant));

		if (ants.Count < MaxAnts && PlayerIsNear())
		{
			SpawnAnt();
		}
	}

	public void TakeHit(int damage, Vector3 fromPosition)
	{
		Health?.TakeDamage(damage);
	}

	private bool PlayerIsNear()
	{
		return IsInstanceValid(player) && player.GlobalPosition.DistanceTo(GlobalPosition) < ActivationRadius;
	}

	private void SpawnAnt()
	{
		if (AntScene == null)
		{
			return;
		}

		var ant = AntScene.Instantiate<Enemy>();
		float angle = rng.RandfRange(0.0f, Mathf.Tau);

		// Al lado del nido, en la misma sala (si la sala se descarga, sus hormigas también).
		ant.Position = Position + new Vector3(Mathf.Cos(angle) * 2.0f, 0.5f, Mathf.Sin(angle) * 2.0f);
		GetParent().AddChild(ant);

		if (ant.Behavior is AntBehavior antBehavior)
		{
			antBehavior.Home = GlobalPosition;
		}

		ants.Add(ant);
	}

	private void OnDied()
	{
		WorldState.Instance?.MarkNestDestroyed(RoomCoord);
		GameEvents.PostMessage("Destruiste el nido: de aquí no saldrán más hormigas");
		QueueFree();
	}
}
