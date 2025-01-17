using System;
using System.IO;
using System.Collections.Generic;
using MathNet.Numerics;

using GeoFun.MathUtils;

namespace GeoFun.GNSS
{
    public class SP3File
    {
        public string Path { get; set; }

        ///文件版本
        public string Version
        {
            get
            {
                return Header.Version;
            }
        }

        ///类型 P或V
        public string Type
        {
            get
            {
                return Header.OrbitType;
            }
        }

        ///开始时间
        public GPST StartTime
        {
            get
            {
                return Header.StartGPSTime;
            }
        }

        public string CenterName = "igs";

        private int week;
        /// <summary>
        /// 周数
        /// </summary>
        public int Week
        {
            get
            {
                return week;
            }
            set
            {
                week = value;
            }
        }
        private int dow;
        /// <summary>
        /// 天数
        /// </summary>
        public int DayOfWeek {
            get
            {
                return dow;
            }
            set
            {
                dow = value;
            }
        }
        public int AllDayNum
        {
            get
            {
                return week * 7 + dow;
            }
        }

        ///历元数
        public int EpochNum
        {
            get
            {
                return Header.EpochNum;
            }
        }

        /// <summary>
        /// 历元间隔(s)，默认900
        /// </summary>
        public double Interval { get; set; } = 900d;

        ///文件头
        public SP3Header Header;

        public List<SP3Epoch> AllEpoch { get; set; } = new List<SP3Epoch>(96);

        public SP3File(string path = "")
        {
            Path = path;
            Header = new SP3Header();

            FileName.ParseSP3Name(System.IO.Path.GetFileName(path), out CenterName, out week, out dow);
        }

        /// <summary>
        /// 尝试读取数据
        /// </summary>
        /// <returns></returns>
        public bool TryRead()
        {
            if (!File.Exists(Path)) return false;

            string[] lines;
            try
            {
                lines = File.ReadAllLines(Path);
            }
            catch (Exception ex)
            {
                return false;
            }

            if (Header is null) Header = new SP3Header();

            //// Line1
            Header.Version = lines[0].Substring(0, 2);
            Header.P_V_Flag = lines[0][2].ToString();

            Header.StartTime = GPST.Decode(lines[0].Substring(3, 23));

            Header.EpochNum = Int32.Parse(lines[0].Substring(32, 7).Trim());//读完数据之后会更改此值
            Header.Data_Used = lines[0].Substring(40, 5).Trim();
            Header.Coordinate_Sys = lines[0].Substring(46, 5).Trim();
            Header.OrbitType = lines[0].Substring(52, 3).Trim();
            Header.Agency = lines[0].Substring(56, 4).Trim();

            //// Line2
            int weeks;
            double seconds;
            weeks = Convert.ToInt32(lines[1].Substring(3, 4).Trim());
            seconds = Convert.ToDouble(Math.Floor(Double.Parse(lines[1].Substring(8, 15).Trim())));
            Header.StartGPSTime = new GPST(weeks, seconds);
            Header.Epoch_Interval = Double.Parse(lines[1].Substring(24, 14).Trim());

            //读取卫星PRN号，3-7行
            Header.Num_Sats = Int32.Parse(lines[2].Substring(4, 2).Trim());
            Header.SatPRN = new List<string>();
            for (int ii = 0; ii < 5; ii++)
            {
                int j = 10;
                for (j = 10; j < 59; j = j + 3)
                {
                    if (lines[2 + ii].Substring(j - 1, 3).Trim() != "0")
                    {
                        Header.SatPRN.Add(lines[2 + ii].Substring(j - 1, 3).Trim());
                    }
                }
            }

            //读取卫星精度，8-12行
            Header.SatAccuracy = new List<string>();
            int i = 0;
            for (int ii = 0; ii < 5; ii++)
            {
                int j = 10;
                if (i < Header.Num_Sats)
                {
                    for (j = 10; j < 59; j = j + 3)
                    {
                        Header.SatAccuracy.Add(lines[7 + ii].Substring(j - 1, 3).Trim());
                        i = i + 1;
                        if (i >= Header.Num_Sats)
                            break;
                    }
                }
            }

            for (i = 22; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("EOF")) break;

                if (lines[i].StartsWith("*"))
                {
                    SP3Epoch epoch = DecodeEpoch(lines, ref i);
                    i--;

                    AllEpoch.Add(epoch);
                }
            }

            return true;
        }

        /// <summary>
        /// 读取一个历元的数据
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="lineNum"></param>
        /// <returns></returns>
        public SP3Epoch DecodeEpoch(string[] lines, ref int lineNum)
        {
            SP3Epoch epoch = new SP3Epoch();
            epoch.Epoch = GPST.Decode(lines[lineNum].Substring(1));

            Dictionary<string, SP3Sat> allSat = new Dictionary<string, SP3Sat>();

            int i = lineNum + 1;
            for (; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("*"))
                {
                    break;
                }
                else if (lines[i].StartsWith("EOF"))
                {
                    break;
                }

                SP3Sat sat = DecodeSat(lines[i]);

                epoch.AllSat.Add(sat.Prn, sat);
            }
            lineNum = i;

            return epoch;
        }

        /// <summary>
        /// 读取一颗卫星的数据
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public SP3Sat DecodeSat(string line)
        {
            SP3Sat sat = SP3Sat.New();

            //// 移除多余的空格,并且按空格分割
            string[] segs = StringHelper.SplitFields(line);

            sat.Type = segs[0][0];
            sat.Prn = segs[0].Substring(1);

            sat.X = double.Parse(segs[1]) * 1e3;
            sat.Y = double.Parse(segs[2]) * 1e3;
            sat.Z = double.Parse(segs[3]) * 1e3;
            sat.C = double.Parse(segs[4]);

            if (segs.Length >= 9)
            {
                sat.Std[0] = int.Parse(segs[5]);
                sat.Std[1] = int.Parse(segs[6]);
                sat.Std[2] = int.Parse(segs[7]);
                sat.Std[3] = int.Parse(segs[8]);
            }

            return sat;
        }

        /// <summary>
        /// 获取某颗卫星的位置
        /// </summary>
        /// <param name="t0"></param>
        /// <param name="prn"></param>
        /// <returns></returns>
        public double[] GetSatPos(GPST t0, string prn)
        {
            double[] p = { 0, 0, 0 };

            // 00:00:00之前，无法插值
            if (t0 - StartTime + 1e-13 < 0) return p;
            // 00:00:00之后，无法插值
            if (t0 - StartTime > 24 * 3600) return p;

            // 10阶插值
            double[] t = new double[10];
            double[] x = new double[10];
            double[] y = new double[10];
            double[] z = new double[10];

            GPST ts = StartTime;

            int index = (int)System.Math.Floor((t0 - ts) / Interval);

            // Console.WriteLine("index:{0}", index);

            // 刚好落在采样点上
            if (Math.Abs(ts - t0 + Interval * index) < 1e-13)
            {
                p[0] = AllEpoch[index][prn].X;
                p[1] = AllEpoch[index][prn].Y;
                p[2] = AllEpoch[index][prn].Z;
                return p;
            }
            else if (Math.Abs(ts - t0 + Interval * index + Interval) < 1e-13)
            {
                p[0] = AllEpoch[index + 1][prn].X;
                p[1] = AllEpoch[index + 1][prn].Y;
                p[2] = AllEpoch[index + 1][prn].Z;
                return p;
            }

            // 在开始的几个历元内
            if (index < 4)
            {
                for (int i = 0; i < 10; i++)
                {
                    x[i] = AllEpoch[i][prn].X;
                    y[i] = AllEpoch[i][prn].Y;
                    z[i] = AllEpoch[i][prn].Z;

                    t[i] = AllEpoch[i].Epoch.TotalSeconds;
                }
            }
            // 在结束的几个历元内
            else if (EpochNum - index < 6)
            {
                for (int i = 0; i < 10; i++)
                {
                    x[i] = AllEpoch[EpochNum - 10 + i][prn].X;
                    y[i] = AllEpoch[EpochNum - 10 + i][prn].Y;
                    z[i] = AllEpoch[EpochNum - 10 + i][prn].Z;

                    t[i] = AllEpoch[EpochNum - 10 + i].Epoch.TotalSeconds;
                }
            }
            // 在中间
            else
            {
                for (int i = 0; i < 10; i++)
                {
                    x[i] = AllEpoch[index - 4 + i][prn].X;
                    y[i] = AllEpoch[index - 4 + i][prn].Y;
                    z[i] = AllEpoch[index - 4 + i][prn].Z;

                    t[i] = AllEpoch[index - 4 + i].Epoch.TotalSeconds;

                }
            }

            p[0] = Interp.GetValueLagrange(10, t, x, t0.TotalSeconds);
            p[1] = Interp.GetValueLagrange(10, t, y, t0.TotalSeconds);
            p[2] = Interp.GetValueLagrange(10, t, z, t0.TotalSeconds);
            return p;
        }
    }
}