using Godot;

/// <summary>
/// Autoload "Global Listener" (Observer, clase 11): para avisos entre nodos que no
/// se conocen, como un cofre que quiere mostrar un mensaje en el HUD.
/// Si dos nodos se conocen por la escena, se conectan directo con señales.
/// </summary>
public partial class GameEvents : Node
{
	public static GameEvents Instance { get; private set; }

	// Mensaje corto ("Mochila llena", "Encontraste...").
	[Signal]
	public delegate void MessagePostedEventHandler(string text);

	// Texto largo para leer (notas).
	[Signal]
	public delegate void NoteOpenedEventHandler(string text);

	// El jugador llegó al final del nivel.
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
