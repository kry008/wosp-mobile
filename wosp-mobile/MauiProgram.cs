using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

#if MACCATALYST
using UIKit;
using CoreGraphics;
#endif

#if WINDOWS
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Windows.Graphics;
#endif

namespace wosp_mobile;


public static class MauiProgram
{
    
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseBarcodeReader()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.ConfigureLifecycleEvents(lifecycle =>
        {

#if MACCATALYST
            lifecycle.AddiOS(ios =>
            {
                ios.SceneWillConnect((scene, session, options) =>
                {
                    if (scene is UIWindowScene windowScene)
                    {
                        var size = new CGSize(430, 620);

                        // wymuszenie rozmiaru
                        windowScene.SizeRestrictions.MinimumSize = size;
                        windowScene.SizeRestrictions.MaximumSize = size;
                    }
                });
            });
#endif

#if WINDOWS
			lifecycle.AddWindows(windows =>
			{
				windows.OnWindowCreated(window =>
				{
					var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
					var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(handle);
					var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

					if(appWindow != null)
					{
						appWindow.Resize(new Windows.Graphics.SizeInt32(430, 680));
					}
				});
			});
#endif
        });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
