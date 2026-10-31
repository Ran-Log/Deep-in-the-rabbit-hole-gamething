namespace Tests.Integration;

using System.Threading.Tasks;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

// Feature 1.5: techos, tragaluces y salas oscuras.
[TestSuite]
[RequireGodotRuntime]
public class DarknessTest
{
	[TestCase]
	public void ElCampamentoDeCadaNivelTieneTragaluz()
	{
		for (int n = 1; n <= 3; n++)
		{
			RoomSpec camp = RoomGenerator.Generate(1, Vector2I.Zero, 2.0f, GameData.Level(n));

			AssertThat(camp.HasSkylight).IsTrue();
		}
	}

	[TestCase]
	public void HayMasTragaluzArribaQueEnLasProfundidades()
	{
		int level1 = CountSkylights(GameData.Level(1));
		int level3 = CountSkylights(GameData.Level(3));

		AssertThat(level1).IsGreater(level3);
	}

	[TestCase]
	public void LaAranaSoloApareceEnSalasOscuras()
	{
		var spider = GD.Load<PackedScene>("res://Enemies/Spider.tscn").Instantiate<Enemy>();
		var zombie = GD.Load<PackedScene>("res://Enemies/Zombie.tscn").Instantiate<Enemy>();

		AssertThat(spider.OnlyInDarkRooms).IsTrue();
		AssertThat(zombie.OnlyInDarkRooms).IsFalse();

		spider.Free();
		zombie.Free();
	}

	[TestCase]
	public async Task ConTragaluzElTechoTieneCuatroPiezasYSinElUna()
	{
		AssertThat(await CeilingPiecesOf(skylight: true, at: new Vector3(500, 0, 500))).IsEqual(4);
		AssertThat(await CeilingPiecesOf(skylight: false, at: new Vector3(600, 0, 600))).IsEqual(1);
	}

	[TestCase]
	public async Task LaLamparaDelCascoSePrendeYApaga()
	{
		var root = new Node3D();
		var light = new OmniLight3D();
		var lamp = new LampComponent { Lamp = light };
		root.AddChild(light);
		root.AddChild(lamp);

		ISceneRunner runner = ISceneRunner.Load(root, autoFree: true);
		await runner.AwaitIdleFrame();

		bool? lastSignal = null;
		lamp.Toggled += on => lastSignal = on;

		AssertThat(lamp.IsOn).IsTrue();

		lamp.Toggle();
		AssertThat(lamp.IsOn).IsFalse();
		AssertThat(lastSignal).IsEqual(false);

		lamp.Toggle();
		AssertThat(lamp.IsOn).IsTrue();
	}

	private static async Task<int> CeilingPiecesOf(bool skylight, Vector3 at)
	{
		var room = GD.Load<PackedScene>("res://World/Rooms/Room.tscn").Instantiate<Room>();
		room.Width = 12;
		room.Depth = 12;
		room.HasSkylight = skylight;
		room.Position = at;

		ISceneRunner runner = ISceneRunner.Load(room, autoFree: true);
		await runner.AwaitIdleFrame();

		return room.CeilingPieceCount;
	}

	private static int CountSkylights(LevelDefinition level)
	{
		int count = 0;

		for (int i = 0; i < 300; i++)
		{
			count += RoomGenerator.Generate(99, new Vector2I(40 + i, -40), 2.0f, level).HasSkylight ? 1 : 0;
		}

		return count;
	}
}
