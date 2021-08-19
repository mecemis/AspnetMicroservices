using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using Polly;

namespace Discount.Infrastructure.Extensions
{
    public static class HostExtensions
    {
        public static IHost MigrateDatabase<TContext>(this IHost host)
        {
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var configuration = services.GetRequiredService<IConfiguration>();
                var logger = services.GetRequiredService<ILogger<TContext>>();

                try
                {
                    logger.LogInformation("Migrating postresql database.");

                    var retry = Policy.Handle<NpgsqlException>()
                        .WaitAndRetry(
                            retryCount: 5,
                            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // 2,4,8,16,32 sc
                            onRetry: (exception, retryCount, context) =>
                            {
                                logger.LogError($"Retry {retryCount} of {context.PolicyKey} at {context.OperationKey}, due to: {exception}.");
                            });

                    //if the postgresql server container is not created on run docker compose this
                    //migration can't fail for network related exception. The retry options for database operations
                    //apply to transient exceptions                    
                    retry.Execute(() => ExecuteMigrations(configuration));

                    logger.LogInformation("Migrated postresql database.");
                }
                catch (NpgsqlException ex)
                {
                    logger.LogError(ex, "An error occurred while migrating the postresql database");
                }
            }

            return host;
        }

        private static void ExecuteMigrations(IConfiguration configuration)
        {
            using var connection = new NpgsqlConnection(configuration.GetConnectionString("PostgreSql"));
            connection.Open();

            using var command = new NpgsqlCommand
            {
                Connection = connection
            };

            command.CommandText = "CREATE TABLE IF NOT EXISTS Coupon(ID SERIAL PRIMARY KEY NOT NULL, ProductName VARCHAR(24) NOT NULL, Description TEXT, Amount INT);";
            command.ExecuteNonQuery();

            command.CommandText = @"INSERT INTO coupon

                    (productname, description, amount)

                SELECT 'Iphone X' AS productname, 'Iphone X description' AS description, 150 AS Price

                WHERE 0 = (SELECT count(*) FROM coupon)

                UNION SELECT 'Samsung 10' AS productname, 'Samsung 10 description' AS description, 100 AS Price

                WHERE 0 = (SELECT count(*) FROM coupon);";
            command.ExecuteNonQuery();
            
        }
    }
}