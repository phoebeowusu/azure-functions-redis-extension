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

        [FunctionName(nameof(StreamTriggerAsync))]
        public static async Task StreamTriggerAsync(
                [RedisStreamTrigger(localhostSetting, "streamTest")] StreamEntry entry,
                [CosmosDB(
                    databaseName: "database-id",
                    containerName: "container-id",
                    Connection = "COSMOS_CONNECTION")]
                    IAsyncCollector<CosmosItem2> items,
                ILogger logger)
        {
            CosmosItem2 sample = new CosmosItem2 { id = entry.Id, Name = entry.Values[0].ToString() };
            await items.AddAsync(sample);
        }
    }
}

public class CosmosItem2
{
    public string id { get; set; }
    public string Name { get; set; }
}