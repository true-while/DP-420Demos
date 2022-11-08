using System;
using System.Threading.Tasks;
using System.Configuration;
using System.Collections.Generic;
using System.Net;
using Microsoft.Azure.Cosmos;
using System.Reflection.Metadata;
using System.Diagnostics;

namespace TheCloudShopsLoader
{
    public class Program
    {
        // The Azure Cosmos DB endpoint for running this sample.
        private static readonly string EndpointUri = ConfigurationManager.AppSettings["EndpointUri"];
        // The primary key for the Azure Cosmos account.
        private static readonly string PrimaryKey = ConfigurationManager.AppSettings["PrimaryKey"];

        //The connection string for Gateway
        private static readonly string GateWayString = ConfigurationManager.AppSettings["GWString"];
        
        // The Cosmos client instance
        private CosmosClient directCosmosClient;

        // The Cosmos client instance
        private CosmosClient cacheCosmosClient;

        // The database we will create
        private Database directDatabase;
        private Database cacheDatabase;

        // The container we will create.
        private Container directContainer;
        // The container we will create.
        private Container cacheContainer;
        private static readonly string containerid = "IndexDemo2";

        // The name of the database and container we will create
        private string databaseId = "DP420Demo";

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

        public async Task GetStartedDemoAsync()
        {
            // Create a new instance of the Cosmos Client
            this.directCosmosClient = new CosmosClient(EndpointUri, PrimaryKey);


            // Create a new instance of the Cosmos Client
            this.cacheCosmosClient = new CosmosClient(GateWayString);

            //create db and containers
            await this.CreateDatabaseAsync();
            await this.CreateContainerAsync();

            Console.WriteLine("\r\n-------------direct performance---------------------------");
            
            await this.QueryItemsAsync(directContainer, 
                new QueryRequestOptions() {ConsistencyLevel = ConsistencyLevel.Eventual});


            Console.WriteLine("\r\n-------------cached performance---------------------------");

           
            await this.QueryItemsAsync(cacheContainer,  new QueryRequestOptions()
            {
                ConsistencyLevel = ConsistencyLevel.Eventual,
                DedicatedGatewayRequestOptions = new DedicatedGatewayRequestOptions
                {
                    MaxIntegratedCacheStaleness = TimeSpan.FromSeconds(120)
                }
            });

        }

        /// <summary>
        /// Create the container if it does not exist. 
        /// Specify "/OrderAddress/City" as the partition key since we're storing different types of documents.
        /// </summary>
        /// <returns></returns>
        private async Task CreateContainerAsync()
        {
            // Create a new container
            this.directContainer = (Container)await this.directDatabase.CreateContainerIfNotExistsAsync(containerid, "/id");
            Console.WriteLine("Created Direct Container: {0}\n", directContainer.Id);

            // Create a new container
            this.cacheContainer = (Container)await this.cacheDatabase.CreateContainerIfNotExistsAsync(containerid, "/id");
            Console.WriteLine("Created Cached Container: {0}\n", cacheContainer.Id);
        }

        /// <summary>
        /// Create the database if it does not exist
        /// </summary>
        private async Task CreateDatabaseAsync()
        {
            // Create a new database
            this.directDatabase = (Database)await this.directCosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);
            Console.WriteLine("Created Direct Database: {0}\n", directDatabase.Id);

            // Create a new database
            this.cacheDatabase = (Database)await this.cacheCosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);
            Console.WriteLine("Created Cached Database: {0}\n", cacheDatabase.Id);
        }


        private async Task QueryItemsAsync(Container container, QueryRequestOptions ops)
        {
            Stopwatch sw = new Stopwatch(); 
            sw.Start();

            var sqlQueryText = "SELECT * FROM o";

            Console.WriteLine("Running query: {0}\n", sqlQueryText);

            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
            FeedIterator<Order> queryResultSetIterator = container.GetItemQueryIterator<Order>(queryDefinition, null, ops);

            List<Order> families = new List<Order>();

            while (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<Order> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                foreach (Order order in currentResultSet)
                {
                    families.Add(order);
                }
                Console.WriteLine("Select orders. Operation consumed {0} RUs. Time {1}ms \n",
                    currentResultSet.RequestCharge, sw.ElapsedMilliseconds);
            }

        }
    }

}
