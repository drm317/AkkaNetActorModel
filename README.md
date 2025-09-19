# Akka.NET Banking System - Actor Model Reference Implementation

This project demonstrates a comprehensive banking system built using the Actor Model with Akka.NET, showcasing real-world application of actor-based programming patterns in financial services.

## Banking System Overview

The banking system implements a complete financial services platform with the following components:

### Core Banking Actors

- **AccountActor**: Manages individual account state, balance, and transaction history
- **BankActor**: Coordinates multiple accounts and manages account registry
- **TransactionActor**: Handles money transfers and maintains transaction records
- **FraudDetectionActor**: Monitors transactions for suspicious patterns
- **AtmActor**: Simulates ATM operations with authentication and cash management
- **BankingSupervisorActor**: Provides fault tolerance and system coordination

## Key Features Demonstrated

### 1. Secure Account Management
- Thread-safe balance operations without locks
- Account creation with automatic PIN generation
- Account freezing/unfreezing capabilities
- Transaction history tracking

### 2. Money Transfer System
- Atomic transfer operations between accounts
- Insufficient funds protection
- Transaction status tracking (pending, completed, failed)
- Real-time balance updates

### 3. ATM Operations
- PIN-based authentication
- Cash withdrawal with daily limits
- Deposit processing
- Balance inquiries
- ATM cash management and monitoring

### 4. Fraud Detection System
- **Large Transaction Monitoring**: Alerts for transactions over $10,000
- **Frequency Analysis**: Detects unusual transaction patterns
- **Time-based Detection**: Flags transactions during unusual hours
- **Rapid Succession Detection**: Identifies transactions within seconds
- **Automatic Account Freezing**: Freezes accounts for high-severity alerts

### 5. Fault Tolerance & Supervision
- Automatic actor restart on failures
- Hierarchical supervision strategies
- System-wide error recovery
- State preservation during restarts

## Project Structure

```
ActorModelReference/
├── Banking/
│   ├── Models/
│   │   └── BankingModels.cs        # Domain models and enums
│   ├── Messages/
│   │   └── BankingMessages.cs      # Message contracts
│   ├── Actors/
│   │   ├── AccountActor.cs         # Account state management
│   │   ├── BankActor.cs           # Account coordination
│   │   ├── TransactionActor.cs     # Transfer processing
│   │   ├── FraudDetectionActor.cs  # Security monitoring
│   │   ├── AtmActor.cs            # ATM operations
│   │   └── BankingSupervisorActor.cs # System supervision
│   └── Examples/
│       └── BankingExamples.cs      # Demonstration scenarios
└── Program.cs                      # Main entry point
```

## Running the Banking System

```bash
cd ActorModelReference
dotnet run
```

The application demonstrates five comprehensive scenarios:

### 1. Basic Banking Operations
- Account creation for different account types
- Deposit and withdrawal operations
- Balance inquiries and account management

### 2. Money Transfer System
- Transfers between accounts
- Insufficient funds handling
- Transaction verification and logging

### 3. ATM Operations
- Simulated ATM interactions with PIN authentication
- Cash withdrawal with limits and balance checks
- Deposit processing and balance updates

### 4. Fraud Detection
- Simulation of suspicious transaction patterns
- Real-time fraud alert generation
- Automatic account freezing for critical threats

### 5. Account Management
- Account freezing and unfreezing
- Transaction blocking on frozen accounts
- Administrative operations and account recovery

## Actor Model Benefits in Banking

### 1. **State Isolation**
Each account's state is completely isolated within its actor, eliminating race conditions and ensuring data consistency without complex locking mechanisms.

### 2. **Fault Tolerance**
The supervisor hierarchy ensures that individual actor failures don't cascade through the system. Failed actors are automatically restarted while maintaining system availability.

### 3. **Scalability**
Actors can be distributed across multiple machines, allowing the banking system to scale horizontally while maintaining location transparency.

### 4. **Real-time Processing**
Asynchronous message passing enables real-time transaction processing and fraud detection without blocking operations.

### 5. **Security**
Actor encapsulation provides natural security boundaries, and the fraud detection system can monitor and respond to threats in real-time.

## Message Flow Examples

### Money Transfer Flow
```
Client → BankingSupervisor → TransactionActor → BankActor → AccountActor(From) 
                                                         → AccountActor(To)
                           → FraudDetectionActor (monitoring)
```

### ATM Withdrawal Flow
```
ATM → BankingSupervisor → BankActor → AccountActor (authentication)
                                   → AccountActor (withdrawal)
    ← ATM Response ← BankingSupervisor ← BankActor ← AccountActor
```

## Fraud Detection Patterns

The system monitors for several fraud indicators:

- **Large Transactions**: Transactions over $10,000 trigger alerts
- **High Frequency**: More than 10 transactions in 30 minutes
- **Unusual Timing**: Transactions outside 6 AM - 11 PM
- **Rapid Succession**: Transactions within 5 seconds of each other

Severity levels (Low, Medium, High, Critical) determine the response:
- **High/Critical**: Automatic account freezing
- **Medium**: Alert logging and monitoring
- **Low**: Statistical tracking

## Testing Scenarios

The system includes comprehensive test scenarios that demonstrate:

1. **Normal Operations**: Standard banking transactions
2. **Error Handling**: Insufficient funds, invalid accounts
3. **Security**: Authentication failures, fraud detection
4. **Recovery**: System restart and state preservation
5. **Concurrency**: Multiple simultaneous operations

## Real-World Applications

This implementation demonstrates patterns applicable to:

- **Financial Services**: Banking, payment processing, trading systems
- **E-commerce**: Order processing, inventory management
- **Gaming**: Player state management, real-time interactions
- **IoT Systems**: Device coordination, sensor data processing
- **Microservices**: Service coordination, event processing

## Dependencies

- .NET 9.0
- Akka.NET 1.5.49

## Extensions

This banking system can be extended with:

- **Persistence**: Add Akka.Persistence for transaction durability
- **Clustering**: Deploy across multiple nodes with Akka.Cluster
- **Streaming**: Real-time analytics with Akka.Streams
- **Web API**: REST API layer with ASP.NET Core integration
- **Event Sourcing**: Complete audit trail with event-driven architecture