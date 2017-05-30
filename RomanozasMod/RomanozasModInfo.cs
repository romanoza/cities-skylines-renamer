using System;
using System.Collections.Generic;
// using System.Linq;
using System.Text;
// using System.Threading.Tasks;
using ICities;

namespace RomanozasMod
{
    public class RomanozasModInfo : IUserMod
    {
        public string Name {
            get { return "Romanoza's Mod"; }
        }

        public string Description {
            get { return "This is what I need to play"; }
        }
    }
}
