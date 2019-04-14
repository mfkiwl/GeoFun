﻿using System;
using System.Collections;
using System.Collections.Generic;

namespace GeoFun.GNSS
{
    public class OSat
    {
        /// <summary>
        /// 测站名
        /// </summary>
        public string StationName;
        /// <summary>
        /// 历元时间
        /// </summary>
        public GPST Epoch;
        /// <summary>
        /// 卫星PRN号
        /// </summary>
        public string SatPRN;
        /// <summary>
        /// 各种不同的观测值，例如C1、P1、P2、L1、L2、LP1（相位平滑伪距结果）、LP2（相位平滑伪距结果）等等，用哈希表储存，便于查找.
        /// </summary>
        public Dictionary<string,double> SatData = new Dictionary<string, double>();
        /// <summary>
        /// 失锁标识符
        /// </summary>
        public Dictionary<string,int> LLI;
        /// <summary>
        /// 信号强度，1表示可能的最小信号强度，5表示良好S/N比的值，9表示可能的最大信号强度，0或空表示未知或未给出
        /// </summary>
        public Dictionary<string,int> SignalStrength;
        /// <summary>
        /// 卫星坐标，可由精密星历或者广播星历计算得到
        /// </summary>
        public Coor3 SatCoor;
        /// <summary>
        /// 卫星钟差，可有精密星历或者广播星历计算得到
        /// </summary>
        public double SatClock;
        /// <summary>
        /// 高度角，单位：度
        /// </summary>
        public double altitude_angle;
        /// <summary>
        /// 测站近似坐标
        /// </summary>
        public Coor3 approPOS;

        public double N1;

        public double N2;
        /// <summary>
        /// 利用P码伪距求得的带DCB影响的STEC
        /// </summary>
        public double STEC_PWithDCB;
        /// <summary>
        /// 利用载波相位和模糊度求得的带DCB影响的STEC
        /// </summary>
        public double STEC_CPWithDCB;

        /// <summary>
        /// 该历元是否发生周跳
        /// </summary>
        public bool IsCycleSlip;
        /// <summary>
        /// 伪距是否被平滑过
        /// </summary>
        public bool IsCodePhased;
        /// <summary>
        /// 是否缺少P1
        /// </summary>
        public bool IsLackOfP1;
    }
}