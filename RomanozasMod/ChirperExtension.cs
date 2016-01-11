using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ColossalFramework.Plugins;
using ICities;

namespace RomanozasMod
{
    // source: https://github.com/mabako/reddit-for-city-skylines/blob/master/RedditSkylines/RedditUpdater.cs
    class ChirperExtension : ChirperExtensionBase
    {
        private bool IsPaused {
            get {
                return SimulationManager.instance.SimulationPaused;
            }
        }

        public override void OnCreated(IChirper threading) {
            DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "ChirperExtension oncreated");
        }

        private void AddMessage(Message m) {
            //if (IsPaused)
            //    return;

            MessageManager.instance.QueueMessage(m);
        }


    }
}
