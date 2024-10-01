using System;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace CosmosDBFeed
{
    public static class ChangeFeedProcessor
    {
        [FunctionName("ChangeFeedProcessor")]
        public static void Run([CosmosDBTrigger(
            databaseName: "DP420Demo",
            containerName: "TheCloudShops",
            Connection = "alex-cosmos",
            LeaseContainerName = "leases",
            CreateLeaseContainerIfNotExists = true)]IReadOnlyList<Oreder> input,
            ILogger log)
        {
            if (input != null && input.Count > 0)
            {
                log.LogInformation("Documents modified " + input.Count);
                log.LogInformation("First document Id " + input[0].id);
            }
        }
    }

    // Customize the model with your own desired properties
    public class Oreder
    {
        public string id { get; set; }
    }
}
