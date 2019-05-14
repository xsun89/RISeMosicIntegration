using Newtonsoft.Json;

namespace UBC.RISe.MosicIntegration.DataModels
{
    public class VIV_ANIMAL_USE_MASTER_Dto
    {
        [JsonProperty("VIVAUM_GUID")]
        public string VIVAUM_GUID { get; set; }
        [JsonProperty("VIVAUM_ADMIN_PERS_GUID")]
        public string VIVAUM_ADMIN_PERS_GUID { get; set; }
        [JsonProperty("VIVAUM_AUTHOR2_PERS_GUID")]
        public string VIVAUM_AUTHOR2_PERS_GUID { get; set; }
        [JsonProperty("VIVAUM_ETHICS_PERS_GUID")]
        public string VIVAUM_ETHICS_PERS_GUID { get; set; }
        [JsonProperty("VIVAUM_SUFFER_LOOKV_GUID")]
        public string VIVAUM_SUFFER_LOOKV_GUID { get; set; }
        [JsonProperty("VIVAUM_SPECIES_LOOKV_GUID")]
        public string VIVAUM_SPECIES_LOOKV_GUID { get; set; }
        [JsonProperty("VIVAUM_NAME")]
        public string VIVAUM_NAME { get; set; }
        [JsonProperty("VIVAUM_IVTEST_GUID")]
        public string VIVAUM_IVTEST_GUID { get; set; }
        [JsonProperty("VIVAUM_IDENTIFIER")]
        public string VIVAUM_IDENTIFIER { get; set; }
        [JsonProperty("VIVAUM_DESCRIPTION")]
        public string VIVAUM_DESCRIPTION { get; set; }
        [JsonProperty("VIVAUM_RESEARCH_NOTE")]
        public string VIVAUM_RESEARCH_NOTE { get; set; }
        [JsonProperty("VIVAUM_ISSUED_DATE")]
        public string VIVAUM_ISSUED_DATE { get; set; }
        [JsonProperty("VIVAUM_EXPIRATION_DATE")]
        public string VIVAUM_EXPIRATION_DATE { get; set; }
        [JsonProperty("VIVAUM_ALLOCATION_COUNT")]
        public object VIVAUM_ALLOCATION_COUNT { get; set; }
        [JsonProperty("VIVAUM_GM_ALLOCATION_COUNT")]
        public object VIVAUM_GM_ALLOCATION_COUNT { get; set; }
        [JsonProperty("VIVAUM_WT_ALLOCATION_COUNT")]
        public object VIVAUM_WT_ALLOCATION_COUNT { get; set; }
        [JsonProperty("VIVAUM_EXTERNAL_URL")]
        public string VIVAUM_EXTERNAL_URL { get; set; }
        [JsonProperty("Key")]
        public string Key { get; set; }
    }
}