using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivalEngine
{
    
    public class ReadPanel : UIPanel
    {
        public Text title;
        public Text desc;

        private static ReadPanel _instance;

        protected override void Awake()
        {
            _instance = this;
            base.Awake();

        }

        public void ShowPanel(string title, string desc)
        {
            this.title.text = title;
            this.desc.text = desc;

            Show();
        }

        public void ClickOK()
        {
            Hide();
        }

        public static ReadPanel Get()
        {
            return _instance;
        }
    }

}
