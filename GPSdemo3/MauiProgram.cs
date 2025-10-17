using Microsoft.Extensions.Logging;

namespace GPSdemo3
{
    public static class DatabaseConfig
    {
        public static string ConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=C:\\USERS\\KENAN PRINS\\MATRICLEARNINGDB.MDF;Trusted_Connection=True;TrustServerCertificate=True;";
    }

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

            return builder.Build();
        }
    }
}
