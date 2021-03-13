using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Split item stack into 2 stacks
    /// </summary>
    
    [CreateAssetMenu(fileName = "Action", menuName = "SurvivalEngine/Actions/Split", order = 50)]
    public class ActionSplit : SAction
    {
        public override void DoAction(PlayerCharacter character, ItemSlot slot)
        {
            int half = slot.GetQuantity() / 2;
            ItemData item = slot.GetItem();
            InventoryData inventory = slot.GetInventory();
            InventoryItemData item_data = inventory.GetItem(slot.index);
            inventory.RemoveItemAt(slot.index, half);

            InventoryData ninventory = character.Inventory.GetValidInventory(item, half);
            int new_slot = ninventory.GetFirstEmptySlot();
            ninventory.AddItemAt(item.id, new_slot, half, item_data.durability, UniqueID.GenerateUniqueID());
        }

        public override bool CanDoAction(PlayerCharacter character, ItemSlot slot)
        {
            ItemData item = slot.GetItem();
            InventoryData inventory = slot.GetInventory();
            return item != null && inventory != null && slot.GetQuantity() > 1 && character.Inventory.HasEmptySlot();
        }
    }

}