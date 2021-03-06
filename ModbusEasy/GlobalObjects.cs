﻿using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Timers;
using TransactionClasses;
using BaseTypes;
using Protocols;
using Ports;
using Loggers;
using TagDataBase;
using CommandUtils;

namespace GlobalObjects
{

    //класс задает отображение множества адресов на множество переменных
    //одной переменной может соответствовать несколько тэгов в одном адресном пространстве
    //адресу может не соответствовать переменная, тогда он не включается в словарь
    //не может быть две переменных с одинаковыми адресами или пересекающимися диапазонами адресов
    //
    //экземпляр класса создается при создании объекта транзакции (класс Transaction)
    //и нужен для быстрого поиска переменной по адресу
    //----------------------------------------------------------------
    public class AddrVarMap : Dictionary<ModbusAddress, Var>
    { 
        public void AddVar(Var var)
        {
            for (UInt16 i = var.addr.tag; i < var.addr.tag + var.numTags; i++)
            {
                ModbusAddress currAddr = new ModbusAddress(var.addr.space, i);
                if(!ContainsKey(currAddr))
                    Add(currAddr, var);
            }
        }
    }

    //класс глобального объекта, который включает в себя объекты, 
    //существующие в единственном экземпляре.
    // varList - список переменных, задаваемый в config-е
    // queryList - список запросов, задаваемый в config
    // currentTransaction - объект текущей транзакции
    // protocol - объект поддержки протокола
    // port - объект поддержки коммуникационного порта
    // cvsLogger - объект логгера, сохраняющего данные в cvs-файл
    // timer - главный таймер, задающий цикл опроса тэгов
    // timeOut - вспомогательный таймер, задающий величину тайм-аута ответа устройства

    public class GlobalObject //глобальный объект, сущ-вует в единственном экземпляре
    {
        //private fields
        private LinkedList<Var> mVarList;
        private TagStorage mTagStorage;

        public Mode mode { get; set; }

        public TagStorage tagStorage {get {return mTagStorage;}}

        //variables list
        public LinkedList<Var> varList // just link to the configReader.varList
        {
            get { return mVarList; }
            set
            {
                mVarList = value;
                foreach (Var v in mVarList)
                {
                    v.varChangedEvent += varChanged;
                }
                buildAddrVarMap();
            }
        }

        //query list
        public LinkedList<Query> queryList { get; set; } // является ссылкой на configReader.queryList

        public Transaction currentTransaction; // создается в Program

        public Protocol protocol; //создается в Program

        public Port port { get; set; } // создается в Program

        public CsvLogger csvLogger { get; set; } // создается здесь

        public Timer timer { get; set; } // создается здесь

        public int timeOut { get; set; } // значение заполняется в Program

        //--------------
        // отображение множества тэгов на множество переменных
        public AddrVarMap addrVarMap = new AddrVarMap(); // значение заполняется в buildAddrVarMap()
        //-------------
        // events
        //-------------
        public TransactionFinishedHandler transactonFinishedEvent { get; set; }
        //-------------
        //methods
        //-------------
        public GlobalObject()
        {
            mTagStorage = new TagStorage();
            csvLogger = new CsvLogger();
            timer = new Timer();
            mode = Mode.Test;
        }
        
        public Var findVar(string name)
        {
            foreach (Var v in mVarList)
                if (v.name == name)
                    return v;
            return null;
        }

        public void updateTags(Space space, UInt16 firstTag, UInt16[] tagData)
        {
            if (tagData != null)
            {
                for(int tag = firstTag; tag < tagData.Length + firstTag; tag++)
                    tagStorage.setTag(space, tag, tagData[tag - firstTag]);
            }
        }

        public void updateTags(Space space, UInt16 firstTag, bool[] tagData)
        {
            if (tagData != null)
            {
                for (int tag = firstTag; tag < tagData.Length + firstTag; tag++)
                    tagStorage.setTag(space, tag, tagData[tag - firstTag]? 1: 0);                
            }
        }

        public void updateVariables(Space space, UInt16 firslTag, UInt16 lastTag)
        {
            LinkedList<Var> varList = new LinkedList<Var>();
            for (UInt16 tag = firslTag; tag <= lastTag; tag++)
            {
                ModbusAddress addr = new ModbusAddress(space, tag);
                if (addrVarMap.ContainsKey(addr))
                {
                    Var v = addrVarMap[addr];
                    v.setTagValue(tag, tagStorage.getTag(space, tag));
                    varList.AddLast(v);
                }
            }
            foreach (Var v in varList)
            {
                v.varChanged();
            }
        }

        public void updateVariables(Space space, int firslTag, int lastTag)
        {
            updateVariables(space, (UInt16)firslTag, (UInt16)lastTag);
        }

        public void fillTagsFromVarList()
        {
            if (varList == null)
                return; // for test purpose its allowed use this object without the varList
             //   throw new NullReferenceException("varList is null");
            if(tagStorage == null)
                throw new NullReferenceException("tagStorage is null");
            foreach(Var v in varList)
            {
                tagStorage.setTag(v.addr, v.tag1);
                if(v.numTags >= 2)
                    tagStorage.setTag(v.addr.space, v.addr.tag + 1, v.tag2);
            }
        }

        protected void buildAddrVarMap()
        {
            addrVarMap.Clear();
            foreach (Var v in varList)
            {
                addrVarMap.AddVar(v);
            }
        }

        protected void varChanged(Var var)
        {
            if (csvLogger != null)
            {
                csvLogger.Append(var);
            }
        }
    }
}
