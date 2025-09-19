namespace ActorModelReference.Banking.Models
{
    public record AccountInfo(string AccountNumber, string CustomerName, decimal Balance, DateTime CreatedAt);
    
    public record TransactionInfo(
        string TransactionId, 
        string FromAccount, 
        string ToAccount, 
        decimal Amount, 
        string Description, 
        DateTime Timestamp,
        TransactionStatus Status);
    
    public record CustomerInfo(string CustomerId, string Name, string Email, DateTime CreatedAt);
    
    public enum TransactionStatus
    {
        Pending,
        Completed,
        Failed,
        Cancelled
    }
    
    public enum AccountType
    {
        Checking,
        Savings,
        Business
    }
    
    public record FraudAlert(
        string AccountNumber,
        string Reason,
        decimal Amount,
        DateTime DetectedAt,
        FraudSeverity Severity);
    
    public enum FraudSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }
}