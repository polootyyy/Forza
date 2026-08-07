using Microsoft.Extensions.Logging;
using Polootyyy.Services;
using Polootyyy.ViewModels;
using Polootyyy.Views;

namespace Polootyyy;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Services
        builder.Services.AddSingleton<MemoryService>();
        builder.Services.AddSingleton<SignatureService>();
        builder.Services.AddSingleton<CrcBypassEngine>();
        builder.Services.AddSingleton<VehHookingEngine>();
        builder.Services.AddSingleton<HotkeyService>();
        builder.Services.AddSingleton<CheatEngineService>();

        // ViewModels
        builder.Services.AddSingleton<MainViewModel>();

        // Pages
        builder.Services.AddSingleton<MainPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
