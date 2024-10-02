using System;
using System.Threading.Tasks;
using System.Configuration;
using System.Collections.Generic;
using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using System.Threading;

namespace TheCloudShopsLoader
{
    public class Program
    {
        // The Azure Cosmos DB endpoint for running this sample.
        private static readonly string EndpointUri = ConfigurationManager.AppSettings["EndpointUri"];
        // The primary key for the Azure Cosmos account.
        private static readonly string PrimaryKey = ConfigurationManager.AppSettings["PrimaryKey"];
        
        // The Cosmos client instance
        private CosmosClient cosmosClient;

        // The database we will create
        private Database database;

        // The container we will create.
        private Container container;

        // The name of the database and container we will create
        private string databaseId = "DP420Demo";
        private string containerId = "TheCloudShops";

        public static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("Beginning operations...\n");
                Program p = new Program();
                await p.GetStartedDemoAsync();

            }
            catch (CosmosException de)
            {
                Exception baseException = de.GetBaseException();
                Console.WriteLine("{0} error occurred: {1}", de.StatusCode, de);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: {0}", e);
            }
            finally
            {
                Console.WriteLine("End of demo, press any key to exit.");
                Console.ReadKey();
            }
        }

        public class LogHandler : RequestHandler
        {
            public override async Task<ResponseMessage> SendAsync(RequestMessage request, CancellationToken cancellationToken)
            {
                Console.WriteLine($"[{request.Method.Method}]\t{request.RequestUri}");
                ResponseMessage response = await base.SendAsync(request, cancellationToken);
                Console.WriteLine($"[{Convert.ToInt32(response.StatusCode)}]\t{response.StatusCode}");
                return response;
            }
        }

        public async Task GetStartedDemoAsync()
        {
            // Create a new instance of the Cosmos Client by builder
            using (cosmosClient = new CosmosClientBuilder(EndpointUri, PrimaryKey)
                  .AddCustomHandlers(new LogHandler())
                  .WithApplicationPreferredRegions(new List<string> { Regions.EastUS2, Regions.WestUS2 })
                  .WithApplicationName("CosmosDBDotnetQuickstart")
                  .Build()
                )
            {
                await CreateDatabaseAsync();
                await CreateContainerAsync();
            }
        }

        /// <summary>
        /// Create the container if it does not exist. 
        /// Specify "/OrderAddress/City" as the partition key since we're storing different types of documents.
        /// </summary>
        /// <returns></returns>
        private async Task CreateContainerAsync()
        {
            // Create a new container
            this.container = (Container)await this.database.CreateContainerIfNotExistsAsync(containerId, "/OrderAddress/City");
            Console.WriteLine("Created Container: {0}\n", this.container.Id);
        }

        /// <summary>
        /// Create the database if it does not exist
        /// </summary>
        private async Task CreateDatabaseAsync()
        {
            // Create a new database
            this.database = (Database) await this.cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);
            Console.WriteLine("Created Database: {0}\n", this.database.Id);
        }


     
    }
}
