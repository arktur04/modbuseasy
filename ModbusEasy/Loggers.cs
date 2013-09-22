using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.IO;
using BaseTypes;

namespace Loggers
{

    public abstract class Logger
    {
        public abstract void Append(Var var);
    }

    public class CsvLogger: Logger
    {
        public string fileName { get; set; }
        public string separator { get; set; }
        private StreamWriter sw;
        private FileStream fs;

        public CsvLogger() 
        {
        }

        public CsvLogger(string fileName, string separator)
        {
            this.fileName = fileName;
            this.separator = separator;
        }

        public void open()
        {
            fs = new FileStream(fileName, FileMode.Append);
            sw = new StreamWriter(fs);
        }

        public override void Append(Var var)
        {
            switch (var.state = VarState.OK)
            {
                case VarState.OK:
                    sw.WriteLine("{1}{0}{2}{0}{3}", separator, DateTime.Now, var.name, var.stringValue);
                    break;
                case VarState.Error:
                    sw.WriteLine("{1}{0}{2}{0}Error", separator, DateTime.Now, var.name);
                    break;
                case VarState.TimeOut:
                    sw.WriteLine("{1}{0}{2}{0}Time out", separator, DateTime.Now, var.name);
                    break;
            }

            sw.Flush();
        }
    }
}
