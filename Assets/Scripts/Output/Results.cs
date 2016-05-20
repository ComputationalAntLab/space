using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Assets.Scripts.Output
{
    public abstract class Results : IDisposable
    {
        private StreamWriter _writer;

        public Results(string fileNameWithoutExtension)
        {
            _writer = new StreamWriter(fileNameWithoutExtension + ".txt", false);
        }

        protected void Write(string message)
        {
            _writer.WriteLine(message);
        }

        public void Dispose()
        {
            if (_writer != null)
                _writer.Close();
        }
    }
}
