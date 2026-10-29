using Godot;

/// <summary>
/// Autoload: bus de eventos globales que no tienen un dueño natural en el
/// árbol. Ej. un cofre dentro de una sala generada (que se carga y descarga
/// con el streaming) quiere mostrar "Encontraste una mochila" en el HUD del
/// jugador, pero no conoce al HUD ni tiene cómo recibir su ruta en el
/// editor. Con el bus, el cofre publica y el HUD escucha, sin conocerse.
///
/// Se usa SOLO para estos casos; cuando dos nodos sí se conocen por escena
/// (ej. Hud ↔ MiningComponent) se conectan directo con señales.
/// </summary>
public partial class GameEvents : Node
{
	public static GameEvents Instance { get; private set; }

	/// <summary>Mensaje corto para el jugador ("Mochila llena", "Encontraste...").</summary>
	[Signal]
	public delegate void MessagePostedEventHandler(string text);

	/// <summary>Texto largo para leer (notas abandonadas).</summary>
	[Signal]
	public delegate void NoteOpenedEventHandler(string text);

	/// <summary>El jugador llegó al final del nivel.</summary>
	[Signal]
	public delegate void LevelCompletedEventHandler();

	public override void _EnterTree()
	{
		Instance = this;
	}

	public override void _ExitTree()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	public static void PostMessage(string text)
	{
		Instance?.EmitSignal(SignalName.MessagePosted, text);
	}

	public static void OpenNote(string text)
	{
		Instance?.EmitSignal(SignalName.NoteOpened, text);
	}

	public static void CompleteLevel()
	{
		Instance?.EmitSignal(SignalName.LevelCompleted);
	}
}
