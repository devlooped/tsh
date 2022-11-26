namespace Terminal.Gui;

/// <summary>
/// View usability extensions.
/// </summary>
public static class ViewExtensions
{
    /// <summary>
    /// Traverses all subviews in a breadth first manner.
    /// </summary>
    public static IEnumerable<View> TraverseSubViews(this View view) =>
        view.Subviews.Traverse(TraverseKind.BreadthFirst, v => v.Subviews);

    /// <summary>
    /// Gets the first <see cref="View.SuperView"/> of the given type.
    /// </summary>
    public static T? GetSuperView<T>(this View view) where T : View =>
        view.SuperView is T superView ? superView : view.SuperView is null ? default : view.SuperView.GetSuperView<T>();
}