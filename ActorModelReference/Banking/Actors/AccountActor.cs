using Akka.Actor;
using ActorModelReference.Banking.Messages;
using ActorModelReference.Banking.Models;

namespace ActorModelReference.Banking.Actors
{
    public class AccountActor : ReceiveActor
    {
        private readonly string _accountNumber;
        private readonly string _customerName;
        private readonly AccountType _accountType;
        private decimal _balance;
        private bool _isFrozen;
        private string? _freezeReason;
        private readonly DateTime _createdAt;
        private readonly List<TransactionInfo> _transactionHistory;
        private readonly string _pin;

        public AccountActor(string accountNumber, string customerName, AccountType accountType, decimal initialBalance)
        {
            _accountNumber = accountNumber;
            _customerName = customerName;
            _accountType = accountType;
            _balance = initialBalance;
            _isFrozen = false;
            _createdAt = DateTime.UtcNow;
            _transactionHistory = new List<TransactionInfo>();
            _pin = GeneratePin();

            ReceiveMessages();
        }

        private void ReceiveMessages()
        {
            Receive<DepositMessage>(deposit =>
            {
                if (_isFrozen)
                {
                    Sender.Tell($"Account {_accountNumber} is frozen: {_freezeReason}");
                    return;
                }

                _balance += deposit.Amount;
                var transaction = new TransactionInfo(
                    Guid.NewGuid().ToString(),
                    "EXTERNAL",
                    _accountNumber,
                    deposit.Amount,
                    deposit.Description,
                    DateTime.UtcNow,
                    TransactionStatus.Completed);
                
                _transactionHistory.Add(transaction);
                
                Console.WriteLine($"[{Self.Path.Name}] Deposited {deposit.Amount:C} - New balance: {_balance:C}");
                Sender.Tell(new BalanceResponseMessage(_balance));
            });

            Receive<WithdrawMessage>(withdraw =>
            {
                if (_isFrozen)
                {
                    Sender.Tell($"Account {_accountNumber} is frozen: {_freezeReason}");
                    return;
                }

                if (_balance >= withdraw.Amount)
                {
                    _balance -= withdraw.Amount;
                    var transaction = new TransactionInfo(
                        Guid.NewGuid().ToString(),
                        _accountNumber,
                        "EXTERNAL",
                        withdraw.Amount,
                        withdraw.Description,
                        DateTime.UtcNow,
                        TransactionStatus.Completed);
                    
                    _transactionHistory.Add(transaction);
                    
                    Console.WriteLine($"[{Self.Path.Name}] Withdrew {withdraw.Amount:C} - New balance: {_balance:C}");
                    Sender.Tell(new BalanceResponseMessage(_balance));
                }
                else
                {
                    Console.WriteLine($"[{Self.Path.Name}] Insufficient funds for withdrawal of {withdraw.Amount:C}");
                    Sender.Tell("Insufficient funds");
                }
            });

            Receive<GetBalanceMessage>(_ =>
            {
                Console.WriteLine($"[{Self.Path.Name}] Balance inquiry: {_balance:C}");
                Sender.Tell(new BalanceResponseMessage(_balance));
            });

            Receive<GetAccountInfoMessage>(_ =>
            {
                var accountInfo = new AccountInfo(_accountNumber, _customerName, _balance, _createdAt);
                Sender.Tell(new AccountInfoResponseMessage(accountInfo));
            });

            Receive<TransferMessage>(transfer =>
            {
                if (_isFrozen)
                {
                    Sender.Tell(new TransferResponseMessage(false, $"Account {_accountNumber} is frozen: {_freezeReason}"));
                    return;
                }

                if (transfer.FromAccount == _accountNumber)
                {
                    if (_balance >= transfer.Amount)
                    {
                        _balance -= transfer.Amount;
                        var transaction = new TransactionInfo(
                            Guid.NewGuid().ToString(),
                            transfer.FromAccount,
                            transfer.ToAccount,
                            transfer.Amount,
                            transfer.Description,
                            DateTime.UtcNow,
                            TransactionStatus.Completed);
                        
                        _transactionHistory.Add(transaction);
                        
                        Console.WriteLine($"[{Self.Path.Name}] Transfer out {transfer.Amount:C} to {transfer.ToAccount} - New balance: {_balance:C}");
                        Sender.Tell(new TransferResponseMessage(true, "Transfer successful", transaction.TransactionId));
                        
                        Context.Parent.Tell(new MonitorTransactionMessage(transaction));
                    }
                    else
                    {
                        Console.WriteLine($"[{Self.Path.Name}] Insufficient funds for transfer of {transfer.Amount:C}");
                        Sender.Tell(new TransferResponseMessage(false, "Insufficient funds"));
                    }
                }
                else if (transfer.ToAccount == _accountNumber)
                {
                    _balance += transfer.Amount;
                    var transaction = new TransactionInfo(
                        Guid.NewGuid().ToString(),
                        transfer.FromAccount,
                        transfer.ToAccount,
                        transfer.Amount,
                        transfer.Description,
                        DateTime.UtcNow,
                        TransactionStatus.Completed);
                    
                    _transactionHistory.Add(transaction);
                    
                    Console.WriteLine($"[{Self.Path.Name}] Transfer in {transfer.Amount:C} from {transfer.FromAccount} - New balance: {_balance:C}");
                    Sender.Tell(new TransferResponseMessage(true, "Transfer received", transaction.TransactionId));
                }
            });

            Receive<FreezeAccountMessage>(freeze =>
            {
                _isFrozen = true;
                _freezeReason = freeze.Reason;
                Console.WriteLine($"[{Self.Path.Name}] Account frozen: {freeze.Reason}");
                Sender.Tell(new AccountFrozenMessage(freeze.Reason));
            });

            Receive<UnfreezeAccountMessage>(_ =>
            {
                _isFrozen = false;
                _freezeReason = null;
                Console.WriteLine($"[{Self.Path.Name}] Account unfrozen");
                Sender.Tell(new AccountUnfrozenMessage());
            });

            Receive<AuthenticateMessage>(auth =>
            {
                var success = auth.AccountNumber == _accountNumber && auth.Pin == _pin;
                var message = success ? "Authentication successful" : "Invalid account number or PIN";
                Sender.Tell(new AuthenticationResponseMessage(success, message));
            });

            Receive<GetTransactionHistoryMessage>(_ =>
            {
                Sender.Tell(new TransactionHistoryResponseMessage(_transactionHistory.ToList()));
            });
        }

        private static string GeneratePin()
        {
            var random = new Random();
            return random.Next(1000, 9999).ToString();
        }

        protected override void PreStart()
        {
            Console.WriteLine($"[{Self.Path.Name}] Account created for {_customerName} with balance {_balance:C} (PIN: {_pin})");
        }

        public static Props Props(string accountNumber, string customerName, AccountType accountType, decimal initialBalance)
            => Akka.Actor.Props.Create(() => new AccountActor(accountNumber, customerName, accountType, initialBalance));
    }
}