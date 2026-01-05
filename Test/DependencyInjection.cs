using Oakton.Resources;
using System.Diagnostics;
using System.Text.Json.Serialization;
using Wolverine;
using Wolverine.ErrorHandling;
using Wolverine.Kafka;
using Wolverine.Postgresql;

namespace Test
{
    public static class DependencyInjection
    {
        public static IHostBuilder AddOutboxMessaging(this IHostBuilder hostBuilder)
        {
            hostBuilder.ConfigureAppConfiguration((context, config) =>
            {
                hostBuilder.UseWolverine(options =>
                {
                    options.UseKafka("localhost:19092")
                        .ConfigureClient(config =>
                        {
                            config.AllowAutoCreateTopics = true;
                            config.BootstrapServers = "localhost:19092";
                        })
                        .ConfigureConsumers(config =>
                        {
                            config.AllowAutoCreateTopics = true;
                            config.BootstrapServers = "localhost:19092";
                        });

                    options.UseSystemTextJsonForSerialization(x =>
                    {
                        x.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                        x.UnknownTypeHandling = JsonUnknownTypeHandling.JsonNode;
                    });

                    options.ListenToKafkaTopic("product-listing")
                           .UseInterop(new OutboxEventFilters())
                           .MaximumParallelMessages(1)
                           .ListenerCount(1);
                    
                    options.PersistMessagesWithPostgresql("Host=localhost;Port=5432;Database=wv_db;Username=postgres;Password=postgres");

                    options.Policies.LogMessageStarting(LogLevel.Information);
                    options.Policies.UseDurableInboxOnAllListeners();
                    options.Policies.UseDurableOutboxOnAllSendingEndpoints();
                });

            });

            hostBuilder.UseResourceSetupOnStartup();

            return hostBuilder;
        }

    }
}
