using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;

namespace BaseTypes
{
    //режим работы:
    // Master - modbus master
    // Slave - mock modbus device
    // Test - работает, как master, при этом все запросы направляются к имитатору modbus device

    public enum Mode { Master, Slave, Test}

    public enum VarType { Float, UInt16, UInt32, Int16, Int32 }
    
    public enum Space { Coils, DiscreteInputs, AnalogInputs, Holdings }

    public enum VarState { OK, TimeOut, Error, NotAssigned }

    class Converter
    {
        static public UInt32 tagsToUInt32(UInt16 tagHi, UInt16 tagLo)
        {
            return (UInt32)((tagHi << 16) | tagLo);
        }

        static public Int32 tagsToInt32(UInt16 tagHi, UInt16 tagLo)
        {
            return (Int32)((tagHi << 16) | tagLo);
        }

        static public unsafe float tagsToFloat(UInt16 tagHi, UInt16 tagLo)
        {
            UInt32 data = tagsToUInt32(tagHi, tagLo);
            return *((float*)&data);
        }

        static public UInt16 bytesToUInt16(byte byteHi, byte byteLo)
        { 
            return (UInt16)((byteHi << 8) | byteLo);
        }
    }
/*
    public struct ModbusAddress
    {
        public Space space;
        public UInt16 tag;
        public ModbusAddress(Space space, UInt16 tag)
        {
            this.space = space;
            this.tag = tag;
        }
    }
 * */
   
    public class ModbusAddress
    {
        public Space space;// { get; set; }
        public UInt16 tag;//{ get; set; }

        public ModbusAddress()
        {
            this.space = Space.Holdings;
            this.tag = 0;
        }

        public ModbusAddress(Space space, UInt16 tag)
        {
            this.space = space;
            this.tag = tag;
        }
        
        public static bool operator ==(ModbusAddress a, ModbusAddress b)
        {
            if ((object)a == null && (object)b == null)
                return true;
            if ((object)a == null || (object)b == null)
                return false;
            return (a.space == b.space) && (a.tag == b.tag);
        }

        public static bool operator !=(ModbusAddress a, ModbusAddress b)
        {
            if ((object)a == null && (object)b == null)
                return false;
            if ((object)a == null || (object)b == null)
                return true;
            return (a.space != b.space) || (a.tag != b.tag);
        }

        public override bool Equals(object obj)
        {
            try
            {
                return this == (ModbusAddress)obj;
            }
            catch { }
            return false;
        }

        public override int GetHashCode()
        {
            return ((Int32)space << 16) | (Int32)tag;
        } 

    }

    public delegate void VarChangedHandler(Var var);

    public class Var
    {
        public string name { get; set; }
        public VarType varType { get; set; }
        public ModbusAddress addr { get; set; } // space and tag number
        //public Space space { get; set; }
        public int numTags { get { return getNumTags(); } } // quantity of tags
        public VarState state { get; set; }
        //------------------------------
        // переделать на private?
        public UInt16 tag1 { get; set; }
        public UInt16 tag2 { get; set; }

        private bool tag1Changed;
        private bool tag2Changed;
        //------------------------------
        public UInt16 UInt16Value { get { return tag1; } }
        public Int16 Int16Value { get { return (Int16)tag1; } }
        public UInt32 UInt32Value { get { return Converter.tagsToUInt32(tag1, tag2); } }
        public Int32 Int32Value { get { return Converter.tagsToInt32(tag1, tag2); } }
        public float FloatValue { get { return Converter.tagsToFloat(tag1, tag2); } }

        public string StringValue { get { return strValue(); } }
        //-------------
        // events
        //-----------------
        public VarChangedHandler varChangedEvent { get; set; }

        //-------------------
        //methods
        //--------------
        public Var()
        {
            addr = new ModbusAddress();
            state = VarState.NotAssigned;
        }

        public Var(string name, VarType varType, ModbusAddress addr)
        {
            this.name = name;
            this.varType = varType;
            this.addr = addr;
            state = VarState.NotAssigned;
        }

        //функция используется при декодировании сообщения, устанавливает значения тэгов переменной
        //Для того, чтобы обновить значение переменной, необходимо после заполнения всех тэгов 
        //вызвать метод varChanged()
        //
        public void setTagValue(UInt16 tag, UInt16 value)
        {
            if (tag == addr.tag)
            {
                tag1 = value;
                tag1Changed = true;
            }
            else
                if (numTags > 1 && tag == addr.tag + 1)
                {
                    tag2 = value;
                    tag2Changed = true;
                }
                else
                {
                    throw new ArgumentException("Invalid tag access attemption");
                }
        }

        //эта функция объявлена как public, а не как protected, так как она 
        // вызывается из других модулей (при декодировании modbus-запроса).
        // Пока апдейт не вызывается вручную при изменении значения тэгов
        // Это связано с тем, что переменная имеет два тэга, и не нужно,
        // чтобы она обновляла лог дважды при изменении значений тэгов
        // 
        //если переменная имеет размер 2 тэга, и они не обновлены, то переменная принимает состояние NotAssigned
        //для переменной размером 1 тэг достаточно обновить только его
        public void varChanged()
        {
            if (tag1Changed && ( numTags == 1 || ( numTags == 2 && tag2Changed)))
            {
                state = VarState.OK;
            }
            else
            {
                state = VarState.NotAssigned;
            }
            tag1Changed = false;
            tag2Changed = false;

            if (varChangedEvent != null)
            {
                varChangedEvent(this);
            }
        }

        private string strValue()
        {
            switch (varType)
            {
                case VarType.Float: return FloatValue.ToString();
                case VarType.UInt16: return Int16Value.ToString();
                case VarType.UInt32: return UInt32Value.ToString();
                case VarType.Int16: return Int16Value.ToString();
                case VarType.Int32: return Int32Value.ToString();
                default: return "";
            }
        }

        private int getNumTags()
        {
            switch (varType)
            {
                case VarType.Float: return 2;
                case VarType.UInt16: return 1;
                case VarType.UInt32: return 2;
                case VarType.Int16: return 1;
                case VarType.Int32: return 2;
                default: return 1;
            }
        }
    }
}
