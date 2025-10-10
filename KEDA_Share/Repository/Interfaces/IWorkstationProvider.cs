using KEDA_Share.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Share.Repository.Interfaces;
public interface IWorkstationProvider
{
    Workstation? Current { get; }
    Task StartAsync(CancellationToken cancellationToken);
}

