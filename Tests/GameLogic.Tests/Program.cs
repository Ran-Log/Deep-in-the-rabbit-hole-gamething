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

// --- WorldSaveState: mochila con capacidad ---

{
	var state = new WorldSaveState { Capacity = 5 };

	Check("mochila: agrega si cabe", state.TryAddItem("Stone", 3));
	Check("mochila: rechaza si NO cabe todo (todo o nada)", !state.TryAddItem("Coal", 3) && state.GetMineralCount("Coal") == 0);
	Check("mochila: el total cuenta todos los ítems", state.TotalItems == 3 && state.FreeSpace == 2);
	Check("mochila: quitar más de lo que hay falla y no cambia nada", !state.RemoveItem("Stone", 4) && state.GetMineralCount("Stone") == 3);
	Check("mochila: quitar todo borra la entrada", state.RemoveItem("Stone", 3) && !state.MineralCounts.ContainsKey("Stone"));
}

// --- WorldSaveState: cobrar un costo (todo o nada) ---
// Las recetas reales están en Data/Upgrades y se prueban en Tests/Integration/GameDataTest.

{
	var state = new WorldSaveState();
	state.AddMineral("Stone", 8);
	state.AddMineral("Coal", 3);

	var cost = new System.Collections.Generic.Dictionary<string, int> { ["Stone"] = 8, ["Coal"] = 4 };

	Check("costo: si falta algo no se cobra nada", !state.TrySpend(cost) && state.GetMineralCount("Stone") == 8);

	state.AddMineral("Coal", 1);

	Check("costo: con todo lo necesario se cobra completo", state.TrySpend(cost) && state.GetMineralCount("Stone") == 0 && state.GetMineralCount("Coal") == 0);
}

// --- Nivel 1 ---

{
	// Las salas fijas de cada nivel están en Data/Levels y se prueban en Tests/Integration/LevelDataTest.
	string hint = ExitHint.Towards(0, 0, 3, -1);
	Check("pista: desde el campamento a la salida (3,-1) apunta al este y al norte", hint.Contains("3 salas al este") && hint.Contains("1 salas al norte"));
	Check("pista: hacia el oeste y el sur", ExitHint.Towards(5, 5, 2, 7).Contains("oeste") && ExitHint.Towards(5, 5, 2, 7).Contains("sur"));
	Check("pista: estando en la salida dice 'aquí mismo'", ExitHint.Towards(3, -1, 3, -1).Contains("aquí mismo"));
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

// --- Vida y día (bug 0.6) ---

{
	var state = new WorldSaveState { Day = 3 };
	state.SetHealth("player", 40);
	state.SetHealth("camp", 70);

	WorldSaveState restored = WorldSaveState.FromDto(JsonSerializer.Deserialize<WorldSaveData>(JsonSerializer.Serialize(state.ToDto(0, 0, 0))));

	Check("guardado: conserva el día", restored.Day == 3);
	Check("guardado: conserva la vida del jugador y del campamento",
		restored.TryGetHealth("player", out int player) && player == 40
		&& restored.TryGetHealth("camp", out int camp) && camp == 70);

	WorldSaveState fromOldSave = WorldSaveState.FromDto(new WorldSaveData());
	Check("guardado: un save viejo (sin día ni vida) arranca en el día 1 y sin vidas guardadas",
		fromOldSave.Day == 1 && !fromOldSave.TryGetHealth("player", out _));
}

// --- Niveles (feature 1.1) ---

{
	var state = new WorldSaveState { Capacity = 45, ToolLevel = 2, Day = 4 };
	state.AddMineral("Iron", 5);
	state.MarkCellMined(1, 0, 2, 0, 2);
	state.MarkChestOpened(1, 0);
	state.AddDefense(1, 0, 1);
	state.AddTorch(2, 0, 2);
	state.MarkRoomVisited(1, 0);
	state.SetHealth("camp", 30);

	Check("nivel: una partida nueva empieza en el nivel 1", new WorldSaveState().CurrentLevel == 1);

	state.StartNextLevel();

	Check("nivel: pasar de nivel suma 1", state.CurrentLevel == 2);
	Check("nivel: se conservan inventario, pico, mochila y día",
		state.GetMineralCount("Iron") == 5 && state.ToolLevel == 2 && state.Capacity == 45 && state.Day == 4);
	Check("nivel: el mundo es nuevo (sin bloques minados, cofres, defensas, antorchas ni salas visitadas)",
		!state.IsCellMined(1, 0, 2, 0, 2) && !state.IsChestOpened(1, 0)
		&& !state.PlacedDefenses.Any() && !state.PlacedTorches.Any() && !state.IsRoomVisited(1, 0));
	Check("nivel: el campamento nuevo empieza con la vida llena", !state.TryGetHealth("camp", out _));

	WorldSaveState restored = WorldSaveState.FromDto(JsonSerializer.Deserialize<WorldSaveData>(JsonSerializer.Serialize(state.ToDto(0, 0, 0))));
	Check("nivel: el nivel actual se guarda", restored.CurrentLevel == 2);
	Check("nivel: un save viejo carga en el nivel 1", WorldSaveState.FromDto(new WorldSaveData()).CurrentLevel == 1);
}

// --- Nidos de hormigas (feature 1.2) ---

{
	var state = new WorldSaveState();

	Check("nido: uno nuevo no está destruido", !state.IsNestDestroyed(2, 1));

	state.MarkNestDestroyed(2, 1);

	WorldSaveState restored = WorldSaveState.FromDto(JsonSerializer.Deserialize<WorldSaveData>(JsonSerializer.Serialize(state.ToDto(0, 0, 0))));
	Check("nido: destruido se guarda", restored.IsNestDestroyed(2, 1) && !restored.IsNestDestroyed(0, 0));

	restored.StartNextLevel();
	Check("nido: en el nivel siguiente los nidos son nuevos", !restored.IsNestDestroyed(2, 1));
}

// --- Gas explosivo (feature 1.7) ---

{
	var state = new WorldSaveState();
	state.MarkGasExploded(3, -2);

	WorldSaveState restored = WorldSaveState.FromDto(JsonSerializer.Deserialize<WorldSaveData>(JsonSerializer.Serialize(state.ToDto(0, 0, 0))));
	Check("gas: el que explotó se guarda", restored.IsGasExploded(3, -2) && !restored.IsGasExploded(0, 0));

	restored.StartNextLevel();
	Check("gas: en el nivel siguiente vuelve a haber gas", !restored.IsGasExploded(3, -2));
}

// --- Daño por caída (feature 1.8) ---

Check("caida: saltar desde un bloque (4.3 m) no hace daño", FallDamage.For(4.3f, 5.0f, 10.0f) == 0);
Check("caida: justo en el límite no hace daño", FallDamage.For(5.0f, 5.0f, 10.0f) == 0);
Check("caida: 8 m son 3 m de más, 30 de daño", FallDamage.For(8.0f, 5.0f, 10.0f) == 30);
Check("caida: redondea (6.26 m da 13)", FallDamage.For(6.26f, 5.0f, 10.0f) == 13);

// --- Techo de las salas (feature 1.5) ---

{
	// Sala de 24 x 24 m (de -13 a 11), agujero de 6 x 6 m en el medio (de -4 a 2).
	var full = (-13f, -13f, 11f, 11f);
	var hole = (-4f, -4f, 2f, 2f);

	var withoutHole = RoomMath.CeilingPieces(full, hole, hasHole: false);
	Check("techo: sin tragaluz es una sola pieza que cubre todo", withoutHole.Count == 1 && withoutHole[0] == full);

	var withHole = RoomMath.CeilingPieces(full, hole, hasHole: true);
	float Area((float MinX, float MinZ, float MaxX, float MaxZ) r) => (r.MaxX - r.MinX) * (r.MaxZ - r.MinZ);
	float piecesArea = withHole.Sum(Area);

	Check("techo: con tragaluz son 4 piezas", withHole.Count == 4);
	Check("techo: las piezas suman el total menos el agujero", Math.Abs(piecesArea - (Area(full) - Area(hole))) < 0.001f);
	Check("techo: ninguna pieza tapa el centro del agujero",
		withHole.All(r => !(r.MinX < -1f && -1f < r.MaxX && r.MinZ < -1f && -1f < r.MaxZ)));
}

Console.WriteLine();
Console.WriteLine($"{total - failures}/{total} tests OK");

return failures == 0 ? 0 : 1;
