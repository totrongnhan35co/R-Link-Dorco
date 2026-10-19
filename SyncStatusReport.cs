namespace RLinkMasterSimulator.Models;

public class SyncStatusReport
{
    public string line_id      { get; set; } = "";
    public string rlink_name   { get; set; } = "";
    public int pending_in      { get; set; }
    public int pending_camera  { get; set; }
    public int pending_error   { get; set; }
    public DateTime reported_at { get; set; }
}