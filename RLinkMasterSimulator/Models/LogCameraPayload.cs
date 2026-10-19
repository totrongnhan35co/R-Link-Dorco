using System;

namespace RLinkMasterSimulator.Models;

    public class LogCameraPayload
    {
        public string line_id { get; set; } = "";
        public string rlink_name { get; set; } = "";
        public string job_name { get; set; } = "";
        public string batch { get; set; } = "";
        public string product_id { get; set; } = "";
        public string product_name { get; set; } = "";
    public string? status { get; set; }
    public int camera_ok { get; set; }
        public int camera_fail { get; set; }
        public int total_check => camera_ok + camera_fail;
        public DateTime timestamp { get; set; } = DateTime.Now;
    }
