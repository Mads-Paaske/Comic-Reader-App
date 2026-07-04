namespace ComicReaderApp;

public partial class MainPage : ContentPage, IQueryAttributable
{
    readonly MainPageViewModel _vm;

    double _currentScale = 1;
    double _startScale = 1;
    double _xOffset = 0;
    double _yOffset = 0;
    
    bool _isPinching = false;
    bool _panSessionValid = false;
    
    double _originX;
    double _originY;

    const double MinScale = 1;
    const double MaxScale = 4;

    public MainPage(MainPageViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = _vm;

        // Reset zoom/pan whenever the displayed page changes.
        _vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainPageViewModel.CurrentPageImage))
                ResetZoom();
        };
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("SelectedBook", out var value) && value is Book book)
        {
            await _vm.LoadBookAsync(book);
        }
    }

    void OnPinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
    {
        if (e.Status == GestureStatus.Started)
        {
            _isPinching = true;
            _startScale = PageImage.Scale;
            PageImage.AnchorX = 0;
            PageImage.AnchorY = 0;

            _originX = e.ScaleOrigin.X - (_xOffset / ZoomContainer.Width);
            _originY = e.ScaleOrigin.Y - (_yOffset / ZoomContainer.Height);
        }
        else if (e.Status == GestureStatus.Running)
        {
            _currentScale += (e.Scale - 1) * _startScale;
            _currentScale = Math.Clamp(_currentScale, MinScale, MaxScale);

            double targetX = _xOffset - (_originX * ZoomContainer.Width) * (_currentScale - _startScale) / _startScale;
            double targetY = _yOffset - (_originY * ZoomContainer.Height) * (_currentScale - _startScale) / _startScale;

            // Bounds clamp: translation must keep the image covering the viewport.
            PageImage.TranslationX = Math.Clamp(targetX, -ZoomContainer.Width * (_currentScale - 1), 0);
            PageImage.TranslationY = Math.Clamp(targetY, -ZoomContainer.Height * (_currentScale - 1), 0);
            PageImage.Scale = _currentScale;
        }
        else // Completed OR Canceled
        {
            _isPinching = false;

            // Snap threshold: "almost zoomed out" counts as zoomed out.
            if (_currentScale < 1.1)
            {
                ResetZoom();
            }
            else
            {
                _startScale = _currentScale;
                _xOffset = PageImage.TranslationX;
                _yOffset = PageImage.TranslationY;
            }
        }
    }
    
    void OnPreviousZoneTapped(object sender, TappedEventArgs e)
    {
        if (_currentScale <= MinScale && _vm.PreviousPageCommand.CanExecute(null))
            _vm.PreviousPageCommand.Execute(null);
    }

    void OnNextZoneTapped(object sender, TappedEventArgs e)
    {
        if (_currentScale <= MinScale && _vm.NextPageCommand.CanExecute(null))
            _vm.NextPageCommand.Execute(null);
    }

    void OnPanUpdated(object sender, PanUpdatedEventArgs e)
    {
        if (e.StatusType == GestureStatus.Started)
        {
            // A pan that begins during a pinch is the leftover finger — reject the whole session.
            _panSessionValid = !_isPinching;
            return;
        }

        if (!_panSessionValid || _isPinching || _currentScale <= MinScale)
            return;

        if (e.StatusType == GestureStatus.Running)
        {
            double targetX = _xOffset + e.TotalX;
            double targetY = _yOffset + e.TotalY;

            // Same bounds rule as in pinch.
            PageImage.TranslationX = Math.Clamp(targetX, -ZoomContainer.Width * (_currentScale - 1), 0);
            PageImage.TranslationY = Math.Clamp(targetY, -ZoomContainer.Height * (_currentScale - 1), 0);
        }
        else if (e.StatusType == GestureStatus.Completed)
        {
            _xOffset = PageImage.TranslationX;
            _yOffset = PageImage.TranslationY;
            _panSessionValid = false;
        }
    }

    void OnDoubleTapped(object sender, TappedEventArgs e) => ResetZoom();

    void ResetZoom()
    {
        _currentScale = 1;
        _startScale = 1;
        _xOffset = 0;
        _yOffset = 0;
        PageImage.Scale = 1;
        PageImage.TranslationX = 0;
        PageImage.TranslationY = 0;
    }

    async void OnLibraryButtonClicked(object sender, EventArgs e)
    {
        try
        {
            await Shell.Current.GoToAsync(nameof(LibraryPage));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Navigation failed", ex.Message, "OK");
        }
    }
}