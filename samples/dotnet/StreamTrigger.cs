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
        public static Dictionary<string, string> ParseResult(StreamEntry entry) => entry.Values.ToDictionary(x => x.Name.ToString(), x => x.Value.ToString());

        [FunctionName(nameof(WriteBehindAsync))]
        public static async Task WriteBehindAsync(
                [RedisStreamTrigger(localhostSetting, "streamTest")] StreamEntry entry,
                [CosmosDB(
                    databaseName: "database-id",
                    containerName: "container-id",
                    Connection = "COSMOS_CONNECTION")]
                    IAsyncCollector<CosmosItem> items,
                ILogger logger)
        {
            Dictionary<string, string> dict = ParseResult(entry);
            CosmosItem sampleItem = new CosmosItem { id = entry.Id, values = dict };
            await items.AddAsync(sampleItem);
        }

        [FunctionName(nameof(WriteThrough))]
        public static void WriteThrough(
             [RedisStreamTrigger(localhostSetting, "streamTest")] StreamEntry entry,
                [CosmosDB(
                    databaseName: "database-id",
                    containerName: "container-id",
                    Connection = "COSMOS_CONNECTION")]
                    ICollector<CosmosItem> items,
                ILogger logger)
        {
            Dictionary<string, string> dict = ParseResult(entry);
            CosmosItem sampleItem = new CosmosItem { id = entry.Id, values = dict };
            items.Add(sampleItem);

        }
    }
}
public class CosmosItem
{
    public string id { get; set; }
    public Dictionary<string, string> values { get; set; }
}