using System.IO;
using ITExam.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ITExam.Models
{
    // Allows `dotnet ef` commands to create the DbContext at design-time
    // without bootstrapping the entire WebApplication pipeline (and without requiring JWT secrets).
    public class ITExamDbContextFactory : IDesignTimeDbContextFactory<ITExamDbContext>
    {
        public ITExamDbContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("SQL");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Missing connection string `ConnectionStrings:SQL`.");
            }

            var optionsBuilder = new DbContextOptionsBuilder<ITExamDbContext>();
            optionsBuilder.UseSqlServer(connectionString);
            return new ITExamDbContext(optionsBuilder.Options);
        }
    }
}

