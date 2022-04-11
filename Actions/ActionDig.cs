﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Dig using the shovel, to remove plants or to dig burried things
    /// </summary>

    [CreateAssetMenu(fileName = "Action", menuName = "Data/Actions/Dig", order = 50)]
    public class ActionDig : SAction
    {
        public float dig_range = 2f;

        public override void DoAction(PlayerCharacter character, ItemSlot slot)
        {
            DigSpot spot = DigSpot.GetNearest(character.transform.position, dig_range);
            Plant plant = Plant.GetNearest(character.transform.position, dig_range);

            Vector3 pos = plant != null ? plant.transform.position : character.transform.position;
            if (spot != null)
                pos = spot.transform.position;

            string animation = PlayerCharacterAnim.Get() ? PlayerCharacterAnim.Get().dig_anim : "";
            character.TriggerAction(animation, pos, 1.5f, () =>
            {
                if (spot != null)
                    spot.Dig();
                else if (plant != null)
                    plant.Kill();

                InventoryItemData ivdata = PlayerData.Get().GetEquippedItemSlot(slot.index);
                if (ivdata != null)
                    ivdata.durability -= 1;
            });
        }

        public override bool CanDoAction(PlayerCharacter character, ItemSlot slot)
        {
            return slot.type == ItemSlotType.Equipment;
        }
    }

}