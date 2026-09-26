namespace LifeQuest.Mobile.Controls;

/// <summary>BindableLayout ile doldurulan Grid'de her öğeyi sıradaki sütuna yerleştirir (eşit genişlikte segmentler).</summary>
public static class SegmentRow
{
    public static void Attach(Grid grid)
    {
        Layout(grid);
        grid.ChildAdded += (_, _) => Layout(grid);
    }

    private static void Layout(Grid grid)
    {
        for (var i = 0; i < grid.Children.Count; i++)
            Grid.SetColumn((BindableObject)grid.Children[i], i);
    }
}
