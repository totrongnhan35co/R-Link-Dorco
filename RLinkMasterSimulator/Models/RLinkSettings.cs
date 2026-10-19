using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLinkMasterSimulator.Models;

public class RLinkSettings
{
    public int OperatingMode { get; set; } = 1;   // 1=BatchOneQr | 2=AutoRefresh | 3=ProductOneQr
    public int DeltaMinutes { get; set; } = 5;
    public int NMinutes { get; set; } = 30;
    public int BufferCount { get; set; } = 50;
    public int MonitoringIntervalMinutes { get; set; } = 5;
    public int LogIntervalMinutes { get; set; } = 10;
    public int QrThreshold { get; set; } = 100;
    public string ErrorImageFolder { get; set; } = @"C:\THTrueMilk\ErrorImages";
}