using System;
using System.Linq;
using System.Text.Json;

// Tests de la lógica que no usa Godot (RoomMath, RoomMineralTable, WorldSaveState...).
// Imprime PASS/FAIL y sale con código != 0 si algo falla, para que el CI lo detecte.
// Lo que usa Godot se prueba con GdUnit4 en Tests/Integration.

int total = 0;
int failures = 0;

void Check(string name, bool condition)
{
	total++;

	if (condition)
	{
		Console.WriteLine($"PASS  {name}");
	}
	else
	{
		failures++;
		Console.WriteLine($"FAIL  {name}");
	}
}

// --- RoomMath.IsPerimeterCell ---

Check("perimetro: esquina (0,0) de una sala 12x12 es pared", RoomMath.IsPerimeterCell(0, 0, 12, 12));
Check("perimetro: esquina opuesta (11,11) es pared", RoomMath.IsPerimeterCell(11, 11, 12, 12));
Check("perimetro: celda interior (5,5) NO es pared", !RoomMath.IsPerimeterCell(5, 5, 12, 12));
Check("perimetro: borde x=0 con z interior es pared", RoomMath.IsPerimeterCell(0, 5, 12, 12));
Check("perimetro: borde z=depth-1 con x interior es pared", RoomMath.IsPerimeterCell(5, 9, 10, 10));
Check("perimetro: celda interior cerca del borde (1,1) NO es pared", !RoomMath.IsPerimeterCell(1, 1, 12, 12));

// --- RoomMath.CenterOffset ---

Check("centro: offset(0, 12) es -6", RoomMath.CenterOffset(0, 12) == -6);
Check("centro: offset(11, 12) es 5", RoomMath.CenterOffset(11, 12) == 5);
Check("centro: el ancho total (max-min) es width-1", RoomMath.CenterOffset(11, 12) - RoomMath.CenterOffset(0, 12) == 11);

// --- Regresión: dos salas de 12x12 (celdas de 2 m) separadas 24 m quedan pegadas, sin hueco.
{
	const float cellSize = 2.0f;
	const int width = 12;

	int startFarWallCell = 0 + RoomMath.CenterOffset(width - 1, width);
	int roomNorthOriginCellZ = (int)(24.0f / cellSize);
	int northNearWallCell = roomNorthOriginCellZ + RoomMath.CenterOffset(0, width);

	Check(
		"regresion: la pared lejana de RoomStart y la cercana de RoomNorth son celdas consecutivas (adyacentes, sin hueco ni superposición)",
		northNearWallCell - startFarWallCell == 1
	);
}

// --- RoomMineralTable ---

Check("minerales: roll 0 es Stone", RoomMineralTable.Pick(0) == MineralType.Stone);
Check("minerales: roll 54 es Stone (último del rango)", RoomMineralTable.Pick(54) == MineralType.Stone);
Check("minerales: roll 55 es Coal (primero del rango)", RoomMineralTable.Pick(55) == MineralType.Coal);
Check("minerales: roll 92 es Iron", RoomMineralTable.Pick(92) == MineralType.Iron);
Check("minerales: roll 98 es Gold", RoomMineralTable.Pick(98) == MineralType.Gold);
Check("minerales: roll 99 es Bedrock (el más raro)", RoomMineralTable.Pick(99) == MineralType.Bedrock);

// --- WorldSaveState: minado ---

{
	var state = new WorldSaveState();

	Check("guardado: una celda nueva no está minada", !state.IsCellMined(0, 0, 1, 1, 1));

	state.MarkCellMined(0, 0, 1, 1, 1);

	Check("guardado: después de marcarla, la celda queda minada", state.IsCellMined(0, 0, 1, 1, 1));
	Check("guardado: una celda distinta en la misma sala sigue sin minar", !state.IsCellMined(0, 0, 1, 1, 2));
	Check("guardado: la misma celda en OTRA sala sigue sin minar", !state.IsCellMined(1, 0, 1, 1, 1));
}

// --- WorldSaveState: inventario ---

{
	var state = new WorldSaveState();

	Check("inventario: arranca en 0", state.GetMineralCount("Stone") == 0);

	int afterFirst = state.AddMineral("Stone");
	int afterSecond = state.AddMineral("Stone");

	Check("inventario: sumar de a uno acumula (1, luego 2)", afterFirst == 1 && afterSecond == 2);
	Check("inventario: el conteo final coincide con lo acumulado", state.GetMineralCount("Stone") == 2);
	Check("inventario: otro mineral no se mezcla con el primero", state.GetMineralCount("Coal") == 0);
}

// --- WorldSaveState: round-trip completo (estado -> DTO -> JSON -> DTO -> estado) ---

{
	var original = new WorldSaveState { Seed = 12345 };

	original.MarkCellMined(0, 0, 1, 2, 3);
	original.MarkCellMined(0, 0, -1, 0, -1);
	original.MarkCellMined(2, -3, 5, 5, 5);
	original.AddMineral("Stone", 4);
	original.AddMineral("Coal", 1);

	WorldSaveData dto = original.ToDto(playerX: 10.5f, playerY: 1.0f, playerZ: -20.25f);
	string json = JsonSerializer.Serialize(dto);
	WorldSaveData roundTrippedDto = JsonSerializer.Deserialize<WorldSaveData>(json);
	WorldSaveState restored = WorldSaveState.FromDto(roundTrippedDto);

	Check("guardado: el JSON de ida y vuelta conserva la semilla", roundTrippedDto.Seed == 12345);
	Check("guardado: el JSON de ida y vuelta conserva la posición del jugador", roundTrippedDto.PlayerX == 10.5f && roundTrippedDto.PlayerZ == -20.25f);
	Check("guardado: el estado restaurado recuerda las celdas minadas", restored.IsCellMined(0, 0, 1, 2, 3) && restored.IsCellMined(2, -3, 5, 5, 5));
	Check("guardado: el estado restaurado NO inventa celdas minadas que no estaban", !restored.IsCellMined(0, 0, 9, 9, 9));
	Check("guardado: el estado restaurado conserva el inventario", restored.GetMineralCount("Stone") == 4 && restored.GetMineralCount("Coal") == 1);
}

// --- MineralMaterialProfile ---

Check(
	"materiales: el oro es más metálico que la piedra",
	MineralMaterialProfile.For(MineralType.Gold).Metallic > MineralMaterialProfile.For(MineralType.Stone).Metallic
);
Check(
	"materiales: el bedrock es más rugoso (menos pulido) que el oro",
	MineralMaterialProfile.For(MineralType.Bedrock).Roughness > MineralMaterialProfile.For(MineralType.Gold).Roughness
);
Check(
	"materiales: el piso de las salas no es metálico (es tierra/roca, no un bloque de mineral)",
	MineralMaterialProfile.Floor().Metallic == 0.0f
);
Check(
	"materiales: todos los parámetros PBR de cada mineral quedan en rango [0,1]",
	Enum.GetValues<MineralType>().All(mineral => {
		var p = MineralMaterialProfile.For(mineral);
		float[] values = { p.BaseR, p.BaseG, p.BaseB, p.VeinR, p.VeinG, p.VeinB, p.Metallic, p.Roughness, p.VeinDensity };
		return values.All(v => v >= 0.0f && v <= 1.0f);
	})
);

// --- WorldSaveState: mochila con capacidad ---

{
	var state = new WorldSaveState { Capacity = 5 };

	Check("mochila: agrega si cabe", state.TryAddItem("Stone", 3));
	Check("mochila: rechaza si NO cabe todo (todo o nada)", !state.TryAddItem("Coal", 3) && state.GetMineralCount("Coal") == 0);
	Check("mochila: el total cuenta todos los ítems", state.TotalItems == 3 && state.FreeSpace == 2);
	Check("mochila: quitar más de lo que hay falla y no cambia nada", !state.RemoveItem("Stone", 4) && state.GetMineralCount("Stone") == 3);
	Check("mochila: quitar todo borra la entrada", state.RemoveItem("Stone", 3) && !state.MineralCounts.ContainsKey("Stone"));
}

// --- WorldSaveState: cobrar costos (mejoras / defensas) ---

{
	var state = new WorldSaveState();
	state.AddMineral("Stone", 8);
	state.AddMineral("Coal", 3);

	UpgradeTable.TryGetNext(ToolTier.Basic, out ToolTier next, out var cost);

	Check("mejora: de Básico se pasa a Reforzado", next == ToolTier.Reinforced);
	Check("mejora: sin carbón suficiente no se cobra nada", !state.TrySpend(cost) && state.GetMineralCount("Stone") == 8);

	state.AddMineral("Coal", 1);

	Check("mejora: con todo lo necesario se cobra el costo completo", state.TrySpend(cost) && state.GetMineralCount("Stone") == 0 && state.GetMineralCount("Coal") == 0);
	Check("mejora: el pico avanzado es el máximo", !UpgradeTable.TryGetNext(ToolTier.Advanced, out _, out _));
}

// --- Reglas de minerales ---

Check("reglas: la piedra se rompe con el pico básico", MineralRules.RequiredTool(MineralType.Stone) == ToolTier.Basic);
Check("reglas: el hierro exige pico reforzado", MineralRules.RequiredTool(MineralType.Iron) == ToolTier.Reinforced);
Check("reglas: mejor pico = más daño", MineralRules.ToolDamage(ToolTier.Advanced) > MineralRules.ToolDamage(ToolTier.Basic));

// --- Nivel 1 ---

{
	Check("nivel 1: el campamento (0,0) no tiene gas ni cofre", LevelOneLayout.TryGet(0, 0, out var camp) && !camp.HasGas && camp.Loot == ChestLoot.None);
	Check(
		"nivel 1: la sala de salida existe y exige pico reforzado (hierro)",
		LevelOneLayout.TryGet(LevelOneLayout.ExitRoomX, LevelOneLayout.ExitRoomZ, out var exit)
			&& exit.HasExit
			&& MineralRules.RequiredTool(exit.Mineral) == ToolTier.Reinforced
	);
	Check("nivel 1: hay un pico abandonado en el nivel (la salida es alcanzable sin farmear)", LevelOneLayout.TryGet(1, -1, out var tool) && tool.Loot == ChestLoot.Pickaxe);
	Check("nivel 1: salas lejanas quedan para la generación procedural", !LevelOneLayout.TryGet(20, 20, out _));
	Check("nivel 1: la pista desde el campamento apunta al este y al norte", LevelOneLayout.HintTowardsExit(0, 0).Contains("este") && LevelOneLayout.HintTowardsExit(0, 0).Contains("norte"));
}

// --- WorldSaveState: round-trip de los datos nuevos ---

{
	var original = new WorldSaveState { Capacity = 45, ToolLevel = 2 };
	original.MarkChestOpened(1, -1);
	original.AddDefense(2, 0, -3);

	string json = JsonSerializer.Serialize(original.ToDto(0, 0, 0));
	WorldSaveState restored = WorldSaveState.FromDto(JsonSerializer.Deserialize<WorldSaveData>(json));

	Check("guardado: conserva capacidad y nivel del pico", restored.Capacity == 45 && restored.ToolLevel == 2);
	Check("guardado: conserva cofres abiertos y defensas", restored.IsChestOpened(1, -1) && restored.PlacedDefenses.Contains((2, 0, -3)));

	WorldSaveState fromOldSave = WorldSaveState.FromDto(new WorldSaveData());
	Check("guardado: un save viejo (sin campos nuevos) arranca con mochila y pico por defecto", fromOldSave.Capacity == WorldSaveState.DefaultCapacity && fromOldSave.ToolLevel == 1);
}

// --- Salas visitadas y antorchas ---

{
	var state = new WorldSaveState();

	Check("salas: una sala nueva no está visitada", !state.IsRoomVisited(1, 0));
	Check("salas: la primera visita devuelve true (sala descubierta)", state.MarkRoomVisited(1, 0));
	Check("salas: la segunda visita devuelve false (ya se conocía)", !state.MarkRoomVisited(1, 0));

	state.AddTorch(3, 0, -2);

	WorldSaveState restored = WorldSaveState.FromDto(JsonSerializer.Deserialize<WorldSaveData>(JsonSerializer.Serialize(state.ToDto(0, 0, 0))));

	Check("guardado: conserva salas visitadas y antorchas", restored.IsRoomVisited(1, 0) && restored.PlacedTorches.Contains((3, 0, -2)));
}

Console.WriteLine();
Console.WriteLine($"{total - failures}/{total} tests OK");

return failures == 0 ? 0 : 1;
