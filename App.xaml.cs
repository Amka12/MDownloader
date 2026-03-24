using System.Windows;
using MDownloader.Services;
using MDownloader.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MDownloader;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var collection = new ServiceCollection();
        collection.AddSingleton<IFileService, FileService>();
        collection.AddSingleton<IYouTubeService, YouTubeService>();
        collection.AddSingleton<MainViewModel>();
        collection.AddTransient<MainWindow>();

        Services = collection.BuildServiceProvider();
        var mainWindow = Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (Services.GetService<MainViewModel>() is MainViewModel vm) vm.Dispose();
        //Services.Dispose();
        base.OnExit(e);
    }
}