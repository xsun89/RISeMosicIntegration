using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;

namespace UBC.RISe.MosicIntegration.DataModels
{
    public class MosaicReturnedEntity
    {
        public List<COR_PERSON_RedactedDto> PersonCreated { get; set; }
        public List<VIV_ANIMAL_USE_MASTER_Dto> MasterProtocolCreatedOrUpdated { get; set; }
        public List<VIV_ANIMAL_USE_PROTOCOL_Dto> AllocationCreatedOrUpdated { get; set; }
        public List<COR_LOOKUP_VALUE_RedactedDto> SpeciesCreated { get; set; }

        public MosaicReturnedEntity()
        {

        }
    }
}
