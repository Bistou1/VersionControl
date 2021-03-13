using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Melt an item in the furnace
    /// </summary>

    [CreateAssetMenu(fileName = "Action", menuName = "SurvivalEngine/Actions/Furnace", order = 50)]
    public class ActionFurnace : MAction
    {
        public ItemData melt_item;
        public float duration = 1f; //In game hours

        //Merge action
        public override void DoAction(PlayerCharacter character, ItemSlot slot, Selectable select)
        {
            InventoryData inventory = slot.GetInventory();
            InventoryItemData iidata = inventory.GetItem(slot.index);
            inventory.RemoveItemAt(slot.index, iidata.quantity);

            Furnace furnace = select.GetComponent<Furnace>();
            if (furnace != null && !furnace.HasItem())
                furnace.PutItem(slot.GetItem(), melt_item, duration, iidata.quantity);
        }

        public override bool CanDoAction(PlayerCharacter character, ItemSlot slot, Selectable select)
        {
            Furnace furnace = select.GetComponent<Furnace>();
            InventoryData inventory = slot.GetInventory();
            InventoryItemData iidata = inventory?.GetItem(slot.index);
            return furnace != null && iidata != null && !furnace.HasItem();
        }
    }

}
