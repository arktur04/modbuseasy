using System;
using System.Text.RegularExpressions;
using BaseTypes;

namespace CommandUtils
{
    //static methods for command line parsing and user input parsing
    public class CParser
    {
        // find config file name in the command line
        public static bool configFileName(string commandLine, ref string fileName)
        {
            //--------------------------------------------------
            // command line parsing
            //-------------------------------------------------
            // command line args:
            // config=filename
            // or
            // config="filename"
            //
            try
            {
                Regex configRe = new Regex(@"^-?config(?:\s)*=(?:\s)*(.+)");
                // Regex configRe = new Regex(@"^-?config=(?:(?:""(.+)"")|(\S+))");
                MatchCollection m = configRe.Matches(commandLine);
                string group1 = m[0].Groups[1].Value;  //group 1 - filename in quotes
                string group2 = m[0].Groups[2].Value;  //group 2 - filename without quotes
                if (group1.Length > 0)
                {
                    fileName = group1;
                    return true;
                }
                else
                {
                    if (group2.Length > 0)
                    {
                        fileName = group2;
                        return true;
                    }
                }
            }
            catch { }
            return false;
        }

        //-------------------
        public static bool isHelpLine(string commandLine)
        {
            Regex configRe = new Regex(@"(?:^help)|(?:\s-help)|(?:\shelp)");
            return configRe.IsMatch(commandLine);
        }
        //aux function for command parsing
        private static bool StringToSpace(string val, out Space space)
        {
            switch (val)
            {
                case "coil":
                case "coils":
                    space = Space.Coils;
                    return true;
                case "di":
                    space = Space.DiscreteInputs;
                    return true;
                case "ai":
                    space = Space.AnalogInputs;
                    return true;
                case "hold":
                    space = Space.Holdings;
                    return true;
            }
            space = Space.Coils; // we must assign some value to space, even if the function returns false
            return false;
        }

        public static bool isFunction(string commandLine, out int func, out UInt16 tag1, out UInt16 tag2, out UInt16[] tagData)
        {
            func = 0; //in case of fail, all out parameters are set to 0
            tag1 = 0;
            tag2 = 0;
            tagData = null;
            Space space = Space.Coils;
            bool isSet = false;
            UInt16[] tagData_ = null; //сначала записываем данные в этот массив, потом копируем в tagData

            try
            {
                //regex for debug: ^-?(set|get)\s(coils?|hold|di|ai)\s(\d+)(?:(?:\s(\d+))?(?:\s?=\s?(?:(?:(\d+)|(on|off))(?:\s|$))+)?)?
                Regex getTagsRe = new Regex(@"^-?(?'setget'set|get)\s(?'space'coils?|hold|di|ai)\s(?'tag1'\d+)(?:(?:\s(?'tag2'\d+))?(?:\s?=\s?(?:(?:(?'data'\d+)|(?'booldata'on|off))(?:\s|$))+)?)?");
                MatchCollection m = getTagsRe.Matches(commandLine);
                isSet = m[0].Groups["setget"].Value == "set";
                if(!StringToSpace(m[0].Groups["space"].Value, out space))
                    return false;
                tag1 = Convert.ToUInt16(m[0].Groups["tag1"].Value);
                if (m[0].Groups["tag2"].Captures.Count == 1)
                    tag2 = Convert.ToUInt16(m[0].Groups["tag2"].Value);
                else
                {
                    tag2 = tag1;
                }
                if (tag2 < tag1)  //wrong tags;
                    return false;

                int tagCount = tag2 - tag1 + 1;

                bool singleTag = tagCount == 1;
                int dataCount = m[0].Groups["data"].Captures.Count;
                if (dataCount > 0)
                {
                    tagData_ = new UInt16[dataCount];
                    for (int i = 0; i < dataCount; i++)
                    {
                        tagData_[i] = Convert.ToUInt16(m[0].Groups["data"].Captures[i].Value);
                    }
                }
                else
                {
                    dataCount = m[0].Groups["booldata"].Captures.Count;
                    if (dataCount > 0)
                    {
                        tagData_ = new UInt16[dataCount];
                        for (int i = 0; i < dataCount; i++)
                        {
                            tagData_[i] = (UInt16)((m[0].Groups["booldata"].Captures[i].Value == "on") ? 0xFF00: 0);
                        }
                    }
                }
                if (space == Space.Coils && !isSet)
                {
                    func = 1;
                    return true;
                }
                if (space == Space.DiscreteInputs && !isSet)
                {
                    func = 2;
                    return true;
                }
                if (space == Space.Holdings && !isSet)
                {
                    func = 3;
                    return true;
                }
                if (space == Space.AnalogInputs && !isSet)
                {
                    func = 4;
                    return true;
                }
                if (space == Space.Coils && isSet && singleTag)
                {
                    func = 5;
                    tagData = new UInt16[1];
                    if (tagData_ == null)
                        tagData[0] = 0;
                    else
                        tagData[0] = (UInt16)((tagData_[0] != 0) ? 0xFF00 : 0); 
                    return true;
                }
                if (space == Space.Holdings && isSet && singleTag)
                {
                    func = 6;
                    tagData = new UInt16[1];
                    if (tagData_ == null)
                        tagData[0] = 0;
                    else
                        tagData[0] = tagData_[0];
                    return true;
                }
                if (space == Space.Coils && isSet && !singleTag)
                {
                    func = 15;
                    UInt16 lastData = 0;
                    tagData = new UInt16[tagCount];
                    for (int i = 0; i < tagCount; i++)
                    {
                        if (tagData_ != null)
                        {
                            if (i < tagData_.Length)
                            { 
                                tagData[i] = (UInt16)((tagData_[i] != 0) ? 0xFF00 : 0);
                                lastData = tagData[i];
                            }
                            else
                                tagData[i] = lastData;
                            //tagData[i] = (UInt16)((i < tagData_.Length) ? ((tagData_[i] != 0) ? 0xFF00 : 0) : 0);
                        }
                        else
                            tagData[i] = 0;
                    }
                    return true;
                }
                if (space == Space.Holdings && isSet && !singleTag)
                {
                    func = 16;
                    UInt16 lastData = 0;
                    tagData = new UInt16[tagCount];
                    for (int i = 0; i < tagCount; i++)
                    {
                        if (tagData_ != null)
                        {
                            if(i < tagData_.Length)
                            {
                                tagData[i] = tagData_[i];
                                lastData = tagData[i];
                            }
                            else
                                tagData[i] = lastData;
                            //tagData[i] = (UInt16)((i < tagData_.Length) ? tagData_[i] : 0);
                        }
                        else
                            tagData[i] = 0;
                    }
                    return true;
                }
                return false;  //something is wrong...
            }
            catch { return false; }
        }

        #region storage commands
        //--------------------------------------------------------------------------------
        // tag storage get/set commands
        //----------------------------------------------------------------------------

        //command:
        // getdb <space> <tag1> [tag2]
        //where:
        // space ::= (coils | di | ai | hold)
        //examples:
        // getdb holds 100
        // getdb ai 100 120
        public static bool isGetDbTags(string commandLine, ref Space space, ref UInt16 tag1, ref UInt16 tag2)
        {
            try
            {
                Regex getTagsRe = new Regex(@"^-?getdb (coil|coils|di|ai|hold)\s*(\d+)\s*(\d*)");
                MatchCollection m = getTagsRe.Matches(commandLine);
                try
                {
                    if (!StringToSpace(m[0].Groups[1].Value, out space))
                        return false;
                }
                catch
                {
                    return false;
                }
                try
                {
                    tag1 = Convert.ToUInt16(m[0].Groups[2].Value);
                }
                catch 
                {
                    return false; 
                };
                try
                {
                    tag2 = Convert.ToUInt16(m[0].Groups[3].Value);
                }
                catch
                {
                    tag2 = tag1;  //если конечный тэг не указан, то приравниваем его к начальному тэгу
                };
                return tag2 >= tag1;  //ok
            }
            catch
            {
                return false;
            }
        }

        //command:
        // setdb <space> <tag1> [tag2] = <data> [<data>]...
        //where:
        // space ::= (coils | di | ai | hold)
        //examples:
        // setdb holds 100 = 55
        // setdb ai 100 120 = 200 201 202 203
        //---------
        //Если в команде setdb указаны лишние данные, они игнорируются, 
        //если указано меньше данных, чем тэгов, то тэги заполняются последним указанным значением
        public static bool isSetDbTags(string commandLine, ref Space space, ref UInt16 tag1, ref UInt16 tag2, ref UInt16[] data)
        {
            try
            {
                Regex setTagsRe = new Regex(@"^-?setdb (coils?|di|ai|hold)\s*(\d+)\s*(\d*)\s*=\s*(?:(\d+)(?:\s|$))+");
                MatchCollection m = setTagsRe.Matches(commandLine);
                try
                {
                    if (!StringToSpace(m[0].Groups[1].Value, out space))
                        return false;
                }
                catch
                {
                    return false;
                }
                try
                {
                    tag1 = Convert.ToUInt16(m[0].Groups[2].Value);
                }
                catch
                {
                    return false;
                };
                try
                {
                    tag2 = Convert.ToUInt16(m[0].Groups[3].Value);
                }
                catch
                {
                    tag2 = tag1;  //если конечный тэг не указан, то приравниваем его к начальному тэгу
                };

                if(tag2 < tag1)
                    return false;

                Regex tempRe = new Regex(@"=(?:\s*(\d+))+"); //select a text after "="
                MatchCollection tempDataMatches = tempRe.Matches(commandLine);
                string tempS = tempDataMatches[0].ToString();
                Regex dataRe = new Regex(@"(\d+)");
                MatchCollection dataMatches = dataRe.Matches(tempS);

                int quantityOfTags = tag2 - tag1 + 1;
                data = new UInt16[quantityOfTags];
                if (quantityOfTags <= dataMatches.Count)
                {
                    for (int i = 0; i < quantityOfTags; i++)
                    {
                        data[i] = Convert.ToUInt16(dataMatches[i].Value);
                    }
                }
                else
                {
                    for (int i = 0; i < dataMatches.Count; i++)
                    {
                        data[i] = Convert.ToUInt16(dataMatches[i].Value);
                    }
                    UInt16 lastValue = data[dataMatches.Count - 1];
                    for (int i = dataMatches.Count; i < quantityOfTags; i++)
                    { 
                        data[i] = lastValue;
                    }
                }

                return true;  //ok
            }
            catch
            {
                return false;
            }
        }

        //-----------------------------------------------------------
        // set/get storage value by name
        //----------------------------------------------------------
        //command:
        // getdb <name>
        //where <name> is a variable name
        public static bool isGetDbValue(string commandLine, out string varName)
        {
            try
            {
                Regex getDbValueRe = new Regex(@"^getdb ([a-zA-Z_]\w*)$");
                MatchCollection m = getDbValueRe.Matches(commandLine);
                varName = m[0].Groups[1].Value;
                return true;
            }
            catch
            {
                varName = null;
                return false;
            }
        }

        //command:
        // setdb <name> <value>
        //where <name> is a variable name
        public static bool isSetDbValue(string commandLine, out string varName, out double value)
        {
            try
            {
                Regex setDbValueRe = new Regex(@"^setdb ([a-zA-Z_]\w*) ([-+]?[0-9]*\.?[0-9]+(?:[eE][-+]?[0-9]+)?)$");
                MatchCollection m = setDbValueRe.Matches(commandLine);
                varName = m[0].Groups[1].Value;
                value = Converter.stringToDouble(m[0].Groups[2].Value);
                return true;
            }
            catch
            {
                varName = null;
                value = 0;
                return false;
            }
        }

        //-----------------------------------------------------------
        // get all storage variables
        //----------------------------------------------------------
        // getdb -a
        public static bool isGetDbVariables(string commandLine)
        {
            try
            {
                Regex getDbVarsRe = new Regex(@"^-?getdb -a$");
                return getDbVarsRe.IsMatch(commandLine);
            }
            catch
            {
                return false;
            }
        }
        #endregion
       
        #region get/set mode
        public static bool isGetMode(string commandLine)
        {
            Regex getModeRe = new Regex(@"^-?get(\s*)mode");
            return getModeRe.Match(commandLine).Success;
        }

        public static bool isSetMode(string commandLine, ref Mode mode)
        { 
            Regex setModeRe = new Regex(@"^-?set(?:\s*)mode(?:\s+)(slave|master|test)");
            MatchCollection m = setModeRe.Matches(commandLine);
            try
            {
                switch (m[0].Groups[1].ToString())
                {
                    case "slave":
                        {
                            mode = Mode.Slave;
                            return true;
                        }
                    case "master":
                        {
                            mode = Mode.Master;
                            return true;
                        }
                    case "test":
                        {
                            mode = Mode.Test;
                            return true;
                        }
                }
            }
            catch { }
            return false;
        }
        #endregion
        
    }

    class MiscUtils
    {
        public static string help
        {
            get
            {
                string text =
                    "Access to tags:\r\n" +
                    "get <space> <tag1> [tag2]\r\n" +
                    "<space> ::= coil[s]|halt|di|ai\r\n" +
                    "examples:\r\n" +
                    "get coil 123 456\r\n" +
                    "get hold 345\r\n" +
                    "\r\n" +
                    "set <space> <tag1> [tag2] = [<data>]\r\n" +
                    "<space> ::= coil[s]|halt\r\n" +
                    "if tag2 is omitted, it is assumed that tag2 = tag1\r\n" +
                    "if data is omitted, it is assumed that value of all tags = 0 (off for coils)\r\n" +
                    "if data is partially omitted, it is assumed that last value is repeated for all tags.\r\n" +
                    "data for coil can be in form \"on\"/\"off\" or in digital form, 0 is for \"off\",\r\n" +
                    "any other value is for \"on\"\r\n" +
                    "examples:\r\n" +
                    "set coils 123 456 =      // all coils off\r\n" +
                    "set coil 123 = on\r\n" +
                    "set coils 567 569 = on off on     // coil 567 on, coil 568 off, coil 569 on\r\n" +
                    "set coil 100 500 = on off on    // coil 100 on, coil 101 off, coil 102 - 500 on\r\n" +
                    "set hold 400 = 123\r\n" +
                    "set hold 500 502 = 123 124 125\r\n" +
                    "\r\n" +
                    "Access to tag database:\r\n" +
                    "getdb <space> <tag1> [<tag2>]  //getting values of tags from tag1 to tag2\r\n" +
                    "examples:\r\n" +
                    "getdb coils 1000 // get a value of coil number 1000" +
                    "getdb di 500 520 //get values of discrete inputs from 500 to 520\r\n" +
                    "getdb -a //get names of all variables" + 
                    "getdb <varname> //get var value" +
                    "\r\n" +
                    "setdb <space> <tag1> [<tag2>] = <value> ... <value> // set values of tags from tag1 to tag2\r\n" +
                    "examples:\r\n" +
                    "setdb coil 1000 = 1 //turn on a coil number 1000\r\n"+
                    "setdb hold 200 202 = 2000 2010 2020 //set values of holdings: [200] = 2000, [201] = 2010, [202] = 2020\r\n" +
                    "if data is partially omitted, it is assumed that last value is repeated for all tags.\r\n" +
                    "setdb hold 200 202 = 2000 2010 //set values of holdings: [200] = 2000, [201] = 2010, [202] = 2010\r\n" +
                    "setdb <varname> <value> //example: setdb FloatVar 123.456" +
                    "General commands:\r\n" +
                    "p - pause\r\n" +
                    "r - resume\r\n" +
                    "q - quit\r\n" +
                    "config = <filename> //config file name\r\n" +
                    "help //this help\r\n" +
                    "\r\n";
                return text;
            }
        }
    }
}
