namespace Tests.Integration;

using System.Threading.Tasks;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

// Tests de integración: cargan la escena real del bloque dentro de Godot,
// con el autoload BlockGrid de verdad.
[TestSuite]
[RequireGodotRuntime]
public class DestructibleBlockTest
{
	private const string BlockScenePath = "res://World/Blocks/DestructibleBlock.tscn";

	[TestCase]
	public async Task SeRegistraEnLaGrillaAlEntrarAlArbol()
	{
		ISceneRunner runner = LoadBlock(MineralType.Stone, new Vector3(20.0f, 0.0f, 20.0f));
		await runner.AwaitIdleFrame();

		var block = (DestructibleBlock)runner.Scene();

		AssertThat(BlockGrid.Instance.GetBlock(block.Cell)).IsSame(block);
	}

	[TestCase]
	public async Task GolpeConPicoInsuficienteRebota()
	{
		ISceneRunner runner = LoadBlock(MineralType.Iron, new Vector3(24.0f, 0.0f, 20.0f));
		await runner.AwaitIdleFrame();

		var block = (DestructibleBlock)runner.Scene();
		int healthBefore = block.Health;

		AssertThat(block.TakeDamage(5, ToolTier.Basic)).IsFalse();
		AssertThat(block.Health).IsEqual(healthBefore);
	}

	[TestCase]
	public async Task RomperloLiberaLaCelda()
	{
		ISceneRunner runner = LoadBlock(MineralType.Stone, new Vector3(28.0f, 0.0f, 20.0f));
		await runner.AwaitIdleFrame();

		var block = (DestructibleBlock)runner.Scene();
		Vector3I cell = block.Cell;

		block.TakeDamage(block.MaxHealth, ToolTier.Basic);
		// QueueFree se aplica al final del frame; ahí _ExitTree lo saca de la grilla.
		await runner.SimulateFrames(2);

		AssertThat(BlockGrid.Instance.IsOccupied(cell)).IsFalse();
	}

	private static ISceneRunner LoadBlock(MineralType mineral, Vector3 position)
	{
		var block = GD.Load<PackedScene>(BlockScenePath).Instantiate<DestructibleBlock>();
		block.Mineral = mineral;
		block.RequiredTool = MineralRules.RequiredTool(mineral);
		block.Position = position;

		return ISceneRunner.Load(block, autoFree: true);
	}
}
