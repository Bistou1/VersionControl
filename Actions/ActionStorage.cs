using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace SurvivalEngine
{
    /// <summary>
    /// Use to allow storing item inside a construction (chest)
    /// Careful! This action will not work without a UniqueID set on the selectable
    /// </summary>

    [CreateAssetMenu(fileName = "Action", menuName = "SurvivalEngine/Actions/Storage", order = 50)]
    public class ActionStorage : AAction
    {
        public int max_storage = 10;

        public override void DoAction(PlayerCharacter character, Selectable select)
        {
            string uid = select.GetUID();
            if (!string.IsNullOrEmpty(uid))
                StoragePanel.Get().ShowStorage(character, uid, max_storage);
        }
    }

}
