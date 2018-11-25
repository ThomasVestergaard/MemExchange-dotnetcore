using System;
using MemExchange.Core.Logging;
using MemExchange.Core.Serialization;
using MemExchange.Core.SharedDto.ClientToServer;
using NetMQ;
using NetMQ.Sockets;

namespace MemExchange.ClientApi.Commands
{
    public class MessageConnection : IMessageConnection
    {
        private readonly ILogger logger;
        private readonly ISerializer serializer;
        private PushSocket pushSocket;

        public MessageConnection(ILogger logger, ISerializer serializer)
        {
            this.logger = logger;
            this.serializer = serializer;
        }

        public void Start(string serverIpAddress, int serverPort)
        {
            
            
            
            string serverAddress = string.Format("tcp://{0}:{1}", serverIpAddress, serverPort);
            pushSocket = new PushSocket();
            pushSocket.Connect(serverAddress);
        }

        public void Stop()
        {
            pushSocket.Close();
        }

        public void SendMessage(ClientToServerMessage message)
        {
            if (message == null)
                return;

            if (pushSocket == null)
                return;
            
            try
            {
                var data = serializer.Serialize(message);
                var msg = new Msg();
                msg.InitGC(data, 0, data.Length);
                pushSocket.Send(ref msg, false);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception while sending message to server.");
            }
        }
    }
}