// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Collections;
using Dignus.Log.Attributes;
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
    [LogTarget("file")]
    public class FileLogTarget : ILogTarget
    {
        public string LogFileName { get; set; } = "log.txt";
        public FileRollingType ArchiveRollingType { get; set; }
        public string ArchiveFileName
        {
            get => _archiveFileName;
            set
            {
                _archiveFileName = value.Replace(@"{#}", @"{0}");
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

        private readonly FileStreamWrapper _logFileStream = new FileStreamWrapper();
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
                fileName = fileMakeTime.ToString("yyyyMMdd");
            }
            return fileName;
        }
        private void CheckAndArchiveLogFile()
        {
            var logFileInfo = _logFileInfo;
            if (logFileInfo == null)
            {
                logFileInfo = new FileInfo(LogFileName);
                _logFileInfo = logFileInfo;
            }
            else if (string.Equals(logFileInfo.ToString(), LogFileName, StringComparison.Ordinal) == false)
            {
                logFileInfo = new FileInfo(LogFileName);
                _logFileInfo = logFileInfo;
            }
            else if (_logFileStream.IsOpen() == false)
            {
                logFileInfo.Refresh();
            }

            if (logFileInfo.Exists == false)
            {
                return;
            }

            var currentTime = DateTime.Now;

            var fileMakeTime = logFileInfo.CreationTime;

            while (true)
            {
                var needMakeNewFile = false;

                if (ArchiveRollingType == FileRollingType.Hour)
                {
                    if (fileMakeTime.Date != currentTime.Date)
                    {
                        needMakeNewFile = true;
                    }
                    if (fileMakeTime.Hour != currentTime.Hour)
                    {
                        needMakeNewFile = true;
                    }
                }
                else if (ArchiveRollingType == FileRollingType.Day)
                {
                    if (fileMakeTime.Date != currentTime.Date)
                    {
                        needMakeNewFile = true;
                    }
                }
                else if (ArchiveRollingType == FileRollingType.Week)
                {
                    if (fileMakeTime.Date.AddDays(7) <= currentTime.Date)
                    {
                        needMakeNewFile = true;
                    }
                }
                else if (ArchiveRollingType == FileRollingType.Month)
                {
                    if (fileMakeTime.Year != currentTime.Year)
                    {
                        needMakeNewFile = true;
                    }
                    if (fileMakeTime.Month != currentTime.Month)
                    {
                        needMakeNewFile = true;
                    }
                }

                if (needMakeNewFile == false)
                {
                    return;
                }

                logFileInfo.Refresh();
                if (logFileInfo.Exists == false)
                {
                    return;
                }

                var refreshedFileMakeTime = logFileInfo.CreationTime;
                if (refreshedFileMakeTime != fileMakeTime)
                {
                    fileMakeTime = refreshedFileMakeTime;
                    continue;
                }

                break;
            }

            _logFileStream.Dispose();

            var suffix = GenerateArchiveSuffix(fileMakeTime);

            var archiveFileName = string.Format(ArchiveFileName, suffix);

            DirectoryInfo directoryInfo = new DirectoryInfo(Path.GetDirectoryName(archiveFileName));

            if (directoryInfo.Exists == false)
            {
                directoryInfo.Create();
            }

            var files = directoryInfo.GetFiles(Path.GetFileName(string.Format(ArchiveFileName, "*"))).Where(fileInfo => string.Equals(fileInfo.FullName, logFileInfo.FullName, StringComparison.OrdinalIgnoreCase) == false).OrderBy(fileInfo => fileInfo.Name).ToList();

            if (MaxArchiveFile > 0)
            {
                if (files.Count >= MaxArchiveFile)
                {
                    FileInfo oldFileInfo = files[0];
                    oldFileInfo.Delete();
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
            CheckAndArchiveLogFile();

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
            _logFileStream.Dispose();
        }
    }
}
