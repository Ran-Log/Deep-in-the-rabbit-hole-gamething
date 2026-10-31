using Godot;

/// <summary>
/// Comer (GDD 2.1.11): gasta una ración y recupera vida. Con Q o con el botón
/// "Usar" del inventario.
/// </summary>
public partial class EatComponent : Node
{
	[Signal]
	public delegate void AteEventHandler();

	[ExportCategory("EatComponent")]
	[ExportGroup("Referencias")]
	[Export] public HealthComponent Health;

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("eat") && CrosshairRay.PlayerHasControl())
		{
			TryEat();
		}
	}

	public bool TryEat()
	{
		if (Health == null || WorldState.Instance == null)
		{
			return false;
		}

		if (WorldState.Instance.GetItemCount(ItemCatalog.Food) <= 0)
		{
			GameEvents.PostMessage("No tienes comida");
			return false;
		}

		if (Health.IsFull)
		{
			GameEvents.PostMessage("Tienes la vida llena; guarda la comida");
			return false;
		}

		WorldState.Instance.RemoveItem(ItemCatalog.Food);
		Health.Heal(ItemCatalog.FoodHeal);
		GameEvents.PostMessage($"Comiste una ración (+{ItemCatalog.FoodHeal} vida)");
		EmitSignal(SignalName.Ate);

		return true;
	}
}
