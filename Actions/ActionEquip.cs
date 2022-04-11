using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Use to equip/unequip equipment items
    /// </summary>
    
    [CreateAssetMenu(fileName = "Action", menuName = "Data/Actions/Equip", order = 50)]
    public class ActionEquip : SAction
    {

        public override void DoAction(PlayerCharacter character, ItemSlot slot)
        {
            ItemData item = slot.GetItem();
            if (item != null && item.type == ItemType.Equipment)
            {
                if (slot.is_equip)
                {
                    character.UnEquipItem(item, slot.index);
                }
                else
                {
                    character.EquipItem(item, slot.index);
                }

                TheUI.Get().CancelSelection();
            }
        }

        public override bool CanDoAction(PlayerCharacter character, ItemSlot slot)
        {
            ItemData item = slot.GetItem();
            return item != null && item.type == ItemType.Equipment;
        }
    }

}