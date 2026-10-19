using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLinkMasterSimulator.Models;

public class LineInfo
{
    public string factory_code { get; set; } = "";
    public string factory_name { get; set; } = ""; //
    public string line_id { get; set; } = "";
    public string line_name { get; set; } = "";
    public string line_ip { get; set; } = "";
    public bool is_active { get; set; }
}