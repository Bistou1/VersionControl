using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Learn a crafting recipe
    /// </summary>
    

    [CreateAssetMenu(fileName = "Action", menuName = "Data/Actions/Learn", order = 50)]
    public class ActionLearn : SAction
    {
        public AudioClip learn_audio;
        public bool destroy_on_learn = true;
        public CraftData[] learn_list;

        public override void DoAction(PlayerCharacter character, ItemSlot slot)
        {
            foreach (CraftData data in learn_list)
            {
                PlayerData.Get().UnlockID(data.id);
            }

            TheAudio.Get().PlaySFX("learn", learn_audio);

            if (destroy_on_learn)
                PlayerData.Get().RemoveItemAt(slot.index, 1);

            CraftSubBar.Get().RefreshPanel();
        }

        public override bool CanDoAction(PlayerCharacter character, ItemSlot slot)
        {
            return true;
        }
    }

}