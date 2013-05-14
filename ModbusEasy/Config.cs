using System.Xml;
using System.IO;
using System.Text;
using System;
using System.IO.Ports;
using System.Collections.Generic;
using BaseTypes;
using TransactionClasses;
using Debugs;

namespace Config
{
    enum PortType { ComPort, Ethernet, Test };

    class ConfigReader
    {
        //private members
        //port 
        private PortType mPortType;

        private string mPortName;
        private int mBps;
        private Parity mParity;
        private StopBits mStopBits;
        private double mComPortTimeOut;

        //common
        int mAddress;
        int mPeriod;
        int mTimeOut;
        Mode mMode;

        //saving
        string mCsvFileName;
        string mCsvSeparator;

        //variables list
        public LinkedList<Var> varList { get; set; }

        //query list
        public LinkedList<Query> queryList { get; set; }

        //properties
        public PortType portType { get { return mPortType; } }
        public string portName { get { return mPortName; } }
        public int bps { get { return mBps; } }
        public Parity parity { get { return mParity; } }
        public StopBits stopBits { get { return mStopBits; } }
        public double comPortTimeOut { get { return mComPortTimeOut; } }
        public int address { get { return mAddress; } }
        public int period { get { return mPeriod; } }
        public int timeOut { get { return mTimeOut; } }
        public Mode mode { get { return mMode; } } 
        public string csvFileName { get { return mCsvFileName; } }
        public string csvSeparator { get { return mCsvSeparator; } }

        //methods
        public ConfigReader()
        {
            varList = new LinkedList<Var>();
            queryList = new LinkedList<Query>();

            mPortType = PortType.ComPort; // default port type
            //default settings for a serial port
            mPortName = "COM1";
            mBps = 19200;
            mParity = Parity.None;
            mStopBits = StopBits.One;
            mComPortTimeOut = 0;

            mPeriod = 1000; //the period of the main timer in ms
            mTimeOut = 200;
            mMode = Mode.Master;

            mCsvFileName = "saving.csv";
            mCsvSeparator = ", ";
        }

        public void loadFromFile(string fileName)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(fileName);
                try
                {
                    XmlNode n = doc.SelectSingleNode("config/port");
                    switch (n.Attributes["type"].Value)
                    {
                        case "comport": mPortType = PortType.ComPort;
                            break;
                        case "ethernet": mPortType = PortType.Ethernet;
                            break;
                        case "test": mPortType = PortType.Test;
                            break;
                    };
                }
                catch { }

                if (mPortType == PortType.ComPort)
                {
                    try
                    {
                        mPortName = doc.SelectSingleNode("config/port/portname").InnerText;
                    }
                    catch { }

                    try
                    {
                        mBps = Convert.ToInt32(doc.SelectSingleNode("config/port/bps").InnerText);
                    }
                    catch { }

                    try
                    {
                        switch (doc.SelectSingleNode("config/port/parity").InnerText)
                        {
                            case "none": mParity = Parity.None;
                                break;
                            case "odd": mParity = Parity.Odd;
                                break;
                            case "even": mParity = Parity.Even;
                                break;
                            case "mark": mParity = Parity.Mark;
                                break;
                            case "space": mParity = Parity.Space;
                                break;
                        };
                    }
                    catch { }

                    try
                    {
                        switch (doc.SelectSingleNode("config/port/stopbit").InnerText)
                        {
                            case "none": mStopBits = StopBits.None;
                                break;
                            case "one": mStopBits = StopBits.One;
                                break;
                            case "two": mStopBits = StopBits.Two;
                                break;
                            case "onepointfive": mStopBits = StopBits.OnePointFive;
                                break;
                        }
                    }
                    catch { }

                    try
                    {
                        mComPortTimeOut = Convert.ToDouble(doc.SelectSingleNode("config/port/comtimeout").InnerText);
                    }
                    catch { }
                }
                //----------------------------------
                // common properties
                //----------------------------------
                try
                {
                    mAddress = Convert.ToInt32(doc.SelectSingleNode("config/common/devaddress").InnerText);
                }
                catch { }

                try
                {
                    mPeriod = Convert.ToInt32(doc.SelectSingleNode("config/common/period").InnerText);
                }
                catch { }

                try
                {
                    mTimeOut = Convert.ToInt32(doc.SelectSingleNode("config/common/timeout").InnerText);
                }
                catch { }

                try
                {
                    switch (doc.SelectSingleNode("config/common/mode").InnerText)
                    {
                        case "master": mMode = Mode.Master;
                            break;
                        case "slave": mMode = Mode.Slave;
                            break;
                        case "test": mMode = Mode.Test;
                            break;
                    }
                }
                catch { }
                //----------------------------------
                // saving
                //----------------------------------
                try
                {
                    mCsvFileName = doc.SelectSingleNode("config/saving/csv").Attributes["filename"].Value;
                }
                catch { }
                try
                {
                    mCsvSeparator = doc.SelectSingleNode("config/saving/csv").Attributes["separator"].Value;
                }
                catch { }
                //----------------------------------
                // var list
                //----------------------------------
                try
                {
                    XmlNodeList nl = doc.SelectNodes("config/varlist/var");
                    foreach (XmlNode n in nl)
                    {
                        Var v = new Var();
                        try
                        {
                            v.name = n.Attributes["name"].Value;
                        }
                        catch { }

                        try
                        {
                            //Float, UInt16, UInt32, Int16, Int32
                            switch(n.Attributes["type"].Value)
                            {
                                case "float": v.varType = VarType.Float;
                                    break;
                                case "int16": v.varType = VarType.Int16;
                                    break;
                                case "int32": v.varType = VarType.Int32;
                                    break;
                                case "uint16": v.varType = VarType.UInt16;
                                    break;
                                case "uint32": v.varType = VarType.UInt32;
                                    break;
                            }
                        }
                        catch { }

                        try
                        {
                            v.addr.tag = Convert.ToUInt16(n.Attributes["tag"].Value);
                        }
                        catch {}

                        //Coils, DiscreteInputs, Inputs, Holdings
                        try
                        {
                            switch(n.Attributes["space"].Value)
                            {
                                case "coils": v.addr.space = Space.Coils;
                                    break;
                                case "discreteinputs": v.addr.space = Space.DiscreteInputs;
                                    break;
                                case "analoginputs": v.addr.space = Space.AnalogInputs;
                                    break;
                                case "holdings": v.addr.space = Space.Holdings;
                                    break;
                            }
                        }
                        catch { }

                        if(varList != null)
                        {
                            varList.AddLast(v);
                        }
                    }
                }
                catch { }

                //----------------------------------
                // query list
                //----------------------------------
                try
                {
                    XmlNodeList nl = doc.SelectNodes("config/querylist/query");
                    foreach (XmlNode n in nl)
                    {
                        Query q = new Query();
                        try
                        {
                            q.function = Convert.ToInt32(n.Attributes["function"].Value);
                        }
                        catch { }

                        try
                        {
                            q.startTag = Convert.ToUInt16(n.Attributes["start"].Value);
                        }
                        catch { }

                        try
                        {
                            q.tagNum = Convert.ToUInt16(n.Attributes["tagnum"].Value);
                        }
                        catch { }

                        // функции 5, 6 используют данные одного тэга, записанные в атрибуте data, например:
                        //     <query function="6" start="2000" value="1234"/>
                        if (q.function == 5 || q.function == 6)
                        {
                            try
                            {
                                q.tagData = new UInt16[1];
                                q.tagData[0] = Convert.ToUInt16(n.Attributes["value"].Value);
                            }
                            catch { }
                        }

                        //функции 15, 16 используют данные в виде списка, например:
                        // <query function="16" start="2000" tagnum="4">
                        //    <data tag="2000" value="2001"/>
                        //    <data tag="2001" value="2002"/>
                        //    <data tag="2002" value="2003"/>
                        //    <data tag="2003" value="2004"/>
                        // </query>
                        if (q.function == 15 || q.function == 16)
                        {
                            try
                            {
                                XmlNodeList dataNodes = n.SelectNodes("data");
                                q.tagData = new UInt16[dataNodes.Count];
                                foreach (XmlNode dataNode in dataNodes)
                                {
                                    try
                                    {
                                        int dataTag = Convert.ToInt32(dataNode.Attributes["tag"].Value);

                                        if (dataTag >= q.startTag && dataTag < (q.startTag + q.tagNum))
                                        {
                                            q.tagData[dataTag - q.startTag] = Convert.ToUInt16(dataNode.Attributes["value"].Value);
                                        }
                                        else
                                        {
                                            Debug.msg("tag number does not lay in the range of the query tags");
                                        }
                                    }
                                    catch { }
                                }

                                if (q.tagNum != dataNodes.Count)
                                {
                                    Debug.msg("Mismatch tagnum");
                                    throw new InvalidDataException("Mismatch tagnum");
                                }
                            }
                            catch { }
                        }

                        if (queryList != null)
                        {
                            queryList.AddLast(q);
                        }
                    }
                }
                catch { }
            }
            catch
            {
                throw new FileLoadException();
            }
        }
    }
}
