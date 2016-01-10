using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ColossalFramework.UI;
using ICities;
using UnityEngine;

namespace RomanozasMod
{
    public class LoadingExtension: LoadingExtensionBase
    {
        // source: https://github.com/Alakaiser/Cities-Skylines-Stat-Button/blob/master/City%20Statistics%20Button/StatButton.cs
        public override void OnLevelLoaded(LoadMode mode) {

            //Debug.Log("OnLevelLoaded");
            var uiView = UIView.GetAView();
            var btnRename = (UIButton)uiView.AddUIComponent(typeof(UIButton));

            var uiView2 = GameObject.FindObjectOfType<UIView>();
            if (uiView2 == null) return;

            btnRename.width = 135;
            btnRename.height = 30;

            btnRename.normalBgSprite = "ButtonMenu";
            btnRename.hoveredBgSprite = "ButtonMenuHovered";
            btnRename.focusedBgSprite = "ButtonMenuFocused";
            btnRename.pressedBgSprite = "ButtonMenuPressed";
            btnRename.textColor = new Color32(186, 217, 238, 0);
            btnRename.disabledTextColor = new Color32(7, 7, 7, 255);
            btnRename.hoveredTextColor = new Color32(7, 132, 255, 255);
            btnRename.focusedTextColor = new Color32(255, 255, 255, 255);
            btnRename.pressedTextColor = new Color32(30, 30, 44, 255);

            btnRename.transformPosition = new Vector3(1.2f, -0.93f);
            btnRename.BringToFront();

            btnRename.text = "Rename Buildings!";
            btnRename.playAudioEvents = true;

            btnRename.eventClick += btnRename_Click;
        }

        private void btnRename_Click(UIComponent component, UIMouseEventParameter eventParam) {

            /* Not much here, ironically!  Most/all of the functionality for this mode more or less existed in-game.
             * You can insert any panel where "StatisticsPanel" is.  They're all contained in Assembly-CSharp.
             * 
             * Accessing whateverPanel should also allow us to add elements to these things.
             * For instance, I'm fairly confident that I could add another graph to StatisticsPanel if
             * I beat my head against it for long enough.
             * 
             * Go HOG WILD. http://imgur.com/sUK6b0m */

            // Debug.LogDebugMessage("Adding 'Generate City Report' button to UI");

            UIView.library.ShowModal("StatisticsPanel");
            UIView.library.ShowModal("StatisticsPanel").BringToFront();
            SimulationManager.instance.SimulationPaused = true;
        }

    }
}
