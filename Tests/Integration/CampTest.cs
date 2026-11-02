namespace Tests.Integration;

using System.Threading.Tasks;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

// Feature 1.9: reparar y mejorar el campamento, trampa de pinchos.
// Los tests que cambian WorldState (autoload compartido) lo dejan como estaba.
[TestSuite]
[RequireGodotRuntime]
public class CampTest
{
	private static readonly string StoneId = ItemCatalog.IdOf(MineralType.Stone);
	private static readonly string IronId = ItemCatalog.IdOf(MineralType.Iron);
	private static readonly string GoldId = ItemCatalog.IdOf(MineralType.Gold);

	[TestCase]
	public void CadaNivelDelCampamentoAguantaMasYElUltimoUsaOro()
	{
		CampDefinition fire = GameData.Camp(1);
		CampDefinition reinforced = GameData.Camp(2);
		CampDefinition fortified = GameData.Camp(3);

		AssertThat(reinforced.MaxHealth).IsGreater(fire.MaxHealth);
		AssertThat(fortified.MaxHealth).IsGreater(reinforced.MaxHealth);
		AssertThat(reinforced.BarricadeHealth).IsGreater(fire.BarricadeHealth);
		AssertThat(fortified.CostByItemId().ContainsKey(GoldId)).IsTrue();
		AssertThat(GameData.Camp(4)).IsNull();
	}

	[TestCase]
	public async Task RepararGastaPiedraYCura()
	{
		CampCore core = await LoadCampCore();
		WorldState.Instance.TryAddItem(StoneId, 3);
		int stoneBefore = WorldState.Instance.GetItemCount(StoneId);

		core.Health.FullHeal();
		core.Health.TakeDamage(40);
		int damaged = core.Health.Current;

		AssertThat(core.TryRepair()).IsTrue();
		AssertThat(core.Health.Current).IsEqual(damaged + core.RepairAmount);
		AssertThat(WorldState.Instance.GetItemCount(StoneId)).IsEqual(stoneBefore - core.RepairStoneCost);
	}

	[TestCase]
	public async Task ConElCampamentoSanoNoSeGastaPiedra()
	{
		CampCore core = await LoadCampCore();
		WorldState.Instance.TryAddItem(StoneId, 3);
		int stoneBefore = WorldState.Instance.GetItemCount(StoneId);

		core.Health.FullHeal();

		AssertThat(core.TryRepair()).IsFalse();
		AssertThat(WorldState.Instance.GetItemCount(StoneId)).IsEqual(stoneBefore);
	}

	[TestCase]
	public async Task MejorarElCampamentoEnLaEstacionSubeSuVida()
	{
		WorldState world = WorldState.Instance;
		int oldLevel = world.CampLevel;
		world.CampLevel = 1;

		try
		{
			CampCore core = await LoadCampCore();
			core.Health.FullHeal();
			AssertThat(core.Health.MaxHealth).IsEqual(GameData.Camp(1).MaxHealth);

			world.TryAddItem(StoneId, 10);
			world.TryAddItem(IronId, 4);
			var campRow = UpgradeTable.Options(new PlayerEquipment())[3];

			AssertThat(UpgradeTable.TryBuy(campRow, null, out _)).IsTrue();
			AssertThat(world.CampLevel).IsEqual(2);
			AssertThat(core.Health.MaxHealth).IsEqual(GameData.Camp(2).MaxHealth);
			AssertThat(core.Health.IsFull).IsTrue();
		}
		finally
		{
			world.CampLevel = oldLevel;
		}
	}

	[TestCase]
	public void LasBarricadasNuevasSiguenElNivelDelCampamento()
	{
		WorldState world = WorldState.Instance;
		int oldLevel = world.CampLevel;
		var defenses = new CampDefenses();

		try
		{
			world.CampLevel = 2;
			AssertThat(defenses.BarricadeHealth).IsEqual(GameData.Camp(2).BarricadeHealth);
		}
		finally
		{
			world.CampLevel = oldLevel;
			defenses.Free();
		}
	}

	[TestCase]
	public async Task LaTrampaPinchaAlEnemigoYSeGasta()
	{
		var cell = new Vector3I(300, 0, 300);

		try
		{
			var (trap, zombie) = await LoadTrapWithZombie(cell, usesLeft: 2);
			int before = zombie.Health.Current;

			await WaitPhysics(6);

			AssertThat(zombie.Health.Current).IsEqual(before - trap.Damage);
			AssertThat(trap.UsesLeft).IsEqual(1);
		}
		finally
		{
			WorldState.Instance.RemoveTrap(cell);
		}
	}

	[TestCase]
	public async Task SinUsosLaTrampaSeRompe()
	{
		var cell = new Vector3I(310, 0, 310);
		WorldState.Instance.SetTrap(cell, 1);

		var (trap, _) = await LoadTrapWithZombie(cell, usesLeft: 1);
		await WaitPhysics(6);

		AssertThat(GodotObject.IsInstanceValid(trap)).IsFalse();
		AssertThat(WorldState.Instance.HasTrap(cell)).IsFalse();
	}

	[TestCase]
	public async Task LaTrampaSoloVaEnElSueloDelCampamento()
	{
		var root = new Node3D();
		var defenses = new CampDefenses();
		var placer = new TrapPlacer
		{
			TrapScene = GD.Load<PackedScene>("res://Camp/SpikeTrap.tscn"),
			Camp = defenses,
		};
		root.AddChild(defenses);
		root.AddChild(placer);

		ISceneRunner runner = ISceneRunner.Load(root, autoFree: true);
		await runner.AwaitIdleFrame();

		WorldState.Instance.TryAddItem(IronId, 2);
		WorldState.Instance.TryAddItem(StoneId, 2);

		AssertThat(placer.CanPlace(new Vector3I(2, 0, 2), true, out _)).IsTrue();
		AssertThat(placer.CanPlace(new Vector3I(20, 0, 20), true, out _)).IsFalse();
		AssertThat(placer.CanPlace(new Vector3I(2, 1, 2), true, out _)).IsFalse();
		AssertThat(placer.CanPlace(new Vector3I(2, 0, 2), false, out _)).IsFalse();
	}

	private static async Task<CampCore> LoadCampCore()
	{
		ISceneRunner runner = ISceneRunner.Load("res://Camp/Camp.tscn", autoFree: true);
		await runner.AwaitIdleFrame();

		return runner.Scene().GetNode<CampCore>("CampCore");
	}

	// Un zombie quieto (sin velocidad, gravedad ni retroceso) parado sobre la trampa.
	private static async Task<(SpikeTrap, Enemy)> LoadTrapWithZombie(Vector3I cell, int usesLeft)
	{
		var root = new Node3D();
		var trap = GD.Load<PackedScene>("res://Camp/SpikeTrap.tscn").Instantiate<SpikeTrap>();
		var zombie = GD.Load<PackedScene>("res://Enemies/Zombie.tscn").Instantiate<Enemy>();
		Vector3 at = new Vector3(cell.X * 2.0f, 0.0f, cell.Z * 2.0f);

		trap.Cell = cell;
		trap.UsesLeft = usesLeft;
		trap.Position = at;
		zombie.Speed = 0.0f;
		zombie.Gravity = 0.0f;
		zombie.KnockbackStrength = 0.0f;
		zombie.Position = at + new Vector3(0.0f, 1.0f, 0.0f);

		root.AddChild(trap);
		root.AddChild(zombie);

		ISceneRunner runner = ISceneRunner.Load(root, autoFree: true);
		await runner.AwaitIdleFrame();

		return (trap, zombie);
	}

	private static async Task WaitPhysics(int frames)
	{
		SceneTree tree = (SceneTree)Engine.GetMainLoop();

		for (int i = 0; i < frames; i++)
		{
			await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
		}
	}
}
