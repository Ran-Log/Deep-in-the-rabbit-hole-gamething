namespace Tests.Integration;

using System.Threading.Tasks;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

// Feature 1.11: mapa con fog of war (minimapa + mapa grande).
// Usa celdas lejanas para no pisar lo que haya en WorldState (autoload compartido).
[TestSuite]
[RequireGodotRuntime]
public class MapTest
{
	[TestCase]
	public async Task SeRevelaLoQueEstaCercaYNoLoLejano()
	{
		var (revealer, _) = await LoadRevealer();

		revealer.RevealAround(new Vector3(1600, 0, 1600));

		AssertThat(WorldState.Instance.GetMapCell(new Vector2I(800, 800))).IsEqual(MapCell.Floor);
		AssertThat(WorldState.Instance.GetMapCell(new Vector2I(800 + revealer.RevealRadius, 800))).IsEqual(MapCell.Floor);
		AssertThat(WorldState.Instance.GetMapCell(new Vector2I(800 + revealer.RevealRadius + 1, 800))).IsEqual(MapCell.Unknown);
	}

	[TestCase]
	public async Task UnBloqueSeVeComoParedYAlMinarloComoTunel()
	{
		var (revealer, root) = await LoadRevealer();
		var block = GD.Load<PackedScene>("res://World/Blocks/DestructibleBlock.tscn").Instantiate<DestructibleBlock>();
		block.Position = new Vector3(1706, 0, 1700);
		root.AddChild(block);
		await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame);

		revealer.RevealAround(new Vector3(1700, 0, 1700));
		AssertThat(WorldState.Instance.GetMapCell(new Vector2I(853, 850))).IsEqual(MapCell.Wall);

		bool reported = false;
		revealer.CellsRevealed += cells => reported = cells.Contains(new Vector3I(853, 850, (int)MapCell.Floor));

		block.Free();
		revealer.RevealAround(new Vector3(1700, 0, 1700));

		AssertThat(WorldState.Instance.GetMapCell(new Vector2I(853, 850))).IsEqual(MapCell.Floor);
		AssertThat(reported).IsTrue();
	}

	[TestCase]
	public async Task ElMapaPintaLasCeldasReveladasYSeAbreConM()
	{
		var (revealer, root) = await LoadRevealer();
		var map = GD.Load<PackedScene>("res://UI/WorldMap.tscn").Instantiate<WorldMap>();
		map.Revealer = revealer;
		root.AddChild(map);
		await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame);

		// Dentro de la imagen (el campamento está en el medio): celda (120, 120) = (240 m, 240 m).
		revealer.RevealAround(new Vector3(240, 0, 240));

		AssertThat(SameColor(map.PixelAt(new Vector2I(120, 120)), map.FloorColor)).IsTrue();
		AssertThat(map.IsFullMapOpen).IsFalse();

		map.ToggleFullMap();
		AssertThat(map.IsFullMapOpen).IsTrue();
		AssertThat(map.Minimap.Visible).IsFalse();
	}

	// La imagen guarda 8 bits por canal: se acepta 1/255 de diferencia.
	private static bool SameColor(Color a, Color b)
	{
		const float Tolerance = 1.5f / 255.0f;
		return Mathf.Abs(a.R - b.R) < Tolerance && Mathf.Abs(a.G - b.G) < Tolerance
			&& Mathf.Abs(a.B - b.B) < Tolerance && Mathf.Abs(a.A - b.A) < Tolerance;
	}

	[TestCase]
	public void LaBrujulaSabeHaciaDondeMira()
	{
		// El norte es -Z (arriba en el mapa) y el este +X.
		AssertThat(Compass.BearingOf(0, -1)).IsEqualApprox(0.0f, 0.01f);
		AssertThat(Compass.BearingOf(1, 0)).IsEqualApprox(90.0f, 0.01f);
		AssertThat(Compass.BearingOf(0, 1)).IsEqualApprox(180.0f, 0.01f);
		AssertThat(Compass.BearingOf(-1, 0)).IsEqualApprox(270.0f, 0.01f);

		// Mirando al norte, el oeste queda 90° a la izquierda; cruzar el norte no da la vuelta larga.
		AssertThat(Compass.DeltaFrom(0, 270)).IsEqualApprox(-90.0f, 0.01f);
		AssertThat(Compass.DeltaFrom(350, 10)).IsEqualApprox(20.0f, 0.01f);
	}

	private static async Task<(MapRevealer, Node3D)> LoadRevealer()
	{
		var root = new Node3D();
		var revealer = new MapRevealer();
		root.AddChild(revealer);

		ISceneRunner runner = ISceneRunner.Load(root, autoFree: true);
		await runner.AwaitIdleFrame();

		// Solo lo que pide el test, no cada RevealInterval.
		revealer.SetProcess(false);
		return (revealer, root);
	}
}
