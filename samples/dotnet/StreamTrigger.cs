using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Microsoft.Azure.WebJobs.Extensions.Redis.Samples
{
    internal class StreamTrigger
    {
        // New instance of CosmosClient class using a connection string
        private static CosmosClient client = new(
            connectionString: Environment.GetEnvironmentVariable("COSMOS_CONNECTION_STRING")!
        );
        public const string localhostSetting = "REDIS_CONNECTION";

        // Inline function to handle the parsing
        public static Dictionary<string, string> ParseResult(StreamEntry entry) => entry.Values.ToDictionary(x => x.Name.ToString(), x => x.Value.ToString());

        [FunctionName(nameof(StreamTriggerAsync))]
        public static async Task StreamTriggerAsync(
                [RedisStreamTrigger(localhostSetting, "streamTest")] StreamEntry entry,
                ILogger logger)
        {
            var dict = ParseResult(entry);
            logger.LogInformation($"Read result: Id {entry.Id.ToString()} name {dict["name"]}");


            // Retrivew CosmosDB database and container  
            Cosmos.Database database = await client.CreateDatabaseIfNotExistsAsync(
                id: "database-id"
            );

            Cosmos.Container container = await database.CreateContainerIfNotExistsAsync(
                id: "container-id",
                partitionKeyPath: "/category",
                throughput: 400
            );

            // Create new item using message information
            CosmosItem item = new(
                id: entry.Id.ToString(),
                name: dict["name"]
            );

            //Unpload item into CosmosDB container
            CosmosItem upsertedItem = await container.UpsertItemAsync<CosmosItem>(item);

        }
    }
}

// C# record type for items in the container
public record CosmosItem(
    string id,
    string name
);
