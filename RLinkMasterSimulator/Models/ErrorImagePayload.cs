using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLinkMasterSimulator.Models;

public class ErrorImagePayload
{
    public string line_id { get; set; } = "";
    public string? job_name { get; set; }
    public string? batch { get; set; }
    public string? qr_code { get; set; }
    public string? image_base64 { get; set; }
    public DateTime timestamp { get; set; } = DateTime.Now;
}