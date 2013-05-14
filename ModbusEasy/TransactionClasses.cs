using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Timers;
using BaseTypes;
using Protocols;
using Ports;
using GlobalObjects;
using Debugs;

namespace TransactionClasses
{
    public enum TransactionState { Idle, Busy };

    public delegate void TransactionFinishedHandler(Transaction transaction);

    public class Transaction
    {
        Query mQuery;
        byte[] mMsg;
        Timer mTimeOutTimer;
        GlobalObject mGlobalObject;

        public Query query { get { return mQuery; } }
        public TransactionState transactionState { get; set; }

        public static int currId;
        public int id { get; set; }
        //------------------
        //events
        //------------------
        public TransactionFinishedHandler transactonFinishedEvent { get; set; }
        //-------------------
        // methods
        //-------------------
        public Transaction(Query query, GlobalObject globalObject)
        {
            currId++;
            id = currId;
            transactionState = TransactionState.Busy;
            //The transaction object is created from a query object and a time out value
            //also the link to the Globaldata object is created
            mQuery = query;
            //-----------
            Debug.msg("new transaction", id, " query = ", mQuery);
            //-----------
            mGlobalObject = globalObject;
            //build the protocol message from the query
            mMsg = mGlobalObject.protocol.buildMessage(query);
            transactonFinishedEvent += globalObject.transactonFinishedEvent;
            //start timeOut
            mTimeOutTimer = new Timer(mGlobalObject.timeOut);
            mTimeOutTimer.AutoReset = false;
            mTimeOutTimer.Elapsed += new ElapsedEventHandler(onTimeOutElapsed);
            mTimeOutTimer.Start();
            //send the message
            mGlobalObject.port.messageReceivedEvent = new MessageReceivedHandler(onMessageReceived_MasterMode);
            mGlobalObject.port.send(mMsg);
        }

        protected void setVarsState(VarState state)
        {
            if(mGlobalObject != null && mGlobalObject.addrVarMap != null)
            {
                Space space = ModbusProtocol.getSpace(mQuery.function);

                for (UInt16 i = mQuery.startTag; i < mQuery.startTag + mQuery.tagNum; i++)
                {
                    ModbusAddress currAddr = new ModbusAddress(space, i);
                    if (mGlobalObject.addrVarMap.ContainsKey(currAddr))
                    {
                        mGlobalObject.addrVarMap[currAddr].state = VarState.TimeOut;
                    }
                }
            }        
        }

        void onTimeOutElapsed(object sender, ElapsedEventArgs e)
        {
           // if ((sender as Timer).Enabled) //костыль, нужный потому, что этот обработчик вызывается, даже если после запуска таймера Enabled установлено в false.
           // {
                Debug.msg("time out elapsed on trans. ", id);
             
                setVarsState(VarState.TimeOut);

                transactionState = TransactionState.Idle;
                if (transactonFinishedEvent != null)
                {
                    transactonFinishedEvent(this);
                }
           // }
        }

        void onMessageReceived_MasterMode(object sender, byte[] msg)
        {
            Debug.msg("message received. ", id);
            Debug.msg("msg: ", msg);

            switch (mGlobalObject.protocol.decode(msg, mQuery, new Response(mQuery)))
            {
                case ProtocolError.OK:
                    setVarsState(VarState.OK);
                    if (mTimeOutTimer != null)
                    {
                        mTimeOutTimer.Stop();
                        mGlobalObject.currentTransaction.transactionState = TransactionState.Idle;
                    }
                    break;
                case ProtocolError.CRCError:
                case ProtocolError.DataLengthError:
                case ProtocolError.MsgLengthError:
                case ProtocolError.UnexpectedError:
                    setVarsState(VarState.Error);
                    if(mTimeOutTimer != null)
                    {
                        mTimeOutTimer.Stop();
                        mGlobalObject.currentTransaction.transactionState = TransactionState.Idle;
                    }
                    break;
                case ProtocolError.AddressMismatch: break;  //some strange thing has happened... message with a wrong address has received
            }

            //mGlobalObject.updateTagRange(mQuery.startTag, mQuery.tagNum);
            mQuery.updateVars();

            transactionState = TransactionState.Idle;

            if (transactonFinishedEvent != null)
            {
                transactonFinishedEvent(this);
            }
        }
    }

    public class Query
    {
        public int function { get; set; }
        //начальный тэг запроса и количество тэгов в запросе
        public UInt16 startTag { get; set; }
        public UInt16 tagNum { get; set; }

        public UInt16[] tagData { get; set; }

        private HashSet<Var> mVarSet = new HashSet<Var>(); //множество переменных, значения которых используются в транзакции
        public HashSet<Var> varSet { get { return mVarSet; } set { mVarSet = value; } }
        public GlobalObject globalObject { get; set; }

        public Query()
        {
            function = 3;
            startTag = 0;
            tagNum = 1;
        }

        public void buildVarHashSet()
        {
            for(UInt16 i = startTag; i < startTag + tagNum; i++)
            {
                Space space = ModbusProtocol.getSpace(function);
                ModbusAddress addr = new ModbusAddress(space, i);
                if(globalObject.addrVarMap.ContainsKey(addr))
                {
                    mVarSet.Add(globalObject.addrVarMap[addr]);
                }
            }
        }

        public void updateVars()
        {
            foreach (Var v in mVarSet)
            {
                v.varChanged();
            }
        }
    }
        
    public class Response
    {
        Query mQuery;

        byte[] data;
        int dataSize;
        //public int function { get; set; }
        /*
        public int startTag { get; set; }
        public int finishTag { get; set; }
        public int tag { get; set; }
        */
      //  public LinkedList<Var> varList { get; set; } //ссылка на глобальный VarList, здесь экземпляр класса не создается
       // private HashSet<Var> mVarSet { get; set; } //множество переменных, значения которых используются в транзакции, копируется из Query

        public Response(Query query)
        {
            mQuery = query;

           // mVarSet = query.mVarSet;
            // function = query.function;
        }
        /*
        public void fillVarSet()
        {
        }
         */
    }
}
