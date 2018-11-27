namespace MemExchange.Server.Common
{
    public class Configuration : IConfiguration
    {
        public string MarketSymbol { get; private set; }

        public Configuration(string marketSymbol)
        {
            MarketSymbol = marketSymbol.ToUpper();

        }

    }
}