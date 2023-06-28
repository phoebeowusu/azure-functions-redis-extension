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
        private static readonly IDatabaseAsync redisDB = ConnectionMultiplexer.ConnectAsync("pow-RedTrig.redis.cache.windows.net:6380,password=qSBG6V4sueLHLZMJoqBucrlvCERUgAgE4AzCaA9fBUI=,ssl=True,abortConnect=False,tiebreaker=").Result.GetDatabase();

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


            // Cache miss

            // Failure: Not in CosmosDB
            // Insert into Redis reading stream asynchronously
            //await redisDB.StreamAddAsync("cosmosRead", entry.Values, maxLength: 100);
        }
    }
}

public class Data
{
    public string id { get; set; }
    public Dictionary<string, string> values { get; set; }
}
