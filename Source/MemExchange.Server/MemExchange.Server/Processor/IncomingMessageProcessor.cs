using MemExchange.Core.Serialization;
using MemExchange.Core.SharedDto.ClientToServer;
using MemExchange.Server.Common;
using MemExchange.Server.Incoming;
using MemExchange.Server.Outgoing;
using MemExchange.Server.Processor.Book;

namespace MemExchange.Server.Processor
{
    public class IncomingMessageProcessor : IIncomingMessageProcessor
    {
        private readonly IOrderRepository ordeRepository;
        private readonly IOutgoingQueue outgoingQueue;
        private readonly IOrderDispatcher dispatcher;

        public IncomingMessageProcessor(IOrderRepository ordeRepository, IOutgoingQueue outgoingQueue, IDateService dateService, IOrderDispatcher dispatcher, ISerializer serializer)
        {
            this.ordeRepository = ordeRepository;
            this.outgoingQueue = outgoingQueue;
            this.dispatcher = dispatcher;

        }

        public void HandleMessage(ClientToServerMessage clientToServerMessage)
        {
            switch (clientToServerMessage.MessageType)
            {
                case ClientToServerMessageTypeEnum.ModifyStopLimitOrder:
                    if (clientToServerMessage.ClientId <= 0)
                        break;

                    var stopLimitOrderToModify = ordeRepository.TryGetStopLimitOrder(clientToServerMessage.StopLimitOrder.ExchangeOrderId);
                    if (stopLimitOrderToModify == null)
                        return;

                    stopLimitOrderToModify.Modify(clientToServerMessage.StopLimitOrder.TriggerPrice, clientToServerMessage.StopLimitOrder.LimitPrice, clientToServerMessage.StopLimitOrder.Quantity);
                    outgoingQueue.EnqueueUpdatedStopLimitOrder(stopLimitOrderToModify);

                    break;

                case ClientToServerMessageTypeEnum.RequestOpenStopLimitOrders:
                    if (clientToServerMessage.ClientId <= 0)
                        break;

                    var orders = ordeRepository.GetClientStopLimitOrders(clientToServerMessage.ClientId);
                    if (orders.Count == 0)
                        return;

                    outgoingQueue.EnqueueStopLimitOrderSnapshot(clientToServerMessage.ClientId, orders);
                    break;

                case ClientToServerMessageTypeEnum.CancelStopLimitOrder:
                    var stopOrderToCancel = ordeRepository.TryGetStopLimitOrder(clientToServerMessage.StopLimitOrder.ExchangeOrderId);

                    if (stopOrderToCancel != null)
                    {
                        stopOrderToCancel.Delete();
                        outgoingQueue.EnqueueDeletedStopLimitOrder(stopOrderToCancel);
                    }
                    break;

                case ClientToServerMessageTypeEnum.PlaceStopLimitOrder:
                    if (!clientToServerMessage.StopLimitOrder.ValidateForAdd())
                        return;

                    var newStopLimitOrder = ordeRepository.NewStopLimitOrder(clientToServerMessage.StopLimitOrder);
                    dispatcher.HandleAddStopLimitOrder(newStopLimitOrder);
                    break;


                case ClientToServerMessageTypeEnum.PlaceMarketOrder:
                    if (!clientToServerMessage.MarketOrder.ValidateForExecute())
                        return;

                    var newMarketOrder = ordeRepository.NewMarketOrder(clientToServerMessage.MarketOrder);
                    dispatcher.HandleMarketOrder(newMarketOrder);
                    break;

                case ClientToServerMessageTypeEnum.PlaceLimitOrder:
                    if (!clientToServerMessage.LimitOrder.ValidatesForAdd())
                    {
                        outgoingQueue.EnqueueMessage(clientToServerMessage.ClientId, "Error: Limit order was rejected.");
                        break;
                    }

                    var newLimitOrder = ordeRepository.NewLimitOrder(clientToServerMessage.LimitOrder);
                    newLimitOrder.RegisterDeleteNotificationHandler(outgoingQueue.EnqueueDeletedLimitOrder);
                    newLimitOrder.RegisterModifyNotificationHandler(outgoingQueue.EnqueueUpdatedLimitOrder);
                    newLimitOrder.RegisterFilledNotification(outgoingQueue.EnqueueDeletedLimitOrder);
                    newLimitOrder.RegisterFilledNotification((order) => order.Delete());

                    dispatcher.HandleAddLimitOrder(newLimitOrder);
                    break;

                case ClientToServerMessageTypeEnum.CancelLimitOrder:
                    if (!clientToServerMessage.LimitOrder.ValidateForDelete())
                    {
                        outgoingQueue.EnqueueMessage(clientToServerMessage.ClientId, "Error: Cancellation of limit order was rejected.");
                        break;
                    }

                    var orderToDelete = ordeRepository.TryGetLimitOrder(clientToServerMessage.LimitOrder.ExchangeOrderId);
                    if (orderToDelete != null)
                    {
                        orderToDelete.Delete();
                        outgoingQueue.EnqueueDeletedLimitOrder(orderToDelete);
                    }
                    break;

                case ClientToServerMessageTypeEnum.ModifyLimitOrder:
                    if (!clientToServerMessage.LimitOrder.ValidatesForModify())
                    {
                        outgoingQueue.EnqueueMessage(clientToServerMessage.ClientId, "Error: Modification of limit order was rejected.");
                        break;
                    }

                    var orderToModify = ordeRepository.TryGetLimitOrder(clientToServerMessage.LimitOrder.ExchangeOrderId);
                    if (orderToModify != null)
                        orderToModify.Modify(clientToServerMessage.LimitOrder.Quantity, clientToServerMessage.LimitOrder.Price);
                    break;

                case ClientToServerMessageTypeEnum.RequestOpenLimitOrders:
                    if (clientToServerMessage.ClientId <= 0)
                        break;

                    var orderList = ordeRepository.GetClientStopLimitOrders(clientToServerMessage.ClientId);
                    outgoingQueue.EnqueueStopLimitOrderSnapshot(clientToServerMessage.ClientId, orderList);
                    break;


                case ClientToServerMessageTypeEnum.DuoLimitOrderUpdate:
                    var order1ToModify = ordeRepository.TryGetLimitOrder(clientToServerMessage.DuoLimitOrder.LimitOrder1.ExchangeOrderId);
                    var order2ToModify = ordeRepository.TryGetLimitOrder(clientToServerMessage.DuoLimitOrder.LimitOrder2.ExchangeOrderId);

                    if (order1ToModify == null || order2ToModify == null)
                        return;

                    if (order1ToModify.Symbol != order2ToModify.Symbol)
                        return;

                    dispatcher.HandDuoLimitOrderUpdate(
                        order1ToModify,
                        clientToServerMessage.DuoLimitOrder.LimitOrder1.Price,
                        clientToServerMessage.DuoLimitOrder.LimitOrder1.Quantity,
                        order2ToModify,
                        clientToServerMessage.DuoLimitOrder.LimitOrder2.Price,
                        clientToServerMessage.DuoLimitOrder.LimitOrder2.Quantity);

                    break;
            }

            clientToServerMessage.Reset();

        }


    }
}