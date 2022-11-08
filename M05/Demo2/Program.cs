using System;
using System.Threading.Tasks;
using System.Configuration;
using System.Collections.Generic;
using System.Net;
using Microsoft.Azure.Cosmos;
using System.Reflection.Metadata;
using System.Collections;

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
            await this.QueryItemsWithParametersAsync("NYC", "Miami");
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
            this.database = (Database)await this.cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);
            Console.WriteLine("Created Database: {0}\n", this.database.Id);
        }

        private async Task<Order> ReadDocument(string orderid, string partition)
        {
            ItemResponse<Order> readResponse = await this.container.ReadItemAsync<Order>(orderid, new PartitionKey(partition));
            Console.WriteLine("Read item from database with id: {0} Operation consumed {1} RUs.\n", readResponse.Resource.id, readResponse.RequestCharge);
            return readResponse;
        }

        private async Task QueryItemsWithParametersAsync(string city1, string city2)
        {

            var sqlQueryText = "SELECT * FROM o WHERE o.OrderAddress.City IN(@city1, @city2)";

            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText)
                .WithParameter("@city1", city1)
                .WithParameter("@city2", city2);

            Console.WriteLine("Running query: {0}\n", queryDefinition.QueryText);

            string continuation = null;

            List<Order> results = new List<Order>();
            using (FeedIterator<Order> resultSetIterator = container.GetItemQueryIterator<Order>(
                queryDefinition,
                requestOptions: new QueryRequestOptions()
                {
                    MaxItemCount = 3
                }))
            {
                // Execute query and get 1 item in the results. Then, get a continuation token to resume later
                while (resultSetIterator.HasMoreResults)
                {
                    FeedResponse<Order> response = await resultSetIterator.ReadNextAsync();

                    results.AddRange(response);

                    // Get continuation token once we've gotten > 0 results. 
                    if (response.Count > 0)
                    {
                        continuation = response.ContinuationToken;
                        break;
                    }
                }
            }
            Console.WriteLine("First resultset");
            foreach (var order in results)
            {
                Console.WriteLine("\tRead {0} from city {1}", order.OrderNumber, order.OrderAddress.City);
            }

            //reset list
            results = new List<Order>();

            // Check if query has already been fully drained
            if (continuation == null)
            {
                return;
            }

            // Resume query using continuation token
            using (FeedIterator<Order> resultSetIterator = container.GetItemQueryIterator<Order>(
                    queryDefinition,
                    requestOptions: new QueryRequestOptions()
                    {
                        MaxItemCount = -1
                    },
                    continuationToken: continuation))
            {
                while (resultSetIterator.HasMoreResults)
                {
                    FeedResponse<Order> response = await resultSetIterator.ReadNextAsync();
                    results.AddRange(response);
                }

                Console.WriteLine("Rest...");
                foreach (var order in results)
                {
                    Console.WriteLine("\tRead {0} from city {1}", order.OrderNumber, order.OrderAddress.City);
                }

            }
        }
    }
}
