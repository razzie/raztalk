using System;

namespace raztalk.bot
{
    public interface IBot : IDisposable
    {
        string Name { get; }
        event EventHandler<string> NewMessage;
        void ConsumeMessage(string user, string message, DateTime timestamp);
    }

    public abstract class Bot : IBot
    {
        public abstract string Name { get; }
        public abstract event EventHandler<string> NewMessage;
        public abstract void ConsumeMessage(string user, string message, DateTime timestamp);
        public abstract void Dispose();
    }
}
