﻿using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace GeoFun
{
    public class SevenPara
    {
        /// <summary>
        /// 平移参数(m)
        /// </summary>
        public double XOff { get; set; }

        /// <summary>
        /// 平移参数(m)
        /// </summary>
        public double YOff { get; set; }

        /// <summary>
        /// 平移参数(m)
        /// </summary>
        public double ZOff { get; set; }

        /// <summary>
        /// 旋转(秒)
        /// </summary>
        public double XRot { get; set; }

        /// <summary>
        /// 旋转(秒)
        /// </summary>
        public double YRot { get; set; }

        /// <summary>
        /// 旋转(秒)
        /// </summary>
        public double ZRot { get; set; }

        /// <summary>
        /// 尺度参数(ppm)
        /// </summary>
        public double M { get; set; }

        /// <summary>
        /// 计算七参数(椭球1到椭球2)
        /// </summary>
        /// <param name="b1">纬度(弧度)</param>
        /// <param name="l1">经度(弧度)</param>
        /// <param name="h1">大地高</param>
        /// <param name="b2">纬度(弧度)</param>
        /// <param name="l2">经度(弧度)</param>
        /// <param name="h2">大地高</param>
        /// <param name="ell1">椭球1</param>
        /// <param name="ell2">椭球2</param>
        /// <returns></returns>
        public static SevenPara CalPara(List<double> b1, List<double> l1, List<double> h1,
            List<double> b2, List<double> l2, List<double> h2,
            Ellipsoid ell1, Ellipsoid ell2)
        {
            if (b1 is null || l1 is null || b2 is null || l2 is null || ell1 is null || ell2 is null) throw new ArgumentNullException("公共点不能为空");
            if ((h1 is null) || (h2 is null)) throw new ArgumentNullException("至少需要一个坐标系的高程值");

            if (h1 is null) h1 = h2;
            if (h2 is null) h2 = h1;

            var leng = new List<int> { b1.Count, l1.Count, h1.Count, b2.Count, l2.Count, h2.Count };
            var pointNum = leng.Min();

            if (pointNum < 3) throw new ArgumentException("公共点不足");

            List<double> X1 = new List<double>();
            List<double> Y1 = new List<double>();
            List<double> Z1 = new List<double>();

            List<double> X2 = new List<double>();
            List<double> Y2 = new List<double>();
            List<double> Z2 = new List<double>();

            Trans trans = new Trans();
            for (int i = 0; i < pointNum; i++)
            {
                double x, y, z;
                Coordinate.BLH2XYZ(b1[i], l1[i], h1[i], out x, out y, out z, ell1);
                X1.Add(x); Y1.Add(y); Z1.Add(z);

                Coordinate.BLH2XYZ(b2[i], l2[i], h2[i], out x, out y, out z, ell2);
                X2.Add(x); Y2.Add(y); Z2.Add(z);
            }

            Matrix<double> matrix = new DenseMatrix(pointNum * 3, 7);
            Vector<double> vector = new DenseVector(pointNum * 3);

            for (int i = 0; i < pointNum; i++)
            {
                matrix[i * 3, 0] = 1;
                matrix[i * 3, 1] = 0;
                matrix[i * 3, 2] = 0;
                matrix[i * 3, 3] = X1[i];
                matrix[i * 3, 4] = 0;
                matrix[i * 3, 5] = -Z1[i];
                matrix[i * 3, 6] = Y1[i];

                matrix[i * 3 + 1, 0] = 0;
                matrix[i * 3 + 1, 1] = 1;
                matrix[i * 3 + 1, 2] = 0;
                matrix[i * 3 + 1, 3] = Y1[i];
                matrix[i * 3 + 1, 4] = Z1[i];
                matrix[i * 3 + 1, 5] = 0;
                matrix[i * 3 + 1, 6] = -X1[i];

                matrix[i * 3 + 2, 0] = 0;
                matrix[i * 3 + 2, 1] = 0;
                matrix[i * 3 + 2, 2] = 1;
                matrix[i * 3 + 2, 3] = Z1[i];
                matrix[i * 3 + 2, 4] = -Y1[i];
                matrix[i * 3 + 2, 5] = X1[i];
                matrix[i * 3 + 2, 6] = 0;

                vector[i * 3] = X2[i];
                vector[i * 3 + 1] = Y2[i];
                vector[i * 3 + 2] = Z2[i];
            }

            var BTPB = (matrix.Transpose().Multiply(matrix)).Inverse();
            var BTPL = matrix.Transpose().Multiply(vector);
            var result = BTPB.Multiply(BTPL);

            double dX = result[0];
            double dY = result[1];
            double dZ = result[2];

            double a1 = result[3];
            double a2 = result[4];
            double a3 = result[5];
            double a4 = result[6];

            double m = (a1 - 1) * 1e6;
            double rx = a2 / a1;
            double ry = a3 / a1;
            double rz = a4 / a1;

            rx *= Angle.R2D * 3600;
            ry *= Angle.R2D * 3600;
            rz *= Angle.R2D * 3600;

            return new SevenPara
            {
                XOff = dX,
                YOff = dY,
                ZOff = dZ,

                XRot = rx,
                YRot = ry,
                ZRot = rz,

                M = m,
            };
        }

        /// <summary>
        /// 计算七参数(椭球1到椭球2)
        /// </summary>
        /// <param name="b1">纬度(弧度)</param>
        /// <param name="l1">经度(弧度)</param>
        /// <param name="h1">大地高</param>
        /// <param name="b2">纬度(弧度)</param>
        /// <param name="l2">经度(弧度)</param>
        /// <param name="h2">大地高</param>
        /// <param name="ell1">椭球1</param>
        /// <param name="ell2">椭球2</param>
        /// <param name="maxRes">最大残差(m)</param>
        /// <returns></returns>
        public static SevenPara CalParaIter(List<double> b1, List<double> l1, List<double> h1,
            List<double> b2, List<double> l2, List<double> h2,
            Ellipsoid ell1, Ellipsoid ell2)
        {
            if (b1 is null || l1 is null || b2 is null || l2 is null || ell1 is null || ell2 is null) throw new ArgumentNullException("公共点不能为空");
            if ((h1 is null) || (h2 is null)) throw new ArgumentNullException("至少需要一个坐标系的高程值");

            if (h1 is null) h1 = h2;
            if (h2 is null) h2 = h1;

            var leng = new List<int> { b1.Count, l1.Count, h1.Count, b2.Count, l2.Count, h2.Count };
            var pointNum = leng.Min();

            if (pointNum < 3) throw new ArgumentException("公共点不足");

            if (pointNum == 3) return CalPara(b1, l1, h1, b2, l2, h2, ell1, ell2);

            //// 最终选取的公共点坐标
            List<double> b1Com = new List<double>();
            List<double> l1Com = new List<double>();
            List<double> b2Com = new List<double>();
            List<double> l2Com = new List<double>();
            List<double> h1Com = new List<double>();
            List<double> h2Com = new List<double>();

            SevenPara sev = new SevenPara();
            Trans trans = new Trans();
            var indexes = Enumerable.Range(0, pointNum).ToList();

            //// 最大残差
            var maxDiff = 1d;
            var maxInde = 0;
            while (indexes.Count > 3)
            {
                b1Com.Clear();
                l1Com.Clear();
                h1Com.Clear();
                b2Com.Clear();
                l2Com.Clear();
                h2Com.Clear();

                foreach (var index in indexes)
                {
                    b1Com.Add(b1[index]);
                    l1Com.Add(l1[index]);
                    h1Com.Add(h1[index]);

                    b2Com.Add(b2[index]);
                    l2Com.Add(l2[index]);
                    h2Com.Add(h2[index]);
                }

                sev = CalPara(b1Com, l1Com, h1Com, b2Com, l2Com, h2Com, ell1, ell2);

                //// 转换后的blh
                List<double> b2Trans, l2Trans, h2Trans;
                trans.Seven3d(b1Com, l1Com, h1Com, out b2Trans, out l2Trans, out h2Trans, sev, ell1, ell2);

                //// 转换后的xyz
                List<double> X2Trans, Y2Trans, Z2Trans;
                Coordinate.BLH2XYZ(b2Trans, l2Trans, h2Trans, out X2Trans, out Y2Trans, out Z2Trans, Ellipsoid.ELLIP_CGCS2000);

                //// 原始的xyz
                List<double> X2Tem, Y2Tem, Z2Tem;
                Coordinate.BLH2XYZ(b2Com, l2Com, h2Com, out X2Tem, out Y2Tem, out Z2Tem, Ellipsoid.ELLIP_CGCS2000);

                ///// 计算残差(单位米)
                var diff = (from i in Enumerable.Range(0, b2Com.Count)
                            select new
                            {
                                Index = i,
                                Diff = Math.Sqrt(Math.Pow(X2Tem[i] - X2Trans[i], 2) + Math.Pow(Y2Trans[i] - Y2Tem[i], 2) + Math.Pow(Z2Trans[i] - Z2Tem[i], 2))
                            }).ToList();

                double sigma = Math.Sqrt(diff.Sum(d => d.Diff * d.Diff) / indexes.Count);

                var max = diff.OrderByDescending(d => d.Diff).First();

                maxInde = max.Index;
                maxDiff = max.Diff;

                if (maxDiff > sigma * 3d)
                {
                    indexes.Remove(maxInde);
                }
                else
                {
                    break;
                }
            }

            return sev;
        }

        override
        public string ToString()
        {
            return string.Format("{0:f4} {1:f4} {2:f4} {3:f10} {4:f10} {5:f10} {6:f10}",
                XOff, YOff, ZOff, XRot, YRot, ZRot, M);
        }
    }
}