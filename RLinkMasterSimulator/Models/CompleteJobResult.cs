using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLinkMasterSimulator.Models;

public class CompleteJobResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = "";
    public List<string> AllocatedQrCodes { get; set; } = new();
}