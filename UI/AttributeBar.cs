using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{

    /// <summary>
    /// Just a bar showing one of the attributes
    /// </summary>

    [RequireComponent(typeof(ProgressBar))]
    public class AttributeBar : MonoBehaviour
    {
        public AttributeType attribute;

        private ProgressBar bar;

        void Awake()
        {
            bar = GetComponent<ProgressBar>();
        }

        void Update()
        {
            PlayerCharacter character = PlayerCharacter.Get();
            bar.SetMax(Mathf.RoundToInt(character.GetAttributeMax(attribute)));
            bar.SetValue(Mathf.RoundToInt(character.GetAttributeValue(attribute)));
        }
    }

}