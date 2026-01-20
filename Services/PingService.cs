using System;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace PingMonitor.Services
{
    public class PingService
    {
        /// <summary>
        /// Pings a host with retries and returns success status and average latency.
        /// </summary>
        /// <param name="ipAddress">The IP address to ping.</param>
        /// <param name="timeoutMs">Timeout in milliseconds for each ping.</param>
        /// <param name="retryCount">Number of retries (total attempts = retryCount). Minimum 1.</param>
        /// <returns>A tuple containing (IsSuccess, AverageLatency).</returns>
        public async Task<(bool IsSuccess, long Latency)> PingHostAsync(string ipAddress, int timeoutMs, int retryCount)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
                return (false, 0);

            // Ensure at least one attempt
            int maxAttempts = Math.Max(1, retryCount);
            
            bool anySuccess = false;
            long totalLatency = 0;
            int successCount = 0;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                // If we already had a success, and we're just gathering more data for average latency?
                // OR do we stop on first success?
                // The previous logic was: "Ping multiple times... online if at least one succeeds".
                // BUT it also calculated average latency. To get a *good* average, you might want to continue.
                // However, for speed, usually "first success" is enough to say "Online".
                // Let's stick to the previous logic: Try up to retryCount.
                // Wait, the previous logic in RefreshAllPings was:
                // for (attempt < retryCount && !anySuccess)
                // This implies it STOPS as soon as it gets a success. 
                // So average latency is just the latency of the *first* successful ping.
                // That effectively means "Latency" is just the first successful roundtrip.
                
                if (anySuccess) break; // Stop after first success as per original logic for speed

                try
                {
                    using (var ping = new Ping())
                    {
                        var reply = await ping.SendPingAsync(ipAddress, timeoutMs);
                        if (reply.Status == IPStatus.Success)
                        {
                            anySuccess = true;
                            totalLatency += reply.RoundtripTime;
                            successCount++;
                        }
                    }
                }
                catch
                {
                    // Ignore exceptions (ping failed)
                }

                // Small delay between retries if failed
                if (!anySuccess && attempt < maxAttempts - 1)
                {
                    await Task.Delay(100);
                }
            }

            long avgLatency = successCount > 0 ? totalLatency / successCount : 0;
            return (anySuccess, avgLatency);
        }
    }
}
