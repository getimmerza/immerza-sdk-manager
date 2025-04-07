using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace ImmerzaSDK.Manager.Editor
{
    internal class LogChannelFile : IDisposable, ILogChannel
    {
        private StreamWriter _writer;
        private bool _disposed;

        public LogChannelFlags Flags => LogChannelFlags.ChannelType | 
                                        LogChannelFlags.Timestamp;

        public LogChannelType Type { get; private set; } 

        public LogChannelFile(string name, LogChannelType channelType)
        {
            Type = channelType;

            string logFileName = Path.Combine(Application.persistentDataPath, $"{name}.log");

            if(File.Exists(logFileName))
            {
                FileInfo fileInfo = new FileInfo(logFileName);
                if (fileInfo.Length != 0)
                {
                    string backupLogFileName = Path.Combine(Application.persistentDataPath, $"{name}-prev.log");
                    File.Copy(logFileName, backupLogFileName, true);
                }
            }

            _writer = new StreamWriter(new FileStream(logFileName, FileMode.Create, FileAccess.Write, FileShare.Read));
        }

        public void Free() 
        {
            if (_writer != null)
            {
                _writer.Flush();
                _writer.Close();
                _writer.Dispose();
            }
            _writer = null;
        }

        public void Dispatch(LogInfo logInfo)
        {
            _writer.WriteLine(logInfo.Message);
            _writer.Flush();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
                if (disposing)
                {
                    Free();
                }
            }
        }
    }
}
