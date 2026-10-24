// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Collections;
using Dignus.Log.Formatting;
using Dignus.Log.Formatting.Interfaces;
using Dignus.Log.LogTarget.Interfaces;
using Dignus.Log.Model;
using Dignus.Log.Stream;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Dignus.Log.LogTarget
{
    public class FileLogTarget : ILogTarget
    {
        public string LogFileName { get; set; } = "log.txt";
        public FileRollingType ArchiveRollingType { get; set; }
        public string ArchiveFileName
        {
            get => _archiveFileName;
            set
            {
                _archiveFileName = value?.Replace(@"{#}", @"{0}");
            }
        }
        public uint MaxArchiveFile { get; set; }
        public bool AutoFlush { get; set; }
        public bool KeepOpenFile { get; set; }

        public LogFormatRenderer LogFormatRenderer
        {
            get => _logFormatRenderer;
            set
            {
                if (value is ILogFormatRenderer)
                {
                    _logFormatRenderer = value;
                }
            }
        }
        private LogFormatRenderer _logFormatRenderer;
        private string _archiveFileName;
        private FileInfo _logFileInfo;
        private DateTime _rollingPeriodStart;
        private DateTime _rollingPeriodEnd;
        private FileRollingType _cachedArchiveRollingType;
        private bool _rollingPeriodCached;

        private readonly FileStreamWrapper _logFileStream = new FileStreamWrapper();
        private readonly object _syncObj = new object();
        private readonly StringBuilder _logStringBuilder = new StringBuilder();
        private char[] _characterBuffer = new char[1024];
        private byte[] _byteBuffer = new byte[4096];
        public FileLogTarget()
        {
            _logFormatRenderer = LogFormatElementCreator.CreateDefaultRenderer();
        }
        private string GenerateArchiveSuffix(DateTime fileMakeTime)
        {
            var fileName = string.Empty;
            if (ArchiveRollingType == FileRollingType.Hour)
            {
                fileName = fileMakeTime.ToString("yyyyMMdd.HH");
            }
            else if (ArchiveRollingType == FileRollingType.Day)
            {
                fileName = fileMakeTime.ToString("yyyyMMdd");
            }
            else if (ArchiveRollingType == FileRollingType.Week)
            {
                fileName = fileMakeTime.ToString("yyyyMMdd");
            }
            else if (ArchiveRollingType == FileRollingType.Month)
            {
                fileName = fileMakeTime.ToString("yyyyMM");
            }
            return fileName;
        }
        private void CacheRollingPeriod(DateTime fileMakeTime)
        {
            if (ArchiveRollingType == FileRollingType.Hour)
            {
                _rollingPeriodStart = fileMakeTime.Date.AddHours(fileMakeTime.Hour);
                _rollingPeriodEnd = _rollingPeriodStart.AddHours(1);
            }
            else if (ArchiveRollingType == FileRollingType.Day)
            {
                _rollingPeriodStart = fileMakeTime.Date;
                _rollingPeriodEnd = _rollingPeriodStart.AddDays(1);
            }
            else if (ArchiveRollingType == FileRollingType.Week)
            {
                _rollingPeriodStart = DateTime.MinValue;
                _rollingPeriodEnd = fileMakeTime.Date.AddDays(7);
            }
            else if (ArchiveRollingType == FileRollingType.Month)
            {
                _rollingPeriodStart = new DateTime(fileMakeTime.Year, fileMakeTime.Month, 1);
                _rollingPeriodEnd = _rollingPeriodStart.AddMonths(1);
            }
            else
            {
                _rollingPeriodStart = DateTime.MinValue;
                _rollingPeriodEnd = DateTime.MaxValue;
            }
            _cachedArchiveRollingType = ArchiveRollingType;
            _rollingPeriodCached = true;
        }
        private void CheckAndArchiveLogFile(DateTime currentTime)
        {
            var logFileInfo = _logFileInfo;
            if (logFileInfo == null)
            {
                logFileInfo = new FileInfo(LogFileName);
                _logFileInfo = logFileInfo;
                _rollingPeriodCached = false;
            }
            else if (string.Equals(logFileInfo.ToString(), LogFileName, StringComparison.Ordinal) == false)
            {
                logFileInfo = new FileInfo(LogFileName);
                _logFileInfo = logFileInfo;
                _rollingPeriodCached = false;
            }
            else if (_logFileStream.IsOpen() == false)
            {
                logFileInfo.Refresh();
                _rollingPeriodCached = false;
            }

            if (logFileInfo.Exists == false)
            {
                _rollingPeriodCached = false;
                return;
            }

            if (_rollingPeriodCached == false)
            {
                CacheRollingPeriod(logFileInfo.CreationTime);
            }
            else if (_cachedArchiveRollingType != ArchiveRollingType)
            {
                CacheRollingPeriod(logFileInfo.CreationTime);
            }

            if (_rollingPeriodStart <= currentTime)
            {
                if (currentTime < _rollingPeriodEnd)
                {
                    return;
                }
            }

            var fileMakeTime = logFileInfo.CreationTime;

            while (true)
            {
                if (_rollingPeriodStart <= currentTime)
                {
                    if (currentTime < _rollingPeriodEnd)
                    {
                        return;
                    }
                }

                logFileInfo.Refresh();
                if (logFileInfo.Exists == false)
                {
                    _rollingPeriodCached = false;
                    return;
                }

                var refreshedFileMakeTime = logFileInfo.CreationTime;
                if (refreshedFileMakeTime != fileMakeTime)
                {
                    fileMakeTime = refreshedFileMakeTime;
                    CacheRollingPeriod(fileMakeTime);
                    continue;
                }

                break;
            }

            _logFileStream.Dispose();

            var suffix = GenerateArchiveSuffix(fileMakeTime);

            var archiveFileName = string.Format(ArchiveFileName, suffix);

            DirectoryInfo directoryInfo = new DirectoryInfo(Path.GetDirectoryName(Path.GetFullPath(archiveFileName)));

            if (directoryInfo.Exists == false)
            {
                directoryInfo.Create();
            }

            var files = directoryInfo.GetFiles(Path.GetFileName(string.Format(ArchiveFileName, "*"))).Where(fileInfo => string.Equals(fileInfo.FullName, logFileInfo.FullName, StringComparison.OrdinalIgnoreCase) == false).OrderBy(fileInfo => fileInfo.Name).ToList();

            if (MaxArchiveFile > 0)
            {
                for (int i = 0; i + MaxArchiveFile <= files.Count; ++i)
                {
                    files[i].Delete();
                }
            }
            var count = 1;
            while (File.Exists(archiveFileName))
            {
                archiveFileName = string.Format(ArchiveFileName, $"{suffix} ({count})");
                count++;
            }

            File.Move(LogFileName, archiveFileName);
            _logFileInfo = null;
            _rollingPeriodCached = false;
        }
        private void CreateAndOpenLogFileStream(string path)
        {
            var fileInfo = new FileInfo(path);
            if (fileInfo.Directory.Exists == false)
            {
                fileInfo.Directory.Create();
            }
            if (_logFileStream.CreateFileStream(path) == false)
            {
                fileInfo.CreationTime = DateTime.Now;
            }
            _logFileInfo = fileInfo;
            _rollingPeriodCached = false;
        }
        private int EncodeLogMessageToUtf8()
        {
            int characterCount = _logStringBuilder.Length;
            if (_characterBuffer.Length < characterCount)
            {
                Array.Resize(ref _characterBuffer, Math.Max(_characterBuffer.Length * 2, characterCount));
            }

            int requiredByteCount = Encoding.UTF8.GetMaxByteCount(characterCount);
            if (_byteBuffer.Length < requiredByteCount)
            {
                Array.Resize(ref _byteBuffer, Math.Max(_byteBuffer.Length * 2, requiredByteCount));
            }

            _logStringBuilder.CopyTo(0, _characterBuffer, 0, characterCount);
            return Encoding.UTF8.GetBytes(_characterBuffer, 0, characterCount, _byteBuffer, 0);
        }
        public void Write(LogEvent logEvent)
        {
            lock (_syncObj)
            {
                WriteCore(logEvent);
            }
        }
        private void WriteCore(LogEvent logEvent)
        {
            if (string.IsNullOrEmpty(_archiveFileName) == false)
            {
                CheckAndArchiveLogFile(logEvent.DateTime);
            }

            if (_logFileStream.IsOpen() == false)
            {
                CreateAndOpenLogFileStream(LogFileName);
            }

            _logFormatRenderer.RenderTo(_logStringBuilder, logEvent);
            _logStringBuilder.AppendLine();

            int byteCount = EncodeLogMessageToUtf8();
            _logStringBuilder.Clear();
            _logFileStream.Write(_byteBuffer, byteCount);

            if (AutoFlush == true)
            {
                _logFileStream.Flush();
            }

            if (KeepOpenFile == false)
            {
                _logFileStream.Dispose();
            }
        }
        public void Dispose()
        {
            lock (_syncObj)
            {
                _logFileStream.Dispose();
            }
        }
    }
}
