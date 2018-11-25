using System;
using MemExchange.Core.SharedDto.ClientToServer;
using ProtoBuf;

namespace MemExchange.Server.Incoming
{
    [ProtoContract]
    public class ClientToServerMessageQueueItem
    {
        [ProtoMember(1)]
        public ClientToServerMessage Message { get; set; }
        [ProtoMember(2)]
        public DateTimeOffset StartProcessTime { get; set; }

        public ClientToServerMessageQueueItem()
        {
            Message = new ClientToServerMessage();
        }

        public void Update(ClientToServerMessage otherMessage)
        {
            Message.Update(otherMessage);
        }
    }
}
