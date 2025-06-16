using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ImmerzaSDK.Manager.Editor
{
    public enum LogSeverity
    {
        Info, Warning, Error
    }

    public enum LogChannelType
    {
        SDKManager = 1,
        SDK        = 2,
        All        = 0x7fffffff
    }

    public struct LogInfo
    {
        public string Function;
        public string File;
        public int Line;
        public string Message;
        public LogSeverity Severity;
    }

    [Flags]
    public enum LogChannelFlags
    {
        None = 0,
        Timestamp = 1,
        ChannelType = 2,
        RawMessage = 4
    }

    public interface ILogChannel
    {
        public void Dispatch(LogInfo logInfo);
        public void Free() { }
        public LogChannelFlags Flags { get; }
        public LogChannelType Type { get; }
    }

    public static class Log 
    {
        private static List<ILogChannel> _logChannels = new();

        public static void LogError(string message, LogChannelType channel, [CallerMemberName]string function = "", [CallerFilePath]string file = "", [CallerLineNumber]int line = 0)
        {
            DispatchMessage(LogSeverity.Error, channel, message, function, file, line);
        }

        public static void LogWarning(string message, LogChannelType channel, [CallerMemberName] string function = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            DispatchMessage(LogSeverity.Warning, channel, message, function, file, line);
        }

        public static void LogInfo(string message, LogChannelType channel, [CallerMemberName] string function = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            DispatchMessage(LogSeverity.Info, channel, message, function, file, line);
        }

        public static void RegisterChannel(ILogChannel channel)
        {
            _logChannels.Add(channel);
        }
        public static void UnregisterChannel(ILogChannel channel)
        {
            _logChannels.Remove(channel);
        }

        public static void FlushAndTeardown()
        {
            foreach (var logChannel in _logChannels)
            {
                logChannel.Free();
            }
        }

        private static void DispatchMessage(LogSeverity severity, LogChannelType channelType, string message, string function, string file, int line)
        {
            foreach (var logChannel in _logChannels)
            {
                if ((channelType & logChannel.Type) == 0)
                    continue;

                string formattedMessage = string.Empty; 

                if (!logChannel.Flags.HasFlag(LogChannelFlags.RawMessage))
                {
                    if (logChannel.Flags.HasFlag(LogChannelFlags.Timestamp))
                    {
                        formattedMessage += $"{DateTime.Now.ToString("[H:mm:ss] ")}";
                    }

                    formattedMessage += $"{severity} ";

                    if (logChannel.Flags.HasFlag(LogChannelFlags.ChannelType))
                    {
                        formattedMessage += $"({channelType.ToString().ToUpper()}) ";
                    }

                    formattedMessage += message;

                    if (severity >= LogSeverity.Warning)
                    {
                        formattedMessage += $"\r\n   from {file}:{line} ({function})";
                    }
                } 
                else
                {
                    formattedMessage = message;
                }

                logChannel.Dispatch(new LogInfo
                {
                    Function = function,
                    File = file,
                    Line = line,
                    Message = formattedMessage,
                    Severity = severity
                });
            }
        }
    }
}