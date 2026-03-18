using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Generators;

public interface IVehicleModelGenerator
{
    public List<VehicleModelJsonItem> Generate();
}

