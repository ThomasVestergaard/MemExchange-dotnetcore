using System.Security;
using MemExchange.Core.Serialization;
using MemExchange.Core.SharedDto;
using MemExchange.Core.SharedDto.ClientToServer;
using MemExchange.Core.SharedDto.Orders;
using MemExchange.Server.Common;
using MemExchange.Server.Incoming;
using MemExchange.Server.Outgoing;
using MemExchange.Server.Processor;
using MemExchange.Server.Processor.Book;
using MemExchange.Server.Processor.Book.Orders;
using NUnit.Framework;
using Moq;


namespace MemExchange.Tests.Server
{
    [TestFixture]
    public class IncomingMessageProcessorTests
    {
        private Mock<IOrderRepository> ordereRepositoryMock;
        private Mock<IOutgoingQueue> outgoingQueueMock;
        private Mock<IDateService> dateServiceMock;
        private Mock<IOrderDispatcher> orderDispatcherMock;
        private ISerializer serializer;

        [SetUp]
        public void Setup()
        {
            ordereRepositoryMock = new Mock<IOrderRepository>();
            outgoingQueueMock = new Mock<IOutgoingQueue>();
            dateServiceMock = new Mock<IDateService>();
            orderDispatcherMock = new Mock<IOrderDispatcher>();
            serializer = new ProtobufSerializer();
        }

        [Test]
        public void ShouldNotCallDispatcherIfOrderDoesNotValidate()
        {
            var processor = new IncomingMessageProcessor(ordereRepositoryMock.Object, outgoingQueueMock.Object, dateServiceMock.Object, orderDispatcherMock.Object, serializer);

            var invalidLimitOrder = new LimitOrderDto();
            invalidLimitOrder.Reeset();
            
            var message = new ClientToServerMessage
            {
                ClientId = 1,
                LimitOrder = invalidLimitOrder,
                MessageType = ClientToServerMessageTypeEnum.PlaceLimitOrder
            };
            
            processor.HandleMessage(message);

            orderDispatcherMock.Verify(a => a.HandleAddLimitOrder(It.IsAny<ILimitOrder>()), Times.Never);
            outgoingQueueMock.Verify(a => a.EnqueueAddedLimitOrder(It.IsAny<ILimitOrder>()), Times.Never);
            
        }

        [Test]
        public void ShouldCallDispatcherWhenLimitOrderValidates()
        {
            var limitOrder = new LimitOrderDto();
            limitOrder.Reeset();
            limitOrder.Symbol = "QQQ";
            limitOrder.Price = 30;
            limitOrder.Quantity = 10;
            limitOrder.ClientId = 1;
            limitOrder.Way = WayEnum.Sell;

            var processor = new IncomingMessageProcessor(ordereRepositoryMock.Object, outgoingQueueMock.Object, dateServiceMock.Object, orderDispatcherMock.Object, serializer);

            ordereRepositoryMock.Setup(a => a.NewLimitOrder(It.IsAny<LimitOrderDto>())).Returns(new LimitOrder(limitOrder.Symbol, limitOrder.Quantity, limitOrder.Price, limitOrder.Way, limitOrder.ClientId));
            
            var message =

                new ClientToServerMessage
                {
                    ClientId = 1,
                    LimitOrder = limitOrder,
                    MessageType = ClientToServerMessageTypeEnum.PlaceLimitOrder
                };

            processor.HandleMessage(message);

            orderDispatcherMock.Verify(a => a.HandleAddLimitOrder(It.Is<ILimitOrder>(order =>
                order.Symbol == "QQQ" &&
                order.Price == 30 &&
                order.Quantity == 10 &&
                order.ClientId == 1 &&
                order.Way == WayEnum.Sell

            )), Times.Once);
        }

        [Test]
        public void ShouldNotCallDispatcherWhenLimitOrderIsInvalid()
        {
            var processor = new IncomingMessageProcessor(ordereRepositoryMock.Object, outgoingQueueMock.Object,
                dateServiceMock.Object, orderDispatcherMock.Object, serializer);

            var limitOrder = new LimitOrderDto();
            limitOrder.Reeset();
            limitOrder.Symbol = "QQQ";
            limitOrder.Price = -1;
            limitOrder.Quantity = -1;
            limitOrder.ClientId = 1;
            limitOrder.Way = WayEnum.Sell;
            
            var message =

                new ClientToServerMessage
                {
                    ClientId = 1,
                    LimitOrder = limitOrder,
                    MessageType = ClientToServerMessageTypeEnum.PlaceLimitOrder
                };

            processor.HandleMessage(message);
            orderDispatcherMock.Verify(a => a.HandleAddLimitOrder(It.IsAny<ILimitOrder>()), Times.Never);
        }

        [Test]
        public void ShouldNotCallDispatcherWhenLimitOrderIsInvalidOnCancelOrder()
        {
            var processor = new IncomingMessageProcessor(ordereRepositoryMock.Object, outgoingQueueMock.Object,
                dateServiceMock.Object, orderDispatcherMock.Object, serializer);

            var limitOrder = new LimitOrderDto();
            limitOrder.Reeset();


            var message =
                new ClientToServerMessage
                {
                    ClientId = 1,
                    LimitOrder = limitOrder,
                    MessageType = ClientToServerMessageTypeEnum.CancelLimitOrder
                };


            processor.HandleMessage(message);

            orderDispatcherMock.Verify(a => a.HandleAddLimitOrder(It.IsAny<ILimitOrder>()), Times.Never());
        }


    }
}
