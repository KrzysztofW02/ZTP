using System;
using System.Threading;
using RabbitMQ.Client;

namespace Shared
{
    public static class ConnectionHelper
    {
        public static IConnection CreateConnectionWithRetry(
            ConnectionFactory factory,
            int maxAttempts = 5,
            int delayMs = 2000)
        {
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    Console.WriteLine($"[RabbitMQ] Attempting connection {attempt}/{maxAttempts}...");
                    return factory.CreateConnection();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[RabbitMQ] Connection attempt failed: {ex.Message}");
                    if (attempt == maxAttempts)
                        throw; 
                    Thread.Sleep(delayMs);
                }
            }
            throw new InvalidOperationException("Unable to connect to RabbitMQ.");
        }
    }
}
