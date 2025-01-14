﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeoFun.GNSS
{
    public class Smoother
    {
        /// <summary>
        /// 权重的最小值
        /// </summary>
        public static readonly double MIN_POWER = 0.01;

        /// <summary>
        /// 权重的减小的速率
        /// </summary>
        public static readonly double DEC_POWER = 0.01;

        /// <summary>
        /// 用另一个观测序列来平滑本观测序列
        /// </summary>
        public static void SmoothByAnother()
        {

        }

        /// <summary>
        /// 用L4观测值平滑P4观测值
        /// </summary>
        /// <param name="epoches"></param>
        /// <param name="prn"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        public static void SmoothP4ByL4(ref List<OEpoch> epoches, string prn, int start = 0, int end = -1)
        {
            if (epoches is null) return;
            if (prn is null) return;
            if (start >= epoches.Count) return;
            if (start < 0) return;
            if (end < start) return;
            if (end >= epoches.Count) end = epoches.Count;

            double power = 1;
            // P4预报值
            double p4_est = 0d;
            for (int i = start + 1; i < end; i++)
            {
                power = power - DEC_POWER;
                p4_est = epoches[i - 1][prn]["P4"] + epoches[i][prn]["L4"] - epoches[i - 1][prn]["L4"];

                epoches[i][prn]["P4"] = epoches[i][prn]["P4"] * power + (1 - power) * p4_est;
            }

        }

        /// <summary>
        /// 平滑一个弧段
        /// </summary>
        /// <param name="arc"></param>
        public static void SmoothP4ByL4_1(ref OArc arc)
        {
            double power = 1;

            // P4预报值
            double p4_est = 0d;
            for (int i = 1; i < arc.Length; i++)
            {
                power = power - DEC_POWER;
                if (power < MIN_POWER)
                    power = MIN_POWER;
                p4_est = arc[i - 1]["P4"] + arc[i]["L4"] - arc[i - 1]["L4"];

                arc[i]["P4"] = arc[i]["P4"] * power + (1 - power) * p4_est;
            }
        }

        /// <summary>
        /// 平滑一个弧段
        /// </summary>
        /// <param name="arc"></param>
        public static void SmoothP4ByL4(ref OArc arc, string type = "hatch")
        {
            if (type == "hatch")
            {
                // 整个弧段P4+L4的均值<P4+L4>
                int n = 0;
                double p4, l4;
                double p4l4 = 0d;
                double[] p4l4All = new double[arc.Length];
                for (int i = 0; i < arc.Length; i++)
                {
                    p4 = arc[i]["P4"];
                    l4 = arc[i]["L4"];
                    if (p4 != 0 && l4 != 0)
                    {
                        p4l4 += p4 + l4;
                        n++;
                    }
                    p4l4All[i] = p4 + l4;
                }
                if (n > 0) p4l4 /= n;

                // 平滑P4
                for (int j = 0; j < arc.Length; j++)
                {
                    p4 = arc[j]["P4"];
                    l4 = arc[j]["L4"];

                    arc[j].SatData["SP4"] = 0;
                    if (p4 != 0 && l4 != 0)
                    {
                        arc[j].SatData["SP4"] = p4l4 - l4;
                    }
                }
            }
            else
            {//双向hatch滤波
                double[] sp4Forward = new double[arc.Length];
                double[] sp4Backward = new double[arc.Length];

                // 前向
                double power = 1;
                double MIN_POWER = 0.01;
                double POWER_DEC = 0.02;
                sp4Forward[0] = arc[0]["P4"];
                for (int i = 1; i < arc.Length; i++)
                {
                    power = power - POWER_DEC >= MIN_POWER ? power - POWER_DEC : MIN_POWER;
                    sp4Forward[i] = (sp4Forward[i - 1] + arc[i]["L4"] - arc[i - 1]["L4"]) * (1 - power) +
                        arc[i]["P4"] * power;
                }

                // 后向
                power = 1d;
                sp4Backward[arc.Length - 1] = arc[arc.Length - 1]["P4"];
                for (int i = arc.Length - 2; i >= 0; i--)
                {
                    power = power - POWER_DEC >= MIN_POWER ? power - POWER_DEC : MIN_POWER;
                    sp4Backward[i] = (sp4Forward[i + 1] + arc[i]["L4"] - arc[i + 1]["L4"]) * (1 - power) +
                        arc[i]["P4"] * power;
                }

                if (arc.Length > 60)
                {
                    for (int i = 0; i < 30; i++)
                    {
                        arc[i]["SP4"] = sp4Backward[i];
                    }
                    for (int i = arc.Length - 1; i >= arc.Length - 30; i--)
                    {
                        arc[i]["SP4"] = sp4Forward[i];
                    }
                    for (int i = 30; i < arc.Length - 30; i++)
                    {
                        arc[i]["SP4"] = sp4Forward[i] / 2d + sp4Backward[i] / 2d;
                    }
                }
                else
                {
                    int midIndex = arc.Length / 2;
                    for (int i = 0; i < midIndex; i++)
                    {
                        arc[i]["SP4"] = sp4Backward[i];
                    }
                    for (int i = midIndex; i < arc.Length; i++)
                    {
                        arc[i]["SP4"] = sp4Forward[i];
                    }
                }
            }
        }

        /// <summary>
        /// 滑动平均
        /// </summary>
        /// <param name="arc">弧段</param>
        /// <param name="meas">要平滑的观测值名称</param>
        /// <param name="order">阶数</param>
        public static void Smooth(ref OArc arc, string meas, int order)
        {
            if (arc is null) return;

            int left = 0, right = 0;
            if (order % 2 == 0)
            {
                left = order / 2;
                right = left - 1;
            }
            else
            {
                left = right = (order - 1) / 2;
            }

            double[] values = new double[arc.Length];
            double[] values1 = new double[arc.Length];
            for (int i = left; i < arc.Length - right; i++)
            {
                values[i] = arc[i][meas];

                int k = 0;
                double mean = 0d;
                for (int j = i - left; j < i + right + 1; j++)
                {
                    if (Math.Abs(arc[j][meas]) < 1e-10) continue;

                    mean = mean * k / (k + 1) + arc[j][meas] / (k + 1);
                    k++;
                }

                values[i] = mean;
            }
            for (int i = 0; i < arc.Length; i++)
            {
                arc[i]["dtec"] = arc[i][meas] - values[i];
                arc[i][meas] = values[i];
            }

            arc.StartIndex += left;
            arc.EndIndex -= right;
        }
    }
}
