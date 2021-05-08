using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UncreatedWarfare
{
    public class ConfigReadException : Exception
    {
        public ConfigReadException() { }

        public ConfigReadException(StreamReader reader, string directory)
            : base(string.Format("Could not read the Config File {0} because the data was corrupted.", directory))
        {
            reader.Close();
            reader.Dispose();
        }
    }
}
