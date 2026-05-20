using System.Windows;
using ExamRosterApp.Application;
using ExamRosterApp.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ExamRosterApp.Desktop;

public partial class App : System.Windows.Application
{
    private IHost? _host;

    private async void Application_Startup(object sender, StartupEventArgs e)
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                var connectionString = context.Configuration.GetConnectionString("examrosterdb")
                    ?? "Server=localhost,1433;Database=ExamRoster_LocalDev;User Id=sa;Password=Your_strong_password123;TrustServerCertificate=True";

                services.AddDbContext<ExamRosterDbContext>(options => options.UseSqlServer(connectionString));
                services.AddScoped<IAppRepository, EfRepository>();
                services.AddScoped<IRosterGenerator, RosterGenerator>();
                services.AddScoped<ITeacherService, TeacherService>();
                services.AddScoped<IExamDutySlotService, ExamDutySlotService>();
                services.AddScoped<IRosterService, RosterService>();
                services.AddScoped<IDatabaseSetupService, DatabaseSetupService>();
                services.AddScoped<MainViewModel>();
                services.AddScoped<MainWindow>();
            })
            .Build();

        await _host.StartAsync();

        var window = _host.Services.GetRequiredService<MainWindow>();
        window.DataContext = _host.Services.GetRequiredService<MainViewModel>();
        window.Show();
    }
}
