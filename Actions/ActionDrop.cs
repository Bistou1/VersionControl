using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Drop an item 
    /// </summary>
    
    [CreateAssetMenu(fileName = "Action", menuName = "Data/Actions/Drop", order = 50)]
    public class ActionDrop : SAction
    {

        public override void DoAction(PlayerCharacter character, ItemSlot slot)
        {
            if (slot.is_equip)
                character.DropEquippedItem(slot.index);
            else
                character.DropItem(slot.index);
        }
    }

}