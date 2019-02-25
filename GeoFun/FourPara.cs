﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace GeoFun
{
    public class FourPara
    {
        /// <summary>
        /// 四参数模型，默认与PowerCoor一致
        /// </summary>
        public enumFourMode Mode { get; set; } = enumFourMode.ORS;

        /// <summary>
        /// X平移量(米),默认0
        /// </summary>
        public double DX { get; set; } = 0d;
        /// <summary>
        /// Y平移量(米),默认0
        /// </summary>
        public double DY { get; set; } = 0d;
        /// <summary>
        /// 旋转量(秒),默认0
        /// </summary>
        public double R { get; set; } = 0d;
        /// <summary>
        /// 尺度(ppm),默认0
        /// </summary>
        public double S { get; set; } = 0d;

        /// <summary>
        /// 四参数转仿射参数
        /// </summary>
        /// <param name="four"></param>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <param name="C"></param>
        /// <param name="D"></param>
        /// <param name="E"></param>
        /// <param name="F"></param>
        public static void Four2Affine(FourPara four, out double A, out double B, out double C, out double D, out double E, out double F)
        {
            Four2Affine(four.DX, four.DY, four.R, four.S, out A, out B, out C, out D, out E, out F, Mode2Str(four.Mode));
        }

        /// <summary>
        /// 将四参数变为仿射参数  
        ///     仿射参数公式为 
        ///     X2 = A*X1 + B*Y1 + C
        ///     Y2 = D*X1 + E*Y1 + F
        /// </summary>
        /// <param name="dx">平移量(米)</param>
        /// <param name="dy">平移量(米)</param>
        /// <param name="r">旋转量(弧度)</param>
        /// <param name="s">尺度(ppm)</param>
        /// <param name="mode">模型 三个字母，每个字母表示一个顺序 o(ffset) R(otate) S(cale) 例如powercoor模型为ors</param>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <param name="C"></param>
        /// <param name="D"></param>
        /// <param name="E"></param>
        /// <param name="F"></param>
        public static void Four2Affine(double dx, double dy, double r, double s, out double A, out double B, out double C,
            out double D, out double E, out double F, string mode = "ors")
        {
            A = 1d; B = 0d; C = 0d;
            D = 0d; E = 1d; F = 0d;

            A = (1 + s * 1e-6) * Math.Cos(r);
            B = (1 + s * 1e-6) * Math.Sin(r);
            D = -(1 + s * 1e-6) * Math.Sin(r);
            E = (1 + s * 1e-6) * Math.Cos(r);

            //// 默认的模型为O(ffset)-R(otate)-S(cale)
            if (string.IsNullOrWhiteSpace(mode))
            {
                mode = "ors";
            }

            if (mode.StartsWith("o"))
            {
                C = (1 + s * 1e-6) * (Math.Cos(r) * dx + Math.Sin(r) * dy);
                F = (1 + s * 1e-6) * (-Math.Sin(r) * dx + Math.Cos(r) * dy);
            }
            else if (mode.EndsWith("o"))
            {
                C = dx;
                F = dy;
            }
        }

        /// <summary>
        /// 将四参数变为仿射参数  
        ///     仿射参数公式为 
        ///     X2 = A*X1 + B*Y1 + C
        ///     Y2 = D*X1 + E*Y1 + F
        /// </summary>
        /// <param name="dx">平移量(米)</param>
        /// <param name="dy">平移量(米)</param>
        /// <param name="r">旋转量(弧度)</param>
        /// <param name="s">尺度(ppm)</param>
        /// <param name="mode">模型 三个字母，每个字母表示一个顺序 o(ffset) R(otate) S(cale) 例如powercoor模型为ors</param>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <param name="C"></param>
        /// <param name="D"></param>
        /// <param name="E"></param>
        /// <param name="F"></param>
        public static void Four2Affine(double dx, double dy, double r, double s, enumFourMode mode, out double A, out double B, out double C,
            out double D, out double E, out double F)
        {
            Four2Affine(dx, dy, r, s, out A, out B, out C, out D, out E, out F, Mode2Str(mode));
        }

        /// <summary>
        /// 根据一种四参数模型的参数推导另一种四参数模型的参数
        /// </summary>
        /// <param name="dx1">平移量(米)</param>
        /// <param name="dy1">平移量(米)</param>
        /// <param name="r1">旋转量(弧度)</param>
        /// <param name="s1">尺度(ppm)</param>
        /// <param name="mode1">输入模型</param>
        /// <param name="dx2">平移量(米)</param>
        /// <param name="dy2">平移量(米)</param>
        /// <param name="r2">旋转量(弧度)</param>
        /// <param name="s2">尺度(ppm)</param>
        /// <param name="mode2">输出模型</param>
        public static void ChangeMode(double dx1, double dy1, double r1, double s1, enumFourMode mode1,
            out double dx2, out double dy2, out double r2, out double s2, enumFourMode mode2)
        {
            string modeStr1 = Mode2Str(mode1);
            string modeStr2 = Mode2Str(mode2);

            ChangeMode(dx1, dy1, r1, s1, modeStr1, out dx2, out dy2, out r2, out s2, modeStr2);
        }

        /// <summary>
        /// 根据一种四参数模型的参数推导另一种四参数模型的参数
        /// </summary>
        /// <param name="dx1">平移量(米)</param>
        /// <param name="dy1">平移量(米)</param>
        /// <param name="r1">旋转量(弧度)</param>
        /// <param name="s1">尺度(ppm)</param>
        /// <param name="mode1">输入模型</param>
        /// <param name="dx2">平移量(米)</param>
        /// <param name="dy2">平移量(米)</param>
        /// <param name="r2">旋转量(弧度)</param>
        /// <param name="s2">尺度(ppm)</param>
        /// <param name="mode2">输出模型</param>
        public static void ChangeMode(double dx1, double dy1, double r1, double s1, string mode1,
            out double dx2, out double dy2, out double r2, out double s2, string mode2)
        {
            dx2 = dy2 = r2 = s2 = 0d;
            if (string.IsNullOrWhiteSpace(mode1)) return;

            if (mode1.StartsWith("o"))
            {
                if (mode2.StartsWith("o"))
                {
                    dx2 = dx1;
                    dy2 = dy1;
                    r2 = r1;
                    s2 = s1;
                }
                else
                {
                    r2 = r1;
                    s2 = s1;
                    dx2 = (1 + s1 * 1e-6) * (Math.Cos(r1) * dx1 + Math.Sin(r1) * dy1);
                    dy2 = (1 + s1 * 1e-6) * (-Math.Sin(r1) * dx1 + Math.Cos(r1) * dy1);
                }
            }
            else if (mode1.EndsWith("o"))
            {
                if (mode2.EndsWith("o"))
                {
                    dx2 = dx1;
                    dy2 = dy1;
                    r2 = r1;
                    s2 = s1;
                }
                else
                {
                    r2 = r1;
                    s2 = s1;
                    dx2 = 1 / (1 + s1 * 1e-6) * (Math.Cos(r1) * dx1 - Math.Sin(r1) * dy1);
                    dy2 = 1 / (1 + s1 * 1e-6) * (Math.Sin(r1) * dx1 + Math.Cos(r1) * dy1);
                }
            }
        }

        /// <summary>
        /// 根据一种四参数模型的参数推导另一种四参数模型的参数
        /// </summary>
        /// <param name="inFour">输入四参数</param>
        /// <param name="outMode">输出模型</param>
        /// <returns></returns>
        public static FourPara ChangeMode(FourPara inFour, enumFourMode outMode)
        {
            if (inFour is null) return null;

            double dx2, dy2, r2, s2;
            ChangeMode(inFour.DX, inFour.DY, inFour.R, inFour.S, inFour.Mode, out dx2, out dy2, out r2, out s2, outMode);
            FourPara outFour = new FourPara
            {
                DX = dx2,
                DY = dy2,
                R = r2,
                S = s2,
                Mode = outMode,
            };

            return outFour;
        }

        /// <summary>
        /// 交换x和y方向的参数
        /// </summary>
        /// <param name="dx1">平移量(m)</param>
        /// <param name="dy1">平移量(m)</param>
        /// <param name="r1">旋转</param>
        /// <param name="s1">尺度(ppm)</param>
        /// <param name="dx2"></param>
        /// <param name="dy2"></param>
        /// <param name="r2"></param>
        /// <param name="s2"></param>
        public static void SwapXY(double dx1, double dy1, double r1, double s1,
            out double dx2, out double dy2, out double r2, out double s2)
        {
            dx2 = dy1;
            dy2 = dx1;
            r2 = -r1;
            s2 = s1;
        }

        /// <summary>
        /// 交换x和y方向的参数
        /// </summary>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        /// <param name="r"></param>
        /// <param name="s"></param>
        public static void SwapXY(ref double dx, ref double dy, ref double r, ref double s)
        {
            double temp = dx;
            dx = dy;
            dy = temp;
            r = -r;
            s = s;
        }

        /// <summary>
        /// 根据正算的四参数 计算 反算的四参数
        /// </summary>
        /// <param name="mode">四参数模型</param>
        /// <param name="dx1">正算:平移量(米)</param>
        /// <param name="dy1">正算:平移量(米)</param>
        /// <param name="r1">正算:旋转量(弧度)</param>
        /// <param name="s1">正算:尺度(ppm)</param>
        /// <param name="dx2">反算:平移量(米)</param>
        /// <param name="dy2">反算:平移量(米)</param>
        /// <param name="r2">反算:旋转量(弧度)</param>
        /// <param name="s2">反算:尺度(ppm)</param>
        public static void GetInv(string mode, double dx1, double dy1, double r1, double s1,
            out double dx2, out double dy2, out double r2, out double s2)
        {
            if (mode.StartsWith("o"))
            {
                s2 = (1d / (1 + s1 * 1e-6) - 1d) * 1e6;
                r2 = -r1;

                dx2 = -(1 + s1 * 1e-6) * (Math.Cos(r1) * dx1 + Math.Sin(r1) * dy1);
                dy2 = -(1 + s1 * 1e-6) * (-Math.Sin(r1) * dx1 + Math.Cos(r1) * dy1);

            }
            else if (mode.EndsWith("o"))
            {
                r2 = -r1;
                s2 = (1d / (1d + s1 * 1e-6) - 1d) * 1e6;

                dx2 = -(1 + s2 * 1e-6) * (Math.Cos(r2) * dx1 + Math.Sin(r2) * dy1);
                dy2 = -(1 + s2 * 1e-6) * (-Math.Sin(r2) * dx1 + Math.Cos(r2) * dy1);
            }
            else
            {
                throw new Exception("无法识别的模型");
            }
        }

        /// <summary>
        /// 根据正算的四参数计算反算的四参数
        /// </summary>
        /// <param name="inFour">正算四参数</param>
        /// <returns>反算四参数</returns>
        public static FourPara GetInv(FourPara inFour)
        {
            if (inFour is null) return null;

            string modeStr = Mode2Str(inFour.Mode);

            double dx, dy, r, s;
            GetInv(modeStr, inFour.DX, inFour.DY, inFour.R, inFour.S, out dx, out dy, out r, out s);

            FourPara outFour = new FourPara
            {
                DX = dx,
                DY = dy,
                R = r,
                S = s,
                Mode = inFour.Mode,
            };

            return outFour;
        }

        /// <summary>
        /// 计算四参数
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="mode">默认是"ors",即o(ffset)-r(otate)-s(cale)</param>
        /// <returns></returns>
        public static FourPara CalPara(List<double> x1, List<double> y1,
            List<double> x2, List<double> y2, string mode = "rso")
        {
            if (x1 == null || y1 == null || x2 == null || y2 == null)
            {
                return null;
            }

            //// 模型,默认是 旋转-缩放-平移
            if(mode is null)
            {
                mode = "rso";
            }

            int minCnt = Min(Min(x1.Count, y1.Count), Min(x2.Count, y2.Count));

            if (minCnt < 2) return null;

            Matrix<double> B = new DenseMatrix(minCnt * 2, 4);
            Vector<double> L = new DenseVector(minCnt * 2);

            for (int i = 0; i < minCnt; i++)
            {
                B[i * 2, 0] = y1[i];
                B[i * 2, 1] = x1[i];
                B[i * 2, 2] = 1;
                B[i * 2, 3] = 0;

                B[i * 2 + 1, 0] = -x1[i];
                B[i * 2 + 1, 1] = y1[i];
                B[i * 2 + 1, 2] = 0;
                B[i * 2 + 1, 3] = 1;

                L[i * 2] = x2[i];
                L[i * 2 + 1] = y2[i];
            }

            Vector<double> xHat = (B.Transpose() * B).Inverse() * (B.Transpose() * L);

            double a = xHat[0];  // m*sin(θ)
            double b = xHat[1];  // m*cos(θ)
            double c = xHat[2];  // Δx
            double d = xHat[3];  // Δy

            FourPara four = new FourPara
            {
                Mode = enumFourMode.RSO,
                R = Math.Atan2(a, b),
                S = (Math.Sqrt(a * a + b * b) - 1) * 1e6,
                DX = c,
                DY = d
            };

            try
            {
                //// 将模型改为 平移-旋转-缩放
                if (mode.ToLower().StartsWith("o"))
                {
                    double dx = four.DX;
                    double dy = four.DY;

                    four.DX = (1 + four.S * 1e-6) * (Math.Cos(four.R) * dx + Math.Sin(four.R) * dy);
                    four.DY = (1 + four.S * 1e-6) * (-Math.Sin(four.R) * dx + Math.Cos(four.R) * dy);
                }
            }
            catch (Exception ex)
            {
            }

            return four;
        }

        /// <summary>
        /// 计算四参数
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="dx">平移量(米)</param>
        /// <param name="dy">平移量(米)</param>
        /// <param name="r">旋转(弧度)</param>
        /// <param name="s">尺度(ppm)</param>
        /// <param name="mode">模型 ors或者rso o(ffset) s(cale) r(otate)</param>
        /// <returns></returns>
        public static bool CalPara(List<double> x1, List<double> y1,
            List<double> x2, List<double> y2, out double dx, out double dy, out double r, out double s,
            string mode = "ors")
        {
            dx = dy = r = s = 0;
            if (x1 == null || y1 == null || x2 == null || y2 == null)
            {
                return false;
            }

            int minCnt = Min(Min(x1.Count, y1.Count), Min(x2.Count, y2.Count));

            if (minCnt < 2) return false;

            Matrix<double> B = new DenseMatrix(minCnt * 2, 4);
            Vector<double> L = new DenseVector(minCnt * 2);

            for (int i = 0; i < minCnt; i++)
            {
                B[i * 2, 0] = y1[i];
                B[i * 2, 1] = x1[i];
                B[i * 2, 2] = 1;
                B[i * 2, 3] = 0;

                B[i * 2 + 1, 0] = -x1[i];
                B[i * 2 + 1, 1] = y1[i];
                B[i * 2 + 1, 2] = 0;
                B[i * 2 + 1, 3] = 1;

                L[i * 2] = x2[i];
                L[i * 2 + 1] = y2[i];
            }

            Vector<double> xHat = (B.Transpose() * B).Inverse() * (B.Transpose() * L);

            double a = xHat[0];  // m*sin(θ)
            double b = xHat[1];  // m*cos(θ)
            double c = xHat[2];  // Δx
            double d = xHat[3];  // Δy

            r = Math.Atan2(a, b);
            s = (Math.Sqrt(a * a + b * b) - 1) * 1e6;
            dx = c;
            dy = d;

            //// 计算出参数的模型是rso，转换成ors
            if (mode.StartsWith("o"))
            {
                double dx2, dy2, r2, s2;
                ChangeMode(dx, dy, r, s, "rso", out dx2, out dy2, out r2, out s2, "ors");
                dx = dx2;
                dy = dy2;
                r = r2;
                s = s2;
            }

            return true;
        }

        /// <summary>
        /// 迭代计算四参数，剔除粗差(三倍中误差)的点
        /// </summary>
        /// <param name="x1">x坐标(米)</param>
        /// <param name="y1">y坐标(米)</param>
        /// <param name="x2">x坐标(米)</param>
        /// <param name="y2">y坐标(米)</param>
        /// <returns></returns>
        public static FourPara CalParaIter(List<double> x1, List<double> y1,
            List<double> x2, List<double> y2)
        {
            List<int> counts = new List<int> { x1.Count, x2.Count, y1.Count, y2.Count };
            int minCount = counts.Min();
            List<int> indexes = Enumerable.Range(0, minCount).ToList();

            FourPara four = new FourPara();

            Trans trans = new Trans();

            List<double> x3 = new List<double>();
            List<double> y3 = new List<double>();

            List<double> xx1 = new List<double>();
            List<double> yy1 = new List<double>();
            List<double> xx2 = new List<double>();
            List<double> yy2 = new List<double>();

            while (indexes.Count > 3)
            {
                x3.Clear();
                y3.Clear();

                xx1.Clear();
                yy1.Clear();
                xx2.Clear();
                yy2.Clear();

                for (int i = 0; i < indexes.Count; i++)
                {
                    xx1.Add(x1[indexes[i]]);
                    yy1.Add(y1[indexes[i]]);
                    xx2.Add(x2[indexes[i]]);
                    yy2.Add(y2[indexes[i]]);
                }

                //// 计算四参数
                four = CalPara(xx1, yy1, xx2, yy2);

                //// 用得到的四参数进行转换
                double x = 0d, y = 0d;
                for (int i = 0; i < xx1.Count; i++)
                {
                    trans.Four(xx1[i], yy1[i], out x, out y, four.DX, four.DY, four.R, four.S);
                    x3.Add(x);
                    y3.Add(y);
                }

                //// 计算点位误差
                var diff = from i in Enumerable.Range(0, indexes.Count)
                           select new
                           {
                               Index = indexes[i],
                               Diff = Math.Sqrt(Math.Pow(x3[i] - x2[indexes[i]], 2) +
                               Math.Pow(y3[i] - y2[indexes[i]], 2)),
                           };

                //// 计算验后中误差
                double sigma = Math.Sqrt(diff.Sum(d => d.Diff * d.Diff) / indexes.Count);

                var max = diff.OrderByDescending(d => d.Diff).First();

                //// 剔除点
                if (max.Diff < sigma * 3d) break;
                else
                {
                    indexes.Remove(max.Index);
                }
            }

            return four;
        }

        private static int Min(int cnt1, int cnt2)
        {
            return cnt1 < cnt2 ? cnt1 : cnt2;
        }

        public static string Mode2Str(enumFourMode mode)
        {
            switch (mode)
            {
                case enumFourMode.ORS:
                    return "ors";
                case enumFourMode.OSR:
                    return "osr";
                case enumFourMode.RSO:
                    return "rso";
                case enumFourMode.SRO:
                    return "sro";
                default:
                    return "ors";
            }
        }

        public static enumFourMode Str2Mode(string mode)
        {
            mode = mode.ToLower();
            if (mode.StartsWith("o"))
            {
                return enumFourMode.ORS;
            }
            else if (mode.EndsWith("o"))
            {
                return enumFourMode.RSO;
            }
            //// 默认是与PowerCoor一致
            else
            {
                return enumFourMode.ORS;
            }
        }
    }
}
