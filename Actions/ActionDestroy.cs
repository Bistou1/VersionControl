using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Just destroy the destructible
    /// </summary>

    [CreateAssetMenu(fileName = "Action", menuName = "Data/Actions/Destroy", order = 50)]
    public class ActionDestroy : AAction
    {
        public string animation;

        public override void DoAction(PlayerCharacter character, Selectable select)
        {
            select.GetDestructible().KillIn(0.5f);
            character.TriggerAction(animation, select.transform.position);
        }

        public override bool CanDoAction(PlayerCharacter character, Selectable select)
        {
            return select.GetDestructible() && !select.GetDestructible().IsDead();
        }
    }

}