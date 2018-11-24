using System.Threading;
using MemExchange.Core.Logging;
using MemExchange.Core.SharedDto;
using MemExchange.Core.SharedDto.ServerToClient;
using MemExchange.Server.Outgoing;
using MemExchange.Server.Processor.Book.Orders;
using NUnit.Framework;
using Moq;

namespace MemExchange.Tests.Server
{
    [TestFixture]
    public class OutgoingQueueTests
    {

        private Mock<IMessagePublisher> messagePublisherMock;
        private Mock<ILogger> loggerMock;
        private IOutgoingQueue outgoingQueue;

        [SetUp]
        public void Setup()
        {
            messagePublisherMock = new Mock<IMessagePublisher>();
            loggerMock = new Mock<ILogger>();
            outgoingQueue = new OutgoingQueue(loggerMock.Object, messagePublisherMock.Object);
            outgoingQueue.Start();
        }

        [TearDown]
        public void Teardown()
        {
            outgoingQueue.Stop();
        }

        [Test]
        public void PublisherShouldReceiveOrderAddedData()
        {
            var limitOrder = new LimitOrder("ABC", 21, 20d, WayEnum.Buy, 90);
            
            outgoingQueue.EnqueueAddedLimitOrder(limitOrder);

            Thread.Sleep(100);
            messagePublisherMock.Verify(a => a.OnEvent(It.Is<ServerToClientMessage>(b => 
                b.MessageType == ServerToClientMessageTypeEnum.LimitOrderAccepted
                && b.LimitOrder.ClientId == 90
                && b.LimitOrder.Price == 20d
                && b.LimitOrder.Quantity == 21
                && b.LimitOrder.Symbol == "ABC"
                && b.LimitOrder.Way == WayEnum.Buy),
                It.IsAny<long>(),
                It.IsAny<bool>()), Times.Once);
        }

        [Test]
        public void PublisherShouldReceiveOrderModifiedData()
        {
            var limitOrder = new LimitOrder("ABC", 21, 20d, WayEnum.Buy, 90);
            
            outgoingQueue.EnqueueUpdatedLimitOrder(limitOrder, 21, 20d);

            Thread.Sleep(100);
            messagePublisherMock.Verify(a => a.OnEvent(It.Is<ServerToClientMessage>(b =>
                b.MessageType == ServerToClientMessageTypeEnum.LimitOrderChanged
                && b.LimitOrder.ClientId == 90
                && b.LimitOrder.Price == 20d
                && b.LimitOrder.Quantity == 21
                && b.LimitOrder.Symbol == "ABC"
                && b.LimitOrder.Way == WayEnum.Buy),
                It.IsAny<long>(),
                It.IsAny<bool>()), Times.Once);
        }

        [Test]
        public void PublisherShouldReceiveOrderDeletedData()
        {
            var limitOrder = new LimitOrder("ABC", 21, 20d, WayEnum.Buy, 90);
            
            outgoingQueue.EnqueueDeletedLimitOrder(limitOrder);

            Thread.Sleep(100);
            messagePublisherMock.Verify(a => a.OnEvent(It.Is<ServerToClientMessage>(b =>
                b.MessageType == ServerToClientMessageTypeEnum.LimitOrderDeleted
                && b.LimitOrder.ClientId == 90
                && b.LimitOrder.Price == 20d
                && b.LimitOrder.Quantity == 21
                && b.LimitOrder.Symbol == "ABC"
                && b.LimitOrder.Way == WayEnum.Buy),
                It.IsAny<long>(),
                It.IsAny<bool>()), Times.Once);
        }

    }
}
