using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;

namespace MDownloader.Behaviors;

public class ScrollIntoViewForListBox : Behavior<ListView>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.SelectionChanged += AssociatedObject_SelectionChanged;
    }

    private void AssociatedObject_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListView)
        {
            ListBox listBox = sender as ListView;
            if (listBox.SelectedItem != null)
                listBox.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        if (listBox.SelectedItem != null)
                        {
                            listBox.UpdateLayout();
                            listBox.ScrollIntoView(listBox.SelectedItem);
                        }
                    }));
        }
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.SelectionChanged -=
            AssociatedObject_SelectionChanged;
    }
}