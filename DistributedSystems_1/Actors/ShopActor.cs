using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using DistributedSystems_1.Priority_queue;

namespace DistributedSystems_1.Actors
{
    internal enum State
    {
        Woking,
        Finishing
    }

    public class ShopActor : UntypedActor
    {
        private const int PurchaseDuration = 2;

        private readonly ICanTell _logger;
        private readonly Dictionary<long, InputQueue<int>> _inputQueues;
        private readonly PriorityQeue<SheduledTask> _innerTasks;

        private int _lbts;
        private int _currentTime;
        private decimal _amountOfMoney;
        private State _state;

        //Минимальное время, с которым могут запланировать следующее событие
        private int MinNextSheduleTime => _currentTime + PurchaseDuration;

        public ShopActor(decimal amountOfMoney)
        {
            _amountOfMoney = amountOfMoney;
            _currentTime = 0;
            _lbts = 0;
            _state = State.Woking;

            _inputQueues = new Dictionary<long, InputQueue<int>>();
            _innerTasks = new PriorityQeue<SheduledTask>();

            _logger = Context.ActorSelection(LoggingActor.Path);

        }

        private void Start(IEnumerable<IActorRef> actors)
        {
            foreach (var actor in actors)
            {
                _inputQueues.Add(actor.Path.Uid, new InputQueue<int>(actor));
            }
            
            SendNullMsgBroadcast(_currentTime);
        }

        protected override void OnReceive(object message)
        {
            if (message is SettingsMessage settings)
            {
                Start(settings.Actors);
            }
            if (message is Message<int> msg)
            {

                if (!_inputQueues.ContainsKey(Sender.Path.Uid))
                {
                    Log($"received message from unlnown sender: {Sender.Path.Name}");
                }

                _inputQueues[Sender.Path.Uid].Enqueue(msg);
                Log($"received message from {Sender.Path.Name}");
                ProcessMessages();
            }
            else
            {
                Log("incorrect msg received");
            }
        }


        private void ProcessMessages()
        {
            if (_inputQueues.Any(dic => dic.Value.Epmty()))
            {
                return;
            }

            var actorsArr = _inputQueues.Values.ToArray();
            Array.Sort(actorsArr, (a, b) => a.Time.CompareTo(b.Time));

            var time = actorsArr[0].Time;
            if (time > _lbts)
            {
                _lbts = time;
            }

            HandleSheduledTasks();
            IncreaseLocalTime();

            var msg = actorsArr[0].Dequeue();
            var sender = actorsArr[0].SenderActor;

            switch (msg)
            {
                case TransactionResponse<int> responseMsg:
                    ProcessTransactionResponse(responseMsg, sender);
                    break;
                case NullMessage<int> nullMsg:
                    ProcessNullMessage(nullMsg, sender);
                    break;
                case DoneMessage<int> doneMsg:
                    ProcessDoneMessage(doneMsg, sender);
                    break;
                default:
                    Log("can't cast message");
                    break;
            }
        }

        private void HandleSheduledTasks()
        {
            if (_currentTime > _lbts || _innerTasks.IsEmpty())
            {
                return;
            }

            while (_innerTasks.Peek().SheduledTime == _lbts)
            {
                var task = _innerTasks.Dequeue();
                task.Action();
            }

            if (_innerTasks.IsEmpty() && _state == State.Finishing)
            {
                SendDoneBroadcast();
            }
        }

        private void IncreaseLocalTime()
        {
            _currentTime = !_innerTasks.IsEmpty()
                ? Math.Min(_innerTasks.Peek().SheduledTime, MinNextSheduleTime)
                : MinNextSheduleTime;

            SendNullMsgBroadcast(_currentTime);
        }

        #region Actions

        private void MakeWithdrawRequest(int amountOfMoney, IActorRef recepient)
        {
            recepient.Tell(new BankActor.WithdrawMoney<int>(amountOfMoney, _currentTime));
            Log($"Withdraw request sent to {recepient.Path.Name}");
        }

        private void SendDoneBroadcast()
        {
            var msg = new DoneMessage<int>(_currentTime);
            Parallel.ForEach(_inputQueues.Values, node =>
            {
                node.SenderActor.Tell(msg, Self);
                Log($"Actor sent Done to {node.SenderActor.Path.Name} at {_currentTime}");
            });
        }

        private void SendNullMsgBroadcast(int time)
        {
            var msg = new NullMessage<int>(time);
            Parallel.ForEach(_inputQueues.Values, node =>
            {
                node.SenderActor.Tell(msg, Self);
                Log($"Sent message to {node.SenderActor.Path.Name} with time {time}");
            });
        }

        #endregion

        #region ProcessMessages

        private static void ProcessNullMessage(NullMessage<int> message, IActorRef sender)
        {
        }

        private void ProcessDoneMessage(DoneMessage<int> doneMsg, IActorRef sender)
        {
            if (_innerTasks.IsEmpty() && _state == State.Woking)
            {
                SendDoneBroadcast();
            }

            _state = State.Finishing;

            if (_inputQueues.ContainsKey(sender.Path.Uid))
            {
                _inputQueues.Remove(sender.Path.Uid);
                Log($"Done from {sender.Path.Name} received");
            }
            else
            {
                Log($"Error: Done from finished actor {sender.Path.Name} received");
            }

            if (_inputQueues.Count != 0)
            {
                return;
            }

            FinishWork();
        }

        private void FinishWork()
        {
            Log($"Actor is finishing at {_currentTime} with money: {_amountOfMoney}, with lbts: {_lbts}");
            Context.Stop(Self);
        }

        private void ProcessTransactionResponse(TransactionResponse<int> message, IActorRef sender)
        {
            if (message.Success)
            {
                _amountOfMoney += message.Amount;
                Log($"Actor has got P{message.Amount} money from {sender.Path.Name}");
            }

            Log($"Actor hasn't got any money from {sender.Path.Name}");
        }

        #endregion

        private void Log(string text)
        {
            var logMsg = new LoggingActor.Log(text);
            _logger.Tell(logMsg, Self);
        }

        public class BuyStuff<T> : Message<T>
        {
            public BuyStuff(decimal cash, T time) : base(time)
            {
                Cash = cash;
            }

            public decimal Cash { get; }
        }
    }
}