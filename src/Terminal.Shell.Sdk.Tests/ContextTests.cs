using System.ComponentModel;
using ScenarioTests;

namespace Terminal.Shell;

// Uses PropertyChanged.Fody to make it notifying automatically
record GitHub(string Login) : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string? Organization { get; set; }
    public int? Id { get; init; }
}
public record CurrentUser(int? Id, string Login, bool IsAdmin);

public partial class ContextTests
{
    [TestAttributeCtor(nameof(GitHub))]
    [Scenario(NamingPolicy = ScenarioTestMethodNamingPolicy.Test)]
    public void BasicUsage(ScenarioContext scenario)
    {
        var composition = CompositionSetup.CreateDefaultProvider();
        var context = composition.GetExportedValue<IContext>();

        string? changed = default;
        context.PropertyChanged += (sender, args) => changed = args.PropertyName;

        var user = new GitHub("kzu")
        {
            Organization = "Devlooped",
            Id = 1
        };

        scenario.Fact("Context starts inactive", 
            () => Assert.False(context.IsActive<GitHub>()));

        var disposable = context.Push(user);

        scenario.Fact("Context becomes active after a push", 
            () => Assert.True(context.IsActive<GitHub>()));

        scenario.Fact("Context raises PropertyChanged on activation with the context name", 
            () => Assert.Equal(nameof(GitHub), changed));

        changed = null;
        user.Organization = "Moq";

        // After reset, we updated data object, changed should be raised
        scenario.Fact("Data object changes cause context PropertyChanged to be raised again", 
            () => Assert.Equal(nameof(GitHub), changed));

        scenario.Fact("Context data dictionary is updated from data object change too", 
            () => Assert.Equal("Moq", context.Get(nameof(GitHub))![nameof(GitHub.Organization)]));

        disposable.Dispose();

        scenario.Fact("Disposing context push deactivates the context",
            () => Assert.False(context.IsActive<GitHub>()));

        changed = null;
        user.Organization = "Devlooped";

        scenario.Fact("Further changes to data object do not trigger context changes",
            () => Assert.Null(changed));
    }

    [Fact]
    public void MapRecordFromDictionary()
    {
        var composition = CompositionSetup.CreateDefaultProvider();
        var context = composition.GetExportedValue<IContext>();

        context.Push("CurrentUser", new Dictionary<string, object?>
        {
            ["Id"] = 123,
            ["Login"] = "kzu",
            ["IsAdmin"] = true
        });

        Assert.True(context.TryGet<CurrentUser>("CurrentUser", out var user));
        Assert.True(context.TryGet<CurrentUser>(out var user2));

        Assert.True(user.Equals(user2));

        Assert.NotNull(user);
        Assert.Equal(123, user.Id);
        Assert.Equal("kzu", user.Login);
        Assert.True(user.IsAdmin);
    }

    [Menu(nameof(SameContext), "GitHub")]
    public void SameContext() { }

    [Menu(nameof(CanPushAndPopContext), "GitHub")]
    [Fact]
    public void CanPushAndPopContext()
    {
        var composition = CompositionSetup.CreateDefaultProvider();
        var context = composition.GetExportedValue<IContext>();

        Assert.NotNull(context);

        // Push a (named) context value
        var disposable = context.Push(nameof(GitHub), new GitHub("kzu"));

        // Query if it's active
        Assert.True(context.IsActive(nameof(GitHub)));

        // Can evaluate boolean expression, such as 'Initialized && GitHub'
        Assert.True(context.Evaluate("GitHub"));

        Assert.True(context.TryGet<GitHub>(nameof(GitHub), out var github));

        Assert.NotNull(github);
        Assert.Equal("kzu", github.Login);

        // Unregisters context
        disposable.Dispose();

        // Now it's all null
        Assert.False(context.IsActive(nameof(GitHub)));
        Assert.False(context.TryGet<GitHub>(nameof(GitHub), out _));
    }

    [Menu(nameof(CanPushAndPopUnnamedContext), "GitHub")]
    [Fact]
    public void CanPushAndPopUnnamedContext()
    {
        var composition = CompositionSetup.CreateDefaultProvider();
        var context = composition.GetExportedValue<IContext>();

        Assert.NotNull(context);

        // Push a (named) context value
        var disposable = context.Push(new GitHub("kzu"));

        // Query if it's active
        Assert.True(context.IsActive(nameof(GitHub)));

        // Can evaluate boolean expression, such as 'Initialized && GitHub'
        Assert.True(context.Evaluate("GitHub"));

        Assert.True(context.TryGet<GitHub>(out var github));

        Assert.NotNull(github);
        Assert.Equal("kzu", github.Login);

        // Unregisters context
        disposable.Dispose();

        // Now it's all null
        Assert.False(context.IsActive(nameof(GitHub)));
        Assert.False(context.TryGet<GitHub>(out _));
    }

    [TestAttributeCtor("IsTest && IsCtor")]
    [Fact]
    public void CanUseExpressionOnCtor()
    {
        var composition = CompositionSetup.CreateDefaultProvider();
        var context = composition.GetExportedValue<IContext>();

        context.Push("IsTest");
        context.Push("IsCtor");

        Assert.True(context.Evaluate("IsTest && IsCtor"));
    }

    [TestAttributeCtor(expression: "IsTest && IsNamed")]
    [Fact]
    public void CanUseExpressionOnCtorNamed()
    {
        var composition = CompositionSetup.CreateDefaultProvider();
        var context = composition.GetExportedValue<IContext>();

        context.Push("IsTest");
        context.Push("IsNamed");

        Assert.True(context.Evaluate("IsTest && IsNamed"));
    }

    [TestAttributeProp(Expression = "IsTest && IsProp")]
    [Fact]
    public void CanUseExpressionOnProp()
    {
        var composition = CompositionSetup.CreateDefaultProvider();
        var context = composition.GetExportedValue<IContext>();

        context.Push("IsTest");
        context.Push("IsProp");

        Assert.True(context.Evaluate("IsTest && IsProp"));
    }

    [Fact]
    public void FailsForUnsupportedExpression()
    {
        var composition = CompositionSetup.CreateDefaultProvider();
        var context = composition.GetExportedValue<IContext>();

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