using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;

[assembly: AvaloniaTestApplication(typeof(Platinum.Presentation.Avalonia.Tests.TestAppBuilder))]

namespace Platinum.Presentation.Avalonia.Tests;

public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<Platinum.Presentation.Avalonia.App>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
