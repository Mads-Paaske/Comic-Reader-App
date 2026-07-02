namespace ComicReaderApp;

public class ShelfLineDrawable : IDrawable
{
    public List<float> LineYPositions { get; set; } = new();
    public float ContentWidth { get; set; }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.StrokeColor = Colors.Black;
        canvas.StrokeSize = 1;

        foreach (var y in LineYPositions)
            canvas.DrawLine(10, y, ContentWidth - 10, y);
    }
}