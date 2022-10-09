using System.Collections;

namespace Terminal.Shell;

public class ShellView : Toplevel, ICollection<View>
{
    public ShellView()
    {
        Width = Dim.Fill(1);
        Height = Dim.Fill(1);
    }

    public int Count => Subviews.Count;
    public bool IsReadOnly => false;
    public bool Contains(View item) => Subviews.Contains(item);
    public void CopyTo(View[] array, int arrayIndex) => Subviews.CopyTo(array, arrayIndex);
    public IEnumerator<View> GetEnumerator() => Subviews.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => Subviews.GetEnumerator();
    bool ICollection<View>.Remove(View item) => Subviews.Remove(item);
}
