using Discord;
using NLog;
using NLog.Conditions;
using NLog.Config;
using NLog.Fluent;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MagicConchBot
{
    public static class Logging
    {

        public static void ConfigureLogs()
        {
            // Step 1. Create configuration object 
            var config = new LoggingConfiguration();

            // Step 2. Create targets and add them to the configuration 
            var consoleTarget = new ColoredConsoleTarget();

            config.AddTarget("console", consoleTarget);

            var fileTarget = new FileTarget();
            config.AddTarget("file", fileTarget);

            consoleTarget.UseDefaultRowHighlightingRules = false;

            ConsoleRowHighlightingRule RowHighlight(LogLevel loglevel, ConsoleOutputColor foregroundColor,
                ConsoleOutputColor backgroundColor = ConsoleOutputColor.Black)
            {
                var condition = ConditionParser.ParseExpression($"level == {loglevel.GetType().Name}.{loglevel}");
                return new ConsoleRowHighlightingRule(condition, foregroundColor, backgroundColor);
            }

            consoleTarget.RowHighlightingRules.Add(RowHighlight(LogLevel.Info, ConsoleOutputColor.Green));
            consoleTarget.RowHighlightingRules.Add(RowHighlight(LogLevel.Debug, ConsoleOutputColor.Yellow));
            consoleTarget.RowHighlightingRules.Add(RowHighlight(LogLevel.Fatal, ConsoleOutputColor.Red));
            consoleTarget.RowHighlightingRules.Add(RowHighlight(LogLevel.Warn, ConsoleOutputColor.Blue));

            // Step 3. Set target properties 
            const string layout = @"${date:format=HH\:mm\:ss} | ${pad:padding=-5:fixedlength=true:inner:${level:uppercase=true}} ${message} ${exception}";
            consoleTarget.Layout = layout;
            fileTarget.Layout = layout;
            fileTarget.FileName = "log.txt";

            // Step 4. Define rules
            var rule1 = new LoggingRule("*", LogLevel.Debug, consoleTarget);
            config.LoggingRules.Add(rule1);

            var rule2 = new LoggingRule("*", LogLevel.Debug, fileTarget);
            config.LoggingRules.Add(rule2);

            // Step 5. Activate the configuration
            LogManager.Configuration = config;
        }

        public static void WriteToLog(this ILogger Log, LogMessage message)
        {
            if (message.Message != null && message.Message.Contains("Unknown OpCode"))
            {
                return;
            }

            switch (message.Severity)
            {
                case LogSeverity.Debug:
                    Log.Debug(message.Exception, message.Message);
                    break;
                case LogSeverity.Verbose:
                case LogSeverity.Info:
                    Log.Info(message.Exception, message.Message);
                    break;
                case LogSeverity.Warning:
                    Log.Warn(message.Exception, message.Message);
                    break;
                case LogSeverity.Error:
                case LogSeverity.Critical:
                    Log.Fatal(message.Exception, message.Message);
                    break;
            }
        }
    }
}
