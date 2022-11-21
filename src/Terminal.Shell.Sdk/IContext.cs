using System.ComponentModel;

namespace Terminal.Shell;

/// <summary>
/// Provides context facilities for evaluating, pushing and 
/// retrieving contextual information.
/// </summary>
public interface IContext
{
    /// <summary>
    /// Evaluates a boolean expression against the current context.
    /// </summary>
    /// <param name="expression">A boolean expression using context names, such 
    /// as <c>Initialized && GitHub</c></param>
    /// <returns><see langword="true"/> if the entire expression evaluates to 
    /// true, where each context name is evaluated using <see cref="IsActive"/>.</returns>
    bool Evaluate([ContextExpression] string expression);

    /// <summary>
    /// Gets the first context value compatible with <typeparamref name="T"/> with 
    /// the given named context.
    /// </summary>
    /// <typeparam name="T">The type of the context value to retrieve.</typeparam>
    /// <param name="name">Name the context was registered with, using <see cref="Push"/>.</param>
    /// <returns>The pushed context value, or <see langword="null"/> if no such context 
    /// is currently active (or no compatible type can be retrieved).</returns>
    T? Get<T>(string name);

    /// <summary>
    /// Gets all context values compatible with <typeparamref name="T"/> with 
    /// the given named context.
    /// </summary>
    /// <typeparam name="T">The type of the context values to retrieve.</typeparam>
    /// <param name="name">Name the context values were registered with, using <see cref="Push"/>.</param>
    /// <returns>The compatible values or an empty enumeration if the context isn't 
    /// active or no compatible values are found.</returns>
    IEnumerable<T> GetAll<T>(string name);

    /// <summary>
    /// Gets whether the given named context is currently active.
    /// </summary>
    /// <param name="name">Context name, as used to <see cref="Push"/> contexts so they 
    /// become active.</param>
    /// <returns><see langword="true"/> if the given context is active.</returns>
    bool IsActive(string name);

    /// <summary>
    /// Pushes a context value with the given name, causing the 
    /// given context to become active.
    /// </summary>
    /// <typeparam name="T">Type of value being pushed.</typeparam>
    /// <param name="name">Name of the context to activate with the value.</param>
    /// <returns>A <see cref="IDisposable"/> object that can be used to 
    /// deactivate the context that was pushed. No other components other 
    /// than the pushing one can deactivate it.</returns>
    IDisposable Push<T>(string name, T value);
}

/// <summary>
/// Marker interface used by <see cref="IContext"/> to retreive 
/// available evaluation contexts for given expressions.
/// </summary>
/// <remarks>
/// Implementations of this interface are generated automatically 
/// by the SDK for context expressions in use throughout the code base,
/// so it should typically not be implemented manually.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IEvaluationContext { }

/// <summary>
/// Attribute applied to string values to denote they are 
/// context expressions to be used in conjuntion (directly or indirectly) with 
/// <see cref="IContext.Evaluate(string)"/>.
/// </summary>
/// <remarks>
/// Used typically by the SDK itself, and not intended for 
/// consumption by end users.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public class ContextExpressionAttribute : System.Attribute { }