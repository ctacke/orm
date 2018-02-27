using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenNETCF.ORM.SqlServer.Integration.Test.Entities
{
    [Entity(KeyScheme = KeyScheme.GUID)]
    public class PublishedTenantBuildingState : PublishedEntityBase
    {
        public PublishedTenantBuildingState()
        {
        }

        [Field]
        public double OutsideTemperature { get; set; }
        [Field]
        public double UnoccupiedSetPoint { get; set; }
        [Field]
        public double UnoccupiedHeatDeadband { get; set; }
        [Field]
        public double UnoccupiedCoolDeadband { get; set; }
    }
}
