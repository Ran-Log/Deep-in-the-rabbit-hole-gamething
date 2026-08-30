using Godot;

public partial class DestructibleWall : StaticBody3D
{
	[Export]
	public int Health = 5;

	public void TakeDamage(int damage)
	{
		Health -= damage;

		GD.Print("Wall health: " + Health);

		if (Health <= 0)
		{
			Destroy();
		}
	}

	private void Destroy()
	{
		GD.Print("Wall destroyed!");

		QueueFree();
	}
}
