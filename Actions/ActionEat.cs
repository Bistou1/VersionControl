using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Eat an item
    /// </summary>
    

    [CreateAssetMenu(fileName = "Action", menuName = "Data/Actions/Eat", order = 50)]
    public class ActionEat : SAction
    {

        public override void DoAction(PlayerCharacter character, ItemSlot slot)
        {
            ItemData item = slot.GetItem();
            if (item != null)
            {
                character.EatItem(item, slot.index);
            }

        }

        public override bool CanDoAction(PlayerCharacter character, ItemSlot slot)
        {
            ItemData item = slot.GetItem();
            return item != null && item.type == ItemType.Consumable;
        }
    }

}