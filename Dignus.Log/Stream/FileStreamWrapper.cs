// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using System;
using System.IO;

namespace Dignus.Log.Stream
{
    internal class FileStreamWrapper : IDisposable
    {
        private FileStream _fileStream;
        public bool CreateFileStream(string path)
        {
            var exists = File.Exists(path);
            if (exists)
            {
                _fileStream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            }
            else
            {
                _fileStream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite);
            }
            return exists;
        }
        public bool IsOpen()
        {
            return _fileStream != null;
        }
        public void Dispose()
        {
            if (_fileStream != null)
            {
                _fileStream.Flush();
                _fileStream.Close();
                _fileStream = null;
            }
        }
        
        public void Write(byte[] bytes, int count)
        {
            _fileStream.Write(bytes, 0, count);
        }
        public void Flush()
        {
            _fileStream.Flush();
        }
    }

}
