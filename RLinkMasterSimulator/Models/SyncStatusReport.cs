using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLinkMasterSimulator.Models;

public class SyncStatusReport
{
    public string line_id { get; set; } = "";
    public string rlink_name { get; set; } = "";
    public int pending_in { get; set; }
    public int pending_camera { get; set; }
    public int pending_error { get; set; }
    public DateTime reported_at { get; set; }
    public List<SyncJobDetailItem> job_details { get; set; } = new();
}

public class SyncJobDetailItem
{
    public string job_name { get; set; } = "";
    public string batch { get; set; } = "";
    public int pending_in { get; set; }
    public int pending_camera { get; set; }
    public int pending_error { get; set; }
}