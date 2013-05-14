using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using TransactionClasses;

namespace Debugs
{
    class Debug
    {
        static bool debugMode = true;

        public static void msg(string s)
        {
            if (debugMode)
            {
                Console.WriteLine(s);
            }
        }
        public static void msg(string s, int i)
        {
            if (debugMode)
            {
                Console.WriteLine("{0} {1}", s, i);
            }
        }

        public static void msg(string s, Query q)
        {
            if (debugMode)
            {
                if (q != null)
                {
                    Console.WriteLine("{0} func: {1}, start tag: {2}, tag num: {3}", s, q.function, q.startTag, q.tagNum);
                }
                else
                {
                    Console.WriteLine("{0} null", s);
                }
            }
        }

        public static void msg(string s1, int i, string s2, Query q)
        {
            if (debugMode)
            {
                if (q != null)
                {
                    Console.WriteLine("{0} {1} {2} func: {3}, start tag: {4}, tag num: {5}", s1, i, s2, q.function, q.startTag, q.tagNum);
                }
                else
                {
                    Console.WriteLine("{0} {1} {2} null", s1, i, s2);
                }
            }
        }

        public static void msg(string s, byte[] arr)
        { 
            if(debugMode)
            {
                Console.Write(s);
                if (arr != null)
                {
                    foreach (byte b in arr)
                    {
                        Console.Write(" {0}", b);
                    }
                }
                else
                {
                    Console.Write("null");
                }
                Console.WriteLine();
            }
        }
    }

}
