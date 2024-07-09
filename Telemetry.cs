using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOT_DEVICE_CRUD
{
    public class Telemetry
    {
        public decimal Latitude { get; set; }

        public decimal Longitude { get; set; }

        public StatusType Status { get; set; }
    }
}
