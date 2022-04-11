using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{

    /// <summary>
    /// Use to take a construction that has an item variant (lure/trap)
    /// </summary>

    [CreateAssetMenu(fileName = "Action", menuName = "Data/Actions/Take", order = 50)]
    public class ActionTake : SAction
    {
        public override void DoAction(PlayerCharacter character, Selectable select)
        {
            PlayerData pdata = PlayerData.Get();
            Construction construction = select.GetComponent<Construction>();
            if (construction != null)
            {
                ItemData take_item = construction.data.take_item_data;
                if (take_item != null && pdata.CanTakeItem(take_item.id, 1))
                {
                    BuiltConstructionData bdata = pdata.GetConstructed(construction.GetUID());
                    float durability = bdata != null ? bdata.durability : take_item.durability;
                    pdata.AddItem(take_item.id, 1, durability);
                    select.Destroy();
                }
            }
        }
    }

}
