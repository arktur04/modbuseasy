using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaseTypes;

namespace TagDataBase
{
    public class TagStorage  //сохранят занчения тэгов в памяти. Потом можно сделать связь с субд
    {
        const int TAGNUM = 65536;

        private bool[] mCoils;
        private bool[] mDInputs;
        private UInt16[] mAInputs;
        private UInt16[] mHoldings;

        public TagStorage()
        {
            mCoils = new bool[TAGNUM];
            mDInputs = new bool[TAGNUM];
            mAInputs = new UInt16[TAGNUM];
            mHoldings = new UInt16[TAGNUM];
        }
        

        public void setTag(ModbusAddress addr, UInt16 value)
        {
            setTag(addr.space, addr.tag, value);
        }

        public void setTag(Space space, UInt16 tag, UInt16 value)
        {
            switch (space)
            {
                case Space.Coils:
                    mCoils[tag] = value != 0;
                    break;
                case Space.DiscreteInputs:
                    mDInputs[tag] = value != 0;
                    break;
                case Space.AnalogInputs:
                    mAInputs[tag] = value;
                    break;
                case Space.Holdings:
                    mHoldings[tag] = value;
                    break;
            }

        }

        public void setTag(Space space, int tag, int value)
        {
            if (tag > UInt16.MaxValue || tag < 0)
                new ArgumentOutOfRangeException(String.Format("tag {0} is out  of range", tag));
            if (value > UInt16.MaxValue || value < 0)
                new ArgumentOutOfRangeException(String.Format("value {0} is out  of range", value));
            setTag(space, (UInt16)tag, (UInt16)value);
        }

        public UInt16 getTag(ModbusAddress addr)
        {
            return getTag(addr.space, addr.tag);
        }

        public UInt16 getTag(Space space, UInt16 tag)
        {
            try
            {
                switch (space)
                {
                    case Space.Coils:
                        return (UInt16)(mCoils[tag] ? 1 : 0);
                    case Space.DiscreteInputs:
                        return (UInt16)(mDInputs[tag] ? 1 : 0);
                    case Space.AnalogInputs:
                        return mAInputs[tag];
                    case Space.Holdings:
                        return mHoldings[tag];
                }
            }
            catch { }
            throw new Exception("getTag error");
        }

        public UInt16 getTag(Space space, int tag)
        {
            if (tag > UInt16.MaxValue || tag < 0)
                new ArgumentOutOfRangeException(String.Format("tag {0} is out  of range", tag));
            return getTag(space, (UInt16)tag);
        }
    }

    //  ---------------------------------------------
    //  не работает! отладить потом.
    /*
    public class DbStorage
    {
        private SQLiteConnection connection;

        public string dbName { get; set; }

        public dbStorage()
        {
            dbName = @"Data Source=file::memory:;Version=3;";
            //dbName = @"Data Source=C:\Users\V\Desktop\SQLite\test3.db;Version=3;";
        }

        public void Connect()
        {
            connection = new SQLiteConnection(dbName);
            connection.Open();
        }

        public void Connect(string name)
        {
            connection = new SQLiteConnection(name);
            connection.Open();
        }

        public void Disconnect()
        {
            connection.Close();
        }
        
        public void CreateTables()
        {
            const int NUMTAGS = 200; //65536;
            SQLiteCommand command;
            try
            {
                command = new SQLiteCommand("CREATE TABLE 'coils' ('tag' INTEGER PRIMARY KEY NOT NULL UNIQUE, 'value' BOOLEAN NOT NULL );", connection);
                command.ExecuteNonQuery();
                command = new SQLiteCommand("CREATE TABLE dinputs ('tag' INTEGER PRIMARY KEY NOT NULL UNIQUE, 'value' BOOLEAN NOT NULL );", connection);
                command.ExecuteNonQuery();
                command = new SQLiteCommand("CREATE TABLE 'ainputs' ('tag' INTEGER PRIMARY KEY NOT NULL UNIQUE, 'value' BOOLEAN NOT NULL );", connection);
                command.ExecuteNonQuery();
                command = new SQLiteCommand("CREATE TABLE 'holdings' ('tag' INTEGER PRIMARY KEY NOT NULL UNIQUE, 'value' BOOLEAN NOT NULL );", connection);
                command.ExecuteNonQuery();
            }
            catch { }

            for (int i = 0; i < NUMTAGS; i++)
            {
                command = new SQLiteCommand(String.Format("INSERT OR REPLACE INTO coils (tag, value) VALUES ({0}, 'false');", i), connection);
                command.ExecuteNonQuery();
                Console.WriteLine("coils tag: " + i.ToString());
            }

            for (int i = 0; i < NUMTAGS; i++)
            {
                command = new SQLiteCommand(String.Format("INSERT OR REPLACE INTO dinputs (tag, value) VALUES ({0}, 'false');", i), connection);
                command.ExecuteNonQuery();
                Console.WriteLine("dinputs tag: " + i.ToString());
            }

            for (int i = 0; i < NUMTAGS; i++)
            {
                command = new SQLiteCommand(String.Format("INSERT OR REPLACE INTO ainputs (tag, value) VALUES ({0}, {0});", i), connection);
                command.ExecuteNonQuery();
                Console.WriteLine("ainputs tag: " + i.ToString());
            }

            for (int i = 0; i < NUMTAGS; i++)
            {
                command = new SQLiteCommand(String.Format("INSERT OR REPLACE INTO holdings (tag, value) VALUES ({0}, {0});", i), connection);
                command.ExecuteNonQuery();
                Console.WriteLine("holdings tag: " + i.ToString());
            }
        }

        private string spaceToTableName(Space space)
        {
            switch (space)
            {
                case Space.Coils:
                    return "coils";
                case Space.DiscreteInputs:
                    return "dinputs";
                case Space.AnalogInputs:
                    return "ainputs";
                case Space.Holdings:
                    return "holdings";
            };
            throw new ArgumentException("wrong space value");
        }

        public UInt16 getTag(ModbusAddress addr)
        {
            if (addr == null)
                throw new Exception("Address is null");
            try
            {
                SQLiteCommand command = new SQLiteCommand(String.Format("SELECT * FROM {0} WHERE tag='{1}'", spaceToTableName(addr.space), addr.tag), connection);
                SQLiteDataReader reader = command.ExecuteReader();
                string s = reader["value"].ToString();
                switch(s.ToLower())
                {
                    case "false": return 0;
                    case "true": return 0xFF00;
                    default: return Convert.ToUInt16(s);
                }
            }
            catch
            {
                throw new Exception("Reading tag error");
            }
        }

        public void setTag(ModbusAddress addr, UInt16 value)
        {
            if (addr == null)
                throw new Exception("Address is null");
            try
            {
                string val = null;
                switch (addr.space)
                {
                    case Space.Coils:
                    case Space.DiscreteInputs:
                        val = (value != 0) ? "true" : "false";
                        break;
                    case Space.AnalogInputs:
                    case Space.Holdings:
                        val = value.ToString();
                        break;
                }
                SQLiteCommand command = new SQLiteCommand(String.Format("UPDATE {0} SET value='{1}' WHERE tag='{2}';", spaceToTableName(addr.space), val, addr.tag), connection);
                command.ExecuteNonQuery();
            }
            catch
            {
                throw new Exception("Update tag error");
            }
        }
     
    }*/
}
