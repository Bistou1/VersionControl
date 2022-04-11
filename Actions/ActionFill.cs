using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Fill a jug with water (or other)
    /// </summary>
    
    [CreateAssetMenu(fileName = "Action", menuName = "Data/Actions/Fill", order = 50)]
    public class ActionFill : MAction
    {
        public ItemData filled_item;
        public float fill_range = 2f;

        public override void DoAction(PlayerCharacter character, ItemSlot slot)
        {
            if (slot.type == ItemSlotType.Equipment)
            {
                PlayerData.Get().UnequipItem(slot.index);
                PlayerData.Get().EquipItem(slot.index, filled_item.id, filled_item.durability);
            }
            else 
            {
                PlayerData.Get().RemoveItemAt(slot.index, 1);
                character.GainItem(filled_item, 1, character.transform.position);
            }
        }

        public override void DoAction(PlayerCharacter character, ItemSlot slot, Selectable select)
        {
            if (select.HasGroup(merge_target))
            {
                if (slot.type == ItemSlotType.Equipment)
                {
                    PlayerData.Get().UnequipItem(slot.index);
                    PlayerData.Get().EquipItem(slot.index, filled_item.id, filled_item.durability);
                }
                else 
                {
                    PlayerData.Get().RemoveItemAt(slot.index, 1);
                    character.GainItem(filled_item, 1, select.transform.position);
                }
            }
        }

        public override bool CanDoAction(PlayerCharacter character, ItemSlot slot)
        {
            Selectable water_source = Selectable.GetNearestGroup(merge_target, character.transform.position, fill_range);
            return water_source != null;
        }
    }

}