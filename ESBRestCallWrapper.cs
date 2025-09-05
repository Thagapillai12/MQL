using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using System.Net;
using System.Configuration;
using System.IO;
using System.Text;
using System.ServiceModel;
using BT.SaaS.IspssAdapter;
using com.bt.util.logging;
using BT.SaaS.Core.MDMAPI.Entities.Product;
using SAASPE = BT.SaaS.Core.Shared.Entities;
using BT.ESB.RoBT.ManageCustomer;
using System.ServiceModel.Channels;
using BT.ESB.RoBT.ManageCustomerOrder;
using System.Globalization;
using System.Xml.Linq;
using BT.ESB.RoBT.MCCPV1;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using BT.Helpers;
using ClosedXML.Excel;
using System.Data;
using Newtonsoft.Json;

namespace BT.SaaS.MSEOAdapter
{
    public class ESBRestCallWrapper
    {
        const string StartedCMPSCall = "StartedCMPSCall";
        const string GotResponseFromCMPSWithBusinessError = "GotResponseFromCMPSWithBusinessError";
        const string GotResponseFromCMPS = "GotResponseFromCMPS";
        const string DownStreamError = "DownStreamError";

    

        public static BT.ESB.RoBT.GetServiceRoleRequests GetRoleRequestfromESB(string urlKey)
        {
            BT.ESB.RoBT.GetServiceRoleRequests getRoleRequest = new BT.ESB.RoBT.GetServiceRoleRequests();

            try
            {
                string responseXml;
                HttpWebRequest webrequest;

                webrequest = (HttpWebRequest)WebRequest.Create(urlKey);
                webrequest.Method = "GET";
                webrequest.Accept = "application/xml";
                webrequest.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["CMPSTimeOut"].ToString());

                using (HttpWebResponse webResponse = (HttpWebResponse)webrequest.GetResponse())
                {
                    using (StreamReader readStream = new StreamReader(webResponse.GetResponseStream()))
                    {
                        responseXml = readStream.ReadToEnd();
                        readStream.Close();
                        readStream.Dispose();
                    }
                    webResponse.Close();
                }
                using (StringReader reader = new StringReader(responseXml))
                {
                    XmlSerializer serailizer = new XmlSerializer(typeof(BT.ESB.RoBT.GetServiceRoleRequests));
                    getRoleRequest = (BT.ESB.RoBT.GetServiceRoleRequests)serailizer.Deserialize(reader);
                    reader.Close();
                    reader.Dispose();
                }

            }
            /*catch (WebException DnpEx)
            {
                if (DnpEx.Response != null)
                {
                    string statusCode = string.Empty;
                    string responseXml = string.Empty;
                    using (HttpWebResponse errorResponse = (HttpWebResponse)DnpEx.Response)
                    {
                        statusCode = errorResponse.StatusCode.ToString();
                        using (StreamReader readStream = new StreamReader(errorResponse.GetResponseStream()))
                        {
                            responseXml = readStream.ReadToEnd();
                            readStream.Close();
                            readStream.Dispose();
                        }
                        errorResponse.Close();                        
                    }
                    if (DnpEx.Message.Contains("404") || responseXml.ToLower().StartsWith("no data") || responseXml.ToLower().StartsWith("no records"))
                        return null;
                    else
                        throw new DnpException("DNPException: " + statusCode + " : " + responseXml);
                }
                else
                {
                    throw new DnpException("DNPException: " + DnpEx.Message);
                }
            }*/
            catch (WebException esbException)
            {

                throw esbException;
            }

            return getRoleRequest;
        }

        public static bool PostManageRoleRequest(string urlKey, ServiceRoleRequest roleRequest, ref EsbResponse esbResponse)
        {
            string responseXml = string.Empty;
            bool isPostCallSucess = false;
            Guid guid = Guid.NewGuid();
            try
            {
                HttpWebRequest webrequest;
                webrequest = (HttpWebRequest)WebRequest.Create(urlKey);
                webrequest.Method = "POST";
                webrequest.ContentType = "application/xml; charset=utf8";
                webrequest.Accept = "application/xml";
                webrequest.ContentLength = 0;
                webrequest.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["CMPSTimeOut"].ToString());

                XmlSerializer serailizer = new XmlSerializer(typeof(ServiceRoleRequest));
                MemoryStream memoryStream = new MemoryStream();
                StreamWriter streamWriter = new StreamWriter(memoryStream, Encoding.UTF8);
                if (roleRequest != null)
                {
                    serailizer.Serialize(streamWriter, roleRequest);
                    byte[] Response = memoryStream.ToArray();
                    string requestXML = System.Text.Encoding.UTF8.GetString(Response);
                    LogHelper.LogActivityDetails(urlKey, guid, "ServiceRoleRequest : ", TimeSpan.Zero, "ESBCallsTrace", requestXML);
                    webrequest.ContentLength = Response.Length;

                    using (Stream streamRequest = webrequest.GetRequestStream())
                    {
                        streamRequest.Write(Response, 0, Response.Length);
                        streamRequest.Close();
                        streamRequest.Dispose();
                    }
                }

                using (HttpWebResponse webResponse = (HttpWebResponse)webrequest.GetResponse())
                {
                    using (StreamReader readStream = new StreamReader(webResponse.GetResponseStream()))
                    {
                        LogHelper.LogActivityDetails(urlKey, guid, "ServiceRoleResponse : ", TimeSpan.Zero, "ESBCallsTrace", readStream);
                        responseXml = readStream.ReadToEnd();
                        readStream.Close();
                        readStream.Dispose();
                    }
                    webResponse.Close();
                }

                using (StringReader reader = new StringReader(responseXml))
                {
                    XmlSerializer serailizerObject = new XmlSerializer(typeof(EsbResponse));
                    esbResponse = (EsbResponse)serailizerObject.Deserialize(reader);
                    reader.Close();
                    reader.Dispose();
                }
                isPostCallSucess = true;

            }
            /*catch(WebException ESBEx)
            {
                if (ESBEx.Response!=null)
                {
                    string statusCode = string.Empty;
                    string responseCode = string.Empty;
                    using (HttpWebResponse errorResponse = (HttpWebResponse)ESBEx.Response)
                    {
                        statusCode = errorResponse.StatusCode.ToString();
                        using (StreamReader readStream = new StreamReader(errorResponse.GetResponseStream()))
                        {
                            responseCode = readStream.ReadToEnd();
                            readStream.Close();
                            readStream.Dispose();
                        }
                        errorResponse.Close();
                    }

                    throw statusCode;
                }
                else
                {
                    throw ESBEx;
                }
            }*/
            catch (WebException esbException)
            {
                LogHelper.LogActivityDetails(urlKey, guid, "Error in ServiceRoleResponse : ", TimeSpan.Zero, "ESBCallsTrace", esbException.Message.ToString());
                throw esbException;
            }
            return isPostCallSucess;
        }

        public static QueryCustomerResponse GetQueryCustomer(OrderItem orderItem)
        {
            string deviceID = string.Empty;
            if (orderItem.Instance[0].InstanceCharacteristic.Count() > 0)
            {
                if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(insChar => insChar.Name.Equals("DeviceID", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(insChar.Value)))
                {
                    deviceID = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(insChar => insChar.Name.Equals("DeviceID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                }
            }

            QueryCustomerResponse queryCustomerResp = new QueryCustomerResponse();

            //queryCustomerRequest1 queryCustRequest = new queryCustomerRequest1();
            QueryCustomerRequest queryCustRequest = new QueryCustomerRequest();
            queryCustRequest.queryCustomerRequest = new QueryCustomerReq();

            // Standard header block
            queryCustRequest.standardHeader = new BT.ESB.RoBT.ManageCustomer.StandardHeaderBlock();
            queryCustRequest.standardHeader.e2e = new BT.ESB.RoBT.ManageCustomer.E2E();

            queryCustRequest.standardHeader.e2e.E2EDATA = ConfigurationManager.AppSettings["E2EData"].ToString();

            queryCustRequest.standardHeader.serviceAddressing = new BT.ESB.RoBT.ManageCustomer.ServiceAddressing();
            queryCustRequest.standardHeader.serviceAddressing.serviceName = @"http://capabilities.nat.bt.com/ManageCustomer";
            queryCustRequest.standardHeader.serviceAddressing.action = @"http://capabilities.nat.bt.com/wsdl/ManageCustomer#queryCustomerRequest";
            queryCustRequest.standardHeader.serviceAddressing.messageId = Guid.NewGuid().ToString();
            queryCustRequest.standardHeader.serviceAddressing.from = @"VasFulfilment"; // todo: temporary one - need to check with ESB and to change this
            queryCustRequest.standardHeader.serviceAddressing.to = new BT.ESB.RoBT.ManageCustomer.AddressReference();
            queryCustRequest.standardHeader.serviceAddressing.to.address = @"http://cmpal.bt.com";

            queryCustRequest.standardHeader.serviceState = new BT.ESB.RoBT.ManageCustomer.ServiceState();
            queryCustRequest.standardHeader.serviceState.stateCode = "OK";


            queryCustRequest.queryCustomerRequest = new QueryCustomerReq();
            queryCustRequest.queryCustomerRequest.customerAccount = new BT.ESB.RoBT.ManageCustomer.CustomerAccount1();
            queryCustRequest.queryCustomerRequest.customerAccount.productInstance = new ProductInstance();
            queryCustRequest.queryCustomerRequest.customerAccount.productInstance.id = new ProductInstanceIdentifier();
            queryCustRequest.queryCustomerRequest.customerAccount.productInstance.id.name = "ServiceId";
            queryCustRequest.queryCustomerRequest.customerAccount.productInstance.id.value = deviceID;

            queryCustomerResp = getQueryCustomerResponse(deviceID,queryCustRequest);

            return queryCustomerResp;
        }
        private static QueryCustomerResponse getQueryCustomerResponse(string identity, QueryCustomerRequest request)
        {
            //queryCustomerResponse1 Response = new queryCustomerResponse1();
            QueryCustomerResponse esbResponse = new QueryCustomerResponse();
            // string xml = SerializeObject1(queryCustRequest);
            Guid guid = Guid.NewGuid();

            LogHelper.LogActivityDetails(identity, guid, "GetQueryCustomerRequest : ", TimeSpan.Zero, "ESBCallsTrace", request.SerializeObject());

            using (ChannelFactory<BT.ESB.RoBT.ManageCustomer.ManageCustomerSyncBinding> factory = new ChannelFactory<BT.ESB.RoBT.ManageCustomer.ManageCustomerSyncBinding>("ManageCustomerSyncPort"))
            {
                BT.ESB.RoBT.ManageCustomer.ManageCustomerSyncBinding svc = factory.CreateChannel();

                esbResponse = svc.queryCustomer(request);

                LogHelper.LogActivityDetails(identity, guid, "GetQueryCustomerResponse : ", TimeSpan.Zero, "ESBCallsTrace", esbResponse.SerializeObject());
            }

            //if (esbResponse != null && esbResponse.customerAccount!= null)
            //    esbResponse = Response.queryCustomerResponse;

            return esbResponse;
        }
        public static QueryCustomerResponse GetQueryCustomerDetailswithBAC(string bac, Guid guid, ref string errorDescription)
        {

            QueryCustomerResponse queryCustomerResp = new QueryCustomerResponse();

            try
            {
               // queryCustomerRequest1 queryCustRequest = new queryCustomerRequest1();
                QueryCustomerRequest queryCustRequest = new QueryCustomerRequest();

                // Standard header block
                queryCustRequest.standardHeader = new BT.ESB.RoBT.ManageCustomer.StandardHeaderBlock();
                queryCustRequest.standardHeader.e2e = new BT.ESB.RoBT.ManageCustomer.E2E();

                queryCustRequest.standardHeader.e2e.E2EDATA = ConfigurationManager.AppSettings["E2EData"].ToString();

                queryCustRequest.standardHeader.serviceAddressing = new BT.ESB.RoBT.ManageCustomer.ServiceAddressing();
                queryCustRequest.standardHeader.serviceAddressing.serviceName = @"http://capabilities.nat.bt.com/ManageCustomer";
                queryCustRequest.standardHeader.serviceAddressing.action = @"http://capabilities.nat.bt.com/wsdl/ManageCustomer#queryCustomerRequest";
                queryCustRequest.standardHeader.serviceAddressing.messageId = Guid.NewGuid().ToString();
                queryCustRequest.standardHeader.serviceAddressing.from = @"VasFulfilment"; // todo: temporary one - need to check with ESB and to change this
                queryCustRequest.standardHeader.serviceAddressing.to = new BT.ESB.RoBT.ManageCustomer.AddressReference();
                queryCustRequest.standardHeader.serviceAddressing.to.address = @"http://cmpal.bt.com";

                queryCustRequest.standardHeader.serviceState = new BT.ESB.RoBT.ManageCustomer.ServiceState();
                queryCustRequest.standardHeader.serviceState.stateCode = "OK";


                queryCustRequest.queryCustomerRequest = new QueryCustomerReq();
                queryCustRequest.queryCustomerRequest.customerAccount = new BT.ESB.RoBT.ManageCustomer.CustomerAccount1();
                queryCustRequest.queryCustomerRequest.customerAccount.billingAccount = new BT.ESB.RoBT.ManageCustomer.BillingAccount();
                queryCustRequest.queryCustomerRequest.customerAccount.billingAccount.accountNumber = bac;
               
                queryCustomerResp = getQueryCustomerResponse(bac,queryCustRequest);
            }
            catch (Exception ex)
            {
                errorDescription = ex.Message.ToString();
                throw ex;
            }

            return queryCustomerResp;
        }        

        public static bool GetIdentityDetails(string domainValue, string domainName, ref string downStreamError, ref string updatedE2EDataValue, out BT.SaaS.MSEOAdapter.ESB.ID.IdentityDetailType iddetails, Guid guid)
        {
            bool isEsbResponseSuccess = false;
            iddetails = new BT.SaaS.MSEOAdapter.ESB.ID.IdentityDetailType();
            Guid APIGWTracker = Guid.NewGuid();
            try
            {
                if (!string.IsNullOrEmpty(domainValue) && (!string.IsNullOrEmpty(domainName)))
                {
                    //System.Diagnostics.Trace.WriteLine("In Get Identity Details call");

                    //string url = string.Format("{0}/RoBTESB/mcir/v6/IdentityDetails?ID={1}&IDType={2}", ConfigurationManager.AppSettings["ESBV6BaseURI"], domainValue, domainName);
                   // var response = BT.Helpers.RESTHelper.DoRESTCall("GET", url, ref updatedE2EDataValue, true, new NetworkCredential { UserName = null, Password = null }, null, null);

                    string url = string.Format("{0}/common/v1/customer-identities?id={1}&idType={2}&org=BT&responseVersion=V6", ConfigurationManager.AppSettings["APIGWBaseURI"], domainValue, domainName);
                    var response = RESTHelper.DoRESTCallforAPIGW("GET", url, ref updatedE2EDataValue, true, new NetworkCredential { UserName = null, Password = null }, null, null,APIGWTracker);

                    //System.Diagnostics.Trace.WriteLine(response.ToString());

                    LogHelper.LogActivityDetails(domainValue, APIGWTracker, "GetIdentityDetails Response:", TimeSpan.Zero, "ESBCallsTrace", url + " : " + response.ToString());

                    //LogHelper.LogActivityDetails(url, Guid.NewGuid(), "GetIdentityDetails response", TimeSpan.Zero, "VASEligilityTrace", response.ToString());

                    if ((!string.IsNullOrEmpty(response)) && (!response.Contains("RESTCALLFAILURE")))
                    {
                        iddetails = BT.Helpers.XmlHelper.DeserializeObject<BT.SaaS.MSEOAdapter.ESB.ID.IdentityDetailType>(response);
                        isEsbResponseSuccess = true;
                    }
                    else
                    {
                        downStreamError = response;
                    }
                }
            }
            catch (Exception ex)
            {
                downStreamError = ex.Message;
            }
            return isEsbResponseSuccess;
        }

        public static bool GetContactDetails(string inputData, ref string downStreamError, ref string updatedE2EDataValue, out BT.SaaS.MSEOAdapter.ESB.GCD.V9.ContactDetailsResponse contactDetailsResponse, Guid guid)
        {
            updatedE2EDataValue = string.Empty;
            bool result = false; contactDetailsResponse = new BT.SaaS.MSEOAdapter.ESB.GCD.V9.ContactDetailsResponse();
            Guid APIGWTracker = Guid.NewGuid();
            try
            {
                if (!string.IsNullOrEmpty(inputData))
                {
                    var ovResponse = string.Empty;
                    string url = string.Empty;
                    if (Convert.ToBoolean(ConfigurationManager.AppSettings["IsTSOCE25333enabled"]))
                    {
                        url = ConfigurationManager.AppSettings["APIGWBaseURI"] + @"/bt-consumer/v1/customer-contact-manager/contacts/" + inputData + "?iscdatarequired=Y&status=All&responseVersion=v9";
                        ovResponse = RESTHelper.DoRESTCallforAPIGW("GET", url, ref updatedE2EDataValue, true, new NetworkCredential { UserName = null, Password = null }, null, null,APIGWTracker);
                    }
                    else
                    {
                        url = ConfigurationManager.AppSettings["ESBGCDBaseURI"] + @"/RoBTESB/V9/contacts/" + inputData + ".xml?btblegacy=Y&STACK=CCP&status=All";
                        ovResponse = BT.Helpers.RESTHelper.DoRESTCall("GET", url, ref updatedE2EDataValue, true, new NetworkCredential { UserName = null, Password = null }, null, null);
                    }

                    LogHelper.LogActivityDetails(inputData, APIGWTracker, "GetContactDetails Response", TimeSpan.Zero, "ESBCallsTrace", url + " : " + ovResponse.ToString());

                    if (!string.IsNullOrEmpty(ovResponse) && !ovResponse.Contains("RESTCALLFAILURE"))
                    {
                        contactDetailsResponse = BT.Helpers.XmlHelper.DeserializeObject<BT.SaaS.MSEOAdapter.ESB.GCD.V9.ContactDetailsResponse>(ovResponse);
                        result = true;
                    }
                    else
                    {
                        downStreamError = ovResponse;
                    }
                }
            }
            catch (Exception ex)
            {
                //LogHelper.Write(string.Format("OV call failed with exception: {0}", ex.Message), Logger.LogHelper.TypeEnum.ExceptionTrace);
            }
            return result;
        }

        #region createcustomerid
        /// <summary>
        /// to create customerid 
        /// </summary>
        /// <returns></returns>
        public static bool CreateCustomerid(string conkid, ref string downStreamError, out string cakID, Guid guid, string bptmTxnId, ref E2ETransaction e2eData)
        {
            bool isCAKCreated = false; cakID = string.Empty;
            CreateCustomerResponse custResponse = new CreateCustomerResponse();
            //createCustomerRequest1 custRequest = new createCustomerRequest1();
            try
            {
                CreateCustomerRequest Request = new CreateCustomerRequest();

                Request.standardHeader = new BT.ESB.RoBT.ManageCustomer.StandardHeaderBlock();
                Request.standardHeader.e2e = new BT.ESB.RoBT.ManageCustomer.E2E();

                string OVreferencenumber = "MSEOCustomerIDORDER" + RandomNumber(1234567, 9999999);
                Request.standardHeader.e2e.E2EDATA = e2eData.toString();//ConfigurationManager.AppSettings["E2EData"].ToString() + "CustomerReferenceNumber:" + OVreferencenumber;
                string E2EData = e2eData.toString();
                Request.standardHeader.serviceState = new BT.ESB.RoBT.ManageCustomer.ServiceState();
                Request.standardHeader.serviceState.stateCode = "OK";
                Request.standardHeader.serviceAddressing = new BT.ESB.RoBT.ManageCustomer.ServiceAddressing();
                Request.standardHeader.serviceAddressing.from = @"VasFulfilment";
                Request.standardHeader.serviceAddressing.messageId = Guid.NewGuid().ToString();
                Request.standardHeader.serviceAddressing.to = new BT.ESB.RoBT.ManageCustomer.AddressReference();
                Request.standardHeader.serviceAddressing.to.address = @"http://cmpal.bt.com";
                Request.standardHeader.serviceAddressing.serviceName = @"http://capabilities.nat.bt.com/ManageCustomer";
                Request.standardHeader.serviceAddressing.action = @"http://capabilities.nat.bt.com/wsdl/ManageCustomer#createCustomerRequest";

                Request.contact = new BT.ESB.RoBT.ManageCustomer.CustomerContact2();
                Request.contact.customerAccount = new BT.ESB.RoBT.ManageCustomer.CustomerAccount3();
                Request.contact.customerAccount.customerType = "Consumer";
                Request.contact.id = conkid;// need to pass the conkid                 

                e2eData.logMessage("StartedESBCall to creat customerid", "");
                e2eData.startOutboundCall(ConfigurationManager.AppSettings["ESBSystemID"].ToString(), E2ETransaction.vREQ, "Reinstate Journey");

                string finalrequestxml = PrepareCustomerIdCreationRequest(Request);
                                
                LogHelper.LogActivityDetails(bptmTxnId, guid, "Customerid reqeust", TimeSpan.Zero, "ESBCallsTrace", finalrequestxml.ToString());

                string url = ConfigurationManager.AppSettings["ESBcakcreateurl"];

                var response = RESTHelper.DoRESTCall("POST", url, ref E2EData, true, new NetworkCredential { UserName = null, Password = null }, finalrequestxml, null);
                
                LogHelper.LogActivityDetails(bptmTxnId, guid, "Customerid response", TimeSpan.Zero, "ESBCallsTrace", response.ToString());

                if (!string.IsNullOrEmpty(response) && response.Contains("The requested transaction has been completed successfully"))
                {
                    cakID = GetCustomeridfromCCResponse(response.ToString());
                    if (!string.IsNullOrEmpty(cakID))
                        isCAKCreated = true;
                }
                else
                {
                    downStreamError = "CreateCustomerid failed ";
                }

                #region soapcode
                /*
                using (ChannelFactory<BT.ESB.RoBT.ManageCustomer.ManageCustomerSyncBinding> factory = new ChannelFactory<BT.ESB.RoBT.ManageCustomer.ManageCustomerSyncBinding>("ManageCustomerSyncPort"))
                {
                    BT.ESB.RoBT.ManageCustomer.ManageCustomerSyncBinding svc = factory.CreateChannel();

                    LogHelper.LogActivityDetails(bptmTxnId, guid, "Customerid reqeust", TimeSpan.Zero, "ESBCallsTrace", Request.SerializeObject());
                    custResponse = svc.createCustomer(Request);
                    LogHelper.LogActivityDetails(bptmTxnId, guid, "Customerid response", TimeSpan.Zero, "ESBCallsTrace", custResponse.SerializeObject());

                    if (custResponse != null && custResponse != null && custResponse.standardHeader != null
                         && custResponse.standardHeader.serviceState != null && custResponse.standardHeader.serviceState.stateCode != null
                         && custResponse.standardHeader.serviceState.stateCode.Equals("OK", StringComparison.OrdinalIgnoreCase) && custResponse.customer != null && custResponse.customer.id != null)
                    {
                        isCAKCreated = true; cakID = custResponse.customer.id;
                        if (custResponse.standardHeader != null && custResponse.standardHeader.e2e != null && !string.IsNullOrEmpty(custResponse.standardHeader.e2e.E2EDATA))
                        {
                            e2eData.endOutboundCall(custResponse.standardHeader.e2e.E2EDATA);
                        }
                        else
                            e2eData.endOutboundCall(e2eData.toString());
                    }
                    else if (custResponse != null && custResponse != null && custResponse.standardHeader != null &&
                             custResponse.standardHeader.serviceState.errorDesc != null && !string.IsNullOrEmpty(custResponse.standardHeader.serviceState.errorDesc))
                    {
                        downStreamError = custResponse.standardHeader.serviceState.errorDesc;
                        if (custResponse.standardHeader != null && custResponse.standardHeader.e2e != null && !string.IsNullOrEmpty(custResponse.standardHeader.e2e.E2EDATA))
                        {
                            e2eData.endOutboundCall(custResponse.standardHeader.e2e.E2EDATA);
                        }
                        else
                            e2eData.endOutboundCall(e2eData.toString());
                    }
                    else
                    {
                        downStreamError = "CAK creation failed:Null response from esb or no CAK returned during CAK creation";
                        e2eData.endOutboundCall(e2eData.toString());
                    }
                }*/
                #endregion
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return isCAKCreated;
        }
        #endregion

        /// <summary>
        /// to add the missing tags and namespaces for esb create customerid request.
        /// </summary>
        /// <param name="Request"></param>
        /// <returns></returns>
        public static string PrepareCustomerIdCreationRequest(CreateCustomerRequest Request)
        {
            string xmlns = "<createCustomerRequest xmlns=\"http://capabilities.nat.bt.com/xsd/ManageCustomer/2017/10/12\">";
            //string oldXmlns = "<requestOrder xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">";
            string oldXmlns = "<CreateCustomerRequest xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">";
            string oldfooterxmlns = "</CreateCustomerRequest>";
            string newfooterxmlns = "</createCustomerRequest >";
            string header = "<NS1:Envelope xmlns:NS1=\"http://schemas.xmlsoap.org/soap/envelope/\"><NS1:Body>";
            string footer = "</NS1:Body></NS1:Envelope>";

            string xml = ObjectSerializer.SerializeObject(Request);
            xml = xml.Replace(oldXmlns, xmlns);
            xml = xml.Replace(oldfooterxmlns, newfooterxmlns);
            xml = xml.Replace("<?xml version=\"1.0\" encoding=\"utf-16\"?>", "");
            string finalrequestxml = header + xml + footer;

            return finalrequestxml;
        }
        /// <summary>
        /// to get the cakid/customerid from CCRespnose. 
        /// </summary>
        /// <param name="Response"></param>
        /// <returns></returns>
        public static string GetCustomeridfromCCResponse(string Response)
        {
            string id = string.Empty;

            int firstposition = Response.IndexOf("</NS4:id>");
            int secondposition = Response.IndexOf("<NS4:id xmlns:NS4=\"http://capabilities.nat.bt.com/xsd/ManageCustomer/2017/10/12/CCM/CreateCustomerResponse/Party/PartyRoles/Customer\">");
            int secondpostionlength = "<NS4:id xmlns:NS4=\"http://capabilities.nat.bt.com/xsd/ManageCustomer/2017/10/12/CCM/CreateCustomerResponse/Party/PartyRoles/Customer\">".Length;
            int substringposition = secondpostionlength + secondposition;
            id = Response.Substring(substringposition, firstposition - substringposition);

            return id;
        }

        #region BAC creation
        /// <summary>
        /// to create BAC mapper
        /// </summary>
        public static bool CreateBillingAccount(string custid, string conkid, ref string bac, BT.SaaS.MSEOAdapter.ESB.ID.IdentityDetailType idDetails, Guid guid)
        {
            try
            {
                BT.ESB.RoBT.ManageCustomerBilling.createCustomerBillingAccountRequest bacRequest = new BT.ESB.RoBT.ManageCustomerBilling.createCustomerBillingAccountRequest();

                bacRequest.standardHeader = new BT.ESB.RoBT.ManageCustomerBilling.StandardHeaderBlock();
                bacRequest.standardHeader.e2e = new BT.ESB.RoBT.ManageCustomerBilling.E2E();
                string OVreferencenumber = "MSEOBACCreateORDER" + RandomNumber(1234567, 9999999);
                bacRequest.standardHeader.e2e.E2EDATA = ConfigurationManager.AppSettings["E2EData"].ToString() + "CustomerReferenceNumber:" + OVreferencenumber;
                bacRequest.standardHeader.serviceAddressing = new BT.ESB.RoBT.ManageCustomerBilling.ServiceAddressing();

                bacRequest.standardHeader.serviceState = new BT.ESB.RoBT.ManageCustomerBilling.ServiceState();
                bacRequest.standardHeader.serviceState.stateCode = "OK";
                bacRequest.standardHeader.serviceAddressing = new BT.ESB.RoBT.ManageCustomerBilling.ServiceAddressing();
                bacRequest.standardHeader.serviceAddressing.from = @"VasFulfilment";
                bacRequest.standardHeader.serviceAddressing.messageId = Guid.NewGuid().ToString();
                bacRequest.standardHeader.serviceAddressing.to = new BT.ESB.RoBT.ManageCustomerBilling.AddressReference();
                bacRequest.standardHeader.serviceAddressing.to.address = @"http://cmpal.bt.com";
                bacRequest.standardHeader.serviceAddressing.serviceName = @"http://capabilities.nat.bt.com/ManageCustomerBilling";
                bacRequest.standardHeader.serviceAddressing.action = @"http://capabilities.nat.bt.com/wsdl/ManageCustomerBilling#createCustomerBillingAccountRequest";

                bacRequest.billAccount = new BT.ESB.RoBT.ManageCustomerBilling.BillingAccount();
                bacRequest.billAccount.type = "BT Retail Customer";
                bacRequest.billAccount.ratingType = "Cyclic";
                bacRequest.billAccount.billDateDetails = new BT.ESB.RoBT.ManageCustomerBilling.BillDateDetails();
                bacRequest.billAccount.billDateDetails.billFrequency = "Monthly";
                bacRequest.billAccount.partyBankAccount = new BT.ESB.RoBT.ManageCustomerBilling.PartyBankAccount();
                bacRequest.billAccount.partyBankAccount = null;
                bacRequest.billAccount.preferredPaymentMethod = new BT.ESB.RoBT.ManageCustomerBilling.PaymentMethod();
                bacRequest.billAccount.preferredPaymentMethod.code = "Cheque/Cash";
                bacRequest.billAccount.customerAccount = new BT.ESB.RoBT.ManageCustomerBilling.CustomerAccount();
                bacRequest.billAccount.customerAccount.id = custid;//"4001309887";// conkid;//need to pass conkid
                bacRequest.billAccount.customerContact = new BT.ESB.RoBT.ManageCustomerBilling.CustomerContact();
                bacRequest.billAccount.customerContact.id = conkid;//"2311428219";// custid;//need to pass custmerid
                bacRequest.billAccount.billProductionDetails = new BT.ESB.RoBT.ManageCustomerBilling.BillProductionDetails();
                bacRequest.billAccount.billProductionDetails.billMedia = "Paper Free";

                bacRequest.billAccount.address = new BT.ESB.RoBT.ManageCustomerBilling.Address();
                if (idDetails != null && idDetails.ContactDetails != null && idDetails.ContactDetails.PostalAddresses != null && idDetails.ContactDetails.PostalAddresses.Count() > 0)
                {
                    if (idDetails.ContactDetails.PostalAddresses.ToList().Exists(poad => !string.IsNullOrEmpty(poad.BuildingName)))
                        bacRequest.billAccount.address.address.buildingName = idDetails.ContactDetails.PostalAddresses[0].BuildingName;
                    if (idDetails.ContactDetails.PostalAddresses.ToList().Exists(poad => !string.IsNullOrEmpty(poad.DependentThoroughFareName)))
                        bacRequest.billAccount.address.address.dependentThoroughfareName = idDetails.ContactDetails.PostalAddresses[0].DependentThoroughFareName;
                    if (idDetails.ContactDetails.PostalAddresses.ToList().Exists(poad => !string.IsNullOrEmpty(poad.ThoroughFareName)))
                        bacRequest.billAccount.address.address.thoroughfareName = idDetails.ContactDetails.PostalAddresses[0].ThoroughFareName;
                    if (idDetails.ContactDetails.PostalAddresses.ToList().Exists(poad => !string.IsNullOrEmpty(poad.DoubleDependentLocality)))
                        bacRequest.billAccount.address.address.doubleDependentLocality = idDetails.ContactDetails.PostalAddresses[0].DoubleDependentLocality;
                    if (idDetails.ContactDetails.PostalAddresses.ToList().Exists(poad => !string.IsNullOrEmpty(poad.Locality)))
                        bacRequest.billAccount.address.address.dependentLocality = idDetails.ContactDetails.PostalAddresses[0].Locality;
                    if (idDetails.ContactDetails.PostalAddresses.ToList().Exists(poad => !string.IsNullOrEmpty(poad.PostTown)))
                        bacRequest.billAccount.address.address.postTown = idDetails.ContactDetails.PostalAddresses[0].PostTown;
                    if (idDetails.ContactDetails.PostalAddresses.ToList().Exists(poad => !string.IsNullOrEmpty(poad.POBox)))
                        bacRequest.billAccount.address.address.poBox = idDetails.ContactDetails.PostalAddresses[0].POBox;

                    bacRequest.billAccount.address.address.thoroughfareNumber = "7";// not sure from where need to read this value.

                    bacRequest.billAccount.address.addressIdentifier = new BT.ESB.RoBT.ManageCustomerBilling.AddressIdentifier();
                    if (idDetails.ContactDetails.PostalAddresses.ToList().Exists(poad => !string.IsNullOrEmpty(poad.PostalAddressNADKey)))
                        bacRequest.billAccount.address.addressIdentifier.id = idDetails.ContactDetails.PostalAddresses[0].PostalAddressNADKey;
                    bacRequest.billAccount.address.country = new BT.ESB.RoBT.ManageCustomerBilling.Country();
                    if (idDetails.ContactDetails.PostalAddresses.ToList().Exists(poad => !string.IsNullOrEmpty(poad.Country)))
                        bacRequest.billAccount.address.country.name = idDetails.ContactDetails.PostalAddresses[0].Country;
                }

                if (PostCreateBillingAccountRequest(bacRequest, ref bac, OVreferencenumber, guid))
                {
                    return true;
                }
                else
                    return false;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        /// <summary>
        /// rest call to post the CreateBillingAccount
        /// </summary>
        /// <param name="bacRequest"></param>
        /// <param name="bacResponse"></param>
        /// <returns></returns>
        public static bool PostCreateBillingAccountRequest(BT.ESB.RoBT.ManageCustomerBilling.createCustomerBillingAccountRequest bacRequest, ref string bac, string orderref, Guid guid)
        {
            bool result = false;
            try
            {
                string url = ConfigurationManager.AppSettings["ESBBaccreateurl"].ToString();
                string responseXml;
                string errorDescription = string.Empty;

                string oldXmlns = "<createCustomerBillingAccountRequest xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">";
                string xmlns = "<createCustomerBillingAccountRequest xmlns=\"http://capabilities.nat.bt.com/xsd/ManageCustomerBilling/2016/09/01\">";

                string header = "<NS1:Envelope xmlns:NS1=\"http://schemas.xmlsoap.org/soap/envelope/\"><NS1:Body>";
                string header1 = "<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:ns=\"http://capabilities.nat.bt.com/xsd/ManageCustomerBilling/2016/09/01\" xmlns:stan=\"http://wsi.nat.bt.com/2005/06/StandardHeader/\" xmlns:fin=\"http://capabilities.nat.bt.com/xsd/ManageCustomerBilling/2016/09/01/CCM/createCustomerBillingAccountRequest/Finance\" xmlns:plac=\"http://capabilities.nat.bt.com/xsd/ManageCustomerBilling/2016/09/01/CCM/createCustomerBillingAccountRequest/Places\" xmlns:par=\"http://capabilities.nat.bt.com/xsd/ManageCustomerBilling/2016/09/01/CCM/createCustomerBillingAccountRequest/Parties\" xmlns:bas=\"http://capabilities.nat.bt.com/xsd/ManageCustomerBilling/2016/09/01/CCM/createCustomerBillingAccountRequest/BaseTypes\" xmlns:cus=\"http://capabilities.nat.bt.com/xsd/ManageCustomerBilling/2016/09/01/CCM/createCustomerBillingAccountRequest/Parties/PartyRoles/Customer\" xmlns:par1=\"http://capabilities.nat.bt.com/xsd/ManageCustomerBilling/2016/09/01/CCM/createCustomerBillingAccountRequest/Parties/PartyRoles\" xmlns:hr=\"http://capabilities.nat.bt.com/xsd/ManageCustomerBilling/2016/09/01/CCM/createCustomerBillingAccountRequest/HR\"><soapenv:Body>";
                string footer = "</soapenv:Body></soapenv:Envelope>";

                string xml = ObjectSerializer.SerializeObject(bacRequest);
                //xml = xml.Replace(oldXmlns, xmlns);
                xml = xml.Replace(oldXmlns, xmlns);
                xml = xml.Replace("<?xml version=\"1.0\" encoding=\"utf-16\"?>", "");
                string finalrequestxml = header1 + xml + footer;

                LogHelper.LogActivityDetails(orderref, guid, "CreateBAC request", TimeSpan.Zero, "ESBCallsTrace", finalrequestxml);
                System.Diagnostics.Trace.WriteLine("CreateBAC request", finalrequestxml);
                HttpWebRequest webrequest;

                webrequest = (HttpWebRequest)WebRequest.Create(url);//"http://10.52.16.67:61001/RoBTESB/Retail/CMP/Capability/ManageCustomerBilling");
                webrequest.Method = "POST";
                webrequest.ContentType = "application/xml";
                webrequest.Timeout = 300000;

                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
                UTF8Encoding encoding = new UTF8Encoding();
                byte[] postBytes = encoding.GetBytes(finalrequestxml);
                webrequest.ContentLength = postBytes.Length;

                Stream postStream = webrequest.GetRequestStream();
                postStream.Write(postBytes, 0, postBytes.Length);
                postStream.Close();

                using (HttpWebResponse webResponse = (HttpWebResponse)webrequest.GetResponse())
                {
                    using (StreamReader readStream = new StreamReader(webResponse.GetResponseStream()))
                    {
                        responseXml = readStream.ReadToEnd();
                        LogHelper.LogActivityDetails(orderref, guid, "CreateBAC response", TimeSpan.Zero, "ESBCallsTrace", responseXml);
                    }
                }
                if (responseXml.Contains("<NS3:stateCode>OK</NS3:stateCode>") && responseXml.Contains("<NS3:errorCode>00000</NS3:errorCode>"))
                {
                    int pFrom = responseXml.IndexOf("BillingAccountNumber</NS4:name><NS4:value>") + "BillingAccountNumber</NS4:name><NS4:value>".Length;
                    int pTo = responseXml.IndexOf("</NS4:value></NS4:accountNumber>");

                    bac = responseXml.Substring(pFrom, pTo - pFrom);

                    result = true;
                }
                else
                {
                    errorDescription = responseXml.ToString();
                    result = false;
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            return result;
        }
        #endregion

        #region BAC&Basic email creation
        /// <summary>
        /// create bac and basic email using request order.
        /// </summary>
        /// <returns></returns>
        public static bool CreateBACandBasicEmailcalltoESB(string bac, string emailid, string custid, Guid guid, string bptmTxnId, ref E2ETransaction e2eData)
        {
            bool result = false;
            try
            {
                string url = ConfigurationManager.AppSettings["ESBrequestorderurl"].ToString();
                string responseXml;
                string orderref = string.Empty;
                string errorDescription = string.Empty;
                string oldXmlns = string.Empty;
                requestOrder esbRequestOrder = new requestOrder();
                esbRequestOrder = CreateBACandBasicEmailcalltoESBMapper(bac, emailid, custid, ref orderref, ref errorDescription, ref e2eData);

                string xmlns = "<requestOrder xmlns=\"http://capabilities.nat.bt.com/xsd/ManageCustomerOrder/2018/02/28\">";

                if (Convert.ToBoolean(ConfigurationManager.AppSettings["IsRequestOrderfailed"]))
                    oldXmlns = "<requestOrder xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">";
                else
                    oldXmlns = "<requestOrder xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">";

                string header = "<NS1:Envelope xmlns:NS1=\"http://schemas.xmlsoap.org/soap/envelope/\"><NS1:Body>";
                string footer = "</NS1:Body></NS1:Envelope>";

                string xml = ObjectSerializer.SerializeObject(esbRequestOrder);
                xml = xml.Replace(oldXmlns, xmlns);
                xml = xml.Replace("<?xml version=\"1.0\" encoding=\"utf-16\"?>", "");
                string finalrequestxml = header + xml + footer;

                LogHelper.LogActivityDetails(bptmTxnId, guid, "CreateBASIC email request", TimeSpan.Zero, "ESBCallsTrace", finalrequestxml);

                HttpWebRequest webrequest;

                e2eData.logMessage("StartedESBCall to creat BASIC mail", "");
                e2eData.startOutboundCall(ConfigurationManager.AppSettings["ESBSystemID"].ToString(), E2ETransaction.vREQ, "Reinstate Journey");

                webrequest = (HttpWebRequest)WebRequest.Create(url);//"http://10.52.16.67:61001/RoBTESB/Retail/CMP/Capability/ManageCustomerOrder");
                webrequest.Method = "POST";
                webrequest.ContentType = "application/xml";
                webrequest.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["CMPSTimeOut"].ToString());

                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
                UTF8Encoding encoding = new UTF8Encoding();
                byte[] postBytes = encoding.GetBytes(finalrequestxml);
                webrequest.ContentLength = postBytes.Length;

                Stream postStream = webrequest.GetRequestStream();
                postStream.Write(postBytes, 0, postBytes.Length);
                postStream.Close();

                using (HttpWebResponse webResponse = (HttpWebResponse)webrequest.GetResponse())
                {
                    using (StreamReader readStream = new StreamReader(webResponse.GetResponseStream()))
                    {
                        responseXml = readStream.ReadToEnd();

                        LogHelper.LogActivityDetails(bptmTxnId, guid, "CreateBASIC email response", TimeSpan.Zero, "ESBCallsTrace", responseXml);
                        e2eData.endOutboundCall(e2eData.toString());
                    }
                }
                if (responseXml.Contains("<NS3:stateCode>OK</NS3:stateCode>"))
                {
                    result = true;
                }
                else
                {
                    errorDescription = responseXml.ToString();
                    result = false;
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            return result;
        }

        /// <summary>
        /// create bac and basic email mapper method.
        /// </summary>
        /// <returns></returns>
        public static requestOrder CreateBACandBasicEmailcalltoESBMapper(string Bacid, string emailid, string customerid, ref string OVreferencenumber, ref string errorDescription,ref E2ETransaction e2eData)
        {
            requestOrder1 req = new requestOrder1();
            requestOrder requestOrder = new requestOrder();
            try
            {
                OVreferencenumber = "MSEOBACORDER" + RandomNumber(1234567, 9999999);


                BT.ESB.RoBT.ManageCustomerOrder.StandardHeaderBlock headerBlock = new BT.ESB.RoBT.ManageCustomerOrder.StandardHeaderBlock();
                headerBlock.e2e = new BT.ESB.RoBT.ManageCustomerOrder.E2E();
                headerBlock.e2e.E2EDATA = e2eData.toString(); //ConfigurationManager.AppSettings["E2EData"].ToString() + "CustomerReferenceNumber:" + OVreferencenumber;

                Logger.Write("ESB CeaseBAC call OV referenceNumber: " + OVreferencenumber + " with E2E Data: " + headerBlock.e2e.E2EDATA, Logger.TypeEnum.MessageTrace);

                headerBlock.serviceAddressing = new BT.ESB.RoBT.ManageCustomerOrder.ServiceAddressing();
                headerBlock.serviceAddressing.from = @"VasFulfilment";
                headerBlock.serviceAddressing.to = new BT.ESB.RoBT.ManageCustomerOrder.AddressReference();
                headerBlock.serviceAddressing.to.address = @"http://cmpal.bt.com";
                headerBlock.serviceAddressing.to.contextItemList = new BT.ESB.RoBT.ManageCustomerOrder.ContextItem[1];
                headerBlock.serviceAddressing.to.contextItemList[0] = new BT.ESB.RoBT.ManageCustomerOrder.ContextItem();
                headerBlock.serviceAddressing.to.contextItemList[0].contextName = "targetSystem";
                headerBlock.serviceAddressing.to.contextItemList[0].contextId = @"http://cmpal.bt.com/contextItem" + ">VOL01";

                headerBlock.serviceAddressing.messageId = Guid.NewGuid().ToString();
                headerBlock.serviceAddressing.serviceName = @"http://capabilities.nat.bt.com/ManageCustomerOrder";
                headerBlock.serviceAddressing.action = @"http://capabilities.nat.bt.com/wsdl/ManageCustomerOrder#requestOrder";

                headerBlock.serviceState = new BT.ESB.RoBT.ManageCustomerOrder.ServiceState();
                headerBlock.serviceState.stateCode = "OK";

                headerBlock.serviceSpecification = new BT.ESB.RoBT.ManageCustomerOrder.ServiceSpecification();
                headerBlock.serviceSpecification.version = "Version 2.0";

                RequestOrderReq orderRequest = new RequestOrderReq();
                orderRequest.order = new Order1();

                orderRequest.order.action = new OrderAction();
                orderRequest.order.action.code = codeOrderActionEnum.Create;

                orderRequest.order.fromParty = new BT.ESB.RoBT.ManageCustomerOrder.Party[2];
                orderRequest.order.fromParty[0] = new BT.ESB.RoBT.ManageCustomerOrder.Party();
                orderRequest.order.fromParty[0].identifier = new PartyIdentifier();
                orderRequest.order.fromParty[0].identifier.name = "CustomerId";
                orderRequest.order.fromParty[0].identifier.value = customerid;

                orderRequest.order.fromParty[1] = new BT.ESB.RoBT.ManageCustomerOrder.Party();
                orderRequest.order.fromParty[1].identifier = new PartyIdentifier();
                orderRequest.order.fromParty[1].identifier.name = "OriginatingUserIdentifier";
                orderRequest.order.fromParty[1].identifier.value = emailid;

                orderRequest.order.thirdPartyChannel = new BT.ESB.RoBT.ManageCustomerOrder.Party[2];
                orderRequest.order.thirdPartyChannel[0] = new BT.ESB.RoBT.ManageCustomerOrder.Party();
                orderRequest.order.thirdPartyChannel[0].identifier = new PartyIdentifier();
                orderRequest.order.thirdPartyChannel[0].identifier.name = "SalesPartner";
                //orderRequest.order.thirdPartyChannel[0].identifier.value = "000";//need to pass email id.

                orderRequest.order.thirdPartyChannel[1] = new BT.ESB.RoBT.ManageCustomerOrder.Party();
                orderRequest.order.thirdPartyChannel[1].name = new BT.ESB.RoBT.ManageCustomerOrder.PartyName();
                orderRequest.order.thirdPartyChannel[1].name.nameElement = "SalesChannel";
                orderRequest.order.thirdPartyChannel[1].name.nameValue = "BT Web";

                orderRequest.order.orderDate = System.DateTime.Today;

                orderRequest.order.alternativeIdentifier = new BT.ESB.RoBT.ManageCustomerOrder.GlobalIdentifier[1];
                orderRequest.order.alternativeIdentifier[0] = new BT.ESB.RoBT.ManageCustomerOrder.GlobalIdentifier();
                orderRequest.order.alternativeIdentifier[0].type = "CustomerReferenceNumber";
                orderRequest.order.alternativeIdentifier[0].value = OVreferencenumber;

                orderRequest.order.billingAccount = new BT.ESB.RoBT.ManageCustomerOrder.BillingAccount();
                orderRequest.order.billingAccount.accountNumber = Bacid;

                List<OrderItem1> orderItemList = new List<OrderItem1>();
                OrderItem1 orderItem = null;

                #region BT BASIC MAIL
                orderItem = new OrderItem1();
                orderItem.committedDateTime = System.DateTime.Now;
                orderItem.requiredDateTime = System.DateTime.Now;

                orderItem.quantity = 1;

                orderItem.action = new BT.ESB.RoBT.ManageCustomerOrder.OrderItemAction();
                orderItem.action.code = BT.ESB.RoBT.ManageCustomerOrder.codeOrderItemActionEnum.Create;
                orderItem.action.codeSpecified = true;

                orderItem.fromReference = "280430";
                orderItem.ceaseInstruction = "Default";
                orderItem.nRCPaymentMethod = "Monthly/Quarterly Bill";
                orderItem.priceType = "Recurring";

                orderItem.billingAccount = new BT.ESB.RoBT.ManageCustomerOrder.BillingAccount();
                orderItem.billingAccount.accountNumber = Bacid;

                orderItem.specification = new BT.ESB.RoBT.ManageCustomerOrder.Specification[1];
                orderItem.specification[0] = new BT.ESB.RoBT.ManageCustomerOrder.Specification();
                orderItem.specification[0].name = "BT BASIC MAIL";
                orderItem.specification[0].specCategory = new BT.ESB.RoBT.ManageCustomerOrder.SpecCategory();
                orderItem.specification[0].specCategory.code = "S0395134";
                orderItem.instance = new BT.ESB.RoBT.ManageCustomerOrder.Instance[1];
                orderItem.instance[0] = new BT.ESB.RoBT.ManageCustomerOrder.Instance();
                orderItem.instance[0].instanceIdentifier = new BT.ESB.RoBT.ManageCustomerOrder.InstanceIdentifier[1];
                orderItem.instance[0].instanceIdentifier[0] = new BT.ESB.RoBT.ManageCustomerOrder.InstanceIdentifier();
                orderItem.instance[0].instanceIdentifier[0].name = "AssetId";
                orderItem.instance[0].instanceIdentifier[0].value = "DUMMY_10";

                orderItemList.Add(orderItem);
                #endregion

                #region ISP
                orderItem = new OrderItem1();
                orderItem.committedDateTime = System.DateTime.Now;
                orderItem.requiredDateTime = System.DateTime.Now;
                orderItem.quantity = 1;

                orderItem.action = new BT.ESB.RoBT.ManageCustomerOrder.OrderItemAction();
                orderItem.action.code = BT.ESB.RoBT.ManageCustomerOrder.codeOrderItemActionEnum.Create;
                orderItem.action.codeSpecified = true;

                orderItem.fromReference = "280430";
                orderItem.nRCPaymentMethod = "Monthly/Quarterly Bill";
                orderItem.ceaseInstruction = "Default";

                orderItem.billingAccount = new BT.ESB.RoBT.ManageCustomerOrder.BillingAccount();
                orderItem.billingAccount.accountNumber = Bacid;

                orderItem.specification = new BT.ESB.RoBT.ManageCustomerOrder.Specification[1];
                orderItem.specification[0] = new BT.ESB.RoBT.ManageCustomerOrder.Specification();
                orderItem.specification[0].name = "ISP Service";
                orderItem.specification[0].specCategory = new BT.ESB.RoBT.ManageCustomerOrder.SpecCategory();
                orderItem.specification[0].specCategory.code = "S0145869";
                orderItem.instance = new BT.ESB.RoBT.ManageCustomerOrder.Instance[1];
                orderItem.instance[0] = new BT.ESB.RoBT.ManageCustomerOrder.Instance();

                orderItem.instance[0].instanceCharactersticList = new BT.ESB.RoBT.ManageCustomerOrder.InstanceCharacteristic[1];
                orderItem.instance[0].instanceCharactersticList[0] = new BT.ESB.RoBT.ManageCustomerOrder.InstanceCharacteristic();
                orderItem.instance[0].instanceCharactersticList[0].action = new BT.ESB.RoBT.ManageCustomerOrder.InstanceAction();
                orderItem.instance[0].instanceCharactersticList[0].action.code = BT.ESB.RoBT.ManageCustomerOrder.codeInstanceActionEnum.Add;
                orderItem.instance[0].instanceCharactersticList[0].name = "ispServiceClass";
                orderItem.instance[0].instanceCharactersticList[0].value = "ISP_SC_BM";


                List<BT.ESB.RoBT.ManageCustomerOrder.InstanceIdentifier> instanceidentifierlist = new List<BT.ESB.RoBT.ManageCustomerOrder.InstanceIdentifier>();
                BT.ESB.RoBT.ManageCustomerOrder.InstanceIdentifier instanceidentifier = null;

                instanceidentifier = new BT.ESB.RoBT.ManageCustomerOrder.InstanceIdentifier();
                instanceidentifier.name = "ServiceIdReservationKey";
                instanceidentifier.value = "9999999";
                instanceidentifierlist.Add(instanceidentifier);

                instanceidentifier = new BT.ESB.RoBT.ManageCustomerOrder.InstanceIdentifier();
                instanceidentifier.name = "PromotionId";
                instanceidentifier.value = "DUMMY_10";
                instanceidentifierlist.Add(instanceidentifier);

                instanceidentifier = new BT.ESB.RoBT.ManageCustomerOrder.InstanceIdentifier();
                instanceidentifier.name = "ServiceId";
                instanceidentifier.value = emailid;//need to pass email id.
                instanceidentifierlist.Add(instanceidentifier);

                orderItem.instance[0].instanceIdentifier = instanceidentifierlist.ToArray();
                BT.ESB.RoBT.ManageCustomerOrder.OrderItemAssociation association = new BT.ESB.RoBT.ManageCustomerOrder.OrderItemAssociation();

                association.orderItem = new OrderItem1();
                association.orderItem.committedDateTime = System.DateTime.Now;
                association.orderItem.requiredDateTime = System.DateTime.Now;
                association.orderItem.quantity = 1;

                association.orderItem.action = new BT.ESB.RoBT.ManageCustomerOrder.OrderItemAction();
                association.orderItem.action.code = BT.ESB.RoBT.ManageCustomerOrder.codeOrderItemActionEnum.Create;
                association.orderItem.action.codeSpecified = true;

                association.orderItem.fromReference = "280430";
                association.orderItem.nRCPaymentMethod = "Monthly/Quarterly Bill";
                association.orderItem.ceaseInstruction = "Default";

                association.orderItem.billingAccount = new BT.ESB.RoBT.ManageCustomerOrder.BillingAccount();
                association.orderItem.billingAccount.accountNumber = Bacid;

                association.orderItem.specification = new BT.ESB.RoBT.ManageCustomerOrder.Specification[1];
                association.orderItem.specification[0] = new BT.ESB.RoBT.ManageCustomerOrder.Specification();
                association.orderItem.specification[0].name = "ISP Transients";
                association.orderItem.specification[0].specCategory = new BT.ESB.RoBT.ManageCustomerOrder.SpecCategory();
                association.orderItem.specification[0].specCategory.code = "S0145872";
                association.orderItem.instance = new BT.ESB.RoBT.ManageCustomerOrder.Instance[1];
                association.orderItem.instance[0] = new BT.ESB.RoBT.ManageCustomerOrder.Instance();

                List<BT.ESB.RoBT.ManageCustomerOrder.InstanceCharacteristic> instancecharlist = new List<BT.ESB.RoBT.ManageCustomerOrder.InstanceCharacteristic>();
                BT.ESB.RoBT.ManageCustomerOrder.InstanceCharacteristic instancechar = null;

                instancechar = new BT.ESB.RoBT.ManageCustomerOrder.InstanceCharacteristic();
                instancechar.action = new BT.ESB.RoBT.ManageCustomerOrder.InstanceAction();
                instancechar.action.code = BT.ESB.RoBT.ManageCustomerOrder.codeInstanceActionEnum.Add;
                instancechar.name = "Postcode";
                instancechar.value = "AB13 0JT";
                instancecharlist.Add(instancechar);

                instancechar = new BT.ESB.RoBT.ManageCustomerOrder.InstanceCharacteristic();
                instancechar.action = new BT.ESB.RoBT.ManageCustomerOrder.InstanceAction();
                instancechar.action.code = BT.ESB.RoBT.ManageCustomerOrder.codeInstanceActionEnum.Add;
                instancechar.name = "dateOfBirth";
                instancechar.value = "14/10/1967";
                instancecharlist.Add(instancechar);

                instancechar = new BT.ESB.RoBT.ManageCustomerOrder.InstanceCharacteristic();
                instancechar.action = new BT.ESB.RoBT.ManageCustomerOrder.InstanceAction();
                instancechar.action.code = BT.ESB.RoBT.ManageCustomerOrder.codeInstanceActionEnum.Add;
                instancechar.name = "emailPassword";
                instancechar.value = "aLGWlxoXHg5LJmj+OP0TH5IYeVxL8abTTb0ycEy4n28=";
                instancecharlist.Add(instancechar);
                //orderItemList.Add(orderItem);

                instancechar = new BT.ESB.RoBT.ManageCustomerOrder.InstanceCharacteristic();
                instancechar.action = new BT.ESB.RoBT.ManageCustomerOrder.InstanceAction();
                instancechar.action.code = BT.ESB.RoBT.ManageCustomerOrder.codeInstanceActionEnum.Add;
                instancechar.name = "secretAnswer";
                instancecharlist.Add(instancechar);

                instancechar = new BT.ESB.RoBT.ManageCustomerOrder.InstanceCharacteristic();
                instancechar.action = new BT.ESB.RoBT.ManageCustomerOrder.InstanceAction();
                instancechar.action.code = BT.ESB.RoBT.ManageCustomerOrder.codeInstanceActionEnum.Add;
                instancechar.name = "emailPassword";
                instancechar.value = "1";
                instancecharlist.Add(instancechar);
                association.orderItem.instance[0].instanceCharactersticList = instancecharlist.ToArray();

                orderItem.orderItemAssociation = new BT.ESB.RoBT.ManageCustomerOrder.OrderItemAssociation[1];
                orderItem.orderItemAssociation[0] = new BT.ESB.RoBT.ManageCustomerOrder.OrderItemAssociation();
                orderItem.orderItemAssociation[0] = association;

                orderItem.installation = new BT.ESB.RoBT.ManageCustomerOrder.Installation();
                orderItem.installation.installationLocation = new BT.ESB.RoBT.ManageCustomerOrder.Location();
                orderItem.installation.installationLocation.address = new BT.ESB.RoBT.ManageCustomerOrder.Address();
                orderItem.installation.installationLocation.address.addressIdentifier = new BT.ESB.RoBT.ManageCustomerOrder.AddressIdentifier();
                orderItem.installation.installationLocation.address.addressIdentifier.id = "R00026740827";
                orderItem.installation.installationLocation.address.country = new BT.ESB.RoBT.ManageCustomerOrder.Country();
                orderItem.installation.installationLocation.address.country.name = "UK";

                orderItemList.Add(orderItem);
                #endregion

                #region Narrowband Service               

                orderItem = new OrderItem1();
                orderItem.committedDateTime = System.DateTime.Now;
                orderItem.requiredDateTime = System.DateTime.Now;

                orderItem.quantity = 1;

                orderItem.action = new BT.ESB.RoBT.ManageCustomerOrder.OrderItemAction();
                orderItem.action.code = BT.ESB.RoBT.ManageCustomerOrder.codeOrderItemActionEnum.Create;
                orderItem.action.codeSpecified = true;

                orderItem.fromReference = "280430";
                orderItem.ceaseInstruction = "Default";
                orderItem.nRCPaymentMethod = "Monthly/Quarterly Bill";
                orderItem.priceType = string.Empty;

                orderItem.billingAccount = new BT.ESB.RoBT.ManageCustomerOrder.BillingAccount();
                orderItem.billingAccount.accountNumber = Bacid;

                orderItem.specification = new BT.ESB.RoBT.ManageCustomerOrder.Specification[1];
                orderItem.specification[0] = new BT.ESB.RoBT.ManageCustomerOrder.Specification();
                orderItem.specification[0].name = "Narrowband Service";
                orderItem.specification[0].specCategory = new BT.ESB.RoBT.ManageCustomerOrder.SpecCategory();
                orderItem.specification[0].specCategory.code = "S0200763";
                orderItem.instance = new BT.ESB.RoBT.ManageCustomerOrder.Instance[1];
                orderItem.instance[0] = new BT.ESB.RoBT.ManageCustomerOrder.Instance();
                orderItem.instance[0].instanceIdentifier = new BT.ESB.RoBT.ManageCustomerOrder.InstanceIdentifier[1];
                orderItem.instance[0].instanceIdentifier[0] = new BT.ESB.RoBT.ManageCustomerOrder.InstanceIdentifier();
                orderItem.instance[0].instanceIdentifier[0].name = "PromotionId";
                orderItem.instance[0].instanceIdentifier[0].value = "DUMMY_10";
                orderItemList.Add(orderItem);
                #endregion

                #region Basic Mail Marker               

                orderItem = new OrderItem1();
                orderItem.committedDateTime = System.DateTime.Now;
                orderItem.requiredDateTime = System.DateTime.Now;

                orderItem.quantity = 1;

                orderItem.action = new BT.ESB.RoBT.ManageCustomerOrder.OrderItemAction();
                orderItem.action.code = BT.ESB.RoBT.ManageCustomerOrder.codeOrderItemActionEnum.Create;
                orderItem.action.codeSpecified = true;

                orderItem.fromReference = "280430";
                orderItem.ceaseInstruction = "Default";
                orderItem.nRCPaymentMethod = "Monthly/Quarterly Bill";
                orderItem.priceType = "One-Time";

                orderItem.billingAccount = new BT.ESB.RoBT.ManageCustomerOrder.BillingAccount();
                orderItem.billingAccount.accountNumber = Bacid;

                orderItem.specification = new BT.ESB.RoBT.ManageCustomerOrder.Specification[1];
                orderItem.specification[0] = new BT.ESB.RoBT.ManageCustomerOrder.Specification();
                orderItem.specification[0].name = "Basic Mail Marker";
                orderItem.specification[0].specCategory = new BT.ESB.RoBT.ManageCustomerOrder.SpecCategory();
                orderItem.specification[0].specCategory.code = "S0395043";
                orderItem.instance = new BT.ESB.RoBT.ManageCustomerOrder.Instance[1];
                orderItem.instance[0] = new BT.ESB.RoBT.ManageCustomerOrder.Instance();
                orderItem.instance[0].instanceIdentifier = new BT.ESB.RoBT.ManageCustomerOrder.InstanceIdentifier[1];
                orderItem.instance[0].instanceIdentifier[0] = new BT.ESB.RoBT.ManageCustomerOrder.InstanceIdentifier();
                orderItem.instance[0].instanceIdentifier[0].name = "PromotionId";
                orderItem.instance[0].instanceIdentifier[0].value = "DUMMY_10";

                orderItemList.Add(orderItem);

                #endregion

                orderRequest.order.orderItem = orderItemList.ToArray();

                //requestOrder.orderRequest.order.deliveryDetails = new DeliveryDetails();
                //requestOrder.orderRequest.order.consentChanged = string.Empty;

                requestOrder.standardHeader = headerBlock;
                requestOrder.orderRequest = orderRequest;
            }
            catch (Exception ex)
            {
                throw ex;
            }

            req.requestOrder = requestOrder;
            return requestOrder;
        }
        #endregion

        #region create conkid
        /// <summary>
        /// createConkid rest post call.
        /// </summary>
        /// <returns></returns>
        public static bool PostCreateConkRequest(BT.SaaS.MSEOAdapter.ESB.ID.IdentityDetailType idDetails, ref string conkid, Guid guid)
        {
            bool result = false;
            string conkidrefnumber = string.Empty;
            System.DateTime ActivityStartTime = System.DateTime.Now;
            try
            {
                string url = ConfigurationManager.AppSettings["ESBcankcreateurl"].ToString();

                BT.ESB.RoBT.MCCP.CreateCustomerContactPersonRequest conkRequest = new BT.ESB.RoBT.MCCP.CreateCustomerContactPersonRequest();
                BT.ESB.RoBT.MCCP.CreateCustomerContactPersonResponse conkResponse = new BT.ESB.RoBT.MCCP.CreateCustomerContactPersonResponse();
                conkRequest = createConkid(idDetails, ref conkidrefnumber);

                string responseXml;
                string response = string.Empty;
                string errorDescription = string.Empty;

                string header = "<NS1:Envelope xmlns:NS1=\"http://schemas.xmlsoap.org/soap/envelope/\"><NS1:Body>";
                string footer = "</NS1:Body></NS1:Envelope>";
                string xml = ObjectSerializer.SerializeObject(conkRequest);
                //xml = xml.Replace(oldXmlns, xmlns);
                xml = xml.Replace("<?xml version=\"1.0\" encoding=\"utf-16\"?>", "");
                string finalrequestxml = header + xml + footer;

                LogHelper.LogActivityDetails(conkidrefnumber, guid, "Conkid request", TimeSpan.Zero, "ESBCallsTrace", finalrequestxml);

                ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback(ValidateRemoteCertificate);
                HttpWebRequest webrequest;

                webrequest = (HttpWebRequest)WebRequest.Create(url);//"https://vtm1.test.robt.esb.intra.bt.com:53081/RoBTESB/Retail/CMP/Capability/ManageCustomerContactPerson");
                webrequest.Method = "POST";
                webrequest.ContentType = "application/xml";
                webrequest.Timeout = 300000;

                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
                UTF8Encoding encoding = new UTF8Encoding();
                byte[] postBytes = encoding.GetBytes(finalrequestxml);
                webrequest.ContentLength = postBytes.Length;

                Stream postStream = webrequest.GetRequestStream();
                postStream.Write(postBytes, 0, postBytes.Length);
                postStream.Close();

                using (HttpWebResponse webResponse = (HttpWebResponse)webrequest.GetResponse())
                {
                    using (StreamReader readStream = new StreamReader(webResponse.GetResponseStream()))
                    {
                        responseXml = readStream.ReadToEnd();
                        LogHelper.LogActivityDetails(conkidrefnumber, guid, "Conkid response", TimeSpan.Zero, "ESBCallsTrace", responseXml);
                    }
                }

                if (responseXml.Contains("<header:stateCode>OK</header:stateCode>") && !responseXml.Contains("<header:errorText>FAILED</header:errorText>"))
                {
                    #region xmldeserialization
                    //var Value = XDocument.Parse(responseXml);

                    //XNamespace ns = @"http://schemas.xmlsoap.org/soap/envelope/";
                    //var unwrappedResponse = Value.Descendants((XNamespace)"http://schemas.xmlsoap.org/soap/envelope/" + "Body").First().FirstNode;

                    //XmlSerializer oXmlSerializer = new XmlSerializer(typeof(BT.ESB.RoBT.MCCP.CreateCustomerContactPersonResponse));

                    //conkResponse = (BT.ESB.RoBT.MCCP.CreateCustomerContactPersonResponse)oXmlSerializer.Deserialize(unwrappedResponse.CreateReader());
                    //if (conkResponse != null && conkResponse.contact != null && conkResponse.contact.id != null)
                    //{
                    //    conkid = conkResponse.contact.id;
                    //}
                    #endregion
                    int pFrom = responseXml.IndexOf("<customer:id>") + "<customer:id>".Length;
                    int pTo = responseXml.IndexOf("</customer:id>");

                    conkid = responseXml.Substring(pFrom, pTo - pFrom);

                    result = true;
                }
                else
                {
                    errorDescription = responseXml.ToString();
                    result = false;
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            return result;
        }

        /// <summary>
        /// createConkid mapper method.
        /// </summary>
        /// <returns></returns>
        public static BT.ESB.RoBT.MCCP.CreateCustomerContactPersonRequest createConkid(BT.SaaS.MSEOAdapter.ESB.ID.IdentityDetailType idDetails, ref string OVreferencenumber)
        {
            //Todo add checks for MCIRChangesInvoke Switch
            BT.ESB.RoBT.MCCP.CreateCustomerContactPersonRequest conkRequest = new BT.ESB.RoBT.MCCP.CreateCustomerContactPersonRequest();

            try
            {
                conkRequest.standardHeader = new BT.ESB.RoBT.MCCP.StandardHeaderBlock();
                conkRequest.standardHeader.e2e = new BT.ESB.RoBT.MCCP.E2E();
                OVreferencenumber = "MSEOConkIdORDER" + RandomNumber(1234567, 9999999);
                conkRequest.standardHeader.e2e.E2EDATA = ConfigurationManager.AppSettings["E2EData"].ToString() + "CustomerReferenceNumber:" + OVreferencenumber;
                //conkRequest.standardHeader.e2e.E2EDATA = ConfigurationManager.AppSettings["E2EData"].ToString();

                conkRequest.standardHeader.serviceAddressing = new BT.ESB.RoBT.MCCP.ServiceAddressing();
                conkRequest.standardHeader.serviceAddressing.from = @"VasFulfilment";
                conkRequest.standardHeader.serviceAddressing.to = new BT.ESB.RoBT.MCCP.AddressReference();
                conkRequest.standardHeader.serviceAddressing.to.address = @"http://cmpal.bt.com";
                conkRequest.standardHeader.serviceAddressing.messageId = Guid.NewGuid().ToString();
                conkRequest.standardHeader.serviceAddressing.serviceName = @"http://capabilities.nat.bt.com/ManageCustomerContactPerson";
                conkRequest.standardHeader.serviceAddressing.action = @"http://capabilities.nat.bt.com/wsdl/ManageCustomerContactPerson#createCustomerContactPersonRequest";

                conkRequest.standardHeader.serviceState = new BT.ESB.RoBT.MCCP.ServiceState();
                conkRequest.standardHeader.serviceState.stateCode = "OK";

                conkRequest.customerContact = new BT.ESB.RoBT.MCCP.CustomerContact2();

                List<BT.ESB.RoBT.MCCP.ContactDetails1> contactdetailslist = new List<BT.ESB.RoBT.MCCP.ContactDetails1>();

                BT.ESB.RoBT.MCCP.ContactDetails1 contactdetails = null;
                if (idDetails != null && idDetails.ContactDetails != null)
                {
                    if (idDetails.ContactDetails.EmailAddresses != null && idDetails.ContactDetails.EmailAddresses.Count() > 0)
                    {
                        contactdetails = new BT.ESB.RoBT.MCCP.ContactDetails1();
                        contactdetails.emailID = new BT.ESB.RoBT.MCCP.EmailAddress1();
                        if (idDetails.ContactDetails.EmailAddresses.ToList().Exists(mail => mail.Name.Equals(BT.SaaS.MSEOAdapter.ESB.ID.EmailName.PrimaryEmail)))
                            contactdetails.emailID.emailAddress = idDetails.ContactDetails.EmailAddresses.ToList().Where(mail => mail.Name.Equals(BT.SaaS.MSEOAdapter.ESB.ID.EmailName.PrimaryEmail)).FirstOrDefault().Value;
                        else
                            contactdetails.emailID.emailAddress = idDetails.ContactDetails.EmailAddresses.FirstOrDefault().Value;
                        contactdetails.emailID.primarySpecified = true;
                        contactdetails.emailID.primary = true;
                        contactdetails.type = "Email";
                        contactdetailslist.Add(contactdetails);
                    }
                    if (idDetails.ContactDetails.PhoneNumbers != null && idDetails.ContactDetails.PhoneNumbers.Count() > 0)
                    {
                        contactdetails = new BT.ESB.RoBT.MCCP.ContactDetails1();
                        contactdetails.phoneNumber = new BT.ESB.RoBT.MCCP.PhoneNumber1();
                        contactdetails.phoneNumber.phoneNumber = idDetails.ContactDetails.PhoneNumbers[0].Value.ToString();
                        contactdetails.phoneNumber.primarySpecified = true;
                        contactdetails.phoneNumber.primary = true;
                        contactdetails.type = "Home";
                        contactdetailslist.Add(contactdetails);
                    }

                    conkRequest.customerContact.contactDetails = contactdetailslist.ToArray();

                    if (idDetails.ContactDetails.PostalAddresses != null && idDetails.ContactDetails.PostalAddresses.Count() > 0)
                    {
                        conkRequest.customerContact.address = new BT.ESB.RoBT.MCCP.Address1[1];
                        conkRequest.customerContact.address[0] = new BT.ESB.RoBT.MCCP.Address1();
                        conkRequest.customerContact.address[0].addID = new BT.ESB.RoBT.MCCP.AddressIdentifier1();
                        if (idDetails.ContactDetails.PostalAddresses.ToList().Exists(poad => !string.IsNullOrEmpty(poad.PostalAddressNADKey)))
                            conkRequest.customerContact.address[0].addID.id = idDetails.ContactDetails.PostalAddresses[0].PostalAddressNADKey;// need to check this value
                        conkRequest.customerContact.address[0].primary = "Y";
                        conkRequest.customerContact.address[0].country = new BT.ESB.RoBT.MCCP.Country1();
                        if (idDetails.ContactDetails.PostalAddresses.ToList().Exists(poad => !string.IsNullOrEmpty(poad.Country)))
                            conkRequest.customerContact.address[0].country.countryName = idDetails.ContactDetails.PostalAddresses[0].Country;
                        conkRequest.customerContact.address[0].timeAtAddress = "24";
                        conkRequest.customerContact.address[0].format = "PAF";

                        conkRequest.customerContact.address[0].uKaddress = new BT.ESB.RoBT.MCCP.UKPostalAddress1();
                        if (idDetails.ContactDetails.PostalAddresses.ToList().Exists(poad => !string.IsNullOrEmpty(poad.BuildingName)))
                            conkRequest.customerContact.address[0].uKaddress.buildingName = idDetails.ContactDetails.PostalAddresses[0].BuildingName;
                        if (idDetails.ContactDetails.PostalAddresses.ToList().Exists(poad => !string.IsNullOrEmpty(poad.BuildingNumber)))
                            conkRequest.customerContact.address[0].uKaddress.buildingNumber = idDetails.ContactDetails.PostalAddresses[0].BuildingNumber;
                        if (idDetails.ContactDetails.PostalAddresses.ToList().Exists(poad => !string.IsNullOrEmpty(poad.DependentThoroughFareName)))
                            conkRequest.customerContact.address[0].uKaddress.dependentThoroughfareName = idDetails.ContactDetails.PostalAddresses[0].DependentThoroughFareName;
                        if (idDetails.ContactDetails.PostalAddresses.ToList().Exists(poad => !string.IsNullOrEmpty(poad.ThoroughFareName)))
                            conkRequest.customerContact.address[0].uKaddress.thoroughfareName = idDetails.ContactDetails.PostalAddresses[0].ThoroughFareName;
                        if (idDetails.ContactDetails.PostalAddresses.ToList().Exists(poad => !string.IsNullOrEmpty(poad.DoubleDependentLocality)))
                            conkRequest.customerContact.address[0].uKaddress.doubleDependentLocality = idDetails.ContactDetails.PostalAddresses[0].DoubleDependentLocality;
                        if (idDetails.ContactDetails.PostalAddresses.ToList().Exists(poad => !string.IsNullOrEmpty(poad.Locality)))
                            conkRequest.customerContact.address[0].uKaddress.dependentLocality = idDetails.ContactDetails.PostalAddresses[0].Locality; // need to check the locality as well
                        if (idDetails.ContactDetails.PostalAddresses.ToList().Exists(poad => !string.IsNullOrEmpty(poad.PostTown)))
                            conkRequest.customerContact.address[0].uKaddress.postTown = idDetails.ContactDetails.PostalAddresses[0].PostTown;
                        if (idDetails.ContactDetails.PostalAddresses.ToList().Exists(poad => !string.IsNullOrEmpty(poad.County)))
                            conkRequest.customerContact.address[0].uKaddress.county = idDetails.ContactDetails.PostalAddresses[0].County;
                        if (idDetails.ContactDetails.PostalAddresses.ToList().Exists(poad => !string.IsNullOrEmpty(poad.PostCode)))
                            conkRequest.customerContact.address[0].uKaddress.postCode = idDetails.ContactDetails.PostalAddresses[0].PostCode;
                        if (idDetails.ContactDetails.PostalAddresses.ToList().Exists(poad => !string.IsNullOrEmpty(poad.SubBuildingName)))
                            conkRequest.customerContact.address[0].uKaddress.subBuildingName = idDetails.ContactDetails.PostalAddresses[0].SubBuildingName;
                        if (idDetails.ContactDetails.PostalAddresses.ToList().Exists(poad => !string.IsNullOrEmpty(poad.POBox)))
                            conkRequest.customerContact.address[0].uKaddress.poBox = idDetails.ContactDetails.PostalAddresses[0].POBox;
                    }

                    conkRequest.customerContact.person = new BT.ESB.RoBT.MCCP.Person1();
                    conkRequest.customerContact.person.dateOfBirthSpecified = true;
                    if (idDetails.ContactDetails.DOB != null)
                        conkRequest.customerContact.person.dateOfBirth = System.DateTime.Parse(idDetails.ContactDetails.DOB);
                    conkRequest.customerContact.person.personName = new BT.ESB.RoBT.MCCP.PersonName1();
                    if (idDetails.ContactDetails.FirstName != null)
                        conkRequest.customerContact.person.personName.forename = idDetails.ContactDetails.FirstName;
                    if (idDetails.ContactDetails.LastName != null)
                        conkRequest.customerContact.person.personName.surname = idDetails.ContactDetails.LastName;
                    if (idDetails.ContactDetails.Title != null)
                        conkRequest.customerContact.person.personName.title = idDetails.ContactDetails.Title;
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }

            return conkRequest;
        }
        #endregion

        public static bool CreateConkidV1(BT.SaaS.MSEOAdapter.ESB.ID.IdentityDetailType idDetails, ref string conkid, Guid guid, string bptmTxnId, ref string downStreamError,ref E2ETransaction e2eData)
        {
            bool isCAKCreated = false; //cakID = string.Empty;
            string OVreferencenumber = string.Empty;
            bool isPrimary = false;
            try
            {
                createCustomerContactPersonRequest1 ConkReq = new createCustomerContactPersonRequest1();
                createCustomerContactPersonResponse1 ConkRes = new createCustomerContactPersonResponse1();
                CreateCustomerContactPersonRequest conkRequest = new CreateCustomerContactPersonRequest();
                conkRequest.standardHeader = new BT.ESB.RoBT.MCCPV1.StandardHeaderBlock();
                conkRequest.standardHeader.e2e = new BT.ESB.RoBT.MCCPV1.E2E();
                OVreferencenumber = "MSEOConkIdORDER" + RandomNumber(1234567, 9999999);
                conkRequest.standardHeader.e2e.E2EDATA = e2eData.toString();// ConfigurationManager.AppSettings["E2EData"].ToString() + "CustomerReferenceNumber:" + OVreferencenumber;
                //conkRequest.standardHeader.e2e.E2EDATA = ConfigurationManager.AppSettings["E2EData"].ToString();

                conkRequest.standardHeader.serviceAddressing = new BT.ESB.RoBT.MCCPV1.ServiceAddressing();
                conkRequest.standardHeader.serviceAddressing.from = @"VasFulfilment";
                conkRequest.standardHeader.serviceAddressing.to = new BT.ESB.RoBT.MCCPV1.AddressReference();
                conkRequest.standardHeader.serviceAddressing.to.address = @"http://cmpal.bt.com";
                conkRequest.standardHeader.serviceAddressing.messageId = Guid.NewGuid().ToString();
                conkRequest.standardHeader.serviceAddressing.serviceName = @"http://capabilities.nat.bt.com/ManageCustomerContactPerson";
                conkRequest.standardHeader.serviceAddressing.action = @"http://capabilities.nat.bt.com/wsdl/ManageCustomerContactPerson#createCustomerContactPersonRequest";

                conkRequest.standardHeader.serviceState = new BT.ESB.RoBT.MCCPV1.ServiceState();
                conkRequest.standardHeader.serviceState.stateCode = "OK";

                conkRequest.customerContact = new BT.ESB.RoBT.MCCPV1.CustomerContact1();

                List<BT.ESB.RoBT.MCCPV1.ContactDetails1> contactdetailslist = new List<BT.ESB.RoBT.MCCPV1.ContactDetails1>();

                BT.ESB.RoBT.MCCPV1.ContactDetails1 contactdetails = null;
                if (idDetails != null && idDetails.ContactDetails != null)
                {
                    if (idDetails.ContactDetails.EmailAddresses != null && idDetails.ContactDetails.EmailAddresses.Count() > 0)
                    {
                        contactdetails = new BT.ESB.RoBT.MCCPV1.ContactDetails1();
                        contactdetails.emailID = new BT.ESB.RoBT.MCCPV1.EmailAddress1();
                        if (idDetails.ContactDetails.EmailAddresses.ToList().Exists(mail => mail.Name.Equals(BT.SaaS.MSEOAdapter.ESB.ID.EmailName.PrimaryEmail)))
                        {
                            contactdetails.emailID.emailAddress = idDetails.ContactDetails.EmailAddresses.ToList().Where(mail => mail.Name.Equals(BT.SaaS.MSEOAdapter.ESB.ID.EmailName.PrimaryEmail)).FirstOrDefault().Value;
                            isPrimary = true;
                        }
                        else
                            contactdetails.emailID.emailAddress = idDetails.ContactDetails.EmailAddresses.FirstOrDefault().Value;
                        contactdetails.emailID.primarySpecified = true;
                        contactdetails.emailID.primary = true;
                        contactdetails.type = "Email";
                        contactdetailslist.Add(contactdetails);
                    }
                    //as discussed with kalyani no need to pass phone NumberFormatInfo.
                    //if (idDetails.ContactDetails.PhoneNumbers != null && idDetails.ContactDetails.PhoneNumbers.Count() > 0)
                    //{
                    //    contactdetails = new BT.ESB.RoBT.MCCPV1.ContactDetails1();
                    //    contactdetails.phoneNumber = new BT.ESB.RoBT.MCCPV1.PhoneNumber1();
                    //    contactdetails.phoneNumber.phoneNumber = idDetails.ContactDetails.PhoneNumbers[0].Value.ToString();
                    //    contactdetails.phoneNumber.primarySpecified = true;
                    //    contactdetails.phoneNumber.primary = true;
                    //    contactdetails.type = "Home";
                    //    contactdetailslist.Add(contactdetails);
                    //}

                    conkRequest.customerContact.contactDetails = contactdetailslist.ToArray();

                    conkRequest.customerContact.address = new BT.ESB.RoBT.MCCPV1.Address1[1];
                    conkRequest.customerContact.address[0] = new BT.ESB.RoBT.MCCPV1.Address1();
                    conkRequest.customerContact.address[0].format = "PAF";
                    if (isPrimary)
                        conkRequest.customerContact.address[0].primary = "Y";
                    else
                        conkRequest.customerContact.address[0].primary = "N";

                    if (idDetails.ContactDetails.PostalAddresses != null && idDetails.ContactDetails.PostalAddresses.Count() > 0)
                    {
                        conkRequest.customerContact.address[0].addID = new BT.ESB.RoBT.MCCPV1.AddressIdentifier1();
                        conkRequest.customerContact.address[0].addID.id = idDetails.ContactDetails.PostalAddresses[0].PostalAddressNADKey;// need to check this value                        
                        conkRequest.customerContact.address[0].country = new BT.ESB.RoBT.MCCPV1.Country1();
                        if (idDetails.ContactDetails.PostalAddresses.ToList().Exists(poad => !string.IsNullOrEmpty(poad.Country)))
                            conkRequest.customerContact.address[0].country.countryName = idDetails.ContactDetails.PostalAddresses[0].Country;
                        conkRequest.customerContact.address[0].timeAtAddress = "24";

                        conkRequest.customerContact.address[0].uKaddress = new BT.ESB.RoBT.MCCPV1.UKPostalAddress1();
                        if (idDetails.ContactDetails.PostalAddresses.ToList().Exists(poad => !string.IsNullOrEmpty(poad.BuildingName)))
                            conkRequest.customerContact.address[0].uKaddress.buildingName = idDetails.ContactDetails.PostalAddresses[0].BuildingName;
                        if (idDetails.ContactDetails.PostalAddresses.ToList().Exists(poad => !string.IsNullOrEmpty(poad.BuildingNumber)))
                            conkRequest.customerContact.address[0].uKaddress.buildingNumber = idDetails.ContactDetails.PostalAddresses[0].BuildingNumber;
                        if (idDetails.ContactDetails.PostalAddresses.ToList().Exists(poad => !string.IsNullOrEmpty(poad.DependentThoroughFareName)))
                            conkRequest.customerContact.address[0].uKaddress.dependentThoroughfareName = idDetails.ContactDetails.PostalAddresses[0].DependentThoroughFareName;
                        if (idDetails.ContactDetails.PostalAddresses.ToList().Exists(poad => !string.IsNullOrEmpty(poad.ThoroughFareName)))
                            conkRequest.customerContact.address[0].uKaddress.thoroughfareName = idDetails.ContactDetails.PostalAddresses[0].ThoroughFareName;
                        if (idDetails.ContactDetails.PostalAddresses.ToList().Exists(poad => !string.IsNullOrEmpty(poad.DoubleDependentLocality)))
                            conkRequest.customerContact.address[0].uKaddress.doubleDependentLocality = idDetails.ContactDetails.PostalAddresses[0].DoubleDependentLocality;
                        //if (idDetails.ContactDetails.PostalAddresses.ToList().Exists(poad => !string.IsNullOrEmpty(poad.Locality)))
                        //    conkRequest.customerContact.address[0].uKaddress.dependentLocality = null; // need to check the locality as well
                        if (idDetails.ContactDetails.PostalAddresses.ToList().Exists(poad => !string.IsNullOrEmpty(poad.PostTown)))
                            conkRequest.customerContact.address[0].uKaddress.postTown = idDetails.ContactDetails.PostalAddresses[0].PostTown;
                        if (idDetails.ContactDetails.PostalAddresses.ToList().Exists(poad => !string.IsNullOrEmpty(poad.County)))
                            conkRequest.customerContact.address[0].uKaddress.county = idDetails.ContactDetails.PostalAddresses[0].County;
                        if (idDetails.ContactDetails.PostalAddresses.ToList().Exists(poad => !string.IsNullOrEmpty(poad.PostCode)))
                            conkRequest.customerContact.address[0].uKaddress.postCode = idDetails.ContactDetails.PostalAddresses[0].PostCode;
                        if (idDetails.ContactDetails.PostalAddresses.ToList().Exists(poad => !string.IsNullOrEmpty(poad.SubBuildingName)))
                            conkRequest.customerContact.address[0].uKaddress.subBuildingName = idDetails.ContactDetails.PostalAddresses[0].SubBuildingName;
                        if (idDetails.ContactDetails.PostalAddresses.ToList().Exists(poad => !string.IsNullOrEmpty(poad.POBox)))
                            conkRequest.customerContact.address[0].uKaddress.poBox = idDetails.ContactDetails.PostalAddresses[0].POBox;
                    }
                    else
                    {
                        conkRequest.customerContact.address[0].addID = new BT.ESB.RoBT.MCCPV1.AddressIdentifier1();
                        conkRequest.customerContact.address[0].addID.id = "X05000000877";
                        conkRequest.customerContact.address[0].country = new BT.ESB.RoBT.MCCPV1.Country1();
                        conkRequest.customerContact.address[0].country.countryName = "United Kingdom";
                        conkRequest.customerContact.address[0].timeAtAddress = "24";
                        conkRequest.customerContact.address[0].uKaddress = new BT.ESB.RoBT.MCCPV1.UKPostalAddress1();
                        conkRequest.customerContact.address[0].uKaddress.buildingName = "Odeon";
                        conkRequest.customerContact.address[0].uKaddress.buildingNumber = "5";
                        conkRequest.customerContact.address[0].uKaddress.dependentThoroughfareName = "Stephens";
                        conkRequest.customerContact.address[0].uKaddress.thoroughfareName = "Hoop Lane";
                        conkRequest.customerContact.address[0].uKaddress.doubleDependentLocality = "Shelley Avenue";
                        conkRequest.customerContact.address[0].uKaddress.dependentLocality = "Mansion"; // need to check the locality as well
                        conkRequest.customerContact.address[0].uKaddress.postTown = "LONDON";
                        conkRequest.customerContact.address[0].uKaddress.county = "ESSEX";
                        conkRequest.customerContact.address[0].uKaddress.postCode = "SL9 0NE";
                        conkRequest.customerContact.address[0].uKaddress.subBuildingName = "Main";
                        conkRequest.customerContact.address[0].uKaddress.poBox = "40";
                    }

                    conkRequest.customerContact.person = new BT.ESB.RoBT.MCCPV1.Person1();
                    conkRequest.customerContact.person.dateOfBirthSpecified = true;
                    if (idDetails.ContactDetails.DOB != null)
                        conkRequest.customerContact.person.dateOfBirth = System.DateTime.Parse(idDetails.ContactDetails.DOB);
                    conkRequest.customerContact.person.personName = new BT.ESB.RoBT.MCCPV1.PersonName1();
                    if (idDetails.ContactDetails.FirstName != null)
                        conkRequest.customerContact.person.personName.forename = idDetails.ContactDetails.FirstName;
                    if (idDetails.ContactDetails.LastName != null)
                        conkRequest.customerContact.person.personName.surname = idDetails.ContactDetails.LastName;
                    if (idDetails.ContactDetails.Title != null)
                        conkRequest.customerContact.person.personName.title = idDetails.ContactDetails.Title;
                }

                ConkReq.createCustomerContactPersonRequest = conkRequest;

                e2eData.logMessage("StartedESBCall to creat Conkid", "");
                e2eData.startOutboundCall(ConfigurationManager.AppSettings["ESBSystemID"].ToString(), E2ETransaction.vREQ, "Reinstate Journey");

                using (ChannelFactory<BT.ESB.RoBT.MCCPV1.ManageCustomerContactPersonSyncPortType> factory = new ChannelFactory<BT.ESB.RoBT.MCCPV1.ManageCustomerContactPersonSyncPortType>("ManageCustomerContactPersonSyncPort"))
                {
                    BT.ESB.RoBT.MCCPV1.ManageCustomerContactPersonSyncPortType svc = factory.CreateChannel();

                    LogHelper.LogActivityDetails(bptmTxnId, guid, "Conkid reqeust", TimeSpan.Zero, "ESBCallsTrace", ConkReq.SerializeObject());
                    ConkRes = svc.createCustomerContactPerson(ConkReq);
                    LogHelper.LogActivityDetails(bptmTxnId, guid, "Conkid response", TimeSpan.Zero, "ESBCallsTrace", ConkRes.SerializeObject());
                    if (ConkRes != null && ConkRes.createCustomerContactPersonResponse != null && ConkRes.createCustomerContactPersonResponse.contact != null && !string.IsNullOrEmpty(ConkRes.createCustomerContactPersonResponse.contact.id))
                    {
                        conkid = ConkRes.createCustomerContactPersonResponse.contact.id;
                        isCAKCreated = true;

                        e2eData.logMessage("Got Response from ESB", "");                        
                        if (ConkRes.createCustomerContactPersonResponse.standardHeader != null && ConkRes.createCustomerContactPersonResponse.standardHeader.e2e != null && !string.IsNullOrEmpty(ConkRes.createCustomerContactPersonResponse.standardHeader.e2e.E2EDATA))
                        {
                            e2eData.endOutboundCall(ConkRes.createCustomerContactPersonResponse.standardHeader.e2e.E2EDATA);
                        }
                        else
                            e2eData.endOutboundCall(e2eData.toString());
                    }
                    else if (ConkRes != null && ConkRes.createCustomerContactPersonResponse != null && ConkRes.createCustomerContactPersonResponse.standardHeader != null
                        && ConkRes.createCustomerContactPersonResponse.standardHeader.serviceState != null && ConkRes.createCustomerContactPersonResponse.standardHeader.serviceState.errorCode != null && ConkRes.createCustomerContactPersonResponse.standardHeader.serviceState.errorDesc != null)
                    {
                        downStreamError = ConkRes.createCustomerContactPersonResponse.standardHeader.serviceState.errorCode + " : " + ConkRes.createCustomerContactPersonResponse.standardHeader.serviceState.errorDesc;
                        if (ConkRes.createCustomerContactPersonResponse.standardHeader != null && ConkRes.createCustomerContactPersonResponse.standardHeader.e2e != null && !string.IsNullOrEmpty(ConkRes.createCustomerContactPersonResponse.standardHeader.e2e.E2EDATA))
                        {
                            e2eData.endOutboundCall(ConkRes.createCustomerContactPersonResponse.standardHeader.e2e.E2EDATA);
                        }
                        else
                            e2eData.endOutboundCall(e2eData.toString());
                    }
                    else
                    {
                        downStreamError = "Conk creation failed:Null response from esb or no Conk returned during Conk creation";
                        e2eData.endOutboundCall(e2eData.toString());
                    }

                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return isCAKCreated;
        }

        public static bool CreateBillingAccountV1(string custid, string conkid, ref string bac, BT.SaaS.MSEOAdapter.ESB.ID.IdentityDetailType idDetails, Guid guid, string bptmTxnId, ref string downStreamError,ref E2ETransaction e2eData)
        {
            bool result = false;
            try
            {
                BT.ESB.RoBT.MCB.createCustomerBillingAccountRequest1 BACRequest = new BT.ESB.RoBT.MCB.createCustomerBillingAccountRequest1();
                BT.ESB.RoBT.MCB.createCustomerBillingAccountResponse1 BACResponse = new BT.ESB.RoBT.MCB.createCustomerBillingAccountResponse1();

                BT.ESB.RoBT.MCB.CreateCustomerBillingAccountRequest bacRequest = new BT.ESB.RoBT.MCB.CreateCustomerBillingAccountRequest();

                bacRequest.standardHeader = new BT.ESB.RoBT.MCB.StandardHeaderBlock();
                bacRequest.standardHeader.e2e = new BT.ESB.RoBT.MCB.E2E();
                string OVreferencenumber = "MSEOBACCreateORDER" + RandomNumber(1234567, 9999999);
                bacRequest.standardHeader.e2e.E2EDATA = e2eData.toString();//ConfigurationManager.AppSettings["E2EData"].ToString() + "CustomerReferenceNumber:" + OVreferencenumber;
                bacRequest.standardHeader.serviceAddressing = new BT.ESB.RoBT.MCB.ServiceAddressing();

                bacRequest.standardHeader.serviceState = new BT.ESB.RoBT.MCB.ServiceState();
                bacRequest.standardHeader.serviceState.stateCode = "OK";
                bacRequest.standardHeader.serviceAddressing = new BT.ESB.RoBT.MCB.ServiceAddressing();
                bacRequest.standardHeader.serviceAddressing.from = @"VasFulfilment";
                bacRequest.standardHeader.serviceAddressing.messageId = Guid.NewGuid().ToString();
                bacRequest.standardHeader.serviceAddressing.to = new BT.ESB.RoBT.MCB.AddressReference();
                bacRequest.standardHeader.serviceAddressing.to.address = @"http://cmpal.bt.com";
                bacRequest.standardHeader.serviceAddressing.serviceName = @"http://capabilities.nat.bt.com/ManageCustomerBilling";
                bacRequest.standardHeader.serviceAddressing.action = @"http://capabilities.nat.bt.com/wsdl/ManageCustomerBilling#createCustomerBillingAccountRequest";

                bacRequest.billAccount = new BT.ESB.RoBT.MCB.BillingAccount();
                bacRequest.billAccount.type = "BT Retail Customer";
                bacRequest.billAccount.ratingType = "Cyclic";
                bacRequest.billAccount.billDateDetails = new BT.ESB.RoBT.MCB.BillDateDetails();
                bacRequest.billAccount.billDateDetails.billFrequency = "Monthly";
                bacRequest.billAccount.partyBankAccount = new BT.ESB.RoBT.MCB.PartyBankAccount();
                bacRequest.billAccount.partyBankAccount = null;
                bacRequest.billAccount.preferredPaymentMethod = new BT.ESB.RoBT.MCB.PaymentMethod();
                bacRequest.billAccount.preferredPaymentMethod.code = "Cheque/Cash";
                bacRequest.billAccount.customerAccount = new BT.ESB.RoBT.MCB.CustomerAccount();
                bacRequest.billAccount.customerAccount.id = custid;//"4001309887";// conkid;//need to pass conkid
                bacRequest.billAccount.customerContact = new BT.ESB.RoBT.MCB.CustomerContact();
                bacRequest.billAccount.customerContact.id = conkid;//"2311428219";// custid;//need to pass custmerid
                bacRequest.billAccount.billProductionDetails = new BT.ESB.RoBT.MCB.BillProductionDetails();
                bacRequest.billAccount.billProductionDetails.billMedia = "Paper Free";

                bacRequest.billAccount.address = new BT.ESB.RoBT.MCB.Address();
                bacRequest.billAccount.address.address = new BT.ESB.RoBT.MCB.UKPostalAddress();

                if (idDetails != null && idDetails.ContactDetails != null && idDetails.ContactDetails.PostalAddresses != null && idDetails.ContactDetails.PostalAddresses.Count() > 0)
                {
                    if (idDetails.ContactDetails.PostalAddresses.ToList().Exists(poad => !string.IsNullOrEmpty(poad.BuildingName)))
                        bacRequest.billAccount.address.address.buildingName = idDetails.ContactDetails.PostalAddresses[0].BuildingName;
                    if (idDetails.ContactDetails.PostalAddresses.ToList().Exists(poad => !string.IsNullOrEmpty(poad.DependentThoroughFareName)))
                        bacRequest.billAccount.address.address.dependentThoroughfareName = idDetails.ContactDetails.PostalAddresses[0].DependentThoroughFareName;
                    if (idDetails.ContactDetails.PostalAddresses.ToList().Exists(poad => !string.IsNullOrEmpty(poad.ThoroughFareName)))
                        bacRequest.billAccount.address.address.thoroughfareName = idDetails.ContactDetails.PostalAddresses[0].ThoroughFareName;
                    if (idDetails.ContactDetails.PostalAddresses.ToList().Exists(poad => !string.IsNullOrEmpty(poad.DoubleDependentLocality)))
                        bacRequest.billAccount.address.address.doubleDependentLocality = idDetails.ContactDetails.PostalAddresses[0].DoubleDependentLocality;
                    if (idDetails.ContactDetails.PostalAddresses.ToList().Exists(poad => !string.IsNullOrEmpty(poad.Locality)))
                        bacRequest.billAccount.address.address.dependentLocality = idDetails.ContactDetails.PostalAddresses[0].Locality;
                    if (idDetails.ContactDetails.PostalAddresses.ToList().Exists(poad => !string.IsNullOrEmpty(poad.PostTown)))
                        bacRequest.billAccount.address.address.postTown = idDetails.ContactDetails.PostalAddresses[0].PostTown;
                    if (idDetails.ContactDetails.PostalAddresses.ToList().Exists(poad => !string.IsNullOrEmpty(poad.POBox)))
                        bacRequest.billAccount.address.address.poBox = idDetails.ContactDetails.PostalAddresses[0].POBox;

                    //bacRequest.billAccount.address.address.thoroughfareNumber = "7";// not sure from where need to read this value.

                    bacRequest.billAccount.address.addressIdentifier = new BT.ESB.RoBT.MCB.AddressIdentifier();
                    if (idDetails.ContactDetails.PostalAddresses.ToList().Exists(poad => !string.IsNullOrEmpty(poad.PostalAddressNADKey)))
                        bacRequest.billAccount.address.addressIdentifier.id = idDetails.ContactDetails.PostalAddresses[0].PostalAddressNADKey;
                    bacRequest.billAccount.address.country = new BT.ESB.RoBT.MCB.Country();
                    if (idDetails.ContactDetails.PostalAddresses.ToList().Exists(poad => !string.IsNullOrEmpty(poad.Country)))
                        bacRequest.billAccount.address.country.name = idDetails.ContactDetails.PostalAddresses[0].Country;
                }
                else
                {

                    bacRequest.billAccount.address.address.buildingName = "Odeon";
                    bacRequest.billAccount.address.address.dependentThoroughfareName = "Stephens";
                    bacRequest.billAccount.address.address.thoroughfareName = "Hoop Lane";
                    bacRequest.billAccount.address.address.doubleDependentLocality = "Shelley Avenue";
                    bacRequest.billAccount.address.address.dependentLocality = "Mansion";
                    bacRequest.billAccount.address.address.postTown = "LONDON";
                    bacRequest.billAccount.address.address.poBox = "40";

                    //bacRequest.billAccount.address.address.thoroughfareNumber = "7";// not sure from where need to read this value.

                    bacRequest.billAccount.address.addressIdentifier = new BT.ESB.RoBT.MCB.AddressIdentifier();
                    bacRequest.billAccount.address.addressIdentifier.id = "R00002503228";
                    bacRequest.billAccount.address.country = new BT.ESB.RoBT.MCB.Country();
                    bacRequest.billAccount.address.country.name = "United Kingdom";
                }

                BACRequest.createCustomerBillingAccountRequest = bacRequest;

                e2eData.logMessage("StartedESBCall to creat BAC", "");
                e2eData.startOutboundCall(ConfigurationManager.AppSettings["ESBSystemID"].ToString(), E2ETransaction.vREQ, "Reinstate Journey");

                using (ChannelFactory<BT.ESB.RoBT.MCB.ManageCustomerBillingSyncPortType> factory = new ChannelFactory<BT.ESB.RoBT.MCB.ManageCustomerBillingSyncPortType>("ManageCustomerBillingSyncPort"))
                {
                    BT.ESB.RoBT.MCB.ManageCustomerBillingSyncPortType svc = factory.CreateChannel();

                    LogHelper.LogActivityDetails(bptmTxnId, guid, "BAC Create reqeust", TimeSpan.Zero, "ESBCallsTrace", BACRequest.SerializeObject());
                    BACResponse = svc.createCustomerBillingAccount(BACRequest);
                    LogHelper.LogActivityDetails(bptmTxnId, guid, "BAC Create response", TimeSpan.Zero, "ESBCallsTrace", BACResponse.SerializeObject());

                    if (BACResponse != null && BACResponse.createCustomerBillingAccountResponse != null && BACResponse.createCustomerBillingAccountResponse.billAccount != null && BACResponse.createCustomerBillingAccountResponse.billAccount.accountNumber != null
                        && BACResponse.createCustomerBillingAccountResponse.billAccount.accountNumber.name.Equals("BillingAccountNumber", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(BACResponse.createCustomerBillingAccountResponse.billAccount.accountNumber.value))
                    {
                        bac = BACResponse.createCustomerBillingAccountResponse.billAccount.accountNumber.value;
                        result = true;
                        e2eData.logMessage("Got Response from ESB", "");
                        if (BACResponse.createCustomerBillingAccountResponse.standardHeader != null && BACResponse.createCustomerBillingAccountResponse.standardHeader.e2e != null && !string.IsNullOrEmpty(BACResponse.createCustomerBillingAccountResponse.standardHeader.e2e.E2EDATA))
                        {
                            e2eData.endOutboundCall(BACResponse.createCustomerBillingAccountResponse.standardHeader.e2e.E2EDATA);
                        }
                        else
                            e2eData.endOutboundCall(e2eData.toString());

                    }
                    else if (BACResponse != null && BACResponse.createCustomerBillingAccountResponse != null && BACResponse.createCustomerBillingAccountResponse.standardHeader != null
                        && BACResponse.createCustomerBillingAccountResponse.standardHeader.serviceState != null && BACResponse.createCustomerBillingAccountResponse.standardHeader.serviceState.errorCode != null && BACResponse.createCustomerBillingAccountResponse.standardHeader.serviceState.errorDesc != null)
                    {
                        downStreamError = BACResponse.createCustomerBillingAccountResponse.standardHeader.serviceState.errorCode + " : " + BACResponse.createCustomerBillingAccountResponse.standardHeader.serviceState.errorDesc;
                        e2eData.logMessage("Got Response from ESB", "");
                        if (BACResponse.createCustomerBillingAccountResponse.standardHeader != null && BACResponse.createCustomerBillingAccountResponse.standardHeader.e2e != null && !string.IsNullOrEmpty(BACResponse.createCustomerBillingAccountResponse.standardHeader.e2e.E2EDATA))
                        {
                            e2eData.endOutboundCall(BACResponse.createCustomerBillingAccountResponse.standardHeader.e2e.E2EDATA);
                        }
                        else
                            e2eData.endOutboundCall(e2eData.toString());
                    }
                    else
                    {
                        downStreamError = "BAC creation failed:Null response from esb or no BAC returned during BAC creation";
                        e2eData.endOutboundCall(e2eData.toString());
                    }

                }


            }
            catch (Exception ex)
            {
                throw ex;
            }
            return result;
        }

        #region BAC&email cease
        /// <summary>
        /// Cease bac and basic email mapper method.
        /// </summary>
        /// <returns></returns>
        public static requestOrder CeaseBACandEmailCeaseMapper(QueryCustomerResponse qryCustResponse, string emailType, ref string OVreferencenumber, ref string errorDescription)
        {
            requestOrder1 req = new requestOrder1();
            requestOrder requestOrder = new requestOrder();
            try
            {
                OVreferencenumber = "MSEOBACORDER" + RandomNumber(1234567, 9999999);

                if (qryCustResponse != null && qryCustResponse.customerAccount != null && qryCustResponse.customerAccount.billingAccountList != null && qryCustResponse.customerAccount.billingAccountList.Count() > 0 && qryCustResponse.customerAccount.productInstanceList != null && qryCustResponse.customerAccount.productInstanceList.Count() > 0)
                {
                    BT.ESB.RoBT.ManageCustomerOrder.StandardHeaderBlock headerBlock = new BT.ESB.RoBT.ManageCustomerOrder.StandardHeaderBlock();
                    headerBlock.e2e = new BT.ESB.RoBT.ManageCustomerOrder.E2E();
                    headerBlock.e2e.E2EDATA = ConfigurationManager.AppSettings["E2EData"].ToString() + "CustomerReferenceNumber:" + OVreferencenumber;

                    Logger.Write("ESB CeaseBAC call OV referenceNumber: " + OVreferencenumber + " with E2E Data: " + headerBlock.e2e.E2EDATA, Logger.TypeEnum.MessageTrace);

                    headerBlock.serviceAddressing = new BT.ESB.RoBT.ManageCustomerOrder.ServiceAddressing();
                    headerBlock.serviceAddressing.from = @"VasFulfilment";
                    headerBlock.serviceAddressing.to = new BT.ESB.RoBT.ManageCustomerOrder.AddressReference();
                    headerBlock.serviceAddressing.to.address = @"http://cmpal.bt.com";
                    headerBlock.serviceAddressing.messageId = Guid.NewGuid().ToString();
                    headerBlock.serviceAddressing.serviceName = @"http://capabilities.nat.bt.com/ManageCustomerOrder";
                    headerBlock.serviceAddressing.action = @"http://capabilities.nat.bt.com/wsdl/ManageCustomerOrder#requestOrder";

                    headerBlock.serviceState = new BT.ESB.RoBT.ManageCustomerOrder.ServiceState();
                    headerBlock.serviceState.stateCode = "OK";

                    headerBlock.serviceSpecification = new BT.ESB.RoBT.ManageCustomerOrder.ServiceSpecification();
                    headerBlock.serviceSpecification.version = "Version 2.0";

                    RequestOrderReq orderRequest = new RequestOrderReq();
                    orderRequest.order = new Order1();

                    orderRequest.order.action = new OrderAction();
                    orderRequest.order.action.code = codeOrderActionEnum.Cancel;

                    orderRequest.order.fromParty = new BT.ESB.RoBT.ManageCustomerOrder.Party[1];
                    orderRequest.order.fromParty[0] = new BT.ESB.RoBT.ManageCustomerOrder.Party();
                    orderRequest.order.fromParty[0].identifier = new PartyIdentifier();
                    orderRequest.order.fromParty[0].identifier.name = "CustomerId";
                    orderRequest.order.fromParty[0].identifier.value = qryCustResponse.customerAccount.id;

                    orderRequest.order.thirdPartyChannel = new BT.ESB.RoBT.ManageCustomerOrder.Party[2];
                    orderRequest.order.thirdPartyChannel[0] = new BT.ESB.RoBT.ManageCustomerOrder.Party();
                    orderRequest.order.thirdPartyChannel[0].identifier = new PartyIdentifier();
                    orderRequest.order.thirdPartyChannel[0].identifier.name = "SalesPartner";
                    orderRequest.order.thirdPartyChannel[0].identifier.value = "000";

                    orderRequest.order.thirdPartyChannel[1] = new BT.ESB.RoBT.ManageCustomerOrder.Party();
                    orderRequest.order.thirdPartyChannel[1].name = new BT.ESB.RoBT.ManageCustomerOrder.PartyName();
                    orderRequest.order.thirdPartyChannel[1].name.nameElement = "SalesChannel";
                    orderRequest.order.thirdPartyChannel[1].name.nameValue = "Bulk Order";

                    orderRequest.order.orderDate = System.DateTime.Today;

                    orderRequest.order.alternativeIdentifier = new BT.ESB.RoBT.ManageCustomerOrder.GlobalIdentifier[1];
                    orderRequest.order.alternativeIdentifier[0] = new BT.ESB.RoBT.ManageCustomerOrder.GlobalIdentifier();
                    orderRequest.order.alternativeIdentifier[0].type = "CustomerReferenceNumber";
                    orderRequest.order.alternativeIdentifier[0].value = OVreferencenumber;

                    orderRequest.order.billingAccount = new BT.ESB.RoBT.ManageCustomerOrder.BillingAccount();
                    orderRequest.order.billingAccount.accountNumber = qryCustResponse.customerAccount.billingAccountList[0].accountNumber;

                    List<OrderItem1> orderItemList = new List<OrderItem1>();
                    OrderItem1 orderItem = null;
                    foreach (ProductInstance1 prodInstance in qryCustResponse.customerAccount.productInstanceList)
                    {
                        if (prodInstance != null && prodInstance.id != null && prodInstance.productName != null && prodInstance.billingAccount != null)
                        {
                            if (prodInstance.productName.name.Equals("Narrowband Service", StringComparison.OrdinalIgnoreCase)
                                || prodInstance.productName.name.Equals("ISP Service", StringComparison.OrdinalIgnoreCase)
                                || prodInstance.productName.name.Equals("BT Premium Mail", StringComparison.OrdinalIgnoreCase)
                                || prodInstance.productName.name.Equals("BT BASIC MAIL", StringComparison.OrdinalIgnoreCase)
                                || prodInstance.productName.name.Equals("Basic Email", StringComparison.OrdinalIgnoreCase)
                                || prodInstance.productName.name.Equals("Premium Email", StringComparison.OrdinalIgnoreCase))
                            {
                                orderItem = new OrderItem1();
                                orderItem.committedDateTimeSpecified = true;                                
                                orderItem.requiredDateTimeSpecified = true;
                                orderItem.committedDateTime = System.DateTime.Now;
                                orderItem.requiredDateTime = System.DateTime.Now;

                                orderItem.quantity = 1;

                                orderItem.action = new BT.ESB.RoBT.ManageCustomerOrder.OrderItemAction();

                                orderItem.action.reason = "Cease BAC & Email Service as last email hard deleted.";

                                orderItem.action.code = BT.ESB.RoBT.ManageCustomerOrder.codeOrderItemActionEnum.Cancel;
                                orderItem.action.codeSpecified = true;

                                orderItem.fromReference = "1220579";
                                orderItem.ceaseInstruction = "Default";

                                orderItem.billingAccount = new BT.ESB.RoBT.ManageCustomerOrder.BillingAccount();
                                orderItem.billingAccount.accountNumber = prodInstance.billingAccount.accountNumber;

                                if (prodInstance.productName.name.Equals("Narrowband Service", StringComparison.OrdinalIgnoreCase))
                                {
                                    orderItem.specification = new BT.ESB.RoBT.ManageCustomerOrder.Specification[1];
                                    orderItem.specification[0] = new BT.ESB.RoBT.ManageCustomerOrder.Specification();
                                    orderItem.specification[0].name = prodInstance.productName.name;
                                    orderItem.specification[0].specCategory = new BT.ESB.RoBT.ManageCustomerOrder.SpecCategory();
                                    orderItem.specification[0].specCategory.code = prodInstance.productName.code;
                                    orderItem.instance = new BT.ESB.RoBT.ManageCustomerOrder.Instance[1];
                                    orderItem.instance[0] = new BT.ESB.RoBT.ManageCustomerOrder.Instance();

                                    if (prodInstance.productCharge != null && !string.IsNullOrEmpty(prodInstance.productCharge.priceType))
                                    {
                                        orderItem.priceType = prodInstance.productCharge.priceType;
                                    }
                                    orderItem.nRCPaymentMethod = null;
                                    orderItem.quantitySpecified = true;
                                    orderItem.quantity = 1;
                                    List<BT.ESB.RoBT.ManageCustomerOrder.InstanceIdentifier> instanceidentifierlist = new List<BT.ESB.RoBT.ManageCustomerOrder.InstanceIdentifier>();
                                    BT.ESB.RoBT.ManageCustomerOrder.InstanceIdentifier instanceidentifier = null;

                                    instanceidentifier = new BT.ESB.RoBT.ManageCustomerOrder.InstanceIdentifier();
                                    instanceidentifier.name = "AssetId";
                                    if (prodInstance.id.ToList().Exists(id => id.name.Equals("IntegrationId", StringComparison.OrdinalIgnoreCase)))
                                    {
                                        instanceidentifier.value = prodInstance.id.ToList().Where(id => id.name.Equals("IntegrationId", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                    }
                                    instanceidentifierlist.Add(instanceidentifier);

                                    instanceidentifier = new BT.ESB.RoBT.ManageCustomerOrder.InstanceIdentifier();
                                    instanceidentifier.name = "PromotionId";
                                    if (prodInstance.id.ToList().Exists(id => id.name.Equals("PromotionIntegrationId", StringComparison.OrdinalIgnoreCase)))
                                    {
                                        instanceidentifier.value = prodInstance.id.ToList().Where(id => id.name.Equals("PromotionIntegrationId", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                    }
                                    instanceidentifierlist.Add(instanceidentifier);

                                    instanceidentifier = new BT.ESB.RoBT.ManageCustomerOrder.InstanceIdentifier();
                                    instanceidentifier.name = "PreviousPromotionId";
                                    if (prodInstance.id.ToList().Exists(id => id.name.Equals("PromotionIntegrationId", StringComparison.OrdinalIgnoreCase)))
                                    {
                                        instanceidentifier.value = prodInstance.id.ToList().Where(id => id.name.Equals("PromotionIntegrationId", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                    }
                                    instanceidentifierlist.Add(instanceidentifier);
                                    orderItem.instance[0].instanceIdentifier = instanceidentifierlist.ToArray();
                                }
                                else if ((prodInstance.productName.name.Equals("BT Premium Mail", StringComparison.OrdinalIgnoreCase) || prodInstance.productName.name.Equals("Premium Email", StringComparison.OrdinalIgnoreCase))
                                    && prodInstance.productName.name.Contains(emailType))
                                {
                                    orderItem.specification = new BT.ESB.RoBT.ManageCustomerOrder.Specification[1];
                                    orderItem.specification[0] = new BT.ESB.RoBT.ManageCustomerOrder.Specification();
                                    orderItem.specification[0].name = prodInstance.productName.name;
                                    orderItem.specification[0].specCategory = new BT.ESB.RoBT.ManageCustomerOrder.SpecCategory();
                                    orderItem.specification[0].specCategory.code = prodInstance.productName.code;
                                    orderItem.instance = new BT.ESB.RoBT.ManageCustomerOrder.Instance[1];
                                    orderItem.instance[0] = new BT.ESB.RoBT.ManageCustomerOrder.Instance();

                                    List<BT.ESB.RoBT.ManageCustomerOrder.InstanceIdentifier> instanceidentifierlist = new List<BT.ESB.RoBT.ManageCustomerOrder.InstanceIdentifier>();
                                    BT.ESB.RoBT.ManageCustomerOrder.InstanceIdentifier instanceidentifier = null;

                                    instanceidentifier = new BT.ESB.RoBT.ManageCustomerOrder.InstanceIdentifier();
                                    instanceidentifier.name = "AssetId";
                                    if (prodInstance.id.ToList().Exists(id => id.name.Equals("IntegrationId", StringComparison.OrdinalIgnoreCase)))
                                    {
                                        instanceidentifier.value = prodInstance.id.ToList().Where(id => id.name.Equals("IntegrationId", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                    }

                                    if (prodInstance.productCharge != null && !string.IsNullOrEmpty(prodInstance.productCharge.priceType))
                                    {
                                        orderItem.priceType = prodInstance.productCharge.priceType;
                                    }
                                    orderItem.nRCPaymentMethod = null;
                                    orderItem.quantitySpecified = true;
                                    orderItem.quantity = 1;

                                    instanceidentifierlist.Add(instanceidentifier);
                                    orderItem.instance[0].instanceIdentifier = instanceidentifierlist.ToArray();
                                    orderItem.priceType = prodInstance.productCharge.priceType;
                                }
                                else if ((prodInstance.productName.name.Equals("BT BASIC MAIL", StringComparison.OrdinalIgnoreCase) || prodInstance.productName.name.Equals("Basic Email", StringComparison.OrdinalIgnoreCase))
                                    && prodInstance.productName.name.Contains(emailType))
                                {
                                    orderItem.specification = new BT.ESB.RoBT.ManageCustomerOrder.Specification[1];
                                    orderItem.specification[0] = new BT.ESB.RoBT.ManageCustomerOrder.Specification();
                                    orderItem.specification[0].name = prodInstance.productName.name;
                                    orderItem.specification[0].specCategory = new BT.ESB.RoBT.ManageCustomerOrder.SpecCategory();
                                    orderItem.specification[0].specCategory.code = prodInstance.productName.code;
                                    orderItem.instance = new BT.ESB.RoBT.ManageCustomerOrder.Instance[1];
                                    orderItem.instance[0] = new BT.ESB.RoBT.ManageCustomerOrder.Instance();

                                    List<BT.ESB.RoBT.ManageCustomerOrder.InstanceIdentifier> instanceidentifierlist = new List<BT.ESB.RoBT.ManageCustomerOrder.InstanceIdentifier>();
                                    BT.ESB.RoBT.ManageCustomerOrder.InstanceIdentifier instanceidentifier = null;

                                    instanceidentifier = new BT.ESB.RoBT.ManageCustomerOrder.InstanceIdentifier();
                                    instanceidentifier.name = "AssetId";
                                    if (prodInstance.id.ToList().Exists(id => id.name.Equals("IntegrationId", StringComparison.OrdinalIgnoreCase)))
                                    {
                                        instanceidentifier.value = prodInstance.id.ToList().Where(id => id.name.Equals("IntegrationId", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                    }

                                    if (prodInstance.productCharge != null && !string.IsNullOrEmpty(prodInstance.productCharge.priceType))
                                    {
                                        orderItem.priceType = prodInstance.productCharge.priceType;
                                    }
                                    orderItem.nRCPaymentMethod = null;
                                    orderItem.quantitySpecified = true;
                                    orderItem.quantity = 1;

                                    instanceidentifierlist.Add(instanceidentifier);
                                    orderItem.instance[0].instanceIdentifier = instanceidentifierlist.ToArray();
                                    orderItem.priceType = prodInstance.productCharge.priceType;
                                }
                                else if (prodInstance.productName.name.Equals("ISP Service", StringComparison.OrdinalIgnoreCase))
                                {
                                    orderItem.specification = new BT.ESB.RoBT.ManageCustomerOrder.Specification[1];
                                    orderItem.specification[0] = new BT.ESB.RoBT.ManageCustomerOrder.Specification();
                                    orderItem.specification[0].name = prodInstance.productName.name;
                                    orderItem.specification[0].specCategory = new BT.ESB.RoBT.ManageCustomerOrder.SpecCategory();
                                    orderItem.specification[0].specCategory.code = prodInstance.productName.code;
                                    orderItem.instance = new BT.ESB.RoBT.ManageCustomerOrder.Instance[1];
                                    orderItem.instance[0] = new BT.ESB.RoBT.ManageCustomerOrder.Instance();

                                    orderItem.instance[0].instanceCharactersticList = new BT.ESB.RoBT.ManageCustomerOrder.InstanceCharacteristic[1];
                                    orderItem.instance[0].instanceCharactersticList[0] = new BT.ESB.RoBT.ManageCustomerOrder.InstanceCharacteristic();
                                    orderItem.instance[0].instanceCharactersticList[0].action = new BT.ESB.RoBT.ManageCustomerOrder.InstanceAction();
                                    orderItem.instance[0].instanceCharactersticList[0].action.code = BT.ESB.RoBT.ManageCustomerOrder.codeInstanceActionEnum.Delete;
                                    orderItem.instance[0].instanceCharactersticList[0].name = "ispServiceClass";
                                    orderItem.instance[0].instanceCharactersticList[0].value = "ISP_SC_PM";
                                    orderItem.instance[0].instanceCharactersticList[0].previousValue = "ISP_SC_PM";

                                    if (prodInstance.productCharge != null && !string.IsNullOrEmpty(prodInstance.productCharge.priceType))
                                    {
                                        orderItem.priceType = prodInstance.productCharge.priceType;
                                    }
                                    orderItem.nRCPaymentMethod = null;
                                    orderItem.quantitySpecified = true;
                                    orderItem.quantity = 1;

                                    List<BT.ESB.RoBT.ManageCustomerOrder.InstanceIdentifier> instanceidentifierlist = new List<BT.ESB.RoBT.ManageCustomerOrder.InstanceIdentifier>();
                                    BT.ESB.RoBT.ManageCustomerOrder.InstanceIdentifier instanceidentifier = null;

                                    instanceidentifier = new BT.ESB.RoBT.ManageCustomerOrder.InstanceIdentifier();
                                    instanceidentifier.name = "AssetId";
                                    if (prodInstance.id.ToList().Exists(id => id.name.Equals("IntegrationId", StringComparison.OrdinalIgnoreCase)))
                                    {
                                        instanceidentifier.value = prodInstance.id.ToList().Where(id => id.name.Equals("IntegrationId", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                    }
                                    instanceidentifierlist.Add(instanceidentifier);

                                    instanceidentifier = new BT.ESB.RoBT.ManageCustomerOrder.InstanceIdentifier();
                                    instanceidentifier.name = "PromotionId";
                                    if (prodInstance.id.ToList().Exists(id => id.name.Equals("PromotionIntegrationId", StringComparison.OrdinalIgnoreCase)))
                                    {
                                        instanceidentifier.value = prodInstance.id.ToList().Where(id => id.name.Equals("PromotionIntegrationId", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                    }
                                    instanceidentifierlist.Add(instanceidentifier);

                                    instanceidentifier = new BT.ESB.RoBT.ManageCustomerOrder.InstanceIdentifier();
                                    instanceidentifier.name = "PreviousPromotionId";
                                    if (prodInstance.id.ToList().Exists(id => id.name.Equals("PromotionIntegrationId", StringComparison.OrdinalIgnoreCase)))
                                    {
                                        instanceidentifier.value = prodInstance.id.ToList().Where(id => id.name.Equals("PromotionIntegrationId", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                    }
                                    instanceidentifierlist.Add(instanceidentifier);

                                    instanceidentifier = new BT.ESB.RoBT.ManageCustomerOrder.InstanceIdentifier();
                                    instanceidentifier.name = "ServiceId";
                                    if (prodInstance.id.ToList().Exists(id => id.name.Equals("ServiceId", StringComparison.OrdinalIgnoreCase)))
                                    {
                                        instanceidentifier.value = prodInstance.id.ToList().Where(id => id.name.Equals("ServiceId", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                    }
                                    instanceidentifierlist.Add(instanceidentifier);
                                    orderItem.instance[0].instanceIdentifier = instanceidentifierlist.ToArray();
                                }
                                orderItemList.Add(orderItem);
                            }
                        }
                        else
                        {
                            errorDescription = "haven't received product details in querycustomer response from ESB to cease the BAC";
                        }
                    }

                    orderRequest.order.orderItem = orderItemList.ToArray();

                    orderRequest.notification = new BT.ESB.RoBT.ManageCustomerOrder.Notification();
                    orderRequest.notification.notificationType = "Description";

                    requestOrder.standardHeader = headerBlock;
                    requestOrder.orderRequest = orderRequest;
                }
            }
            catch (Exception ex)
            {
                errorDescription = errorDescription + " : " + ex.Message.ToString();
                throw ex;
            }

            req.requestOrder = requestOrder;
            return requestOrder;
        }
        #endregion

        #region BAC&Email cease.
        /// <summary>
        /// create bac and basic email using request order.
        /// </summary>
        /// <returns></returns>
        public static bool CeaseBACandEmailServicecalltoESB(QueryCustomerResponse qryCustResponse, Guid guid, string emailType, ref string downStreamError)
        {
            bool result = false;
            try
            {
                string url = ConfigurationManager.AppSettings["ESBrequestorderurl"].ToString();
                string responseXml;
                string orderref = string.Empty;
                string errorDescription = string.Empty;
                requestOrder esbRequestOrder = new requestOrder();
                esbRequestOrder = CeaseBACandEmailCeaseMapper(qryCustResponse, emailType, ref orderref, ref downStreamError);

                string xmlns = "<requestOrder xmlns=\"http://capabilities.nat.bt.com/xsd/ManageCustomerOrder/2018/02/28\">";
                string oldXmlns = "<requestOrder xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">";
                //string oldXmlns = "<requestOrder xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">";
                string header = "<NS1:Envelope xmlns:NS1=\"http://schemas.xmlsoap.org/soap/envelope/\"><NS1:Body>";
                string footer = "</NS1:Body></NS1:Envelope>";

                string xml = ObjectSerializer.SerializeObject(esbRequestOrder);
                xml = xml.Replace(oldXmlns, xmlns);
                xml = xml.Replace("<?xml version=\"1.0\" encoding=\"utf-16\"?>", "");
                string finalrequestxml = header + xml + footer;

                LogHelper.LogActivityDetails(orderref, guid, "Cease Email request", TimeSpan.Zero, "ESBCallsTrace", finalrequestxml);

                HttpWebRequest webrequest;

                webrequest = (HttpWebRequest)WebRequest.Create(url);//"http://10.52.16.67:61001/RoBTESB/Retail/CMP/Capability/ManageCustomerOrder");
                webrequest.Method = "POST";
                webrequest.ContentType = "application/xml";
                webrequest.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["CMPSTimeOut"].ToString());

                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
                UTF8Encoding encoding = new UTF8Encoding();
                byte[] postBytes = encoding.GetBytes(finalrequestxml);
                webrequest.ContentLength = postBytes.Length;

                Stream postStream = webrequest.GetRequestStream();
                postStream.Write(postBytes, 0, postBytes.Length);
                postStream.Close();

                using (HttpWebResponse webResponse = (HttpWebResponse)webrequest.GetResponse())
                {
                    using (StreamReader readStream = new StreamReader(webResponse.GetResponseStream()))
                    {
                        responseXml = readStream.ReadToEnd();

                        LogHelper.LogActivityDetails(orderref, guid, "Cease Email response", TimeSpan.Zero, "ESBCallsTrace", responseXml);
                    }
                }
                if (responseXml.Contains("<NS3:stateCode>OK</NS3:stateCode>"))
                {
                    result = true;
                }
                else
                {
                    downStreamError = downStreamError+" : "+ responseXml.ToString();
                    result = false;
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            return result;
        }
        #endregion
        //modified CON-73882 - start
        public static string APIGWKillSessionRequest(string BBSID, string baseUrl, string method)
        {
            string getapiresponseXml = string.Empty;
            string postapiresponseXml = string.Empty;
            //For Post method body passing for {}
            string jsonPayload = "{}";

            int retryCount = 0;
            Guid APIGWTracker = Guid.NewGuid();
            string url = $"{baseUrl}{BBSID}/disconnect";
            string result = string.Empty;
            HttpWebRequest webrequest = (HttpWebRequest)WebRequest.Create(url);
            webrequest.Method = method;
            webrequest.ContentType = "application/json";
            webrequest.Accept = "application/json";
            webrequest.Headers.Add("APIGW-Tracking-Header", APIGWTracker.ToString());
            Guid guid = Guid.NewGuid();
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            string certificatePath = ConfigurationManager.AppSettings["APIGWcertpath"].ToString();
            string password = ConfigurationManager.AppSettings["APIGWcertpassword"].ToString();
            X509Certificate2 certificate = new X509Certificate2(certificatePath, password);
            webrequest.ClientCertificates.Add(certificate);
            
            //Additionally we are adding the below parameters if method=POST
            if (webrequest.Method == "POST")
            { 
                //here we are sending empty braces 
                webrequest.ContentLength = jsonPayload.Length;

            // Write the JSON payload to the request stream
            using (StreamWriter writer = new StreamWriter(webrequest.GetRequestStream()))
            {
                writer.Write(jsonPayload);
            }
          }

            

            BT.Helpers.RESTHelper restHelpers = new RESTHelper();
            restHelpers.WriteToFile("APIGWKillsession method :::::URL is "+ url + "APIGWTrackingHeader is::"+ APIGWTracker.ToString());
            LogHelper.LogActivityDetails("bptmTxnId", guid, "Inside APIGWKillSessionRequest method", TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", "APIGWTracker"+ APIGWTracker.ToString());
            webrequest.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["KillsessionTimeOut"].ToString());
            try
            {
                restHelpers.WriteToFile("APIGWKillsession method :::::Before executing (HttpWebResponse)webrequest.GetResponse()"+ webrequest.ToString());
                using (var webResponse = (HttpWebResponse)webrequest.GetResponse())
                {
                    restHelpers.WriteToFile("Inside (HttpWebResponse)webrequest.GetResponse()" + webrequest.ToString());
                    if (method == "GET")
                    {
                     //added webResponse.StatusCode == HttpStatusCode.Created, as radius sends 200 ok response for APIGW
                       /* if (webResponse.StatusCode == HttpStatusCode.Accepted || webResponse.StatusCode == HttpStatusCode.OK || webResponse.StatusCode == HttpStatusCode.Created)//added webResponse.StatusCode == HttpStatusCode.Created, as radius sends 200 ok response for APIGW
                        {
                            using (StreamReader readStream = new StreamReader(webResponse.GetResponseStream()))
                            {
                                getapiresponseXml = readStream.ReadToEnd();
                                LogHelper.LogActivityDetails(BBSID, guid, "APIGWKillSession Success Response", TimeSpan.Zero, "ESBCallsTrace", getapiresponseXml);
                                LogHelper.LogActivityDetails("bptmTxnId", guid, "Successful Killsessionservice in APIGWKillSession", TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", getapiresponseXml);
                                readStream.Close();
                            }
                            JsonResponse getresponsejson = JsonConvert.DeserializeObject<JsonResponse>(getapiresponseXml);
                            if (getresponsejson != null)
                            {
                                if (getresponsejson.state == "20200")
                                    result = "Success";
                            }
                            restHelpers.WriteToFile("Inside GET APIGWKillsession" + webResponse.StatusCode+ "webResponse.statusdescription"+ getresponsejson.message + "apiresponseXml::"+ getapiresponseXml);
                            LogHelper.LogActivityDetails("bptmTxnId", guid, "Success GET APIGWKillSession result is: " + result, TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", "");
                            result = "Success";
                        }//Failures
                        else 
                        { */
                            using (StreamReader readStream = new StreamReader(webResponse.GetResponseStream()))
                            {
                                getapiresponseXml = readStream.ReadToEnd();
                                LogHelper.LogActivityDetails(BBSID, guid, "APIGWKillSession Success Response from POST", TimeSpan.Zero, "ESBCallsTrace", postapiresponseXml);
                                LogHelper.LogActivityDetails("bptmTxnId", guid, "Successful Killsessionservice in APIGWKillSession from POST", TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", postapiresponseXml);
                                JsonResponse getresponsejson = JsonConvert.DeserializeObject<JsonResponse>(getapiresponseXml);

                                if (getresponsejson != null)
                                {   
                                        result = getresponsejson.state;
                                }
                                //Here adding GET CALL failed description
                                restHelpers.WriteToFile("Inside GET APIGWKillsession" + getresponsejson.state + "webResponse.statusdescription" + getresponsejson.message);
                                LogHelper.LogActivityDetails("bptmTxnId", guid, "Failed GET APIGWKillSession ErrorDescription  is: " + getresponsejson.message, TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", "");
                               
                            }
                        return result;
                        // }
                    }
                    if(method == "POST")
                    {    //added webResponse.StatusCode == HttpStatusCode.Created, as radius sends 202 ok response for APIGW
                        /*if (webResponse.StatusCode == HttpStatusCode.Accepted || webResponse.StatusCode == HttpStatusCode.OK || webResponse.StatusCode == HttpStatusCode.Created)//added webResponse.StatusCode == HttpStatusCode.Created, as radius sends 202 ok response for APIGW
                        {
                            using (StreamReader readStream = new StreamReader(webResponse.GetResponseStream()))
                            {
                                postapiresponseXml = readStream.ReadToEnd();
                                LogHelper.LogActivityDetails(BBSID, guid, "APIGWKillSession Success Response from POST", TimeSpan.Zero, "ESBCallsTrace", postapiresponseXml);
                                LogHelper.LogActivityDetails("bptmTxnId", guid, "Successful Killsessionservice in APIGWKillSession from POST", TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", postapiresponseXml);
                              // if(postapiresponseXml!=null)
                                JsonResponse responsejson = JsonConvert.DeserializeObject<JsonResponse>(postapiresponseXml);

                                if (responsejson != null)
                                {
                                    result= responsejson.state;
                                    //if(responsejson.state == "20200")
                                    //    result = "Success";
                                }
                                restHelpers.WriteToFile("Inside POST APIGWKillsession POST responsejson.state" + responsejson.state + "responsejson.message:::" + responsejson.message + "apiresponseXml ::" + postapiresponseXml);
                                return result; // = "Success";
                                LogHelper.LogActivityDetails("bptmTxnId", guid, "Success POST APIGWKillSession result is: " + result, TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", "");
                            }
                        }//Failures
                        else
                        { */
                            using (StreamReader readStream = new StreamReader(webResponse.GetResponseStream()))
                            {
                                postapiresponseXml = readStream.ReadToEnd();
                                LogHelper.LogActivityDetails(BBSID, guid, "APIGWKillSession Success Response from POST", TimeSpan.Zero, "ESBCallsTrace", postapiresponseXml);
                                LogHelper.LogActivityDetails("bptmTxnId", guid, "Successful Killsessionservice in APIGWKillSession from POST", TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", postapiresponseXml);
                                JsonResponse responsejson = JsonConvert.DeserializeObject<JsonResponse>(postapiresponseXml);

                                if (responsejson != null)
                                {
                                    result = responsejson.state;
                                }
                                restHelpers.WriteToFile("Inside POST APIGWKillsession webResponse.StatusCode " + responsejson.state + "webResponse.statusdescription" + responsejson.message);
                                LogHelper.LogActivityDetails("bptmTxnId", guid, "Failed POST APIGWKillSession ErrorDescription is: " + webResponse.StatusDescription.ToString(), TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", "");
                                //Here adding POST CALL failed description
                            }
                        return result; //moved to after webResponse.Close();
                        //}
                    }
                    //webResponse.Close();
                    //return result;
                }
                
            }
            catch (Exception ex)
            {
                restHelpers.WriteToFile(" APIGW Inside catch balock" + ex.Message.ToString());
                LogHelper.LogActivityDetails("APIWKIllSESSIONRequestMethod", guid, "In Catch block KillSession Response"+ex.Message.ToString(), TimeSpan.Zero, "ESBCallsTrace", BBSID);
                result = "APIWKIllSESSIONRequestMethodk in Catch block failed with exception"+ex.Message.ToString();
                return result;
            }
            return result;
        }

        public static void KillSessionRequest(string BBSID, string e2eData,string Action,string Reason,string OrderKey)
        {

            int retryCount = 0;
            while (retryCount <= ReadConfig.MAX_RETRY_COUNT)
            {
                string responseXml = string.Empty;
                Guid guid = Guid.NewGuid();
                try
                {
                    HttpWebRequest webrequest;
                    string uri = ConfigurationManager.AppSettings["killsessionurl"];
                   
                    string requestxml = string.Format("<urn:AddSessionKillRequest xmlns:urn=\"urn:com.btwholesale.Assurance4-v5-0\" xmlns:stan=\"http://wsi.nat.bt.com/2005/06/StandardHeader/\" xmlns:ass=\"urn:org.uk.telcob2b/tML/Assurance.ServiceAction4-v5-0\" xmlns:NS1=\"http://schemas.xmlsoap.org/soap/envelope/\"><stan:standardHeader><stan:e2e><stan:E2EDATA>11=REQ,13=logicalQueue:MSEO/ManageTroubleTOS2/B2BGateway,15=MSEO:app09704,16=logicalQueue:MSEO/ManageTroubleTOS2/B2BGateway,{0}</stan:E2EDATA></stan:e2e><stan:serviceState><stan:stateCode>OK</stan:stateCode></stan:serviceState><stan:serviceAddressing><stan:from>MSEO</stan:from><stan:to><stan:address>http://ccm.intra.bt.com/robtesb</stan:address><stan:contextItemList><stan:contextItem contextId=\"String\" contextName=\"ClientConversationID\">SessionRestart</stan:contextItem></stan:contextItemList></stan:to><stan:messageId>MSEO:{1}-29:1</stan:messageId><stan:action/></stan:serviceAddressing></stan:standardHeader><ass:ServiceAction><ass:ServiceActionHeader><ass:ServiceActionRequesterId>MSEO:{1}-29:1</ass:ServiceActionRequesterId><ass:ServiceActionDate>2020-08-27T22:19:10.000+01:00</ass:ServiceActionDate><ass:ServiceActionReference><ass:ConductorReference><ass:InvokeIdentifier><ass:RefNum>{1}</ass:RefNum></ass:InvokeIdentifier></ass:ConductorReference></ass:ServiceActionReference><ass:ServiceActionParty><ass:ConductorParty><ass:Party PartyID=\"{3}\" AgencyID=\"DUNS\"/></ass:ConductorParty><ass:PerformerParty><ass:Party PartyID=\"232510151\" AgencyID=\"DUNS\"/></ass:PerformerParty></ass:ServiceActionParty></ass:ServiceActionHeader><ass:ListOfServiceActionDetail><ass:ServiceActionDetail><ass:ServiceActionCategory>PPPSessionKill</ass:ServiceActionCategory><ass:ServiceType>21C_WBC_SERVICE</ass:ServiceType><ass:ToBeServiceActionedMorts><ass:Mort><ass:ServiceID><ass:Ident>{2}</ass:Ident></ass:ServiceID></ass:Mort></ass:ToBeServiceActionedMorts></ass:ServiceActionDetail></ass:ListOfServiceActionDetail></ass:ServiceAction></urn:AddSessionKillRequest>", e2eData, ReferenceNumber(), BBSID, ConfigurationManager.AppSettings["DUNSID"]);
                    LogHelper.LogActivityDetails(BBSID, guid, "InsideESBKillSession Method", TimeSpan.Zero, "ESBCallsTrace", requestxml);
                    LogHelper.LogActivityDetails(BBSID, guid, "InsideESBKillSession Method", TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", requestxml);

                    webrequest = (HttpWebRequest)WebRequest.Create(uri);
                    webrequest.Method = "POST";
                    webrequest.ContentType = "application/xml ";
                    webrequest.Accept = "application/xml ";
                    webrequest.Headers.Add("e2eData", e2eData);
                    webrequest.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["KillsessionTimeOut"].ToString());

                    LogHelper.LogActivityDetails(BBSID, guid, "ESBKillSession Request", TimeSpan.Zero, "ESBCallsTrace", requestxml);
                    LogHelper.LogActivityDetails(BBSID, guid, "ESBKillSession Request", TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", requestxml);

                    ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
                    UTF8Encoding encoding = new UTF8Encoding();
                    byte[] postBytes = encoding.GetBytes(requestxml);
                    webrequest.ContentLength = postBytes.Length;

                    Stream postStream = webrequest.GetRequestStream();
                    postStream.Write(postBytes, 0, postBytes.Length);
                    postStream.Close();


                    try
                    {
                        using (HttpWebResponse webResponse = (HttpWebResponse)webrequest.GetResponse())
                        {
                            
                            if (webResponse.StatusCode == HttpStatusCode.Accepted || webResponse.StatusCode == HttpStatusCode.OK || webResponse.StatusCode == HttpStatusCode.Created)//added webResponse.StatusCode == HttpStatusCode.Created, as esb sends 201 ok response for ManageServiceRoleRequest
                            {
                                using (StreamReader readStream = new StreamReader(webResponse.GetResponseStream()))
                                {
                                    responseXml = readStream.ReadToEnd();
                                    LogHelper.LogActivityDetails(BBSID, guid, "KillSession Success Response", TimeSpan.Zero, "ESBCallsTrace", responseXml);
                                    LogHelper.LogActivityDetails("bptmTxnId", guid, "Successful Killsessionservice in ESB", TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", responseXml);
                                    readStream.Close();
                                }
                                webResponse.Close();

                            }
                            else
                            {
                               // responseXml = readStream.ReadToEnd();
                                retryCount = ReadConfig.MAX_RETRY_COUNT + 1;
                                if (retryCount > ReadConfig.MAX_RETRY_COUNT)
                                {
                                    //Capture the failed log
                                    LogHelper.LogActivityDetails("bptmTxnId", guid, "FailedAt :ESBKillSession" + " Killsessionservice method ErrorDescription is: " + webResponse.StatusDescription.ToString() + "Action is " + Action + "Reason is " + Reason + "BBSID :" + BBSID + ": " + "OrderKey::" + OrderKey, TimeSpan.Zero, "ESBCallsTrace", responseXml);
                                    LogHelper.LogActivityDetails("bptmTxnId", guid, "FailedAt :ESBKillSession" + " Killsessionservice method ErrorDescription is: " + webResponse.StatusDescription.ToString() + "Action is " + Action + "Reason is " + Reason + "BBSID :" + BBSID +": "+"OrderKey::"+OrderKey, TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", responseXml);
                                }
                            }
                        }
                    }
                    catch (TimeoutException ex)
                    {
                        retryCount++;
                        System.Threading.Thread.Sleep(ReadConfig.SLEEP_TIME_OUT);
                        if (retryCount > ReadConfig.MAX_RETRY_COUNT)
                        {
                            LogHelper.LogActivityDetails("bptmTxnId", Guid.NewGuid(), "FailedAt :ESBKillSession" + " TimeoutException in Catch KillSession method ErrorDescription is: " + ex.Message.ToString() + "Action is " + Action + "Reason is " + Reason + "BBSID :" + BBSID + ": ", TimeSpan.Zero, "ESBCallsTrace", OrderKey);
                            LogHelper.LogActivityDetails("bptmTxnId", Guid.NewGuid(), "FailedAt :ESBKillSession" + " TimeoutException in Catch KillSession method ErrorDescription is: " + ex.Message.ToString() + "Action is " + Action + "Reason is " + Reason + "BBSID :" + BBSID + ": ", TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", OrderKey);
                            LogHelper.LogActivityDetails(BBSID, guid, "KillSession error Response", TimeSpan.Zero, "ESBCallsTrace", ex.Message.ToString());
                        }
                    }

                }
                //catch (WebException cmpsEx)
                //{
                //    retryCount++;
                //    System.Threading.Thread.Sleep(ReadConfig.SLEEP_TIME_OUT);
                //    if (retryCount > ReadConfig.MAX_RETRY_COUNT)
                //    {
                //        if (cmpsEx.Response != null)
                //        {
                //            string statusCode = string.Empty;
                //            responseXml = string.Empty;
                //            using (HttpWebResponse errorResponse = (HttpWebResponse)cmpsEx.Response)
                //            {
                //                statusCode = errorResponse.StatusCode.ToString();
                //                using (StreamReader readStream = new StreamReader(errorResponse.GetResponseStream()))
                //                {
                //                    responseXml = readStream.ReadToEnd();
                //                    LogHelper.LogActivityDetails(BBSID, guid, "KillSession error Response", TimeSpan.Zero, "ESBCallsTrace", responseXml);
                //                    readStream.Close();
                //                }
                //                errorResponse.Close();
                //            }
                //            //e2eData.businessError(GotResponseFromCMPSWithBusinessError, "CMPSException: " + statusCode + " : " + responseXml);
                //            //e2eData.downstreamError(DownStreamError, "CMPSException: " + statusCode + " : " + responseXml);
                //            //string E2EData = E2ETransaction.getSimulatedReply(e2eData.toString(), E2ETransaction.vRES, null);
                //            //e2eData.endOutboundCall(E2EData);
                //        }
                //        else
                //        {
                //            LogHelper.LogActivityDetails(BBSID, guid, "KillSession error Response", TimeSpan.Zero, "ESBCallsTrace", cmpsEx.Message.ToString());
                //            //e2eData.businessError(GotResponseFromCMPSWithBusinessError, "CMPSException: " + cmpsEx.Message);
                //            //e2eData.downstreamError(DownStreamError, "CMPSException: " + cmpsEx.Message);
                //            //string E2EData = E2ETransaction.getSimulatedReply(e2eData.toString(), E2ETransaction.vRES, null);
                //            //e2eData.endOutboundCall(E2EData);
                //        }
                //    }
                //}
                catch (Exception cmpsException)
                {
                    retryCount++;
                    System.Threading.Thread.Sleep(ReadConfig.SLEEP_TIME_OUT);
                    if (retryCount > ReadConfig.MAX_RETRY_COUNT)
                    {
                        LogHelper.LogActivityDetails("bptmTxnId", Guid.NewGuid(), "FailedAt :ESBKillSession" + " exception in Catch KillSession method ErrorDescription is: " + cmpsException.Message.ToString() + "Action is " + Action + "Reason is " + Reason + "BBSID :" + BBSID + ": ", TimeSpan.Zero, "ESBCallsTrace", OrderKey);
                        LogHelper.LogActivityDetails("bptmTxnId", Guid.NewGuid(), "FailedAt :ESBKillSession" + " exception in Catch KillSession method ErrorDescription is: " + cmpsException.Message.ToString() + "Action is " + Action + "Reason is " + Reason + "BBSID :" + BBSID + ": ", TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", OrderKey);
                    }
                }

            }//end while
        }
        //modified CON-73882 - end
        public static string RandomNumber(int min, int max)
        {
            Random random = new Random();
            return random.Next(min, max).ToString();
        }
        public static string ReferenceNumber()
        {
            Random ran = new Random();
            String b = "abcdefghijklmnopqrstuvwxyz0123456789";
            int length = 11;
            String random = "";

            for (int i = 0; i < length; i++)
            {
                int a = ran.Next(b.Length); 
                random = random + b.ElementAt(a);
            }

            return random;
        }
        private static bool ValidateRemoteCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors policyErrors)
        {
            return true;
        }
        /// <summary>
        /// APIGW POST & GET Responses reading from JSON
        /// </summary>
        public class JsonResponse
        {
            public string message { get; set; }
            public string state { get; set; }
        }
    }
}
