using Akka.Actor;
using DistributedSystems_1.Actors;
using DistributedSystems_1.Logging;

namespace DistributedSystems_1
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var (bank, shop, man) = CreateActors();
            
            bank.Tell(new SettingsMessage(new [] {shop, man}));
            shop.Tell(new SettingsMessage(new [] {bank, man}));
            man.Tell(new SettingsMessage(new [] {bank, shop}));
        }

        private static (IActorRef, IActorRef, IActorRef) CreateActors()
        {
            var system = ActorSystem.Create("DistributedService");
            var logger = new SerilogFileLogger();

            var loggerProps = Props.Create(() => new LoggingActor(logger));
            system.ActorOf(loggerProps,"Logger");

            var bankProps = Props.Create(() => new Actors.BankActor(100));
            var shopProps = Props.Create(() => new Actors.ShopActor(0));
            var guyProps  = Props.Create(() => new Actors.UserActor(100));

            var bankActor = system.ActorOf(bankProps, "bank");
            var shopActor = system.ActorOf(shopProps, "shop");
            var guyActor  = system.ActorOf(guyProps, "guy");

            return (bankActor, shopActor, guyActor);
        }
    }
}
