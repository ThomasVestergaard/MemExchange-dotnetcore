using MemExchange.Core.SharedDto.ClientToServer;

namespace MemExchange.Server.Processor
{
    public interface IIncomingMessageProcessor
    {
        void HandleMessage(ClientToServerMessage clientToServerMessage);
    }
}
