namespace UBC.RISe.MosicIntegration.DataModels
{
    public class COR_LOOKUP_VALUE_RedactedDto
    {
        public string LOOKV_GUID { get; set; }
        
        public string LOOKV_LOOKL_GUID { get; set; }
        public string LOOKV_ORIGINAL_LOOKL_GUID { get; set; }
        public string LOOKV_PARENT_LOOKV_GUID { get; set; }
        public string LOOKV_VALUE { get; set; }
        public string LOOKV_SHORT_VALUE { get; set; }
        public object LOOKV_VALUE_DOUBLE { get; set; }
        public string LOOKV_DESCRIPTION { get; set; }
        public object LOOKV_ORDER { get; set; }
        public string LOOKV_INTERNAL_FLAG { get; set; }
        public string LOOKV_DEFAULT_FLAG { get; set; }
        public string LOOKV_ACTIVE_FLAG { get; set; }
        public string LOOKV_COLOR { get; set; }
        public string Key { get; set; }
    }
}