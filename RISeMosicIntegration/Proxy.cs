using com.webridge.script;
using com.webridge.wom;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClickCommerce.Extranet.Mail.InBoundEmail;
using RISe.Mosic.Integration.DataModels;
using UBC.RISe.MosicIntegration.DataModels;
using UBC.RISe.MosicIntegration.Service;

namespace UBC.RISe.MosicIntegration
{
    public class Proxy : BaseReflectiveJavaMethod
    {
        public MosaicReturnedEntity MosaicReturnedEntity { get; set; }
        public bool Debug { get; set; }

        public string pushRISeDataToMosaic(com.webridge.entity.EntityType context, string host, string mappingJson,
            string jSonString, bool debug)
        {
            try
            {
                this.Debug = debug;
                MosaicReturnedEntity = new MosaicReturnedEntity();
                MosaicReturnedEntity.AllocationCreatedOrUpdated = new List<VIV_ANIMAL_USE_PROTOCOL_Dto>();
                MosaicReturnedEntity.MasterProtocolCreatedOrUpdated = new List<VIV_ANIMAL_USE_MASTER_Dto>();
                MosaicReturnedEntity.PersonCreated = new List<COR_PERSON_RedactedDto>();
                MosaicReturnedEntity.SpeciesCreated = new List<COR_LOOKUP_VALUE_RedactedDto>();

                string userName = "sun38";
                string userPassword = "Sunzhenhe36!s";
                string configUrl = "https://ca.mosaicvivarium.com/b5068a9325cf43afa9857f3e3c9188/";
                if (Debug)
                {
                    WOM.Log("ubc.rise.mosicIntegration.pushRISeDataToeMosaic(), species mapping json: " + mappingJson);
                    WOM.Log("ubc.rise.mosicIntegration.pushRISeDataToeMosaic(), request json: " + jSonString);
                }

                Dictionary<string, List<string>> speciesMappingObj =
                    JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(mappingJson);
                if (string.IsNullOrWhiteSpace(userName) ||
                    string.IsNullOrWhiteSpace(userPassword) ||
                    string.IsNullOrWhiteSpace(configUrl))
                {
                    throw new MosaicValidationException(
                        otherErrorMessage: "Authentication Url or User name or Password is null");
                }


                MosaicApiAccessImplementation myTask = MosaicApiAuthenticate(userName, userPassword, configUrl);

                if (myTask.Cookie == null)
                {
                    throw new MosaicValidationException(otherErrorMessage: "Authentication does not set Cookies");
                }

                RISeProtocol riseProtocol = JsonConvert.DeserializeObject<RISeProtocol>(jSonString);
                if (riseProtocol == null)
                {
                    throw new MosaicValidationException(null, "not able to deserialize json sent from RISe");
                }

                string ret = string.Empty;
                if (Debug)
                    WOM.Log(
                        $"ubc.rise.mosicIntegration.pushRISeDataToeMosaic(), {riseProtocol.prot_nxt_state}, {riseProtocol.prot_cur_state}");
                switch (riseProtocol.prot_nxt_state)
                {
                    case "Approved" when (riseProtocol.prot_cur_state == "Suspended" &&
                                          riseProtocol.prot_con_approval == "0" &&
                                          riseProtocol.prot_action == "create"):
                        ret = HandleProtocolApproval(riseProtocol, myTask, speciesMappingObj);
                        break;
                    case "Approved" when (riseProtocol.prot_cur_state == "Suspended" &&
                                          riseProtocol.prot_con_approval == "1" &&
                                          riseProtocol.prot_action == "create"):
                        ret = HandleProtocolApprovalHadConditionalApproval(riseProtocol, myTask, speciesMappingObj);
                        break;

                    case "Approved" when (riseProtocol.prot_action == "update"):
                    case "Suspended" when (riseProtocol.prot_action == "update"):
                    case "Terminated" when (riseProtocol.prot_action == "update"):
                    case "Expired" when (riseProtocol.prot_action == "update"):

                        ret = HandleProtocolSuspention(riseProtocol, myTask, speciesMappingObj);
                        break;

                    default:
                        throw new MosaicValidationException(null, "No Action Taken");
                }

                if (Debug)
                {
                    WOM.Log("ubc.rise.mosicIntegration.pushRISeDataToeMosaic(), result: " + "Success");
                }

                return "{\"success\":\"true\",\"message\":" + ret + "}";

                ;
            }
            catch (MosaicValidationException msg)
            {
                string retMsg = string.Empty;
                StringBuilder sb = new StringBuilder();
                if (msg.ExceptionMessage != null)
                {
                    sb.AppendLine(msg.ExceptionMessage.Message);
                    sb.AppendLine(msg.ExceptionMessage.Errors.FirstOrDefault()?.Description);
                    WOM.Log("" + msg.ExceptionMessage.Message);
                    WOM.Log(msg.ExceptionMessage.Errors.FirstOrDefault()?.Description);
                }

                if (msg.OtherErrorMessage != null)
                {
                    sb.AppendLine(msg.OtherErrorMessage);
                    WOM.Log(msg.OtherErrorMessage);
                }

                return "{\"success\":\"false\",\"message\":\"ubc.rise.mosicIntegration.pushRISeDateToMosaic() error: " +
                       sb.ToString() + "\"}";
                ;
            }
            catch (Exception e)
            {
                return "{\"success\":\"false\",\"message\":\"ubc.rise.mosicIntegration.pushRISeDateToMosaic() error: " +
                       e + "\"}";
            }
        }

        private string HandleProtocolApproval(RISeProtocol riseProtocol, MosaicApiAccessImplementation myTask,
            Dictionary<string, List<string>> speciesMappingObj)
        {
            try
            {
                if (Debug)
                    WOM.Log("ubc.rise.mosicIntegration.pushRISeDataToeMosaic(), start HandleProtocolApproval()");


                string piKey = PersonKeyLookupMethod(riseProtocol.prot_pi.FirstOrDefault()?.pi_cwl, myTask);
                if (string.IsNullOrEmpty(piKey))
                {
                    piKey = CreateMosaicPerson(myTask, riseProtocol.prot_pi.FirstOrDefault()) ??
                            throw new MosaicValidationException(null, "Pi Can not be identified or Created");
                }

                string masterPainLevel = PainLevelLookupS(riseProtocol, myTask);


                string purpose = PurposeLookupMethod(riseProtocol, myTask);

                string createListPersonRet = CreateListPerson(myTask, riseProtocol.prot_persons);
                string masterGuidRet = CreateMasterProtocolMethod(piKey, masterPainLevel,
                    riseProtocol, myTask);

                if (masterGuidRet == null)
                {
                    throw new MosaicValidationException(null,
                        "Master Protocol creation does not return master protocol key");
                }

                string ret = CreateAllocationsMethod(riseProtocol, speciesMappingObj, myTask, masterGuidRet, piKey,
                    purpose);

                string returnVal = JsonConvert.SerializeObject(MosaicReturnedEntity);
                WOM.Log(
                    $"ubc.rise.mosicIntegration.pushRISeDataToeMosaic(), end HandleProtocolApproval() return {returnVal}");
                return returnVal;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private string HandleProtocolApprovalHadConditionalApproval(RISeProtocol riseProtocol,
            MosaicApiAccessImplementation myTask,
            Dictionary<string, List<string>> speciesMappingObj)
        {
            try
            {
                if (Debug)
                    WOM.Log("ubc.rise.mosicIntegration.pushRISeDataToeMosaic(), start HandleProtocolApproval()");


                string piKey = PersonKeyLookupMethod(riseProtocol.prot_pi.FirstOrDefault()?.pi_cwl, myTask);
                if (string.IsNullOrEmpty(piKey))
                {
                    piKey = CreateMosaicPerson(myTask, riseProtocol.prot_pi.FirstOrDefault()) ??
                            throw new MosaicValidationException(null, "Pi Can not be identified or Created");
                }

                string masterPainLevel = PainLevelLookupS(riseProtocol, myTask);


                string purpose = PurposeLookupMethod(riseProtocol, myTask);

                string createListPersonRet = CreateListPerson(myTask, riseProtocol.prot_persons);
                string masterGuidRet = AmendMasterProtocolMethod(piKey, masterPainLevel,
                    riseProtocol, myTask);

                if (masterGuidRet == null)
                {
                    throw new MosaicValidationException(null,
                        "Master Protocol creation does not return master protocol key");
                }

                string ret = AmendAllocationsMethod(riseProtocol, speciesMappingObj, myTask, masterGuidRet, piKey,
                    purpose);

                string returnVal = JsonConvert.SerializeObject(MosaicReturnedEntity);
                WOM.Log(
                    $"ubc.rise.mosicIntegration.pushRISeDataToeMosaic(), end HandleProtocolApproval() return {returnVal}");
                return returnVal;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private string HandleProtocolSuspention(RISeProtocol riseProtocol, MosaicApiAccessImplementation myTask,
            Dictionary<string, List<string>> speciesMappingObj)
        {
            try
            {
                if (Debug)
                    WOM.Log("ubc.rise.mosicIntegration.pushRISeDataToeMosaic(), start HandleProtocolSuspention()");

                var masterGuid = UpdateMasterProtocolExpirationDate(riseProtocol, myTask);

                string updateAllocationRet = UpdateAllocationsExpirationDate(riseProtocol, myTask, masterGuid);
                if (Debug)
                    WOM.Log(
                        "ubc.rise.mosicIntegration.pushRISeDataToeMosaic(), end CreateMasterProtocolMethod() return " +
                        masterGuid);


                string returnVal = JsonConvert.SerializeObject(MosaicReturnedEntity);

                if (Debug)
                    WOM.Log(
                        $"ubc.rise.mosicIntegration.pushRISeDataToeMosaic(), end HandleProtocolSuspention() return {returnVal}");
                return returnVal;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private string UpdateMasterProtocolExpirationDate(RISeProtocol riseProtocol,
            MosaicApiAccessImplementation myTask)
        {
            if (Debug)
                WOM.Log(
                    $"ubc.rise.mosicIntegration.pushRISeDataToeMosaic(), start UpdateMasterProtocolExpirationDate()");

            string getMasterProtocolmethod = "customer/VivAnimalUseMaster/GetByAlternateKey";
            string getMasterProtocolkey = riseProtocol.prot_no;
            string returnedMasterProtocol =
                HttpClientSyncCall.RunSync(
                    new Func<Task<string>>(async () =>
                        await myTask.GetResultsByAlternateKey(getMasterProtocolkey, getMasterProtocolmethod)));
            if (returnedMasterProtocol == null || returnedMasterProtocol == "[]")
            {
                throw new MosaicValidationException(null, $"Can not find master protocol {getMasterProtocolkey}");
            }

            List<VIV_ANIMAL_USE_MASTER_Dto> returnedMasterProtocolObj =
                JsonConvert.DeserializeObject<List<VIV_ANIMAL_USE_MASTER_Dto>>(returnedMasterProtocol);

            if (returnedMasterProtocolObj == null || returnedMasterProtocolObj.Count == 0)
            {
                throw new MosaicValidationException(null, $"Can not find master protocol {getMasterProtocolkey}");
            }

            returnedMasterProtocolObj.FirstOrDefault().VIVAUM_EXPIRATION_DATE = riseProtocol.prot_expire;

            string masterGuid = returnedMasterProtocolObj.FirstOrDefault()?.VIVAUM_GUID;

            string jsonStr = JsonConvert.SerializeObject(returnedMasterProtocolObj);

            string updateMasterProtocolRet = HttpClientSyncCall.RunSync(
                new Func<Task<string>>(async () =>
                    await myTask.CreateMosaicEntity("customer/VivAnimalUseMaster/UpdateEntity", jsonStr)));
            List<string> updateRet = JsonConvert.DeserializeObject<List<string>>(updateMasterProtocolRet);

            MosaicReturnedEntity.MasterProtocolCreatedOrUpdated.AddRange(returnedMasterProtocolObj);
            string retStr = JsonConvert.SerializeObject(returnedMasterProtocolObj);
            if (Debug)
                WOM.Log(
                    $"ubc.rise.mosicIntegration.pushRISeDataToeMosaic(), end UpdateMasterProtocolExpirationDate() return {retStr}");

            return masterGuid;
        }

        private string UpdateAllocationsExpirationDate(RISeProtocol riseProtocol,
            MosaicApiAccessImplementation myTask,
            string masterGuid)
        {
            try
            {
                string retStr = string.Empty;

                if (Debug)
                    WOM.Log(
                        "ubc.rise.mosicIntegration.pushRISeDataToeMosaic(), start UpdateAllocationsExpirationDate()");


                string method = "customer/VivAnimalUseProtocol/GetByVivAnimalUseMasterId";
                string key = masterGuid;
                string retAllocation =
                    HttpClientSyncCall.RunSync(new Func<Task<string>>(async () =>
                        await myTask.GetEntityByID(key, method)));

                List<UpdateAllocationsDic> allocationResultDict =
                    JsonConvert.DeserializeObject<List<UpdateAllocationsDic>>(retAllocation);

                if (allocationResultDict == null || allocationResultDict.Count == 0)
                {
                    if (Debug)
                        WOM.Log("ubc.rise.mosicIntegration.pushRISeDataToeMosaic(), no allocation found exist for" +
                                riseProtocol.prot_no);
                    return retStr;
                }

                List<VIV_ANIMAL_USE_PROTOCOL_Dto> allocationResultObj = null;
                foreach (var pair in allocationResultDict)
                {
                    if (pair.Key == key)
                    {
                        allocationResultObj = pair.Value;
                    }
                }

                if (allocationResultObj == null || allocationResultObj.Count == 0)
                {
                    if (Debug)
                        WOM.Log("ubc.rise.mosicIntegration.pushRISeDataToeMosaic(), no allocation found exist for" +
                                riseProtocol.prot_no);
                    return retStr;
                }

                foreach (var allocationDto in allocationResultObj)
                {
                    allocationDto.VIVAUP_EXPIRATION_DATE = riseProtocol.prot_expire;
                }

                string jsonStr = JsonConvert.SerializeObject(allocationResultObj);
                string updateAllocationRet = HttpClientSyncCall.RunSync(new Func<Task<string>>(async () =>
                    await myTask.CreateMosaicEntity("customer/VivAnimalUseProtocol/UpdateEntity", jsonStr)));

                MosaicReturnedEntity.AllocationCreatedOrUpdated.AddRange(allocationResultObj);
                retStr = JsonConvert.SerializeObject(allocationResultObj);
                if (Debug)
                    WOM.Log(
                        $"ubc.rise.mosicIntegration.pushRISeDataToeMosaic(), end UpdateAllocationsExpirationDate() return {retStr}");


                return retStr;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private string CreateAllocationsMethod(RISeProtocol riseProtocol,
            Dictionary<string, List<string>> speciesMappingObj,
            MosaicApiAccessImplementation myTask,
            string masterGuid,
            string piKey,
            string purpose)
        {
            try
            {
                string createAllocationRet = string.Empty;

                if (Debug)
                    WOM.Log("ubc.rise.mosicIntegration.pushRISeDataToeMosaic(), start CreateAllocationsMethod()");


                List<RISeProtocolSpecies> species = riseProtocol.prot_species;
                List<VIV_ANIMAL_USE_PROTOCOL_Dto> allocations = new List<VIV_ANIMAL_USE_PROTOCOL_Dto>();

                string mosaicSpecie = string.Empty;

                Dictionary<string, List<RISeProtocolSpecies>> groupedSpecies = species.GroupBy(x => x.sp_nm)
                    .OrderBy(p => p.Key.ToString())
                    .ToDictionary(x => x.Key, x => x.ToList());
                string defaultAllocation = HttpClientSyncCall.RunSync(new Func<Task<string>>(async () =>
                    await myTask.GetDefaultEntityForMosaic("customer/VivAnimalUseProtocol/GetDefaultEntity",
                        groupedSpecies.Count)));

                List<VIV_ANIMAL_USE_PROTOCOL_Dto> defaultAllocationList =
                    JsonConvert.DeserializeObject<List<VIV_ANIMAL_USE_PROTOCOL_Dto>>(defaultAllocation);

                if (defaultAllocationList == null || defaultAllocationList.Count != groupedSpecies.Count)
                {
                    throw new MosaicValidationException(null, "Return Default Allocation error");
                }

                int i = 0;
                foreach (string sepeciesItem in groupedSpecies.Keys)
                {
                    VIV_ANIMAL_USE_PROTOCOL_Dto currentDefaultAllocation = defaultAllocationList[i++];

                    List<RISeProtocolSpecies> speciesList = groupedSpecies[sepeciesItem];
                    decimal totalApprovedAnimalUse = speciesList.Sum(x => Convert.ToDecimal(x.sp_num));
                    string highestPainLevel = speciesList.OrderByDescending(x => x.sp_pain_level).FirstOrDefault()
                        ?.sp_pain_level;
                    RISeProtocolSpecies specie = speciesList.FirstOrDefault();


                    string lookupList = MosaicLookupLists.SpeciesLookup;
                    string riseSpecie = specie?.sp_nm;
                    mosaicSpecie = riseSpecie;
                    string allocationName = string.Empty;
                    if (speciesMappingObj.ContainsKey(riseSpecie ??
                                                      throw new MosaicValidationException(null,
                                                          "Specie Pushed from RISe is null")))
                    {
                        mosaicSpecie = speciesMappingObj[riseSpecie][0];
                        string allocationShortName = speciesMappingObj[riseSpecie][1];
                        allocationName = riseProtocol.prot_no + "-" + allocationShortName + "-01";
                    }
                    else
                    {
                        //todo

                        allocationName = riseProtocol.prot_no + "-" +
                                         (!String.IsNullOrWhiteSpace(mosaicSpecie) && mosaicSpecie.Length >= 3
                                             ? mosaicSpecie.Substring(0, 3)
                                             : mosaicSpecie) + "-01";
                    }

                    string specieLookupRet = HttpClientSyncCall.RunSync(new Func<Task<string>>(async () =>
                        await myTask.GetCorLookupValueByID(mosaicSpecie, lookupList)));
                    if (string.IsNullOrEmpty(specieLookupRet))
                    {
                        if (Debug)
                            WOM.Log("ubc.rise.mosicIntegration.pushRISeDataToeMosaic(), species " + mosaicSpecie +
                                    "does not exist.");
                        string createSpeciesRet = CreateMosaicSpecies(myTask, riseSpecie);
                        if (string.IsNullOrEmpty(createSpeciesRet))
                        {
                            throw new MosaicValidationException(null, "Species for allocation creation is null");
                        }

                        specieLookupRet = createSpeciesRet;
                    }

                    lookupList = MosaicLookupLists.PainLevelLookup;

                    string painLevelLookupRet = HttpClientSyncCall.RunSync(new Func<Task<string>>(async () =>
                        await myTask.GetCorLookupValueByID(highestPainLevel, lookupList)));

                    //string allocationGuid = Guid.NewGuid().ToString().Replace("-", string.Empty);

                    currentDefaultAllocation.VIVAUP_VIVAUM_GUID = masterGuid;
                    currentDefaultAllocation.VIVAUP_INVESTIGATOR_PERS_GUID = piKey;
                    currentDefaultAllocation.VIVAUP_SPECIES_LOOKV_GUID = specieLookupRet;
                    currentDefaultAllocation.VIVAUP_SUFFER_LOOKV_GUID = painLevelLookupRet;
                    currentDefaultAllocation.VIVAUP_PURPOSE_LOOKV_GUID = purpose;
                    currentDefaultAllocation.VIVAUP_NAME = allocationName;
                    currentDefaultAllocation.VIVAUP_DESCRIPTION = riseProtocol.prot_title;
                    currentDefaultAllocation.VIVAUP_ISSUED_DATE = riseProtocol.prot_start;
                    currentDefaultAllocation.VIVAUP_EXPIRATION_DATE = riseProtocol.prot_expire;
                    currentDefaultAllocation.VIVAUP_ALLOCATION_COUNT = totalApprovedAnimalUse;
                    //Check if allocaion already exist
                    string method = "customer/VivAnimalUseProtocol/GetByAlternateKey";
                    string key = allocationName;
                    string checkAllocation =
                        HttpClientSyncCall.RunSync(new Func<Task<string>>(async () =>
                            await myTask.GetResultsByAlternateKey(key, method)));

                    List<VIV_ANIMAL_USE_PROTOCOL_Dto> checkedAllocationResultObj =
                        JsonConvert.DeserializeObject<List<VIV_ANIMAL_USE_PROTOCOL_Dto>>(checkAllocation);
                    if (checkedAllocationResultObj == null || checkedAllocationResultObj.Count == 0)
                    {
                        allocations.Add(currentDefaultAllocation);
                    }
                    else
                    {
                        if (Debug)
                            WOM.Log(
                                "ubc.rise.mosicIntegration.pushRISeDataToeMosaic(), allocation already exist " + key);
                    }
                }

                if (allocations.Count > 0)
                {
                    string jsonStr = JsonConvert.SerializeObject(allocations);
                    createAllocationRet = HttpClientSyncCall.RunSync(new Func<Task<string>>(async () =>
                        await myTask.CreateMosaicEntity("customer/VivAnimalUseProtocol/CreateEntity", jsonStr)));
                }

                List<string> retObj = JsonConvert.DeserializeObject<List<string>>(createAllocationRet);


                List<VIV_ANIMAL_USE_PROTOCOL_Dto> retList = allocations.FindAll(x => retObj.Contains(x.VIVAUP_GUID));
                MosaicReturnedEntity.AllocationCreatedOrUpdated.AddRange(retList);
                string retStr = JsonConvert.SerializeObject(retList);
                if (Debug)
                    WOM.Log(
                        $"ubc.rise.mosicIntegration.pushRISeDataToeMosaic(), end CreateAllocationsMethod() return {retStr}");


                return retStr;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private string AmendAllocationsMethod(RISeProtocol riseProtocol,
            Dictionary<string, List<string>> speciesMappingObj,
            MosaicApiAccessImplementation myTask,
            string masterGuid,
            string piKey,
            string purpose)
        {
            try
            {
                string retStr = string.Empty;

                if (Debug)
                    WOM.Log(
                        "ubc.rise.mosicIntegration.pushRISeDataToeMosaic(), start AmendAllocations()");

                List<RISeProtocolSpecies> species = riseProtocol.prot_species;
                List<VIV_ANIMAL_USE_PROTOCOL_Dto> updateAllocations = new List<VIV_ANIMAL_USE_PROTOCOL_Dto>();
                List<VIV_ANIMAL_USE_PROTOCOL_Dto> createAllocations = new List<VIV_ANIMAL_USE_PROTOCOL_Dto>();

                string mosaicSpecie = string.Empty;

                Dictionary<string, List<RISeProtocolSpecies>> groupedSpecies = species.GroupBy(x => x.sp_nm)
                    .OrderBy(p => p.Key.ToString())
                    .ToDictionary(x => x.Key, x => x.ToList());

                string defaultAllocation = HttpClientSyncCall.RunSync(new Func<Task<string>>(async () =>
                    await myTask.GetDefaultEntityForMosaic("customer/VivAnimalUseProtocol/GetDefaultEntity",
                        groupedSpecies.Count)));

                List<VIV_ANIMAL_USE_PROTOCOL_Dto> defaultAllocationList =
                    JsonConvert.DeserializeObject<List<VIV_ANIMAL_USE_PROTOCOL_Dto>>(defaultAllocation);

                if (defaultAllocationList == null || defaultAllocationList.Count != groupedSpecies.Count)
                {
                    throw new MosaicValidationException(null, "Return Default Allocation error");
                }

                string method = "customer/VivAnimalUseProtocol/GetByVivAnimalUseMasterId";
                string key = masterGuid;
                string retAllocation =
                    HttpClientSyncCall.RunSync(new Func<Task<string>>(async () =>
                        await myTask.GetEntityByID(key, method)));

                List<UpdateAllocationsDic> allocationResultDict =
                    JsonConvert.DeserializeObject<List<UpdateAllocationsDic>>(retAllocation);


                List<VIV_ANIMAL_USE_PROTOCOL_Dto> allocationResultObj = null;
                if (allocationResultDict != null)
                    foreach (var pair in allocationResultDict)
                    {
                        if (pair.Key == key)
                        {
                            allocationResultObj = pair.Value;
                        }
                    }

                if (allocationResultObj == null)
                {
                    if (Debug)
                        WOM.Log("ubc.rise.mosicIntegration.pushRISeDataToeMosaic(), no allocation found exist for" +
                                riseProtocol.prot_no);
                    allocationResultObj = new List<VIV_ANIMAL_USE_PROTOCOL_Dto>();
                }

                int i = 0;
                foreach (string sepeciesItem in groupedSpecies.Keys)
                {
                    List<RISeProtocolSpecies> speciesList = groupedSpecies[sepeciesItem];
                    decimal totalApprovedAnimalUse = speciesList.Sum(x => Convert.ToDecimal(x.sp_num));
                    string highestPainLevel = speciesList.OrderByDescending(x => x.sp_pain_level).FirstOrDefault()
                        ?.sp_pain_level;
                    RISeProtocolSpecies specie = speciesList.FirstOrDefault();


                    string lookupList = MosaicLookupLists.SpeciesLookup;
                    string riseSpecie = specie?.sp_nm;
                    mosaicSpecie = riseSpecie;
                    string allocationName = string.Empty;
                    if (speciesMappingObj.ContainsKey(riseSpecie ??
                                                      throw new MosaicValidationException(null,
                                                          "Specie Pushed from RISe is null")))
                    {
                        mosaicSpecie = speciesMappingObj[riseSpecie][0];
                        string allocationShortName = speciesMappingObj[riseSpecie][1];
                        allocationName = riseProtocol.prot_no + "-" + allocationShortName + "-01";
                    }
                    else
                    {
                        //todo

                        allocationName = riseProtocol.prot_no + "-" +
                                         (!String.IsNullOrWhiteSpace(mosaicSpecie) && mosaicSpecie.Length >= 3
                                             ? mosaicSpecie.Substring(0, 3)
                                             : mosaicSpecie) + "-01";
                    }

                    string specieLookupRet = HttpClientSyncCall.RunSync(new Func<Task<string>>(async () =>
                        await myTask.GetCorLookupValueByID(mosaicSpecie, lookupList)));
                    if (string.IsNullOrEmpty(specieLookupRet))
                    {
                        if (Debug)
                            WOM.Log("ubc.rise.mosicIntegration.pushRISeDataToeMosaic(), species " + mosaicSpecie +
                                    "does not exist.");
                        string createSpeciesRet = CreateMosaicSpecies(myTask, riseSpecie);
                        if (string.IsNullOrEmpty(createSpeciesRet))
                        {
                            throw new MosaicValidationException(null, "Species for allocation creation is null");
                        }

                        specieLookupRet = createSpeciesRet;
                    }

                    lookupList = MosaicLookupLists.PainLevelLookup;

                    string painLevelLookupRet = HttpClientSyncCall.RunSync(new Func<Task<string>>(async () =>
                        await myTask.GetCorLookupValueByID(highestPainLevel, lookupList)));


                    //Check if allocaion already exist
                    VIV_ANIMAL_USE_PROTOCOL_Dto foundAllocation =
                        allocationResultObj.FirstOrDefault(x => x.VIVAUP_SPECIES_LOOKV_GUID == specieLookupRet);


                    if (foundAllocation != null)
                    {
                        allocationResultObj.Remove(foundAllocation);
                        foundAllocation.VIVAUP_INVESTIGATOR_PERS_GUID = piKey;
                        //foundAllocation.VIVAUP_SPECIES_LOOKV_GUID = specieLookupRet;
                        foundAllocation.VIVAUP_SUFFER_LOOKV_GUID = painLevelLookupRet;
                        foundAllocation.VIVAUP_PURPOSE_LOOKV_GUID = purpose;
                        foundAllocation.VIVAUP_DESCRIPTION = riseProtocol.prot_title;
                        foundAllocation.VIVAUP_ISSUED_DATE = riseProtocol.prot_start;
                        foundAllocation.VIVAUP_EXPIRATION_DATE = riseProtocol.prot_expire;
                        foundAllocation.VIVAUP_ALLOCATION_COUNT = totalApprovedAnimalUse;

                        updateAllocations.Add(foundAllocation);
                    }
                    else
                    {
                        if (Debug)
                            WOM.Log(
                                "ubc.rise.mosicIntegration.pushRISeDataToeMosaic(), allocation does not exist ");
                        VIV_ANIMAL_USE_PROTOCOL_Dto currentDefaultAllocation = defaultAllocationList[i++];
                        currentDefaultAllocation.VIVAUP_VIVAUM_GUID = masterGuid;
                        currentDefaultAllocation.VIVAUP_INVESTIGATOR_PERS_GUID = piKey;
                        currentDefaultAllocation.VIVAUP_SPECIES_LOOKV_GUID = specieLookupRet;
                        currentDefaultAllocation.VIVAUP_SUFFER_LOOKV_GUID = painLevelLookupRet;
                        currentDefaultAllocation.VIVAUP_PURPOSE_LOOKV_GUID = purpose;
                        currentDefaultAllocation.VIVAUP_NAME = allocationName;
                        currentDefaultAllocation.VIVAUP_DESCRIPTION = riseProtocol.prot_title;
                        currentDefaultAllocation.VIVAUP_ISSUED_DATE = riseProtocol.prot_start;
                        currentDefaultAllocation.VIVAUP_EXPIRATION_DATE = riseProtocol.prot_expire;
                        currentDefaultAllocation.VIVAUP_ALLOCATION_COUNT = totalApprovedAnimalUse;
                        createAllocations.Add(currentDefaultAllocation);
                    }
                }

                DateTime dt = DateTime.Now;
                string today = dt.ToString("yyyy-MM-dd'T00:00:00'");
                foreach (var item in allocationResultObj)
                {
                    item.VIVAUP_EXPIRATION_DATE = today;
                    item.VIVAUP_ALLOCATION_COUNT = Convert.ToDecimal(0);
                }

                updateAllocations.AddRange(allocationResultObj);

                if (updateAllocations != null && updateAllocations.Count > 0)
                {
                    string jsonStr = JsonConvert.SerializeObject(updateAllocations);
                    string updateAllocationRet = HttpClientSyncCall.RunSync(new Func<Task<string>>(async () =>
                        await myTask.CreateMosaicEntity("customer/VivAnimalUseProtocol/UpdateEntity", jsonStr)));
                }

                if (createAllocations != null && createAllocations.Count > 0)
                {
                    string jsonStr = JsonConvert.SerializeObject(createAllocations);
                    string createAllocationRet = HttpClientSyncCall.RunSync(new Func<Task<string>>(async () =>
                        await myTask.CreateMosaicEntity("customer/VivAnimalUseProtocol/CreateEntity", jsonStr)));
                }


                var finalAllocationRet = HttpClientSyncCall.RunSync(new Func<Task<string>>(async () =>
                    await myTask.GetEntityByID(key, method)));

                List<UpdateAllocationsDic> finalAllocationsResultsDict =
                    JsonConvert.DeserializeObject<List<UpdateAllocationsDic>>(finalAllocationRet);

                List<VIV_ANIMAL_USE_PROTOCOL_Dto> finalAllocationResultObj = null;
                foreach (var pair in finalAllocationsResultsDict)
                {
                    if (pair.Key == key)
                    {
                        finalAllocationResultObj = pair.Value;
                    }
                }

                MosaicReturnedEntity.AllocationCreatedOrUpdated.AddRange(finalAllocationResultObj);
                retStr = JsonConvert.SerializeObject(finalAllocationResultObj);

                if (Debug)
                    WOM.Log(
                        $"ubc.rise.mosicIntegration.pushRISeDataToeMosaic(), end AmendAllocations() return {retStr}");


                return retStr;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private string CreateMasterProtocolMethod(string piKey, string masterPainLevel,
            RISeProtocol riseProtocol,
            MosaicApiAccessImplementation myTask)
        {
            try
            {
                if (Debug)
                    WOM.Log("ubc.rise.mosicIntegration.pushRISeDataToeMosaic(), start CreateMasterProtocolMethod()");


                string defaultMasterProtocolJson = HttpClientSyncCall.RunSync(new Func<Task<string>>(async () =>
                    await myTask.GetDefaultEntityForMosaic("customer/VivAnimalUseMaster/GetDefaultEntity", 1)));

                List<VIV_ANIMAL_USE_MASTER_Dto> defaultMasterProtocolList =
                    JsonConvert.DeserializeObject<List<VIV_ANIMAL_USE_MASTER_Dto>>(defaultMasterProtocolJson);


                VIV_ANIMAL_USE_MASTER_Dto defaultMasterProtocol = defaultMasterProtocolList.FirstOrDefault();
                defaultMasterProtocol.VIVAUM_ADMIN_PERS_GUID = piKey;
                defaultMasterProtocol.VIVAUM_SUFFER_LOOKV_GUID = masterPainLevel;

                defaultMasterProtocol.VIVAUM_NAME = riseProtocol.prot_no;
                defaultMasterProtocol.VIVAUM_DESCRIPTION = riseProtocol.prot_title;
                defaultMasterProtocol.VIVAUM_RESEARCH_NOTE = riseProtocol.prot_keywords;
                defaultMasterProtocol.VIVAUM_IDENTIFIER = riseProtocol.prot_type;
                defaultMasterProtocol.VIVAUM_ISSUED_DATE = riseProtocol.prot_start;
                defaultMasterProtocol.VIVAUM_EXPIRATION_DATE = riseProtocol.prot_expire;
                defaultMasterProtocol.VIVAUM_ALLOCATION_COUNT = Convert.ToDecimal(riseProtocol.prot_total_num);
                defaultMasterProtocol.VIVAUM_EXTERNAL_URL = riseProtocol.prot_url;
                string masterGuid = defaultMasterProtocol.VIVAUM_GUID;

                string jsonStr = JsonConvert.SerializeObject(defaultMasterProtocolList);
                //Check to see if Master Protocol already there

                string getMasterProtocolmethod = "customer/VivAnimalUseMaster/GetByAlternateKey";
                string getMasterProtocolkey = riseProtocol.prot_no;
                string checkMasterProtocol =
                    HttpClientSyncCall.RunSync(
                        new Func<Task<string>>(async () =>
                            await myTask.GetResultsByAlternateKey(getMasterProtocolkey, getMasterProtocolmethod)));
                List<VIV_ANIMAL_USE_MASTER_Dto> checkedResultObj =
                    JsonConvert.DeserializeObject<List<VIV_ANIMAL_USE_MASTER_Dto>>(checkMasterProtocol);
                if (checkedResultObj == null || checkedResultObj.Count == 0)
                {
                    string createMasterProtocolRet = HttpClientSyncCall.RunSync(
                        new Func<Task<string>>(async () =>
                            await myTask.CreateMosaicEntity("customer/VivAnimalUseMaster/CreateEntity", jsonStr)));
                    List<string> createRet = JsonConvert.DeserializeObject<List<string>>(createMasterProtocolRet);
                    List<VIV_ANIMAL_USE_MASTER_Dto> retList =
                        defaultMasterProtocolList.FindAll(x => createRet.Contains(x.VIVAUM_GUID));
                    MosaicReturnedEntity.MasterProtocolCreatedOrUpdated.AddRange(retList);
                    masterGuid = createRet?.FirstOrDefault();
                }
                else
                {
                    masterGuid = checkedResultObj.FirstOrDefault()?.VIVAUM_GUID;
                    if (Debug)
                        WOM.Log(
                            "ubc.rise.mosicIntegration.pushRISeDataToeMosaic(), Master Protocol Already Exist: return " +
                            masterGuid);
                }

                if (Debug)
                    WOM.Log(
                        "ubc.rise.mosicIntegration.pushRISeDataToeMosaic(), end CreateMasterProtocolMethod() return " +
                        masterGuid);


                return masterGuid;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private string AmendMasterProtocolMethod(string piKey, string masterPainLevel,
            RISeProtocol riseProtocol,
            MosaicApiAccessImplementation myTask)
        {
            try
            {
                if (Debug)
                    WOM.Log("ubc.rise.mosicIntegration.pushRISeDataToeMosaic(), start AmendMasterProtocolMethod()");
                string getMasterProtocolmethod = "customer/VivAnimalUseMaster/GetByAlternateKey";
                string getMasterProtocolkey = riseProtocol.prot_no;
                string returnedMasterProtocol =
                    HttpClientSyncCall.RunSync(
                        new Func<Task<string>>(async () =>
                            await myTask.GetResultsByAlternateKey(getMasterProtocolkey, getMasterProtocolmethod)));
                if (returnedMasterProtocol == null || returnedMasterProtocol == "[]")
                {
                    throw new MosaicValidationException(null, $"Can not find master protocol {getMasterProtocolkey}");
                }

                List<VIV_ANIMAL_USE_MASTER_Dto> returnedMasterProtocolObj =
                    JsonConvert.DeserializeObject<List<VIV_ANIMAL_USE_MASTER_Dto>>(returnedMasterProtocol);

                if (returnedMasterProtocolObj == null || returnedMasterProtocolObj.Count == 0)
                {
                    throw new MosaicValidationException(null, $"Can not find master protocol {getMasterProtocolkey}");
                }

                var returnedMasterProtocolEntity = returnedMasterProtocolObj.FirstOrDefault();
                returnedMasterProtocolEntity.VIVAUM_EXPIRATION_DATE = riseProtocol.prot_expire;
                returnedMasterProtocolEntity.VIVAUM_ADMIN_PERS_GUID = piKey;
                returnedMasterProtocolEntity.VIVAUM_SUFFER_LOOKV_GUID = masterPainLevel;

                //returnedMasterProtocolEntity.VIVAUM_NAME = riseProtocol.prot_no;
                returnedMasterProtocolEntity.VIVAUM_DESCRIPTION = riseProtocol.prot_title;
                returnedMasterProtocolEntity.VIVAUM_RESEARCH_NOTE = riseProtocol.prot_keywords;
                returnedMasterProtocolEntity.VIVAUM_IDENTIFIER = riseProtocol.prot_type;
                returnedMasterProtocolEntity.VIVAUM_ISSUED_DATE = riseProtocol.prot_start;
                returnedMasterProtocolEntity.VIVAUM_EXPIRATION_DATE = riseProtocol.prot_expire;
                returnedMasterProtocolEntity.VIVAUM_ALLOCATION_COUNT = Convert.ToDecimal(riseProtocol.prot_total_num);
                returnedMasterProtocolEntity.VIVAUM_EXTERNAL_URL = riseProtocol.prot_url;
                string masterGuid = returnedMasterProtocolEntity.VIVAUM_GUID;

                string jsonStr = JsonConvert.SerializeObject(returnedMasterProtocolObj);

                string updateMasterProtocolRet = HttpClientSyncCall.RunSync(
                    new Func<Task<string>>(async () =>
                        await myTask.CreateMosaicEntity("customer/VivAnimalUseMaster/UpdateEntity", jsonStr)));
                List<string> updateRet = JsonConvert.DeserializeObject<List<string>>(updateMasterProtocolRet);

                MosaicReturnedEntity.MasterProtocolCreatedOrUpdated.AddRange(returnedMasterProtocolObj);
                string retStr = JsonConvert.SerializeObject(returnedMasterProtocolObj);
                if (Debug)
                    WOM.Log(
                        $"ubc.rise.mosicIntegration.pushRISeDataToeMosaic(), end UpdateMasterProtocolExpirationDate() return {retStr}");

                return masterGuid;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private string PurposeLookupMethod(RISeProtocol riseProtocol, MosaicApiAccessImplementation myTask)
        {
            try
            {
                if (Debug) WOM.Log("ubc.rise.mosicIntegration.pushRISeDataToeMosaic(), start PurposeLookupMethod()");

                string lookupList = MosaicLookupLists.PurposeLookup;
                string risePurpose = riseProtocol.prot_purpose;
                string purpose =
                    HttpClientSyncCall.RunSync(new Func<Task<string>>(async () =>
                        await myTask.GetCorLookupValueByID(risePurpose, lookupList)));

                if (Debug)
                    WOM.Log(
                        "ubc.rise.mosicIntegration.pushRISeDataToeMosaic(), end PurposeLookupMethod() return " +
                        purpose);


                return purpose;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private string SpeciesLookupMethod(RISeProtocol riseProtocol,
            Dictionary<string, List<string>> speciesMappingObj,
            MosaicApiAccessImplementation myTask)
        {
            try
            {
                if (Debug) WOM.Log("ubc.rise.mosicIntegration.pushRISeDataToeMosaic(), start SpeciesLookupMethod()");

                string firstSpecie = riseProtocol.prot_species.FirstOrDefault()?.sp_nm;

                string mapedFirstSpecie = string.Empty;
                if (speciesMappingObj.ContainsKey(firstSpecie ?? throw new InvalidOperationException()))
                {
                    mapedFirstSpecie = speciesMappingObj[firstSpecie][0];
                }

                string lookupList = MosaicLookupLists.SpeciesLookup;
                string masterSpecie = HttpClientSyncCall.RunSync(new Func<Task<string>>(async () =>
                    await myTask.GetCorLookupValueByID(mapedFirstSpecie, lookupList)));

                if (Debug)
                    WOM.Log("ubc.rise.mosicIntegration.pushRISeDataToeMosaic(), end SpeciesLookupMethod() return " +
                            lookupList);

                return lookupList;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private string PainLevelLookupS(RISeProtocol riseProtocol, MosaicApiAccessImplementation myTask)
        {
            try
            {
                if (Debug) WOM.Log("ubc.rise.mosicIntegration.pushRISeDataToeMosaic(), start PainLevelLookupS()");

                string lookupListGuid = MosaicLookupLists.PainLevelLookup;
                string ccacCategory = riseProtocol.prot_ccaccategory;

                string masterPainLevel =
                    HttpClientSyncCall.RunSync(new Func<Task<string>>(async () =>
                        await myTask.GetCorLookupValueByID(ccacCategory, lookupListGuid)));

                if (Debug)
                    WOM.Log("ubc.rise.mosicIntegration.pushRISeDataToeMosaic(), end PainLevelLookupS() return " +
                            masterPainLevel);

                return masterPainLevel;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private string PersonKeyLookupMethod(string cwl, MosaicApiAccessImplementation myTask)
        {
            try
            {
                if (Debug) WOM.Log("ubc.rise.mosicIntegration.pushRISeDataToeMosaic(), start PersonKeyLookupMethod()");

                string method = "customer/CorPerson/GetByAlternateKey";
                string result =
                    HttpClientSyncCall.RunSync(
                        new Func<Task<string>>(async () => await myTask.GetResultsByAlternateKey(cwl, method)));
                List<COR_PERSON_RedactedDto> personObj =
                    JsonConvert.DeserializeObject<List<COR_PERSON_RedactedDto>>(result);
                if (personObj == null)
                {
                    throw new MosaicValidationException(null, "PersonKeyLookupMethod get null object");
                }

                string piKey = personObj.FirstOrDefault()?.PERS_GUID;

                if (Debug)
                    WOM.Log(
                        "ubc.rise.mosicIntegration.pushRISeDataToeMosaic(), end PersonKeyLookupMethod() return " +
                        piKey);

                return piKey;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private MosaicApiAccessImplementation MosaicApiAuthenticate(string userName, string userPassword,
            string configUrl)
        {
            try
            {
                if (Debug) WOM.Log("ubc.rise.mosicIntegration.pushRISeDataToeMosaic(), start MosaicApiAuthenticate()");

                AuthenticateRequest authRequest = new AuthenticateRequest()
                    {Username = userName, Password = userPassword};
                RequestConfigurations config = new RequestConfigurations()
                    {AuthenticateRequest = authRequest, ConfigurationUrl = configUrl};
                MosaicApiAccessImplementation myTask = new MosaicApiAccessImplementation(config);

                string authRet =
                    HttpClientSyncCall.RunSync(new Func<Task<string>>(async () => await myTask.Authenticate()));

                if (Debug) WOM.Log("ubc.rise.mosicIntegration.pushRISeDataToeMosaic(), end MosaicApiAuthenticate()");

                return myTask;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public string PullMosaicDataToRiSe(com.webridge.entity.EntityType context, string host, string methodCall,
            string jSonString)
        {
            try
            {
                string url = host + methodCall + jSonString;

                if (Debug) WOM.Log("ubc.rise.mosicIntegration.pullMosaicDataToRISe(), pass in URL: " + url);


                return string.Empty;
            }
            catch (MosaicValidationException msg)
            {
                if (msg.ExceptionMessage != null)
                {
                    WOM.Log("" + msg.ExceptionMessage.Message);
                    WOM.Log(msg.ExceptionMessage.Errors.FirstOrDefault().Description);
                }

                if (msg.OtherErrorMessage != null)
                {
                    WOM.Log(msg.OtherErrorMessage);
                }
            }
            catch (Exception e)
            {
                return "{\"success\":\"false\",\"message\":\"ubc.rise.mosicIntegration.pullMosaicDataToRISe() error: " +
                       e + "\"}";
            }

            return "";
        }

        public string CreateMosaicSpecies(MosaicApiAccessImplementation myTask, string speciesName)
        {
            try
            {
                if (Debug) WOM.Log("ubc.rise.mosicIntegration.pushRISeDataToeMosaic(), start CreateMosaicSpecies()");

                var defaultSpeciesEnityJson = HttpClientSyncCall.RunSync(new Func<Task<string>>(async () =>
                    await myTask.GetDefaultEntityForMosaic("customer/CorLookupValue/GetDefaultEntity", 1)));

                List<COR_LOOKUP_VALUE_RedactedDto> defaultSpeciesEnityJsonList =
                    JsonConvert.DeserializeObject<List<COR_LOOKUP_VALUE_RedactedDto>>(defaultSpeciesEnityJson);


                COR_LOOKUP_VALUE_RedactedDto defaultSpeciesEntity = defaultSpeciesEnityJsonList.FirstOrDefault();
                if (defaultSpeciesEntity == null)
                {
                    throw new MosaicValidationException(null, "Returened Default Species Entity is null");
                }

                defaultSpeciesEntity.LOOKV_LOOKL_GUID = MosaicLookupLists.SpeciesLookup;
                defaultSpeciesEntity.LOOKV_VALUE = speciesName;

                defaultSpeciesEntity.LOOKV_SHORT_VALUE = speciesName;


                string jsonStr = JsonConvert.SerializeObject(defaultSpeciesEnityJsonList);
                string createSpeciesMethod = "customer/CorLookupValue/CreateEntity";

                string createSpeciesRetList =
                    HttpClientSyncCall.RunSync(new Func<Task<string>>(async () =>
                        await myTask.CreateMosaicEntity(createSpeciesMethod, jsonStr)));
                List<string> createSpeciesRet = JsonConvert.DeserializeObject<List<string>>(createSpeciesRetList);
                List<COR_LOOKUP_VALUE_RedactedDto> retList =
                    defaultSpeciesEnityJsonList.FindAll(x => createSpeciesRetList.Contains(x.LOOKV_GUID));
                MosaicReturnedEntity.SpeciesCreated.AddRange(retList);
                string retVal = createSpeciesRet?.FirstOrDefault();

                if (Debug)
                    WOM.Log("ubc.rise.mosicIntegration.pushRISeDataToeMosaic(), end CreateMosaicSpecies() return " +
                            retVal);

                return retVal;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public string CreateMosaicPerson(MosaicApiAccessImplementation myTask, RISePerson person)
        {
            try
            {
                if (Debug) WOM.Log("ubc.rise.mosicIntegration.pushRISeDataToeMosaic(), start CreateMosaicPerson()");

                var defaultPersonEnityJson = HttpClientSyncCall.RunSync(new Func<Task<string>>(async () =>
                    await myTask.GetDefaultEntityForMosaic("customer/CorPerson/GetDefaultEntity", 1)));

                List<COR_PERSON_RedactedDto> defaultPersonEnityJsonList =
                    JsonConvert.DeserializeObject<List<COR_PERSON_RedactedDto>>(defaultPersonEnityJson);


                COR_PERSON_RedactedDto defaultPersonRedactedEntity = defaultPersonEnityJsonList.FirstOrDefault();
                if (defaultPersonRedactedEntity == null)
                {
                    throw new MosaicValidationException(null, "Returned Default Person Entity is null");
                }

                defaultPersonRedactedEntity.PERS_USERNAME = person.pi_cwl;
                defaultPersonRedactedEntity.PERS_EMAIL = person.pi_email;
                defaultPersonRedactedEntity.PERS_PHONE = person.pi_phone;
                defaultPersonRedactedEntity.PERS_FIRST_NAME = person.pi_firstname;
                defaultPersonRedactedEntity.PERS_LAST_NAME = person.pi_lastname;


                string jsonStr = JsonConvert.SerializeObject(defaultPersonEnityJsonList);
                string createPersonMethod = "customer/CorPerson/CreateEntity";

                string createPersonRetList =
                    HttpClientSyncCall.RunSync(new Func<Task<string>>(async () =>
                        await myTask.CreateMosaicEntity(createPersonMethod, jsonStr)));
                List<string> createPersonRet = JsonConvert.DeserializeObject<List<string>>(createPersonRetList);

                List<COR_PERSON_RedactedDto> retList =
                    defaultPersonEnityJsonList.FindAll(x => createPersonRet.Contains(x.PERS_GUID));
                MosaicReturnedEntity.PersonCreated.AddRange(retList);
                string retVal = createPersonRet?.FirstOrDefault();

                if (Debug)
                    WOM.Log("ubc.rise.mosicIntegration.pushRISeDataToeMosaic(), end CreateMosaicPerson() return " +
                            retVal);


                return retVal;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private List<COR_PERSON_RedactedDto> PersonListLookupMethod(List<RISeOtherPerson> persons,
            MosaicApiAccessImplementation myTask)
        {
            try
            {
                if (Debug) WOM.Log("ubc.rise.mosicIntegration.pushRISeDataToeMosaic(), start PersonListLookupMethod()");
                List<string> cwlList = new List<string>();
                foreach (var sePerson in persons)
                {
                    cwlList.Add(sePerson.per_cwl);
                }

                string cwlListJson = null;
                if (cwlList.Count > 0)
                {
                    cwlListJson = JsonConvert.SerializeObject(cwlList);
                }

                var method = "customer/CorPerson/GetByAlternateKey";
                if (cwlListJson == null)
                {
                    return null;
                }

                var result =
                    HttpClientSyncCall.RunSync(
                        new Func<Task<string>>(async () =>
                            await myTask.GetResultsByAlternateKeyList(cwlListJson, method)));
                var personObj = JsonConvert.DeserializeObject<List<COR_PERSON_RedactedDto>>(result);
                if (Debug)
                    WOM.Log(
                        $"ubc.rise.mosicIntegration.pushRISeDataToeMosaic(), end PersonListLookupMethod() return {result}");
                return personObj;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private string CreateListPerson(MosaicApiAccessImplementation myTask, List<RISeOtherPerson> persons)
        {
            try
            {
                if (Debug) WOM.Log("ubc.rise.mosicIntegration.pushRISeDataToeMosaic(), start CreateListPerson()");
                var defaultPersonEntityJson = HttpClientSyncCall.RunSync(new Func<Task<string>>(async () =>
                    await myTask.GetDefaultEntityForMosaic("customer/CorPerson/GetDefaultEntity", persons.Count)));
                var lookupResult = PersonListLookupMethod(persons, myTask);
                List<COR_PERSON_RedactedDto> defaultPersonEntityJsonList =
                    JsonConvert.DeserializeObject<List<COR_PERSON_RedactedDto>>(defaultPersonEntityJson);

                List<COR_PERSON_RedactedDto> personEntityJsonList = new List<COR_PERSON_RedactedDto>();

                for (int i = 0; i < persons.Count; i++)
                {
                    COR_PERSON_RedactedDto defaultPersonRedactedEntity = defaultPersonEntityJsonList[i];
                    var person = persons[i];
                    if (defaultPersonRedactedEntity == null)
                    {
                        throw new MosaicValidationException(null, "Returned Default Person Entity is null");
                    }

                    if (string.IsNullOrEmpty(person.per_cwl))
                    {
                        continue;
                    }

                    if (lookupResult != null && lookupResult.Select(x => x.PERS_USERNAME).Contains(person.per_cwl))
                    {
                        continue;
                    }

                    defaultPersonRedactedEntity.PERS_USERNAME = person.per_cwl;
                    defaultPersonRedactedEntity.PERS_EMAIL = person.per_email;
                    defaultPersonRedactedEntity.PERS_PHONE = person.per_phone;
                    defaultPersonRedactedEntity.PERS_FIRST_NAME = person.per_firstname;
                    defaultPersonRedactedEntity.PERS_LAST_NAME = person.per_lastname;

                    personEntityJsonList.Add(defaultPersonRedactedEntity);
                    i++;
                }

                if (personEntityJsonList.Count == 0)
                {
                    if (Debug)
                        WOM.Log(
                            $"ubc.rise.mosicIntegration.pushRISeDataToeMosaic(), end CreateListPerson() return empty list");
                    return "[]";
                }

                string jsonStr = JsonConvert.SerializeObject(personEntityJsonList);
                string createPersonMethod = "customer/CorPerson/CreateEntity";

                string createPersonRetList =
                    HttpClientSyncCall.RunSync(new Func<Task<string>>(async () =>
                        await myTask.CreateMosaicEntity(createPersonMethod, jsonStr)));

                List<string> retObj = JsonConvert.DeserializeObject<List<string>>(createPersonRetList);


                List<COR_PERSON_RedactedDto> retList = personEntityJsonList.FindAll(x => retObj.Contains(x.PERS_GUID));
                MosaicReturnedEntity.PersonCreated.AddRange(retList);
                string retStr = JsonConvert.SerializeObject(retList);
                if (Debug)
                    WOM.Log(
                        $"ubc.rise.mosicIntegration.pushRISeDataToeMosaic(), end CreateListPerson() return {retStr}");
                return retStr;
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}