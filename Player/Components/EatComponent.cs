using Godot;

/// <summary>
/// Comer y beber (GDD 2.1.11). Q come una ración, F bebe una cantimplora; también
/// con el botón "Usar" de la mochila.
/// </summary>
public partial class EatComponent : Node
{
	[Signal]
	public delegate void AteEventHandler();

	[Signal]
	public delegate void DrankEventHandler();

	[ExportCategory("EatComponent")]
	[ExportGroup("Referencias")]
	[Export] public HealthComponent Health;
	[Export] public NeedComponent Hunger;
	[Export] public NeedComponent Thirst;

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!CrosshairRay.PlayerHasControl())
		{
			return;
		}

		if (@event.IsActionPressed("eat"))
		{
			TryEat();
		}
		else if (@event.IsActionPressed("drink"))
		{
			TryDrink();
		}
	}

	// Para el botón "Usar" de la mochila.
	public bool Use(string itemId)
	{
		return itemId == ItemCatalog.Water ? TryDrink() : TryEat();
	}

	public bool TryEat()
	{
		if (Health == null || Hunger == null || WorldState.Instance == null)
		{
			return false;
		}

		if (WorldState.Instance.GetItemCount(ItemCatalog.Food) <= 0)
		{
			GameEvents.PostMessage("No tienes comida");
			return false;
		}

		if (Hunger.IsFull && Health.IsFull)
		{
			GameEvents.PostMessage("No tienes hambre; guarda la comida");
			return false;
		}

		WorldState.Instance.RemoveItem(ItemCatalog.Food);
		Hunger.Restore(ItemCatalog.FoodHunger);
		Health.Heal(ItemCatalog.FoodHeal);
		GameEvents.PostMessage($"Comiste una ración (+{ItemCatalog.FoodHeal} vida)");
		EmitSignal(SignalName.Ate);

		return true;
	}

	public bool TryDrink()
	{
		if (Thirst == null || WorldState.Instance == null)
		{
			return false;
		}

		if (WorldState.Instance.GetItemCount(ItemCatalog.Water) <= 0)
		{
			GameEvents.PostMessage("No tienes agua");
			return false;
		}

		if (Thirst.IsFull)
		{
			GameEvents.PostMessage("No tienes sed; guarda el agua");
			return false;
		}

		WorldState.Instance.RemoveItem(ItemCatalog.Water);
		Thirst.Restore(ItemCatalog.WaterThirst);
		GameEvents.PostMessage("Bebiste de la cantimplora");
		EmitSignal(SignalName.Drank);

		return true;
	}
}
