using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Threading;
using System.IO;
using System.Data;
using MySql.Data.MySqlClient;

namespace TextApp
{
    class Program
    {
        static void Main(string[] args)
        {
            new Program().GetConfig();
            Console.ReadKey();
            //Thread taska = new Thread(JobA);
            //Thread taskb = new Thread(() => JobB("ShowTime"));
            //taska.Start();
            //taskb.Start();

        }

        string SQLConnectString = "server=escodb.cou5lzgkdcjg.ap-northeast-1.rds.amazonaws.com;user=root;database=auodb2;port=3306;password=LqaZ2wsX;Charset=utf8;Connection Timeout=10000;";
        Dictionary<string, double> TodaySun = new Dictionary<string, double>();

        private string GetTodaySUN( )
        {
            string Result = "";

           
                DataTable Table = new DataTable();
                DataRow[] RowsRaw = new DataRow[] { };
                double SumSun = 0.0;
                double Value = 0.0;
                string dataTime = "";
                int mins = 1;
                double Coef = 1 * (60 / mins);
                DateTime RealT = DateTime.Now;
                Table = ScanSQL(RealT, mins, VM.DBdpids, VM.DBdsid);

                for (int i = 0; i < 5 * mins; i++)
                {
                    dataTime = RealT.AddMinutes(-(i + 1)).ToString("yyyy-MM-dd HH:mm:00");
                    SumSun = 0.0;
                    if (TodaySun.Count > 0)
                    {
                        if (TodaySun.ContainsKey(dataTime)) { SumSun = TodaySun[dataTime]; break; }
                        else
                        {
                            if (i > mins) { SumSun = -1.0; break; }
                        }
                    }
                    else
                    {
                        SumSun = 0.0;
                        break;
                    }

                }

                for (int i = 0; i < 5 * mins; i++)
                {
                    dataTime = RealT.AddMinutes(-i).ToString("yyyy-MM-dd HH:mm:00");
                    Value = 0.0;
                    try { RowsRaw = Table.Select("time ='" + dataTime + "'"); }
                    catch { RowsRaw = new DataRow[] { }; }
                    if (RowsRaw.Length > 0)
                    {
                        try
                        {
                            Value = double.Parse(RowsRaw[RowsRaw.Length - 1]["value"].ToString());
                        }
                        catch (Exception ex)
                        {
                            Value = 0.0;
                            Console.WriteLine($"GetSUN() > Value, {ex.Message} at time : {dataTime}");
                        }
                        break;
                    }
                    else
                    {
                        if (i > mins) { Value = -1.0; break; }
                    }

                }

                if (Value >= 0.0 && SumSun >= 0.0)
                {
                    SumSun = Math.Round(SumSun + Value / Coef, 3);
                }
                Result = $"{dataTime}={SumSun.ToString()}";
                TodaySun.Add(dataTime, SumSun);
           
            return Result;
        }

        private DataTable ScanSQL(DateTime RealTime, int mins, string Dpids, string SourceID)
        {
            string SqlCom;
            DataSet Dats = new DataSet();
            DataTable Table = new DataTable();
            MySqlDataAdapter datadp = null;
            MySqlConnection SQLConnetion = new MySqlConnection(SQLConnectString);
            string startTime = RealTime.ToString("yyyy-MM-dd HH:mm:00");
            string endTime = RealTime.AddMinutes(-5 * mins).ToString("yyyy-MM-dd HH:mm:00");
            SqlCom = "SELECT DATE_FORMAT(record_time,'%Y-%m-%d %H:%i:%00') as time , data_value as value, dpid FROM sys_data_raw where dsid = " + SourceID + " AND dpid in (" + Dpids + ") and record_time >= '" + startTime + "' and record_time <= '" + endTime + "' AND data_value >= 0  Order by time ASC;";
            try
            {
                SQLConnetion.Open();
                datadp = new MySqlDataAdapter(SqlCom, SQLConnetion);
                datadp.Fill(Dats, "tab");
                Table = Dats.Tables["tab"];
                SQLConnetion.Close();
                Console.WriteLine(DateTime.Now + " : Scan_mysql( ) finished !!! ");
            }
            catch
            {
                SQLConnetion.Close();
                Console.WriteLine(DateTime.Now + " : Scan_mysql( ) filure ");
                //Write_File(PathLOG + DateTime.Now.ToString("yyyyMMdd") + ".txt", DateTime.Now.ToString("HH:mm") + " :  ErrorMessage : ScanSQL ( ) failure \r\n", true);
            }

            return Table;
        }

        static void JobA()
        {
            
            for (int i = 0; i < 60; i++)
            {
                Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId} NowTime : ");
                if (i == 60 - 1) { i = 0; }
                Thread.Sleep(1000);

            }
           
        }

        static void JobB(object para)
        {
            for (int i = 0; i < 30; i++)
            {
                Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} > {Thread.CurrentThread.ManagedThreadId}");
                if (i == 60 - 1) { i = 0; }
                Thread.Sleep(2000);

            }

        }

        public void GetData()
        {
            Thread taska = new Thread(JobA);
            taska.Start();
            for (int i = 0; i < 5; i++)
            {
                Thread.Sleep(2000);
            }

        }
         public void GetConfig()
        {
            DpidConfig JConfig = new DpidConfig();
            Dictionary<string, string> dpid = new Dictionary<string, string>();
            string[] ConfigJString = new string[] { };
            if (File.Exists(Environment.CurrentDirectory + @"\SUM.csv"))
            {
                ConfigJString = File.ReadAllLines(Environment.CurrentDirectory + @"\SUM.csv");
            }
            else { }
            string TXT = "";
            double sumSun = 0;
            double sum = 0;
            for (int i = 1; i < ConfigJString.Length; i++)
            {
                //if (ConfigJString[i] != "") { dpid.Add(ConfigJString[i].Split(',')[1], ConfigJString[i].Split(',')[0]); }
                
                try { sum += double.Parse(ConfigJString[i].Split(',')[2])/12000; }
                catch {  }
                
                try { sumSun += double.Parse(ConfigJString[i].Split(',')[2])/12000.0; }
                catch {  }

                TXT += $"{ConfigJString[i].Split(',')[0]},{ConfigJString[i].Split(',')[1]},{Math.Round(sum,3)},{Math.Round(sumSun,3)}\n";
            }
            Console.WriteLine(TXT);
            //JConfig.SourceID = "DEKRA_test";
            //JConfig.Headline = 9;
            //JConfig.Logdatapath = @"D:\DATA LOG\";
            //JConfig.Realdatapath = @"C:\Program Files (x86)\MELSOFT\SGT2000\Multi\00001\Drive\A\Package1\LOG00001\";
            //JConfig.Dataline = 13;
            //JConfig.DataFiles = new List<string> { "LOG_AIO.CSV", "LOG_PM.CSV" };
            //JConfig.Dpids = dpid;
            //string dpidString = JsonConvert.SerializeObject(JConfig, Formatting.Indented);

            //dpid.Add("dataPoint","211");
            //dpid.Add("Coef","1");
            //dpid.Add("Adress", "0x101C");
            //dpid.Add("Num", "1");
            //dpid.Add("Unit", "˚C");
            //dpid.Add("Type", "S16");
            //DataMap map = new DataMap("1", dpid);
            //List<DataMap> maplist = new List<DataMap>();
            //maplist.Add(map);
            //ModbusConfig config1 = new ModbusConfig("COM1", "9600", "TM", maplist);
            //ModbusConfig config2 = new ModbusConfig("COM1", "9600", "INV", maplist);
            //List<ModbusConfig> configlist = new List<ModbusConfig>();
            //configlist.Add(config1);
            //configlist.Add(config2);
            //Config ModbusConofig = new Config("DFI_SOLAR", configlist);

            //string dpidString = JsonConvert.SerializeObject(ModbusConofig, Formatting.Indented);
        }
    }

    

    class Config
    {
       public string SourceID { get; set; }

       public List<ModbusConfig> DataConfig { get; set; }
        public Config(string id, List<ModbusConfig> map)
        {
            SourceID = id;
            DataConfig = map;
        }

    }

    class ModbusConfig
    {
        public string PortName { get; set; }

        public string BaudRate { get; set; }
        public string MeterType { get; set; }
       
        public List<DataMap> Device { get; set; }
        public ModbusConfig(string name, string rate, string type, List<DataMap> map)
        {
            PortName = name;
            BaudRate = rate;
            MeterType = type;
            Device = map;
        }
     }

    class DataMap
    {
        public string SlaveID { get; set; }
        public Dictionary<string,string> Datamap { get; set; }
        public DataMap(string id, Dictionary<string, string> map)
        {
            SlaveID = id;
            Datamap= map;
        }
    }

    class DpidConfig
    {
        public string SourceID { get; set; }
        public int Dataline { get; set; }
        public int Headline { get; set; }
        public List<string> DataFiles { get; set; }
        public string Realdatapath { get; set; }
        public string Logdatapath { get; set; }
        public Dictionary<string, string> Dpids { get; set; }
    }
}
