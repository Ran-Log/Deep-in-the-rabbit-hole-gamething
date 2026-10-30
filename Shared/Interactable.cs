using Godot;

/// <summary>
/// Base de lo que se usa con E (cama, estación, antorcha, cofre, grieta; GDD 2.1.9).
/// Cada subclase decide qué texto mostrar y qué pasa al usarla.
/// </summary>
public abstract partial class Interactable : StaticBody3D
{
	// Texto bajo la mira (ej. "Descansar"). Vacío = ahora no se puede usar.
	public abstract string GetPrompt(Player player);

	public abstract void Interact(Player player);
}
