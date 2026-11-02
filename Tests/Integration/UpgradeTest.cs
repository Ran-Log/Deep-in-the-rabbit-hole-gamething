namespace Tests.Integration;

using System.Threading.Tasks;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

// Feature 1.10: estación con menú, armas, antorchas mejoradas y armadura de enemigos.
// Los tests que cambian el equipo lo dejan como estaba (WorldState es un autoload compartido).
[TestSuite]
[RequireGodotRuntime]
public class UpgradeTest
{
	private static readonly string IronId = ItemCatalog.IdOf(MineralType.Iron);
	private static readonly string CoalId = ItemCatalog.IdOf(MineralType.Coal);
	private static readonly string GoldId = ItemCatalog.IdOf(MineralType.Gold);

	[TestCase]
	public void LasEspadasSeForjanEnOrdenYLaDeOroUsaOro()
	{
		WeaponDefinition iron = GameData.NextWeapon(WeaponTier.None);
		WeaponDefinition gold = GameData.NextWeapon(WeaponTier.Iron);

		AssertThat(iron.Tier).IsEqual(WeaponTier.Iron);
		AssertThat(gold.Tier).IsEqual(WeaponTier.Gold);
		AssertThat(gold.Damage).IsGreater(iron.Damage);
		AssertThat(gold.CostByItemId().ContainsKey(GoldId)).IsTrue();
		AssertThat(GameData.NextWeapon(WeaponTier.Gold)).IsNull();
	}

	[TestCase]
	public void LasAntorchasMejoradasAlumbranMasLejos()
	{
		LightDefinition common = GameData.Light(1);
		LightDefinition improved = GameData.Light(2);

		AssertThat(improved.TorchRange).IsGreater(common.TorchRange);
		AssertThat(improved.LampRange).IsGreater(common.LampRange);
	}

	[TestCase]
	public async Task ElMenuForjaLaEspadaGastandoLosMateriales()
	{
		WorldState world = WorldState.Instance;
		WeaponTier oldWeapon = world.Weapon;
		world.Weapon = WeaponTier.None;

		try
		{
			var (menu, equipment) = await LoadMenu();
			world.TryAddItem(IronId, 5);
			world.TryAddItem(CoalId, 2);
			int ironBefore = world.GetItemCount(IronId);

			AssertThat(menu.Buy(1)).IsTrue();
			AssertThat(equipment.Weapon).IsEqual(WeaponTier.Iron);
			AssertThat(world.GetItemCount(IronId)).IsEqual(ironBefore - 5);
			AssertThat(equipment.CombatDamage).IsEqual(GameData.Weapon(WeaponTier.Iron).Damage);
		}
		finally
		{
			world.Weapon = oldWeapon;
		}
	}

	[TestCase]
	public async Task SinMaterialesNoSeMejora()
	{
		WorldState world = WorldState.Instance;
		WeaponTier oldWeapon = world.Weapon;
		world.Weapon = WeaponTier.Iron;
		world.RemoveItem(GoldId, world.GetItemCount(GoldId));

		try
		{
			var (menu, equipment) = await LoadMenu();

			AssertThat(menu.Buy(1)).IsFalse();
			AssertThat(equipment.Weapon).IsEqual(WeaponTier.Iron);
		}
		finally
		{
			world.Weapon = oldWeapon;
		}
	}

	[TestCase]
	public async Task ElMenuTieneLasCuatroLineasYSeCierra()
	{
		var (menu, equipment) = await LoadMenu();
		var options = UpgradeTable.Options(equipment);

		AssertThat(options.Count).IsEqual(4);
		AssertThat(options[0].Title).IsEqual("Pico");
		AssertThat(options[1].Title).IsEqual("Arma");
		AssertThat(options[2].Title).IsEqual("Antorchas");
		AssertThat(options[3].Title).IsEqual("Campamento");
		AssertThat(menu.IsOpen).IsTrue();

		menu.Close();
		AssertThat(menu.IsOpen).IsFalse();
	}

	[TestCase]
	public async Task MejorarLasAntorchasAlargaSuLuz()
	{
		WorldState world = WorldState.Instance;
		int oldLevel = world.LightLevel;
		world.LightLevel = 1;

		try
		{
			var torch = GD.Load<PackedScene>("res://World/Props/Torch.tscn").Instantiate<Torch>();
			ISceneRunner runner = ISceneRunner.Load(torch, autoFree: true);
			await runner.AwaitIdleFrame();

			AssertThat(torch.Light.OmniRange).IsEqual(GameData.Light(1).TorchRange);

			world.LightLevel = 2;
			AssertThat(torch.Light.OmniRange).IsEqual(GameData.Light(2).TorchRange);
		}
		finally
		{
			world.LightLevel = oldLevel;
		}
	}

	[TestCase]
	public async Task LaArmaduraDelTopoRestaDano()
	{
		ISceneRunner runner = ISceneRunner.Load("res://Enemies/Mole.tscn", autoFree: true);
		await runner.AwaitIdleFrame();
		var mole = (Enemy)runner.Scene();
		int before = mole.Health.Current;

		mole.TakeHit(4, mole.GlobalPosition + Vector3.Forward);

		AssertThat(mole.Armor).IsEqual(1);
		AssertThat(mole.Health.Current).IsEqual(before - 3);
	}

	[TestCase]
	public async Task UnGolpeDebilContraLaHormigaAvisaYSuenaDistinto()
	{
		var root = new Node3D();
		var ant = GD.Load<PackedScene>("res://Enemies/Ant.tscn").Instantiate<Enemy>();
		var combat = new CombatComponent();
		root.AddChild(ant);
		root.AddChild(combat);

		ISceneRunner runner = ISceneRunner.Load(root, autoFree: true);
		await runner.AwaitIdleFrame();

		bool resisted = false;
		combat.HitResisted += _ => resisted = true;

		// Sin equipo pega 1; la hormiga tiene armadura 2.
		combat.Strike(ant, ant, ant.GlobalPosition + Vector3.Forward);

		AssertThat(resisted).IsTrue();
		AssertThat(AudioManager.Instance.LastPlayed).IsSame(AudioManager.Instance.HitRejected);
	}

	private static async Task<(UpgradeMenu, PlayerEquipment)> LoadMenu()
	{
		var root = new Node();
		var menu = GD.Load<PackedScene>("res://UI/UpgradeMenu.tscn").Instantiate<UpgradeMenu>();
		var equipment = new PlayerEquipment();
		root.AddChild(menu);
		root.AddChild(equipment);

		ISceneRunner runner = ISceneRunner.Load(root, autoFree: true);
		await runner.AwaitIdleFrame();

		menu.Open(equipment);
		return (menu, equipment);
	}
}
