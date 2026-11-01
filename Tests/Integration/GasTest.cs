namespace Tests.Integration;

using System.Threading.Tasks;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

// Feature 1.7: gas explosivo.
[TestSuite]
[RequireGodotRuntime]
public class GasTest
{
	[TestCase]
	public async Task ElDanoBajaConLaDistanciaAlBorde()
	{
		var (_, zone) = await LoadGas(new Vector3(1000, 0, 1000), new Vector2I(900, 900));

		// Gas de 14 x 3 x 14: el borde está a 7 m del centro. Margen de 5 m.
		AssertThat(zone.DamageAt(new Vector3(1000, 0, 1000))).IsEqual(70);
		AssertThat(zone.DamageAt(new Vector3(1009.5f, 0, 1000))).IsEqual(35);
		AssertThat(zone.DamageAt(new Vector3(1013, 0, 1000))).IsEqual(0);
	}

	[TestCase]
	public async Task UnaAntorchaEncendidaCercaLoHaceExplotar()
	{
		var (root, zone) = await LoadGas(new Vector3(1100, 0, 1100), new Vector2I(901, 901));
		AddTorch(root, new Vector3(1108, 0, 1100), lit: true);

		await AwaitPhysics();

		AssertThat(GodotObject.IsInstanceValid(zone)).IsFalse();
		AssertThat(WorldState.Instance.IsGasExploded(new Vector2I(901, 901))).IsTrue();
		AssertThat(AudioManager.Instance.LastPlayed).IsSame(AudioManager.Instance.Explosion);
	}

	[TestCase]
	public async Task UnaAntorchaApagadaNoLoEnciende()
	{
		var (root, zone) = await LoadGas(new Vector3(1200, 0, 1200), new Vector2I(902, 902));
		AddTorch(root, new Vector3(1208, 0, 1200), lit: false);

		await AwaitPhysics();

		AssertThat(GodotObject.IsInstanceValid(zone)).IsTrue();
	}

	[TestCase]
	public async Task LaExplosionRompeBloquesPeroNoLaRocaMadre()
	{
		var (root, zone) = await LoadGas(new Vector3(1300, 0, 1300), new Vector2I(903, 903));
		DestructibleBlock stone = AddBlock(root, new Vector3(1310, 0, 1300), destructible: true);
		DestructibleBlock bedrock = AddBlock(root, new Vector3(1310, 0, 1302), destructible: false);
		await AwaitPhysics();

		zone.Explode(new Vector3(1309, 0, 1300));
		await AwaitPhysics();

		AssertThat(GodotObject.IsInstanceValid(stone)).IsFalse();
		AssertThat(GodotObject.IsInstanceValid(bedrock)).IsTrue();
	}

	[TestCase]
	public async Task UnaSalaConElGasYaExplotadoNoLoVuelveAPoner()
	{
		WorldState.Instance.MarkGasExploded(new Vector2I(904, 904));

		AssertThat(await GasCountInRoom(new Vector2I(904, 904), new Vector3(1400, 0, 1400))).IsEqual(0);
		AssertThat(await GasCountInRoom(new Vector2I(905, 905), new Vector3(1500, 0, 1500))).IsEqual(1);
	}

	private static async Task<(Node3D, GasZone)> LoadGas(Vector3 at, Vector2I roomCoord)
	{
		var root = new Node3D();
		var zone = GD.Load<PackedScene>("res://World/Props/GasZone.tscn").Instantiate<GasZone>();
		zone.RoomCoord = roomCoord;
		zone.Position = at;
		root.AddChild(zone);

		ISceneRunner runner = ISceneRunner.Load(root, autoFree: true);
		await runner.AwaitIdleFrame();

		return (root, zone);
	}

	private static void AddTorch(Node3D root, Vector3 at, bool lit)
	{
		var torch = GD.Load<PackedScene>("res://World/Props/Torch.tscn").Instantiate<Torch>();
		torch.StartLit = lit;
		torch.Position = at;
		root.AddChild(torch);
	}

	private static DestructibleBlock AddBlock(Node3D root, Vector3 at, bool destructible)
	{
		var block = GD.Load<PackedScene>("res://World/Blocks/DestructibleBlock.tscn").Instantiate<DestructibleBlock>();
		block.IsDestructible = destructible;
		block.Position = at;
		root.AddChild(block);
		return block;
	}

	private static async Task<int> GasCountInRoom(Vector2I coord, Vector3 at)
	{
		var room = GD.Load<PackedScene>("res://World/Rooms/Room.tscn").Instantiate<Room>();
		room.Width = 12;
		room.Depth = 12;
		room.HasGas = true;
		room.RoomGridCoord = coord;
		room.Position = at;

		ISceneRunner runner = ISceneRunner.Load(room, autoFree: true);
		await runner.AwaitIdleFrame();

		int count = 0;

		foreach (Node child in room.GetChildren())
		{
			count += child is GasZone ? 1 : 0;
		}

		return count;
	}

	private static async Task AwaitPhysics()
	{
		SceneTree tree = (SceneTree)Engine.GetMainLoop();
		await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
		await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
		await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
	}
}
