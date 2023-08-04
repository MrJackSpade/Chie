using ChieApi.Shared.Entities;
using Logging.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http.Json;

namespace Logging
{
    public class LoggingApiClient : ILogger, IDisposable
    {
        private readonly HttpClient _httpClient = new();

        private readonly AutoResetEvent _logGate = new(false);

        private readonly Thread _processingThread;

        private readonly AutoResetEvent _exitGate = new(false);

        private bool _disposed = false;

        private readonly Stack<ILoggingScope> _scopes = new();

        private readonly LoggingApiClientSettings _settings;

        private readonly ConcurrentQueue<LogEntry> _toSend = new();

        public LoggingApiClient(LoggingApiClientSettings settings)
        {
            this._settings = settings;
            this._processingThread = new Thread(() => this.SendLoop());
            this._processingThread.Start();
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            LoggingScope<TState> loggingScope = new(state);

            this._scopes.Push(loggingScope);

            loggingScope.OnDispose += (l, e) =>
            {
                if (this._scopes.Peek() != l)
                {
                    throw new InvalidOperationException("Popped scope does not equal last disposed object. Scopes out of sync");
                }

                this._scopes.Pop();
            };
            return loggingScope;
        }

        public void Dispose()
        {
            this._disposed = true;
            this._logGate.Set();
            this._exitGate.WaitOne();
        }
        public bool IsEnabled(LogLevel logLevel) => this._settings.LogLevel <= logLevel;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            LogEntry logEntry = new()
            {
                Level = logLevel,
                EventId = eventId.Id,
                EventName = eventId.Name,
                Content = formatter.Invoke(state, exception),
                DateCreated = DateTime.UtcNow,
                Scope = string.Join(".", this._scopes.Select(s => s.State.ToString())),
                ApplicationName = this._settings.ApplicationName
            };

            Debug.WriteLine(logEntry.Content);

            this._toSend.Enqueue(logEntry);

            this._logGate.Set();
        }

        private async void SendLoop()
        {
            do
            {
                this._logGate.WaitOne();

                bool exitAfter = this._disposed;

                try
                {
                    List<LogEntry> toSend = new();

                    while (this._toSend.Any() && this._toSend.TryDequeue(out LogEntry result))
                    {
                        toSend.Add(result);
                    }

                    if (toSend.Any())
                    {
                        try
                        {
                            HttpResponseMessage response = await this._httpClient.PostAsync($"{this._settings.Host}/Logging/Log", JsonContent.Create(toSend.ToArray()));
                        }
                        catch (Exception ex)
                        {
                            foreach (LogEntry entry in toSend)
                            {
                                this._toSend.Enqueue(entry);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                }

                if (exitAfter)
                {
                    this._exitGate.Set();
                    return;
                }

                Thread.Sleep(1000);
            } while (true);
        }
    }
}