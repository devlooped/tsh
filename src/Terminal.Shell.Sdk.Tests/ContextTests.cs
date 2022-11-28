﻿namespace Terminal.Shell;

record GitHub(string Login)
{
    public string? Organization { get; init; }
    public int? Id { get; init; }
};
public record CurrentUser(int? Id, string Login, bool IsAdmin);

public static class ContextExtensionTests
{
    public class ContextTests
    {
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
}