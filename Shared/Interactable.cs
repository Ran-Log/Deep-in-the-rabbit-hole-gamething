using Godot;

/// <summary>
/// Base de todo objeto del entorno con el que se interactúa con la acción
/// "interact" (E) apuntándole con la mira: cama, estación de mejora,
/// antorchas, cofres, la grieta de salida (GDD 2.1.9).
///
/// Es un StaticBody3D para que el rayo de InteractionComponent lo pueda
/// golpear con su propia colisión. Cada subclase solo decide qué texto
/// mostrar y qué pasa al interactuar; InteractionComponent no conoce
/// ningún tipo concreto.
/// </summary>
public abstract partial class Interactable : StaticBody3D
{
	/// <summary>Texto bajo la mira, sin la tecla (ej. "Descansar"). Vacío = no se puede interactuar ahora.</summary>
	public abstract string GetPrompt(Player player);

	public abstract void Interact(Player player);
}
