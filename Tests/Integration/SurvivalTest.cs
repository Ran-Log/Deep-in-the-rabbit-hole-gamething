namespace Tests.Integration;

using System.Threading.Tasks;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

// Feature 1.6: hambre y sed.
[TestSuite]
[RequireGodotRuntime]
public class SurvivalTest
{
	[TestCase]
	public async Task LaNecesidadBajaConElTiempo()
	{
		var (_, need, _) = await BuildPlayerParts();

		need.Tick(60.0);

		AssertThat(need.Current).IsEqual(90);
	}

	[TestCase]
	public async Task EnCeroQuitaVidaCadaIntervalo()
	{
		var (health, need, _) = await BuildPlayerParts();

		need.Tick(600.0);
		int before = health.Current;
		need.Tick(3.0);

		AssertThat(need.IsEmpty).IsTrue();
		AssertThat(health.Current).IsEqual(before - 2);
	}

	[TestCase]
	public async Task RestaurarNoPasaDelMaximo()
	{
		var (_, need, _) = await BuildPlayerParts();

		need.Tick(60.0);
		need.Restore(500);

		AssertThat(need.Current).IsEqual(100);
	}

	[TestCase]
	public async Task ConSedEnCeroNoSePuedeCorrer()
	{
		var (_, need, _) = await BuildPlayerParts();
		var player = new Player { Thirst = need };

		AssertThat(player.CanRun).IsTrue();

		need.Tick(600.0);
		AssertThat(player.CanRun).IsFalse();

		player.Free();
	}

	[TestCase]
	public async Task BeberDeLaCantimploraGastaUnaYQuitaSed()
	{
		var (_, need, eat) = await BuildPlayerParts();
		WorldState world = WorldState.Instance;
		world.TryAddItem(ItemCatalog.Water, 1);
		int waterBefore = world.GetItemCount(ItemCatalog.Water);

		AssertThat(eat.TryDrink()).IsFalse(); // sin sed no se gasta

		need.Tick(300.0);
		AssertThat(eat.TryDrink()).IsTrue();
		AssertThat(need.Current).IsEqual(100);
		AssertThat(world.GetItemCount(ItemCatalog.Water)).IsEqual(waterBefore - 1);
	}

	[TestCase]
	public async Task ElBarrilDelCampamentoQuitaTodaLaSed()
	{
		var (_, need, _) = await BuildPlayerParts();
		var player = new Player { Thirst = need };
		var barrel = GD.Load<PackedScene>("res://Camp/WaterBarrel.tscn").Instantiate<WaterBarrel>();

		need.Tick(500.0);
		barrel.Interact(player);

		AssertThat(need.IsFull).IsTrue();

		player.Free();
		barrel.Free();
	}

	[TestCase]
	public void AlgunosCofresAlAzarTraenAgua()
	{
		int water = 0;

		for (int i = 0; i < 300; i++)
		{
			RoomSpec room = RoomGenerator.Generate(7, new Vector2I(60 + i, 60), 2.0f, GameData.Level(1));
			water += room.Loot == ChestLoot.Water ? 1 : 0;
		}

		AssertThat(water).IsGreater(0);
	}

	// Vida + una necesidad (usada como sed) + EatComponent, sin cargar todo el jugador.
	private static async Task<(HealthComponent, NeedComponent, EatComponent)> BuildPlayerParts()
	{
		var root = new Node();
		var health = new HealthComponent();
		var need = new NeedComponent { Health = health, DrainPerMinute = 10.0f };
		var eat = new EatComponent { Health = health, Thirst = need };
		root.AddChild(health);
		root.AddChild(need);
		root.AddChild(eat);

		ISceneRunner runner = ISceneRunner.Load(root, autoFree: true);
		await runner.AwaitIdleFrame();

		// Que _Process no la siga bajando durante el test.
		need.SetProcess(false);

		return (health, need, eat);
	}
}
