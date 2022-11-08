using System;
using System.Threading.Tasks;
using System.Configuration;
using System.Collections.Generic;
using System.Net;
using Microsoft.Azure.Cosmos;
using System.Reflection.Metadata;

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

        //rand
        private Random rand = new Random(DateTime.Now.Second);

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
            this.cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);
            //create db and containers
            await this.CreateDatabaseAsync();
            await this.CreateContainerAsync();

            //run seed method to load initial data
            await this.QueryItemsAsync();
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

        private async Task<Order> ReadDocument(string orderid, string partition)
        {
            ItemResponse<Order> readResponse = await this.container.ReadItemAsync<Order>(orderid, new PartitionKey( partition));
            Console.WriteLine("Read item from database with id: {0} Operation consumed {1} RUs.\n", readResponse.Resource.id, readResponse.RequestCharge);
            return readResponse;
        }

        private async Task QueryItemsAsync()
        {

            var sqlQueryText = "SELECT * FROM c WHERE c.OrderNumber = 'NL-11'";

            Console.WriteLine("Running query: {0}\n", sqlQueryText);

            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
            FeedIterator<Order> queryResultSetIterator = this.container.GetItemQueryIterator<Order>(queryDefinition);

            List<Order> families = new List<Order>();

            while (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<Order> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                foreach (Order order in currentResultSet)
                {
                    families.Add(order);
                    Console.WriteLine("\tRead {0}", order.OrderNumber);
                }
                Console.WriteLine("Select orders. Operation consumed {0} RUs.\n", currentResultSet.RequestCharge);
            }
        }
    }
}
