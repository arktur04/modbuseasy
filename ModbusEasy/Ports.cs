using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Timers;
using Debugs;
using Protocols;
using BaseTypes;
using TagDataBase;
using GlobalObjects;

namespace Ports
{
    public delegate void MessageReceivedHandler(object port, byte[] msg);

    public abstract class Port
    {
        protected byte[] recievedMsg { get; set; }
        public GlobalObject globalObject { get; set; }
        public abstract void open();
        public abstract void close();
        public virtual void send(byte[] msg)
        {
            Debug.msg("sending ", msg);
        }
        //public delegate void MessageReceivedHandler(object port, byte[] msg);
        public MessageReceivedHandler messageReceivedEvent;

        protected void messageReceived()
        {
            if (messageReceivedEvent != null)
            {
                messageReceivedEvent(this, recievedMsg);
            }
        }
    }

    class ComPort : Port, IDisposable
    {
        SerialPort serialPort;
        Queue<byte> dataQueue = new Queue<byte>();
        Timer recieveTimeOutTimer = new Timer();
        public double recieveTimeOut
        {
            set
            {
                if(value > 0)
                    recieveTimeOutTimer.Interval = value;
            }
        }

        //com port properties
        public string name{ set; get; }
        public int bps { get; set; }
        public Parity parity { get; set; }
        public StopBits stopBits { get; set; }

        //methods
        public ComPort(string name, int bps, Parity parity, StopBits stopBits)
        {
            //debug info
            Console.WriteLine(name.ToString());
            Console.WriteLine(bps.ToString());
            Console.WriteLine(parity.ToString());
            Console.WriteLine(stopBits.ToString());

            this.name = name;
            this.bps = bps;
            this.parity = parity;
            this.stopBits = stopBits;
            recieveTimeOutTimer = new Timer();
            recieveTimeOutTimer.AutoReset = false;
            recieveTimeOutTimer.Elapsed += recieveTimeOut_Elapsed;
        }

        public override void send(byte[] msg)
        {
            try
            {
                serialPort.Write(msg, 0, msg.Length);
            }
            catch { };
        }

        public override void open()
        {
            try
            {
                serialPort = new SerialPort(name, bps, parity, 8, stopBits);
                serialPort.DataReceived += serialPort_DataReceived;
                serialPort.Open();
            }
            catch { }
        }

        public override void close()
        {
            serialPort.Close();
        }

        void serialPort_DataReceived(object s, SerialDataReceivedEventArgs e)
        {
            byte[] data = new byte[serialPort.BytesToRead];
            serialPort.Read(data, 0, data.Length);

            foreach (byte b in data)
            {
                dataQueue.Enqueue(b);
            }

            recieveTimeOutTimer.Start();
        }

        void recieveTimeOut_Elapsed(object sender, ElapsedEventArgs e)
        {
            recievedMsg = dataQueue.ToArray();
            messageReceived();
            dataQueue.Clear();
        }

        public void Dispose()
        {
            if (serialPort != null)
                serialPort.Dispose();
        }
    }

    public class TestPort : Port
    {
        public TestPort()
        {
            Console.WriteLine("test port");
        }

        public override void send(byte[] msg)
        {
            base.send(msg);
            recievedMsg = new byte[msg.Length];
            msg.CopyTo(recievedMsg, 0);
            messageReceived();
        }

        public override void open() {}
        public override void close() {}
    }

    class ModbusRtuTestPort : Port
    {
        //адрес устройства
        public int address {get; set;}
 
        //переменная определяет, происходит ли прямой вызов Receive из метода Send значение (true), 
        //либо асинхронный вызов (false)
        public bool sinchronousMode;

        public ModbusRtuTestPort()
        {
            address = 1;
            Console.WriteLine("test port");
        }

        public override void send(byte[] msg)
        {
            base.send(msg);
            recievedMsg = new byte[msg.Length];
            //msg.CopyTo(receivedMsg, 0);
            recievedMsg = globalObject.protocol.buildResponse(msg);
            //receivedMsg = buildResponse(msg);
            messageReceived();
        }

        public override void open() { }
        public override void close() { }
    }
}
