using Integration.Common;
using Integration.Backend;
using System.Collections.Concurrent;

namespace Integration.Service
{
    public sealed class ItemIntegrationService
    {
        // This dictionary will store item contents as keys and their corresponding IDs as values.
        // It provides fast lookups to check for duplicate content.
        private readonly ConcurrentDictionary<string, int> contentIdMap = new ConcurrentDictionary<string, int>();

        // This is a dependency that is normally fulfilled externally.
        private ItemOperationBackend ItemIntegrationBackend { get; set; } = new ItemOperationBackend();

        // This method is called externally and can be called multithreaded, in parallel.
        // More than one item with the same content should not be saved. However,
        // calling this with different contents at the same time is OK, and should
        // be allowed for performance reasons.
        public Result SaveItem(string itemContent)
        {
            // Check if the content already exists in the dictionary.
            if (contentIdMap.ContainsKey(itemContent))
            {
                return new Result(false, $"Duplicate item received with content {itemContent}.");
            }

            // If not, lock the dictionary to ensure thread safety while adding the content.
            lock (contentIdMap)
            {
                // Double check to ensure uniqueness after acquiring the lock.
                if (contentIdMap.ContainsKey(itemContent))
                {
                    return new Result(false, $"Duplicate item received with content {itemContent}.");
                }

                // Save the item content to the backend.
                var item = ItemIntegrationBackend.SaveItem(itemContent);

                // Update the dictionary with the new content and its corresponding ID.
                contentIdMap.TryAdd(itemContent, item.Id);

                return new Result(true, $"Item with content {itemContent} saved with id {item.Id}");
            }
        }

        public List<Item> GetAllItems()
        {
            return ItemIntegrationBackend.GetAllItems();
        }
    }
}
