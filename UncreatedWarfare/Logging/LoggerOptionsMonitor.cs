using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;

namespace Uncreated.Warfare.Logging;

internal class LoggerOptionsMonitor : IOptionsMonitor<LoggerFilterOptions>, IDisposable
{
    private readonly IConfiguration _systemConfig;
    private readonly IDisposable _configChangedToken;

    private readonly List<Action<LoggerFilterOptions, string?>> _listeners;

    public LoggerFilterOptions CurrentValue { get; private set; }

    public LoggerOptionsMonitor(IConfiguration systemConfig)
    {
        _systemConfig = systemConfig;
        _configChangedToken = ChangeToken.OnChange(
            systemConfig.GetReloadToken,
            static thisObj => thisObj.InvokeChange(),
            this
        );

        _listeners = new List<Action<LoggerFilterOptions, string?>>(1);
        CurrentValue = Get(null);
    }

    private void InvokeChange()
    {
        LoggerFilterOptions option = Get(null);
        CurrentValue = option;

        lock (_listeners)
        {
            foreach (Action<LoggerFilterOptions, string?> action in _listeners)
            {
                action(option, null);
            }
        }
    }

    public LoggerFilterOptions Get(string? name)
    {
        LoggerFilterOptions options = new LoggerFilterOptions();

        // configure logging from config
        IConfigurationSection loggingSection = _systemConfig.GetSection("logging");

        string? logLvl = loggingSection["minimum_level"];
        options.MinLevel = logLvl == null
            ? LogLevel.Trace
            : Enum.Parse<LogLevel>(logLvl, ignoreCase: true);


        foreach (IConfigurationSection value in loggingSection.GetSection("filters").GetChildren())
        {
            options.AddFilter(value.Key, Enum.Parse<LogLevel>(value.Value, ignoreCase: true));
        }

        return options;
    }

    public IDisposable OnChange(Action<LoggerFilterOptions, string?> listener)
    {
        lock (_listeners)
        {
            _listeners.Add(listener);
        }

        return new RegisteredListener(listener, this);
    }


    public void Dispose()
    {
        _configChangedToken.Dispose();
    }

    private class RegisteredListener : IDisposable
    {
        private readonly Action<LoggerFilterOptions, string?> _listener;
        private readonly LoggerOptionsMonitor _monitor;

        public RegisteredListener(Action<LoggerFilterOptions, string?> listener, LoggerOptionsMonitor monitor)
        {
            _listener = listener;
            _monitor = monitor;
        }

        public void Dispose()
        {
            lock (_monitor._listeners)
            {
                _monitor._listeners.Remove(_listener);
            }
        }
    }
}