﻿using System;
using MemExchange.Core.Logging;
using MemExchange.Core.Serialization;
using MemExchange.Server.Common;
using MemExchange.Server.Incoming;
using MemExchange.Server.Incoming.Logging;
using MemExchange.Server.Outgoing;
using MemExchange.Server.Processor;
using MemExchange.Server.Processor.Book;

namespace MemExchange.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            string symbol = "ABC";
            
            IConfiguration configuration = new Configuration(symbol);

            ILogger logger = new SerilogLogger();
            logger.Info($"Starting server with symbol '{symbol}'");
            ISerializer serializer = new ProtobufSerializer();
            IMessagePublisher messagePublisher = new MessagePublisher(logger, serializer);
            IOutgoingQueue outgoingQueue = new OutgoingQueue(logger, messagePublisher);

            messagePublisher.Start(9193);
            outgoingQueue.Start();

            IOrderRepository orderRepository = new OrderRepository();
            IOrderDispatcher orderDispatcher = new OrderDispatcher(outgoingQueue, logger, new DateService(), orderRepository, symbol);
            IIncomingMessageProcessor incomingMessageProcessor = new IncomingMessageProcessor(orderRepository, outgoingQueue, new DateService(), orderDispatcher , new ProtobufSerializer(), configuration);
            IPerformanceRecorder performanceRecorder = new PerformanceRecorderDirectConsoleOutput(new DateService());
            IIncomingMessageQueue incomingMessageQueue = new IncomingMessageQueue(logger, incomingMessageProcessor, performanceRecorder);
            IClientMessagePuller clientMessagePuller = new ClientMessagePuller(logger, new ProtobufSerializer(), incomingMessageQueue);

            incomingMessageQueue.Start();
            clientMessagePuller.Start(9192);

            Console.WriteLine("Started. Hit any key to quit.");
            Console.ReadKey();

            Console.WriteLine("Stopping...");

            clientMessagePuller.Stop();
            incomingMessageQueue.Stop();
            outgoingQueue.Stop();
            messagePublisher.Stop();

            Console.WriteLine("Stopped");
        }
    }
}
