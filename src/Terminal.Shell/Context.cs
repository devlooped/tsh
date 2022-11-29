using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Terminal.Shell;

[Shared]
partial class Context : IContext
{
    public event PropertyChangedEventHandler? PropertyChanged;

    readonly ConcurrentDictionary<string, ImmutableList<IDictionary<string, object?>>> context
        = new(StringComparer.OrdinalIgnoreCase);

    readonly ConcurrentDictionary<string, ScriptRunner<bool>?> evaluators
        = new(StringComparer.OrdinalIgnoreCase);

    readonly Dictionary<string, Lazy<object>> evaluationContexts;

    [ImportingConstructor]
    public Context(
        [ImportMany("Terminal.Shell.ExpressionContext")] IEnumerable<Lazy<object, IDictionary<string, object?>>> evaluationContexts)
    {
        this.evaluationContexts = evaluationContexts
            .Select(x => new
            {
                Expression = x.Metadata["Expression"] as string,
                Value = (Lazy<object>)x
            })
            .GroupBy(x => x.Expression)
            .Where(x => x.Key != null)
            .ToDictionary(x => x.Key!, x => x.First().Value, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test constructor.
    /// </summary>
    internal Context() => evaluationContexts = new();

    public bool Evaluate(string expression)
    {
        var evaluator = evaluators.GetOrAdd(expression, x =>
        {
            if (!evaluationContexts.TryGetValue(expression, out var globals))
                return null;

            return CSharpScript
                .Create<bool>(expression, globalsType: globals.Value.GetType())
                .CreateDelegate();
        });

        if (evaluator == null ||
            !evaluationContexts.TryGetValue(expression, out var globals))
            throw new NotSupportedException($"Unsupported context expression '{expression}'.");

        return evaluator(globals.Value).Result;
    }

    public bool IsActive(string name) => context.ContainsKey(name);

    public IDisposable Push(string name, IDictionary<string, object?> values)
    {
        var list = context.AddOrUpdate(name,
            (_, dict) => ImmutableList.Create(dict),
            (_, list, dict) => list.Add(dict),
            values);

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        if (values is INotifyPropertyChanged changed)
        {
            return new CompositeDisposable(
                new PropertyChanger(this, name, changed),
                new ContextRemover(this, name, values));
        }

        return new ContextRemover(this, name, values);
    }

    void RaisePropertyChanged(string name)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    void Remove(string name, IDictionary<string, object?> values)
    {
        try
        {
            while (true)
            {
                if (!context.TryGetValue(name, out var list))
                    return;

                var updated = list.Remove(values);
                // If the values have already been removed, exit.
                if (updated == list)
                    return;

                if (updated.Count == 0)
                {
                    if (context.TryRemove(name, out _))
                        return;
                }
                else if (context.TryUpdate(name, updated, list))
                {
                    return;
                }
            }
        }
        finally
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public IReadOnlyDictionary<string, object?>? Get(string name)
        => context.TryGetValue(name, out var list) ? new ListDictionary(list) : null;

    class PropertyChanger : IDisposable
    {
        public PropertyChanger(Context context, string property, INotifyPropertyChanged sender)
        {
            Context = context;
            Property = property;
            Sender = sender;
            sender.PropertyChanged += OnPropertyChanged;
        }

        public Context Context { get; }
        public string Property { get; }
        public INotifyPropertyChanged Sender { get; }

        public void Dispose() => Sender.PropertyChanged -= OnPropertyChanged;

        void OnPropertyChanged(object? sender, PropertyChangedEventArgs e) => Context.RaisePropertyChanged(Property);
    }

    record ContextRemover(Context Context, string Name, IDictionary<string, object?> Values) : IDisposable
    {
        public void Dispose() => Context.Remove(Name, Values);
    }

    class ListDictionary : IReadOnlyDictionary<string, object?>
    {
        readonly ImmutableList<IDictionary<string, object?>> values;

        public ListDictionary(ImmutableList<IDictionary<string, object?>> values)
            => this.values = values;

        IEnumerable<KeyValuePair<string, object?>> GetEntries()
        {
            HashSet<string> keys = new(StringComparer.OrdinalIgnoreCase);

            for (var i = values.Count - 1; i >= 0; i--)
            {
                foreach (var value in values[i])
                {
                    if (!keys.Contains(value.Key))
                    {
                        yield return value;
                        keys.Add(value.Key);
                    }
                }
            }
        }

        public object? this[string key]
        {
            get
            {
                object? value = default;
                for (var i = values.Count - 1; i >= 0; i--)
                {
                    if (values[i].TryGetValue(key, out value))
                        break;
                }
                return value;
            }
        }

        public IEnumerable<string> Keys => GetEntries().Select(x => x.Key);

        public IEnumerable<object?> Values => GetEntries().Select(x => x.Value);

        // Intentionally use enumerable count, so we can filter out duplicate keys.
        public int Count => GetEntries().Count();

        public bool ContainsKey(string key)
        {
            foreach (var dict in values)
                if (dict.ContainsKey(key))
                    return true;

            return false;
        }

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => GetEntries().GetEnumerator();

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out object? value)
        {
            value = default;
            for (var i = values.Count - 1; i >= 0; i--)
            {
                if (values[i].TryGetValue(key, out value))
                    return true;
            }
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

