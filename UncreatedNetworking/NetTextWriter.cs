using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated.Networking
{
    public class NetTextWriter : TextWriter
    {
        public override System.Text.Encoding Encoding => System.Text.Encoding.UTF8;
        public TextWriter UnderlyingWriter { get; private set; }
        public IFormatProvider Locale { get; private set; }
        public NetCall<string, ConsoleColor, DateTime> Writer { get; private set; }
        public IConnection Connection { get; set; }
        public NetTextWriter(TextWriter underlying, NetCall<string, ConsoleColor, DateTime> writer, IConnection connection, IFormatProvider locale)
        {
            UnderlyingWriter = underlying;
            Writer = writer;
            Connection = connection;
            Locale = locale;
        }
        public override void Write(bool value)
        {
            UnderlyingWriter.Write(value);
            Writer.Invoke(Connection, value ? "True" : "False", Console.ForegroundColor, DateTime.Now);
        }
        public override void Write(char value)
        {
            UnderlyingWriter.Write(value);
            Writer.Invoke(Connection, value.ToString(), Console.ForegroundColor, DateTime.Now);
        }
        public override void Write(char[] buffer)
        {
            UnderlyingWriter.Write(buffer);
            Writer.Invoke(Connection, new string(buffer), Console.ForegroundColor, DateTime.Now);
        }
        public override void Write(char[] buffer, int index, int count)
        {
            UnderlyingWriter.Write(buffer);
            Writer.Invoke(Connection, new string(buffer, index, count), Console.ForegroundColor, DateTime.Now);
        }
        public override void Write(decimal value)
        {
            UnderlyingWriter.Write(value);
            Writer.Invoke(Connection, value.ToString(Locale), Console.ForegroundColor, DateTime.Now);
        }
        public override void Write(double value)
        {
            UnderlyingWriter.Write(value);
            Writer.Invoke(Connection, value.ToString(Locale), Console.ForegroundColor, DateTime.Now);
        }
        public override void Write(float value)
        {
            UnderlyingWriter.Write(value);
            Writer.Invoke(Connection, value.ToString(Locale), Console.ForegroundColor, DateTime.Now);
        }
        public override void Write(int value)
        {
            UnderlyingWriter.Write(value);
            Writer.Invoke(Connection, value.ToString(Locale), Console.ForegroundColor, DateTime.Now);
        }
        public override void Write(long value)
        {
            UnderlyingWriter.Write(value);
            Writer.Invoke(Connection, value.ToString(Locale), Console.ForegroundColor, DateTime.Now);
        }
        public override void Write(object value)
        {
            UnderlyingWriter.Write(value);
            Writer.Invoke(Connection, value.ToString(), Console.ForegroundColor, DateTime.Now);
        }
        public override void Write(string value)
        {
            UnderlyingWriter.Write(value);
            Writer.Invoke(Connection, value, Console.ForegroundColor, DateTime.Now);
        }
        public override void Write(uint value)
        {
            UnderlyingWriter.Write(value);
            Writer.Invoke(Connection, value.ToString(Locale), Console.ForegroundColor, DateTime.Now);
        }
        public override void Write(ulong value)
        {
            UnderlyingWriter.Write(value);
            Writer.Invoke(Connection, value.ToString(Locale), Console.ForegroundColor, DateTime.Now);
        }
        public override void Write(string format, object arg0)
        {
            string formatted = string.Format(format, arg0);
            UnderlyingWriter.Write(formatted);
            Writer.Invoke(Connection, formatted, Console.ForegroundColor, DateTime.Now);
        }
        public override void Write(string format, object arg0, object arg1)
        {
            string formatted = string.Format(format, arg0, arg1);
            UnderlyingWriter.Write(formatted);
            Writer.Invoke(Connection, formatted, Console.ForegroundColor, DateTime.Now);
        }
        public override void Write(string format, object arg0, object arg1, object arg2)
        {
            string formatted = string.Format(format, arg0, arg1, arg2);
            UnderlyingWriter.Write(formatted);
            Writer.Invoke(Connection, formatted, Console.ForegroundColor, DateTime.Now);
        }
        public override void Write(string format, params object[] arg)
        {
            string formatted = string.Format(format, arg);
            UnderlyingWriter.Write(formatted);
            Writer.Invoke(Connection, formatted, Console.ForegroundColor, DateTime.Now);
        }
        public override async Task WriteAsync(char value)
        {
            await UnderlyingWriter.WriteAsync(value);
            Writer.Invoke(Connection, value.ToString(), Console.ForegroundColor, DateTime.Now);
        }
        public override async Task WriteAsync(char[] buffer, int index, int count)
        {
            await UnderlyingWriter.WriteAsync(buffer);
            Writer.Invoke(Connection, new string(buffer, index, count), Console.ForegroundColor, DateTime.Now);
        }
        public override async Task WriteAsync(string value)
        {
            await UnderlyingWriter.WriteAsync(value);
            Writer.Invoke(Connection, value, Console.ForegroundColor, DateTime.Now);
        }
        public override void WriteLine()
        {
            UnderlyingWriter.WriteLine();
            Writer.Invoke(Connection, string.Empty, Console.ForegroundColor, DateTime.Now);
        }
        public override void WriteLine(bool value)
        {
            UnderlyingWriter.WriteLine(value);
            Writer.Invoke(Connection, value ? "True" : "False", Console.ForegroundColor, DateTime.Now);
        }
        public override void WriteLine(char value)
        {
            UnderlyingWriter.WriteLine(value);
            Writer.Invoke(Connection, value.ToString(), Console.ForegroundColor, DateTime.Now);
        }
        public override void WriteLine(char[] buffer)
        {
            UnderlyingWriter.WriteLine(buffer);
            Writer.Invoke(Connection, new string(buffer), Console.ForegroundColor, DateTime.Now);
        }
        public override void WriteLine(char[] buffer, int index, int count)
        {
            UnderlyingWriter.WriteLine(buffer);
            Writer.Invoke(Connection, new string(buffer, index, count), Console.ForegroundColor, DateTime.Now);
        }
        public override void WriteLine(decimal value)
        {
            UnderlyingWriter.WriteLine(value);
            Writer.Invoke(Connection, value.ToString(Locale), Console.ForegroundColor, DateTime.Now);
        }
        public override void WriteLine(double value)
        {
            UnderlyingWriter.WriteLine(value);
            Writer.Invoke(Connection, value.ToString(Locale), Console.ForegroundColor, DateTime.Now);
        }
        public override void WriteLine(float value)
        {
            UnderlyingWriter.WriteLine(value);
            Writer.Invoke(Connection, value.ToString(Locale), Console.ForegroundColor, DateTime.Now);
        }
        public override void WriteLine(int value)
        {
            UnderlyingWriter.WriteLine(value);
            Writer.Invoke(Connection, value.ToString(Locale), Console.ForegroundColor, DateTime.Now);
        }
        public override void WriteLine(long value)
        {
            UnderlyingWriter.WriteLine(value);
            Writer.Invoke(Connection, value.ToString(Locale), Console.ForegroundColor, DateTime.Now);
        }
        public override void WriteLine(object value)
        {
            UnderlyingWriter.WriteLine(value);
            Writer.Invoke(Connection, value.ToString(), Console.ForegroundColor, DateTime.Now);
        }
        public override void WriteLine(string format, object arg0)
        {
            string formatted = string.Format(format, arg0);
            UnderlyingWriter.Write(formatted);
            Writer.Invoke(Connection, formatted, Console.ForegroundColor, DateTime.Now);
        }
        public override void WriteLine(string format, object arg0, object arg1)
        {
            string formatted = string.Format(format, arg0, arg1);
            UnderlyingWriter.Write(formatted);
            Writer.Invoke(Connection, formatted, Console.ForegroundColor, DateTime.Now);
        }
        public override void WriteLine(string format, object arg0, object arg1, object arg2)
        {
            string formatted = string.Format(format, arg0, arg1, arg2);
            UnderlyingWriter.Write(formatted);
            Writer.Invoke(Connection, formatted, Console.ForegroundColor, DateTime.Now);
        }
        public override void WriteLine(string format, params object[] arg)
        {
            string formatted = string.Format(format, arg);
            UnderlyingWriter.Write(formatted);
            Writer.Invoke(Connection, formatted, Console.ForegroundColor, DateTime.Now);
        }
        public override void WriteLine(string value)
        {
            UnderlyingWriter.WriteLine(value);
            Writer.Invoke(Connection, value, Console.ForegroundColor, DateTime.Now);
        }
        public override void WriteLine(uint value)
        {
            UnderlyingWriter.WriteLine(value);
            Writer.Invoke(Connection, value.ToString(Locale), Console.ForegroundColor, DateTime.Now);
        }
        public override void WriteLine(ulong value)
        {
            UnderlyingWriter.WriteLine(value);
            Writer.Invoke(Connection, value.ToString(Locale), Console.ForegroundColor, DateTime.Now);
        }
        public override async Task WriteLineAsync()
        {
            await UnderlyingWriter.WriteLineAsync();
            Writer.Invoke(Connection, string.Empty, Console.ForegroundColor, DateTime.Now);
        }
        public override async Task WriteLineAsync(char value)
        {
            await UnderlyingWriter.WriteLineAsync(value);
            Writer.Invoke(Connection, value.ToString(), Console.ForegroundColor, DateTime.Now);
        }
        public override async Task WriteLineAsync(char[] buffer, int index, int count)
        {
            await UnderlyingWriter.WriteLineAsync(buffer, index, count);
            Writer.Invoke(Connection, new string(buffer, index, count), Console.ForegroundColor, DateTime.Now);
        }
        public override async Task WriteLineAsync(string value)
        {
            await UnderlyingWriter.WriteLineAsync(value);
            Writer.Invoke(Connection, value, Console.ForegroundColor, DateTime.Now);
        }
        public override async Task FlushAsync()
        {
            await UnderlyingWriter.FlushAsync();
            await base.FlushAsync();
        }
        public override void Flush()
        {
            UnderlyingWriter.Flush();
            base.Flush();
        }
        public override void Close()
        {
            UnderlyingWriter.Close();
            base.Close();
        }
        public override object InitializeLifetimeService()
        {
            UnderlyingWriter.InitializeLifetimeService();
            return base.InitializeLifetimeService();
        }
        public override IFormatProvider FormatProvider => Locale;
        protected override void Dispose(bool disposing)
        {
            UnderlyingWriter.Dispose();
            base.Dispose(disposing);
        }
    }
}
