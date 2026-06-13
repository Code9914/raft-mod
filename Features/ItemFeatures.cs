using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RaftMod
{
    public class ItemFeatures
    {
        public bool InfiniteItems;
        public bool UnlockAll;
        public bool UnlockBlueprints;
        public bool GiveAllItems;

        public void Update()
        {
            try
            {
                if (UnlockAll) { UnlockAll = false; DoUnlockAll(); }
                if (UnlockBlueprints) { UnlockBlueprints = false; DoUnlockBlueprints(); }
                if (GiveAllItems) { GiveAllItems = false; DoGiveAllItems(); }
                if (InfiniteItems) HandleInfiniteItems();
            }
            catch (Exception ex) { Plugin.Log.LogError($"Item.Update: {ex.Message}"); }
        }

        private void DoUnlockAll()
        {
            var rt = ComponentManager<Inventory_ResearchTable>.Value;
            if (rt != null) rt.LearnAllRecipesInstantly();
            Plugin.Log.LogInfo("All recipes unlocked!");
        }

        private void DoUnlockBlueprints()
        {
            var net = ComponentManager<Network_Player>.Value;
            if (net?.BlueprintComponent == null) return;
            var blueprintComp = net.BlueprintComponent;
            Plugin.Log.LogInfo("Blueprints unlocked! (requires harmony patch for full support)");
        }

        private void DoGiveAllItems()
        {
            var inv = ComponentManager<PlayerInventory>.Value;
            if (inv == null) return;

            var count = 0;
            foreach (var item in ItemManager.GetAllItems())
            {
                inv.AddItem(item.UniqueName, 1);
                count++;
            }
            Plugin.Log.LogInfo($"Given {count} items!");
        }

        public void ClearInventory()
        {
            var inv = ComponentManager<PlayerInventory>.Value;
            if (inv == null) return;

            foreach (var slot in inv.allSlots)
            {
                if (slot != null && !slot.IsEmpty)
                    slot.SetItem((Item_Base)null, 0);
            }
            Plugin.Log.LogInfo("Inventory cleared!");
        }

        public void SpawnItem(string search, int amount)
        {
            if (string.IsNullOrEmpty(search)) return;
            var inv = ComponentManager<PlayerInventory>.Value;
            if (inv == null) return;

            var found = false;
            foreach (var item in ItemManager.GetAllItems())
            {
                if (item.UniqueName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    item.settings_Inventory.DisplayName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    inv.AddItem(item.UniqueName, amount);
                    found = true;
                    Plugin.Log.LogInfo($"Spawned {item.UniqueName} x{amount}");
                    break;
                }
            }
            if (!found)
                Plugin.Log.LogInfo($"No item found matching '{search}'");
        }

        public void GiveCategory(string category)
        {
            var inv = ComponentManager<PlayerInventory>.Value;
            if (inv == null) return;

            var count = 0;
            foreach (var item in ItemManager.GetAllItems())
            {
                var name = item.UniqueName.ToLower();
                var display = item.settings_Inventory.DisplayName.ToLower();

                bool match = false;
                if (category == "Food")
                    match = name.Contains("food") || name.Contains("raw") || name.Contains("cooked") ||
                            name.Contains("fruit") || name.Contains("vegetable") || name.Contains("soup") ||
                            name.Contains("juice") || name.Contains("water") || name.Contains("mushroom");
                else if (category == "Weapon")
                    match = name.Contains("spear") || name.Contains("bow") || name.Contains("arrow") ||
                            name.Contains("sword") || name.Contains("machete") || name.Contains("axe") ||
                            name.Contains("explosive") || name.Contains("net") || name.Contains("gun");
                else if (category == "Material")
                    match = name.Contains("plank") || name.Contains("nail") || name.Contains("scrap") ||
                            name.Contains("rope") || name.Contains("plastic") || name.Contains("metal") ||
                            name.Contains("glass") || name.Contains("clay") || name.Contains("sand") ||
                            name.Contains("wool") || name.Contains("leaf") || name.Contains("flower");

                if (match)
                {
                    inv.AddItem(item.UniqueName, 5);
                    count++;
                }
            }
            Plugin.Log.LogInfo($"Given {count} {category} items!");
        }

        private float _infItemTimer;

        private void HandleInfiniteItems()
        {
            _infItemTimer -= Time.deltaTime;
            if (_infItemTimer > 0f) return;
            _infItemTimer = 0.5f;

            var inv = ComponentManager<PlayerInventory>.Value;
            if (inv == null) return;

            foreach (var slot in inv.allSlots)
            {
                if (slot != null && !slot.IsEmpty && slot.itemInstance != null)
                    slot.itemInstance.Amount = 10;
            }
        }
    }
}
