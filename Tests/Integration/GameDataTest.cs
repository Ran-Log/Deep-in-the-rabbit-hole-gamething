namespace Tests.Integration;

using System.Collections.Generic;
using GdUnit4;
using static GdUnit4.Assertions;

// Prueba los datos de Data/game_data.tres (los que se editan en el inspector).
[TestSuite]
[RequireGodotRuntime]
public class GameDataTest
{
	[TestCase]
	public void ElCatalogoTieneLosCincoMineralesYElPiso()
	{
		AssertThat(GameData.Catalog).IsNotNull();
		AssertThat(GameData.Catalog.Minerals.Count).IsEqual(5);
		AssertThat(GameData.Floor).IsNotNull();

		foreach (MineralType type in System.Enum.GetValues<MineralType>())
		{
			AssertThat(GameData.Mineral(type)).IsNotNull();
		}
	}

	[TestCase]
	public void PicoRequeridoPorMineral()
	{
		AssertThat(MineralRules.RequiredTool(MineralType.Stone)).IsEqual(ToolTier.Basic);
		AssertThat(MineralRules.RequiredTool(MineralType.Coal)).IsEqual(ToolTier.Basic);
		AssertThat(MineralRules.RequiredTool(MineralType.Iron)).IsEqual(ToolTier.Reinforced);
		AssertThat(MineralRules.RequiredTool(MineralType.Gold)).IsEqual(ToolTier.Advanced);
		AssertThat(MineralRules.RequiredTool(MineralType.Bedrock)).IsEqual(ToolTier.Advanced);
	}

	[TestCase]
	public void DeBasicoSePasaAReforzadoCon8PiedraY4Carbon()
	{
		AssertThat(UpgradeTable.TryGetNext(ToolTier.Basic, out ToolTier next, out var cost)).IsTrue();

		AssertThat(next).IsEqual(ToolTier.Reinforced);
		AssertThat(cost[ItemCatalog.IdOf(MineralType.Stone)]).IsEqual(8);
		AssertThat(cost[ItemCatalog.IdOf(MineralType.Coal)]).IsEqual(4);
	}

	[TestCase]
	public void DeReforzadoSePasaAAvanzadoCon6HierroY4Carbon()
	{
		AssertThat(UpgradeTable.TryGetNext(ToolTier.Reinforced, out ToolTier next, out var cost)).IsTrue();

		AssertThat(next).IsEqual(ToolTier.Advanced);
		AssertThat(cost[ItemCatalog.IdOf(MineralType.Iron)]).IsEqual(6);
		AssertThat(cost[ItemCatalog.IdOf(MineralType.Coal)]).IsEqual(4);
	}

	[TestCase]
	public void ElPicoAvanzadoEsElMaximo()
	{
		AssertThat(UpgradeTable.TryGetNext(ToolTier.Advanced, out _, out _)).IsFalse();
	}

	[TestCase]
	public void LaMejoraSeCobraCompletaOnoSeCobra()
	{
		var state = new WorldSaveState();
		state.AddMineral(ItemCatalog.IdOf(MineralType.Stone), 8);
		state.AddMineral(ItemCatalog.IdOf(MineralType.Coal), 3);

		UpgradeTable.TryGetNext(ToolTier.Basic, out _, out IReadOnlyDictionary<string, int> cost);

		AssertThat(state.TrySpend(cost)).IsFalse();

		state.AddMineral(ItemCatalog.IdOf(MineralType.Coal), 1);

		AssertThat(state.TrySpend(cost)).IsTrue();
		AssertThat(state.TotalItems).IsEqual(0);
	}
}
