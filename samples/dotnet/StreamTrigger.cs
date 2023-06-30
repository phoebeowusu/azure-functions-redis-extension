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
        private static readonly IDatabase redisDB = ConnectionMultiplexer.ConnectAsync("<cache-name>.redis.cache.windows.net:6380,password=<access-key>,ssl=True,abortConnect=False,tiebreaker=").Result.GetDatabase();

        // Parser helper function for reading results - source: https://developer.redis.com/develop/dotnet/streams/stream-basics/#spin-up-most-recent-element-task
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
            logger.LogInformation("ID: {val}", entry.Id.ToString());

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
            await redisDB.StreamAddAsync("cosmosRead", entry.Values, maxLength: 100);
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
            logger.LogInformation("ID: {val}", entry.Id.ToString());

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
            redisDB.StreamAdd("cosmosRead", entry.Values, maxLength: 100);
        }
        
    }
}

public class Data
{
    public string id { get; set; }
    public Dictionary<string, string> values { get; set; }
}
