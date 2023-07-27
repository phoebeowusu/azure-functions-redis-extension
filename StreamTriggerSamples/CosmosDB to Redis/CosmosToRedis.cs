using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;

namespace Microsoft.Azure.WebJobs.Extensions.Redis.Samples
{
    public class CosmosToRedis
    {
        // Redis database and stream stored in local.settings.json
        private static readonly IDatabase redisDB = ConnectionMultiplexer.Connect(Environment.GetEnvironmentVariable("redisConnectionString")).GetDatabase();
        public const string stream = nameof(CosmosToRedis);

        // CosmosDB connection string, database name and container name stored in local.settings.json
        public const string cosmosConnectionString = "cosmosConnectionString";
        public const string cosmosDatabase = "%database-id%";
        public const string cosmosContainer = "%container-id%";

        /// <summary>
        /// Write Around: Write from Cosmos DB to Redis whenever a change occurs in one of the CosmosDB documents
        /// </summary>
        /// <param name="input"> List of changed documents in CosmosDB </param>
        /// <param name="logger"> ILogger used to write key information </param>
        [FunctionName("CosmosToRedis")]
        public static void Run(
            [CosmosDBTrigger(
                databaseName: cosmosDatabase,
                containerName: cosmosContainer,
                Connection = cosmosConnectionString,
                LeaseContainerName = "leases",
                CreateLeaseContainerIfNotExists = true)]IReadOnlyList<Data> input, ILogger logger)
        {
            foreach (var document in input)
            {
                var values = new NameValueEntry[document.values.Count];
                int i = 0;
                foreach (KeyValuePair<string, string> entry in document.values)
                {
                    values[i++] = new NameValueEntry(entry.Key, entry.Value);
                }

                redisDB.StreamAdd(stream, values);
            }
        }
    }
}
