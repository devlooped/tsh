using System.ComponentModel;

namespace Terminal.Shell;

public abstract class ContentView : View
{
    string title;
    View? content;

    public event EventHandler<string>? TitleChanged;

    public ContentView(string title)
        : base()
    {
        this.title = title;

        Width = Dim.Fill();
        Height = Dim.Fill();
    }

    public string Title
    {
        get => title;
        protected set
        {
            title = value;

            SetNeedsDisplay();
            TitleChanged?.Invoke(this, title);
        }
    }

    protected View? Content
    {
        get => content;
        set
        {
            content = value;

            if (content != null)
            {
                content.Width = Dim.Fill(1);
                content.Height = Dim.Fill(1);

                Add(content);
            }
        }
    }
}
