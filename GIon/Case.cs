﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GeoFun.Sys;
using GeoFun.GNSS;
using GeoFun.GNSS.Net;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Data;
using DevComponents.DotNetBar.MicroCharts;
using GeoFun;
using GeoFun.MathUtils;

namespace GIon
{
    public class Case
    {
        public Action<int> SetProgressMax { get; set; }
        public Action<int> SetProgressValue { get; set; }

        /// <summary>
        /// 根路径(文件夹)
        /// </summary>
        public string RootFolder { get; set; } = "";

        /// <summary>
        /// 观测值文件夹
        /// </summary>
        private string obsFolder { get; set; } = "";
        /// <summary>
        /// 结果输出文件夹
        /// </summary>
        private string resFolder { get; set; } = "";
        /// <summary>
        /// 临时文件夹
        /// </summary>
        private string tmpFolder { get; set; } = "";
        /// <summary>
        /// 星历/轨道/钟差文件夹
        /// </summary>
        private string orbFolder { get; set; } = "";
        /// <summary>
        /// 各种表文件
        /// </summary>
        private string tabFolder { get; set; } = "";
        /// <summary>
        /// 日志文件夹
        /// </summary>
        private string logFolder { get; set; } = "";

        public enumFitType FitType = enumFitType.Polynomial;
        public double CutAngle { get; set; } = 15d;
        public int MinArcLen { get; set; } = 120;

        public Observation Obs;
        public Orbit Orb;
        public List<string> StationNames { get; set; } = new List<string>();

        /// <summary>
        /// 本次解算设置
        /// </summary>
        public GeoFun.GNSS.Options Option { get; set; } = new GeoFun.GNSS.Options();

        /// <summary>
        /// 所有的观测文件路径
        /// </summary>
        public List<string> obsFiles { get; set; } = new List<string>();

        /// <summary>
        /// 所有测站
        /// </summary>
        public List<OStation> Stations { get; set; } = new List<OStation>();

        public List<DCBFile> dCBFiles { get; set; } = new List<DCBFile>();

        public Case(string path = "")
        {
            RootFolder = path;
            Init();
        }

        /// <summary>
        /// 读取所有数据，统一进行解算，很耗内存和时间
        /// </summary>
        public void MultiStationMultiDay()
        {
            if (!Directory.Exists(obsFolder))
            {
                PrintWithTime("文件夹不存在，结束解算!");
                return;
            }
            DirectoryInfo obsDir = new DirectoryInfo(obsFolder);

            PrintWithTime("正在搜索o文件...");
            FileInfo[] files = obsDir.GetFiles("*.??o");
            PrintWithTime(string.Format("共找到{0}个o文件", files.Length));

            Dictionary<int, List<string>> stationsPerDOY = new Dictionary<int, List<string>>();
            Dictionary<string, List<int>> doysPerStation = new Dictionary<string, List<int>>();
            string stationName;
            int year, day, doy;
            foreach (var file in files)
            {
                FileName.ParseRinex2(file.Name, out stationName, out day, out year);
                doy = year * 1000 + day;
                if (!stationsPerDOY.ContainsKey(doy))
                {
                    stationsPerDOY[doy] = new List<string>();
                }
                if (!doysPerStation.ContainsKey(stationName))
                {
                    doysPerStation[stationName] = new List<int>();
                }

                stationsPerDOY[doy].Add(stationName);
                doysPerStation[stationName].Add(doy);
            }

            // 观测值起止时间
            int minObsDOY = (from d in stationsPerDOY
                             select d.Key).Min();
            int maxObsDOY = (from d in stationsPerDOY
                             select d.Key).Max();

            // 轨道起止时间
            int minOrbDOY = DOY.AddDays(minObsDOY, -1);
            int maxOrbDOY = DOY.AddDays(maxObsDOY, 1);

            // 下载星历,钟差
            Download(DOY.AddDays(minObsDOY, -1), DOY.AddDays(maxObsDOY, 1));

            // 估计DCB
            List<OFile> ofiles = new List<OFile>();
            foreach (var station in stationsPerDOY[minObsDOY])
            {
                year = minObsDOY / 1000;
                day = minObsDOY % 1000;

                string strStart = string.Format("{0}{1:00#}", station, day);
                string strEnd = string.Format(".{0:0#}o", Time.GetYear2(year));

                FileInfo file = files.First(f => f.Name.StartsWith(strStart) && f.Name.EndsWith(strEnd));
                if (file is null)
                { }
                else
                {
                    OFile of = new OFile(file.FullName);
                    ofiles.Add(of);
                }
            }

            for (int i = 0; i < ofiles.Count(); i++)
            {
                if (!ofiles[i].TryRead())
                {
                    continue;
                }
            }

            PrintWithTime("读取星历文件...");
            Orbit Orb = new Orbit(orbFolder);
            Orb.GetAllSp3Files(orbFolder, minOrbDOY, maxOrbDOY);
            Orb.Read(orbFolder);

            OEpoch epo;
            foreach (var of in ofiles)
            {
                for (int i = 0; i < of.Epoches.Count; i++)
                {
                    epo = of.Epoches[i];
                    Orb.GetSatPos(ref epo);
                }
            }

            foreach (var of in ofiles)
            {
                of.CalAzElIPP();
                of.DetectOutlier();
                of.DetectAllArcs();
                of.DetectCycleSlip();

                of.CalSP4();
            }

            List<string> stationNames = new List<string>(ofiles.Count * 2);
            List<LinkedList<int>> prn = new List<LinkedList<int>>();
            List<LinkedList<double>> lat = new List<LinkedList<double>>();
            List<LinkedList<double>> lon = new List<LinkedList<double>>();
            List<LinkedList<double>> sp4 = new List<LinkedList<double>>();
            List<LinkedList<double>> ele = new List<LinkedList<double>>();

            double b, l;
            int prnNum = 0;
            foreach (var of in ofiles)
            {
                stationNames.Add(of.StationName);
                LinkedList<int> prns = new LinkedList<int>();
                LinkedList<double> lats = new LinkedList<double>();
                LinkedList<double> lons = new LinkedList<double>();
                LinkedList<double> tecs = new LinkedList<double>();
                LinkedList<double> eles = new LinkedList<double>();
                foreach (var p in of.Arcs.Keys)
                {
                    prnNum = int.Parse(p.Substring(1));
                    var arcs = of.Arcs[p];
                    foreach (var arc in arcs)
                    {
                        for (int i = 0; i < arc.Length; i++)
                        {
                            if (arc[i]["SP4"] > 0)
                            {
                                Coordinate.SunGeomagnetic(arc[i].IPP[0], arc[i].IPP[1],
                                    arc[i].Epoch.Hour, arc[i].Epoch.Minute, arc[i].Epoch.Second,
                                    GeoFun.GNSS.Common.GEOMAGNETIC_POLE_LAT, GeoFun.GNSS.Common.GEOMAGENTIC_POLE_LON,
                                    out b, out l);
                                prns.AddLast(prnNum);
                                tecs.AddLast(arc[i]["SP4"]);
                                lats.AddLast(arc[i].IPP[0]);
                                lons.AddLast(arc[i].IPP[1]);
                                eles.AddLast(arc[i].Elevation);
                            }
                        }
                    }
                }

                prn.Add(prns);
                lat.Add(lats);
                lon.Add(lons);
                sp4.Add(tecs);
                ele.Add(eles);
            }

            SphericalHarmonicIonoModel spm;
            Dictionary<string, double> recDCB, satDCB;
            IonoModel.CalSphericalHarmonicModel(9, 9, stationNames, prn, lat, lon, sp4, ele,
                out spm, out recDCB, out satDCB);

            string recDCBName = "0station_dcb.txt";
            string satDCBName = "0satellite_dcb.txt";
            string recDCBPath = Path.Combine(resFolder, recDCBName);
            string satDCBPath = Path.Combine(resFolder, satDCBName);

            string txt = "";
            double dcb = 0d;
            foreach (var rec in recDCB.Keys)
            {
                dcb=recDCB[rec] / GeoFun.GNSS.Common.C0*1e9;
                txt += rec + "," + dcb.ToString("#.0000000000") + "\n";
            }
            using (FileStream fs = new FileStream(recDCBPath, FileMode.Create, FileAccess.Write))
            {
                using (StreamWriter writer = new StreamWriter(fs))
                {
                    writer.Write(txt);
                    writer.Close();
                    fs.Close();
                }
            }

            txt = "";
            foreach (var sat in satDCB.Keys)
            {
                dcb=satDCB[sat] / GeoFun.GNSS.Common.C0*1e9;
                txt += sat + "," + dcb.ToString("#.0000000000") + "\n";
            }
            using (FileStream fs = new FileStream(satDCBPath, FileMode.Create, FileAccess.Write))
            {
                using (StreamWriter writer = new StreamWriter(fs))
                {
                    writer.Write(txt);
                    writer.Close();
                    fs.Close();
                }
            }

            double a = 0;


            //SetProgressMax(files.Count());
            //Orbit orb;
            //List<DOY> allDOY = new List<DOY>();
            //string stationName;
            //DCBHelper dcb = new DCBHelper();
            //for (int i = 0; i < files.Length; i++)
            //{
            //    SetProgressValue(i);
            //    orb = new Orbit(orbFolder);

            //    OFile file = new OFile(files[i].FullName);
            //    file.TryRead();
            //    // 检查观测数据采样率
            //    if (file.Interval != 30d) continue;

            //    // 下载星历、DCB
            //    FileName.ParseRinex2(files[i].Name, out stationName, out doy, out year);
            //    var doys = new List<DOY> { new DOY(year, doy) };
            //    Download(doys);
            //    if (!allDOY.Any(d => d.Year == year && d.Day == doy))
            //    {
            //        allDOY.Add(doys[0]);
            //    }

            //    // 读取DCB
            //    PrintWithTime("读取卫星DCB...");
            //    int month, dom;
            //    Time.DOY2MonthDay(year, doy, out month, out dom);
            //    DCBFile dcbf = dcb.GetDCB(year, month);
            //    if (dcbf is null)
            //    {
            //        string dcbFileName = string.Format("P1P2{0:D2}{1:D2}.DCB", year - 2000, month);
            //        string dcbFilePath = Path.Combine(orbFolder, dcbFileName);
            //        dcbf = new DCBFile(dcbFilePath);

            //        if (!File.Exists(dcbFilePath) || !dcbf.TryRead())
            //        {
            //            PrintWithTime("DCB文件读取失败:" + dcbFileName);
            //            continue;
            //        }
            //    }

            //    // 读取星历
            //    if (orb != null
            //        && orb.StartTime != null
            //        && orb.EndTime != null
            //        && orb.StartTime < file.StartTime
            //        && orb.EndTime > file.EndTime)
            //    {
            //    }
            //    else
            //    {
            //        DOY curDOY = new DOY(year, doy);
            //        orb.GetAllSp3Files(orbFolder, curDOY.AddDays(-1), curDOY.AddDays(1));
            //        orb.Read(orbFolder);
            //    }

            //    PrintWithTime("正在计算轨道...");
            //    for (int j = 0; j < file.Epoches.Count; j++)
            //    {
            //        var epo = file.Epoches[j];
            //        orb.GetSatPos(ref epo);
            //    }

            //    file.CalAzElIPP();
            //    file.DCBCorrect(dcbf);
            //    file.DetectOutlier();
            //    file.DetectAllArcs();
            //    file.DetectCycleSlip();
            //    file.CalSP4();
            //    file.CalVTEC();

            //    file.WriteMeas("vtec", tmpFolder);

            //    SetProgressValue(i + 1);
            //}

            //int a = 0;
        }

        /// <summary>
        /// 逐个测站解算,耗内存小
        /// </summary>
        public void ResolveSingleStationMultiDay()
        {
            var staNames = GetStationNames(obsFolder);
            if (staNames.Count <= 0) return;
            if (SetProgressMax != null) SetProgressMax(staNames.Count);


            for (int i = 0; i < staNames.Count; i++)
            {
                try
                {
                    if (SetProgressValue != null) SetProgressValue(i + 1);

                    var staName = staNames[i];
                    Print(string.Format("\r\n\r\n开始解算第{0}个测站,共{1}个:{2}", i + 1, staNames.Count, staName));
                    PrintWithTime("正在搜索观测文件...");

                    OStation sta = new OStation(staName);
                    sta.SearchAllOFiles(obsFolder);
                    PrintWithTime(string.Format("共找到{0}个文件:", sta.FileNum));
                    for (int j = 0; j < sta.OFiles.Count; j++)
                    {
                        Print(string.Format("\r\n    {0} {1}", j + 1, sta.OFiles[j].Path));
                    }
                    if (sta.FileNum <= 0) return;

                    if (!sta.CheckDataConsist())
                    {
                        PrintWithTime("错误！本站数据不连续");
                        continue;
                    }

                    PrintWithTime("正在下载辅助文件...");
                    if (!Download(sta.DOYs))
                    {
                        PrintWithTime("错误！下载辅助文件失败");
                    }

                    PrintWithTime("读取星历文件...");
                    Orb = new Orbit(orbFolder);
                    Orb.GetAllSp3Files(orbFolder, sta.StartDOY, sta.EndDOY);
                    Orb.Read(orbFolder);

                    PrintWithTime("读取观测文件...");
                    sta.ReadAllObs();
                    sta.SortObs();

                    PrintWithTime(string.Format("共读取到{0}个历元", sta.EpochNum));
                    if (sta.EpochNum > 0)
                    {
                        sta.StartTime = sta.Epoches[0].Epoch;
                    }
                    else
                    {
                        return;
                    }

                    PrintWithTime("计算卫星轨道...");
                    for (int j = 0; j < sta.EpochNum; j++)
                    {
                        OEpoch oepo = sta.Epoches[j];
                        Orb.GetSatPos(ref oepo);
                    }
                    PrintWithTime("\r\n穿刺点计算...");
                    sta.CalAzElIPP();

                    PrintWithTime("粗差探测...");
                    sta.DetectOutlier();
                    PrintWithTime("弧段探测...");
                    sta.DetectArcs();
                    PrintWithTime("周跳探测...");
                    sta.DetectCycleSlip();
                    PrintWithTime("相位平滑伪距计算...");
                    sta.SmoothP4();
                    if (FitType == enumFitType.None)
                    {
                        sta.CalVTEC();
                        PrintWithTime(string.Format("{0}观测值写入文件", "sp4"));
                        sta.WriteMeas(resFolder, "SP4");
                    }
                    else if (FitType == enumFitType.Polynomial)
                    {
                        PrintWithTime("VTEC计算...");
                        sta.CalVTEC();
                        PrintWithTime("多项式拟合计算...");
                        sta.Fit();
                        PrintWithTime(string.Format("{0}观测值写入文件", "vtec"));
                        sta.WriteMeas(resFolder, "vtec");
                        PrintWithTime(string.Format("{0}观测值写入文件", "dtec"));
                        sta.WriteMeas(resFolder, "dtec");
                    }
                    else if (FitType == enumFitType.Smooth)
                    {
                        PrintWithTime("VTEC计算...");
                        sta.CalVTEC();
                        PrintWithTime("滑动平均计算...");
                        sta.Smooth();
                        PrintWithTime(string.Format("{0}观测值写入文件", "vtec"));
                        sta.WriteMeas(resFolder, "vtec");
                        PrintWithTime(string.Format("{0}观测值写入文件", "dtec"));
                        sta.WriteMeas(resFolder, "dtec");
                    }
                    else if (FitType == enumFitType.DoubleDifference)
                    {
                        PrintWithTime("二阶差分计算...");
                        sta.DoubleDiff();
                        PrintWithTime(string.Format("{0}观测值写入文件", "l6"));
                        sta.WriteMeas1(resFolder, "L6");
                    }
                    else if(FitType == enumFitType.ROTI)
                    {
                        PrintWithTime("计算ROTI...");
                        sta.ROTI();
                        PrintWithTime(string.Format("{0}观测值写入文件", "l6"));
                        sta.WriteMeas(resFolder, "roti");
                    }
                }
                catch (Exception e)
                {
                    PrintWithTime("未知异常(" + e.ToString() + ")");
                    continue;
                }
            }
        }

        /// <summary>
        /// 单站单天解
        /// </summary>
        public void ResolveSingleStationSingleDay()
        {
            if (string.IsNullOrWhiteSpace(obsFolder))
            {
                FrmIono.MessHelper.Print("\r\n文件夹不存在:" + obsFolder);
                return;
            }

            DirectoryInfo dir = new DirectoryInfo(obsFolder);
            var files = dir.GetFiles("*.??o");
            for (int i = 0; i < files.Length; i++)
            {
                FrmIono.MessHelper.Print(string.Format("\r\n{0}:{1}", DateTime.Now, "开始读取文件:" + files[i].Name));
                try
                {
                    OFile of = new OFile(files[i].FullName);
                    if (of.TryRead())
                    {
                        int year, doy;
                        string stationName;
                        FileName.ParseRinex2(files[i].Name, out stationName, out doy, out year);

                        // 下载星历
                        Dictionary<string, List<DOY>> doys = new Dictionary<string, List<DOY>>();
                        doys.Add(stationName, new List<DOY> { new DOY(year, doy) });
                        if (!Download(doys))
                        {
                            PrintWithTime("下载星历失败！");
                            return;
                        }

                        DOY fileDOY = new DOY(year, doy);
                        DOY start = fileDOY.AddDays(-1);
                        DOY end = fileDOY.AddDays(1);

                        // 读取星历
                        PrintWithTime("正在读取星历...");
                        Orbit orb = new Orbit(orbFolder);
                        orb.GetAllSp3Files(orbFolder, start, end);
                        orb.Read(orbFolder);

                        // 计算轨道
                        PrintWithTime("正在计算轨道...");
                        for (int j = 0; j < of.Epoches.Count; j++)
                        {
                            var epo = of.Epoches[j];
                            orb.GetSatPos(ref epo);
                        }

                        //探测粗差
                        PrintWithTime("正在探测粗差...");
                        of.DetectOutlier();

                        // 探测弧段
                        of.DetectAllArcs();

                        // 探测周跳
                        PrintWithTime("正在探测周跳...");
                        of.DetectCycleSlip();

                        // 计算穿刺点
                        of.CalIPP();

                        // 相位平滑伪距
                        PrintWithTime("正在计算VTEC...");
                        of.CalSP4();

                        // 计算VTEC
                        of.CalVTEC();

                        // 多项式拟合
                        Print("正在拟合多项式...");
                        if (FitType == enumFitType.Polynomial)
                        {
                            of.Fit();
                        }
                        else if (FitType == enumFitType.DoubleDifference)
                        {
                            of.DoubleDifference();
                        }
                        else if (FitType == enumFitType.Smooth)
                        {
                            of.Smooth();
                        }

                        // 输出结果
                        PrintWithTime("正在写入文件...");
                        of.WriteTEC(resFolder);
                    }
                    else
                    {
                        FrmIono.MessHelper.Print(string.Format("\r\n{0}:{1}", DateTime.Now, "读取文件失败!"));
                    }
                }
                catch (Exception e)
                {
                    FrmIono.MessHelper.Print(string.Format("\r\n{0}:{1}:{2}", DateTime.Now, "解算失败，原因是", e.ToString()));
                }
            }
        }

        public void Init()
        {
            obsFolder = System.IO.Path.Combine(RootFolder, "obs");
            resFolder = System.IO.Path.Combine(RootFolder, "out");
            tmpFolder = System.IO.Path.Combine(RootFolder, "tmp");
            orbFolder = System.IO.Path.Combine(RootFolder, "orb");
            tabFolder = System.IO.Path.Combine(RootFolder, "tab");
            logFolder = System.IO.Path.Combine(RootFolder, "log");

            CreateFolders();
            MoveFiles();
        }

        /// <summary>
        /// 创建文件夹
        /// </summary>
        /// <returns></returns>
        public bool CreateFolders()
        {
            if (!CreateFolder(obsFolder))
            {
                Message.Error("创建文件夹失败:" + obsFolder);
                return false;
            }

            if (!CreateFolder(resFolder))
            {
                Message.Error("创建文件夹失败:" + resFolder);
                return false;
            }

            if (!CreateFolder(orbFolder))
            {
                Message.Error("创建文件夹失败:" + orbFolder);
                return false;
            }

            if (!CreateFolder(tmpFolder))
            {
                Message.Error("创建文件夹失败:" + tmpFolder);
                return false;
            }

            if (!CreateFolder(tabFolder))
            {
                Message.Error("创建文件夹失败:" + tabFolder);
                return false;
            }

            if (!CreateFolder(logFolder))
            {
                Message.Error("创建文件夹失败:" + logFolder);
                return false;
            }

            return true;
        }
        public bool CreateFolder(string folder)
        {
            if (!Directory.Exists(folder))
            {
                try
                {
                    Directory.CreateDirectory(folder);
                }
                catch { }
            }
            return Directory.Exists(folder);
        }

        public void MoveFiles()
        {
            if (!Directory.Exists(RootFolder)) return;
            DirectoryInfo dir = new DirectoryInfo(RootFolder);

            foreach (var file in dir.GetFiles("*.??o*"))
            {
                string newFile = Path.Combine(obsFolder, file.Name);
                try
                {
                    file.CopyTo(newFile, false);
                    file.Delete();
                }
                catch
                { }
            }
            foreach (var file in dir.GetFiles("*.??d*"))
            {
                string newFile = Path.Combine(obsFolder, file.Name);
                try
                {
                    file.CopyTo(newFile, false);
                    file.Delete();
                }
                catch
                { }
            }
            foreach (var file in dir.GetFiles("*.sp3*"))
            {
                string newFile = Path.Combine(orbFolder, file.Name);
                try
                {
                    file.CopyTo(newFile, false);
                    file.Delete();
                }
                catch
                { }
            }
            foreach (var file in dir.GetFiles("*.clk*"))
            {
                string newFile = Path.Combine(orbFolder, file.Name);
                try
                {
                    file.CopyTo(newFile, false);
                    file.Delete();
                }
                catch
                { }
            }
            foreach (var file in dir.GetFiles("*.DCB*"))
            {
                string newFile = Path.Combine(orbFolder, file.Name);
                try
                {
                    file.CopyTo(newFile, false);
                    file.Delete();
                }
                catch
                { }
            }

            string cmd;

            // 解压o文件
            cmd = string.Format("7z.exe x \"{0}\\*.Z\" &exit", obsFolder);
            CMDHelper.ExecuteThenWait(cmd);

            // 解压星历钟差等文件
            cmd = string.Format("7z.exe x \"{0}\\*.Z\" &exit", orbFolder);
            CMDHelper.ExecuteThenWait(cmd);

            // 解压d文件
            DirectoryInfo obsDir = new DirectoryInfo(obsFolder);
            foreach (var file in obsDir.GetFiles("*.??d"))
            {
                string filePathO = file.FullName.Substring(0, file.FullName.Length - 1) + "o";
                if (File.Exists(filePathO)) continue;

                cmd = string.Format("crx2rnx.exe -s -f \"{0}\" &exit", file.FullName);
                CMDHelper.ExecuteThenWait(cmd);
            }
        }

        public List<string> GetStationNames(string folder)
        {
            List<string> names = new List<string>();
            if (!Directory.Exists(folder)) return names;

            DirectoryInfo dir = new DirectoryInfo(folder);
            foreach (var file in dir.GetFiles("*.??o"))
            {
                if (file.Name.Length < 4) continue;

                var staName = file.Name.Substring(0, 4);
                if (!names.Contains(staName))
                {
                    names.Add(staName);
                }
            }

            return names;
        }

        /// <summary>
        /// 从网上下载必须的数据
        /// </summary>
        /// <returns></returns>
        public bool Download(Dictionary<string, List<DOY>> DOYs)
        {
            // 获取数据的起始和结束时间
            DOY start = null;
            DOY end = null;
            foreach (var station in DOYs.Keys)
            {
                DOY min = DOYs[station].Min();
                DOY max = DOYs[station].Max();
                if (start is null)
                {
                    start = min;
                    end = max;
                }
                else
                {
                    if (min < start) start = min;
                    if (max > end) end = max;
                }
            }

            // 前一天的星历
            start = start.AddDays(-1);
            // 后一天的星历
            end = end.AddDays(1);

            while (start <= end)
            {
                DownloadDay(start.Year, start.Day);

                start = start.AddDays(1);
            }

            return true;
        }
        public bool Download(List<DOY> DOYs)
        {
            // 获取数据的起始和结束时间
            DOY start = DOYs.Min();
            DOY end = DOYs.Max();

            // 前一天的星历
            start = start.AddDays(-1);
            // 后一天的星历
            end = end.AddDays(1);

            while (start <= end)
            {
                Print("\r\n    正在下载第" + start.Day.ToString() + "天...");
                DownloadDay(start.Year, start.Day);
                start = start.AddDays(1);
            }

            return true;
        }
        public bool Download(int start, int end)
        {
            if (end < start) return true;

            int year, day;
            while (start <= end)
            {
                year = start / 1000;
                day = start % 1000;

                Print(string.Format("\r\n    DOY:{0}", start));

                DownloadDay(year, day);

                start = DOY.AddDays(start, 1);
            }
            return true;
        }
        public bool DownloadDay(int year, int doy)
        {
            int month, dom;
            Time.DOY2MonthDay(year, doy, out month, out dom);

            Print("\r\n        下载SP3...");
            if (!Downloader.DownloadSp3DOY(year, doy, orbFolder))
            {
                Print(string.Format("\r\n        sp3下载失败..."));
            }
            Print("\r\n        下载ionex...");
            if (!Downloader.DownloadI(year, doy, orbFolder))
            {
                Print(string.Format("\r\n        ionex下载失败..."));
            }
            Print("\r\n        下载p1c1...");
            if (!Downloader.DownloadDCB(year, month, "P1C1", orbFolder))
            {

                Print(string.Format("\r\n        p1c1下载失败..."));
            }
            Print("\r\n        下载p1p2...");
            if (!Downloader.DownloadDCB(year, month, "P1P2", orbFolder))
            {

                Print(string.Format("\r\n        p1p2下载失败..."));
            }
            return true;
        }

        public void ReadFiles()
        {
            Obs.Read(obsFolder);
            Orb.Read(orbFolder);
        }

        /// <summary>
        /// 读取dcb文件
        /// </summary>
        public void ReadDCBFiles() { }
        /// <summary>
        /// 读取星历
        /// </summary>
        public void ReadOrbFiles() { }

        public void PrintWithTime(string msg)
        {
            FrmIono.MessHelper.Print(string.Format("\r\n{0}:{1}", DateTime.Now, msg));
        }

        public void Print(string msg)
        {
            FrmIono.MessHelper.Print(msg);
        }
    }
}
