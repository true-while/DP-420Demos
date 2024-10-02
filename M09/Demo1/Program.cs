using System;
using System.Threading.Tasks;
using System.Configuration;
using System.Collections.Generic;
using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using TheCloudShopsLoader;

namespace CosmosGettingStartedTutorial
{
    class Program
    {
        // The Azure Cosmos DB endpoint for running this sample.
        private static readonly string EndpointUri = ConfigurationManager.AppSettings["EndPointUri"];

        // The primary key for the Azure Cosmos account.
        private static readonly string PrimaryKey = ConfigurationManager.AppSettings["PrimaryKey"];

        // The connection string for the Azure Cosmos account.
        private static readonly string connectionString = ConfigurationManager.AppSettings["ConnectionString"];


        // The Cosmos client instance
        private CosmosClient cosmosClient;

        // The database we will create
        private Database database;

        // The container we will create.
        private Container container;

        // The name of the database and container we will create
        private string databaseId = "DP420Demo";
        private string containerId = "TheCloudShops";

        // <Main>
        public static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("Beginning operations...\n");
                Program p = new Program();

                //connect by use client
                Console.WriteLine("By use cosmos client...");
                await p.CosmosClientAsync();


                //connect by use builder
                Console.WriteLine("By use client builder...");
                await p.CosmosClientBuilderAsync();


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
        // </Main>

        // <GetStartedDemoAsync>
        /// <summary>
        /// Entry point to call methods that operate on Azure Cosmos DB resources in this sample
        /// </summary>
        public async Task CosmosClientAsync()
        {
            Console.WriteLine("Connecting to app pref regions 'centralus'");

            // Create a new instance of the Cosmos Client
            using (cosmosClient = new CosmosClient(EndpointUri, PrimaryKey, new CosmosClientOptions()
            {
                ConsistencyLevel = ConsistencyLevel.Eventual,
                ConnectionMode = ConnectionMode.Direct,
                ApplicationPreferredRegions = new List<string> { Regions.CentralUS },
                ApplicationName = "CosmosDBDotnetQuickstart"
            }))
            {
                

                await CreateDatabaseAsync();
                await CreateContainerAsync();
                await QueryItemsAsync();
            }


        }

        public async Task CosmosClientBuilderAsync()
        {

            Console.WriteLine("Connecting to region:" + Regions.WestUS);

            // Create a new instance of the Cosmos Client by builder
            using (cosmosClient = new CosmosClientBuilder(connectionString)
                  .WithApplicationRegion(Regions.WestUS)
                  .WithApplicationName("CosmosDBDotnetQuickstart")
                  .Build()
                )
            {
                
                await CreateDatabaseAsync();
                await CreateContainerAsync();
                await QueryItemsAsync();
            }
        }

        // </GetStartedDemoAsync>

        // <CreateDatabaseAsync>
        /// <summary>
        /// Create the database if it does not exist
        /// </summary>
        private async Task CreateDatabaseAsync()
        {
            // Create a new database
            this.database = await this.cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);
            Console.WriteLine("Created Database: {0}\n", this.database.Id);
        }
        // </CreateDatabaseAsync>

        // <CreateContainerAsync>
        /// <summary>
        /// Create the container if it does not exist. 
        /// Specifiy "/partitionKey" as the partition key path since we're storing family information, to ensure good distribution of requests and storage.
        /// </summary>
        /// <returns></returns>
        private async Task CreateContainerAsync()
        {
            // Create a new container
            this.container = await this.database.CreateContainerIfNotExistsAsync(containerId, "/OrderAddress/City");
            Console.WriteLine("Created Container: {0}\n", this.container.Id);
        }
        // </CreateContainerAsync>



        // <QueryItemsAsync>
        /// <summary>
        /// Run a query (using Azure Cosmos DB SQL syntax) against the container
        /// Including the partition key value of lastName in the WHERE filter results in a more efficient query
        /// </summary>
        private async Task QueryItemsAsync()
        {
            var sqlQueryText = "SELECT TOP 3 * FROM c";

            Console.WriteLine("Running query: {0}\n", sqlQueryText);

            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
            FeedIterator<Order> queryResultSetIterator = this.container.GetItemQueryIterator<Order>(queryDefinition);

            List<Order> orders = new List<Order>();

            while (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<Order> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                foreach (Order order in currentResultSet)
                {
                    orders.Add(order);
                    Console.WriteLine("\tRead {0}", order.id);
                }
            }
        }
    }
}
