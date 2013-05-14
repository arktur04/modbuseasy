using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using BaseTypes;
using TransactionClasses;
using Debugs;
using GlobalObjects;

namespace Protocols
{
    public enum ProtocolError { OK, CRCError, MsgLengthError, UnexpectedError, AddressMismatch, DataLengthError, WrongData, UnsupportedFunction }

    public abstract class Protocol
    {
        public Query query { get; set; }

        public abstract byte[] buildMessage(Query query);
        public abstract ProtocolError decode(byte[] msg, Query query, Response response);
        public abstract byte[] buildResponse(byte[] msg);
        public GlobalObject globalObject;
    }

    public abstract class ModbusProtocol : Protocol
    {
        private HashSet<int> mFuncSet;
        protected HashSet<int> FuncSet { get { return mFuncSet; } }

        public bool validateData { get; set; } //check if response data in the functions 5, 6, 15, 16 match a query.
        public ModbusProtocol()
        {
            mFuncSet = new HashSet<int>(new int[] { 1, 2, 3, 4, 5, 6, 15, 16 });
            validateData = true;
        }
        //---------------------------------------
        // Статическая функция, возвращает значение Space по номеру функции Modbus
        // 
         public static Space getSpace(int func)
         {
            switch(func)
            {
                case 1: return Space.Coils;
                case 2: return Space.DiscreteInputs;
                case 3: return Space.Holdings;
                case 4: return Space.AnalogInputs;
                case 5: return Space.Coils;
                case 6: return Space.Holdings;
                case 15: return Space.Coils;
                case 16: return Space.Holdings;
            }
            throw new ArgumentOutOfRangeException("unsupported function is requested");
         }
    }

    public class ModbusRtuProtocol : ModbusProtocol
    {
        public int deviceAddress { get; set; }

        public override byte[] buildMessage(Query query)
        {
            UInt16 crc;
            byte[] result;

            if (query != null)
            {
                switch (query.function)
                {
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                        result = new byte[8];
                        result[0] = (byte)deviceAddress;
                        result[1] = (byte)query.function; //function
                        result[2] = (byte)(query.startTag >> 8); //start hi
                        result[3] = (byte)query.startTag; //start lo
                        result[4] = (byte)(query.tagNum >> 8); //num of tags hi
                        result[5] = (byte)query.tagNum; //num of tags lo
                        crc = calculateCrc(result, 6);
                        result[6] = (byte)(crc >> 8);
                        result[7] = (byte)crc;
                        return result;
                    case 5:
                        if (query.tagData.Length > 0)
                        {
                            result = new byte[8];
                            result[0] = (byte)deviceAddress;
                            result[1] = (byte)query.function; //function
                            result[2] = (byte)(query.startTag >> 8); //start hi
                            result[3] = (byte)query.startTag; //start lo
                            result[4] = (query.tagData[0] != 0) ? (byte)0xFF : (byte)0x00; //data
                            result[5] = 0x00; //data
                            crc = calculateCrc(result, 6);
                            result[6] = (byte)(crc >> 8);
                            result[7] = (byte)crc;
                            return result;
                        }
                        else
                            return null;  //если нет данных в query.data, возвращаем null
                    case 6:
                        if (query.tagData.Length > 0)
                        {
                            result = new byte[8];
                            result[0] = (byte)deviceAddress;
                            result[1] = (byte)query.function; //function
                            result[2] = (byte)(query.startTag >> 8); //start hi
                            result[3] = (byte)query.startTag; //start lo
                            result[4] = (byte)(query.tagData[0] >> 8);
                            result[5] = (byte)(query.tagData[0]);
                            crc = calculateCrc(result, 6);
                            result[6] = (byte)(crc >> 8);
                            result[7] = (byte)crc;
                            return result;
                        }
                        else
                            return null;
                    case 15:
                        //-------------------------------------------------------------
                        // данные для coil упакованы в байты по 8 регистров, 
                        // таким образом, длина данных может быть найдена как (dataLen + 7) / 8,
                        // где dataLen - количество регистров
                        // исходные данные находятся в query.data[], состоянию OFF соответствует нулевое значение, 
                        // ON - ненулевое значение
                        if (query.tagData.Length >= query.tagNum)
                        {
                            int coilDataLen = (query.tagNum + 7) / 8;
                            result = new byte[9 + coilDataLen];
                            result[0] = (byte)deviceAddress;
                            result[1] = (byte)query.function; //function
                            result[2] = (byte)(query.startTag >> 8); //start hi
                            result[3] = (byte)query.startTag; //start lo
                            result[4] = (byte)(query.tagNum >> 8); //no of tags hi
                            result[5] = (byte)query.tagNum; //no of tags lo
                            result[6] = (byte)coilDataLen;

                            byte[] coilData = new byte[coilDataLen];
                            for (int i = 0; i < query.tagNum; i++)
                            {
                                coilData[i / 8] |= (byte)((((query.tagData[i]) != 0) ? 1 : 0) << (i % 8));
                            }
                            for (int i = 0; i < coilDataLen; i++)
                            {
                                result[i + 7] = coilData[i]; //data
                            }
                            crc = calculateCrc(result, coilDataLen + 6);
                            result[coilDataLen + 7] = (byte)(crc >> 8);
                            result[coilDataLen + 8] = (byte)crc;
                            return result;
                        }
                        else
                            return null;
                    case 16:
                        if (query.tagData.Length >= query.tagNum)
                        {
                            int holdingsDataLen = query.tagNum * 2; //длина данных в байтах
                            result = new byte[8 + holdingsDataLen];
                            result[0] = (byte)deviceAddress;
                            result[1] = (byte)query.function; //function
                            result[2] = (byte)(query.startTag >> 8); //start hi
                            result[3] = (byte)query.startTag; //start lo
                            result[4] = (byte)(query.tagNum >> 8); //no of tags hi
                            result[5] = (byte)query.tagNum; //no of tags lo

                            byte[] holdingsData = new byte[holdingsDataLen];
                            result[6] = (byte)holdingsDataLen;

                            for (int i = 0; i < query.tagNum; i++)
                            {
                                holdingsData[i * 2] = (byte)(query.tagData[i] >> 8);
                                holdingsData[i * 2 + 1] = (byte)query.tagData[i];
                            }
                            for (int i = 0; i < holdingsDataLen; i++)
                            {
                                result[i + 6] = holdingsData[i]; //data
                            }
                            crc = calculateCrc(result, holdingsDataLen + 6);
                            result[holdingsDataLen + 6] = (byte)(crc >> 8);
                            result[holdingsDataLen + 7] = (byte)crc;
                            return result;
                        }
                        else
                          return null;
                    default:
                        Debug.msg("The function is not supported");
                        return null;
                }
            }
            else
            {
                return null; // function is not supported
            }
            //...

        }

        private bool validateFunction(int function)
        {
            return FuncSet.Contains(function);
        }

        public override ProtocolError decode(byte[] msg, Query query, Response response)
        {
            this.query = query;
            ProtocolError protocolError = validateResponse(msg);
            if (protocolError == ProtocolError.OK)
            {
                if (response != null)
                {
                    int function = msg[1];
                    UInt16[] tagValues = new UInt16[0];
                    switch (function)
                    { 
                        case 1:
                        case 2:
                            tagValues = new UInt16[query.tagNum];
                            for (int i = 0; i < query.tagNum - 1; i++)
                            {
                                tagValues[i] = (UInt16)((msg[3 + i / 8] & (0x01 << i % 8)) != 0 ? 1 : 0); 
                            }
                            break;
                        case 3:
                            tagValues = new UInt16[query.tagNum];
                            for (int i = 0; i < query.tagNum - 1; i++)
                            {
                                tagValues[i] = (UInt16)(((UInt16)msg[3 + i * 2] << 8) | (UInt16)msg[4 + i * 2]); 
                            }
                            break;
                        case 4:
                            tagValues = new UInt16[1];
                            tagValues[0] = (UInt16)(msg[3] << 8 | msg[4]);
                            break;
                        case 5:
                            tagValues = new UInt16[1];
                            tagValues[0] = (UInt16)((msg[4] != 0) ? 1 : 0);
                            break;
                        case 6:
                            tagValues = new UInt16[1];
                            tagValues[0] = (UInt16)((UInt16)msg[4] << 8 | (UInt16)msg[5]);
                            break;
                        case 15:  //no data in the response
                        case 16:  //no data in the response
                            break;
                    }

                    if (function >= 1 && function <= 6)
                    {
                        LinkedList<Var> varList = new LinkedList<Var>();
                        Space space = ModbusProtocol.getSpace(function);
                        for (UInt16 tag = query.startTag; tag < query.startTag + query.tagNum; tag++)
                        {
                            ModbusAddress addr = new ModbusAddress(space, tag);
                            if (globalObject.addrVarMap.ContainsKey(addr))
                            {
                                Var v = globalObject.addrVarMap[addr];
                                v.setTagValue(tag, tagValues[tag - query.startTag]);
                                varList.AddLast(v);
                            }
                        }
                        foreach(Var v in varList)
                        {
                            v.varChanged();
                        }
                    }
                }
                else
                {
                    throw new AccessViolationException("response object not found!");
                }
            }
            return protocolError;
        }

        protected ProtocolError validateResponse(byte[] msg)
        {
            if (query == null)
            {
                throw new MissingMemberException("The query object not exist");
            }
            //common check
            if (msg.Length < 4)  //длина ответа не может быть < 4: минимально ответ должен содержать адрес, номер функции и CRC
                return ProtocolError.MsgLengthError;
            if (msg[0] != deviceAddress)
                return ProtocolError.AddressMismatch;
            if (msg[1] != query.function)
                return ProtocolError.UnexpectedError;
            if (modbusRtuCrc(msg) == ProtocolError.CRCError)
                return ProtocolError.CRCError;
            // function 1 - 4 check
            if (msg[1] >= 1 && msg[1] <= 4)
            {
                if (!(msg[2] > 0 && msg.Length == (msg[2] + 5))) //кол-во байт данных в ответе должно быть больше 0, длина сообщения должна быть msg[2] + 5
                    return ProtocolError.MsgLengthError;
                //func. 1-4
                //[0] - address
                //[1] - function
                //[2] - byte count
                //[...] - data
                //[len - 2] crc
                //[len - 1] crc
                if ((msg[1] == 1 || msg[2] == 2) && (msg[2] * 8) < query.tagNum) //function 1, 2 (read coils, read discrete inputs); data are packed in bytes, 8 tags/byte
                    return ProtocolError.DataLengthError;  //количество байт в ответе не соответствует количеству запрошенных тэгов
                if ((msg[1] == 3 || msg[1] == 4) && msg[2] < (query.tagNum * 2)) //function 3, 4 (read holdings, read analog inputs); 2 byte/tag
                    return ProtocolError.DataLengthError;
                return ProtocolError.OK;
            }
            // function 5, 6, 15, 16 check
            if (msg[1] == 5 || msg[1] == 6 || msg[1] == 15 || msg[1] == 16)
            {
                //func. 5
                //[0] - address
                //[1] - function
                //      func 5, 6 | func 15           | func 16
                //[2] - addr hi   | addr hi           | addr hi
                //[3] - addr lo   | addr lo           | addr lo
                //[4] - data hi   | quantity of coils | quantity of regs
                //[5] - data lo   | quantity of coils | quantity of regs
                //[6] crc
                //[7] crc
                if (msg.Length != 8) 
                    return ProtocolError.MsgLengthError;
                if (msg[1] == 5) //function 5 - force single coil
                {
                    if (validateData)
                    {
                        if (query.tagNum != 1)
                            return ProtocolError.UnexpectedError; // the stragest case...
                        if (query.tagData.Length != 1)
                            return ProtocolError.UnexpectedError; // the stragest case...
                        if (query.tagData[0] == 0) // если был запрос на выключение выхода...
                        {
                            if (msg[4] == 0 && msg[5] == 0)
                                return ProtocolError.OK; // данные в ответе должны быть 0x00, 0x00
                            return ProtocolError.WrongData;
                        }
                        else  //если был запрос на включение входа...
                        {
                            if (msg[4] == 0xff && msg[5] == 0)
                                return ProtocolError.OK; // данные в ответе должны быть 0xff, 0x00
                            return ProtocolError.WrongData;
                        }
                    }
                    else
                        return ProtocolError.OK;
                }
                if (msg[1] == 6) //function 6 - preset single holding
                {
                    if (validateData)
                    {
                        if (query.tagNum != 1)
                            return ProtocolError.UnexpectedError; // the stragest case...
                        if (query.tagData.Length != 1)
                            return ProtocolError.UnexpectedError; // the stragest case...
                        if (query.tagData[0] == Converter.bytesToUInt16(msg[4], msg[5]))
                            return ProtocolError.OK;
                        return ProtocolError.WrongData;
                    }
                    else
                        return ProtocolError.OK;
                }
                if(msg[1] == 15 || msg[1] == 16) //function 15 - force multiple coils, function 16 - preset multiple regs;
                {
                    if (validateData)
                    {
                        if (query.tagNum < 1)
                            return ProtocolError.UnexpectedError; // the stragest case...
                        // start tag and tag number in the query qnd the answer must match.
                        if (query.startTag == Converter.bytesToUInt16(msg[2], msg[3]) && query.tagNum == Converter.bytesToUInt16(msg[4], msg[5]))
                            return ProtocolError.OK;
                        return ProtocolError.WrongData;
                    }
                    else
                        return ProtocolError.OK;
                }
            }
            return ProtocolError.UnsupportedFunction;
        }
        #region RTU CRC tables
        //------------------------------------------------------------------------
        // RTU CRC
        //------------------------------------------------------------------------
        // Table of CRC values for high–order byte 
        static byte[] auchCRCHi = {
0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81,
0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0,
0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01,
0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41,
0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81,
0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0,
0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01,
0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81,
0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0,
0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01,
0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81,
0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0,
0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01,
0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81,
0x40
};

        // Table of CRC values for low–order byte 
        static byte[] auchCRCLo = {
0x00, 0xC0, 0xC1, 0x01, 0xC3, 0x03, 0x02, 0xC2, 0xC6, 0x06, 0x07, 0xC7, 0x05, 0xC5, 0xC4,
0x04, 0xCC, 0x0C, 0x0D, 0xCD, 0x0F, 0xCF, 0xCE, 0x0E, 0x0A, 0xCA, 0xCB, 0x0B, 0xC9, 0x09,
0x08, 0xC8, 0xD8, 0x18, 0x19, 0xD9, 0x1B, 0xDB, 0xDA, 0x1A, 0x1E, 0xDE, 0xDF, 0x1F, 0xDD,
0x1D, 0x1C, 0xDC, 0x14, 0xD4, 0xD5, 0x15, 0xD7, 0x17, 0x16, 0xD6, 0xD2, 0x12, 0x13, 0xD3,
0x11, 0xD1, 0xD0, 0x10, 0xF0, 0x30, 0x31, 0xF1, 0x33, 0xF3, 0xF2, 0x32, 0x36, 0xF6, 0xF7,
0x37, 0xF5, 0x35, 0x34, 0xF4, 0x3C, 0xFC, 0xFD, 0x3D, 0xFF, 0x3F, 0x3E, 0xFE, 0xFA, 0x3A,
0x3B, 0xFB, 0x39, 0xF9, 0xF8, 0x38, 0x28, 0xE8, 0xE9, 0x29, 0xEB, 0x2B, 0x2A, 0xEA, 0xEE,
0x2E, 0x2F, 0xEF, 0x2D, 0xED, 0xEC, 0x2C, 0xE4, 0x24, 0x25, 0xE5, 0x27, 0xE7, 0xE6, 0x26,
0x22, 0xE2, 0xE3, 0x23, 0xE1, 0x21, 0x20, 0xE0, 0xA0, 0x60, 0x61, 0xA1, 0x63, 0xA3, 0xA2,
0x62, 0x66, 0xA6, 0xA7, 0x67, 0xA5, 0x65, 0x64, 0xA4, 0x6C, 0xAC, 0xAD, 0x6D, 0xAF, 0x6F,
0x6E, 0xAE, 0xAA, 0x6A, 0x6B, 0xAB, 0x69, 0xA9, 0xA8, 0x68, 0x78, 0xB8, 0xB9, 0x79, 0xBB,
0x7B, 0x7A, 0xBA, 0xBE, 0x7E, 0x7F, 0xBF, 0x7D, 0xBD, 0xBC, 0x7C, 0xB4, 0x74, 0x75, 0xB5,
0x77, 0xB7, 0xB6, 0x76, 0x72, 0xB2, 0xB3, 0x73, 0xB1, 0x71, 0x70, 0xB0, 0x50, 0x90, 0x91,
0x51, 0x93, 0x53, 0x52, 0x92, 0x96, 0x56, 0x57, 0x97, 0x55, 0x95, 0x94, 0x54, 0x9C, 0x5C,
0x5D, 0x9D, 0x5F, 0x9F, 0x9E, 0x5E, 0x5A, 0x9A, 0x9B, 0x5B, 0x99, 0x59, 0x58, 0x98, 0x88,
0x48, 0x49, 0x89, 0x4B, 0x8B, 0x8A, 0x4A, 0x4E, 0x8E, 0x8F, 0x4F, 0x8D, 0x4D, 0x4C, 0x8C,
0x44, 0x84, 0x85, 0x45, 0x87, 0x47, 0x46, 0x86, 0x82, 0x42, 0x43, 0x83, 0x41, 0x81, 0x80,
0x40
};
        #endregion

        public static ProtocolError modbusRtuCrc(byte[] msg)
        {
            try
            {
                UInt16 crc = Converter.bytesToUInt16(msg[msg.Length - 2], msg[msg.Length - 1]);// (UInt16)((msg[msg.Length - 2] << 8) | msg[msg.Length - 1]);
                if (crc == calculateCrc(msg, msg.Length - 2))
                {
                    return ProtocolError.OK;
                }
            }
            catch (IndexOutOfRangeException)
            {
                return ProtocolError.MsgLengthError;
            }
            return ProtocolError.CRCError;
        }

        public static UInt16 calculateCrc(byte[] data, int len)
        {
            byte uchCRCHi = 0xFF; // high byte of CRC initialized 
            byte uchCRCLo = 0xFF; // low byte of CRC initialized 
            int uIndex; // will index into CRC lookup table 
            int pMsg = 0; //pseudo-pointer to data byte
            for (int i = 0; i < len; i++) // pass through message buffer 
            {
                uIndex = uchCRCHi ^ data[pMsg++]; // calculate the CRC 

                uchCRCHi = (byte)(uchCRCLo ^ auchCRCHi[uIndex]);
                uchCRCLo = auchCRCLo[uIndex];
            };

            Debug.msg("crc = ", new byte[] {uchCRCHi, uchCRCLo});

            return Converter.bytesToUInt16(uchCRCHi, uchCRCLo);// (UInt16)(uchCRCHi << 8 | uchCRCLo);
        }

        public override byte[] buildResponse(byte[] msg)
        {
            // UInt16[] data;
            UInt16 tagValue;
            UInt16 startTag;
            UInt16 tagNum;
            UInt16 crc;
            byte[] response;
            Space space;
            //check if used objects are not null
            if (msg == null)
                throw new AccessViolationException("msg object is null at ModbusRtuTestPort.buildResponse");
            if(globalObject == null)
                throw new AccessViolationException("globalObject object is null at ModbusRtuTestPort.buildResponse");
            if (globalObject.tagStorage == null)
                throw new AccessViolationException("globalObject.tagStorage object is null at ModbusRtuTestPort.buildResponse");
            //check if msg has minimal length
            if (msg.Length < 4)
            {
                return new byte[0];  //wrong query - do not answer
            }
            //check address
            if (msg[0] == deviceAddress)
            {
                byte func = msg[1];
                switch (func)
                {
                    case 1:
                    case 2:
                        if (func == 1)
                        {
                            space = Space.Coils;
                        }
                        else
                        {
                            space = Space.DiscreteInputs;
                        }
                        if (msg.Length != 8)
                            return new byte[0];  //wrong query - do not answer
                        startTag = (UInt16)(msg[2] << 8 | msg[3]);
                        tagNum = (UInt16)(msg[4] << 8 | msg[5]);
                        //для упрощения не проверяем CRC
                        response = new byte[5 + (tagNum + 7) / 8];
                        response[0] = (byte)deviceAddress;
                        response[1] = func;
                        response[2] = (byte)((tagNum + 7) / 8);
                        byte b = 0; //текущее значение байта, в который упаковывается результат
                        for (UInt16 tag = startTag; tag < startTag + tagNum; tag++)
                        {
                            tagValue = globalObject.tagStorage.getTag(new ModbusAddress(space, tag));
                            if ((tag - startTag) % 8 == 0)
                            {
                                b = 0;
                            }
                            b |= (byte)((tagValue == 0) ? 0 : (1 << ((tag - startTag) % 8)));
                            if ((tag - startTag) % 8 == 7 || tag == startTag + tagNum - 1)
                            {
                                response[3 + ((tag - startTag) / 8)] = b;
                            }
                        }
                        crc = ModbusRtuProtocol.calculateCrc(response, 3 + (tagNum + 7) / 8);//вычисляем crc
                        response[3 + (tagNum + 7) / 8] = (byte)(crc >> 8);
                        response[4 + (tagNum + 7) / 8] = (byte)crc;

                        return response;
                    case 3:
                    case 4:
                        if (func == 3)
                        {
                            space = Space.Holdings;
                        }
                        else
                        {
                            space = Space.AnalogInputs;
                        };
                        if (msg.Length != 8)
                            return new byte[0];  //wrong query - do not answer
                        startTag = (UInt16)(msg[2] << 8 | msg[3]);
                        tagNum = (UInt16)(msg[4] << 8 | msg[5]);
                        //для упрощения не проверяем CRC
                        response = new byte[5 + tagNum * 2];
                        response[0] = (byte)deviceAddress;
                        response[1] = func;
                        response[2] = (byte)(tagNum * 2);
                        for (UInt16 tag = startTag; tag < startTag + tagNum; tag++)
                        {
                            tagValue = globalObject.tagStorage.getTag(new ModbusAddress(space, tag));
                            response[3 + (tag - startTag) * 2] = (byte)(tagValue >> 8);
                            response[4 + (tag - startTag) * 2] = (byte)tagValue;
                        }
                        crc = ModbusRtuProtocol.calculateCrc(response, 3 + (tagNum * 2));//вычисляем crc
                        response[3 + tagNum * 2] = (byte)(crc >> 8);
                        response[4 + tagNum * 2] = (byte)crc;

                        return response;
                    case 5:
                    case 6:
                        if (func == 5)
                        {
                            space = Space.Coils;
                        }
                        else
                        {
                            space = Space.Holdings;
                        };
                        if (msg.Length != 8)
                            return new byte[0];  //wrong query - do not answer
                        startTag = (UInt16)(msg[2] << 8 | msg[3]);
                        tagNum = 1;
                        //для упрощения не проверяем CRC
                        response = new byte[8];
                        response[0] = (byte)deviceAddress;
                        response[1] = func;
                        response[2] = (byte)(startTag >> 8);
                        response[3] = (byte)startTag;

                        globalObject.tagStorage.setTag(new ModbusAddress(space, startTag), (UInt16)(msg[4] << 8 | msg[5]));
                        tagValue = globalObject.tagStorage.getTag(new ModbusAddress(space, startTag));
                        if (space == Space.Coils) //for coils, any value != 0 should be convert to 0xFF00
                        {
                            tagValue = (UInt16)((tagValue != 0) ? 0xFF00 : 0);
                        }
                        response[4] = (byte)(tagValue >> 8);
                        response[5] = (byte)tagValue;

                        crc = ModbusRtuProtocol.calculateCrc(response, 6);//вычисляем crc
                        response[6] = (byte)(crc >> 8);
                        response[7] = (byte)crc;
                        return response;
                    case 15:
                    case 16:
                        if (msg.Length < 10)
                            return new byte[0];  //wrong query - do not answer
                        startTag = (UInt16)(msg[2] << 8 | msg[3]);
                        tagNum = (UInt16)(msg[4] << 8 | msg[5]);
                        //для упрощения не проверяем CRC
                        //---------------
                        if(func == 15)
                        {
                            for(int tag = startTag; tag < startTag + tagNum; tag++)
                            {
                                int bit = tag - startTag;
                                int dataByte = bit / 8;
                                int dataBit = bit % 8;
                                if (dataByte < msg[6])
                                {
                                    globalObject.tagStorage.coils[tag] = (msg[dataByte + 7] & (1 << dataBit)) != 0;
                                }
                            }
                        }
                        if(func == 16)
                        {
                            if (msg.Length < 11)
                                return new byte[0];  //wrong query - do not answer
                            for (int tag = startTag; tag < startTag + tagNum; tag++)
                            {
                                int hi = (tag - startTag) * 2;
                                int lo = hi + 1;
                                if (lo < msg[6])
                                {
                                    globalObject.tagStorage.holdings[tag] = (UInt16)(msg[hi + 7] * 256 + msg[lo + 7]);
                                }
                            }
                        }
                        //build the response
                        response = new byte[8];
                        response[0] = (byte)deviceAddress;
                        response[1] = func;
                        response[2] = (byte)(startTag >> 8);
                        response[3] = (byte)startTag;
                        response[4] = (byte)(tagNum >> 8);
                        response[5] = (byte)tagNum;
                        crc = ModbusRtuProtocol.calculateCrc(response, 6);//вычисляем crc
                        response[6] = (byte)(crc >> 8);
                        response[7] = (byte)crc;
                        return response;
                }
            }
            response = new byte[0];  //wrong query - do not answer
            return response;
        }
    }
}
