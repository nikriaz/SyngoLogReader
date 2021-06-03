using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Windows;
using LogReader.Helpers;
using LogReader.Models;
using LogReader.ViewModel;

namespace LogReader
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly IHost _host;
        private readonly IConfiguration _configuration;

        public App()
        {
            //.NET Generic Host is an object that encapsulates an app's resources, incl. DI
            //https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host?view=aspnetcore-5.0

            var configuration = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json")
                        .Build();

            _configuration = configuration;
            _host = CreateHostBuilder().Build();
        }

        private IHostBuilder CreateHostBuilder()
        {
            return
                Host.CreateDefaultBuilder().ConfigureServices((hostContext, services) =>
                    {
                        ConfigureServices(services);
                    });
        }

        private void ConfigureServices(IServiceCollection services)
        {

            var connectionString = _configuration.GetConnectionString("LogDatabase"); //get connection string  from appsettings.json

            var connection = new SqliteConnection(connectionString);
            connection.Open(); //It will stay open forever. We need always-opened connection because of shared in-memory DB.

            services.AddDbContext<MessagingContext>(options =>
            {
                options.UseLoggerFactory(LoggerFactory.Create(builder => builder.AddDebug()));
                options.UseSqlite(connection);
                //options.UseSqlite(connectionString); //this is an option for "normal" on-disk DB.
            });

            Variables.states = _configuration.GetSection("Settings").Get<StateCollection>().StateDictionary;

            services.AddSingleton<MainWindow>();
            services.AddSingleton<MessagesViewModel>();
            services.AddScoped<IFileNavigator, FileNavigator>();
            services.AddScoped<IXMLWriter, XMLWriter>();
            services.AddTransient<ISQLActions, SQLActions>();

        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await _host.StartAsync();
            //_host.Services.GetService<MessagingContext>().Database.EnsureCreated(); //for non-migration DB init. Is not suitable for FTS SQLite option
            _host.Services.GetService<MessagingContext>().Database.Migrate();
            var mainWindow = _host.Services.GetService<MainWindow>();
            mainWindow.DataContext = _host.Services.GetService<MessagesViewModel>();
            mainWindow.Show();

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            using (_host)
            {
                await _host.StopAsync(TimeSpan.FromSeconds(5));
            }
            base.OnExit(e);
        }
    }
}