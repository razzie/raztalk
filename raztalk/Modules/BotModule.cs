using Nancy;

namespace raztalk.Modules
{
    public class BotModule : NancyModule
    {
        public BotModule() : base("bots/")
        {
            Get["available"] = ctx =>
            {
                return BotManager.Available;
            };

            Get["reload"] = ctx =>
            {
                BotManager.Reload();
                return BotManager.Available;
            };
        }
    }
}
