﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using GeoFun.GNSS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace GeoFun.GNSS.Tests
{
    [TestClass()]
    public class ObservationTests
    {
        [TestMethod()]
        public void CalP4Test()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void CalL4Test()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void DetectOutlierTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void DetectCycleSlipTest()
        {
            OStation station = new OStation("fjpt");
            station.ReadAllObs();
            station.DetectCycleSlip();
        }

        [TestMethod()]
        public void DetectClockJumpAndRepairTest()
        {
            OFile ofile = OFile.Read(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Data","rinex","30900700.11o"));
            Assert.IsNotNull(ofile,"读取文件失败!");

            Console.WriteLine(ofile.Path);

            ObsHelper.DetectClockJumpAndRepair(ref ofile.Epoches, (int)ofile.Header.interval);
        }

        [TestMethod()]
        public void DetectArcTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void DetectArcsTest()
        {
            Assert.Fail();
        }
    }
}