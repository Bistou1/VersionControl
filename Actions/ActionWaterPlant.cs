using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{

    /// <summary>
    /// Use to Water plant with the watering can
    /// </summary>

    [CreateAssetMenu(fileName = "Action", menuName = "Data/Actions/WaterPlant", order = 50)]
    public class ActionWaterPlant : AAction
    {
        public GroupData required_item;

        public override void DoAction(PlayerCharacter character, Selectable select)
        {
            InventoryItemData item = character.GetEquippedItemInGroup(required_item);
            ItemData idata = ItemData.Get(item?.item_id);
            Plant plant = select.GetComponent<Plant>();
            if (idata != null && plant != null)
            {
                //Remove water
                if (idata.durability_type == DurabilityType.UsageCount)
                    item.durability -= 1f;
                else
                    character.RemoveEquipItem(ItemData.GetEquipIndex(idata.equip_slot));

                //Add to plant
                plant.AddWater(character);

                string animation = PlayerCharacterAnim.Get() ? PlayerCharacterAnim.Get().water_anim : "";
                character.TriggerAction(animation, plant.transform.position, 1f);
            }
        }

        public override bool CanDoAction(PlayerCharacter character, Selectable select)
        {
            Plant plant = select.GetComponent<Plant>();
            return plant != null && character.HasEquippedItemInGroup(required_item);
        }
    }

}
