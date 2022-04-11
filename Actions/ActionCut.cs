using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Cut an item with another item (ex: open coconut with axe)
    /// </summary>

    [CreateAssetMenu(fileName = "Action", menuName = "Data/Actions/Cut", order = 50)]
    public class ActionCut : MAction
    {
        public ItemData cut_item;

        public override void DoAction(PlayerCharacter character, ItemSlot slot1, ItemSlot slot2)
        {
            if (PlayerData.Get().CanTakeItem(cut_item.id, 1))
            {
                PlayerData.Get().RemoveItemAt(slot1.index, 1);
                int islot = PlayerData.Get().AddItem(cut_item.id, 1, cut_item.durability);

                //Take fx
                ItemTakeFX.DoTakeFX(character.transform.position, cut_item, islot);
            }
        }
    }

}
