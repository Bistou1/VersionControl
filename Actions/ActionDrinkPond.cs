using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Drink directly from a water source
    /// </summary>

    [CreateAssetMenu(fileName = "Action", menuName = "Data/Actions/DrinkPond", order = 50)]
    public class ActionDrinkPond : SAction
    {
        public float drink_hp;
        public float drink_hunger;
        public float drink_thirst;
        public float drink_happiness;

        public override void DoAction(PlayerCharacter character, Selectable select)
        {
            string animation = PlayerCharacterAnim.Get() ? PlayerCharacterAnim.Get().take_anim : "";
            character.TriggerAction(animation, select.transform.position, 0.5f, () =>
            {
                PlayerData.Get().AddAttributeValue(AttributeType.Health, drink_hp, character.GetAttributeMax(AttributeType.Health));
                PlayerData.Get().AddAttributeValue(AttributeType.Hunger, drink_hunger, character.GetAttributeMax(AttributeType.Hunger));
                PlayerData.Get().AddAttributeValue(AttributeType.Thirst, drink_thirst, character.GetAttributeMax(AttributeType.Thirst));
                PlayerData.Get().AddAttributeValue(AttributeType.Happiness, drink_happiness, character.GetAttributeMax(AttributeType.Happiness));

            });
            
        }
    }

}
