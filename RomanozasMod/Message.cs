using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RomanozasMod
{
    public class Message: MessageBase
    {
        string _text;
        uint _residentId;

        public Message(string text): base() {
            _text = text;
            _residentId = MessageManager.instance.GetRandomResidentID();
        }

        public override uint GetSenderID() {
            return _residentId;
        }

        public override string GetSenderName() {
            return CitizenManager.instance.GetCitizenName(_residentId);
        }

        public override string GetText() {
            return _text;
        }
    }
}
