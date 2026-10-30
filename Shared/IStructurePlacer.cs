using Godot;

/// <summary>
/// Algo que se construye con B. Cada implementación decide dónde se puede y cuánto cuesta.
/// Para agregar uno nuevo: una clase que implemente esto y una línea en BuildComponent.Options.
/// </summary>
public interface IStructurePlacer
{
	// onTopOfSurface = se apunta a la cara de arriba de algo.
	bool CanPlace(Vector3I cell, bool onTopOfSurface, out string reason);

	bool TryPlace(Vector3I cell, bool onTopOfSurface);
}
