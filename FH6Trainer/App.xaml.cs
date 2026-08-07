namespace Polootyyy;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell())
        {
            Title = "Polootyyy - FH6 Trainer",
            Width = 1280,
            Height = 850,
            MinimumWidth = 1000,
            MinimumHeight = 650
        };
    }
}
