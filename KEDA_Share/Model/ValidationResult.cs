using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Share.Model;
public class ValidationResult
{
    public bool IsValid {  get; set; }
    public string? ErrorMessage {  get; set; }
}
