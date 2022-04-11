using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Add fuel to a fire (wood, grass, etc)
    /// </summary>

    [CreateAssetMenu(fileName = "Action", menuName = "Data/Actions/AddFuel", order = 50)]
    public class ActionAddFuel : MAction
    {
        public override void DoAction(PlayerCharacter character, ItemSlot slot, Selectable select)
        {
            PlayerData pdata = PlayerData.Get();
            Firepit fire = select.GetComponent<Firepit>();
            if (fire != null && slot.GetItem() && pdata.HasItem(slot.GetItem().id))
            {
                fire.AddFuel(fire.wood_add_fuel);
                pdata.RemoveItemAt(slot.index, 1);
            }

        }
    }

}