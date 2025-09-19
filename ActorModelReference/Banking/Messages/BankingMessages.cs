using ActorModelReference.Banking.Models;

namespace ActorModelReference.Banking.Messages
{
    public record CreateAccountMessage(string CustomerName, AccountType AccountType, decimal InitialDeposit);
    public record AccountCreatedMessage(string AccountNumber, decimal Balance);
    public record AccountCreationFailedMessage(string Reason);
    
    public record DepositMessage(decimal Amount, string Description = "Deposit");
    public record WithdrawMessage(decimal Amount, string Description = "Withdrawal");
    public record GetBalanceMessage();
    public record BalanceResponseMessage(decimal Balance);
    public record GetAccountInfoMessage();
    public record AccountInfoResponseMessage(AccountInfo AccountInfo);
    
    public record TransferMessage(string FromAccount, string ToAccount, decimal Amount, string Description = "Transfer");
    public record TransferResponseMessage(bool Success, string Message, string? TransactionId = null);
    public record TransferCompletedMessage(TransactionInfo Transaction);
    public record TransferFailedMessage(string TransactionId, string Reason);
    
    public record FreezeAccountMessage(string Reason);
    public record UnfreezeAccountMessage();
    public record AccountFrozenMessage(string Reason);
    public record AccountUnfrozenMessage();
    
    public record MonitorTransactionMessage(TransactionInfo Transaction);
    public record FraudDetectedMessage(FraudAlert Alert);
    public record GetFraudAlertsMessage(string AccountNumber);
    public record FraudAlertsResponseMessage(List<FraudAlert> Alerts);
    
    public record AtmWithdrawMessage(string AccountNumber, string Pin, decimal Amount);
    public record AtmDepositMessage(string AccountNumber, string Pin, decimal Amount);
    public record AtmBalanceInquiryMessage(string AccountNumber, string Pin);
    public record AtmResponseMessage(bool Success, string Message, decimal? Balance = null);
    
    public record AuthenticateMessage(string AccountNumber, string Pin);
    public record AuthenticationResponseMessage(bool Success, string Message);
    
    public record GetAllAccountsMessage();
    public record AllAccountsResponseMessage(List<AccountInfo> Accounts);
    
    public record GetTransactionHistoryMessage(string AccountNumber);
    public record TransactionHistoryResponseMessage(List<TransactionInfo> Transactions);
}