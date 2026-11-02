namespace Tests.Integration;

using System.Threading.Tasks;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

// Feature 1.11: diario de objetivos de la sesión.
// Cada test limpia el diario (autoload compartido) y deja WorldState como estaba.
[TestSuite]
[RequireGodotRuntime]
public class JournalTest
{
	private static Journal Journal => SessionJournal.Instance.Journal;

	[BeforeTest]
	public void ClearJournal()
	{
		Journal.Clear();
	}

	[TestCase]
	public async Task RecolectarCuentaSoloLoQueEntraALaMochila()
	{
		JournalGoal goal = SessionJournal.Instance.AddGoal(GoalKind.Collect, "Gold", 2);
		var mining = await AddToScene(new MiningComponent());

		string completed = "";
		SessionJournal.Instance.GoalCompleted += description => completed = description;

		mining.EmitSignal(MiningComponent.SignalName.MineralCollected, "Gold");
		mining.EmitSignal(MiningComponent.SignalName.MineralCollected, "Gold");

		AssertThat(goal.IsDone).IsTrue();
		AssertThat(completed).IsEqual("Recolectar 2 Oro");
		AssertThat(Journal.SessionTotal(GoalKind.Collect, "Gold")).IsEqual(2);
	}

	[TestCase]
	public async Task ConstruirCuentaPorTipo()
	{
		JournalGoal goal = SessionJournal.Instance.AddGoal(GoalKind.Build, "Barricada", 1);
		var build = await AddToScene(new BuildComponent());

		build.EmitSignal(BuildComponent.SignalName.StructureBuilt, "Antorcha");
		AssertThat(goal.Progress).IsEqual(0);

		build.EmitSignal(BuildComponent.SignalName.StructureBuilt, "Barricada");
		AssertThat(goal.IsDone).IsTrue();
	}

	[TestCase]
	public void MejorarCuentaSoloCuandoSubeElNivel()
	{
		WorldState world = WorldState.Instance;
		int oldLevel = world.LightLevel;

		try
		{
			world.LightLevel = 1;
			JournalGoal goal = SessionJournal.Instance.AddGoal(GoalKind.Upgrade, UpgradeTable.LightLine, 1);

			world.LightLevel = 2;
			AssertThat(goal.IsDone).IsTrue();

			// Bajar (ej. cargar otra partida) no es una mejora.
			world.LightLevel = 1;
			AssertThat(goal.Progress).IsEqual(1);
		}
		finally
		{
			world.LightLevel = oldLevel;
		}
	}

	[TestCase]
	public void DescubrirSalasNoCuentaElCampamento()
	{
		JournalGoal goal = SessionJournal.Instance.AddGoal(GoalKind.Explore, SessionJournal.RoomsSubject, 1);

		WorldState.Instance.MarkRoomVisited(Vector2I.Zero);
		AssertThat(goal.Progress).IsEqual(0);

		WorldState.Instance.MarkRoomVisited(new Vector2I(777, -777));
		AssertThat(goal.IsDone).IsTrue();
	}

	[TestCase]
	public async Task DerrotarEnemigosCuenta()
	{
		JournalGoal goal = SessionJournal.Instance.AddGoal(GoalKind.Defeat, SessionJournal.EnemiesSubject, 1);
		var zombie = GD.Load<PackedScene>("res://Enemies/Zombie.tscn").Instantiate<Enemy>();
		await AddToScene(zombie);

		zombie.TakeHit(999, zombie.GlobalPosition + Vector3.Forward);

		AssertThat(goal.IsDone).IsTrue();
	}

	[TestCase]
	public async Task LaPantallaMuestraLosObjetivosYElResumen()
	{
		var screen = GD.Load<PackedScene>("res://UI/JournalScreen.tscn").Instantiate<JournalScreen>();
		await AddToScene(screen);

		screen.Open();
		screen.AddGoal(GoalKind.Collect, "Stone", 3);
		SessionJournal.Instance.Record(GoalKind.Collect, "Stone", 4);

		AssertThat(screen.GoalsContainer.GetChildCount()).IsEqual(1);
		AssertThat(screen.SummaryLabel.Text).Contains("4 Piedra");
		AssertThat(JournalScreen.SubjectsFor(GoalKind.Upgrade).Count).IsEqual(UpgradeTable.Lines.Length);

		screen.Close();
		AssertThat(screen.IsOpen).IsFalse();
	}

	private static async Task<T> AddToScene<T>(T node) where T : Node
	{
		ISceneRunner runner = ISceneRunner.Load(node, autoFree: true);
		await runner.AwaitIdleFrame();
		return node;
	}
}
