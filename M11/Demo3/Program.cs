using System;
using System.Threading.Tasks;
using System.Configuration;
using System.Collections.Generic;
using System.Net;
using Microsoft.Azure.Cosmos;
using System.Reflection.Metadata;
using Azure.Identity;
using Microsoft.Azure.Cosmos.Fluent;
using Azure.Security.KeyVault.Keys.Cryptography;
using Microsoft.Azure.Cosmos.Encryption;

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
        private string databaseId = "EncryptDemo";
        private string containerId = "AlwaysEncrypt";

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
            var tokenCredential = new DefaultAzureCredential();
            var keyResolver = new KeyResolver(tokenCredential);

            this.cosmosClient = new CosmosClient(EndpointUri, PrimaryKey)
                .WithEncryption(keyResolver, KeyEncryptionKeyResolverName.AzureKeyVault);

            //create db and containers
            await this.CreateDatabaseAsync();
            await this.CreateContainerAsync();

            //run seed method to load initial data
            await this.AddItemsToContainerAsync();

            //query items from DB
            await this.QueryItemsAsync();
        }

        /// <summary>
        /// Create the container if it does not exist. 
        /// Specify "/OrderAddress/City" as the partition key since we're storing different types of documents.
        /// </summary>
        /// <returns></returns>
        private async Task CreateContainerAsync()
        {
 
                var path1 = new ClientEncryptionIncludedPath
                {
                    Path = "/SSN",
                    ClientEncryptionKeyId = "my-key",
                    EncryptionType = EncryptionType.Deterministic.ToString(),
                    EncryptionAlgorithm = DataEncryptionAlgorithm.AeadAes256CbcHmacSha256
                };
                var path2 = new ClientEncryptionIncludedPath
                {
                    Path = "/cardNumber",
                    ClientEncryptionKeyId = "my-key",
                    EncryptionType = EncryptionType.Randomized.ToString(),
                    EncryptionAlgorithm = DataEncryptionAlgorithm.AeadAes256CbcHmacSha256
                };


                await database.DefineContainer(containerId, "/OrderAddress/City")
                    .WithClientEncryptionPolicy()
                    .WithIncludedPath(path1)
                    .WithIncludedPath(path2)
                    .Attach()
                    .CreateAsync();

                // Create a new container
                container = (Container)await this.database.CreateContainerIfNotExistsAsync(containerId, "/OrderAddress/City");
            
            Console.WriteLine("Created Container: {0}\n", this.container.Id);
        }

        /// <summary>
        /// Create the database if it does not exist
        /// </summary>
        private async Task CreateDatabaseAsync()
        {
            // Create a new database
            this.database = (Database) await this.cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);


            await database.CreateClientEncryptionKeyAsync(
                "my-key",
                DataEncryptionAlgorithm.AeadAes256CbcHmacSha256,
                new EncryptionKeyWrapMetadata(
                    KeyEncryptionKeyResolverName.AzureKeyVault,
                    "cosmos",
                    ConfigurationManager.AppSettings["KeyPath"],
                    EncryptionAlgorithm.RsaOaep.ToString()));


            Console.WriteLine("Created Database: {0}\n", this.database.Id);
        }

        private async Task<Order> ReadDocument(string orderid, string partition)
        {
            ItemResponse<Order> readResponse = await this.container.ReadItemAsync<Order>(orderid, new PartitionKey( partition));
            Console.WriteLine("Read item from database with id: {0} Operation consumed {1} RUs.\n", readResponse.Resource.id, readResponse.RequestCharge);
            return readResponse;
        }

        private async Task CreateDocumentsIfNotExists(Order order) 
        {
            try
            {
                // Read the item to see if it exists.  
                ItemResponse<Order> readResponse = await this.container.ReadItemAsync<Order>(order.id, new PartitionKey(order.OrderAddress.City));
                Console.WriteLine("Item in database with id: {0} already exists\n", readResponse.Resource.id);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // Create an item in the container.
                ItemResponse<Order> createResponse = await this.container.CreateItemAsync<Order>(order, new PartitionKey(order.OrderAddress.City));

                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine("Created item in database with id: {0} Operation consumed {1} RUs.\n", createResponse.Resource.id, createResponse.RequestCharge);
            }
        }




        /// <summary>
        /// Add Family items to the container
        /// </summary>
        private async Task AddItemsToContainerAsync()
        {
            // Create a objects
            var customers = new List<Customer>() { new Customer() { IsActive = true, Name = "Level4you" }, new Customer() { IsActive = true, Name = "UpperLevel" }, new Customer() { IsActive = false, Name = "Channel-9" } };
            var products = new List<Product>() { new Product() { ProductName = "Food" }, new Product() { ProductName = "Book" }, new Product() { ProductName = "Coffee" } };

            List<string> states = new List<string> { "WA", "NY", "FL", "OH" };
            List<string> county = new List<string> { "King", "Brooklyn", "Miami-Dade", "Hamilton" };
            List<string> city = new List<string> { "Seattle", "NYC", "Miami", "Cincinnati" };


            var orderNum = 0;
            var index = 0;

            for (var id = 0; id < 5; id++)
            {
                index = rand.Next(states.Count - 1);
                orderNum = rand.Next(1000);

                Order order = new Order()
                {
                    SSN = "111-111" + rand.Next(1,9),
                    cardNumber = "000-000-000" + rand.Next(1,9),
                    id = orderNum.ToString(),
                    OrderNumber = $"NL-{orderNum}",
                    Description = "created order",
                    OrderCustomer = customers[rand.Next(customers.Count - 1)],
                    OrderAddress = new Address { State = states[index], County = county[index], City = city[index] },
                    OrderItems = new[] {
                    new OrderItem() { ProductItem  = products[rand.Next(products.Count - 1)], Count = rand.Next(10) },
                    new OrderItem() { ProductItem  = products[rand.Next(products.Count - 1)], Count = rand.Next(10) }
                    }
                };
                //create orders
                await CreateDocumentsIfNotExists(order);

            }

        }

        private async Task QueryItemsAsync()
        {

            var sqlQueryText = "SELECT * FROM c";

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
                    Console.WriteLine("\tRead {0} with SSN {1} and Card Number:{2}", order.OrderNumber, order.SSN, order.cardNumber);
                }
                Console.WriteLine("Select orders. Operation consumed {0} RUs.\n", currentResultSet.RequestCharge);
            }
        }
    }
}

