using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Build an item into a construction (trap, lure)
    /// </summary>
    
    [CreateAssetMenu(fileName = "Action", menuName = "Data/Actions/Build", order = 50)]
    public class ActionBuild : SAction
    {
        public override void DoAction(PlayerCharacter character, ItemSlot slot)
        {
            ItemData item = slot.GetItem();
            if (item != null && item.construction_data != null)
            {
                character.CraftConstructionBuildMode(item.construction_data, false, (Buildable build) =>
                {
                    InventoryItemData invdata = PlayerData.Get().GetItemSlot(slot.index);
                    PlayerData.Get().RemoveItemAt(slot.index, 1);

                    BuiltConstructionData constru = PlayerData.Get().GetConstructed(build.GetUID());
                    if (invdata != null && constru != null && item.HasDurability())
                        constru.durability = invdata.durability; //Save durability
                });

                TheAudio.Get().PlaySFX("craft", item.craft_sound);
            }
        }
    }

}
