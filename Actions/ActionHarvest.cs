using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Harvest the fruit of a plant
    /// </summary>
    
    [CreateAssetMenu(fileName = "Action", menuName = "Data/Actions/Harvest", order = 50)]
    public class ActionHarvest : AAction
    {
        public override void DoAction(PlayerCharacter character, Selectable select)
        {
            Plant plant = select.GetComponent<Plant>();
            if (plant != null)
            {
                string animation = PlayerCharacterAnim.Get() ? PlayerCharacterAnim.Get().take_anim : "";
                character.TriggerAction(animation, plant.transform.position, 0.5f, () =>
                {
                    plant.Harvest(character);
                });
            }
        }

        public override bool CanDoAction(PlayerCharacter character, Selectable select)
        {
            Plant plant = select.GetComponent<Plant>();
            if (plant != null)
            {
                return plant.HasFruit();
            }
            return false;
        }
    }

}