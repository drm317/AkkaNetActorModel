using Akka.Actor;
using ActorModelReference.Banking.Messages;
using ActorModelReference.Banking.Models;

namespace ActorModelReference.Banking.Actors
{
    public class FraudDetectionActor : ReceiveActor
    {
        private readonly Dictionary<string, List<TransactionInfo>> _accountTransactions;
        private readonly List<FraudAlert> _fraudAlerts;
        private readonly Dictionary<string, DateTime> _lastTransactionTime;

        public FraudDetectionActor()
        {
            _accountTransactions = new Dictionary<string, List<TransactionInfo>>();
            _fraudAlerts = new List<FraudAlert>();
            _lastTransactionTime = new Dictionary<string, DateTime>();

            ReceiveMessages();
        }

        private void ReceiveMessages()
        {
            Receive<MonitorTransactionMessage>(monitor =>
            {
                var transaction = monitor.Transaction;
                Console.WriteLine($"[{Self.Path.Name}] Monitoring transaction: {transaction.TransactionId}");

                AnalyzeTransaction(transaction);
            });

            Receive<GetFraudAlertsMessage>(request =>
            {
                var alerts = _fraudAlerts
                    .Where(a => a.AccountNumber == request.AccountNumber)
                    .OrderByDescending(a => a.DetectedAt)
                    .ToList();

                Console.WriteLine($"[{Self.Path.Name}] Retrieved {alerts.Count} fraud alerts for account {request.AccountNumber}");
                Sender.Tell(new FraudAlertsResponseMessage(alerts));
            });

            Receive<string>(message =>
            {
                if (message == "alerts")
                {
                    var recentAlerts = _fraudAlerts
                        .Where(a => a.DetectedAt > DateTime.UtcNow.AddHours(-24))
                        .Count();
                    Sender.Tell($"Fraud alerts in last 24 hours: {recentAlerts}");
                }
                else if (message == "stats")
                {
                    var total = _fraudAlerts.Count;
                    var high = _fraudAlerts.Count(a => a.Severity >= FraudSeverity.High);
                    var critical = _fraudAlerts.Count(a => a.Severity == FraudSeverity.Critical);
                    
                    Sender.Tell($"Fraud Stats - Total: {total}, High/Critical: {high}, Critical: {critical}");
                }
            });
        }

        private void AnalyzeTransaction(TransactionInfo transaction)
        {
            var fromAccount = transaction.FromAccount;
            var toAccount = transaction.ToAccount;

            if (!_accountTransactions.ContainsKey(fromAccount))
                _accountTransactions[fromAccount] = new List<TransactionInfo>();
            
            if (!_accountTransactions.ContainsKey(toAccount))
                _accountTransactions[toAccount] = new List<TransactionInfo>();

            _accountTransactions[fromAccount].Add(transaction);
            _accountTransactions[toAccount].Add(transaction);

            CheckForFraud(transaction);
        }

        private void CheckForFraud(TransactionInfo transaction)
        {
            var fromAccount = transaction.FromAccount;
            
            if (fromAccount == "EXTERNAL") return;

            var alerts = new List<FraudAlert>();

            alerts.AddRange(CheckLargeTransaction(transaction));
            alerts.AddRange(CheckFrequentTransactions(transaction));
            alerts.AddRange(CheckUnusualTiming(transaction));
            alerts.AddRange(CheckRapidSuccessionTransactions(transaction));

            foreach (var alert in alerts)
            {
                _fraudAlerts.Add(alert);
                Console.WriteLine($"[{Self.Path.Name}] FRAUD ALERT: {alert.Severity} - {alert.Reason}");
                Context.Parent.Tell(new FraudDetectedMessage(alert));
            }
        }

        private List<FraudAlert> CheckLargeTransaction(TransactionInfo transaction)
        {
            var alerts = new List<FraudAlert>();
            
            if (transaction.Amount > 10000)
            {
                alerts.Add(new FraudAlert(
                    transaction.FromAccount,
                    $"Large transaction: {transaction.Amount:C}",
                    transaction.Amount,
                    DateTime.UtcNow,
                    transaction.Amount > 50000 ? FraudSeverity.Critical : FraudSeverity.High));
            }

            return alerts;
        }

        private List<FraudAlert> CheckFrequentTransactions(TransactionInfo transaction)
        {
            var alerts = new List<FraudAlert>();
            var fromAccount = transaction.FromAccount;

            if (fromAccount == "EXTERNAL") return alerts;

            var recentTransactions = _accountTransactions[fromAccount]
                .Where(t => t.Timestamp > DateTime.UtcNow.AddMinutes(-30))
                .Count();

            if (recentTransactions > 10)
            {
                alerts.Add(new FraudAlert(
                    fromAccount,
                    $"High frequency transactions: {recentTransactions} in 30 minutes",
                    transaction.Amount,
                    DateTime.UtcNow,
                    recentTransactions > 20 ? FraudSeverity.Critical : FraudSeverity.High));
            }

            return alerts;
        }

        private List<FraudAlert> CheckUnusualTiming(TransactionInfo transaction)
        {
            var alerts = new List<FraudAlert>();
            var hour = transaction.Timestamp.Hour;

            if (hour < 6 || hour > 23)
            {
                alerts.Add(new FraudAlert(
                    transaction.FromAccount,
                    $"Unusual timing: Transaction at {transaction.Timestamp:HH:mm}",
                    transaction.Amount,
                    DateTime.UtcNow,
                    FraudSeverity.Medium));
            }

            return alerts;
        }

        private List<FraudAlert> CheckRapidSuccessionTransactions(TransactionInfo transaction)
        {
            var alerts = new List<FraudAlert>();
            var fromAccount = transaction.FromAccount;

            if (fromAccount == "EXTERNAL") return alerts;

            if (_lastTransactionTime.TryGetValue(fromAccount, out var lastTime))
            {
                var timeBetween = transaction.Timestamp - lastTime;
                if (timeBetween.TotalSeconds < 5)
                {
                    alerts.Add(new FraudAlert(
                        fromAccount,
                        $"Rapid succession transactions: {timeBetween.TotalSeconds:F1} seconds apart",
                        transaction.Amount,
                        DateTime.UtcNow,
                        FraudSeverity.High));
                }
            }

            _lastTransactionTime[fromAccount] = transaction.Timestamp;
            return alerts;
        }

        protected override void PreStart()
        {
            Console.WriteLine($"[{Self.Path.Name}] Fraud detection system started");
        }

        protected override void PostStop()
        {
            Console.WriteLine($"[{Self.Path.Name}] Fraud detection system stopped");
        }

        public static Props Props() => Akka.Actor.Props.Create<FraudDetectionActor>();
    }
}