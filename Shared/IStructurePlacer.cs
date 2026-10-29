using Godot;

/// <summary>
/// Algo que el jugador puede construir desde el menú de construcción (B).
/// BuildComponent solo apunta y muestra el fantasma; cada implementación
/// decide sus propias reglas (dónde se puede, cuánto cuesta) y cómo se
/// guarda. Agregar un tipo de construcción nuevo = una clase nueva que
/// implemente esto + una línea en BuildComponent.Options.
/// </summary>
public interface IStructurePlacer
{
	/// <param name="onTopOfSurface">True si se apunta a la cara de ARRIBA de algo (piso o bloque).</param>
	bool CanPlace(Vector3I cell, bool onTopOfSurface, out string reason);

	bool TryPlace(Vector3I cell, bool onTopOfSurface);
}
