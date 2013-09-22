using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Timers;
using System.Text.RegularExpressions;
using System.IO;
using Config;
using BaseTypes;
using Ports;
using TransactionClasses;
using GlobalObjects;
using Protocols;
using Debugs;
using CommandUtils;

namespace ConfigReaderTest
{
    class Program
    {
        //объект GlobalObject содержит все "глобальные" объекты программы,
        //существующие в единственном экземпляре.
        // 

        static private GlobalObject mGlobalObject = new GlobalObject();
        static public GlobalObject globalObject { get { return mGlobalObject; } }
        //  static private int timeOut;
        //---------------------------------------
        // main timer handler
        //---------------------------------------
        private static void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (globalObject.queryList != null && globalObject.queryList.Count > 0)
             {
                Debug.msg("periodical timer elapsed");
                Debug.msg("");
                Query q = globalObject.queryList.First.Value;
            //    Debug.msg("query ", q);
                globalObject.currentTransaction = new Transaction(q, globalObject);
              //  globalObject.currentTransaction.transactonFinishedEvent += Transaction_Finished;
            }
        }
        //-----------------------------------
        // Transaction Finished Handler
        //-----------------------------------
        private static void Transaction_Finished(Transaction transaction)
        {
            //find the next query
            if (globalObject.queryList != null && globalObject.queryList.Count > 0)
            {
                if (transaction.query != null)
                {
                    LinkedListNode<Query> lnq = (globalObject.queryList.Find(transaction.query));
                    if(lnq != null && lnq != globalObject.queryList.Last)
                    {
                        Query q = lnq.Next.Value;
                        if (q != null)
                        {
                            globalObject.currentTransaction = new Transaction(q, globalObject);
                //            globalObject.currentTransaction.transactonFinishedEvent += Transaction_Finished;
                        }
                    }
                }
            }
        }

        private static void onMessageReceived_SlaveMode(object sender, byte[] msg)
        {
            byte[] response = mGlobalObject.protocol.buildResponse(msg);
            mGlobalObject.port.send(response);
        }

        //------------------------------
        // Main
        //------------------------------
        static void Main(string[] args)
        {
            const string defaultFileName = "config.xml";
            string configFileName = defaultFileName;
            foreach(string arg in args)
            {
                CParser.configFileName(arg, ref configFileName);
            }
            Console.WriteLine("config = {0}", configFileName);  //debug purpouse
            ConfigReader configReader = new ConfigReader();
            try
            {
                configReader.loadFromFile(configFileName);
            }
            catch (FileLoadException)
            {
                Console.WriteLine("Can't load config file");
            }
            globalObject.transactonFinishedEvent = Transaction_Finished;

            Transaction.currId = 0; //initial transaction id

            if (configReader.portType == PortType.ComPort)
            {
                //string name, int bps, Parity parity, StopBits stopBits)
                ComPort comPort = new ComPort(configReader.portName, configReader.bps, configReader.parity, configReader.stopBits);
                comPort.recieveTimeOut = configReader.comPortTimeOut;
                globalObject.port = comPort;
            }

            if (configReader.portType == PortType.Ethernet)  //not supported yet
            {
                // blah blah
                //globalData.port = ethPort
            }

            if (configReader.portType == PortType.Test)
            {
                ModbusRtuTestPort testPort = new ModbusRtuTestPort();
                testPort.address = 1;
                
                globalObject.port = testPort;
            }
            globalObject.port.globalObject = globalObject; //it's awesome!
            globalObject.port.open();

            globalObject.timeOut = configReader.timeOut;

            globalObject.csvLogger.fileName = configReader.csvFileName;
            globalObject.csvLogger.separator = configReader.csvSeparator;
            globalObject.csvLogger.open();

            globalObject.varList = configReader.varList; 
            globalObject.queryList = configReader.queryList;
            foreach (Query q in globalObject.queryList)
            {
                q.globalObject = globalObject;
                q.buildVarHashSet();
            }

            globalObject.protocol = new ModbusRtuProtocol();  //only Modbus RTU is supported yet
            (globalObject.protocol as ModbusRtuProtocol).deviceAddress = configReader.address;
            globalObject.protocol.globalObject = globalObject; //it's amazing!

            globalObject.mode = configReader.mode;
            if (globalObject.mode == Mode.Master)
            {
                if (configReader.period > 0)
                {
                    globalObject.timer.Interval = configReader.period;
                    globalObject.timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
                    globalObject.timer.Start();
                }
            }
            if (globalObject.mode == Mode.Slave)
            {
                globalObject.port.messageReceivedEvent += onMessageReceived_SlaveMode;
             //   globalObject.port.messageReceivedEvent += new MessageReceivedHandler(onMessageReceived_SlaveMode);
            }

            //-------------------------------------------------------------
            //  command parse

            //------------------------------------------------------
            // команды могут вводиться вручную
            //------------------------------------------------------
            // get coils <from> <to> - function 1
            // get din <from> <to> - function 2
            // get h <from> <to> - function 3
            // get <from> <to> - function 3 //то же, что и get h
            // get in <from> <to> - function 4
            // set coil <tag> - function 5
            // set h <tag> - function 6
            // set <tag> - function 6 //то же, что и set h
            // set coil <from> <to> - function 15
            // set h <from> <to> - function 16
            // set <from> <to> - function 16 //то же, что и set h
            //
            // get <varname>
            // set <varname>
            //
            // auto [period] - автоматический режим с указанным периодом
            // auto off - ручной режим
            // protocol [ascii|rtu|tcpip|simple]
            // set port [name] [, bps] [, none|odd|even|mark|space] [, none|one|two|onepointfive]
            // set eth ip [, port]
            //
            // q - выход
            //
            string command;
            do
            {
                Console.Write(">"); //prompt

                // parse command
                command = Console.ReadLine();

                if (CParser.configFileName(command, ref configFileName))
                {
                    Console.WriteLine("config = {0}", configFileName);
                }

                if (CParser.isHelpLine(command))
                {
                    Console.WriteLine(MiscUtils.help);
                }

                UInt16 tag1 = 0;
                UInt16 tag2 = 0;
                UInt16[] data = null;
                int func;

                if (CParser.isFunction(command, out func, out tag1, out tag2, out data))
                {
                    Query query = new Query();
                    query.function = func;
                    query.startTag = tag1;
                    query.tagNum = (UInt16)(tag2 - tag1 + 1);
                    query.tagData = data;
                    query.globalObject = globalObject;
                    globalObject.currentTransaction = new Transaction(query, globalObject);                
                }
                //-----------------------------
                // storage get/set commands
                Space space = Space.Coils;
                if (CParser.isGetDbTags(command, ref space, ref tag1, ref tag2))
                { 
                    for(int tag = tag1; tag <= tag2; tag++)  //tag имеет тип int, а не UInt16, так как иначе неправильно работает цикл с tag2 = 65535
                    {
                        String s1 = space.ToString();
                        String s2 = tag.ToString();
                        String s3 = globalObject.tagStorage.getTag(new ModbusAddress(space, (UInt16)tag)).ToString();
                        Console.WriteLine(String.Format("{0} {1} = {2}", s1, s2, s3));
                    }
                }
                UInt16[] dbTagData = null;
                if (CParser.isSetDbTags(command, ref space, ref tag1, ref tag2, ref dbTagData))
                {
                    for (int tag = tag1; tag <= tag2; tag++)  //tag имеет тип int, а не UInt16, так как иначе неправильно работает цикл с tag2 = 65535
                    {
                        String s1 = space.ToString();
                        String s2 = tag.ToString();
                        globalObject.tagStorage.setTag(new ModbusAddress(space, (UInt16)tag), dbTagData[tag - tag1]);
                        String s3 = dbTagData[tag - tag1].ToString();
                        Console.WriteLine(String.Format("set {0} {1} = {2}", s1, s2, s3));
                    }
                }
                if (CParser.isGetDbVariables(command))
                {
                    foreach(Var variable in globalObject.varList)
                    {
                        Console.WriteLine("{0} {1} {2}", variable.name, variable.varType, variable.stringValue);
                    }
                }

                string varName;
                if (CParser.isGetDbValue(command, out varName))
                {
                    Var v = globalObject.findVar(varName);
                    if (v != null)
                        Console.WriteLine("{0} {1}", varName, v.stringValue);
                }

                double value;
                if (CParser.isSetDbValue(command, out varName, out value))
                {
                    Var v = globalObject.findVar(varName);
                    if(v != null)
                    {
                        if (v.setValue(value))
                        {
                            Console.WriteLine("{0} = {1}", v.name, v.stringValue);
                        }
                        else
                        {
                            Console.WriteLine("error occured");
                        }
                    }
                }
                //-----------------------------
                // get/set mode command
                Mode mode = Mode.Master;
                if (CParser.isGetMode(command))
                {
                    Console.WriteLine(String.Format("{0} mode", globalObject.mode));
                }
                if (CParser.isSetMode(command, ref mode))
                {
                    globalObject.mode = mode;
                    Console.WriteLine(String.Format("set {0} mode", mode));
                }
                //-----------------------------
                if (command == "p")   //pause
                {
                    globalObject.timer.Stop();
                }

                if (command == "r") //resume
                {
                    globalObject.timer.Start();
                }

                //if current transaction is busy, wait a little
                while (globalObject.currentTransaction != null && globalObject.currentTransaction.transactionState != TransactionState.Idle)
                {
                    //  Application.DoEvents();
                    System.Threading.Thread.Sleep(1); //???
                }

                //and execute
              //  Query query = new Query();
                //...
            }
            while (command != "q");
            globalObject.port.close();
        }
    }
}
