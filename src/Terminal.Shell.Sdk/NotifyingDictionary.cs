using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq.Expressions;

namespace Terminal.Shell;

/// <summary>
/// A dictionary that forwards change notifications from an inner 
/// implementation of <see cref="INotifyPropertyChanged"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public class NotifyingDictionary : Dictionary<string, object?>, IDisposable, INotifyPropertyChanged
{
    readonly INotifyPropertyChanged changed;
    readonly ConcurrentDictionary<string, Func<object, object?>> getters = new();

    /// <summary>
    /// Forwards change notifications from an inner source.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Creates a new instance of the <see cref="NotifyingDictionary"/> class.
    /// </summary>
    public NotifyingDictionary(INotifyPropertyChanged changed)
    {
        this.changed = changed;
        changed.PropertyChanged += OnPropertyChanged;
    }

    /// <summary>
    /// Unsubscribes from the inner property changed source.
    /// </summary>
    public void Dispose() => changed.PropertyChanged -= OnPropertyChanged;

    void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        this[e.PropertyName] = getters.GetOrAdd(e.PropertyName, name =>
        {
            var arg = Expression.Parameter(typeof(object), "x");
            return Expression.Lambda<Func<object, object?>>(
                Expression.Convert(
                    Expression.Property(
                        Expression.Convert(arg, sender.GetType()),
                        name),
                    typeof(object)), arg)
                .Compile();
        }).Invoke(changed);

        PropertyChanged?.Invoke(this, e);
    }
}
