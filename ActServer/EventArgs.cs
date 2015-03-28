using System;

namespace RainbowMage.ActServer
{
    class MessageReceivedEventArgs : EventArgs
    {
        public string Target { get; private set; }
        public string Message { get; private set; }
        public MessageReceivedEventArgs(string target, string message)
        {
            this.Target = target;
            this.Message = message;
        }
    }
}
