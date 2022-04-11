using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{

    /// <summary>
    /// Merge Action parent class: Any action that happens when mixing two items (ex: coconut and axe), or one item with a selectable (ex: raw food on top of fire)
    /// </summary>

    public class MAction : SAction
    {
        public GroupData merge_target;

        //When using an ItemData action on another ItemData (ex: cut coconut), slot is the one with the action, slot_other is the one without the action
        public virtual void DoAction(PlayerCharacter character, ItemSlot slot, ItemSlot slot_other)
        {

        }

        //When using an ItemData action on a Selectable (ex: cook meat)
        public virtual void DoAction(PlayerCharacter character, ItemSlot slot, Selectable select)
        {

        }

        public override void DoAction(PlayerCharacter character, ItemSlot slot)
        {
            Selectable select = Selectable.GetNearestGroup(merge_target, character.transform.position);
            if (select != null)
            {
                DoAction(character, slot, select);
            }
        }

        //Condition for the action to be allowed, override if you need to add a new condition
        public override bool CanDoAction(PlayerCharacter character, ItemSlot slot_other) //slot_other is the one without the action
        {
            ItemData item = slot_other.GetItem();
            return item != null && item.HasGroup(merge_target);
        }

        //Condition for the action to be allowed, override if you need to add a new condition
        public override bool CanDoAction(PlayerCharacter character, Selectable select)
        {
            return select != null && select.HasGroup(merge_target);
        }

    }

}