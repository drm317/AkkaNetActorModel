using Akka.Actor;
using ActorModelReference.Banking.Messages;
using ActorModelReference.Banking.Models;

namespace ActorModelReference.Banking.Actors
{
    public class TransactionActor : ReceiveActor
    {
        private readonly Dictionary<string, TransactionInfo> _transactions;
        private readonly Dictionary<string, IActorRef> _pendingTransactions;

        public TransactionActor()
        {
            _transactions = new Dictionary<string, TransactionInfo>();
            _pendingTransactions = new Dictionary<string, IActorRef>();

            ReceiveMessages();
        }

        private void ReceiveMessages()
        {
            Receive<TransferMessage>(transfer =>
            {
                var transactionId = Guid.NewGuid().ToString();
                var transaction = new TransactionInfo(
                    transactionId,
                    transfer.FromAccount,
                    transfer.ToAccount,
                    transfer.Amount,
                    transfer.Description,
                    DateTime.UtcNow,
                    TransactionStatus.Pending);

                _transactions[transactionId] = transaction;
                _pendingTransactions[transactionId] = Sender;

                Console.WriteLine($"[{Self.Path.Name}] Processing transaction {transactionId}: {transfer.Amount:C} from {transfer.FromAccount} to {transfer.ToAccount}");

                Context.Parent.Tell(transfer);
            });

            Receive<TransferResponseMessage>(response =>
            {
                var transactionId = response.TransactionId;
                if (!string.IsNullOrEmpty(transactionId) && _transactions.TryGetValue(transactionId, out var transaction))
                {
                    var updatedTransaction = transaction with
                    {
                        Status = response.Success ? TransactionStatus.Completed : TransactionStatus.Failed
                    };

                    _transactions[transactionId] = updatedTransaction;

                    if (_pendingTransactions.TryGetValue(transactionId, out var originalSender))
                    {
                        originalSender.Tell(response);
                        _pendingTransactions.Remove(transactionId);
                    }

                    var status = response.Success ? "completed" : "failed";
                    Console.WriteLine($"[{Self.Path.Name}] Transaction {transactionId} {status}: {response.Message}");

                    if (response.Success)
                    {
                        Context.Parent.Tell(new TransferCompletedMessage(updatedTransaction));
                    }
                    else
                    {
                        Context.Parent.Tell(new TransferFailedMessage(transactionId, response.Message));
                    }
                }
            });

            Receive<GetTransactionHistoryMessage>(history =>
            {
                var accountTransactions = _transactions.Values
                    .Where(t => t.FromAccount == history.AccountNumber || t.ToAccount == history.AccountNumber)
                    .OrderByDescending(t => t.Timestamp)
                    .ToList();

                Console.WriteLine($"[{Self.Path.Name}] Retrieved {accountTransactions.Count} transactions for account {history.AccountNumber}");
                Sender.Tell(new TransactionHistoryResponseMessage(accountTransactions));
            });

            Receive<string>(message =>
            {
                if (message.StartsWith("status:"))
                {
                    var transactionId = message.Substring(7);
                    if (_transactions.TryGetValue(transactionId, out var transaction))
                    {
                        Sender.Tell($"Transaction {transactionId}: {transaction.Status} - {transaction.Amount:C} from {transaction.FromAccount} to {transaction.ToAccount}");
                    }
                    else
                    {
                        Sender.Tell($"Transaction {transactionId} not found");
                    }
                }
                else if (message == "pending")
                {
                    var pendingCount = _transactions.Values.Count(t => t.Status == TransactionStatus.Pending);
                    Sender.Tell($"Pending transactions: {pendingCount}");
                }
                else if (message == "stats")
                {
                    var total = _transactions.Count;
                    var completed = _transactions.Values.Count(t => t.Status == TransactionStatus.Completed);
                    var failed = _transactions.Values.Count(t => t.Status == TransactionStatus.Failed);
                    var pending = _transactions.Values.Count(t => t.Status == TransactionStatus.Pending);
                    
                    Sender.Tell($"Transaction Stats - Total: {total}, Completed: {completed}, Failed: {failed}, Pending: {pending}");
                }
            });
        }

        protected override void PreStart()
        {
            Console.WriteLine($"[{Self.Path.Name}] Transaction processor started");
        }

        protected override void PostStop()
        {
            Console.WriteLine($"[{Self.Path.Name}] Transaction processor stopped");
        }

        public static Props Props() => Akka.Actor.Props.Create<TransactionActor>();
    }
}