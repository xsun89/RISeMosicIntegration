using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace UBC.RISe.MosicIntegration.DataModels
{
    public class ExceptionError
    {
        [JsonProperty("Name")]
        public string Name { get; set; }
        [JsonProperty("Description")]
        public string Description { get; set; }
    }

    public class ExceptionMessage
    {
        [JsonProperty("Message")]
        public string Message { get; set; }
        [JsonProperty("Errors")]
        public List<ExceptionError> Errors { get; set; }

        
    }

    public class MosaicValidationException : Exception
    {
        public ExceptionMessage ExceptionMessage { get; set; }
        public string OtherErrorMessage { get; set; }
        public MosaicValidationException()
        {

        }

        public MosaicValidationException(ExceptionMessage msg = null, string otherErrorMessage = null)
        {
            ExceptionMessage = msg;
            OtherErrorMessage = otherErrorMessage;
        }

    }
}
