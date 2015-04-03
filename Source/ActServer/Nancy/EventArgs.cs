using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainbowMage.ActServer.Nancy
{
    public class MessageReceivedEventArgs : EventArgs
    {
        public byte[] Message { get; private set; }

        public MessageReceivedEventArgs(byte[] message)
        {
            this.Message = message;
        }
    }
}
