using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using DistributedSystems_1.Priority_queue;

namespace DistributedSystems_1.Actors
{

    public class BankActor : UntypedActor
    {
        private const int DepositDuration = 1;
        private const int WithdrawDuration = 1;

        private readonly ICanTell _logger;
        private readonly Dictionary<long, InputQueue<int>> _inputQueues;
        private readonly PriorityQeue<SheduledTask> _innerTasks;

        private int _lbts;
        private int _currentTime;
        private decimal _amountOfMoney;
        private State _state;

        //Минимальное время, с которым могут запланировать следующее событие
        private int MinNextSheduleTime => _currentTime + Math.Min(WithdrawDuration, DepositDuration);

        public BankActor(decimal amountOfMoney)
        {
            _amountOfMoney = amountOfMoney;
            _currentTime = 0;
            _lbts = 0;
            _state = State.Woking;

            _innerTasks = new PriorityQeue<SheduledTask>();
            _inputQueues = new Dictionary<long, InputQueue<int>>();

            _logger = Context.ActorSelection(LoggingActor.Path);
        }

        public void Start(IEnumerable<IActorRef> actors)
        {
            foreach (var actor in actors)
            {
                _inputQueues.Add(actor.Path.Uid, new InputQueue<int>(actor));
            }

            SendNullMsgBroadcast();
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
                case DepositMoney<int> withdrawMsg:
                    SheduleDeposit(withdrawMsg, sender);
                    break;
                case WithdrawMoney<int> depositMsg:
                    SheduleWithdraw(depositMsg, sender);
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
                FinishWork();
            }
        }

        private void IncreaseLocalTime()
        {
            _currentTime = !_innerTasks.IsEmpty()
                ? Math.Min(_innerTasks.Peek().SheduledTime, MinNextSheduleTime)
                : MinNextSheduleTime;

            SendNullMsgBroadcast();
        }


        #region Actions

        private void SendNullMsgBroadcast()
        {
            var msg = new NullMessage<int>(_currentTime);
            Parallel.ForEach(_inputQueues.Values, node =>
            {
                node.SenderActor.Tell(msg, Self);
                Log($"Sent message to {node.SenderActor.Path.Name} with time {_currentTime}");
            });
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

        private void ApplyWithdraw(WithdrawMoney<int> message, IActorRef sender)
        {
            decimal amountToSend = 0;

            if (message.Amount > _amountOfMoney)
            {
                sender.Tell(new TransactionResponse<int>(false, amountToSend, _currentTime));
                Log($"Withdraw from {sender.Path.Name} declined");
                return;
            }

            _amountOfMoney -= message.Amount;

            amountToSend = message.Amount;

            sender.Tell(new TransactionResponse<int>(true, amountToSend, _currentTime));
            Log($"Withdraw from {sender.Path.Name} applied");
        }

        private void ApplyDeposit(DepositMoney<int> message, IActorRef sender)
        {
            _amountOfMoney += message.Amount;

            Log($"Deposit from {sender.Path.Name} applied");
        }

        #endregion

        #region ProcessMessages

        private static void ProcessNullMessage(NullMessage<int> message, IActorRef sender)
        {
        }

        private void ProcessDoneMessage(DoneMessage<int> doneMsg, IActorRef sender)
        {
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

            if (_innerTasks.IsEmpty())
            {
                FinishWork();
            }
        }

        private void FinishWork()
        {
            SendDoneBroadcast();
            Log($"Actor is finishing at {_currentTime} with money: {_amountOfMoney}, with lbts: {_lbts}");
            Context.Stop(Self);
        }

        private void SheduleWithdraw(WithdrawMoney<int> message, IActorRef sender)
        {

            var time = _currentTime + WithdrawDuration;

            _innerTasks.Enqueue(new SheduledTask
            {
                Action = () => ApplyWithdraw(message, sender),
                SheduledTime = time
            });

            Log($"Withdraw from {sender.Path.Name} sheduled at {time}");
        }



        private void SheduleDeposit(DepositMoney<int> message, IActorRef sender)
        {

            var time = _currentTime + DepositDuration;
            _innerTasks.Enqueue(new SheduledTask
            {
                Action = () => ApplyDeposit(message, sender),
                SheduledTime = time
            });

            Log($"Deposit from {sender.Path.Name} sheduled at {time}");
        }

        #endregion


        private void Log(string text)
        {
            var logMsg = new LoggingActor.Log(text);
            _logger.Tell(logMsg, Self);
        }


        #region Messages

        public class DepositMoney<T> : Message<T>
        {
            public DepositMoney(decimal amount, T time) : base(time)
            {
                Amount = amount;
            }

            public decimal Amount { get; }
        }

        public class WithdrawMoney<T> : Message<T>
        {
            public WithdrawMoney(decimal amount, T time) : base(time)
            {
                Amount = amount;
            }

            public decimal Amount { get; }
        }

        #endregion
    }
}