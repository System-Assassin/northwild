using System;
using System.Collections.Generic;
using UnityEngine;

namespace Northwild
{
    public enum ItemId
    {
        BirchBark,
        DryGrass,
        Twig,
        Stick,
        Log,
        Stone,
        RawWater,
        SafeWater,
        Cloudberry
    }

    public static class ItemCatalog
    {
        public static string DisplayName(ItemId item)
        {
            switch (item)
            {
                case ItemId.BirchBark: return "Birch bark";
                case ItemId.DryGrass: return "Dry grass";
                case ItemId.Twig: return "Dry twig";
                case ItemId.Stick: return "Stick";
                case ItemId.Log: return "Split log";
                case ItemId.Stone: return "Stone";
                case ItemId.RawWater: return "Untreated water";
                case ItemId.SafeWater: return "Boiled water";
                case ItemId.Cloudberry: return "Cloudberry";
                default: return item.ToString();
            }
        }

        public static float WeightKg(ItemId item)
        {
            switch (item)
            {
                case ItemId.BirchBark: return 0.04f;
                case ItemId.DryGrass: return 0.05f;
                case ItemId.Twig: return 0.08f;
                case ItemId.Stick: return 0.45f;
                case ItemId.Log: return 2.2f;
                case ItemId.Stone: return 1.1f;
                case ItemId.RawWater:
                case ItemId.SafeWater: return 1f;
                case ItemId.Cloudberry: return 0.02f;
                default: return 0f;
            }
        }
    }

    [Serializable]
    public struct InventoryEntry
    {
        public ItemId item;
        public int amount;

        public InventoryEntry(ItemId item, int amount)
        {
            this.item = item;
            this.amount = amount;
        }
    }

    public sealed class PlayerInventory : MonoBehaviour
    {
        [SerializeField] private float maximumWeightKg = 22f;
        private readonly Dictionary<ItemId, int> items = new Dictionary<ItemId, int>();

        public float MaximumWeightKg { get { return maximumWeightKg; } }

        public float CurrentWeightKg
        {
            get
            {
                float total = 0f;
                foreach (KeyValuePair<ItemId, int> pair in items)
                    total += ItemCatalog.WeightKg(pair.Key) * pair.Value;
                return total;
            }
        }

        public int Count(ItemId item)
        {
            int count;
            return items.TryGetValue(item, out count) ? count : 0;
        }

        public bool CanCarry(ItemId item, int amount)
        {
            return amount > 0 && CurrentWeightKg + ItemCatalog.WeightKg(item) * amount <= maximumWeightKg;
        }

        public bool Add(ItemId item, int amount, bool showMessage = true)
        {
            if (!CanCarry(item, amount))
            {
                if (showMessage && NorthwildGame.Instance != null)
                    NorthwildGame.Instance.Notify("Too heavy. Drop something before carrying more.");
                return false;
            }

            items[item] = Count(item) + amount;
            if (showMessage && NorthwildGame.Instance != null)
                NorthwildGame.Instance.Notify("Collected " + amount + " × " + ItemCatalog.DisplayName(item) + ".");
            return true;
        }

        public bool Remove(ItemId item, int amount)
        {
            if (amount <= 0 || Count(item) < amount)
                return false;

            int remaining = Count(item) - amount;
            if (remaining == 0)
                items.Remove(item);
            else
                items[item] = remaining;
            return true;
        }

        public bool Has(ItemId item, int amount)
        {
            return Count(item) >= amount;
        }

        public List<InventoryEntry> Capture()
        {
            List<InventoryEntry> result = new List<InventoryEntry>();
            foreach (KeyValuePair<ItemId, int> pair in items)
                result.Add(new InventoryEntry(pair.Key, pair.Value));
            result.Sort((a, b) => a.item.CompareTo(b.item));
            return result;
        }

        public void Restore(List<InventoryEntry> entries)
        {
            items.Clear();
            if (entries == null)
                return;

            foreach (InventoryEntry entry in entries)
            {
                if (entry.amount > 0)
                    items[entry.item] = entry.amount;
            }
        }
    }
}

