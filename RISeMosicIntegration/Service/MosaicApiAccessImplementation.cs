using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UBC.RISe.MosicIntegration.DataModels;

namespace UBC.RISe.MosicIntegration.Service
{
    public class MosaicApiAccessImplementation
    {
        public List<string> Cookie { get; set; }
        public RequestConfigurations Config { get; set; }

        public MosaicApiAccessImplementation(RequestConfigurations requestConfig)
        {
            Config = requestConfig;
            Cookie = new List<string>();
        }

        private async Task<HttpResponseMessage> GetHttpResponse(string url, bool reqCookie, string requestJson)
        {
            Uri uri = new Uri(url);
            StringContent content = null;
            if (!string.IsNullOrWhiteSpace(requestJson))
            {
                content = new StringContent(requestJson, Encoding.UTF8, "application/json");
            }

            HttpClient client = null;
            if (reqCookie)
            {
                HttpClientHandler handler = new HttpClientHandler();
                handler.CookieContainer = new CookieContainer();
                if (this.Cookie != null)
                {
                    string cookieName = Cookie.First().Split('=')[0];
                    string cookieValue = Cookie.First().Split('=')[1];

                    handler.CookieContainer.Add(uri, new Cookie(cookieName, cookieValue));
                }

                client = new HttpClient(handler);
            }
            else
            {
                client = new HttpClient();
            }

            HttpResponseMessage response = null;
            if (content == null)
            {
                response = await client.GetAsync(uri);
            }
            else
            {
                response = await client.PostAsync(uri, content);
            }


            return response;
        }

        

        private async Task<string> ResponseContent(string url, string json, bool reqCookie = true)
        {
            var task = this.GetHttpResponse(url, reqCookie, json);
            task.Wait();
            var response = task.Result;
            if (response.Content == null)
            {
                throw new MosaicValidationException(otherErrorMessage: "No Content Error");
            }

            string responseContent = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode == false)
            {
                var exceptionMessage =
                    JsonConvert.DeserializeObject<ExceptionMessage>(responseContent);
                if (exceptionMessage != null && !string.IsNullOrEmpty(exceptionMessage.Message) &&
                    exceptionMessage.Errors.Count > 0)
                    throw new MosaicValidationException(exceptionMessage);
            }

            response.EnsureSuccessStatusCode();
            return responseContent;
        }

        public async Task<string> Authenticate()
        {
            string url = this.Config.ConfigurationUrl + @"customer/Public/Authenticate";
            string json = JsonConvert.SerializeObject(Config.AuthenticateRequest);
            var responseContent = await ResponseContent(url, json, false);
            AuthenticateResponse authenticateResponse =
               JsonConvert.DeserializeObject<AuthenticateResponse>(responseContent);
            this.Cookie = authenticateResponse.Cookies;
            return responseContent;
        }

        public async Task<string> CorLookupListGetAll()
        {
            
            string url = this.Config.ConfigurationUrl + "customer/CorLookupList/GetAll";
            string json = "{\"Message\": \"string\"}";

            var responseContent = await ResponseContent(url, json);

            return responseContent;
        }
        public async Task<string> GetDefaultEntityForMosaic(string method, int count)
        {
           
            string url = this.Config.ConfigurationUrl + method;
            string json = "{\"Count\":" + count + "}";

            var responseContent = await ResponseContent(url, json);


            return responseContent;
        }

        public async Task<string> GetCorLookupValueByID(string para, string listKey)
        {
            string ret = string.Empty;
            string url = this.Config.ConfigurationUrl + "customer/CorLookupValue/GetByListId";
            string json = "{\"Guids\": [\"" + listKey + "\"]}";

            var responseContent = await ResponseContent(url, json);


            var vlaueLookupResponse =
                    JsonConvert.DeserializeObject<List<ValueLookupObject>>(responseContent);

                ret = vlaueLookupResponse.SelectMany(m => m.Value).Where(c => c.LOOKV_SHORT_VALUE == para)
                    .Select(v => v.LOOKV_GUID).FirstOrDefault();
            

            return ret;
        }

        public async Task<string> GetEntityByID(string key, string method)
        {
            
            string url = this.Config.ConfigurationUrl + method;
            string json = "{\"Guids\": [\"" + key + "\"]}";

            var responseContent = await ResponseContent(url, json);


            
            return responseContent;
        }
        public async Task<string> GetResultsByAlternateKey(string key, string method)
        {
            string url = this.Config.ConfigurationUrl + method;
            string json = "{\"Items\": [\"" + key + "\"]}";
            var responseContent = await ResponseContent(url, json);

            return responseContent;
        }

        public async Task<string> GetResultsByAlternateKeyList(string keysList, string method)
        {
            string url = this.Config.ConfigurationUrl + method;
            string json = "{\"Items\": " + keysList + "}";
            var responseContent = await ResponseContent(url, json);

            return responseContent;
        }
        public async Task<string> CreateMosaicEntity(string method, string jsonStr)
        {
            
            string url = this.Config.ConfigurationUrl + method;
            string json = "{\"Items\":" + jsonStr + "}";
            var responseContent = await ResponseContent(url, json);

            
            return responseContent;
        }
    }
}