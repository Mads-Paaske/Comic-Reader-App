using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComicReaderApp;

public partial class LibraryPage : ContentPage, IQueryAttributable
{
    readonly ShelfLineDrawable _drawable = new();
    readonly LibraryViewModel _vm;
    
    double _lastWidth = -1;

    public LibraryPage(LibraryViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = _vm;

        ShelfLinesView.Drawable = _drawable;

        BooksFlex.Loaded += (s, e) =>
        {
            Dispatcher.Dispatch(RecalculateShelfLines);
        };

        _vm.Books.CollectionChanged += (s, e) =>
        {
            Dispatcher.Dispatch(RecalculateShelfLines);
        };
    }
    
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadBooksAsync();
    }

    void OnFlexLayoutSizeChanged(object sender, EventArgs e)
    {
        const double minSlotWidth = 100;

        double available = BooksFlex.Width;
        if (available <= 0) return;

        if (Math.Abs(available - _lastWidth) > 0.5)
        {
            _lastWidth = available;
            int columns = Math.Max(1, (int)(available / minSlotWidth));
            _vm.SlotWidth = Math.Floor(available / columns);
        }

        Dispatcher.Dispatch(RecalculateShelfLines);   // defer past the current layout pass
    }

    void RecalculateShelfLines()
    {
        var children = BooksFlex.Children
            .OfType<VisualElement>()
            .Where(v => v.Height > 0) // ignore anything not yet laid out
            .ToList();

        if (children.Count == 0) return;

        // Group into rows: items in the same row share (almost) the same Y.
        // Rounding guards against tiny floating-point differences.
        var rows = children
            .GroupBy(v => Math.Round(v.Y, 1))
            .OrderBy(g => g.Key)
            .ToList();

        _drawable.LineYPositions.Clear();
        _drawable.ContentWidth = (float)BooksFlex.Width;

        for (int i = 0; i < rows.Count - 1; i++)
        {
            float rowBottom = (float)rows[i].Max(v => v.Y + v.Height);
            float nextRowTop = (float)rows[i + 1].Min(v => v.Y);
            _drawable.LineYPositions.Add((rowBottom + nextRowTop) / 2f);
        }
        
        foreach (var v in children.Take(4))
            System.Diagnostics.Debug.WriteLine(
                $"child X={v.X:F1}, Width={v.Width:F1}");
        
        ShelfLinesView.Invalidate(); // tells GraphicsView "redraw now"
    }
    
    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("NewBook", out var newValue) && newValue is Book newBook)
        {
            query.Remove("NewBook");   // consumed — don't act on this again if replayed
            await _vm.AddBook(newBook);
        }
        else if (query.TryGetValue("UpdatedBook", out var updatedValue) && updatedValue is Book updatedBook)
        {
            query.Remove("UpdatedBook");
            await _vm.UpdateBook(updatedBook);
        }
    }
}