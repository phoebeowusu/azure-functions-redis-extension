﻿using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Redis.Samples
{
    public static class RedisToCosmos
    {
        public const string localhostSetting = "redisConnectionString";

        // Write through
        [FunctionName(nameof(WriteThrough))]
        public static void WriteThrough(
                [RedisStreamTrigger(localhostSetting, "streamTest2")] StreamEntry entry,
                /*
                 [CosmosDB(
                databaseName: "database-id",
                containerName: "container-id",
                Connection = "cosmosConnectionString")]
                ICollector<Data> items,
                 */
                ILogger logger)
        {
            // Insert data into CosmosDB synchronously
            // items.Add(FormatData(entry, logger));
        }

        private static Data FormatData(StreamEntry entry, ILogger logger)
        {
            logger.LogInformation("ID: {val}", entry.Id.ToString());
            
            // Map each key value pair
            Dictionary<string, string> dict = RedisUtilities.StreamEntryToDictionary(entry);

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
