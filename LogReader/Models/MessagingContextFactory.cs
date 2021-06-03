using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IO;

namespace LogReader.Models
{
    //This is Design Time DBContext factory required to create Migration.
    //https://docs.microsoft.com/en-us/ef/core/cli/dbcontext-creation?tabs=dotnet-core-cli
    class MessagingContextFactory : IDesignTimeDbContextFactory<MessagingContext>
    {
        public MessagingContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json")
                    .Build();

            var optionsBuilder = new DbContextOptionsBuilder<MessagingContext>();
            var connectionString = configuration.GetConnectionString("LogDatabase");
            optionsBuilder.UseSqlite(connectionString);

            var a = new LoggerFactory();

            return new MessagingContext(optionsBuilder.Options);
        }
    }
}
