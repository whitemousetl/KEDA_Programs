using KEDA_Common.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Controller;
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ProtocolTypeAttribute : Attribute
{
    public ProtocolType ProtocolType { get;}
	public ProtocolTypeAttribute(ProtocolType type)
	{
		ProtocolType = type;
	}
}
