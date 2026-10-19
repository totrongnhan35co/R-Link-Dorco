using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLinkMasterSimulator.Models;

public class LogStatusPayload
{
    public string line_id { get; set; } = "";
    public string? rlink_name { get; set; }
    public string? job_name { get; set; }
    public string? batch { get; set; }
    public string? product_id { get; set; }
    public string? product_name { get; set; }
    /// <summary>start | running | stop | completed</summary>
    public string? status { get; set; }
    public string? rlink_status { get; set; }
    public string? operator_user { get; set; }
    public int qr_used { get; set; }
    public int produced { get; set; }
    public DateTime timestamp { get; set; } = DateTime.Now;
}