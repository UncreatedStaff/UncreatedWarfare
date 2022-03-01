using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections;

namespace Uncreated.Networking.Encoding.IO
{
    public class ByteIO
    {
        protected FileInfo FileInfo;
        protected readonly object threadLocker = new object();
        protected ByteIO(string path)
        {
            if (path != null)
            {
                FileInfo = new FileInfo(path);
            }
        }
    }
    public class RawByteIO<T> : ByteIO
    {
        private IAsyncResult CurrentWrite;
        protected readonly ByteReaderRaw<T> Reader;
        protected readonly ByteWriterRaw<T> Writer;
        bool l = false;
        public RawByteIO(ByteReader.Reader<T> Reader, ByteWriter.Writer<T> Writer, string path, int capacity = 0) : base(path)
        {
            this.Reader = new ByteReaderRaw<T>(Reader);
            this.Writer = new ByteWriterRaw<T>(0, Writer, shouldPrepend: false, capacity: capacity);
        }
        public bool Initialize(Func<T> arg) => InitializeTo(arg, FileInfo);
        public bool InitializeTo(Func<T> arg, FileInfo path)
        {
            if (!Directory.Exists(path.DirectoryName))
                Directory.CreateDirectory(path.DirectoryName);
            if (!File.Exists(path.FullName))
            {
                WriteTo(arg(), path);
                return true;
            }
            return false;
        }
        public bool InitializeTo(Func<T> arg, string path)
        {
            FileInfo info = new FileInfo(path);
            if (!Directory.Exists(info.DirectoryName))
                Directory.CreateDirectory(info.DirectoryName);
            if (!File.Exists(info.FullName))
            {
                WriteTo(arg(), info.FullName);
                return true;
            }
            return false;
        }
        public bool Initialize(T arg) => InitializeTo(arg, FileInfo);
        public bool InitializeTo(T arg, FileInfo path)
        {
            if (!Directory.Exists(path.DirectoryName))
                Directory.CreateDirectory(path.DirectoryName);
            if (!File.Exists(path.FullName))
            {
                WriteTo(arg, path);
                return true;
            }
            return false;
        }
        public bool InitializeTo(T arg, string path)
        {
            FileInfo info = new FileInfo(path);
            if (!Directory.Exists(info.DirectoryName))
                Directory.CreateDirectory(info.DirectoryName);
            if (!File.Exists(info.FullName))
            {
                WriteTo(arg, info.FullName);
                return true;
            }
            return false;
        }
        public void Write(T arg) => WriteTo(arg, FileInfo);
        public void WriteTo(T arg, FileInfo path)
        {
            try
            {
                if (CurrentWrite != null && CurrentWrite.AsyncWaitHandle != null)
                    CurrentWrite.AsyncWaitHandle.WaitOne();
                lock (threadLocker)
                {
                    if (!Directory.Exists(path.DirectoryName))
                        Directory.CreateDirectory(path.DirectoryName);
                    using (FileStream fs = new FileStream(path.FullName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
                    {
                        byte[] bytes = Writer.Get(arg);
                        fs.Write(bytes, 0, bytes.Length);
                        fs.Close();
                        fs.Dispose();
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                Logging.LogError($"[{nameof(RawByteIO<T>)}] No access to file: {path.FullName}.");
            }
            catch (PathTooLongException)
            {
                Logging.LogError($"[{nameof(RawByteIO<T>)}] File path too long: {path.FullName}.");
            }
            catch (Exception ex)
            {
                Logging.LogError($"[{nameof(RawByteIO<T>)}] Exception writing to: {path.FullName}\n{ex.GetType().Name}.");
                Logging.LogError(ex);
            }
        }
        public void WriteTo(T arg, string path)
        {
            try
            {
                if (CurrentWrite != null && CurrentWrite.AsyncWaitHandle != null)
                    CurrentWrite.AsyncWaitHandle.WaitOne();
                lock (threadLocker)
                {
                    using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
                    {
                        byte[] bytes = Writer.Get(arg);
                        fs.Write(bytes, 0, bytes.Length);
                        fs.Close();
                        fs.Dispose();
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                Logging.LogError($"[{nameof(RawByteIO<T>)}] No access to file: {path}.");
            }
            catch (PathTooLongException)
            {
                Logging.LogError($"[{nameof(RawByteIO<T>)}] File path too long: {path}.");
            }
            catch (Exception ex)
            {
                Logging.LogError($"[{nameof(RawByteIO<T>)}] Exception writing to: {path}\n{ex.GetType().Name}.");
                Logging.LogError(ex);
            }
        }
        public void StartWrite(T arg) => StartWriteTo(arg, FileInfo);
        public void StartWriteTo(T arg, FileInfo info)
        {
            try
            {
                if (CurrentWrite != null && CurrentWrite.AsyncWaitHandle != null)
                    CurrentWrite.AsyncWaitHandle.WaitOne();
                System.Threading.Monitor.Enter(threadLocker, ref l);
                if (!Directory.Exists(info.DirectoryName))
                    Directory.CreateDirectory(info.DirectoryName);
                FileStream fs = new FileStream(info.FullName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
                byte[] bytes = Writer.Get(arg);
                CurrentWrite = fs.BeginWrite(bytes, 0, bytes.Length, EndWriteTo, fs);
            }
            catch (UnauthorizedAccessException)     
            {
                Logging.LogError($"[{nameof(RawByteIO<T>)}] No access to file: {info.FullName}.");
            }
            catch (PathTooLongException)
            {
                Logging.LogError($"[{nameof(RawByteIO<T>)}] File path too long: {info.FullName}.");
            }
            catch (Exception ex)
            {
                Logging.LogError($"[{nameof(RawByteIO<T>)}] Exception writing to: {info.FullName}\n{ex.GetType().Name}.");
                Logging.LogError(ex);
            }
        }
        public void StartWriteTo(T arg, string info)
        {
            try
            {
                if (CurrentWrite != null && CurrentWrite.AsyncWaitHandle != null)
                    CurrentWrite.AsyncWaitHandle.WaitOne();
                System.Threading.Monitor.Enter(threadLocker, ref l);
                if (!Directory.Exists(info))
                    Directory.CreateDirectory(info);
                FileStream fs = new FileStream(info, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
                byte[] bytes = Writer.Get(arg);
                CurrentWrite = fs.BeginWrite(bytes, 0, bytes.Length, EndWriteTo, fs);
            }
            catch (UnauthorizedAccessException)     
            {
                Logging.LogError($"[{nameof(RawByteIO<T>)}] No access to file: {info}.");
            }
            catch (PathTooLongException)
            {
                Logging.LogError($"[{nameof(RawByteIO<T>)}] File path too long: {info}.");
            }
            catch (Exception ex)
            {
                Logging.LogError($"[{nameof(RawByteIO<T>)}] Exception writing to: {info}\n{ex.GetType().Name}.");
                Logging.LogError(ex);
            }
        }
        private void EndWriteTo(IAsyncResult ar)
        {
            if (ar.AsyncState is FileStream fs)
            {
                fs.EndWrite(ar);
                fs.Close();
                fs.Dispose();
            }
            if (l) System.Threading.Monitor.Exit(threadLocker);
            CurrentWrite = null;
        }
        public bool Read(out T arg) => ReadFrom(FileInfo, out arg);
        public bool ReadFrom(FileInfo path, out T arg)
        {
            try
            {
                if (!Directory.Exists(path.DirectoryName))
                {
                    //Directory.CreateDirectory(FileInfo.DirectoryName);
                    Logging.LogError($"[{nameof(RawByteIO<T>)}] Can't read a missing file: {path.FullName}.");
                    arg = default;
                    return false;
                }
                if (CurrentWrite != null && CurrentWrite.AsyncWaitHandle != null)
                    CurrentWrite.AsyncWaitHandle.WaitOne();
                lock (threadLocker)
                {
                    using (FileStream fs = new FileStream(path.FullName, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read))
                    {
                        Reader.InternalBuffer = new byte[fs.Length];

                        fs.Read(Reader.InternalBuffer, 0, Reader.InternalBuffer.Length);
                        if (!Reader.Read(null, out arg))
                        {
                            Logging.LogError($"[{nameof(RawByteIO<T>)}] Failed to parse file: {path.FullName}.");
                            fs.Close();
                            fs.Dispose();
                            return false;
                        }
                        fs.Close();
                        fs.Dispose();
                        return true;
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                Logging.LogError($"[{nameof(RawByteIO<T>)}] No access to file: {path.FullName}.");
            }
            catch (PathTooLongException)
            {
                Logging.LogError($"[{nameof(RawByteIO<T>)}] File path too long: {path.FullName}.");
            }
            catch (Exception ex)
            {
                Logging.LogError($"[{nameof(RawByteIO<T>)}] Exception writing to: {path.FullName}\n{ex.GetType().Name}.");
                Logging.LogError(ex);
            }
            arg = default;
            return false;
        }
        public bool ReadFrom(string path, out T arg)
        {
            try
            {
                if (CurrentWrite != null && CurrentWrite.AsyncWaitHandle != null)
                    CurrentWrite.AsyncWaitHandle.WaitOne();
                lock (threadLocker)
                {
                    using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read))
                    {
                        Reader.InternalBuffer = new byte[fs.Length];

                        fs.Read(Reader.InternalBuffer, 0, Reader.InternalBuffer.Length);
                        if (!Reader.Read(null, out arg))
                        {
                            Logging.LogError($"[{nameof(RawByteIO<T>)}] Failed to parse file: {path}.");
                            fs.Close();
                            fs.Dispose();
                            return false;
                        }
                        fs.Close();
                        fs.Dispose();
                        return true;
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                Logging.LogError($"[{nameof(RawByteIO<T>)}] No access to file: {path}.");
            }
            catch (PathTooLongException)
            {
                Logging.LogError($"[{nameof(RawByteIO<T>)}] File path too long: {path}.");
            }
            catch (Exception ex)
            {
                Logging.LogError($"[{nameof(RawByteIO<T>)}] Exception writing to: {path}\n{ex.GetType().Name}.");
                Logging.LogError(ex);
            }
            arg = default;
            return false;
        }
    }
}
