using MemExchange.Core.Logging;
using MemExchange.Server.Common;
using MemExchange.Server.Outgoing;
using MemExchange.Server.Processor.Book;
using MemExchange.Server.Processor.Book.MatchingAlgorithms;
using MemExchange.Server.Processor.Book.Orders;

namespace MemExchange.Server.Processor
{
    public class OrderDispatcher : IOrderDispatcher
    {
        private readonly IOutgoingQueue outgoingQueue;
        private readonly ILogger logger;
        private readonly IDateService dateService;
        private readonly IOrderRepository orderRepository;
        private readonly string symbol;
        public IOrderBook OrderBook { get; set; }
        
        public OrderDispatcher(IOutgoingQueue outgoingQueue, ILogger logger, IDateService dateService, IOrderRepository orderRepository, string symbol)
        {
            this.outgoingQueue = outgoingQueue;
            this.logger = logger;
            this.dateService = dateService;
            this.orderRepository = orderRepository;
            this.symbol = symbol.ToUpper();

            var bookMatchingLimitAlgo = new LimitOrderMatchingAlgorithm(dateService);
            bookMatchingLimitAlgo.AddExecutionsHandler(outgoingQueue.EnqueueClientExecution);

            var bookMatchingMarketAlgo = new MarketOrderMatchingAlgorithm(dateService);
            bookMatchingMarketAlgo.AddExecutionsHandler(outgoingQueue.EnqueueClientExecution);

            var level1 = new OrderBookBestBidAsk(symbol);
            level1.RegisterUpdateHandler(outgoingQueue.EnqueueLevel1Update);

            OrderBook= new OrderBook(symbol, bookMatchingLimitAlgo, bookMatchingMarketAlgo, level1);

            
        }

        public void HandleMarketOrder(IMarketOrder marketOrder)
        {
            string symbol = marketOrder.Symbol;
            if (this.symbol != symbol.ToUpper())
                return;

            OrderBook.HandleMarketOrder(marketOrder);
        }

        public void HandleAddStopLimitOrder(IStopLimitOrder stopLimitOrder)
        {
            string symbol = stopLimitOrder.Symbol;
            if (this.symbol != symbol.ToUpper())
                return;

            stopLimitOrder.RegisterOutgoingQueueDeleteHandler(outgoingQueue.EnqueueDeletedStopLimitOrder);
            outgoingQueue.EnqueueAddedStopLimitOrder(stopLimitOrder);
            stopLimitOrder.Trigger.SetTriggerAction(() =>
            {
                stopLimitOrder.Delete();
                var newLimitOrder = orderRepository.NewLimitOrder(stopLimitOrder);
                newLimitOrder.RegisterDeleteNotificationHandler(outgoingQueue.EnqueueDeletedLimitOrder);
                newLimitOrder.RegisterModifyNotificationHandler(outgoingQueue.EnqueueUpdatedLimitOrder);
                newLimitOrder.RegisterFilledNotification(outgoingQueue.EnqueueDeletedLimitOrder);
                newLimitOrder.RegisterFilledNotification((order) => order.Delete());
                HandleAddLimitOrder(newLimitOrder);
            });

            OrderBook.AddStopLimitOrder(stopLimitOrder);
        }

        public void HandleAddLimitOrder(ILimitOrder limitOrder)
        {
            string symbol = limitOrder.Symbol;
            if (this.symbol != symbol.ToUpper())
                return;

            outgoingQueue.EnqueueAddedLimitOrder(limitOrder);
            limitOrder.RegisterDeleteNotificationHandler(OrderBook.RemoveLimitOrder);
            limitOrder.RegisterFilledNotification(OrderBook.RemoveLimitOrder);
            limitOrder.RegisterModifyNotificationHandler(OrderBook.HandleLimitOrderModify);
            
            OrderBook.AddLimitOrder(limitOrder);
        }

        public void HandDuoLimitOrderUpdate(ILimitOrder limitOrder1, double limitOrder1NewPrice, int limitOrder1NewQuantity, ILimitOrder limitOrder2, double limitOrder2NewPrice, int limitOrder2NewQuantity)
        {
            if (this.symbol != symbol.ToUpper())
                return;

            OrderBook.SetSuspendLimitOrderMatchingStatus(true);

            limitOrder1.Modify(limitOrder1NewQuantity, limitOrder1NewPrice);
            limitOrder2.Modify(limitOrder2NewQuantity, limitOrder2NewPrice);

            OrderBook.SetSuspendLimitOrderMatchingStatus(false);
            OrderBook.TryMatchLimitOrder(limitOrder1);
            OrderBook.TryMatchLimitOrder(limitOrder2);
        }
    }
}