using System.ComponentModel;

namespace Terminal.Shell;

// This would be a generated class
public static partial class ExpressionContext
{
    public static partial class Generated
    {
        [Shared]
        [Export(typeof(IEvaluationContext))]
        [ExportMetadata("Expression", "GitHub")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public partial class _e98d5c36e753a133520419cd954dde3e : IEvaluationContext
        {
            readonly IContext context;

            [ImportingConstructor]
            public _e98d5c36e753a133520419cd954dde3e(IContext context)
                => this.context = context;

            public bool Initialized => context.IsActive(nameof(Initialized));
            public bool GitHub => context.IsActive(nameof(GitHub));
        }
    }
}

record GitHub(string Login);

public class ContextTests
{
    [Fact]
    public void CanPushAndPopContext()
    {
        var composition = CompositionSetup.CreateDefaultProvider();
        var context = composition.GetExportedValue<Context>();

        // Push a (named) context value
        var disposable = context.Push(nameof(GitHub), new GitHub("kzu"));

        // Query if it's active
        Assert.True(context.IsActive(nameof(GitHub)));

        // Can evaluate boolean expression, such as 'Initialized && GitHub'
        Assert.True(context.Evaluate("GitHub"));

        // Resolve single instance from named context (can GetAll<T> too)
        var github = context.Get<GitHub>(nameof(GitHub));

        Assert.NotNull(github);
        Assert.Equal("kzu", github.Login);

        // Unregisters context
        disposable.Dispose();

        // Now it's all null
        Assert.False(context.IsActive(nameof(GitHub)));
        Assert.Null(context.Get<GitHub>(nameof(GitHub)));
    }
}
