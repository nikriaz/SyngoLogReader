using Microsoft.EntityFrameworkCore;

namespace LogReader.Models
{
    public class MessagingContext : DbContext
    {
        public MessagingContext(DbContextOptions<MessagingContext> options) : base(options)
        {

        }

        public DbSet<Message> Messages { get; set; }
        public DbSet<FTSMessage> FTSMessages { get; set; }

        //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        //{

        //    optionsBuilder.UseLoggerFactory(_loggerFactory);

        //    //optionsBuilder.UseSqlite(@"Data Source=C:\Temp\logging.db");
        //    //optionsBuilder.UseSqlite(ConfigurationManager.ConnectionStrings["LogDatabase"].ConnectionString);
        //    //optionsBuilder.UseSqlite(Configuration.GetConnectionString("LogDatabase"));
        //}

        protected override void OnModelCreating(ModelBuilder modelBuilder) //1:1 relationship for SQLite FTS Virtual Table
        {
            modelBuilder.Entity<FTSMessage>(
                x =>
                {
                    x.HasKey(fts => fts.RowId);
                    x.Property(fts => fts.Match).HasColumnName("FTSMessages");

                });

            modelBuilder.Entity<Message>(
                x =>
                {
                    x.HasOne(fts => fts.FTSMessage)
                     .WithOne(p => p.Message)
                     .HasForeignKey<Message>(p => p.RowId);

                });
        }
    }
}
