using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BaseTypes;
using GlobalObjects;
using Protocols;
using TransactionClasses;
using CommandUtils;
using TagDataBase;
using Ports;

namespace ModbusEasyTests
{
    [TestClass]
    public class ModbusEasyTests
    {
        
        [TestMethod]
        public void ModbusAddressTest()
        {
            ModbusAddress a = new ModbusAddress(Space.AnalogInputs, 16000);
            ModbusAddress b = new ModbusAddress(Space.Holdings, 1000);
            //the objects are not equals
            Assert.IsFalse(a == b, "the objects must not be equals");
            Assert.IsTrue(a != b, "the objects must not be equals");
            Assert.IsFalse(a.Equals(b), "the objects must not be equals");
            Assert.IsFalse(b.Equals(a), "the objects must notbe equals");
            //the objects are equals
            b.space = Space.AnalogInputs;
            b.tag = 16000;
            Assert.IsTrue(a == b, "the objects must be equals");
            Assert.IsFalse(a != b, "the objects must be equals");
            Assert.IsTrue(a.Equals(b), "the objects must be equals");
            Assert.IsTrue(b.Equals(a), "the objects must be equals");
            //links are equal
            b = a;
            Assert.IsTrue(a == b, "the objects must be equals");
            Assert.IsFalse(a != b, "the objects must be equals");
            Assert.IsTrue(a.Equals(b), "the objects must be equals");
            Assert.IsTrue(b.Equals(a), "the objects must be equals");

            Object c = new Object();
            Assert.IsFalse(a.Equals(c), "the objects must not be equals");
        }
        
        [TestMethod]
        public void AddrVarMapTest()
        {
 
            AddrVarMap map = new AddrVarMap();
            
            ModbusAddress addr0 = new ModbusAddress(Space.Holdings, 0);
            ModbusAddress addr2 = new ModbusAddress(Space.Holdings, 2);
            ModbusAddress addr4 = new ModbusAddress(Space.Holdings, 4);
            ModbusAddress addr6 = new ModbusAddress(Space.Holdings, 6);   

            Var var1 = new Var("var1", VarType.Float, addr0);
            Var var2 = new Var("var2", VarType.Float, addr2);
            Var var3 = new Var("var3", VarType.Float, addr4);
            map.AddVar(var1);
            map.AddVar(var2);
            //Keys are in the dictionary
            Assert.IsTrue(map.ContainsKey(addr0));
            Assert.IsTrue(map.ContainsKey(addr2));

            //There's not the key in the dictionary
            Assert.IsFalse(map.ContainsKey(addr4));

            //Try to add the same variable again
            map.AddVar(var2);
            Assert.IsTrue(map.ContainsKey(addr2));

            //Try to add new variable with address that already used
            Var var4 = new Var("var3", VarType.Float, addr2);
            map.AddVar(var4);
            Assert.IsTrue(map.ContainsKey(addr2));
            //Miscellaneous cases
            Assert.IsTrue(map.ContainsKey(new ModbusAddress(Space.Holdings, 2)));
            Assert.IsFalse(map.ContainsKey(new ModbusAddress(Space.Holdings, 4)));
            Assert.IsFalse(map.ContainsKey(new ModbusAddress(Space.Coils, 2)));
        }

        #region ModbusRtuProtocol test
        [TestMethod]
        public void ModbusRtuProtocol_calculateCrc()
        { 
            byte[] data1 = new byte[] {0x01, 0x01, 0x00, 0x00, 0x00, 0x0A, 0xBC, 0x0D};

            UInt16 crc = ModbusRtuProtocol.calculateCrc(data1, 6);

            Console.WriteLine(crc.ToString());
            Assert.IsTrue(crc == (data1[6] << 8 | data1[7]));

            byte[] data2 = new byte[] {0x01, 0x02, 0x00, 0x00, 0x00, 0x0A, 0xF8, 0x0D};
            crc = ModbusRtuProtocol.calculateCrc(data2, 6);
            Assert.IsTrue(crc == (data2[6] << 8 | data2[7]));
            byte[] data3 = new byte[] {0x01, 0x03, 0x00, 0x00, 0x00, 0x0A, 0xC5, 0xCD};
            crc = ModbusRtuProtocol.calculateCrc(data3, 6);
            Assert.IsTrue(crc == (data3[6] << 8 | data3[7]));
            byte[] data4 = new byte[] {0x01, 0x04, 0x00, 0x00, 0x00, 0x0A, 0x70, 0x0D}; 
            crc = ModbusRtuProtocol.calculateCrc(data4, 6);
            Assert.IsTrue(crc == (data4[6] << 8 | data4[7]));
            byte[] data5 = new byte[] {0x01, 0x05, 0x00, 0x00, 0x00, 0x0A, 0x4D, 0xCD}; 
            crc = ModbusRtuProtocol.calculateCrc(data5, 6);
            Assert.IsTrue(crc == (data5[6] << 8 | data5[7]));
            byte[] data6 = new byte[] {0x01, 0x06, 0x00, 0x00, 0x00, 0x0A, 0x09, 0xCD};
            crc = ModbusRtuProtocol.calculateCrc(data6, 6);
            Assert.IsTrue(crc == (data6[6] << 8 | data6[7]));
            byte[] data7 = new byte[] {0x01, 0x0F, 0x00, 0x00, 0x00, 0x0A, 0xD5, 0xCC};
            crc = ModbusRtuProtocol.calculateCrc(data7, 6);
            Assert.IsTrue(crc == (data7[6] << 8 | data7[7]));
            byte[] data8 = new byte[] {0x01, 0x10, 0x00, 0x00, 0x00, 0x0A, 0x40, 0x0E};
            crc = ModbusRtuProtocol.calculateCrc(data8, 6);
            Assert.IsTrue(crc == (data8[6] << 8 | data8[7]));
        }

        [TestMethod]
        public void ModbusRtuProtocol_modbusRtuCrc()
        {
            byte[] data1 = new byte[] { 0x01, 0x02, 0x00, 0x00, 0x00, 0x0A, 0xF8, 0x0D };   //correct crc
            Assert.IsTrue(ModbusRtuProtocol.modbusRtuCrc(data1) == ProtocolError.OK);

            byte[] data2 = new byte[] { 0x01, 0x02, 0x00, 0x00, 0x00, 0x0A, 0xF8, 0x0C };   //wrong crc
            Assert.IsTrue(ModbusRtuProtocol.modbusRtuCrc(data2) == ProtocolError.CRCError);
            
            byte[] data3 = new byte[] { 0x01 };
            Assert.IsTrue(ModbusRtuProtocol.modbusRtuCrc(data3) == ProtocolError.MsgLengthError);
        }

        [TestMethod]
        public void ModbusRtuProtocol_validateResponse()
        {
            /*
            GlobalObject go = new GlobalObject();
            go.varList = new System.Collections.Generic.LinkedList<Var>();
            go.varList.AddLast(new Var("var1", VarType.Int16, new ModbusAddress(Space.Coils, 0x00)));
            go.varList.AddLast(new Var("var2", VarType.Int16, new ModbusAddress(Space.Coils, 0x01)));
            PrivateObject auxPrivateObject = new PrivateObject(go);
            auxPrivateObject.Invoke("buildAddrVarMap");
            */
            Query query = new Query();
            //query.data = new UInt16[] { 0x01, 0x01, 0x00, 0x00, 0x00, 0x0A, 0xBC, 0x0D };
            query.function = 1;
            query.startTag = 0;
            query.tagNum = 10;
           //query.globalObject = go;

            byte[] respMsg1 = new byte[] { 0x01, 0x01, 0x02, 0x55, 0x40, 0x00, 0x00 };
            UInt16 crc = ModbusRtuProtocol.calculateCrc(respMsg1, 5);
            respMsg1[5] = (byte)(crc >> 8);
            respMsg1[6] = (byte)crc;

            ModbusRtuProtocol modbusRtuProtocol = new ModbusRtuProtocol();
            modbusRtuProtocol.deviceAddress = 1;
            modbusRtuProtocol.query = query;
            PrivateObject po = new PrivateObject(modbusRtuProtocol);
            ProtocolError pe = (ProtocolError)po.Invoke("validateResponse", new Object[] { respMsg1 });
            Assert.IsTrue(pe == ProtocolError.OK);

           // Assert.IsTrue(modbusRtuProtocol.query.globalObject.addrVarMap[new ModbusAddress(Space.Coils, 0x00)].UInt16Value == 0 );
           // Assert.IsTrue(modbusRtuProtocol.query.globalObject.addrVarMap[new ModbusAddress(Space.Coils, 0x01)].UInt16Value == 1);
        }

        [TestMethod]
        public void ModbusRtuProtocol_decode()
        {
            GlobalObject go = new GlobalObject();
            go.varList = new System.Collections.Generic.LinkedList<Var>();
            go.varList.AddLast(new Var("var1-1", VarType.Int16, new ModbusAddress(Space.Coils, 0x00)));
            go.varList.AddLast(new Var("var1-2", VarType.Int16, new ModbusAddress(Space.Coils, 0x01)));
            go.varList.AddLast(new Var("var1-3", VarType.Int16, new ModbusAddress(Space.Coils, 0x02)));
            go.varList.AddLast(new Var("var1-4", VarType.Int16, new ModbusAddress(Space.Coils, 0x03)));

            go.varList.AddLast(new Var("var2-1", VarType.Int16, new ModbusAddress(Space.DiscreteInputs, 0x00)));
            go.varList.AddLast(new Var("var2-2", VarType.Int16, new ModbusAddress(Space.DiscreteInputs, 0x01)));
            go.varList.AddLast(new Var("var2-3", VarType.Int16, new ModbusAddress(Space.DiscreteInputs, 0x02)));
            go.varList.AddLast(new Var("var2-4", VarType.Int16, new ModbusAddress(Space.DiscreteInputs, 0x03)));

            go.varList.AddLast(new Var("var3-1", VarType.Int16, new ModbusAddress(Space.AnalogInputs, 0x00)));
            go.varList.AddLast(new Var("var3-2", VarType.Int16, new ModbusAddress(Space.AnalogInputs, 0x01)));
            go.varList.AddLast(new Var("var3-3", VarType.Int16, new ModbusAddress(Space.AnalogInputs, 0x02)));
            go.varList.AddLast(new Var("var3-4", VarType.Int16, new ModbusAddress(Space.AnalogInputs, 0x03)));

            go.varList.AddLast(new Var("var4-1", VarType.Int16, new ModbusAddress(Space.Holdings, 0x00)));
            go.varList.AddLast(new Var("var4-2", VarType.Int16, new ModbusAddress(Space.Holdings, 0x01)));
            go.varList.AddLast(new Var("var4-3", VarType.Int16, new ModbusAddress(Space.Holdings, 0x02)));
            go.varList.AddLast(new Var("var4-4", VarType.Int16, new ModbusAddress(Space.Holdings, 0x03)));

            PrivateObject auxPrivateObject = new PrivateObject(go);
            auxPrivateObject.Invoke("buildAddrVarMap");

            ModbusRtuProtocol modbusRtuProtocol = new ModbusRtuProtocol();

            Query query = new Query();
           // query.data = new UInt16[] { 0x01, 0x01, 0x00, 0x00, 0x00, 0x0A, 0xBC, 0x0D };
            query.function = 1;
            query.startTag = 0;
            query.tagNum = 10;
            query.globalObject = go;

            byte[] respMsg1 = new byte[] { 0x01, 0x01, 0x02, 0x55, 0x40, 0x00, 0x00 };
            UInt16 crc = ModbusRtuProtocol.calculateCrc(respMsg1, 5);
            respMsg1[5] = (byte)(crc >> 8);
            respMsg1[6] = (byte)crc;

            modbusRtuProtocol.deviceAddress = 1;
            modbusRtuProtocol.query = query;
            modbusRtuProtocol.globalObject = go;
            Response response = new Response(query);

            ProtocolError pe = modbusRtuProtocol.decode(respMsg1, query, response);

            Assert.IsTrue(pe == ProtocolError.OK);
            Assert.IsTrue(modbusRtuProtocol.query.globalObject.addrVarMap[new ModbusAddress(Space.Coils, 0x00)].uInt16Value == 1);
            Assert.IsTrue(modbusRtuProtocol.query.globalObject.addrVarMap[new ModbusAddress(Space.Coils, 0x01)].uInt16Value == 0);
            Assert.IsTrue(modbusRtuProtocol.query.globalObject.addrVarMap[new ModbusAddress(Space.Coils, 0x02)].uInt16Value == 1);
            Assert.IsTrue(modbusRtuProtocol.query.globalObject.addrVarMap[new ModbusAddress(Space.Coils, 0x03)].uInt16Value == 0);

            respMsg1[6] ^= respMsg1[6];  //make the crc wrong
            pe = modbusRtuProtocol.decode(respMsg1, query, response);
            Assert.IsTrue(pe == ProtocolError.CRCError);

            //дописать тесты для других функций
        }

        [TestMethod]
        public void ModbusRtuProtocol_buildMessage()
        {
            Assert.IsTrue(false);  // to do!
        }

        [TestMethod]
        public void ModbusRtuProtocol_buildResponse_Test()
        {
            byte[] msg;
            byte[] resp;
            byte[] ans;
            UInt16 crc;
            bool[] coils, d_inputs;
            UInt16[] a_inputs, holdings;
            GlobalObject go = new GlobalObject();
            ModbusRtuProtocol mrp = new ModbusRtuProtocol();
            mrp.deviceAddress = 1;
            mrp.globalObject = go;
            mrp.globalObject.tagStorage.setTag(new ModbusAddress(Space.Coils, 0), 1);

            //---------------
            // function 1
            coils = new bool[] { true, false, true, true, false, false, true, true, true, false, false, false, true, true, true, true };
            for(int i = 0; i < 16; i++)
            {
                go.tagStorage.setTag(new ModbusAddress(Space.Coils, (UInt16)i), (UInt16)((coils[i]) ? 0xFF00: 0));
            };
            msg = new byte[] { 01, 01, 00, 00, 00, 16, 00, 00 };
            crc = ModbusRtuProtocol.calculateCrc(msg, 8);
            msg[6] = (byte)(crc >> 8);
            msg[7] = (byte)crc;
            resp = mrp.buildResponse(msg);
            ans = new byte[] { 01, 01, 02, 0xCD, 0xF1, 00, 00 };
            crc = ModbusRtuProtocol.calculateCrc(ans, 5);
            ans[5] = (byte)(crc >> 8);
            ans[6] = (byte)crc;
            Assert.IsTrue(resp.Length == 7);
            for (int i = 0; i < resp.Length; i++)
                Assert.IsTrue(resp[i] == ans[i]);
            //----------------------------
            // function 2
            d_inputs = new bool[] { true, false, true, true, false, false, true, true, true, false, false, false, true, true, true, true };
            for (int i = 0; i < 16; i++)
            {
                go.tagStorage.setTag(new ModbusAddress(Space.DiscreteInputs, (UInt16)i), (UInt16)((d_inputs[i]) ? 0xFF00 : 0));
            };
            msg = new byte[] { 01, 02, 00, 00, 00, 16, 00, 00 };
            crc = ModbusRtuProtocol.calculateCrc(msg, 8);
            msg[6] = (byte)(crc >> 8);
            msg[7] = (byte)crc;
            resp = mrp.buildResponse(msg);
            ans = new byte[] { 01, 02, 02, 0xCD, 0xF1, 00, 00 };
            crc = ModbusRtuProtocol.calculateCrc(ans, 5);
            ans[5] = (byte)(crc >> 8);
            ans[6] = (byte)crc;
            Assert.IsTrue(resp.Length == 7);
            for (int i = 0; i < resp.Length; i++)
                Assert.IsTrue(resp[i] == ans[i]);
            //-----------------
            //function 3
            holdings = new UInt16[] { 0x0102, 0x0304, 0x0506 };
            for (int i = 0; i < 3; i++)
            {
                go.tagStorage.setTag(new ModbusAddress(Space.Holdings, (UInt16)i), holdings[i]);
            };
            msg = new byte[] { 01, 03, 00, 00, 00, 03, 00, 00 };
            crc = ModbusRtuProtocol.calculateCrc(msg, 8);
            msg[6] = (byte)(crc >> 8);
            msg[7] = (byte)crc;
            resp = mrp.buildResponse(msg);
            ans = new byte[] { 01, 03, 06, 01, 02, 03, 04, 05, 06, 00, 00 };
            crc = ModbusRtuProtocol.calculateCrc(ans, 9);
            ans[9] = (byte)(crc >> 8);
            ans[10] = (byte)crc;
            Assert.IsTrue(resp.Length == 11);
            for (int i = 0; i < resp.Length; i++)
                Assert.IsTrue(resp[i] == ans[i]);
            //-----------------
            //function 4
            a_inputs = new UInt16[] { 0x0102, 0x0304, 0x0506 };
            for (int i = 0; i < 3; i++)
            {
                go.tagStorage.setTag(new ModbusAddress(Space.AnalogInputs, (UInt16)i), a_inputs[i]);
            };
            msg = new byte[] { 01, 04, 00, 00, 00, 03, 00, 00 };
            crc = ModbusRtuProtocol.calculateCrc(msg, 6);
            msg[6] = (byte)(crc >> 8);
            msg[7] = (byte)crc;
            resp = mrp.buildResponse(msg);
            ans = new byte[] { 01, 04, 06, 01, 02, 03, 04, 05, 06, 00, 00 };
            crc = ModbusRtuProtocol.calculateCrc(ans, 9);
            ans[9] = (byte)(crc >> 8);
            ans[10] = (byte)crc;
            Assert.IsTrue(resp.Length == 11);
            for (int i = 0; i < resp.Length; i++)
                Assert.IsTrue(resp[i] == ans[i]);
            //-----------------
            // function 5
            msg = new byte[] { 01, 05, 200, 100, 0xFF, 0x00, 00, 00 };
            crc = ModbusRtuProtocol.calculateCrc(msg, 6);
            msg[6] = (byte)(crc >> 8);
            msg[7] = (byte)crc;
            resp = mrp.buildResponse(msg);
            ans = new byte[] { 01, 05, 200, 100, 0xFF, 0x00, 00, 00 };
            crc = ModbusRtuProtocol.calculateCrc(ans, 6);
            ans[6] = (byte)(crc >> 8);
            ans[7] = (byte)crc;
            Assert.IsTrue(resp.Length == 8);
            for (int i = 0; i < resp.Length; i++)
                Assert.IsTrue(resp[i] == ans[i]);
            //-----------------
            // function 6
            msg = new byte[] { 01, 06, 200, 100, 0xAA, 0x55, 00, 00 };
            crc = ModbusRtuProtocol.calculateCrc(msg, 6);
            msg[6] = (byte)(crc >> 8);
            msg[7] = (byte)crc;
            resp = mrp.buildResponse(msg);
            ans = new byte[] { 01, 06, 200, 100, 0xAA, 0x55, 00, 00 };
            crc = ModbusRtuProtocol.calculateCrc(ans, 6);
            ans[6] = (byte)(crc >> 8);
            ans[7] = (byte)crc;
            Assert.IsTrue(resp.Length == 8);
            for (int i = 0; i < resp.Length; i++)
                Assert.IsTrue(resp[i] == ans[i]);
            Assert.IsTrue(go.tagStorage.getTag(new ModbusAddress(Space.Holdings, (UInt16)(200 * 256 + 100))) != 0);
            //---------------
            // function 15
            msg = new byte[] {01, 15, 00, 00, 00, 08, 01, 55, 00, 00};
            crc = ModbusRtuProtocol.calculateCrc(msg, 8);
            msg[8] = (byte)(crc >> 8);
            msg[9] = (byte)crc;
            resp = mrp.buildResponse(msg);
            ans = new byte[] { 01, 15, 00, 00, 00, 08, 00, 00};
            crc = ModbusRtuProtocol.calculateCrc(ans, 6);
            ans[6] = (byte)(crc >> 8);
            ans[7] = (byte)crc;
            Assert.IsTrue(resp.Length == 8);
            for(int i = 0; i < resp.Length; i++)
                Assert.IsTrue(resp[i] == ans[i]);
            coils = new bool[] { true, true, true, false, true, true, false, false };
            for (int i = 0; i < 8; i++)
                Assert.IsTrue((go.tagStorage.getTag(Space.Coils, i) != 0) == coils[i]);// coils[i] == coils[i]);

            //---------------
            // function 16
            msg = new byte[] { 01, 16, 00, 00, 00, 03, 06, 00, 01, 02, 03, 04, 05, 00, 00 }; 
            crc = ModbusRtuProtocol.calculateCrc(msg, 12);
            msg[13] = (byte)(crc >> 8);
            msg[14] = (byte)crc;
            resp = mrp.buildResponse(msg);
            ans = new byte[] { 01, 16, 00, 00, 00, 03, 00, 00};
            crc = ModbusRtuProtocol.calculateCrc(ans, 6);
            ans[6] = (byte)(crc >> 8);
            ans[7] = (byte)crc;
            Assert.IsTrue(resp.Length == 8);
            for (int i = 0; i < resp.Length; i++)
                Assert.IsTrue(resp[i] == ans[i]);
            holdings = new UInt16[] { 0x0001, 0x0203, 0x0405 };
            for (int i = 0; i < 3; i++)
                Assert.IsTrue(go.tagStorage.getTag(Space.Holdings, i) == holdings[i]);
        }
        #endregion

        #region command parsers
        [TestMethod]
        public void CParser_configFileName()
        {
            string defaultFileName = "default.xml";
            Assert.IsTrue(CParser.configFileName(@"-config=filename.xml", ref defaultFileName));   //-config=filename.xml
            Assert.IsTrue(defaultFileName == "filename.xml");

            Assert.IsTrue(CParser.configFileName(@"-config=file name.xml", ref defaultFileName)); //-config="file name.xml"
            Assert.IsTrue(defaultFileName == "file name.xml");
            /*
            CParser.configFileName(@"-config=filename.xml", ref defaultFileName);       //-config=filename.xml
            Assert.IsTrue(defaultFileName == "filename.xml");
             * */
            Assert.IsTrue(CParser.configFileName(@"-config=c:/some dir1/somedir2/ some dir3/filename.xml", ref defaultFileName));       //-config=c:/some dir1/somedir2/ some dir3/filename.xml
            Assert.IsTrue(defaultFileName == "c:/some dir1/somedir2/ some dir3/filename.xml");

            Assert.IsTrue(CParser.configFileName(@"-config=c:/somedir1/somedir2/somedir3/filename.xml", ref defaultFileName));
            Assert.IsTrue(defaultFileName == "c:/somedir1/somedir2/somedir3/filename.xml");

            Assert.IsTrue(CParser.configFileName(@"-config=c:\somedir1\somedir2\somedir3\filename.xml", ref defaultFileName));
            Assert.IsTrue(defaultFileName == @"c:\somedir1\somedir2\somedir3\filename.xml");

            defaultFileName = "default.xml";
            Assert.IsFalse(CParser.configFileName(@"-config=", ref defaultFileName));  // void filename - the function returns the default filename
            Assert.IsTrue(defaultFileName == "default.xml");
            
            defaultFileName = "default.xml";
            Assert.IsFalse(CParser.configFileName(@"something wrong", ref defaultFileName));// wrong filename - the function returns the default filename
            Assert.IsTrue(defaultFileName == "default.xml");
        }

        [TestMethod]
        public void CParser_isHelpLine()
        {
            Assert.IsTrue(CParser.isHelpLine("help"));
            Assert.IsTrue(CParser.isHelpLine("appname -help"));
            Assert.IsFalse(CParser.isHelpLine("do_not_help"));
        }

        [TestMethod]
        public void CParser_isFunction()
        {
            int func;
            UInt16 tag1, tag2;
            UInt16[] data;
            Assert.IsTrue(CParser.isFunction("get coil 123 456", out func, out tag1, out tag2, out data));
            Assert.IsTrue(func == 1 && tag1 == 123 && tag2 == 456 && data == null);
            Assert.IsTrue(CParser.isFunction("get coil 123", out func, out tag1, out tag2, out data));
            Assert.IsTrue(func == 1 && tag1 == 123 && tag2 == 123 && data == null);

            Assert.IsTrue(CParser.isFunction("get di 123 456", out func, out tag1, out tag2, out data));
            Assert.IsTrue(func == 2 && tag1 == 123 && tag2 == 456 && data == null);
            Assert.IsTrue(CParser.isFunction("get di 123", out func, out tag1, out tag2, out data));
            Assert.IsTrue(func == 2 && tag1 == 123 && tag2 == 123 && data == null);

            Assert.IsTrue(CParser.isFunction("get hold 123 456", out func, out tag1, out tag2, out data));
            Assert.IsTrue(func == 3 && tag1 == 123 && tag2 == 456 && data == null);
            Assert.IsTrue(CParser.isFunction("get hold 123", out func, out tag1, out tag2, out data));
            Assert.IsTrue(func == 3 && tag1 == 123 && tag2 == 123 && data == null);

            Assert.IsTrue(CParser.isFunction("get ai 123 456", out func, out tag1, out tag2, out data));
            Assert.IsTrue(func == 4 && tag1 == 123 && tag2 == 456 && data == null);
            Assert.IsTrue(CParser.isFunction("get ai 123", out func, out tag1, out tag2, out data));
            Assert.IsTrue(func == 4 && tag1 == 123 && tag2 == 123 && data == null);

            Assert.IsTrue(CParser.isFunction("set coil 123 = 1", out func, out tag1, out tag2, out data));
            Assert.IsTrue(func == 5 && tag1 == 123 && tag2 == 123 && data[0] == 0xFF00);

            Assert.IsTrue(CParser.isFunction("set coil 123 = 0", out func, out tag1, out tag2, out data));
            Assert.IsTrue(func == 5 && tag1 == 123 && tag2 == 123 && data[0] == 0);

            Assert.IsTrue(CParser.isFunction("set coil 123 = on", out func, out tag1, out tag2, out data));
            Assert.IsTrue(func == 5 && tag1 == 123 && tag2 == 123 && data[0] == 0xFF00);

            Assert.IsTrue(CParser.isFunction("set coil 123 = off", out func, out tag1, out tag2, out data));
            Assert.IsTrue(func == 5 && tag1 == 123 && tag2 == 123 && data[0] == 0);

            Assert.IsTrue(CParser.isFunction("set coil 123 = 1", out func, out tag1, out tag2, out data));
            Assert.IsTrue(func == 5 && tag1 == 123 && tag2 == 123 && data[0] == 0xFF00);

            Assert.IsTrue(CParser.isFunction("set coil 123 =", out func, out tag1, out tag2, out data));
            Assert.IsTrue(func == 5 && tag1 == 123 && tag2 == 123 && data[0] == 0);

            Assert.IsTrue(CParser.isFunction("set hold 123 = 456", out func, out tag1, out tag2, out data));
            Assert.IsTrue(func == 6 && tag1 == 123 && tag2 == 123 && data[0] == 456);

            Assert.IsTrue(CParser.isFunction("set hold 123 =", out func, out tag1, out tag2, out data));
            Assert.IsTrue(func == 6 && tag1 == 123 && tag2 == 123 && data[0] == 0);

            Assert.IsTrue(CParser.isFunction("set coils 123 125 = on off on", out func, out tag1, out tag2, out data));
            Assert.IsTrue(func == 15 && tag1 == 123 && tag2 == 125 && data[0] == 0xFF00 && data[1] == 0 && data[2] == 0xFF00);

            Assert.IsTrue(CParser.isFunction("set coils 123 125 = on", out func, out tag1, out tag2, out data));
            Assert.IsTrue(func == 15 && tag1 == 123 && tag2 == 125 && data[0] == 0xFF00 && data[1] == 0xFF00 && data[2] == 0xFF00);

            Assert.IsTrue(CParser.isFunction("set coils 123 125 = ", out func, out tag1, out tag2, out data));
            Assert.IsTrue(func == 15 && tag1 == 123 && tag2 == 125 && data[0] == 0 && data[1] == 0 && data[2] == 0);

            Assert.IsTrue(CParser.isFunction("set hold 123 125 = 256 257 258", out func, out tag1, out tag2, out data));
            Assert.IsTrue(func == 16 && tag1 == 123 && tag2 == 125 && data[0] == 256 && data[1] == 257 && data[2] == 258);

            Assert.IsTrue(CParser.isFunction("set hold 123 125 = 1234", out func, out tag1, out tag2, out data));
            Assert.IsTrue(func == 16 && tag1 == 123 && tag2 == 125 && data[0] == 1234 && data[1] == 1234 && data[2] == 1234);

            Assert.IsTrue(CParser.isFunction("set hold 123 125 = ", out func, out tag1, out tag2, out data));
            Assert.IsTrue(func == 16 && tag1 == 123 && tag2 == 125 && data[0] == 0 && data[1] == 0 && data[2] == 0);

            Assert.IsFalse(CParser.isFunction("some  bullshit", out func, out tag1, out tag2, out data));
        }
        #region trash
        /*
        [TestMethod]
        public void CParser_isFunction1()
        {
            UInt16 tag1 = 0, tag2 = 0;
            Assert.IsTrue(CParser.isFunction1("get coils from 1 to 65535", ref tag1, ref tag2));
            Assert.IsTrue(tag1 == 1);
            Assert.IsTrue(tag2 == 65535);

            Assert.IsTrue(CParser.isFunction1("-get coils from 1 to 65535", ref tag1, ref tag2));
            Assert.IsTrue(tag1 == 1);
            Assert.IsTrue(tag2 == 65535);

            Assert.IsFalse(CParser.isFunction1("something wrong get coils from 1 to 65535", ref tag1, ref tag2));

            Assert.IsFalse(CParser.isFunction1("get coils from 1 to 65536", ref tag1, ref tag2));
            Assert.IsFalse(CParser.isFunction1("get coils from -1 to 65535", ref tag1, ref tag2));
            Assert.IsFalse(CParser.isFunction1("get coils from 100 to 99", ref tag1, ref tag2));
        }

        [TestMethod]
        public void CParserisFunction15()
        {
            UInt16 tag1 = 0, tag2 = 0;
            bool[] data = null;
            Assert.IsTrue(CParser.isFunction15("set coils from 1 to 4 on off on off", ref tag1, ref tag2, ref data));
            Assert.IsTrue(tag1 == 1);
            Assert.IsTrue(tag2 == 4);
            Assert.IsTrue(data[0] && !data[1] && data[2] && !data[3]);
        }

        [TestMethod]
        public void CParserisFunction16()
        {
            UInt16 tag1 = 0, tag2 = 0;
            UInt16[] data = null;
            Assert.IsTrue(CParser.isFunction16("set hold 1 - 4 = 1 2 3 4", ref tag1, ref tag2, ref data));
            Assert.IsTrue(tag1 == 1);
            Assert.IsTrue(tag2 == 4);
            Assert.IsTrue(data[0] == 1 && data[1] == 2 && data[2] == 3 && data[3] == 4);
            //-----------------
            //wrong quantity of tags
            Assert.IsFalse(CParser.isFunction16("set hold 1 - 4 = 1 2 3", ref tag1, ref tag2, ref data));
            //wrong command
            Assert.IsFalse(CParser.isFunction16("set some bullshit", ref tag1, ref tag2, ref data));
            Assert.IsFalse(CParser.isFunction16("set hold 4 - 1 = 1 2 3 4", ref tag1, ref tag2, ref data));
            Assert.IsFalse(CParser.isFunction16("set hold 1 - 4 = 1 2 3 blabla", ref tag1, ref tag2, ref data));
        }*/
#endregion
        [TestMethod]
        public void CParserisGetTagsTest()
        {
            UInt16 tag1 = 0, tag2 = 0;
            Space space = Space.Coils;
            Assert.IsTrue(CParser.isGetDbTags("getdb coils 100 110", ref space, ref tag1, ref tag2));
            Assert.IsTrue(space == Space.Coils && tag1 == 100 && tag2 == 110);
            Assert.IsTrue(CParser.isGetDbTags("getdb coils 100", ref space, ref tag1, ref tag2));
            Assert.IsTrue(space == Space.Coils && tag1 == 100 && tag2 == 100);

            Assert.IsTrue(CParser.isGetDbTags("getdb di 1000 1100", ref space, ref tag1, ref tag2));
            Assert.IsTrue(space == Space.DiscreteInputs && tag1 == 1000 && tag2 == 1100);
            Assert.IsTrue(CParser.isGetDbTags("getdb di 1000", ref space, ref tag1, ref tag2));
            Assert.IsTrue(space == Space.DiscreteInputs && tag1 == 1000 && tag2 == 1000);

            Assert.IsTrue(CParser.isGetDbTags("getdb ai 10000 11000", ref space, ref tag1, ref tag2));
            Assert.IsTrue(space == Space.AnalogInputs && tag1 == 10000 && tag2 == 11000);
            Assert.IsTrue(CParser.isGetDbTags("getdb ai 10000", ref space, ref tag1, ref tag2));
            Assert.IsTrue(space == Space.AnalogInputs && tag1 == 10000 && tag2 == 10000);

            Assert.IsTrue(CParser.isGetDbTags("getdb hold 60000 61000", ref space, ref tag1, ref tag2));
            Assert.IsTrue(space == Space.Holdings && tag1 == 60000 && tag2 == 61000);
            Assert.IsTrue(CParser.isGetDbTags("getdb hold 60000", ref space, ref tag1, ref tag2));
            Assert.IsTrue(space == Space.Holdings && tag1 == 60000 && tag2 == 60000);

            Assert.IsFalse(CParser.isGetDbTags("utter bullshit", ref space, ref tag1, ref tag2));
        }

        [TestMethod]
        public void CParserIsSetDbTags()
        {
            UInt16 tag1 = 0, tag2 = 0;
            Space space = Space.Coils;
            UInt16[] data = null;
            //-----------------
            // the first case
            Assert.IsTrue(CParser.isSetDbTags("setdb coils 100 110 = 0 1 2 3 4 5 6 7 8 9 10", ref space, ref tag1, ref tag2, ref data));
            Assert.IsTrue(space == Space.Coils && tag1 == 100 && tag2 == 110);
            for(int i = 0; i < 11; i++)
            {
                Assert.IsTrue(data[i] == i);
            }
            //-------------------
            // the 2nd  case
            Assert.IsTrue(CParser.isSetDbTags("setdb coils 100 110 = 0 1 2 3 4 5", ref space, ref tag1, ref tag2, ref data));
            Assert.IsTrue(space == Space.Coils && tag1 == 100 && tag2 == 110);
            for (int i = 0; i < 5; i++)
            {
                Assert.IsTrue(data[i] == i);
            }
            for (int i = 5; i < 11; i++)
            { 
                Assert.IsTrue(data[i] == 5);
            }

            //-------------------------
            // the 3rd case
            Assert.IsTrue(CParser.isSetDbTags("setdb coils 100 105 = 0 1 2 3 4 5 6 7 8 9 10", ref space, ref tag1, ref tag2, ref data));
            Assert.IsTrue(space == Space.Coils && tag1 == 100 && tag2 == 105);
            for (int i = 0; i < 6; i++)
            {
                Assert.IsTrue(data[i] == i);
            }

            //-------------------------------
            //the 4th case
            Assert.IsFalse(CParser.isSetDbTags("some bullshit", ref space, ref tag1, ref tag2, ref data));
        }
        #endregion
        #region database test
        /*
        [TestMethod]
        public void CreateMemoryDbTest()
        {
            dbStorage db = new dbStorage();
           // db.Connect("Data Source=:memory:;New=True;Version=3");
            db.Connect("Data Source=C:\\test.db;Version=3");
            db.CreateTables();
            db.setTag(new ModbusAddress(Space.Coils, 100), 0xFF00);
            Assert.IsTrue(db.getTag(new ModbusAddress(Space.Coils, 100)) != 0);
            db.setTag(new ModbusAddress(Space.Coils, 100), 0x0000);
            Assert.IsTrue(db.getTag(new ModbusAddress(Space.Coils, 100)) == 0);
        }
        */
        [TestMethod]
        public void tagStorageTest()
        { 
            const int MAXTAG = 65535;  
            TagStorage tagStorage = new TagStorage();
            for (int i = 0; i <= MAXTAG; i++)
            {
                tagStorage.setTag(new ModbusAddress(Space.Coils, (UInt16)i), (UInt16)(i % 2));
                tagStorage.setTag(new ModbusAddress(Space.DiscreteInputs, (UInt16)i), (UInt16)(i % 2));
                tagStorage.setTag(new ModbusAddress(Space.AnalogInputs, (UInt16)i), (UInt16)i);
                tagStorage.setTag(new ModbusAddress(Space.Holdings, (UInt16)i), (UInt16)i);
            }
            for (int i = 0; i <= MAXTAG; i++)
            {
                Assert.IsTrue(tagStorage.getTag(new ModbusAddress(Space.Coils, (UInt16)i)) == (UInt16)(i % 2));
                Assert.IsTrue(tagStorage.getTag(new ModbusAddress(Space.DiscreteInputs, (UInt16)i)) == (UInt16)(i % 2));
                Assert.IsTrue(tagStorage.getTag(new ModbusAddress(Space.AnalogInputs, (UInt16)i)) == i);
                Assert.IsTrue(tagStorage.getTag(new ModbusAddress(Space.Holdings, (UInt16)i)) == i);               
            }
        }
        #endregion

        #region set/get mode test
        [TestMethod]
        public void cParserIsGetModeTest()
        {
            Assert.IsTrue(CParser.isGetMode("get mode"));
            Assert.IsTrue(CParser.isGetMode("getmode"));
            Assert.IsFalse(CParser.isGetMode("some bullshit"));
        } 

        [TestMethod]
        public void cParserIsSetModeTest()
        {
            Mode mode = Mode.Master;
            Assert.IsTrue(CParser.isSetMode("set mode master", ref mode));
            Assert.IsTrue(mode == Mode.Master);

            Assert.IsTrue(CParser.isSetMode("set mode slave", ref mode));
            Assert.IsTrue(mode == Mode.Slave);

            Assert.IsTrue(CParser.isSetMode("set mode test", ref mode));
            Assert.IsTrue(mode == Mode.Test);

            Assert.IsTrue(CParser.isSetMode("setmode master", ref mode));
            Assert.IsTrue(mode == Mode.Master);

            Assert.IsFalse(CParser.isSetMode("some bullshit", ref mode));
        }
        #endregion
    }
}
