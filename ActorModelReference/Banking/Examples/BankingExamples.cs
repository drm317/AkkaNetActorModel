using Akka.Actor;
using ActorModelReference.Banking.Actors;
using ActorModelReference.Banking.Messages;
using ActorModelReference.Banking.Models;

namespace ActorModelReference.Banking.Examples
{
    public static class BankingExamples
    {
        public static async Task RunBasicBankingExample(ActorSystem actorSystem)
        {
            Console.WriteLine("\n=== Basic Banking Operations Example ===");
            
            var bankingSupervisor = actorSystem.ActorOf(BankingSupervisorActor.Props(), "banking-supervisor");
            
            await bankingSupervisor.Ask<string>("start", TimeSpan.FromSeconds(5));
            Console.WriteLine("Banking system started");

            var createAccount1 = new CreateAccountMessage("John Doe", AccountType.Checking, 1000);
            var account1Response = await bankingSupervisor.Ask<AccountCreatedMessage>(createAccount1, TimeSpan.FromSeconds(5));
            Console.WriteLine($"Created account: {account1Response.AccountNumber} with balance: {account1Response.Balance:C}");

            var createAccount2 = new CreateAccountMessage("Jane Smith", AccountType.Savings, 2000);
            var account2Response = await bankingSupervisor.Ask<AccountCreatedMessage>(createAccount2, TimeSpan.FromSeconds(5));
            Console.WriteLine($"Created account: {account2Response.AccountNumber} with balance: {account2Response.Balance:C}");

            await Task.Delay(1000);

            var deposit = await bankingSupervisor.Ask<object>($"deposit:{account1Response.AccountNumber}:500", TimeSpan.FromSeconds(5));
            Console.WriteLine($"Deposit result: {deposit}");

            var withdraw = await bankingSupervisor.Ask<object>($"withdraw:{account1Response.AccountNumber}:200", TimeSpan.FromSeconds(5));
            Console.WriteLine($"Withdrawal result: {withdraw}");

            var balance = await bankingSupervisor.Ask<object>($"balance:{account1Response.AccountNumber}", TimeSpan.FromSeconds(5));
            Console.WriteLine($"Current balance: {balance}");

            await bankingSupervisor.GracefulStop(TimeSpan.FromSeconds(5));
        }

        public static async Task RunTransferExample(ActorSystem actorSystem)
        {
            Console.WriteLine("\n=== Money Transfer Example ===");
            
            var bankingSupervisor = actorSystem.ActorOf(BankingSupervisorActor.Props(), "banking-supervisor-transfer");
            
            await bankingSupervisor.Ask<string>("start", TimeSpan.FromSeconds(5));

            var account1 = await bankingSupervisor.Ask<AccountCreatedMessage>(
                new CreateAccountMessage("Alice", AccountType.Checking, 5000), TimeSpan.FromSeconds(5));
            
            var account2 = await bankingSupervisor.Ask<AccountCreatedMessage>(
                new CreateAccountMessage("Bob", AccountType.Checking, 1000), TimeSpan.FromSeconds(5));

            Console.WriteLine($"Alice's account: {account1.AccountNumber} with {account1.Balance:C}");
            Console.WriteLine($"Bob's account: {account2.AccountNumber} with {account2.Balance:C}");

            var transfer1 = new TransferMessage(account1.AccountNumber, account2.AccountNumber, 500, "Payment for services");
            var transferResult1 = await bankingSupervisor.Ask<TransferResponseMessage>(transfer1, TimeSpan.FromSeconds(5));
            Console.WriteLine($"Transfer 1: {transferResult1.Success} - {transferResult1.Message}");

            var transfer2 = new TransferMessage(account1.AccountNumber, account2.AccountNumber, 10000, "Large transfer");
            var transferResult2 = await bankingSupervisor.Ask<TransferResponseMessage>(transfer2, TimeSpan.FromSeconds(5));
            Console.WriteLine($"Transfer 2: {transferResult2.Success} - {transferResult2.Message}");

            var aliceBalance = await bankingSupervisor.Ask<object>($"balance:{account1.AccountNumber}", TimeSpan.FromSeconds(5));
            var bobBalance = await bankingSupervisor.Ask<object>($"balance:{account2.AccountNumber}", TimeSpan.FromSeconds(5));
            
            Console.WriteLine($"Final - Alice: {aliceBalance}, Bob: {bobBalance}");

            await bankingSupervisor.GracefulStop(TimeSpan.FromSeconds(5));
        }

        public static async Task RunAtmExample(ActorSystem actorSystem)
        {
            Console.WriteLine("\n=== ATM Operations Example ===");
            
            var bankingSupervisor = actorSystem.ActorOf(BankingSupervisorActor.Props(), "banking-supervisor-atm");
            
            await bankingSupervisor.Ask<string>("start", TimeSpan.FromSeconds(5));

            var account = await bankingSupervisor.Ask<AccountCreatedMessage>(
                new CreateAccountMessage("Customer", AccountType.Checking, 2000), TimeSpan.FromSeconds(5));

            Console.WriteLine($"Created account: {account.AccountNumber}");
            Console.WriteLine("Note: ATM PIN is automatically generated and displayed when account is created");

            await Task.Delay(1000);

            var withdraw = new AtmWithdrawMessage(account.AccountNumber, "1234", 100);
            var withdrawResult = await bankingSupervisor.Ask<AtmResponseMessage>(withdraw, TimeSpan.FromSeconds(10));
            Console.WriteLine($"ATM Withdrawal: {withdrawResult.Success} - {withdrawResult.Message}");
            if (withdrawResult.Success && withdrawResult.Balance.HasValue)
            {
                Console.WriteLine($"Remaining balance: {withdrawResult.Balance:C}");
            }

            var deposit = new AtmDepositMessage(account.AccountNumber, "1234", 300);
            var depositResult = await bankingSupervisor.Ask<AtmResponseMessage>(deposit, TimeSpan.FromSeconds(10));
            Console.WriteLine($"ATM Deposit: {depositResult.Success} - {depositResult.Message}");
            if (depositResult.Success && depositResult.Balance.HasValue)
            {
                Console.WriteLine($"New balance: {depositResult.Balance:C}");
            }

            var balanceInquiry = new AtmBalanceInquiryMessage(account.AccountNumber, "1234");
            var balanceResult = await bankingSupervisor.Ask<AtmResponseMessage>(balanceInquiry, TimeSpan.FromSeconds(10));
            Console.WriteLine($"ATM Balance Inquiry: {balanceResult.Success} - {balanceResult.Message}");
            if (balanceResult.Success && balanceResult.Balance.HasValue)
            {
                Console.WriteLine($"Current balance: {balanceResult.Balance:C}");
            }

            var atmStatus = await bankingSupervisor.Ask<string>("atm:default:status", TimeSpan.FromSeconds(5));
            Console.WriteLine($"ATM Status: {atmStatus}");

            await bankingSupervisor.GracefulStop(TimeSpan.FromSeconds(5));
        }

        public static async Task RunFraudDetectionExample(ActorSystem actorSystem)
        {
            Console.WriteLine("\n=== Fraud Detection Example ===");
            
            var bankingSupervisor = actorSystem.ActorOf(BankingSupervisorActor.Props(), "banking-supervisor-fraud");
            
            await bankingSupervisor.Ask<string>("start", TimeSpan.FromSeconds(5));

            var account1 = await bankingSupervisor.Ask<AccountCreatedMessage>(
                new CreateAccountMessage("Suspicious User", AccountType.Checking, 100000), TimeSpan.FromSeconds(5));

            var account2 = await bankingSupervisor.Ask<AccountCreatedMessage>(
                new CreateAccountMessage("Regular User", AccountType.Checking, 5000), TimeSpan.FromSeconds(5));

            Console.WriteLine("Simulating suspicious transaction patterns...");

            Console.WriteLine("\n1. Large transaction (should trigger fraud alert):");
            var largeTransfer = new TransferMessage(account1.AccountNumber, account2.AccountNumber, 15000, "Large transfer");
            await bankingSupervisor.Ask<TransferResponseMessage>(largeTransfer, TimeSpan.FromSeconds(5));

            Console.WriteLine("\n2. Rapid succession transactions:");
            for (int i = 0; i < 3; i++)
            {
                var rapidTransfer = new TransferMessage(account1.AccountNumber, account2.AccountNumber, 100, $"Rapid transfer {i + 1}");
                await bankingSupervisor.Ask<TransferResponseMessage>(rapidTransfer, TimeSpan.FromSeconds(5));
                await Task.Delay(1000);
            }

            Console.WriteLine("\n3. High frequency transactions:");
            var tasks = new List<Task>();
            for (int i = 0; i < 5; i++)
            {
                var transfer = new TransferMessage(account1.AccountNumber, account2.AccountNumber, 50, $"Frequent transfer {i + 1}");
                tasks.Add(bankingSupervisor.Ask<TransferResponseMessage>(transfer, TimeSpan.FromSeconds(5)));
            }
            await Task.WhenAll(tasks);

            await Task.Delay(2000);

            var status = await bankingSupervisor.Ask<string>("status", TimeSpan.FromSeconds(5));
            Console.WriteLine($"\nSystem status: {status}");

            await bankingSupervisor.GracefulStop(TimeSpan.FromSeconds(5));
        }

        public static async Task RunAccountManagementExample(ActorSystem actorSystem)
        {
            Console.WriteLine("\n=== Account Management Example ===");
            
            var bankingSupervisor = actorSystem.ActorOf(BankingSupervisorActor.Props(), "banking-supervisor-management");
            
            await bankingSupervisor.Ask<string>("start", TimeSpan.FromSeconds(5));

            var checkingAccount = await bankingSupervisor.Ask<AccountCreatedMessage>(
                new CreateAccountMessage("Business Owner", AccountType.Business, 50000), TimeSpan.FromSeconds(5));

            var savingsAccount = await bankingSupervisor.Ask<AccountCreatedMessage>(
                new CreateAccountMessage("Saver", AccountType.Savings, 10000), TimeSpan.FromSeconds(5));

            Console.WriteLine($"Business account: {checkingAccount.AccountNumber}");
            Console.WriteLine($"Savings account: {savingsAccount.AccountNumber}");

            Console.WriteLine("\nFreezing suspicious business account...");
            var freeze = await bankingSupervisor.Ask<string>($"freeze:{checkingAccount.AccountNumber}:Suspicious activity detected", TimeSpan.FromSeconds(5));
            Console.WriteLine($"Freeze result: {freeze}");

            Console.WriteLine("\nAttempting transaction on frozen account...");
            var blockedTransfer = new TransferMessage(checkingAccount.AccountNumber, savingsAccount.AccountNumber, 1000, "Blocked transfer");
            var blockedResult = await bankingSupervisor.Ask<TransferResponseMessage>(blockedTransfer, TimeSpan.FromSeconds(5));
            Console.WriteLine($"Blocked transfer: {blockedResult.Success} - {blockedResult.Message}");

            Console.WriteLine("\nUnfreezing account after investigation...");
            var unfreeze = await bankingSupervisor.Ask<string>($"unfreeze:{checkingAccount.AccountNumber}", TimeSpan.FromSeconds(5));
            Console.WriteLine($"Unfreeze result: {unfreeze}");

            Console.WriteLine("\nRetrying transaction after unfreeze...");
            var successTransfer = new TransferMessage(checkingAccount.AccountNumber, savingsAccount.AccountNumber, 1000, "Successful transfer");
            var successResult = await bankingSupervisor.Ask<TransferResponseMessage>(successTransfer, TimeSpan.FromSeconds(5));
            Console.WriteLine($"Transfer after unfreeze: {successResult.Success} - {successResult.Message}");

            await bankingSupervisor.GracefulStop(TimeSpan.FromSeconds(5));
        }
    }
}