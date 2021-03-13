using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Cook an item on the fire (like raw meat)
    /// </summary>

    [CreateAssetMenu(fileName = "Action", menuName = "SurvivalEngine/Actions/Cook", order = 50)]
    public class ActionCook : MAction
    {
        public ItemData cooked_item;

        //Merge action
        public override void DoAction(PlayerCharacter character, ItemSlot slot, Selectable select)
        {
            InventoryData inventory = slot.GetInventory();
            inventory.RemoveItemAt(slot.index, 1);
            character.Inventory.GainItem(cooked_item, 1);
        }

    }

}
