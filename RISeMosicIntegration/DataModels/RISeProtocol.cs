using System.Collections.Generic;

namespace UBC.RISe.MosicIntegration.DataModels
{
    public class RISePerson
    {
        public string pi_cwl { get; set; }
        public string pi_firstname { get; set; }
        public string pi_lastname { get; set; }
        public string pi_email { get; set; }
        public string pi_phone { get; set; }
    }

    public class RISeOtherPerson
    {
        public string per_cwl { get; set; }
        public string per_firstname { get; set; }
        public string per_lastname { get; set; }
        public string per_email { get; set; }
        public string per_phone { get; set; }
    }
    public class RISeProtocol
    {
        public string prot_cur_state { get; set; }
        public string prot_nxt_state { get; set; }
        public string prot_action { get; set; }
        public string prot_con_approval { get; set; }
        public string prot_no { get; set; }
        public string prot_title { get; set; }
        public string prot_start { get; set; }
        public string prot_expire { get; set; }
        public List<RISePerson> prot_pi { get; set; }
        public string prot_type { get; set; }
        public string prot_purpose { get; set; }
        public string prot_keywords { get; set; }
        public string prot_ccaccategory { get; set; }
        public List<RISeProtocolSpecies> prot_species { get; set; }
        public List<RISeOtherPerson> prot_persons { get; set; }
        public string prot_total_num { get; set; }
        public string prot_url { get; set; }

        
        
    }
}
