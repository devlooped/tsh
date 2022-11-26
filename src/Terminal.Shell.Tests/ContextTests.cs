namespace Terminal.Shell;

record GitHub(string Login);

public class ContextTests
{
    [Menu(nameof(SameContext), "GitHub")]
    public void SameContext() { }

    [Menu(nameof(CanPushAndPopContext), "GitHub")]
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

    [TestAttributeCtor("IsTest && IsCtor")]
    [Fact]
    public void CanUseExpressionOnCtor()
    {
        var composition = CompositionSetup.CreateDefaultProvider();
        var context = composition.GetExportedValue<Context>();

        context.Push("IsTest");
        context.Push("IsCtor");

        Assert.True(context.Evaluate("IsTest && IsCtor"));
    }

    [TestAttributeCtor(expression: "IsTest && IsNamed")]
    [Fact]
    public void CanUseExpressionOnCtorNamed()
    {
        var composition = CompositionSetup.CreateDefaultProvider();
        var context = composition.GetExportedValue<Context>();

        context.Push("IsTest");
        context.Push("IsNamed");

        Assert.True(context.Evaluate("IsTest && IsNamed"));
    }

    [TestAttributeProp(Expression = "IsTest && IsProp")]
    [Fact]
    public void CanUseExpressionOnProp()
    {
        var composition = CompositionSetup.CreateDefaultProvider();
        var context = composition.GetExportedValue<Context>();

        context.Push("IsTest");
        context.Push("IsProp");

        Assert.True(context.Evaluate("IsTest && IsProp"));
    }

    [Fact]
    public void FailsForUnsupportedExpression()
    {
        var composition = CompositionSetup.CreateDefaultProvider();
        var context = composition.GetExportedValue<Context>();

        context.Push("IsTest");
        context.Push("IsSarasa");

        Assert.Throws<NotSupportedException>(() => context.Evaluate("IsTest && IsSarasa"));
    }

    [AttributeUsage(AttributeTargets.All)]
    public class TestAttributeCtor : System.Attribute
    {
        public TestAttributeCtor([ContextExpression] string expression) { }
    }

    [AttributeUsage(AttributeTargets.All)]
    public class TestAttributeProp : System.Attribute
    {
        [ContextExpression]
        public string? Expression { get; set; }
    }
}
