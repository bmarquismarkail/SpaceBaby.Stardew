using System.Collections.Generic;
using StardewValley;

namespace VerticalToolbar.Framework
{
    internal class ModInventory
    {
        public List<Item> Items { get; private set; }
        public int MaxItems { get; private set; }

        public ModInventory(int maxItems)
        {
            MaxItems = maxItems;
            Items = new List<Item>(new Item[maxItems]);
        }

        public bool AddItem(Item item)
        {
            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i] == null)
                {
                    Items[i] = item;
                    return true;
                }
            }
            return false; // Inventory full
        }

        public void RemoveItem(int index)
        {
            if (index >= 0 && index < Items.Count)
                Items[index] = null;
        }

        public Item GetItem(int index)
        {
            return index >= 0 && index < Items.Count ? Items[index] : null;
        }
    }
}