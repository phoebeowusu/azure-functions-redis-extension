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
        private static readonly IDatabase redisDB = ConnectionMultiplexer.Connect(Environment.GetEnvironmentVariable("REDIS_CONNECTION")).GetDatabase();

        // Parser helper function for reading results - https://developer.redis.com/develop/dotnet/streams/stream-basics/#spin-up-most-recent-element-task
        public static Dictionary<string, string> ParseResult(StreamEntry entry) => entry.Values.ToDictionary(x => x.Name.ToString(), x => x.Value.ToString());

        // Write behind
        [FunctionName(nameof(WriteBehindAsync))]
        public static async Task WriteBehindAsync(
                [RedisStreamTrigger(localhostSetting, "stream1")] StreamEntry entry,
                [CosmosDB(
                databaseName: "database-id",
                containerName: "container-id",
                Connection = "COSMOS_CONNECTION")]
                IAsyncCollector<Data> items,
                ILogger logger)
        {
            // Insert data into CosmosDB asynchronously          
            await items.AddAsync(FormatData(entry, logger));
        }

        // Write through
        [FunctionName(nameof(WriteThrough))]
        public static void WriteThrough(
                [RedisStreamTrigger(localhostSetting, "stream1")] StreamEntry entry,
                [CosmosDB(
                databaseName: "database-id",
                containerName: "container-id",
                Connection = "COSMOS_CONNECTION")]
                ICollector<Data> items,
                ILogger logger)
        {
            // Insert data into CosmosDB synchronously
            items.Add(FormatData(entry, logger));
        }

        private static Data FormatData(StreamEntry entry, ILogger logger)
        {
            logger.LogInformation("ID: {val}", entry.Id.ToString());

            // Map each key value pair
            Dictionary<string, string> dict = ParseResult(entry);

            foreach (KeyValuePair<string, string> item in dict)
            {
                logger.LogInformation(item.Key + " => " + item.Value);
            }

            Data sampleItem = new Data { id = entry.Id, values = dict };
            return sampleItem;
        }
    }
}

public class Data
{
    public string id { get; set; }
    public Dictionary<string, string> values { get; set; }
}
