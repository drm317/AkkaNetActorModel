using Akka.Actor;
using ActorModelReference.Banking.Messages;
using ActorModelReference.Banking.Models;

namespace ActorModelReference.Banking.Actors
{
    public class BankActor : ReceiveActor
    {
        private readonly Dictionary<string, IActorRef> _accounts;
        private readonly Dictionary<string, AccountInfo> _accountRegistry;
        private int _nextAccountNumber;
        private IActorRef? _fraudDetector;

        public BankActor()
        {
            _accounts = new Dictionary<string, IActorRef>();
            _accountRegistry = new Dictionary<string, AccountInfo>();
            _nextAccountNumber = 1000;

            ReceiveMessages();
        }

        private void ReceiveMessages()
        {
            Receive<CreateAccountMessage>(create =>
            {
                try
                {
                    var accountNumber = GenerateAccountNumber();
                    var accountRef = Context.ActorOf(
                        AccountActor.Props(accountNumber, create.CustomerName, create.AccountType, create.InitialDeposit),
                        $"account-{accountNumber}");

                    _accounts[accountNumber] = accountRef;
                    
                    var accountInfo = new AccountInfo(accountNumber, create.CustomerName, create.InitialDeposit, DateTime.UtcNow);
                    _accountRegistry[accountNumber] = accountInfo;

                    Console.WriteLine($"[{Self.Path.Name}] Created account {accountNumber} for {create.CustomerName}");
                    Sender.Tell(new AccountCreatedMessage(accountNumber, create.InitialDeposit));
                    
                    Context.Watch(accountRef);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{Self.Path.Name}] Failed to create account: {ex.Message}");
                    Sender.Tell(new AccountCreationFailedMessage(ex.Message));
                }
            });

            Receive<TransferMessage>(transfer =>
            {
                if (_accounts.TryGetValue(transfer.FromAccount, out var fromAccount) &&
                    _accounts.TryGetValue(transfer.ToAccount, out var toAccount))
                {
                    Console.WriteLine($"[{Self.Path.Name}] Processing transfer from {transfer.FromAccount} to {transfer.ToAccount}: {transfer.Amount:C}");
                    
                    fromAccount.Tell(transfer, Self);
                    toAccount.Tell(transfer, Self);
                    
                    Context.Parent.Tell(transfer);
                }
                else
                {
                    var message = !_accounts.ContainsKey(transfer.FromAccount) 
                        ? $"Source account {transfer.FromAccount} not found"
                        : $"Destination account {transfer.ToAccount} not found";
                    
                    Console.WriteLine($"[{Self.Path.Name}] Transfer failed: {message}");
                    Sender.Tell(new TransferResponseMessage(false, message));
                }
            });

            Receive<GetAllAccountsMessage>(_ =>
            {
                var accounts = _accountRegistry.Values.ToList();
                Console.WriteLine($"[{Self.Path.Name}] Retrieved {accounts.Count} accounts");
                Sender.Tell(new AllAccountsResponseMessage(accounts));
            });

            Receive<MonitorTransactionMessage>(monitor =>
            {
                _fraudDetector?.Tell(monitor);
            });

            Receive<FraudDetectedMessage>(fraud =>
            {
                Console.WriteLine($"[{Self.Path.Name}] FRAUD DETECTED: {fraud.Alert.Reason} for account {fraud.Alert.AccountNumber}");
                
                if (_accounts.TryGetValue(fraud.Alert.AccountNumber, out var account) && 
                    fraud.Alert.Severity >= FraudSeverity.High)
                {
                    account.Tell(new FreezeAccountMessage($"Fraud detected: {fraud.Alert.Reason}"));
                }
            });

            Receive<string>(message =>
            {
                if (message.StartsWith("deposit:"))
                {
                    var parts = message.Split(':');
                    if (parts.Length == 3 && _accounts.TryGetValue(parts[1], out var account) && decimal.TryParse(parts[2], out var amount))
                    {
                        account.Forward(new DepositMessage(amount));
                    }
                    else
                    {
                        Sender.Tell("Invalid deposit command format: deposit:accountNumber:amount");
                    }
                }
                else if (message.StartsWith("withdraw:"))
                {
                    var parts = message.Split(':');
                    if (parts.Length == 3 && _accounts.TryGetValue(parts[1], out var account) && decimal.TryParse(parts[2], out var amount))
                    {
                        account.Forward(new WithdrawMessage(amount));
                    }
                    else
                    {
                        Sender.Tell("Invalid withdraw command format: withdraw:accountNumber:amount");
                    }
                }
                else if (message.StartsWith("balance:"))
                {
                    var accountNumber = message.Substring(8);
                    if (_accounts.TryGetValue(accountNumber, out var account))
                    {
                        account.Forward(new GetBalanceMessage());
                    }
                    else
                    {
                        Sender.Tell($"Account {accountNumber} not found");
                    }
                }
                else if (message.StartsWith("freeze:"))
                {
                    var parts = message.Split(':', 3);
                    if (parts.Length == 3 && _accounts.TryGetValue(parts[1], out var account))
                    {
                        account.Forward(new FreezeAccountMessage(parts[2]));
                    }
                    else
                    {
                        Sender.Tell("Invalid freeze command format: freeze:accountNumber:reason");
                    }
                }
                else if (message.StartsWith("unfreeze:"))
                {
                    var accountNumber = message.Substring(9);
                    if (_accounts.TryGetValue(accountNumber, out var account))
                    {
                        account.Forward(new UnfreezeAccountMessage());
                    }
                    else
                    {
                        Sender.Tell($"Account {accountNumber} not found");
                    }
                }
            });

            Receive<Terminated>(terminated =>
            {
                var accountToRemove = _accounts.FirstOrDefault(kvp => kvp.Value.Equals(terminated.ActorRef));
                if (!accountToRemove.Equals(default(KeyValuePair<string, IActorRef>)))
                {
                    _accounts.Remove(accountToRemove.Key);
                    _accountRegistry.Remove(accountToRemove.Key);
                    Console.WriteLine($"[{Self.Path.Name}] Account {accountToRemove.Key} terminated and removed");
                }
            });
        }

        private string GenerateAccountNumber()
        {
            return (++_nextAccountNumber).ToString();
        }

        protected override void PreStart()
        {
            Console.WriteLine($"[{Self.Path.Name}] Bank started");
            _fraudDetector = Context.ActorOf(FraudDetectionActor.Props(), "fraud-detector");
        }

        protected override void PostStop()
        {
            Console.WriteLine($"[{Self.Path.Name}] Bank stopped");
        }

        public static Props Props() => Akka.Actor.Props.Create<BankActor>();
    }
}