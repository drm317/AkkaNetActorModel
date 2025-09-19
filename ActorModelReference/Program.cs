using Akka.Actor;
using ActorModelReference.Banking.Examples;

namespace ActorModelReference
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Akka.NET Banking System - Actor Model Reference Implementation ===");
            Console.WriteLine("\nThis demonstration showcases a complete banking system using the Actor Model:");
            Console.WriteLine("• Account management with state isolation");
            Console.WriteLine("• Secure money transfers between accounts");
            Console.WriteLine("• ATM operations with authentication");
            Console.WriteLine("• Real-time fraud detection and monitoring");
            Console.WriteLine("• System supervision and fault recovery");
            Console.WriteLine("• Concurrent transaction processing\n");

            var config = @"
                akka {
                    loglevel = INFO
                    actor {
                        debug {
                            receive = on
                            autoreceive = on
                            lifecycle = on
                        }
                    }
                }";

            using var actorSystem = ActorSystem.Create("BankingSystem", config);

            try
            {
                Console.WriteLine("Starting banking system demonstrations...\n");

                await BankingExamples.RunBasicBankingExample(actorSystem);
                await Task.Delay(2000);

                await BankingExamples.RunTransferExample(actorSystem);
                await Task.Delay(2000);

                await BankingExamples.RunAtmExample(actorSystem);
                await Task.Delay(2000);

                await BankingExamples.RunFraudDetectionExample(actorSystem);
                await Task.Delay(2000);

                await BankingExamples.RunAccountManagementExample(actorSystem);

                Console.WriteLine("\n=== Banking System Demonstration Completed Successfully ===");
                Console.WriteLine("\nKey Benefits Demonstrated:");
                Console.WriteLine("✓ Thread-safe state management without locks");
                Console.WriteLine("✓ Automatic fault tolerance and recovery");
                Console.WriteLine("✓ Real-time fraud detection and alerting");
                Console.WriteLine("✓ Scalable concurrent transaction processing");
                Console.WriteLine("✓ Clean separation of concerns");
                Console.WriteLine("✓ Location transparency for distributed deployment");
                
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error running banking examples: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                await actorSystem.Terminate();
                Console.WriteLine("\nBanking system terminated gracefully.");
            }
        }
    }
}