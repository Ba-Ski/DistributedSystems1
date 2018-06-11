using System;
using System.Collections.Generic;
using Akka.Actor;
using DistributedSystems_1.Actors;

namespace DistributedSystems_1
{
    internal class InputQueue<T> where T : IComparable
    {
        public InputQueue(IActorRef senderActor)
        {
            SenderActor = senderActor;
            _inputMessages = new Queue<Message<T>>();
            Time = default(T);
        }

        private readonly Queue<Message<T>> _inputMessages;
        public IActorRef SenderActor { get; }

        public T Time { get; private set; }

        public void Enqueue(Message<T> message)
        {
            _inputMessages.Enqueue(message);
            Time = _inputMessages.Peek().Time;
        }

        public Message<T> Dequeue()
        {
            var res = _inputMessages.Dequeue();
            Time = _inputMessages.Peek().Time;
            return res;
        }

        public bool Epmty() => _inputMessages.Count == 0;
    }
}
