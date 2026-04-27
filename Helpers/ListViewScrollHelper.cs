using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace MDownloader.Helpers;

public static class ListViewScrollHelper
{
    public static readonly DependencyProperty ScrollToItemProperty =
        DependencyProperty.RegisterAttached(
            "ScrollToItem",
            typeof(object),
            typeof(ListViewScrollHelper),
            new PropertyMetadata(null, OnScrollToItemChanged));

    public static void SetScrollToItem(DependencyObject obj, object value)
    {
        obj.SetValue(ScrollToItemProperty, value);
    }

    public static object GetScrollToItem(DependencyObject obj)
    {
        return obj.GetValue(ScrollToItemProperty);
    }

    private static void OnScrollToItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ListView listView && e.NewValue != null)
            listView.Dispatcher.BeginInvoke(new Action(() => { listView.ScrollIntoView(e.NewValue); }), DispatcherPriority.Background);
    }
}