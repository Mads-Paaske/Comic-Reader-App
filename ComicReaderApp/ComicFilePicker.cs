namespace ComicReaderApp;

/// <summary>Production <see cref="IComicFilePicker"/> backed by the MAUI platform file picker.</summary>
public class ComicFilePicker : IComicFilePicker
{
    public async Task<PickedFile?> PickComicAsync()
    {
        var result = await Microsoft.Maui.Storage.FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Select a comic file"
        });

        return result is null ? null : new PickedFile(result.FullPath, result.FileName);
    }
}
