using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenNETCF.ORM.SqlServer.Integration.Test.Entities
{
    public enum HVACControlState
    {
        Heating,
        Cooling
    }

    [Entity(KeyScheme = KeyScheme.GUID)]
    public class PublishedTenantApartmentState : PublishedEntityBase
    {
        public PublishedTenantApartmentState()
        {
        }

        /// <summary>
        /// Foreign-key to the PublishedTenenantBuildingState
        /// </summary>
        [Field]
        public Guid PublishedBuildingStateID { get; set; }

        [Field]
        public string ApartmentName { get; set; }

        /// <summary>
        /// Last time the apartment thermostat was contacted
        /// </summary>
        [Field]
        public DateTime LastContact { get; set; }

        [Field]
        public HVACControlState ControlState { get; set; }

        [Field]
        public bool Occupied { get; set; }

        [Field]
        public double SpaceTemperature { get; set; }

        [Field]
        public double? SupplyTemperature { get; set; }

        [Field]
        public double? ReturnTemperature { get; set; }

        [Field]
        public double SetPoint { get; set; }

        [Field]
        public double HeatDeadband { get; set; }

        [Field]
        public double CoolDeadband { get; set; }
    }
}
