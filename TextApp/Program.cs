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
using System.Windows.Markup;

namespace TextApp
{
    class Program
    {
        static void Main(string[] args)
        {
            new Program().GetData();
            Console.ReadKey();
        }
        DataTable Table = new DataTable();
        DataRow[] RowsRaw = new DataRow[] { };
        string SQLConnectString = "server=escodb.cou5lzgkdcjg.ap-northeast-1.rds.amazonaws.com;user=root;database=auodb2;port=3306;password=LqaZ2wsX;Charset=utf8;Connection Timeout=10000;";
        Dictionary<string, double> TodaySun = new Dictionary<string, double>();

        private void GetRawData( )
        {
             
                DateTime RealT= DateTime.Now;
                int mins= 1;
                string dpids="80439,80476,80477";
                string dsids="888,891";
                Table = ScanSQL(RealT, mins,dpids,dsids);
           
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
            SqlCom = "SELECT DATE_FORMAT(record_time,'%Y-%m-%d %H:%i:%00') as time , data_value as value, dpid FROM sys_data_raw where dsid in ( " + SourceID + " ) AND dpid in (" + Dpids + ") and record_time >= '" + startTime + "' and record_time <= '" + endTime + "' AND data_value >= 0  Order by time ASC;";
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

        
        public void GetData()
        {
            
            GetRawData( );
            DateTime RealTime= DateTime.Now;
            string Sumdpids= "10000";
            string Sumvalues="";
            try { RowsRaw = Table.Select("time ='" + RealTime.AddMinutes(-1).ToString("yyyy-MM-dd HH:mm:00") + "' AND dpid = '"+Sumdpids+"'" ); }
            catch { RowsRaw = new DataRow[] { }; }
            if (RowsRaw.Length > 0)
            {
                Sumvalues= RowsRaw[RowsRaw.Length-1]["value"].ToString();   
               Console.WriteLine($"Sumvalues > {Sumvalues}, RealTime.AddMinutes(-1) >{RealTime.AddMinutes(-1)}");
            }
            else
            {

                 Console.WriteLine($"umvalues > null ");
            }
            string Sundpids= "80476";
            string sun="";
            try { RowsRaw = Table.Select("time ='" + RealTime.ToString("yyyy-MM-dd HH:mm:00") + "' AND dpid = '"+Sundpids+"'" ); }
            catch { RowsRaw = new DataRow[] { }; }
            if (RowsRaw.Length > 0)
                {
                sun= RowsRaw[RowsRaw.Length-1]["value"].ToString();   
                Console.WriteLine($"Sun > {sun}, RealTime >{RealTime}");
                }
                else
                {
                  Console.WriteLine($"Sun > null ");
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
