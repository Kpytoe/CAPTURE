using System.Windows;
using CapturaLocal.Editor.Annotations;

namespace CapturaLocal.Editor;

/// <summary>Una accion reversible sobre la lista de anotaciones.</summary>
public interface IUndoableAction
{
    void Apply(IList<Annotation> annotations);
    void Revert(IList<Annotation> annotations);
}

/// <summary>Anadir una anotacion nueva.</summary>
public sealed class AddAnnotationAction(Annotation annotation) : IUndoableAction
{
    public Annotation Annotation { get; } = annotation;

    public void Apply(IList<Annotation> annotations) => annotations.Add(Annotation);

    public void Revert(IList<Annotation> annotations) => annotations.Remove(Annotation);
}

/// <summary>
/// Borrar una anotacion. Guarda su posicion en la lista para que al deshacer vuelva al mismo
/// sitio: si volviese al final, cambiaria el orden de dibujado y lo que estaba debajo pasaria
/// a estar encima.
/// </summary>
public sealed class DeleteAnnotationAction(Annotation annotation, int index) : IUndoableAction
{
    public void Apply(IList<Annotation> annotations) => annotations.Remove(annotation);

    public void Revert(IList<Annotation> annotations) =>
        annotations.Insert(Math.Min(index, annotations.Count), annotation);
}

/// <summary>Mover una anotacion ya existente.</summary>
public sealed class MoveAnnotationAction(Annotation annotation, Vector delta) : IUndoableAction
{
    public void Apply(IList<Annotation> annotations) => annotation.Move(delta);

    public void Revert(IList<Annotation> annotations) => annotation.Move(-delta);
}

/// <summary>
/// Historial de la sesion de edicion (RF-10). Vive solo en memoria y muere con la captura,
/// tal como pide el SRS 10.
/// </summary>
public sealed class UndoStack
{
    private readonly List<Annotation> _annotations;
    private readonly Stack<IUndoableAction> _undo = new();
    private readonly Stack<IUndoableAction> _redo = new();

    public UndoStack(List<Annotation> annotations)
    {
        _annotations = annotations;
    }

    public event Action? Changed;

    public bool CanUndo => _undo.Count > 0;

    public bool CanRedo => _redo.Count > 0;

    /// <summary>Indica si hay cambios sin guardar, para poder preguntar al cerrar (RF-15).</summary>
    public bool IsDirty { get; private set; }

    public void Do(IUndoableAction action)
    {
        action.Apply(_annotations);
        _undo.Push(action);

        // Hacer algo nuevo despues de deshacer abandona la rama que se habia deshecho. Es el
        // comportamiento que espera cualquiera que haya usado un editor.
        _redo.Clear();

        IsDirty = true;
        Changed?.Invoke();
    }

    /// <summary>
    /// Registra una accion que YA esta aplicada. Lo usa el arrastre: la anotacion se mueve en
    /// vivo mientras el raton se desplaza, y al soltar se anota el total como un solo paso.
    /// Volver a aplicarla aqui la moveria el doble.
    /// </summary>
    public void DoWithoutApplying(IUndoableAction action)
    {
        _undo.Push(action);
        _redo.Clear();

        IsDirty = true;
        Changed?.Invoke();
    }

    public void Undo()
    {
        if (!CanUndo) return;

        var action = _undo.Pop();
        action.Revert(_annotations);
        _redo.Push(action);

        IsDirty = true;
        Changed?.Invoke();
    }

    public void Redo()
    {
        if (!CanRedo) return;

        var action = _redo.Pop();
        action.Apply(_annotations);
        _undo.Push(action);

        IsDirty = true;
        Changed?.Invoke();
    }

    /// <summary>Marca el estado actual como guardado, sin perder el historial.</summary>
    public void MarkSaved()
    {
        IsDirty = false;
        Changed?.Invoke();
    }
}
