using System.Net;

namespace DiscountAggregator.Bot.Services
{
    public class RetryHandler : DelegatingHandler
    {
        private const int MaxAttempts = 3;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            int attempt = 0;
            while (true)
            {
                attempt++;
                HttpResponseMessage? response = null;
                Exception? error = null;
                try
                {
                    response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

                    if (IsTransient(response.StatusCode))
                    {
                        if (attempt >= MaxAttempts) return response;
                        await Task.Delay(GetDelay(attempt), cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    return response;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    error = ex;
                    if (attempt >= MaxAttempts) throw;
                    await Task.Delay(GetDelay(attempt), cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    if (error != null && response != null)
                    {
                        response.Dispose();
                    }
                }
            }
        }

        private static bool IsTransient(HttpStatusCode code)
        {
            var i = (int)code;
            return i == 429 || i == 408 || (i >= 500 && i <= 599);
        }

        private static TimeSpan GetDelay(int attempt)
        {
            var baseMs = 200 * Math.Pow(2, attempt);
            var jitter = Random.Shared.Next(0, 150);
            return TimeSpan.FromMilliseconds(baseMs + jitter);
        }
    }
}

