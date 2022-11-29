﻿using System.Collections.Specialized;
using System.ComponentModel;

namespace Terminal.Shell;

/// <summary>
/// Usability overloads for <see cref="IContext"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class IContextExtensions
{
    /// <summary>
    /// Pushes a named context without providing specific data for it.
    /// </summary>
    public static IDisposable Push(this IContext context, string name) => context.Push(name, new Dictionary<string, object?>());
}

/// <summary>
/// Provides context facilities for evaluating, pushing and 
/// retrieving contextual information.
/// </summary>
public interface IContext : INotifyPropertyChanged
{
    /// <summary>
    /// Evaluates a boolean expression against the current context.
    /// </summary>
    /// <param name="expression">A boolean expression using context names, such 
    /// as <c>Initialized &amp;&amp; GitHub</c></param>
    /// <returns><see langword="true"/> if the entire expression evaluates to 
    /// true, where each context name is evaluated using <see cref="IsActive"/>.</returns>
    bool Evaluate([ContextExpression] string expression);

    ///// <typeparam name="T">The type of the context value to retrieve.</typeparam>
    /// <summary>
    /// Gets the values for the given named context.
    /// </summary>
    /// <param name="name">Name the context was registered with, using <see cref="Push"/>.</param>
    /// <returns>The pushed context value, or <see langword="null"/> if no such context 
    /// is currently active (or no compatible type can be retrieved).</returns>
    IReadOnlyDictionary<string, object?>? Get(string name);

    /// <summary>
    /// Gets whether the given named context is currently active.
    /// </summary>
    /// <param name="name">Context name, as used to <see cref="Push"/> contexts so they 
    /// become active.</param>
    /// <returns><see langword="true"/> if the given context is active.</returns>
    bool IsActive(string name);

    ///// <typeparam name="T">Type of value being pushed.</typeparam>
    /// <summary>
    /// Pushes a context value with the given name, causing the 
    /// given context to become active.
    /// </summary>
    /// <param name="name">Name of the context to activate with the value.</param>
    /// <param name="values">The value to associate with the given context.</param>
    /// <returns>A <see cref="IDisposable"/> object that can be used to 
    /// deactivate the context that was pushed. No other components other 
    /// than the pushing one can deactivate it.</returns>
    IDisposable Push(string name, IDictionary<string, object?> values);
}

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