using Integration.Common;
using Integration.Backend;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Integration.Service
{
    /// <summary>
    /// Service responsible for handling item integration in a distributed system.
    /// </summary>
    public sealed class DistributedItemIntegrationService
    {
        // A mechanism for communication between servers is required for data synchronization.
        // In this example, a ConcurrentDictionary is used as a simple communication channel.
        // In a real distributed system, a message queue or distributed database would be used.
        private readonly ConcurrentDictionary<string, int> contentIdMap = new ConcurrentDictionary<string, int>();

        // A database or storage mechanism accessible by all servers is required.
        private readonly DistributedDatabase distributedDatabase;

        public DistributedItemIntegrationService(DistributedDatabase database)
        {
            this.distributedDatabase = database;
        }

        /// <summary>
        /// Saves an item with the given content, ensuring uniqueness across servers.
        /// </summary>
        /// <param name="itemContent">The content of the item to be saved.</param>
        /// <returns>A Result indicating the success or failure of the operation.</returns>
        public async Task<Result> SaveItem(string itemContent)
        {
            // If a server has already saved the content, return immediately.
            if (contentIdMap.ContainsKey(itemContent))
            {
                return new Result(false, $"Duplicate item received with content {itemContent}.");
            }

            // Broadcast the content to all servers for synchronization.
            await SendContentToAllServers(itemContent);

            // Save the content in the local server's database.
            var item = distributedDatabase.SaveItem(itemContent);

            // Update the content-ID mapping.
            contentIdMap.TryAdd(itemContent, item.Id);

            return new Result(true, $"Item with content {itemContent} saved with id {item.Id}");
        }

        /// <summary>
        /// Retrieves all items from all servers and aggregates them.
        /// </summary>
        /// <returns>A list of all items across all servers.</returns>
        public async Task<List<Item>> GetAllItems()
        {
            // Retrieve content from all servers and aggregate them.
            var allItems = new List<Item>();

            foreach (var server in distributedDatabase.GetAllServers())
            {
                var items = await server.GetAllItems();
                allItems.AddRange(items);
            }

            return allItems;
        }

        /// <summary>
        /// Sends the content to all servers for synchronization.
        /// </summary>
        /// <param name="itemContent">The content to be synchronized.</param>
        /// <returns>An asynchronous task.</returns>
        private async Task SendContentToAllServers(string itemContent)
        {
            // Broadcast the content to all servers.
            var tasks = new List<Task>();

            foreach (var server in distributedDatabase.GetAllServers())
            {
                tasks.Add(server.SaveItem(itemContent));
            }

            await Task.WhenAll(tasks);
        }
    }
}