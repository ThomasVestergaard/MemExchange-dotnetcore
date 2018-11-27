using MemExchange.Core.Serialization;
using MemExchange.Core.SharedDto.ClientToServer;
using MemExchange.Server.Processor;
using NATS.Client;

namespace MemExchange.Server.Incoming
{
    public class NatsClientMessagePuller : IClientMessagePuller
    {
        private readonly ISerializer serializer;
        private readonly IIncomingMessageProcessor incomingMessageProcessor;
        private IConnection natsConnection;
        private const string natsClientToServerSubject = "memexchange-client-to-server";

        public NatsClientMessagePuller(ISerializer serializer, IIncomingMessageProcessor incomingMessageProcessor)
        {
            this.serializer = serializer;
            this.incomingMessageProcessor = incomingMessageProcessor;
        }

        public void Start(int listenPort)
        {
            var connectionFactory = new ConnectionFactory();
            natsConnection = connectionFactory.CreateConnection();

            natsConnection.SubscribeAsync(natsClientToServerSubject, (sender, args) =>
            {
                var message = serializer.Deserialize<ClientToServerMessage>(args.Message.Data);
                if (message != null)
                    incomingMessageProcessor.HandleMessage(message);

            });
        }

        public void Stop()
        {
            natsConnection.Close();
        }
    }
}
