using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Share.Model;
public class MongoSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string SaveReadDb { get; set; } = string.Empty;
    public string WriteTaskDb { get; set; } = string.Empty;
    public string WriteTaskCollection { get; set; } = string.Empty;
    public string ConfigDb { get; set; } = string.Empty;
    public string ConfigCollection { get; set; } = string.Empty;
}