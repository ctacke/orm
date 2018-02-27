using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenNETCF.ORM.SqlServer.Integration.Test.Entities
{
    public abstract class PublishedEntityBase
    {
        public PublishedEntityBase()
        {
        }

        public PublishedEntityBase(Guid publishID, int clientID, int portfolioID, int engineID)
        {
            if (publishID == Guid.Empty)
            {
                this.PublishID = Guid.NewGuid();
            }
            else
            {
                this.PublishID = publishID;
            }

            RecordDateUtc = DateTime.Now.ToUniversalTime();

            this.EngineID = engineID;
            this.ClientID = clientID;
            this.PortfolioID = portfolioID;
        }

        [Field(IsPrimaryKey = true)]
        public Guid PublishID { get; set; }

        [Field]
        public int EngineID { get; set; }

        /// <summary>
        /// Time recorded at the building
        /// </summary>
        [Field(SearchOrder = FieldSearchOrder.Descending)]
        public DateTime RecordDateUtc { get; set; }

        /// <summary>
        /// Time it was stored at the server
        /// </summary>
        [Field]
        public DateTime StoredDateUtc { get; set; }

        [Field(SearchOrder = FieldSearchOrder.Ascending)]
        public int ClientID { get; set; }

        [Field(SearchOrder = FieldSearchOrder.Ascending)]
        public int PortfolioID { get; set; }
    }
}
