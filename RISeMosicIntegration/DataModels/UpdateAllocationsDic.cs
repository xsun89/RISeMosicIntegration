using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UBC.RISe.MosicIntegration.DataModels;

namespace RISe.Mosic.Integration.DataModels
{
    class UpdateAllocationsDic
    {
        public string Key { get; set; }
        public List<VIV_ANIMAL_USE_PROTOCOL_Dto> Value { get; set; }
    }
}
