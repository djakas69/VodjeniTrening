using Microsoft.Extensions.Logging;
using VodjeniTrening.Services;
using VodjeniTrening.ViewModels;

namespace VodjeniTrening;

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

#if DEBUG
		builder.Logging.AddDebug();
#endif

		builder.Services.AddSingleton(new HttpClient
		{
			BaseAddress = new Uri(ApiSettings.BaseUrl),
			Timeout = TimeSpan.FromSeconds(15)
		});
		builder.Services.AddSingleton<IWorkoutService, WorkoutService>();
		builder.Services.AddSingleton<WorkoutViewModel>();
		builder.Services.AddSingleton<MainPage>();

		return builder.Build();
	}
}
