using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Cut a plant and return it to growth stage 0, and gain items (cut grass)
    /// </summary>

    [CreateAssetMenu(fileName = "Action", menuName = "Data/Actions/CutPlant", order = 50)]
    public class ActionCutPlant : AAction
    {
        public ItemData[] loots;

        public override void DoAction(PlayerCharacter character, Selectable select)
        {
            Plant plant = select.GetComponent<Plant>();
            if (plant != null)
            {
                string animation = PlayerCharacterAnim.Get() ? PlayerCharacterAnim.Get().take_anim : "";
                character.TriggerAction(animation, plant.transform.position, 0.5f, () =>
                {
                    plant.GrowPlant(0);

                    Destructible destruct = plant.GetDestructible();
                    TheAudio.Get().PlaySFX("destruct", destruct.death_sound);

                    foreach (ItemData item in loots)
                    {
                        destruct.SpawnLoot(item);
                    }
                });
            }
        }

        public override bool CanDoAction(PlayerCharacter character, Selectable select)
        {
            return select.GetComponent<Plant>();
        }
    }

}