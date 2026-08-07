using Polootyyy.ViewModels;

namespace Polootyyy.Views;

public partial class MainPage : ContentPage
{
    private readonly RgbWaveDrawable _rgbDrawable = new();
    private IDispatcherTimer? _rgbTimer;

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

        RgbBorder.Drawable = _rgbDrawable;
        StartRgbAnimation();
    }

    private void StartRgbAnimation()
    {
        _rgbTimer = Dispatcher.CreateTimer();
        _rgbTimer.Interval = TimeSpan.FromMilliseconds(16);
        _rgbTimer.Tick += (_, _) =>
        {
            _rgbDrawable.Time += 0.016f;
            RgbBorder.Invalidate();
        };
        _rgbTimer.Start();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _rgbTimer?.Stop();
    }
}
