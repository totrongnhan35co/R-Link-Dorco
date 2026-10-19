using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLinkMasterSimulator.Models;

public class MarkQrUsedRequest
{
    public List<string> QrCodes { get; set; } = new();
    public string JobName { get; set; } = "";
    public string LineId { get; set; } = "";
    public string LineName { get; set; } = "";
    public string FactoryCode { get; set; } = "";
    public string Batch { get; set; } = "";
    public string RLinkName { get; set; } = "";
    public DateTime? UsedAt { get; set; }
}

public class MarkQrUnusedRequest
{
    public List<string> QrCodes { get; set; } = new();
    public string JobName { get; set; } = "";
    public DateTime? ReleasedAt { get; set; }
}
