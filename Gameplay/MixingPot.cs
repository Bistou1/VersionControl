using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine {

    public class MixingPot : MonoBehaviour
    {
        public ItemData[] recipes;
        public int max_items = 6;
        public bool clear_on_mix = false;

        private Selectable select;

        void Start()
        {
            select = GetComponent<Selectable>();

            select.onUse += OnUse;
        }

        void Update()
        {

        }

        private void OnUse(PlayerCharacter player)
        {
            MixingPanel.Get().ShowMixing(player, this, select.GetUID());
        }
    }

}
