using Akka.Actor;
using ActorModelReference.Banking.Messages;

namespace ActorModelReference.Banking.Actors
{
    public class BankingSupervisorActor : ReceiveActor
    {
        private IActorRef? _bankActor;
        private IActorRef? _transactionActor;
        private readonly Dictionary<string, IActorRef> _atmActors;
        private int _restartCount;

        public BankingSupervisorActor()
        {
            _atmActors = new Dictionary<string, IActorRef>();
            _restartCount = 0;

            ReceiveMessages();
        }

        private void ReceiveMessages()
        {
            Receive<string>(message =>
            {
                if (message == "start")
                {
                    StartBankingSystem();
                    Sender.Tell("Banking system started");
                }
                else if (message == "stop")
                {
                    StopBankingSystem();
                    Sender.Tell("Banking system stopped");
                }
                else if (message == "status")
                {
                    var bankStatus = _bankActor != null ? "Running" : "Stopped";
                    var transactionStatus = _transactionActor != null ? "Running" : "Stopped";
                    var atmCount = _atmActors.Count;
                    
                    Sender.Tell($"Banking System Status - Bank: {bankStatus}, Transactions: {transactionStatus}, ATMs: {atmCount}, Restarts: {_restartCount}");
                }
                else if (message.StartsWith("create-atm:"))
                {
                    var parts = message.Split(':');
                    if (parts.Length >= 3)
                    {
                        var atmId = parts[1];
                        var location = parts[2];
                        var initialCash = parts.Length > 3 && decimal.TryParse(parts[3], out var cash) ? cash : 50000;
                        
                        CreateAtm(atmId, location, initialCash);
                        Sender.Tell($"ATM {atmId} created at {location}");
                    }
                    else
                    {
                        Sender.Tell("Invalid ATM creation format: create-atm:id:location[:initialCash]");
                    }
                }
                else if (message.StartsWith("atm:"))
                {
                    var parts = message.Split(':', 3);
                    if (parts.Length == 3 && _atmActors.TryGetValue(parts[1], out var atm))
                    {
                        atm.Forward(parts[2]);
                    }
                    else
                    {
                        Sender.Tell($"ATM {parts[1]} not found");
                    }
                }
                else
                {
                    _bankActor?.Forward(message);
                }
            });

            Receive<CreateAccountMessage>(create =>
            {
                _bankActor?.Forward(create);
            });

            Receive<TransferMessage>(transfer =>
            {
                _transactionActor?.Forward(transfer);
            });

            Receive<AtmWithdrawMessage>(withdraw =>
            {
                if (_atmActors.TryGetValue("default", out var atm))
                {
                    atm.Forward(withdraw);
                }
                else
                {
                    Sender.Tell(new AtmResponseMessage(false, "No ATM available"));
                }
            });

            Receive<AtmDepositMessage>(deposit =>
            {
                if (_atmActors.TryGetValue("default", out var atm))
                {
                    atm.Forward(deposit);
                }
                else
                {
                    Sender.Tell(new AtmResponseMessage(false, "No ATM available"));
                }
            });

            Receive<AtmBalanceInquiryMessage>(inquiry =>
            {
                if (_atmActors.TryGetValue("default", out var atm))
                {
                    atm.Forward(inquiry);
                }
                else
                {
                    Sender.Tell(new AtmResponseMessage(false, "No ATM available"));
                }
            });

            Receive<Terminated>(terminated =>
            {
                if (terminated.ActorRef.Equals(_bankActor))
                {
                    _restartCount++;
                    Console.WriteLine($"[{Self.Path.Name}] Bank actor terminated, restarting... (restart #{_restartCount})");
                    
                    if (_restartCount < 3)
                    {
                        _bankActor = Context.ActorOf(BankActor.Props(), $"bank-{_restartCount}");
                        Context.Watch(_bankActor);
                    }
                    else
                    {
                        Console.WriteLine($"[{Self.Path.Name}] Max restarts reached for bank actor");
                        _bankActor = null;
                    }
                }
                else if (terminated.ActorRef.Equals(_transactionActor))
                {
                    Console.WriteLine($"[{Self.Path.Name}] Transaction actor terminated, restarting...");
                    _transactionActor = Context.ActorOf(TransactionActor.Props(), "transaction-processor");
                    Context.Watch(_transactionActor);
                }
                else
                {
                    var terminatedAtm = _atmActors.FirstOrDefault(kvp => kvp.Value.Equals(terminated.ActorRef));
                    if (!terminatedAtm.Equals(default(KeyValuePair<string, IActorRef>)))
                    {
                        Console.WriteLine($"[{Self.Path.Name}] ATM {terminatedAtm.Key} terminated");
                        _atmActors.Remove(terminatedAtm.Key);
                    }
                }
            });
        }

        private void StartBankingSystem()
        {
            if (_bankActor == null)
            {
                _bankActor = Context.ActorOf(BankActor.Props(), "bank");
                Context.Watch(_bankActor);
                Console.WriteLine($"[{Self.Path.Name}] Bank actor started");
            }

            if (_transactionActor == null)
            {
                _transactionActor = Context.ActorOf(TransactionActor.Props(), "transaction-processor");
                Context.Watch(_transactionActor);
                Console.WriteLine($"[{Self.Path.Name}] Transaction processor started");
            }

            CreateAtm("default", "Main Branch", 100000);
        }

        private void StopBankingSystem()
        {
            _bankActor?.Tell(PoisonPill.Instance);
            _transactionActor?.Tell(PoisonPill.Instance);
            
            foreach (var atm in _atmActors.Values)
            {
                atm.Tell(PoisonPill.Instance);
            }
            
            _atmActors.Clear();
            _bankActor = null;
            _transactionActor = null;
            
            Console.WriteLine($"[{Self.Path.Name}] Banking system stopped");
        }

        private void CreateAtm(string atmId, string location, decimal initialCash)
        {
            if (!_atmActors.ContainsKey(atmId))
            {
                var atm = Context.ActorOf(AtmActor.Props(atmId, location, initialCash), $"atm-{atmId}");
                _atmActors[atmId] = atm;
                Context.Watch(atm);
                Console.WriteLine($"[{Self.Path.Name}] ATM {atmId} created at {location}");
            }
        }

        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new OneForOneStrategy(
                maxNrOfRetries: 3,
                withinTimeRange: TimeSpan.FromMinutes(1),
                decider: Decider.From(exception => exception switch
                {
                    ArgumentException => Directive.Restart,
                    InvalidOperationException => Directive.Restart,
                    TimeoutException => Directive.Restart,
                    _ => Directive.Escalate
                }));
        }

        protected override void PreStart()
        {
            Console.WriteLine($"[{Self.Path.Name}] Banking supervisor started");
        }

        protected override void PostStop()
        {
            Console.WriteLine($"[{Self.Path.Name}] Banking supervisor stopped");
        }

        public static Props Props() => Akka.Actor.Props.Create<BankingSupervisorActor>();
    }
}