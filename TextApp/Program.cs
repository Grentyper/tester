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
            new Program().GetRawData();
            Console.ReadKey();
        }
        DataTable Table = new DataTable();
        DataRow[] RowsRaw = new DataRow[] { };
        string SQLConnectString = "server=escodb.cou5lzgkdcjg.ap-northeast-1.rds.amazonaws.com;user=root;database=auodb2;port=3306;password=LqaZ2wsX;Charset=utf8;Connection Timeout=10000;";
        Dictionary<string, double> TodaySun = new Dictionary<string, double>();
        DateTime RealT = DateTime.Now;
        int mins = 1;
        string dpids = "";
        string dsids = "";
        private void GetRawData( )
        {
            mins= 1;
            dpids= "80476,80552,80594,80636,80678,80720,80762,83983,83984,83985,83986,83987,83988,83989,84658,84659,84664,84665";
            dsids="891,42,43";
            //Table = ScanSQL(RealT, mins,dpids,dsids);
            //for (int i = 0; i < Table.Rows.Count; i++)
            //{
            //    Console.WriteLine(DateTime.Now + $" :  {Table.Rows[i]["time"]}={Table.Rows[i]["dpid"]}={Table.Rows[i]["value"]} ");
            //}
            //try { GetRealTime(); }
            //catch (Exception ex) { Console.WriteLine(DateTime.Now + $" :  GetSun() filure+{ ex.ToString()} "); }
            //try { GetSun(); }
            //catch (Exception ex) { Console.WriteLine(DateTime.Now + $" :  GetSun() filure+{ ex.ToString()} "); }


            Timer _timer = new Timer(TimerCallback, null, 0, 1 * 60 * 1000);//執行每1分鐘執行一次程式
            Console.ReadLine();// Wait for the user to hit <Enter> 
            


        }

        private void TimerCallback(Object o)
        {
            RealT = DateTime.Now;
            Table = ScanSQL(RealT, mins, dpids, dsids);
            Console.WriteLine($"getTerarT{RealT}" + "\n");
            try { GetRealTime(); }
            catch (Exception ex) { Console.WriteLine(DateTime.Now + $" :  GetSun() filure+{ ex.ToString()} "); }
            try { GetSun(); }
            catch (Exception ex) { Console.WriteLine(DateTime.Now + $" :  GetSun() filure+{ ex.ToString()} "); }

        }
    

        public void GetSun( )
        {
            
            Dictionary<string, string> values = new Dictionary<string, string>();
            values=GetSunData("80476", "83989","891", RealT);
            InsertData("83989", "1_1_1", "891", "QTY_SOLAR", values);
           
           
        }
        public void GetRealTime()
        {
           
            Dictionary<string, string> values = new Dictionary<string, string>();
            values = GetRealTimeData("84658", "84664","42,43", RealT);
            InsertData("84664", "112", "43", "GLOBALWAFERS_HC_AC", values);
           

            values = GetRealTimeData("84659", "84665","42,43", RealT);
            InsertData("84665", "1112", "43", "GLOBALWAFERS_HC_AC", values);
            

        }
        public Dictionary<string, string> GetRealTimeData(string dpid, string vmdpid, string Sourceid, DateTime RealTime)
        {
            string vmdata = "";
            Dictionary<string, string> DpidValue = new Dictionary<string, string>();
            
            try { RowsRaw = Table.Select(" dpid = '" + vmdpid + "'"); }
            catch { RowsRaw = new DataRow[] { }; }
            //for (int i = 0; i < RowsRaw.Length; i++)
            //{
            //    Console.WriteLine($" VMDATA Realtime from TABLE :  {RowsRaw[i]["time"]}={RowsRaw[i]["dpid"]}={RowsRaw[i]["value"]} ");
            //}
            if (RowsRaw.Length > 0)
            {  
                for (int i = 0; i <5; i++)
                {
                    //Console.WriteLine($" VMDATA Realtime from TABLE :  {RowsRaw[i]["time"]}={RowsRaw[i]["dpid"]}={RowsRaw[i]["value"]} ");
                    DateTime dataT = RealTime.AddMinutes(-5 + i);
                    try { RowsRaw = Table.Select("time ='" + dataT.ToString("yyyy-MM-dd HH:mm:00") + "' AND dpid = '" + vmdpid + "'"); }
                    catch { RowsRaw = new DataRow[] { }; }
                    if (RowsRaw.Length == 0)
                    {
                        //Console.WriteLine($"VM Want time>");
                        vmdata = "";
                        try { RowsRaw = Table.Select("time ='" + dataT.ToString("yyyy-MM-dd HH:mm:00") + "' AND dpid = '" + dpid + "'"); }
                        catch { RowsRaw = new DataRow[] { }; }
                        if (RowsRaw.Length > 0)
                        {
                            vmdata = RowsRaw[RowsRaw.Length - 1]["value"].ToString();
                            if (vmdata == "") { vmdata = "0.0"; }
                            Console.WriteLine($"{dataT.ToString("yyyy-MM-dd HH:mm:00")} >{dpid}={vmdpid} > {vmdata} ");
                            if (vmdata != "") { DpidValue.Add(dataT.ToString("yyyy-MM-dd HH:mm:00"), vmdpid + "=" + vmdata); }

                        }

                    }

                }
            }
            else//如果沒有虛擬點位的資料從頭來算
            {
                //Console.WriteLine($"get Init data \n");
                DpidValue = GetInitData(DateTime.Parse($"{RealTime.ToString("yyyy-MM-dd")} 00:00:00"), RealTime, dpid, vmdpid, Sourceid);
                //DpidValue = GetInitData(DateTime.Parse($"2024-02-27 15:00:00"), DateTime.Parse($"2024-02-28 00:00:00"), dpid, vmdpid, Sourceid);

            }
            return DpidValue;
        }
        private Dictionary<string, string> GetInitData(DateTime startTime, DateTime endTime, string dpid,string vmdpid, string SourceID)
        {
            Dictionary<string, string> vmDpidValue = new Dictionary<string, string>();
           
            DataRow[] Raw = new DataRow[] { };
            DataTable InitTable = ScanSQL(startTime.ToString("yyyy-MM-dd HH:mm:00"), endTime.AddMinutes(-1).ToString("yyyy-MM-dd HH:mm:00"), dpid+","+ vmdpid, SourceID);
            //for (int i = 0; i < InitTable.Rows.Count; i++)
            //{
            //    Console.WriteLine(DateTime.Now + $" :  {InitTable.Rows[i]["time"]}={InitTable.Rows[i]["dpid"]}={InitTable.Rows[i]["value"]} ");
            //}
            try { Raw = InitTable.Select("dpid = '" + vmdpid + "'"); }
            catch { Raw = new DataRow[] { }; }
            if (Raw.Length > 0)
            {
                startTime = DateTime.Parse(Raw[Raw.Length - 1]["time"].ToString());
                //Console.WriteLine($"Raw[Raw.Length - 1].ToString() ={Raw[Raw.Length - 1]["time"].ToString()}");
            }
            
           // Console.WriteLine($"startTime ={startTime.ToString()}");
            string data = "";
            for (int i = 0; i < (endTime - startTime).TotalMinutes; i++)
            {

                try { Raw = InitTable.Select("time ='" + startTime.AddMinutes(i+1).ToString("yyyy-MM-dd HH:mm:00") + "' AND dpid = '" + dpid + "'"); }
                catch { Raw = new DataRow[] { }; }

                if (Raw.Length > 0)
                {
                    data = Raw[Raw.Length - 1]["value"].ToString();
                    //Console.WriteLine("time ='" + startTime.AddMinutes(i).ToString("yyyy-MM-dd HH:mm:00") + "' AND dpid = '" + dpid + "'"+ data);
                }

                try
                {
                    // Console.WriteLine($"time={startTime.AddMinutes(i).ToString("yyyy -MM-dd HH:mm:00")} , {dpid}={data}");
                    if (data != "") { vmDpidValue.Add(startTime.AddMinutes(i+1).ToString("yyyy-MM-dd HH:mm:00"), vmdpid + "=" + data); }
                }
                catch (Exception ex) { Console.WriteLine($"GetInitData() Error ={ex.ToString()}"); }

            }
            return vmDpidValue;
        }


        public Dictionary<string, string> GetSunData(string dpid, string vmdpid,string Sourceid , DateTime RealTime)
        {
            string SumSun = "";
            string Sun = "";
            double Coef = 1 * (60 / 1);
          
            Dictionary<string, string> DpidValue = new Dictionary<string, string>();

            try { RowsRaw = Table.Select(" dpid = '" + vmdpid + "'"); }
            catch { RowsRaw = new DataRow[] { }; }
            //for (int i = 0; i < RowsRaw.Length; i++)
            //{
            //    Console.WriteLine($" VMDATA Realtime from TABLE :  {RowsRaw[i]["time"]}={RowsRaw[i]["dpid"]}={RowsRaw[i]["value"]} ");
            //}
            if (RowsRaw.Length > 0)
            {
                for (int i = 0; i < 4; i++)
                {
                    DateTime dataT = RealTime.AddMinutes(-4 + i);
                    
                    try { RowsRaw = Table.Select("time ='" + dataT.ToString("yyyy-MM-dd HH:mm:00") + "' AND dpid = '" + vmdpid + "'"); }
                    catch { RowsRaw = new DataRow[] { }; }
                    if (RowsRaw.Length ==0)
                    {
                                           
                        try { RowsRaw = Table.Select("time ='" + dataT.AddMinutes(-1).ToString("yyyy-MM-dd HH:mm:00") + "' AND dpid = '" + vmdpid + "'"); }
                        catch { RowsRaw = new DataRow[] { }; }
                        if (RowsRaw.Length > 0)
                        {
                            //for (int N = 0; N < RowsRaw.Length; N++)
                            //{
                            //    Console.WriteLine($" VMDATA Realtime :  {RowsRaw[N]["time"]}={RowsRaw[N]["dpid"]}={RowsRaw[N]["value"]} ");
                            //}
                            if (dataT.Hour == 0) { SumSun = "0.0"; }
                            else
                            {
                                SumSun = RowsRaw[RowsRaw.Length - 1]["value"].ToString();
                            }

                        }

                        try { RowsRaw = Table.Select("time ='" + dataT.ToString("yyyy-MM-dd HH:mm:00") + "' AND dpid = '" + dpid + "'"); }
                        catch { RowsRaw = new DataRow[] { }; }
                        if (RowsRaw.Length > 0)
                        {
                            Sun = RowsRaw[RowsRaw.Length - 1]["value"].ToString();
                            if (SumSun == "") { SumSun = "0.0"; }
                            if (Sun == "") { Sun = "0.0"; }
                            SumSun = Math.Round(double.Parse(SumSun) + double.Parse(Sun) / Coef, 3).ToString();
                            Console.WriteLine($"{dataT.ToString("yyyy-MM-dd HH:mm:00")} > {dpid}={Sun} + {vmdpid}={SumSun} ");
                            if (SumSun != "")
                            { DpidValue.Add(dataT.ToString("yyyy-MM-dd HH:mm:00"), vmdpid + "=" + SumSun); }

                        }
                        else
                        {
                            Sun = "";
                        }

                    }

                }
            }
            else//如果沒有虛擬點位的資料從頭來算
            {
                DpidValue=GetSunInitData(DateTime.Parse($"{RealTime.ToString("yyyy-MM-dd")} 00:00:00"), RealTime, dpid, vmdpid, Sourceid);
               //DpidValue=GetSunInitData(DateTime.Parse("2024-02-28 09:00:00"), DateTime.Parse($"2024-02-28 10:00:00"), dpid, vmdpid, Sourceid);

            }

            return DpidValue;
        }

        private Dictionary<string, string> GetSunInitData(DateTime startTime, DateTime endTime, string dpid, string vmdpid, string SourceID)
        {
            Dictionary<string, string> vmDpidValue = new Dictionary<string, string>();
            double Coef = 1 * (60 / 1);
            DataRow[] Raw = new DataRow[] { };
            DataTable InitTable = ScanSQL(startTime.ToString("yyyy-MM-dd HH:mm:00"), endTime.AddMinutes(-1).ToString("yyyy-MM-dd HH:mm:00"), dpid+","+ vmdpid, SourceID);
            
            try { Raw = InitTable.Select("dpid = '" + vmdpid + "'"); }
            catch { Raw = new DataRow[] { }; }
            if (Raw.Length > 0)
            {
                //for (int i = 0; i < Raw.Length; i++)
                //{
                //    Console.WriteLine( $" :  {Raw[i]["time"]}={Raw[i]["dpid"]}={Raw[i]["value"]} ");
                //}
                startTime = DateTime.Parse(Raw[Raw.Length - 1]["time"].ToString());
               
            //Console.WriteLine($"Raw[Raw.Length - 1].ToString() ={Raw[Raw.Length - 1]["time"].ToString()}");
            }
            
            string data = "";
            string vmdata = "";
            
            for (int i = 0; i < (endTime- startTime).TotalMinutes-2; i++)
            {
                DateTime dataT = startTime.AddMinutes(i + 1);
                //Console.WriteLine($"vmdpid  ={vmdpid }");
                try { Raw = InitTable.Select("time ='" + dataT.AddMinutes(-1).ToString("yyyy-MM-dd HH:mm:00") + "' AND dpid = '" + vmdpid + "'"); }
                catch {Raw = new DataRow[] { }; }

                if (Raw.Length > 0)
                {
                     vmdata = Raw[Raw.Length - 1]["value"].ToString();
                }
                
                //Console.WriteLine($"vmdata time  ={dataT.AddMinutes(-1).ToString("yyyy-MM-dd HH:mm")} && vmdata  ={vmdata}");


                try { Raw = InitTable.Select("time ='" + dataT.ToString("yyyy-MM-dd HH:mm:00") + "' AND dpid = '" + dpid + "'"); }
                catch { Raw = new DataRow[] { }; }

                if (Raw.Length > 0)
                {
                    data = Raw[Raw.Length - 1]["value"].ToString();
                }
                else { data = "0.0"; }

                //Console.WriteLine("data time ='" + dataT.ToString("yyyy-MM-dd HH:mm:00") + "' AND dpid = '" + dpid + "'" + data);
                try
                { 
                    vmdata = Math.Round(double.Parse(vmdata) + double.Parse(data) / Coef, 4).ToString();
                    //Console.WriteLine($"time={dataT.ToString("yyyy - MM - dd HH: mm:00")} , {dpid}={data} + vmdata : {vmdpid}={vmdata}");
                    if (vmdata != "") { vmDpidValue.Add(dataT.ToString("yyyy-MM-dd HH:mm:00"), vmdpid + "=" + vmdata); }
                    //Thread.Sleep(10);
                }
                catch(Exception ex){ Console.WriteLine($"GetInitData() Error ={ex.ToString()}"); }
               
            }
            return vmDpidValue;
        }
        public void InsertData(string dpid, string datapoint, string Sourceid, string SourceName, Dictionary<string, string> values)
        {
            string SQLString = "";
            string Value = "";
            //Console.WriteLine("values.Count / 60 : " + values.Count / 60);
            if (values.Count / 60.0 > 1)
            {
                
                for (int i = 0; i < values.Count / 60; i++)
                {

                    SQLString = "";
                    for (int m = i * 60; m < (i + 1) * 60; m++)
                    {
                        Value = values.ElementAt(m).Value.Split('=')[1];
                        if (Value != "") { 
                        if (m == (i + 1) * 60 - 1)
                        {
                            SQLString += $"({Sourceid} , {dpid} ,'{SourceName}','{datapoint}', DEFAULT,'{values.ElementAt(m).Key}','{Value}','{Value}',{Value },{Value });";
                        }
                        else
                        {
                            SQLString += $"({Sourceid} , {dpid} ,'{SourceName}','{datapoint}', DEFAULT,'{values.ElementAt(m).Key}','{Value}','{Value}',{Value},{Value }),\n";
                        }
                        }
                    }
                    if (SQLString != "")
                    {
                        SQLString = "INSERT INTO sys_data_raw VALUES " + SQLString;
                        InsertSQL(SQLString);
                        //Console.WriteLine(DateTime.Now + " : " + SQLString);
                        Write_File(Environment.CurrentDirectory +@"\"+ RealT.ToString("ddHH") + ".txt", SQLString, true);
                        Thread.Sleep(10);
                    }
                    
                }

                if (values.Count / 60.0 - values.Count / 60 > 0.0)
                {

                    SQLString = "";

                    for (int m = (values.Count / 60) * 60; m < values.Count; m++)
                    {
                        Value = values.ElementAt(m).Value.Split('=')[1];
                        if (Value != "")
                        {
                            if (m == values.Count - 1)
                            {
                                SQLString += $"({Sourceid} , {dpid} ,'{SourceName}','{datapoint}', DEFAULT,'{values.ElementAt(m).Key}','{Value }','{Value }',{Value },{Value });";
                            }
                            else
                            {
                                SQLString += $"({Sourceid} , {dpid} ,'{SourceName}','{datapoint}', DEFAULT,'{values.ElementAt(m).Key}','{Value }','{Value }',{Value },{Value }),\n";
                            }
                        }
                    }
                    if (SQLString != "")
                    {
                        SQLString = "INSERT INTO sys_data_raw VALUES " + SQLString;
                        InsertSQL(SQLString);
                        //Console.WriteLine(DateTime.Now + " : " + SQLString);
                        Write_File(Environment.CurrentDirectory + @"\" + RealT.ToString("ddHH") + ".txt", SQLString, true);
                        Thread.Sleep(10);
                    }
                }
            }
            else if(values.Count / 60.0 <= 1)
            {
                SQLString = "";

                for (int m = 0; m < values.Count; m++)
                {
                    Value = values.ElementAt(m).Value.Split('=')[1];
                    if (Value != "")
                    {
                        if (m == values.Count - 1)
                        {
                            SQLString += $"({Sourceid} , {dpid} ,'{SourceName}','{datapoint}', DEFAULT,'{values.ElementAt(m).Key}','{Value }','{Value }',{Value },{Value });";
                        }
                        else
                        {
                            SQLString += $"({Sourceid} , {dpid} ,'{SourceName}','{datapoint}', DEFAULT,'{values.ElementAt(m).Key}','{Value }','{Value }',{Value },{Value }),\n";
                        }
                    }
                }
                if (SQLString != "")
                {
                    SQLString = "INSERT INTO sys_data_raw VALUES " + SQLString;
                    InsertSQL(SQLString);
                    //Console.WriteLine(DateTime.Now + " : " + SQLString);
                    Write_File(Environment.CurrentDirectory + @"\" + RealT.ToString("ddHH") + ".txt", SQLString, true);
                    Thread.Sleep(10);
                }
            }

            
        }
        private DataTable ScanSQL(string startTime, string endTime, string Dpids, string SourceID)
        {
            string SqlCom;
            DataSet Dats = new DataSet();
            DataTable Table = new DataTable();
            MySqlDataAdapter datadp = null;
            MySqlConnection SQLConnetion = new MySqlConnection(SQLConnectString);
            SqlCom = "SELECT DATE_FORMAT(record_time,'%Y-%m-%d %H:%i:%00') as time , data_value as value, dpid FROM sys_data_raw where dsid in (" + SourceID + ") AND dpid in (" + Dpids + ") and record_time >= '" + startTime + "' and record_time <= '" + endTime + "' AND data_value >= 0  Order by time ASC;";
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

        private DataTable ScanSQL(DateTime RealTime, int mins, string Dpids, string SourceID)
        {
            string SqlCom;
            DataSet Dats = new DataSet();
            DataTable Table = new DataTable();
            MySqlDataAdapter datadp = null;
            MySqlConnection SQLConnetion = new MySqlConnection(SQLConnectString);
            string endTime = RealTime.ToString("yyyy-MM-dd HH:mm:00");
            string startTime = RealTime.AddMinutes(-5 * mins).ToString("yyyy-MM-dd HH:mm:00");

            SqlCom = "SELECT DATE_FORMAT(record_time,'%Y-%m-%d %H:%i:%00') as time, data_value as value, dpid FROM sys_data_raw where dsid in (" + SourceID + ") AND dpid in (" + Dpids + ") and record_time >= '" + startTime + "' and record_time <= '" + endTime + "' AND data_value >=0  Order by time ASC;";

            try
            {
                SQLConnetion.Open();
                datadp = new MySqlDataAdapter(SqlCom, SQLConnetion);
                datadp.Fill(Dats, "tab");
                Table = Dats.Tables["tab"];
                SQLConnetion.Close();
                Console.WriteLine(DateTime.Now + " : Scan_mysql( ) finished !!!");
            }
            catch
            {
                SQLConnetion.Close();
                Console.WriteLine(DateTime.Now + " : Scan_mysql( ) filure ");
                //Write_File(PathLOG + DateTime.Now.ToString("yyyyMMdd") + ".txt", DateTime.Now.ToString("HH:mm") + " :  ErrorMessage : ScanSQL ( ) failure \r\n", true);
            }

            return Table;
        }

        private bool InsertSQL(string SQLString)
        {
            bool DBinsertFinish = false;


            if (SQLString == "")
            {
                DBinsertFinish = false;
                // Write_File(PathDataLog + DateTime.Now.ToString("yyyy-MM-dd") + ".txt", "No Insert SQLString; \r\n", true);
            }
            else
            {

                MySqlConnection conn = new MySqlConnection(SQLConnectString);
                try
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand(SQLString, conn);
                    cmd.ExecuteNonQuery();
                    conn.Close();
                    DBinsertFinish = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    DBinsertFinish = false;
                    if (conn.State == ConnectionState.Open)
                    {
                        conn.Close();
                    }
                }
            }
            return DBinsertFinish;
        }
        private void Write_File(string path, string txt, bool re)
        {
            if (txt != "")
            {
                StreamWriter file_wri = new StreamWriter(path, re, Encoding.GetEncoding("UTF-8"));//re:(ture 不覆寫/false 覆寫)
                file_wri.WriteLine(txt);
                file_wri.Flush();
                file_wri.Close();
                Console.WriteLine(DateTime.Now.ToString("HH:mm") + "\r\n" + txt);
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
