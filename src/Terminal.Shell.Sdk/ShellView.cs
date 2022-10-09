using System.Collections;

namespace Terminal.Shell;

/// <summary>
/// A view that can be created with object initializer syntax.
/// </summary>
/// <devdoc>
/// Remove if https://github.com/gui-cs/Terminal.Gui/pull/2079 ships.
/// </devdoc>
public class ShellView : View, IEnumerable
{
    IEnumerator IEnumerable.GetEnumerator() => Subviews.GetEnumerator();
}
