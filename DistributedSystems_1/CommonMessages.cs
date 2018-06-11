using System.Collections.Generic;
using Akka.Actor;

namespace DistributedSystems_1.Actors
{
    public class Message<T>
    {
        public Message(T time)
        {
            Time = time;
        }

        public T Time { get; private set; }
    }

    public class NullMessage<T> : Message<T>
    {
        public NullMessage(T time) : base(time)
        {
        }
    }

    public class DoneMessage<T> : Message<T>
    {
        public DoneMessage(T time) : base(time)
        {
        }
    }

    //TODO: add scenatio to message.
    public class SettingsMessage
    {
        public SettingsMessage(IEnumerable<IActorRef> actors)
        {
            Actors = actors;
        }

        public IEnumerable<IActorRef> Actors { get; private set; }
    }

    public class TransactionResponse<T> : Message<T>
    {
        public TransactionResponse(bool success, decimal amount, T time) : base(time)
        {
            Success = success;
            Amount = amount;
        }

        public bool Success { get; }
        public decimal Amount { get; }
    }
}