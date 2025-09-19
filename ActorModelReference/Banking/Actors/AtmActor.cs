using Akka.Actor;
using ActorModelReference.Banking.Messages;

namespace ActorModelReference.Banking.Actors
{
    public class AtmActor : ReceiveActor
    {
        private readonly string _atmId;
        private readonly string _location;
        private decimal _cashAvailable;
        private int _transactionCount;
        private IActorRef? _bankRef;

        public AtmActor(string atmId, string location, decimal initialCash = 50000)
        {
            _atmId = atmId;
            _location = location;
            _cashAvailable = initialCash;
            _transactionCount = 0;

            ReceiveMessages();
        }

        private void ReceiveMessages()
        {
            ReceiveAsync<AtmWithdrawMessage>(async withdraw =>
            {
                _transactionCount++;
                Console.WriteLine($"[{Self.Path.Name}] ATM withdrawal request: Account {withdraw.AccountNumber}, Amount: {withdraw.Amount:C}");

                if (_cashAvailable < withdraw.Amount)
                {
                    Console.WriteLine($"[{Self.Path.Name}] Insufficient cash in ATM");
                    Sender.Tell(new AtmResponseMessage(false, "ATM has insufficient cash available"));
                    return;
                }

                if (withdraw.Amount > 1000)
                {
                    Console.WriteLine($"[{Self.Path.Name}] Withdrawal amount exceeds daily limit");
                    Sender.Tell(new AtmResponseMessage(false, "Daily withdrawal limit exceeded"));
                    return;
                }

                try
                {
                    var authResult = await _bankRef.Ask<AuthenticationResponseMessage>(
                        new AuthenticateMessage(withdraw.AccountNumber, withdraw.Pin), 
                        TimeSpan.FromSeconds(5));

                    if (!authResult.Success)
                    {
                        Console.WriteLine($"[{Self.Path.Name}] Authentication failed: {authResult.Message}");
                        Sender.Tell(new AtmResponseMessage(false, authResult.Message));
                        return;
                    }

                    var withdrawResult = await _bankRef.Ask<object>(
                        $"withdraw:{withdraw.AccountNumber}:{withdraw.Amount}", 
                        TimeSpan.FromSeconds(5));

                    if (withdrawResult is BalanceResponseMessage balanceResponse)
                    {
                        _cashAvailable -= withdraw.Amount;
                        Console.WriteLine($"[{Self.Path.Name}] Withdrawal successful. ATM cash remaining: {_cashAvailable:C}");
                        Sender.Tell(new AtmResponseMessage(true, "Withdrawal successful", balanceResponse.Balance));
                    }
                    else
                    {
                        Console.WriteLine($"[{Self.Path.Name}] Withdrawal failed: {withdrawResult}");
                        Sender.Tell(new AtmResponseMessage(false, withdrawResult?.ToString() ?? "Withdrawal failed"));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{Self.Path.Name}] ATM error: {ex.Message}");
                    Sender.Tell(new AtmResponseMessage(false, "ATM temporarily unavailable"));
                }
            });

            ReceiveAsync<AtmDepositMessage>(async deposit =>
            {
                _transactionCount++;
                Console.WriteLine($"[{Self.Path.Name}] ATM deposit request: Account {deposit.AccountNumber}, Amount: {deposit.Amount:C}");

                try
                {
                    var authResult = await _bankRef.Ask<AuthenticationResponseMessage>(
                        new AuthenticateMessage(deposit.AccountNumber, deposit.Pin), 
                        TimeSpan.FromSeconds(5));

                    if (!authResult.Success)
                    {
                        Console.WriteLine($"[{Self.Path.Name}] Authentication failed: {authResult.Message}");
                        Sender.Tell(new AtmResponseMessage(false, authResult.Message));
                        return;
                    }

                    var depositResult = await _bankRef.Ask<object>(
                        $"deposit:{deposit.AccountNumber}:{deposit.Amount}", 
                        TimeSpan.FromSeconds(5));

                    if (depositResult is BalanceResponseMessage balanceResponse)
                    {
                        _cashAvailable += deposit.Amount;
                        Console.WriteLine($"[{Self.Path.Name}] Deposit successful. ATM cash: {_cashAvailable:C}");
                        Sender.Tell(new AtmResponseMessage(true, "Deposit successful", balanceResponse.Balance));
                    }
                    else
                    {
                        Console.WriteLine($"[{Self.Path.Name}] Deposit failed: {depositResult}");
                        Sender.Tell(new AtmResponseMessage(false, depositResult?.ToString() ?? "Deposit failed"));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{Self.Path.Name}] ATM error: {ex.Message}");
                    Sender.Tell(new AtmResponseMessage(false, "ATM temporarily unavailable"));
                }
            });

            ReceiveAsync<AtmBalanceInquiryMessage>(async inquiry =>
            {
                _transactionCount++;
                Console.WriteLine($"[{Self.Path.Name}] ATM balance inquiry: Account {inquiry.AccountNumber}");

                try
                {
                    var authResult = await _bankRef.Ask<AuthenticationResponseMessage>(
                        new AuthenticateMessage(inquiry.AccountNumber, inquiry.Pin), 
                        TimeSpan.FromSeconds(5));

                    if (!authResult.Success)
                    {
                        Console.WriteLine($"[{Self.Path.Name}] Authentication failed: {authResult.Message}");
                        Sender.Tell(new AtmResponseMessage(false, authResult.Message));
                        return;
                    }

                    var balanceResult = await _bankRef.Ask<object>(
                        $"balance:{inquiry.AccountNumber}", 
                        TimeSpan.FromSeconds(5));

                    if (balanceResult is BalanceResponseMessage balanceResponse)
                    {
                        Console.WriteLine($"[{Self.Path.Name}] Balance inquiry successful");
                        Sender.Tell(new AtmResponseMessage(true, "Balance inquiry successful", balanceResponse.Balance));
                    }
                    else
                    {
                        Console.WriteLine($"[{Self.Path.Name}] Balance inquiry failed: {balanceResult}");
                        Sender.Tell(new AtmResponseMessage(false, balanceResult?.ToString() ?? "Balance inquiry failed"));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{Self.Path.Name}] ATM error: {ex.Message}");
                    Sender.Tell(new AtmResponseMessage(false, "ATM temporarily unavailable"));
                }
            });

            Receive<string>(message =>
            {
                if (message == "status")
                {
                    Sender.Tell($"ATM {_atmId} at {_location} - Cash: {_cashAvailable:C}, Transactions: {_transactionCount}");
                }
                else if (message == "refill")
                {
                    _cashAvailable = 50000;
                    Console.WriteLine($"[{Self.Path.Name}] ATM refilled with cash");
                    Sender.Tell($"ATM {_atmId} refilled - Cash available: {_cashAvailable:C}");
                }
                else if (message.StartsWith("refill:"))
                {
                    if (decimal.TryParse(message.Substring(7), out var amount))
                    {
                        _cashAvailable += amount;
                        Console.WriteLine($"[{Self.Path.Name}] ATM refilled with {amount:C}");
                        Sender.Tell($"ATM {_atmId} refilled with {amount:C} - Total: {_cashAvailable:C}");
                    }
                    else
                    {
                        Sender.Tell("Invalid refill amount");
                    }
                }
            });
        }

        protected override void PreStart()
        {
            Console.WriteLine($"[{Self.Path.Name}] ATM {_atmId} started at {_location} with {_cashAvailable:C} cash");
            _bankRef = Context.Parent;
        }

        protected override void PostStop()
        {
            Console.WriteLine($"[{Self.Path.Name}] ATM {_atmId} stopped");
        }

        public static Props Props(string atmId, string location, decimal initialCash = 50000)
            => Akka.Actor.Props.Create(() => new AtmActor(atmId, location, initialCash));
    }
}