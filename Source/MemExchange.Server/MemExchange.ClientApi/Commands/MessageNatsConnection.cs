using System;
using MemExchange.Core.Logging;
using MemExchange.Core.Serialization;
using MemExchange.Core.SharedDto.ClientToServer;
using NATS.Client;

namespace MemExchange.ClientApi.Commands
{
    public class MessageNatsConnection : IMessageConnection
    {
        private readonly ILogger logger;
        private readonly ISerializer serializer;
        private IConnection natsConnection;
        private const string natsClientToServerSubject = "memexchange-client-to-server";


        public MessageNatsConnection(ILogger logger, ISerializer serializer)
        {
            this.logger = logger;
            this.serializer = serializer;
        }

        public void Start(string serverIpAddress, int serverPort)
        {
            var connectionFactory = new ConnectionFactory();
            natsConnection = connectionFactory.CreateConnection();
        }

        public void Stop()
        {
            if (natsConnection != null && (!natsConnection.IsClosed() || !natsConnection.IsReconnecting()))
                natsConnection.Close();
        }

        public void SendMessage(ClientToServerMessage message)
        {
            if (message == null)
                return;

            try
            {
                var data = serializer.Serialize(message);
                natsConnection.Publish(natsClientToServerSubject, data);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception while sending message to server.");
            }
        }
    }
}
