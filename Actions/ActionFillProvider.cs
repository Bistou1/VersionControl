using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Fill a jug with item from item provider
    /// </summary>
    
    [CreateAssetMenu(fileName = "Action", menuName = "Data/Actions/FillProvider", order = 50)]
    public class ActionFillProvider : MAction
    {
        public float fill_range = 2f;

        public override void DoAction(PlayerCharacter character, ItemSlot slot)
        {
            Selectable fill_source = Selectable.GetNearestGroup(merge_target, character.transform.position, fill_range);
            ItemProvider provider = fill_source != null ? fill_source.GetComponent<ItemProvider>() : null;
            provider.RemoveItem();

            if (slot.type == ItemSlotType.Equipment)
            {
                PlayerData.Get().UnequipItem(slot.index);
                PlayerData.Get().EquipItem(slot.index, provider.item.id, provider.item.durability);
            }
            else 
            {
                PlayerData.Get().RemoveItemAt(slot.index, 1);
                character.GainItem(provider.item, 1, character.transform.position);
            }
        }

        public override void DoAction(PlayerCharacter character, ItemSlot slot, Selectable select)
        {
            if (select.HasGroup(merge_target))
            {
                ItemProvider provider = select.GetComponent<ItemProvider>();
                provider.RemoveItem();

                if (slot.type == ItemSlotType.Equipment)
                {
                    PlayerData.Get().UnequipItem(slot.index);
                    PlayerData.Get().EquipItem(slot.index, provider.item.id, provider.item.durability);
                }
                else 
                {
                    PlayerData.Get().RemoveItemAt(slot.index, 1);
                    character.GainItem(provider.item, 1, select.transform.position);
                }
            }
        }

        public override bool CanDoAction(PlayerCharacter character, ItemSlot slot)
        {
            Selectable fill_source = Selectable.GetNearestGroup(merge_target, character.transform.position, fill_range);
            ItemProvider provider = fill_source != null ? fill_source.GetComponent<ItemProvider>() : null;
            return provider != null && provider.HasItem();
        }

        public override bool CanDoAction(PlayerCharacter character, Selectable select)
        {
            ItemProvider provider = select != null ? select.GetComponent<ItemProvider>() : null;
            return provider != null && provider.HasItem();
        }
    }

}