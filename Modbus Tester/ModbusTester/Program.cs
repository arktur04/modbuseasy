using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Timers;
using System.Globalization;

namespace ConsoleApplication2
{
    class Program
    {
        const int period = 1000;  //timer period in ms
        const int testVarPeriod = 20;  // test variable period in seconds

        const double fi = Math.PI / 4; //the angle between current and voltage
        static double alpha;

        static NumberFormatInfo formatProvider;

        static private StreamWriter writer;
        static private StreamReader reader;

        static void timer_Elapsed(object source, ElapsedEventArgs e)
        {
            alpha += 2 * Math.PI / period;
            writer.WriteLine("setdb CurrentValue {0}", Convert.ToString( Math.Sin(alpha), formatProvider));
            writer.WriteLine("setdb VoltageValue {0}", Convert.ToString( Math.Sin(alpha + fi), formatProvider));
        }

        static private System.Timers.Timer timer = new System.Timers.Timer(period);
        static string mbeDirectory = "C:/Users/V/Desktop/Modbus Tester/ModbusEasy/";

        static string appName = "ModbusEasy.exe";
        static string appArg = @"config=""" + mbeDirectory + @"config.xml""";

        static void Main(string[] args)
        {

            formatProvider = new NumberFormatInfo();
            formatProvider.NumberDecimalSeparator = ".";

            timer.Elapsed += timer_Elapsed;
            timer.AutoReset = true;
            timer.Start();
            //------------------------------------------------------------------
            //process init
            ProcessStartInfo processStartInfo = new ProcessStartInfo(mbeDirectory + appName, appArg);
            processStartInfo.UseShellExecute = false;
            processStartInfo.RedirectStandardInput = true;
            processStartInfo.RedirectStandardOutput = true;

            Process process = new Process();

            process.StartInfo = processStartInfo;
            process.Start();
            writer = process.StandardInput;
            reader = process.StandardOutput;
            /*
            writer.WriteLine("getdb -a");
            writer.WriteLine("setdb CurrentValue 100.123");
            writer.WriteLine("getdb CurrentValue");
             */
            while (!process.HasExited)
            {
                Console.Write((char)reader.Read());
                if (Console.KeyAvailable && Console.ReadKey().KeyChar == 'q')
                {
                    writer.WriteLine("q");
                    break;
                }
            }
        }
    }
}
