using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
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


        [FunctionName(nameof(StreamTriggerAsync))]
        public static async Task StreamTriggerAsync(
                [RedisStreamTrigger(localhostSetting, "streamTest")] string entry,
                ILogger logger)
        {

            // Parsing message
            char[] delimiterChars = { '"', ',', '{', ':', '}' };
            string[] words = entry.Split(delimiterChars);
            words = words.Where(x => !string.IsNullOrEmpty(x)).ToArray();
            string[] vals = new string[words.Length - 3];
            Array.Copy(words, 3, vals, 0, vals.Length);

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
                id: words[1],
                values: vals
            );

            // Upload item into CosmosDB container
            CosmosItem upsertedItem = await container.UpsertItemAsync<CosmosItem>(item);
        }
    }
}

// C# record type for items in the container
public record CosmosItem(
    string id,
    string[] values
);
