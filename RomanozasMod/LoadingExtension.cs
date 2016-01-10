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

            // Create the button and make it appear on the screen.
            // UIView seems to be what is used to enable whatever element should be appearing.
            // Without it, it seems like that element will never show up.

            var boxuiView = UIView.GetAView();
            var statButton = (UIButton)boxuiView.AddUIComponent(typeof(UIButton));

            // This does more stuff.  Y'know.  Stuuuuuff.

            var uiView = GameObject.FindObjectOfType<UIView>();
            if (uiView == null) return;

            // Define how big the button is!
            // It's come to my attention that this may not display correctly in non-16:9 resolutions.
            // I'll figure it out after I actually play video games for awhile.

            statButton.width = 125;
            statButton.height = 30;

            // Defines the colors and such of the button.  Fancy!
            // The color values for statButton.textColor are that blue-ish color used on the UI to show your cash and population.
            // Seemed right to also use that for my buttons.  Everything looking uniform is cool.

            statButton.normalBgSprite = "ButtonMenu";
            statButton.hoveredBgSprite = "ButtonMenuHovered";
            statButton.focusedBgSprite = "ButtonMenuFocused";
            statButton.pressedBgSprite = "ButtonMenuPressed";
            //statButton.textColor = new Color32(255, 255, 255, 255);
            statButton.textColor = new Color32(186, 217, 238, 0);
            statButton.disabledTextColor = new Color32(7, 7, 7, 255);
            statButton.hoveredTextColor = new Color32(7, 132, 255, 255);
            statButton.focusedTextColor = new Color32(255, 255, 255, 255);
            statButton.pressedTextColor = new Color32(30, 30, 44, 255);

            // .transformPosition places where the button will show up on the screen.  Vector3 uses x/y.  You can also set a Z coordinate but why would you do that here?
            // I am not terribly good at stuff like vectors, so honestly, I've just been punching in numbers until it looks right.
            // Look, I'm not here to judge.

            // BringToFront does exactly what you'd expect. It's part of the ColossalFramework.UI.UIComponent class.
            // Without it, the button would end up behind the rest of the UI.
            // Chances are when I go in to make sure buttons are displaying right at 16:10
            // I'll have to do something with that.

            statButton.transformPosition = new Vector3(1.2f, -0.93f);
            statButton.BringToFront();

            // Mmhm.  You know what this does.

            statButton.text = "Rename Buildings!";

            // Points the button towards the logic needed for the button to do stuff.

            // statButton.eventClick += ButtonClick;

        }

    }
}
