using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLinkMasterSimulator.Models;

public class LogCameraErrorPayload
{
    public string line_id { get; set; } = "";
    public string rlink_name { get; set; } = "";
    public string job_name { get; set; } = "";
    public string batch { get; set; } = "";
    public string product_id { get; set; } = "";
    public string product_name { get; set; } = "";
    public string rlink_status { get; set; } = "";
    public string operator_user { get; set; } = "";
    public string qr_code { get; set; } = "";
    public string expected_code { get; set; } = "";
    public string error_type { get; set; } = "";
    public string error_message { get; set; } = "";
    public string image_path { get; set; } = "";
    public long frame_index { get; set; }
    public DateTime timestamp { get; set; } = DateTime.Now;
}