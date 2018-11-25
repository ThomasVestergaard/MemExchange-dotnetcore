using System;
using MemExchange.Core.Logging;


namespace MemExchange.Client.UI.Setup
{
    public class DummyLogger : ILogger
    {
        public void Info(string message)
        {
            
        }

        public void Error(Exception exception, string message)
        {
            
        }
    }
}
