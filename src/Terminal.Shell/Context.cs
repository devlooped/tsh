using System.Collections.Concurrent;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Terminal.Shell;

[Shared]
partial class Context : IContext
{
    readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, object?>> context
        = new(StringComparer.OrdinalIgnoreCase);

    readonly ConcurrentDictionary<string, ScriptRunner<bool>?> evaluators
        = new(StringComparer.OrdinalIgnoreCase);

    readonly Dictionary<string, Lazy<IEvaluationContext>> evaluationContexts;

    [ImportingConstructor]
    public Context(
        [ImportMany] IEnumerable<Lazy<IEvaluationContext, IDictionary<string, object?>>> evaluationContexts)
    {
        this.evaluationContexts = evaluationContexts
            .Select(x => new
            {
                Expression = x.Metadata["Expression"] as string,
                Value = (Lazy<IEvaluationContext>)x
            })
            .GroupBy(x => x.Expression)
            .Where(x => x.Key != null)
            .ToDictionary(x => x.Key!, x => x.First().Value, StringComparer.OrdinalIgnoreCase);
    }

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

    public T? Get<T>(string name)
    {
        if (context.TryGetValue(name, out var values))
            return values.Values.OfType<T>().FirstOrDefault();

        return default;
    }

    public IEnumerable<T> GetAll<T>(string name)
    {
        if (context.TryGetValue(name, out var values))
            return values.Values.OfType<T>().ToArray();

        return Array.Empty<T>();
    }

    public IDisposable Push<T>(string name, T data)
    {
        var id = Guid.NewGuid();
        context.GetOrAdd(name, _ => new())
            .AddOrUpdate(id, data, (_, _) => data);

        return new ContextRemover(this, name, id);
    }

    void Remove(string name, Guid id)
    {
        if (context.TryGetValue(name, out var values) &&
            values.TryRemove(id, out _) &&
            values.IsEmpty)
            // Clear key if there are no more values for the named context
            context.TryRemove(name, out _);
    }

    record ContextRemover(Context Context, string Name, Guid Id) : IDisposable
    {
        public void Dispose() => Context.Remove(Name, Id);
    }
}

