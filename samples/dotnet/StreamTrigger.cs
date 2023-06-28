using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using System.Linq;
using System.Collections.Generic;
using System.Configuration;
using StackExchange.Redis;
using Microsoft.WindowsAzure.Storage.Queue.Protocol;
using Microsoft.Azure.WebJobs;

namespace Microsoft.Azure.WebJobs.Extensions.Redis.Samples
{
    public static class StreamTrigger
    {

        public const string localhostSetting = "REDIS_CONNECTION";
        public static ConnectionMultiplexer _redisConnection = ConnectionMultiplexer.Connect("REDIS_CONNECTION");

        // Parser helper function for reading results
        public static Dictionary<string, string> ParseResult(StreamEntry entry) => entry.Values.ToDictionary(x => x.Name.ToString(), x => x.Value.ToString());

        // Write behind
        [FunctionName(nameof(WriteBehindAsync))]
        public static async Task WriteBehindAsync(
                [RedisStreamTrigger(localhostSetting, "cosmosWrite")] StreamEntry entry,
                [CosmosDB(
                    databaseName: "database-id",
                    containerName: "container-id",
                    Connection = "COSMOS_CONNECTION")]
                    IAsyncCollector<Data> items,
                ILogger logger)
        {
            // Read from Redis writing stream
            logger.LogInformation("ID: {entry.Id.ToString()}");

            // Map each key value pair
            Dictionary<string, string> dict = ParseResult(entry);
            foreach (KeyValuePair<string, string> item in dict)
            {
                logger.LogInformation(item.Key + "=>" + item.Value.ToString());
            }

            // Insert into CosmosDB asynchronously
            Data sampleItem = new Data { id = entry.Id, values = dict };
            await items.AddAsync(sampleItem);

            // Insert into Redis reading stream asynchronously
            var db = _redisConnection.GetDatabase();
            await db.StreamAddAsync("cosmosRead", entry.Values, maxLength: 100);
        }

        // Write through
        [FunctionName(nameof(WriteThrough))]
        public static void WriteThrough(
             [RedisStreamTrigger(localhostSetting, "cosmosWrite")] StreamEntry entry,
                [CosmosDB(
                    databaseName: "database-id",
                    containerName: "container-id",
                    Connection = "COSMOS_CONNECTION")]
                    ICollector<Data> items,
                ILogger logger)
        {
            // Read from Redis writing stream
            logger.LogInformation("ID: {entry.Id.ToString()}");

            // Map each key value pair
            Dictionary<string, string> dict = ParseResult(entry);
            foreach (KeyValuePair<string, string> item in dict)
            {
                logger.LogInformation(item.Key + "=>" + item.Value.ToString());
            }

            // Insert into CosmosDB synchronously
            Data sampleItem = new Data { id = entry.Id, values = dict };
            items.Add(sampleItem);

            // Insert into Redis reading stream synchronously
            var db = _redisConnection.GetDatabase();
            db.StreamAdd("cosmosRead", entry.Values, maxLength: 100);
        }


        // Read through - WIP
        [FunctionName(nameof(ReadThrough))]
        public static void ReadThrough(
                [RedisStreamTrigger(localhostSetting, "cosmosRead")] StreamEntry entry,
                [CosmosDB(
                    databaseName: "database-id",
                    containerName: "container-id",
                    Connection = "COSMOS_CONNECTION")]
                    IReadOnlyList<Data> items,
                ILogger logger)
        {
            // Search Redis reading stream for value
            var db = _redisConnection.GetDatabase();
            var messages = db.StreamRead("cosmosRead", "");

            // Cache miss
            if (messages == null)
            {

                // Not in CosmosDB 
                if (items.) return;

                // Insert information from CosmosDB to Redis 
                db.StreamAdd("cosmosRead", "", maxLength: 100);
            }
        }

    }
}

public class Data
{
    public string id { get; set; }
    public Dictionary<string, string> values { get; set; }
}
