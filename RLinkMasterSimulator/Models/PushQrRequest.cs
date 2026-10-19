using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLinkMasterSimulator.Models;

public class PushQrRequest
{
    public string RLinkIp { get; set; } = "";
    public int RLinkPort { get; set; } = 5002;
    public string FactoryCode { get; set; } = "";
    public string LineId { get; set; } = "";
    public string LineName { get; set; } = "";
    public int Count { get; set; } = 10;
    public string BearerToken { get; set; } = "";

    // Batch là optional — R-Link tự quản lý khi tạo job
    // Có thể truyền nếu Master biết batch hiện tại, bỏ trống cũng được
    public string? Batch { get; set; } = null;
}