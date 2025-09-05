using System;
using System.Configuration;
using BT.SaaS.IspssAdapter.Dnp;
using BT.SaaS.Core.MDMAPI.Entities.Customer;
using System.Collections.Generic;
using System.ServiceModel.Channels;
using System.ServiceModel;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Net;
using System.IO;

namespace BT.SaaS.IspssAdapter
{
   
    public sealed class DnpWrapper
    {
        private static string MQLogs = string.Empty;
         

        public static void GetVasUsage(List<ProductServiceInstanceEx> serviceInstances, out int totalQuantity, out int freeQuantity)
        {
            getServiceInstanceRequest1 request;
            getServiceInstanceResponse1 response;
            StandardHeaderBlock header = new StandardHeaderBlock();
            totalQuantity = 0;
            freeQuantity = 0;
            int licenceQuantity = 1;
            int allocatedQuantity = 0;

            request = new getServiceInstanceRequest1();
            request.getServiceInstanceRequest = new GetServiceInstanceRequest();

            request.getServiceInstanceRequest.standardHeader = header;
            request.getServiceInstanceRequest.standardHeader.serviceState = new ServiceState();
            request.getServiceInstanceRequest.standardHeader.serviceAddressing = new ServiceAddressing();
            request.getServiceInstanceRequest.standardHeader.serviceAddressing.from = "http://www.profile.com?SAASUI";

            string basicAuthenticationCredentials = ConfigurationManager.AppSettings["DnPUser"].ToString() + ":" + ConfigurationManager.AppSettings["DnPPassword"].ToString();

            foreach (ProductServiceInstanceEx serviceInstance in serviceInstances)
            {
                request.getServiceInstanceRequest.getServiceInstanceReq = new GetServiceInstanceReq();
                request.getServiceInstanceRequest.getServiceInstanceReq.clientServiceInstanceSearchCriteria = new ClientServiceInstanceSearchCriteria();

                request.getServiceInstanceRequest.getServiceInstanceReq.clientServiceInstanceSearchCriteria.serviceInstanceKey = serviceInstance.ServiceInstanceKey;
                request.getServiceInstanceRequest.getServiceInstanceReq.clientServiceInstanceSearchCriteria.serviceCode = serviceInstance.ServiceCode;

                byte[] credBuffer = new UTF8Encoding().GetBytes(basicAuthenticationCredentials);
                HttpRequestMessageProperty httpRequest = new HttpRequestMessageProperty();
                httpRequest.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(credBuffer));
                try
                {

                    using (ChannelFactory<ServiceProfileSyncPortType> factory = new ChannelFactory<ServiceProfileSyncPortType>("ServiceProfile_DnP"))
                    {
                        ServiceProfileSyncPortType svc = factory.CreateChannel();

                        using (OperationContextScope scope = new OperationContextScope((IContextChannel)svc))
                        {
                            OperationContext.Current.OutgoingMessageProperties.Add(HttpRequestMessageProperty.Name, httpRequest);
                            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                            response = svc.getServiceInstance(request);
                        }
                    }
                }
                catch (Exception e)
                {
                    throw new DnpException("DnpWrapper.GetVasUsage() failed to call D&P: " + e.Message, e);
                }

                if (response != null && response.getServiceInstanceResponse != null && response.getServiceInstanceResponse.standardHeader != null &&
                response.getServiceInstanceResponse.standardHeader.serviceState != null)
                {
                    if (response.getServiceInstanceResponse.standardHeader.serviceState.stateCode == "0")
                    {
                        if (response.getServiceInstanceResponse.getServiceInstanceRes.clientServiceInstance[0].clientServiceInstanceRoleOwnerShip[0].numberOwned != null)
                        {
                            licenceQuantity = Convert.ToInt32(response.getServiceInstanceResponse.getServiceInstanceRes.clientServiceInstance[0].clientServiceInstanceRoleOwnerShip[0].numberOwned);
                            totalQuantity += licenceQuantity;
                        }
                        if (response.getServiceInstanceResponse.getServiceInstanceRes.clientServiceInstance[0].clientServiceInstanceRoleOwnerShip[0].numberAssigned != null)
                        {
                            allocatedQuantity += Convert.ToInt32(response.getServiceInstanceResponse.getServiceInstanceRes.clientServiceInstance[0].clientServiceInstanceRoleOwnerShip[0].numberAssigned);
                        }
                    }
                    else
                    {
                        throw new DnpException("DnpWrapper.GetVasUsage() Failed to get service instances from Dnp. stateCode=" +
                                                response.getServiceInstanceResponse.standardHeader.serviceState.stateCode + " " +
                                                ((response.getServiceInstanceResponse.getServiceInstanceRes != null) ? getDnpMessage(response.getServiceInstanceResponse.getServiceInstanceRes.messages) : ""));
                    }
                }
                else
                {
                    throw new DnpException("Failed to get service instances from Dnp");
                }
            }

            freeQuantity = totalQuantity - allocatedQuantity;
        }

        public static List<string> GetAliasIdentities(string btconnectId)
        {
            List<string> aliasIdentities = new List<string>();

            getClientProfileRequest1 request = new getClientProfileRequest1();
            getClientProfileResponse1 response;

            StandardHeaderBlock header = new StandardHeaderBlock();
            header.serviceState = new ServiceState();
            header.serviceAddressing = new ServiceAddressing();
            header.serviceAddressing.from = "http://www.profile.com?SAASUI";

            request.getClientProfileRequest = new GetClientProfileRequest();

            request.getClientProfileRequest.standardHeader = header;

            GetClientProfileReq getClientProfileReq = new GetClientProfileReq();
            request.getClientProfileRequest.getClientProfileReq = getClientProfileReq;

            ClientSearchCriteria clientSearchCriteria = new ClientSearchCriteria()
            {
                identifierDomainCode = "BTCOM",
                identifierValue = btconnectId,
                universalid = ""
            };

            ResponseParameters responseParameters = new ResponseParameters()
            {
                isClientCredentialsRequired = "NO",
                isClientServiceInstanceCharacteristicsrequired = "NO",
                isClientIdentitiesRequired = "YES",
                isClientServiceInstanceRequired = "NO",
                isClientServiceRoleCharacteristicRequired = "NO",
                isClientServiceRolesRequired = "NO",
                isLinkedProfilesRequired = "NO"
            };

            getClientProfileReq.clientSearchCriteria = clientSearchCriteria;
            getClientProfileReq.responseParameter = responseParameters;

            string basicAuthenticationCredentials = ConfigurationManager.AppSettings["DnPUser"].ToString() + ":" + ConfigurationManager.AppSettings["DnPPassword"].ToString();

            byte[] credBuffer = new UTF8Encoding().GetBytes(basicAuthenticationCredentials);
            HttpRequestMessageProperty httpRequest = new HttpRequestMessageProperty();
            httpRequest.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(credBuffer));
            try
            {

                using (ChannelFactory<ClientProfileSyncPortType> factory = new ChannelFactory<ClientProfileSyncPortType>("ClientProfile_DnP"))
                {
                    ClientProfileSyncPortType svc = factory.CreateChannel();

                    using (OperationContextScope scope = new OperationContextScope((IContextChannel)svc))
                    {
                        OperationContext.Current.OutgoingMessageProperties.Add(HttpRequestMessageProperty.Name, httpRequest);
                        System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                        response = svc.getClientProfile(request);
                    }
                }
            }
            catch (Exception e)
            {
                throw new DnpException("DnpWrapper.GetAliasIdentities() failed to call D&P: " + e.Message, e);
            }

            if (response != null &&
                response.getClientProfileResponse != null &&
                response.getClientProfileResponse.getClientProfileRes != null &&
                response.getClientProfileResponse.standardHeader != null &&
                response.getClientProfileResponse.standardHeader.serviceState != null)
            {
                if (response.getClientProfileResponse.standardHeader.serviceState.stateCode == "0")
                {
                    ClientProfile profile = response.getClientProfileResponse.getClientProfileRes.clientProfile;
                    ClientIdentity[] identities = profile.client.clientIdentity;
                    if (identities != null)
                    {
                        foreach (ClientIdentity identity in identities)
                        {

                            if (identity.identityAlias != null && identity.identityAlias.ToUpper().Equals("YES"))
                            {
                                aliasIdentities.Add(identity.value);
                            }
                        }
                    }
                }
                else
                {
                    throw new DnpException("DnpWrapper.GetAliasIdentities() failed to get getClientIdentifiers from Dnp. stateCode=" +
                    response.getClientProfileResponse.standardHeader.serviceState.stateCode + " " +
                    ((response.getClientProfileResponse.getClientProfileRes != null) ? getDnpMessage(response.getClientProfileResponse.getClientProfileRes.messages) : ""));

                }
            }
            else
            {
                throw new DnpException("DnpWrapper.GetAliasIdentities() failed to get getClientIdentifiers from Dnp. null in response.");
            }
            return aliasIdentities;
        }

        private static string getDnpMessage(BT.SaaS.IspssAdapter.Dnp.Message[] messages)
        {
            StringBuilder messageStringBuilder = new StringBuilder();

            if (messages != null)
            {
                foreach (BT.SaaS.IspssAdapter.Dnp.Message message in messages)
                {
                    messageStringBuilder.Append("Code=" + message.messageCode + " Severity=" + message.messageSeverity + " Description=" + message.description + " ");
                }
            }

            return messageStringBuilder.ToString();
        }

        public static ClientServiceInstance[] GetClientServiceInstances(string client, string domain)
        {

            getClientProfileRequest1 request = null;
            getClientProfileResponse1 response = null;
            ClientServiceInstance[] serviceInstances = null;

            request = new getClientProfileRequest1();
            request.getClientProfileRequest = new GetClientProfileRequest();
            request.getClientProfileRequest.standardHeader = new StandardHeaderBlock();
            request.getClientProfileRequest.standardHeader.serviceState = new ServiceState();
            request.getClientProfileRequest.standardHeader.serviceState.stateCode = "OK";

            request.getClientProfileRequest.standardHeader.serviceAddressing = new ServiceAddressing();
            request.getClientProfileRequest.standardHeader.serviceAddressing.from = "http://www.profile.com?SAASUI";

            request.getClientProfileRequest.getClientProfileReq = new GetClientProfileReq();
            request.getClientProfileRequest.getClientProfileReq.responseParameter = new ResponseParameters();


            request.getClientProfileRequest.getClientProfileReq.responseParameter.isClientIdentitiesRequired = "NO";
            request.getClientProfileRequest.getClientProfileReq.responseParameter.isClientServiceRolesRequired = "YES";
            request.getClientProfileRequest.getClientProfileReq.responseParameter.isClientServiceRoleCharacteristicRequired = "NO";
            request.getClientProfileRequest.getClientProfileReq.responseParameter.isLinkedProfilesRequired = "NO";
            request.getClientProfileRequest.getClientProfileReq.responseParameter.isClientServiceInstanceRequired = "YES";
            request.getClientProfileRequest.getClientProfileReq.responseParameter.isClientServiceInstanceCharacteristicsrequired = "NO";

            request.getClientProfileRequest.getClientProfileReq.clientSearchCriteria = new ClientSearchCriteria();
            request.getClientProfileRequest.getClientProfileReq.clientSearchCriteria.identifierDomainCode = domain;
            request.getClientProfileRequest.getClientProfileReq.clientSearchCriteria.identifierValue = client;
            request.getClientProfileRequest.getClientProfileReq.clientSearchCriteria.universalid = "";

            string basicAuthenticationCredentials = ConfigurationManager.AppSettings["DnPUser"].ToString() + ":" + ConfigurationManager.AppSettings["DnPPassword"].ToString();

            byte[] credBuffer = new UTF8Encoding().GetBytes(basicAuthenticationCredentials);
            HttpRequestMessageProperty httpRequest = new HttpRequestMessageProperty();
            httpRequest.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(credBuffer));
            using (ChannelFactory<ClientProfileSyncPortType> factory = new ChannelFactory<ClientProfileSyncPortType>("ClientProfile_DnP"))
            {
                ClientProfileSyncPortType svc = factory.CreateChannel();
                using (OperationContextScope scope = new OperationContextScope((IContextChannel)svc))
                {
                    OperationContext.Current.OutgoingMessageProperties.Add(HttpRequestMessageProperty.Name, httpRequest);
                    System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                    response = svc.getClientProfile(request);
                }
            }

            if (response != null && response.getClientProfileResponse != null && response.getClientProfileResponse.standardHeader != null && response.getClientProfileResponse.getClientProfileRes != null && response.getClientProfileResponse.getClientProfileRes.messages != null && response.getClientProfileResponse.getClientProfileRes.messages[0] != null &&
                    String.IsNullOrEmpty(response.getClientProfileResponse.getClientProfileRes.messages[0].messageCode))
            {
                serviceInstances = response.getClientProfileResponse.getClientProfileRes.clientProfile.clientServiceInstance;
            }
            else
            {
                string errorMsg = string.Empty;
                if ((response != null) && (response.getClientProfileResponse != null)
                    && (response.getClientProfileResponse.getClientProfileRes != null)
                    && (response.getClientProfileResponse.getClientProfileRes.messages != null))
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (BT.SaaS.IspssAdapter.Dnp.Message msg in response.getClientProfileResponse.getClientProfileRes.messages)
                    {
                        sb.AppendFormat("|{0}|", msg.description);
                    }
                    errorMsg = sb.ToString();
                }

                throw new DnpException("Error getting the client service instances: ErrorCode:  " +
                     response.getClientProfileResponse.standardHeader.serviceState.stateCode + " " +
                     "ErrorMessage : " + errorMsg);
            }
            return serviceInstances;
        }

        /// <summary>
        /// call to get client charactestics from DNP
        /// </summary>
        /// <param name="userId"></param>
        /// <returns>List of Client Charatcerstics</returns>
        public static List<ClientCharacteristic> GetClientCharacterstics(string customerKey)
        {
            getClientProfileRequest1 request = null;
            getClientProfileResponse1 response = null;
            List<ClientCharacteristic> clientChar = new List<ClientCharacteristic>();

            request = new getClientProfileRequest1();
            request.getClientProfileRequest = new GetClientProfileRequest();
            request.getClientProfileRequest.standardHeader = new StandardHeaderBlock();
            //req.getClientProfileRequest.standardHeader.e2e = new E2E();
            //req.getClientProfileRequest.standardHeader.e2e.E2EDATA = null;
            request.getClientProfileRequest.standardHeader.serviceState = new ServiceState();
            request.getClientProfileRequest.standardHeader.serviceState.stateCode = "OK";

            request.getClientProfileRequest.standardHeader.serviceAddressing = new ServiceAddressing();
            request.getClientProfileRequest.standardHeader.serviceAddressing.from = "http://www.profile.com?SAASUI";

            request.getClientProfileRequest.getClientProfileReq = new GetClientProfileReq();
            request.getClientProfileRequest.getClientProfileReq.responseParameter = new ResponseParameters();


            request.getClientProfileRequest.getClientProfileReq.responseParameter.isClientIdentitiesRequired = "YES";
            request.getClientProfileRequest.getClientProfileReq.responseParameter.isClientServiceRolesRequired = "NO";
            request.getClientProfileRequest.getClientProfileReq.responseParameter.isClientServiceRoleCharacteristicRequired = "NO";
            request.getClientProfileRequest.getClientProfileReq.responseParameter.isLinkedProfilesRequired = "YES";
            request.getClientProfileRequest.getClientProfileReq.responseParameter.isClientServiceInstanceRequired = "NO";
            request.getClientProfileRequest.getClientProfileReq.responseParameter.isClientServiceInstanceCharacteristicsrequired = "NO";

            request.getClientProfileRequest.getClientProfileReq.clientSearchCriteria = new ClientSearchCriteria();
            request.getClientProfileRequest.getClientProfileReq.clientSearchCriteria.identifierDomainCode = "CRMCUSTOMERKEY";
            request.getClientProfileRequest.getClientProfileReq.clientSearchCriteria.identifierValue = customerKey;
            request.getClientProfileRequest.getClientProfileReq.clientSearchCriteria.universalid = "";

            string basicAuthenticationCredentials = ConfigurationManager.AppSettings["DnPUser"].ToString() + ":" + ConfigurationManager.AppSettings["DnPPassword"].ToString();

            byte[] credBuffer = new UTF8Encoding().GetBytes(basicAuthenticationCredentials);
            HttpRequestMessageProperty httpRequest = new HttpRequestMessageProperty();
            httpRequest.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(credBuffer));
            using (ChannelFactory<ClientProfileSyncPortType> factory = new ChannelFactory<ClientProfileSyncPortType>("ClientProfile_DnP"))
            {
                ClientProfileSyncPortType svc = factory.CreateChannel();
                using (OperationContextScope scope = new OperationContextScope((IContextChannel)svc))
                {
                    OperationContext.Current.OutgoingMessageProperties.Add(HttpRequestMessageProperty.Name, httpRequest);
                    System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                    response = svc.getClientProfile(request);
                }
            }

            if (response != null && response.getClientProfileResponse != null
                && response.getClientProfileResponse.getClientProfileRes != null
                && response.getClientProfileResponse.getClientProfileRes.clientProfile != null)
            {
                var clientProfile = response.getClientProfileResponse.getClientProfileRes.clientProfile;
                if (clientProfile.client != null && clientProfile.client.clientCharacteristic != null && clientProfile.client.clientCharacteristic.Length > 0)
                {
                    clientChar = clientProfile.client.clientCharacteristic.ToList();
                }
            }
            else
            {
                throw new DnpException(string.Format("Response is Null from DNP for the Customer : ", customerKey));
            }

            return clientChar;
        }

        /*
        public static List<string> GetAliasIdentities(string btconnectId)
        {
            List<string> aliasIdentities = new List<string>();

            getClientIdentifiersRequest1 request = new getClientIdentifiersRequest1();
            getClientIdentifiersResponse1 response;
            StandardHeaderBlock header = new StandardHeaderBlock();

            request.getClientIdentifiersRequest = new GetClientIdentifiersRequest();

            request.getClientIdentifiersRequest.standardHeader = header;
            request.getClientIdentifiersRequest.standardHeader.serviceState = new ServiceState();
            request.getClientIdentifiersRequest.standardHeader.serviceAddressing = new ServiceAddressing();
            request.getClientIdentifiersRequest.standardHeader.serviceAddressing.from = "http://www.profile.com?SAASUI";
            
            request.getClientIdentifiersRequest.getClientIdentifierReq = new GetClientIdentifiersReq();
            request.getClientIdentifiersRequest.getClientIdentifierReq.clientSearchCriteria = new ClientSearchCriteria();

            request.getClientIdentifiersRequest.getClientIdentifierReq.clientSearchCriteria.identifierValue = btconnectId;
            request.getClientIdentifiersRequest.getClientIdentifierReq.clientSearchCriteria.identifierDomainCode = "BTCOM";
            request.getClientIdentifiersRequest.getClientIdentifierReq.clientSearchCriteria.universalid = "";
            
            string basicAuthenticationCredentials = ConfigurationManager.AppSettings["DnPUser"].ToString() + ":" + ConfigurationManager.AppSettings["DnPPassword"].ToString();

            byte[] credBuffer = new UTF8Encoding().GetBytes(basicAuthenticationCredentials);
            HttpRequestMessageProperty httpRequest = new HttpRequestMessageProperty();
            httpRequest.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(credBuffer));
            try
            {

                using (ChannelFactory<ClientProfileSyncPortType> factory = new ChannelFactory<ClientProfileSyncPortType>("ClientProfile_DnP"))
                {
                    ClientProfileSyncPortType svc = factory.CreateChannel();

                    using (OperationContextScope scope = new OperationContextScope((IContextChannel)svc))
                    {
                        OperationContext.Current.OutgoingMessageProperties.Add(HttpRequestMessageProperty.Name, httpRequest);
                        response = svc.getClientIdentifiers(request);
                    }
                }
            }
            catch (Exception e)
            {
                throw new DnpException("DnpWrapper.GetAliasIdentities() failed to call D&P: " + e.Message, e);
            }

            if (response != null && 
                response.getClientIdentifiersResponse != null && 
                response.getClientIdentifiersResponse.getClientIdentifiersRes != null &&
                response.getClientIdentifiersResponse.standardHeader != null &&
                response.getClientIdentifiersResponse.standardHeader.serviceState != null)
            {
                if (response.getClientIdentifiersResponse.standardHeader.serviceState.stateCode == "0")
                {
                    ClientIdentity[] identities = response.getClientIdentifiersResponse.getClientIdentifiersRes.clientIdentity;
                    if (identities != null)
                    {
                        foreach (ClientIdentity identity in identities)
                        {
                            if (identity.identityAlias.ToUpper().Equals("Y"))
                            {
                                aliasIdentities.Add(identity.value);
                            }
                        }
                    }
                }
                else
                {
                    throw new DnpException("DnpWrapper.GetAliasIdentities() failed to get getClientIdentifiers from Dnp. stateCode=" +
                    response.getClientIdentifiersResponse.standardHeader.serviceState.stateCode );   
                }
            }
            else
            {
                throw new DnpException("DnpWrapper.GetAliasIdentities() failed to get getClientIdentifiers from Dnp. null in response.");
            }
            return aliasIdentities;
        }
        */


        public static bool getProfileLinkedClients(string identity, string identityDomain, ref GetClientProfileResponse profile)
        {
            profile = null;
            getClientProfileResponse1 profileResponse;
            getClientProfileRequest1 request1 = new getClientProfileRequest1();
            request1.getClientProfileRequest = new GetClientProfileRequest();
            request1.getClientProfileRequest.standardHeader = new StandardHeaderBlock();
            request1.getClientProfileRequest.standardHeader.serviceState = new ServiceState();
            request1.getClientProfileRequest.standardHeader.serviceAddressing = new ServiceAddressing();
            request1.getClientProfileRequest.standardHeader.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
            request1.getClientProfileRequest.getClientProfileReq = new GetClientProfileReq();
            request1.getClientProfileRequest.getClientProfileReq.clientSearchCriteria = new ClientSearchCriteria();
            request1.getClientProfileRequest.getClientProfileReq.clientSearchCriteria.identifierValue = identity;
            request1.getClientProfileRequest.getClientProfileReq.clientSearchCriteria.identifierDomainCode = identityDomain;
            request1.getClientProfileRequest.getClientProfileReq.responseParameter = new ResponseParameters();
            request1.getClientProfileRequest.getClientProfileReq.responseParameter.isClientIdentitiesRequired = "NO";
            request1.getClientProfileRequest.getClientProfileReq.responseParameter.isClientCredentialsRequired = "NO";
            request1.getClientProfileRequest.getClientProfileReq.responseParameter.isLinkedProfilesRequired = "YES";
            request1.getClientProfileRequest.getClientProfileReq.responseParameter.isClientServiceInstanceRequired = "NO";
            request1.getClientProfileRequest.getClientProfileReq.responseParameter.isClientServiceRolesRequired = "NO";
            request1.getClientProfileRequest.getClientProfileReq.responseParameter.isClientServiceRoleCharacteristicRequired = "NO";

            string basicAuthenticationCredentials = ConfigurationManager.AppSettings["DnPUser"].ToString() + ":" + ConfigurationManager.AppSettings["DnPPassword"].ToString();
            byte[] credBuffer = new UTF8Encoding().GetBytes(basicAuthenticationCredentials);
            HttpRequestMessageProperty httpRequest = new HttpRequestMessageProperty();
            httpRequest.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(credBuffer));

            using (ChannelFactory<ClientProfileSyncPortType> factory = new ChannelFactory<ClientProfileSyncPortType>(@"ClientProfile_DnP"))
            {

                ClientProfileSyncPortType svc = factory.CreateChannel();
                using (OperationContextScope scope = new OperationContextScope((IContextChannel)svc))
                {
                    OperationContext.Current.OutgoingMessageProperties.Add(HttpRequestMessageProperty.Name, httpRequest);
                    System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                    profileResponse = svc.getClientProfile(request1);
                }

                if (profileResponse != null
                    && profileResponse.getClientProfileResponse != null
                    && profileResponse.getClientProfileResponse.standardHeader != null
                    && profileResponse.getClientProfileResponse.standardHeader.serviceState != null
                    && profileResponse.getClientProfileResponse.standardHeader.serviceState.stateCode != null
                    && profileResponse.getClientProfileResponse.standardHeader.serviceState.stateCode == "0")
                {
                    if (profileResponse.getClientProfileResponse.getClientProfileRes != null
                        && profileResponse.getClientProfileResponse.getClientProfileRes.clientProfile != null
                        && profileResponse.getClientProfileResponse.getClientProfileRes.clientProfile.client != null

                        )
                    {
                        profile = profileResponse.getClientProfileResponse;
                        return true;
                    }
                }
                else
                {
                    string errorMsg = string.Empty;
                    StringBuilder sb = new StringBuilder();
                    foreach (BT.SaaS.IspssAdapter.Dnp.Message msg in profileResponse.getClientProfileResponse.getClientProfileRes.messages)
                    {
                        sb.AppendFormat("|{0}|", msg.description);
                    }
                    errorMsg = sb.ToString();

                    if (profileResponse != null && profileResponse.getClientProfileResponse != null && profileResponse.getClientProfileResponse.getClientProfileRes != null && profileResponse.getClientProfileResponse.getClientProfileRes.messages != null)
                    {
                        profile = profileResponse.getClientProfileResponse;

                    }
                    throw new DnpException("Error getting the Profile Linked Clients:" + errorMsg);

                }
            }
            return false;
        }

        public static List<ClientIdentityValidation> GetClientIdentityCharacterstics(string identity, string identityDomain)
        {
            getClientProfileRequest1 request = null;
            getClientProfileResponse1 response = null;
            List<ClientIdentityValidation> clientIdentityValidationList = new List<ClientIdentityValidation>();

            request = new getClientProfileRequest1();
            request.getClientProfileRequest = new GetClientProfileRequest();
            request.getClientProfileRequest.standardHeader = new StandardHeaderBlock();
            request.getClientProfileRequest.standardHeader.serviceState = new ServiceState();
            request.getClientProfileRequest.standardHeader.serviceState.stateCode = "OK";

            request.getClientProfileRequest.standardHeader.serviceAddressing = new ServiceAddressing();
            request.getClientProfileRequest.standardHeader.serviceAddressing.from = "http://www.profile.com?SAASUI";

            request.getClientProfileRequest.getClientProfileReq = new GetClientProfileReq();
            request.getClientProfileRequest.getClientProfileReq.responseParameter = new ResponseParameters();

            request.getClientProfileRequest.getClientProfileReq.responseParameter.isClientIdentitiesRequired = "YES";
            request.getClientProfileRequest.getClientProfileReq.responseParameter.isClientServiceRolesRequired = "NO";
            request.getClientProfileRequest.getClientProfileReq.responseParameter.isClientServiceRoleCharacteristicRequired = "NO";
            request.getClientProfileRequest.getClientProfileReq.responseParameter.isLinkedProfilesRequired = "NO";
            request.getClientProfileRequest.getClientProfileReq.responseParameter.isClientServiceInstanceRequired = "NO";
            request.getClientProfileRequest.getClientProfileReq.responseParameter.isClientServiceInstanceCharacteristicsrequired = "NO";

            request.getClientProfileRequest.getClientProfileReq.clientSearchCriteria = new ClientSearchCriteria();
            request.getClientProfileRequest.getClientProfileReq.clientSearchCriteria.identifierDomainCode = identityDomain;
            request.getClientProfileRequest.getClientProfileReq.clientSearchCriteria.identifierValue = identity;
            request.getClientProfileRequest.getClientProfileReq.clientSearchCriteria.universalid = "";

            string basicAuthenticationCredentials = ConfigurationManager.AppSettings["DnPUser"].ToString() + ":" + ConfigurationManager.AppSettings["DnPPassword"].ToString();

            byte[] credBuffer = new UTF8Encoding().GetBytes(basicAuthenticationCredentials);
            HttpRequestMessageProperty httpRequest = new HttpRequestMessageProperty();
            httpRequest.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(credBuffer));
            using (ChannelFactory<ClientProfileSyncPortType> factory = new ChannelFactory<ClientProfileSyncPortType>("ClientProfile_DnP"))
            {
                ClientProfileSyncPortType svc = factory.CreateChannel();
                using (OperationContextScope scope = new OperationContextScope((IContextChannel)svc))
                {
                    OperationContext.Current.OutgoingMessageProperties.Add(HttpRequestMessageProperty.Name, httpRequest);
                    System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                    response = svc.getClientProfile(request);
                }
            }

            if (response != null && response.getClientProfileResponse != null
                && response.getClientProfileResponse.getClientProfileRes != null
                && response.getClientProfileResponse.getClientProfileRes.clientProfile != null)
            {
                var clientProfile = response.getClientProfileResponse.getClientProfileRes.clientProfile;
                if (clientProfile.client != null && clientProfile.client.clientIdentity != null && clientProfile.client.clientIdentity[0].clientIdentityValidation != null)
                {

                    ClientIdentityValidation[] clientIdentityValidation = clientProfile.client.clientIdentity[0].clientIdentityValidation;
                    clientIdentityValidationList = clientIdentityValidation.ToList();

                }
            }
            else
            {
                throw new DnpException(string.Format("Response is Null from DNP for the Customer : ", identity));
            }

            return clientIdentityValidationList;
        }

        public static getClientProfileResponse1 GetProfile(string client, string domain)
        {

            getClientProfileRequest1 request = null;
            getClientProfileResponse1 response = null;

            request = new getClientProfileRequest1();
            request.getClientProfileRequest = new GetClientProfileRequest();
            request.getClientProfileRequest.standardHeader = new StandardHeaderBlock();
            request.getClientProfileRequest.standardHeader.serviceState = new ServiceState();
            request.getClientProfileRequest.standardHeader.serviceState.stateCode = "OK";

            request.getClientProfileRequest.standardHeader.serviceAddressing = new ServiceAddressing();
            request.getClientProfileRequest.standardHeader.serviceAddressing.from = "http://www.profile.com?SAASUI";

            request.getClientProfileRequest.getClientProfileReq = new GetClientProfileReq();
            request.getClientProfileRequest.getClientProfileReq.responseParameter = new ResponseParameters();


            request.getClientProfileRequest.getClientProfileReq.responseParameter.isClientIdentitiesRequired = "YES";
            request.getClientProfileRequest.getClientProfileReq.responseParameter.isClientServiceRolesRequired = "YES";
            request.getClientProfileRequest.getClientProfileReq.responseParameter.isClientServiceRoleCharacteristicRequired = "NO";
            request.getClientProfileRequest.getClientProfileReq.responseParameter.isLinkedProfilesRequired = "NO";
            request.getClientProfileRequest.getClientProfileReq.responseParameter.isClientServiceInstanceRequired = "YES";
            request.getClientProfileRequest.getClientProfileReq.responseParameter.isClientServiceInstanceCharacteristicsrequired = "NO";

            request.getClientProfileRequest.getClientProfileReq.clientSearchCriteria = new ClientSearchCriteria();
            request.getClientProfileRequest.getClientProfileReq.clientSearchCriteria.identifierDomainCode = domain;
            request.getClientProfileRequest.getClientProfileReq.clientSearchCriteria.identifierValue = client;
            request.getClientProfileRequest.getClientProfileReq.clientSearchCriteria.universalid = "";

            string basicAuthenticationCredentials = ConfigurationManager.AppSettings["DnPUser"].ToString() + ":" + ConfigurationManager.AppSettings["DnPPassword"].ToString();

            byte[] credBuffer = new UTF8Encoding().GetBytes(basicAuthenticationCredentials);
            HttpRequestMessageProperty httpRequest = new HttpRequestMessageProperty();
            httpRequest.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(credBuffer));
            using (ChannelFactory<ClientProfileSyncPortType> factory = new ChannelFactory<ClientProfileSyncPortType>("ClientProfile_DnP"))
            {
                ClientProfileSyncPortType svc = factory.CreateChannel();
                using (OperationContextScope scope = new OperationContextScope((IContextChannel)svc))
                {
                    OperationContext.Current.OutgoingMessageProperties.Add(HttpRequestMessageProperty.Name, httpRequest);
                    System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                    response = svc.getClientProfile(request);
                }
            }

            if (response != null && response.getClientProfileResponse != null && response.getClientProfileResponse.standardHeader != null && response.getClientProfileResponse.getClientProfileRes != null && response.getClientProfileResponse.getClientProfileRes.messages != null && response.getClientProfileResponse.getClientProfileRes.messages[0] != null &&
                    String.IsNullOrEmpty(response.getClientProfileResponse.getClientProfileRes.messages[0].messageCode))
            {
            }
            else
            {
                string errorMsg = string.Empty;
                if ((response != null) && (response.getClientProfileResponse != null)
                    && (response.getClientProfileResponse.getClientProfileRes != null)
                    && (response.getClientProfileResponse.getClientProfileRes.messages != null))
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (BT.SaaS.IspssAdapter.Dnp.Message msg in response.getClientProfileResponse.getClientProfileRes.messages)
                    {
                        sb.AppendFormat("|{0}|", msg.description);
                    }
                    errorMsg = sb.ToString();
                }

                throw new DnpException("Error getting the client service instances: ErrorCode:  " +
                     response.getClientProfileResponse.standardHeader.serviceState.stateCode + " " +
                     "ErrorMessage : " + errorMsg);
            }
            return response;
        }
        public static bool getProfile(string identity, string identityDomain, ref GetClientProfileResponse profile)
        {
            profile = null;
            //string secretQuestion = string.Empty;
            getClientProfileResponse1 profileResponse;
            getClientProfileRequest1 request1 = new getClientProfileRequest1();
            request1.getClientProfileRequest = new GetClientProfileRequest();
            request1.getClientProfileRequest.standardHeader = new StandardHeaderBlock();
            request1.getClientProfileRequest.standardHeader.serviceState = new ServiceState();
            request1.getClientProfileRequest.standardHeader.serviceAddressing = new ServiceAddressing();
            request1.getClientProfileRequest.standardHeader.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
            request1.getClientProfileRequest.getClientProfileReq = new GetClientProfileReq();
            request1.getClientProfileRequest.getClientProfileReq.clientSearchCriteria = new ClientSearchCriteria();
            request1.getClientProfileRequest.getClientProfileReq.clientSearchCriteria.identifierValue = identity;
            request1.getClientProfileRequest.getClientProfileReq.clientSearchCriteria.identifierDomainCode = identityDomain;
            request1.getClientProfileRequest.getClientProfileReq.responseParameter = new ResponseParameters();
            request1.getClientProfileRequest.getClientProfileReq.responseParameter.isClientIdentitiesRequired = "YES";
            request1.getClientProfileRequest.getClientProfileReq.responseParameter.isClientCredentialsRequired = "YES";
            request1.getClientProfileRequest.getClientProfileReq.responseParameter.isLinkedProfilesRequired = "YES";
            request1.getClientProfileRequest.getClientProfileReq.responseParameter.isClientServiceInstanceRequired = "YES";
            request1.getClientProfileRequest.getClientProfileReq.responseParameter.isClientServiceRolesRequired = "YES";
            string basicAuthenticationCredentials = ConfigurationManager.AppSettings["DnPUser"].ToString() + ":" + ConfigurationManager.AppSettings["DnPPassword"].ToString();

            byte[] credBuffer = new UTF8Encoding().GetBytes(basicAuthenticationCredentials);
            HttpRequestMessageProperty httpRequest = new HttpRequestMessageProperty();
            httpRequest.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(credBuffer));
            using (ChannelFactory<ClientProfileSyncPortType> factory = new ChannelFactory<ClientProfileSyncPortType>("ClientProfile_DnP"))
            {
                ClientProfileSyncPortType svc = factory.CreateChannel();
                using (OperationContextScope scope = new OperationContextScope((IContextChannel)svc))
                {
                    OperationContext.Current.OutgoingMessageProperties.Add(HttpRequestMessageProperty.Name, httpRequest);
                    System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                    profileResponse = svc.getClientProfile(request1);
                }


                if (profileResponse != null
                        && profileResponse.getClientProfileResponse != null
                        && profileResponse.getClientProfileResponse.standardHeader != null
                        && profileResponse.getClientProfileResponse.standardHeader.serviceState != null
                        && profileResponse.getClientProfileResponse.standardHeader.serviceState.stateCode != null
                        && profileResponse.getClientProfileResponse.standardHeader.serviceState.stateCode == "0")
                {
                    if (profileResponse.getClientProfileResponse.getClientProfileRes != null
                        && profileResponse.getClientProfileResponse.getClientProfileRes.clientProfile != null
                        && profileResponse.getClientProfileResponse.getClientProfileRes.clientProfile.client != null
                        && profileResponse.getClientProfileResponse.getClientProfileRes.clientProfile.client.clientIdentity != null
                        )
                    {
                        profile = profileResponse.getClientProfileResponse;
                        return true;
                    }
                }
                else
                {
                    string errorMsg = string.Empty;
                    if ((profileResponse != null) && (profileResponse.getClientProfileResponse != null)
                        && (profileResponse.getClientProfileResponse.getClientProfileRes != null)
                        && (profileResponse.getClientProfileResponse.getClientProfileRes.messages != null))
                    {
                        StringBuilder sb = new StringBuilder();
                        foreach (BT.SaaS.IspssAdapter.Dnp.Message msg in profileResponse.getClientProfileResponse.getClientProfileRes.messages)
                        {
                            sb.AppendFormat("|{0}|", msg.description);
                        }
                        errorMsg = sb.ToString();
                    }

                    throw new DnpException("Error getting the client Profile: ErrorCode:  " +
                         profileResponse.getClientProfileResponse.standardHeader.serviceState.stateCode + " " +
                         "ErrorMessage : " + errorMsg);
                }
            }
            return false;
        }
        public static bool getBatchProfile(string identity, string identityDomain, string emailIdentity, string emailIdentityDomain, ref GetBatchProfileResponse profile)
        {
            getBatchProfilesResponse getResponse;
            GetBatchProfileRequest getBatchProfileRequest = new GetBatchProfileRequest();
            getBatchProfileRequest = new GetBatchProfileRequest();
            getBatchProfileRequest.standardHeader = new StandardHeaderBlock();
            getBatchProfileRequest.standardHeader.e2e = new E2E();
            getBatchProfileRequest.standardHeader.e2e.E2EDATA = null;
            getBatchProfileRequest.standardHeader.serviceState = new ServiceState();
            getBatchProfileRequest.standardHeader.serviceState.stateCode = "OK";
            getBatchProfileRequest.standardHeader.serviceAddressing = new ServiceAddressing();
            getBatchProfileRequest.standardHeader.serviceAddressing.from = "http://www.profile.com?SAASMSEO";

            List<GetBatchProfileReq> batchProfilesList = new List<GetBatchProfileReq>();
            GetBatchProfileReq profile1 = new GetBatchProfileReq();
            profile1.responseParameter = new ResponseParameters();
            profile1.responseParameter.isClientIdentitiesRequired = "YES";
            profile1.responseParameter.isClientServiceInstanceCharacteristicsrequired = "YES";
            profile1.responseParameter.isClientServiceInstanceRequired = "YES";
            profile1.responseParameter.isClientServiceRoleCharacteristicRequired = "YES";
            profile1.responseParameter.isClientServiceRolesRequired = "YES";
            profile1.responseParameter.isLinkedProfilesRequired = "YES";
            profile1.clientSearchCriteria = new ClientSearchCriteria();
            profile1.clientSearchCriteria.identifierDomainCode = identityDomain;
            profile1.clientSearchCriteria.identifierValue = identity;
            GetBatchProfileReq profile2 = new GetBatchProfileReq();
            profile2.responseParameter = new ResponseParameters();
            profile2.responseParameter.isClientIdentitiesRequired = "YES";
            profile2.responseParameter.isClientServiceInstanceCharacteristicsrequired = "YES";
            profile2.responseParameter.isClientServiceInstanceRequired = "YES";
            profile2.responseParameter.isClientServiceRoleCharacteristicRequired = "YES";
            profile2.responseParameter.isClientServiceRolesRequired = "YES";
            profile2.responseParameter.isLinkedProfilesRequired = "YES";
            profile2.clientSearchCriteria = new ClientSearchCriteria();
            profile2.clientSearchCriteria.identifierDomainCode = emailIdentityDomain;
            profile2.clientSearchCriteria.identifierValue = emailIdentity;
            batchProfilesList.Add(profile1);
            batchProfilesList.Add(profile2);
            getBatchProfileRequest.getBatchProfileReq = new GetBatchProfileReq[batchProfilesList.Count];
            getBatchProfileRequest.getBatchProfileReq = batchProfilesList.ToArray();
            getBatchProfilesRequest getreq = new getBatchProfilesRequest();
            getreq.getBatchProfileRequest = getBatchProfileRequest;
            string basicAuthenticationCredentials = ConfigurationManager.AppSettings["DnPUser"].ToString() + ":" + ConfigurationManager.AppSettings["DnPPassword"].ToString();

            byte[] credBuffer = new UTF8Encoding().GetBytes(basicAuthenticationCredentials);
            HttpRequestMessageProperty httpRequest = new HttpRequestMessageProperty();
            httpRequest.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(credBuffer));
            using (ChannelFactory<ClientProfileSyncPortType> factory = new ChannelFactory<ClientProfileSyncPortType>("ClientProfile_DnP"))
            {
                ClientProfileSyncPortType svc = factory.CreateChannel();
                using (OperationContextScope scope = new OperationContextScope((IContextChannel)svc))
                {
                    OperationContext.Current.OutgoingMessageProperties.Add(HttpRequestMessageProperty.Name, httpRequest);
                    System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                    getResponse = svc.getBatchProfiles(getreq);
                }


                if (getResponse != null
                        && getResponse.getBatchProfileResponse != null
                        && getResponse.getBatchProfileResponse.standardHeader != null
                        && getResponse.getBatchProfileResponse.standardHeader.serviceState != null
                        )
                {
                    if (identityDomain != "BTCOM")
                    {
                        if (getResponse.getBatchProfileResponse.getBatchProfileRes != null)
                        {
                            profile = getResponse.getBatchProfileResponse;
                            return true;
                        }
                    }
                    else
                    {
                        if (getResponse.getBatchProfileResponse.getBatchProfileRes != null
                        && getResponse.getBatchProfileResponse.getBatchProfileRes.clientProfile != null
                        && getResponse.getBatchProfileResponse.getBatchProfileRes.clientProfile[0].client != null
                        && getResponse.getBatchProfileResponse.getBatchProfileRes.clientProfile[0].client.clientIdentity != null
                        && getResponse.getBatchProfileResponse.standardHeader.serviceState.stateCode != null
                        && getResponse.getBatchProfileResponse.standardHeader.serviceState.stateCode == "0"
                        )
                        {
                            profile = getResponse.getBatchProfileResponse;
                            return true;
                        }
                        else
                        {
                            string errorMsg = string.Empty;
                            if ((getResponse != null) && (getResponse.getBatchProfileResponse != null)
                                && (getResponse.getBatchProfileResponse.getBatchProfileRes != null)
                                && (getResponse.getBatchProfileResponse.getBatchProfileRes.messages != null))
                            {
                                StringBuilder sb = new StringBuilder();
                                foreach (BT.SaaS.IspssAdapter.Dnp.Message msg in getResponse.getBatchProfileResponse.getBatchProfileRes.messages)
                                {
                                    sb.AppendFormat("|{0}|", msg.description);
                                }
                                errorMsg = sb.ToString();
                            }

                            throw new DnpException("Error getting the client Profile: ErrorCode:  " +
                                 getResponse.getBatchProfileResponse.standardHeader.serviceState.stateCode + " " +
                                 "ErrorMessage : " + errorMsg);
                        }

                    }

                }
                else
                {
                    throw new DnpException("Error in making the getbatchprofile DNP call");
                }
            }
            return false;
        }
        public static bool getBatchProfileV1(string identity, string identityDomain, string emailIdentity, string emailIdentityDomain, ref GetBatchProfileV1Response profile)
        {
            getBatchProfilesV1Response getResponse;
            try
            {
                GetBatchProfileV1Request getBatchProfileRequest = new GetBatchProfileV1Request();
                getBatchProfileRequest.standardHeader = new StandardHeaderBlock();
                getBatchProfileRequest.standardHeader.e2e = new E2E();
                getBatchProfileRequest.standardHeader.e2e.E2EDATA = null;
                getBatchProfileRequest.standardHeader.serviceState = new ServiceState();
                getBatchProfileRequest.standardHeader.serviceState.stateCode = "OK";
                getBatchProfileRequest.standardHeader.serviceAddressing = new ServiceAddressing();
                getBatchProfileRequest.standardHeader.serviceAddressing.from = "http://www.profile.com?SAASMSEO";

                List<GetBatchProfileV1Req> batchProfilesList = new List<GetBatchProfileV1Req>();
                GetBatchProfileV1Req profile1 = new GetBatchProfileV1Req();
                profile1.responseParameter = new ResponseParameters();
                profile1.responseParameter.isClientIdentitiesRequired = "YES";
                profile1.responseParameter.isClientServiceInstanceCharacteristicsrequired = "YES";
                profile1.responseParameter.isClientServiceInstanceRequired = "YES";
                profile1.responseParameter.isClientServiceRoleCharacteristicRequired = "YES";
                profile1.responseParameter.isClientServiceRolesRequired = "YES";
                profile1.responseParameter.isLinkedProfilesRequired = "YES";
                profile1.clientSearchCriteria = new ClientSearchCriteria();
                profile1.clientSearchCriteria.identifierDomainCode = identityDomain;
                profile1.clientSearchCriteria.identifierValue = identity;
                GetBatchProfileV1Req profile2 = new GetBatchProfileV1Req();
                profile2.responseParameter = new ResponseParameters();
                profile2.responseParameter.isClientIdentitiesRequired = "YES";
                profile2.responseParameter.isClientServiceInstanceCharacteristicsrequired = "YES";
                profile2.responseParameter.isClientServiceInstanceRequired = "YES";
                profile2.responseParameter.isClientServiceRoleCharacteristicRequired = "YES";
                profile2.responseParameter.isClientServiceRolesRequired = "YES";
                profile2.responseParameter.isLinkedProfilesRequired = "YES";
                profile2.clientSearchCriteria = new ClientSearchCriteria();
                profile2.clientSearchCriteria.identifierDomainCode = emailIdentityDomain;
                profile2.clientSearchCriteria.identifierValue = emailIdentity;
                batchProfilesList.Add(profile1);
                batchProfilesList.Add(profile2);

                getBatchProfileRequest.getBatchProfileV1Req = new GetBatchProfileV1Req[batchProfilesList.Count];
                getBatchProfileRequest.getBatchProfileV1Req = batchProfilesList.ToArray();
                getBatchProfilesV1Request getreq = new getBatchProfilesV1Request();
                getreq.getBatchProfileV1Request = getBatchProfileRequest;
                string basicAuthenticationCredentials = ConfigurationManager.AppSettings["DnPUser"].ToString() + ":" + ConfigurationManager.AppSettings["DnPPassword"].ToString();

                byte[] credBuffer = new UTF8Encoding().GetBytes(basicAuthenticationCredentials);
                HttpRequestMessageProperty httpRequest = new HttpRequestMessageProperty();
                httpRequest.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(credBuffer));
                using (ChannelFactory<ClientProfileSyncPortType> factory = new ChannelFactory<ClientProfileSyncPortType>("ClientProfile_DnP"))
                {
                    ClientProfileSyncPortType svc = factory.CreateChannel();
                    using (OperationContextScope scope = new OperationContextScope((IContextChannel)svc))
                    {
                        OperationContext.Current.OutgoingMessageProperties.Add(HttpRequestMessageProperty.Name, httpRequest);
                        System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                        getResponse = svc.getBatchProfilesV1(getreq);
                    }


                    if (getResponse != null
                            && getResponse.getBatchProfileV1Response != null
                            && getResponse.getBatchProfileV1Response.standardHeader != null
                            && getResponse.getBatchProfileV1Response.standardHeader.serviceState != null
                            )
                    {
                        if (identityDomain != "BTCOM")
                        {
                            if (getResponse.getBatchProfileV1Response.getBatchProfileV1Res != null)
                            {
                                profile = getResponse.getBatchProfileV1Response;
                                return true;
                            }
                        }
                        else
                        {
                            if (getResponse.getBatchProfileV1Response.getBatchProfileV1Res != null
                            && getResponse.getBatchProfileV1Response.getBatchProfileV1Res.clientProfileV1 != null
                            && getResponse.getBatchProfileV1Response.getBatchProfileV1Res.clientProfileV1[0].client != null
                            && getResponse.getBatchProfileV1Response.getBatchProfileV1Res.clientProfileV1[0].client.clientIdentity != null
                            && getResponse.getBatchProfileV1Response.standardHeader.serviceState.stateCode != null
                            && getResponse.getBatchProfileV1Response.standardHeader.serviceState.stateCode == "0"
                            )
                            {
                                profile = getResponse.getBatchProfileV1Response;
                                return true;
                            }
                            else
                            {
                                string errorMsg = string.Empty;
                                if ((getResponse != null) && (getResponse.getBatchProfileV1Response != null)
                                    && (getResponse.getBatchProfileV1Response.getBatchProfileV1Res != null)
                                    && (getResponse.getBatchProfileV1Response.getBatchProfileV1Res.messages != null))
                                {
                                    StringBuilder sb = new StringBuilder();
                                    foreach (BT.SaaS.IspssAdapter.Dnp.Message msg in getResponse.getBatchProfileV1Response.getBatchProfileV1Res.messages)
                                    {
                                        sb.AppendFormat("|{0}|", msg.description);
                                    }
                                    errorMsg = sb.ToString();
                                }

                                throw new DnpException("Error getting the client Profile: ErrorCode:  " +
                                     getResponse.getBatchProfileV1Response.standardHeader.serviceState.stateCode + " " +
                                     "ErrorMessage : " + errorMsg);
                            }

                        }

                    }
                    else
                    {
                        throw new DnpException("Error in making the getbatchprofile DNP call");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DnpException(ex.Message);
            }
            return false;
        }

        public static bool getBatchProfileV1(string identity, string identityDomain, string secondIdentity, string secondIdentityDomain, string thirdIdentity, string thirdIdentityDomain, ref GetBatchProfileV1Response profile)
        {
            getBatchProfilesV1Response getResponse;
            try
            {
                GetBatchProfileV1Request getBatchProfileRequest = new GetBatchProfileV1Request();
                getBatchProfileRequest.standardHeader = new StandardHeaderBlock();
                getBatchProfileRequest.standardHeader.e2e = new E2E();
                getBatchProfileRequest.standardHeader.e2e.E2EDATA = null;
                getBatchProfileRequest.standardHeader.serviceState = new ServiceState();
                getBatchProfileRequest.standardHeader.serviceState.stateCode = "OK";
                getBatchProfileRequest.standardHeader.serviceAddressing = new ServiceAddressing();
                getBatchProfileRequest.standardHeader.serviceAddressing.from = "http://www.profile.com?SAASMSEO";

                List<GetBatchProfileV1Req> batchProfilesList = new List<GetBatchProfileV1Req>();
                GetBatchProfileV1Req profile1 = new GetBatchProfileV1Req();
                profile1.responseParameter = new ResponseParameters();
                profile1.responseParameter.isClientIdentitiesRequired = "YES";
                profile1.responseParameter.isClientServiceInstanceCharacteristicsrequired = "YES";
                profile1.responseParameter.isClientServiceInstanceRequired = "YES";
                profile1.responseParameter.isClientServiceRoleCharacteristicRequired = "YES";
                profile1.responseParameter.isClientServiceRolesRequired = "YES";
                profile1.responseParameter.isLinkedProfilesRequired = "YES";
                profile1.clientSearchCriteria = new ClientSearchCriteria();
                profile1.clientSearchCriteria.identifierDomainCode = identityDomain;
                profile1.clientSearchCriteria.identifierValue = identity;

                GetBatchProfileV1Req profile2 = new GetBatchProfileV1Req();
                profile2.responseParameter = new ResponseParameters();
                profile2.responseParameter.isClientIdentitiesRequired = "YES";
                profile2.responseParameter.isClientServiceInstanceCharacteristicsrequired = "YES";
                profile2.responseParameter.isClientServiceInstanceRequired = "YES";
                profile2.responseParameter.isClientServiceRoleCharacteristicRequired = "YES";
                profile2.responseParameter.isClientServiceRolesRequired = "YES";
                profile2.responseParameter.isLinkedProfilesRequired = "YES";
                profile2.clientSearchCriteria = new ClientSearchCriteria();
                profile2.clientSearchCriteria.identifierDomainCode = secondIdentityDomain;
                profile2.clientSearchCriteria.identifierValue = secondIdentity;

                GetBatchProfileV1Req profile3 = new GetBatchProfileV1Req();
                profile3.responseParameter = new ResponseParameters();
                profile3.responseParameter.isClientIdentitiesRequired = "YES";
                profile3.responseParameter.isClientServiceInstanceCharacteristicsrequired = "YES";
                profile3.responseParameter.isClientServiceInstanceRequired = "YES";
                profile3.responseParameter.isClientServiceRoleCharacteristicRequired = "YES";
                profile3.responseParameter.isClientServiceRolesRequired = "YES";
                profile3.responseParameter.isLinkedProfilesRequired = "YES";
                profile3.clientSearchCriteria = new ClientSearchCriteria();
                profile3.clientSearchCriteria.identifierDomainCode = thirdIdentityDomain;
                profile3.clientSearchCriteria.identifierValue = thirdIdentity;

                batchProfilesList.Add(profile1);
                batchProfilesList.Add(profile2);
                batchProfilesList.Add(profile3);

                getBatchProfileRequest.getBatchProfileV1Req = new GetBatchProfileV1Req[batchProfilesList.Count];
                getBatchProfileRequest.getBatchProfileV1Req = batchProfilesList.ToArray();
                getBatchProfilesV1Request getreq = new getBatchProfilesV1Request();
                getreq.getBatchProfileV1Request = getBatchProfileRequest;
                string basicAuthenticationCredentials = ConfigurationManager.AppSettings["DnPUser"].ToString() + ":" + ConfigurationManager.AppSettings["DnPPassword"].ToString();

                byte[] credBuffer = new UTF8Encoding().GetBytes(basicAuthenticationCredentials);
                HttpRequestMessageProperty httpRequest = new HttpRequestMessageProperty();
                httpRequest.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(credBuffer));
                using (ChannelFactory<ClientProfileSyncPortType> factory = new ChannelFactory<ClientProfileSyncPortType>("ClientProfile_DnP"))
                {
                    ClientProfileSyncPortType svc = factory.CreateChannel();
                    using (OperationContextScope scope = new OperationContextScope((IContextChannel)svc))
                    {
                        OperationContext.Current.OutgoingMessageProperties.Add(HttpRequestMessageProperty.Name, httpRequest);
                        System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                        getResponse = svc.getBatchProfilesV1(getreq);
                    }


                    if (getResponse != null
                            && getResponse.getBatchProfileV1Response != null
                            && getResponse.getBatchProfileV1Response.standardHeader != null
                            && getResponse.getBatchProfileV1Response.standardHeader.serviceState != null
                            )
                    {
                        if (identityDomain != "BTCOM")
                        {
                            if (getResponse.getBatchProfileV1Response.getBatchProfileV1Res != null)
                            {
                                profile = getResponse.getBatchProfileV1Response;
                                return true;
                            }
                        }
                        else
                        {
                            if (getResponse.getBatchProfileV1Response.getBatchProfileV1Res != null
                            && getResponse.getBatchProfileV1Response.getBatchProfileV1Res.clientProfileV1 != null
                            && getResponse.getBatchProfileV1Response.getBatchProfileV1Res.clientProfileV1[0].client != null
                            && getResponse.getBatchProfileV1Response.getBatchProfileV1Res.clientProfileV1[0].client.clientIdentity != null
                            && getResponse.getBatchProfileV1Response.standardHeader.serviceState.stateCode != null
                            && getResponse.getBatchProfileV1Response.standardHeader.serviceState.stateCode == "0"
                            )
                            {
                                profile = getResponse.getBatchProfileV1Response;
                                return true;
                            }
                            else
                            {
                                string errorMsg = string.Empty;
                                if ((getResponse != null) && (getResponse.getBatchProfileV1Response != null)
                                    && (getResponse.getBatchProfileV1Response.getBatchProfileV1Res != null)
                                    && (getResponse.getBatchProfileV1Response.getBatchProfileV1Res.messages != null))
                                {
                                    StringBuilder sb = new StringBuilder();
                                    foreach (BT.SaaS.IspssAdapter.Dnp.Message msg in getResponse.getBatchProfileV1Response.getBatchProfileV1Res.messages)
                                    {
                                        sb.AppendFormat("|{0}|", msg.description);
                                    }
                                    errorMsg = sb.ToString();
                                }

                                throw new DnpException("Error getting the client Profile: ErrorCode:  " +
                                     getResponse.getBatchProfileV1Response.standardHeader.serviceState.stateCode + " " +
                                     "ErrorMessage : " + errorMsg);
                            }

                        }

                    }
                    else
                    {
                        throw new DnpException("Error in making the getbatchprofile DNP call");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DnpException(ex.Message);
            }
            return false;
        }
        public static ClientServiceInstanceV1[] GetVASClientServiceInstances(string client, string domain)
        {
            getServiceUserProfilesV1Request1 request = null;
            getServiceUserProfilesV1Response1 response = null;
            List<ClientServiceInstanceV1> serviceInstances = new List<ClientServiceInstanceV1>();
            List<string> serviceList = new List<string>();
            try
            {
                request = new getServiceUserProfilesV1Request1();
                request.getServiceUserProfilesV1Request = new GetServiceUserProfilesV1Request();
                request.getServiceUserProfilesV1Request.standardHeader = new StandardHeaderBlock();
                request.getServiceUserProfilesV1Request.standardHeader.serviceState = new ServiceState();
                request.getServiceUserProfilesV1Request.standardHeader.serviceState.stateCode = "OK";

                request.getServiceUserProfilesV1Request.standardHeader.serviceAddressing = new ServiceAddressing();
                request.getServiceUserProfilesV1Request.standardHeader.serviceAddressing.from = "http://www.profile.com?SAASMSEO";

                request.getServiceUserProfilesV1Request.getServiceUserProfilesV1Req = new GetServiceUserProfilesV1Req();
                request.getServiceUserProfilesV1Request.getServiceUserProfilesV1Req.clientServiceInstanceSearchCriteria = new ClientServiceInstanceSearchCriteriaV1();
                request.getServiceUserProfilesV1Request.getServiceUserProfilesV1Req.clientServiceInstanceSearchCriteria.serviceIdentityDomain = domain;
                request.getServiceUserProfilesV1Request.getServiceUserProfilesV1Req.clientServiceInstanceSearchCriteria.serviceIdentityValue = client;
                request.getServiceUserProfilesV1Request.getServiceUserProfilesV1Req.responseParameter = new ResponseParameters();
                request.getServiceUserProfilesV1Request.getServiceUserProfilesV1Req.responseParameter.isClientServiceInstanceRequired = "NO";
                request.getServiceUserProfilesV1Request.getServiceUserProfilesV1Req.responseParameter.isClientServiceInstanceCharacteristicsrequired = "YES";
                request.getServiceUserProfilesV1Request.getServiceUserProfilesV1Req.responseParameter.isClientServiceRolesRequired = "YES";

                string basicAuthenticationCredentials = ConfigurationManager.AppSettings["DnPUser"].ToString() + ":" + ConfigurationManager.AppSettings["DnPPassword"].ToString();

                byte[] credBuffer = new UTF8Encoding().GetBytes(basicAuthenticationCredentials);
                HttpRequestMessageProperty httpRequest = new HttpRequestMessageProperty();
                httpRequest.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(credBuffer));
                using (ChannelFactory<ServiceProfileSyncPortType> factory = new ChannelFactory<ServiceProfileSyncPortType>("ServiceProfile_DnP"))
                {
                    ServiceProfileSyncPortType svc = factory.CreateChannel();
                    using (OperationContextScope scope = new OperationContextScope((IContextChannel)svc))
                    {
                        OperationContext.Current.OutgoingMessageProperties.Add(HttpRequestMessageProperty.Name, httpRequest);
                        System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                        response = svc.getServiceUserProfilesV1(request);
                    }
                }
                if (response != null
                    && response.getServiceUserProfilesV1Response != null
                    && response.getServiceUserProfilesV1Response.standardHeader != null
                    && response.getServiceUserProfilesV1Response.getServiceUserProfilesV1Res != null
                    && response.getServiceUserProfilesV1Response.getServiceUserProfilesV1Res.messages != null
                    && response.getServiceUserProfilesV1Response.getServiceUserProfilesV1Res.messages[0] != null
                    &&
                    String.IsNullOrEmpty(response.getServiceUserProfilesV1Response.getServiceUserProfilesV1Res.messages[0].messageCode))
                {

                    foreach (ClientProfileV1 cv in response.getServiceUserProfilesV1Response.getServiceUserProfilesV1Res.clientProfileV1)
                    {
                        foreach (ClientServiceInstanceV1 csi in cv.clientServiceInstanceV1)
                        {
                            if (!serviceList.Contains(csi.clientServiceInstanceIdentifier.value))
                            {
                                serviceList.Add(csi.clientServiceInstanceIdentifier.value);
                                serviceInstances.Add(csi);
                            }
                        }

                    }

                }
            }
            catch (Exception ex)
            {
                throw new DnpException(ex.Message);
            }
            if (serviceInstances.Count > 0)
                return serviceInstances.ToArray();
            else
                return null;

        }

        public static GetClientProfileV1Res GetClientProfileV1(string identity, string identityDomain)
        {
            GetClientProfileV1Res response;
            getClientProfileV1Response1 profileResponse;
            try
            {
                getClientProfileV1Request1 profileRequest = new getClientProfileV1Request1();
                profileRequest.getClientProfileV1Request = new GetClientProfileV1Request();
                profileRequest.getClientProfileV1Request.standardHeader = new StandardHeaderBlock();
                profileRequest.getClientProfileV1Request.standardHeader.serviceState = new ServiceState();
                profileRequest.getClientProfileV1Request.standardHeader.serviceState.stateCode = "OK";
                profileRequest.getClientProfileV1Request.standardHeader.serviceAddressing = new ServiceAddressing();
                profileRequest.getClientProfileV1Request.standardHeader.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
                profileRequest.getClientProfileV1Request.getClientProfileV1Req = new GetClientProfileV1Req();
                profileRequest.getClientProfileV1Request.getClientProfileV1Req.clientSearchCriteria = new ClientSearchCriteria();
                profileRequest.getClientProfileV1Request.getClientProfileV1Req.clientSearchCriteria.identifierValue = identity;
                profileRequest.getClientProfileV1Request.getClientProfileV1Req.clientSearchCriteria.identifierDomainCode = identityDomain;
                profileRequest.getClientProfileV1Request.getClientProfileV1Req.responseParameter = new ResponseParameters();
                profileRequest.getClientProfileV1Request.getClientProfileV1Req.responseParameter.isClientIdentitiesRequired = "YES";
                profileRequest.getClientProfileV1Request.getClientProfileV1Req.responseParameter.isClientServiceInstanceRequired = "YES";
                profileRequest.getClientProfileV1Request.getClientProfileV1Req.responseParameter.isClientServiceRolesRequired = "YES";
                profileRequest.getClientProfileV1Request.getClientProfileV1Req.responseParameter.isClientServiceInstanceRequired = "YES";
                profileRequest.getClientProfileV1Request.getClientProfileV1Req.responseParameter.isLinkedProfilesRequired = "YES";
                profileRequest.getClientProfileV1Request.getClientProfileV1Req.responseParameter.isClientServiceInstanceCharacteristicsrequired = "YES";
                profileRequest.getClientProfileV1Request.getClientProfileV1Req.responseParameter.isClientServiceRoleCharacteristicRequired = "YES";

                string basicAuthenticationCredentials = ConfigurationManager.AppSettings["DnPUser"].ToString() + ":" + ConfigurationManager.AppSettings["DnPPassword"].ToString();
                byte[] credBuffer = new UTF8Encoding().GetBytes(basicAuthenticationCredentials);
                HttpRequestMessageProperty httpRequest = new HttpRequestMessageProperty();
                httpRequest.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(credBuffer));
                using (ChannelFactory<ClientProfileSyncPortType> factory = new ChannelFactory<ClientProfileSyncPortType>("ClientProfile_DnP"))
                {
                    ClientProfileSyncPortType svc = factory.CreateChannel();
                    using (OperationContextScope scope = new OperationContextScope((IContextChannel)svc))
                    {
                        OperationContext.Current.OutgoingMessageProperties.Add(HttpRequestMessageProperty.Name, httpRequest);
                        System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                        profileResponse = svc.getClientProfileV1(profileRequest);
                        response = profileResponse.getClientProfileV1Response.getClientProfileV1Res;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DnpException(ex.Message);
            }

            return response;
        }

        public static GetBatchProfileV1Res GetServiceUserProfilesV1(string client, string domain)
        {
            getServiceUserProfilesV1Request1 request = null;
            getServiceUserProfilesV1Response1 serviceProfileResponse = null;

            GetBatchProfileV1Res response = null;
            try
            {
                request = new getServiceUserProfilesV1Request1();
                request.getServiceUserProfilesV1Request = new GetServiceUserProfilesV1Request();
                request.getServiceUserProfilesV1Request.standardHeader = new StandardHeaderBlock();
                request.getServiceUserProfilesV1Request.standardHeader.serviceState = new ServiceState();
                request.getServiceUserProfilesV1Request.standardHeader.serviceState.stateCode = "OK";

                request.getServiceUserProfilesV1Request.standardHeader.serviceAddressing = new ServiceAddressing();
                request.getServiceUserProfilesV1Request.standardHeader.serviceAddressing.from = "http://www.profile.com?SAASMSEO";

                request.getServiceUserProfilesV1Request.getServiceUserProfilesV1Req = new GetServiceUserProfilesV1Req();
                request.getServiceUserProfilesV1Request.getServiceUserProfilesV1Req.clientServiceInstanceSearchCriteria = new ClientServiceInstanceSearchCriteriaV1();
                request.getServiceUserProfilesV1Request.getServiceUserProfilesV1Req.clientServiceInstanceSearchCriteria.serviceIdentityDomain = domain;
                request.getServiceUserProfilesV1Request.getServiceUserProfilesV1Req.clientServiceInstanceSearchCriteria.serviceIdentityValue = client;
                request.getServiceUserProfilesV1Request.getServiceUserProfilesV1Req.responseParameter = new ResponseParameters();
                request.getServiceUserProfilesV1Request.getServiceUserProfilesV1Req.responseParameter.isClientServiceInstanceRequired = "NO";
                request.getServiceUserProfilesV1Request.getServiceUserProfilesV1Req.responseParameter.isClientServiceInstanceCharacteristicsrequired = "YES";

                string basicAuthenticationCredentials = ConfigurationManager.AppSettings["DnPUser"].ToString() + ":" + ConfigurationManager.AppSettings["DnPPassword"].ToString();

                byte[] credBuffer = new UTF8Encoding().GetBytes(basicAuthenticationCredentials);
                HttpRequestMessageProperty httpRequest = new HttpRequestMessageProperty();
                httpRequest.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(credBuffer));
                using (ChannelFactory<ServiceProfileSyncPortType> factory = new ChannelFactory<ServiceProfileSyncPortType>("ServiceProfile_DnP"))
                {
                    ServiceProfileSyncPortType svc = factory.CreateChannel();
                    using (OperationContextScope scope = new OperationContextScope((IContextChannel)svc))
                    {
                        OperationContext.Current.OutgoingMessageProperties.Add(HttpRequestMessageProperty.Name, httpRequest);
                        System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                        serviceProfileResponse = svc.getServiceUserProfilesV1(request);
                    }
                }

                if (serviceProfileResponse != null
                    && serviceProfileResponse.getServiceUserProfilesV1Response != null
                    && serviceProfileResponse.getServiceUserProfilesV1Response.standardHeader != null
                    && serviceProfileResponse.getServiceUserProfilesV1Response.getServiceUserProfilesV1Res != null
                    && serviceProfileResponse.getServiceUserProfilesV1Response.getServiceUserProfilesV1Res.messages != null
                    && serviceProfileResponse.getServiceUserProfilesV1Response.getServiceUserProfilesV1Res.messages[0] != null
                    &&
                    String.IsNullOrEmpty(serviceProfileResponse.getServiceUserProfilesV1Response.getServiceUserProfilesV1Res.messages[0].messageCode))
                {
                    response = serviceProfileResponse.getServiceUserProfilesV1Response.getServiceUserProfilesV1Res;
                }
            }
            catch (Exception ex)
            {
                throw new DnpException(ex.Message);

            }

            return response;
        }
        public static GetServiceUserProfilesV1Response GetServiceUserProfilesV1ForHomeAway(string bacid)
        {
            getServiceUserProfilesV1Request1 request1 = null;
            getServiceUserProfilesV1Response1 response1 = null;
            GetServiceUserProfilesV1Response response = null;
            try
            {
                request1 = new getServiceUserProfilesV1Request1();
                request1.getServiceUserProfilesV1Request = new GetServiceUserProfilesV1Request();
                request1.getServiceUserProfilesV1Request.standardHeader = new StandardHeaderBlock();
                request1.getServiceUserProfilesV1Request.standardHeader.serviceState = new ServiceState();
                request1.getServiceUserProfilesV1Request.standardHeader.serviceState.stateCode = "OK";

                request1.getServiceUserProfilesV1Request.standardHeader.serviceAddressing = new ServiceAddressing();
                request1.getServiceUserProfilesV1Request.standardHeader.serviceAddressing.from = "http://www.profile.com?SAASMSEO";

                request1.getServiceUserProfilesV1Request.getServiceUserProfilesV1Req = new GetServiceUserProfilesV1Req();
                request1.getServiceUserProfilesV1Request.getServiceUserProfilesV1Req.clientServiceInstanceSearchCriteria = new ClientServiceInstanceSearchCriteriaV1();
                request1.getServiceUserProfilesV1Request.getServiceUserProfilesV1Req.clientServiceInstanceSearchCriteria.serviceInstanceKey = bacid;
                request1.getServiceUserProfilesV1Request.getServiceUserProfilesV1Req.clientServiceInstanceSearchCriteria.serviceCode = "HOMEAWAY";
                request1.getServiceUserProfilesV1Request.getServiceUserProfilesV1Req.responseParameter = new ResponseParameters();
                request1.getServiceUserProfilesV1Request.getServiceUserProfilesV1Req.responseParameter.isClientServiceRolesRequired = "YES";
                request1.getServiceUserProfilesV1Request.getServiceUserProfilesV1Req.responseParameter.isClientServiceRoleCharacteristicRequired = "YES";
                request1.getServiceUserProfilesV1Request.getServiceUserProfilesV1Req.responseParameter.isClientIdentitiesRequired = "YES";
                request1.getServiceUserProfilesV1Request.getServiceUserProfilesV1Req.responseParameter.isClientServiceInstanceRequired = "YES";
                request1.getServiceUserProfilesV1Request.getServiceUserProfilesV1Req.responseParameter.isClientServiceInstanceCharacteristicsrequired = "YES";

                string basicAuthenticationCredentials = ConfigurationManager.AppSettings["DnPUser"].ToString() + ":" + ConfigurationManager.AppSettings["DnPPassword"].ToString();

                byte[] credBuffer = new UTF8Encoding().GetBytes(basicAuthenticationCredentials);
                HttpRequestMessageProperty httpRequest = new HttpRequestMessageProperty();
                httpRequest.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(credBuffer));
                using (ChannelFactory<ServiceProfileSyncPortType> factory = new ChannelFactory<ServiceProfileSyncPortType>("ServiceProfile_DnP"))
                {
                    ServiceProfileSyncPortType svc = factory.CreateChannel();
                    using (OperationContextScope scope = new OperationContextScope((IContextChannel)svc))
                    {
                        OperationContext.Current.OutgoingMessageProperties.Add(HttpRequestMessageProperty.Name, httpRequest);
                        System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                        response1 = svc.getServiceUserProfilesV1(request1);
                    }
                }
                if (response1 != null
                    && response1.getServiceUserProfilesV1Response != null
                    && response1.getServiceUserProfilesV1Response.standardHeader != null
                    && response1.getServiceUserProfilesV1Response.getServiceUserProfilesV1Res != null
                    && response1.getServiceUserProfilesV1Response.getServiceUserProfilesV1Res.messages != null
                    && response1.getServiceUserProfilesV1Response.getServiceUserProfilesV1Res.messages[0] != null
                    &&
                    String.IsNullOrEmpty(response1.getServiceUserProfilesV1Response.getServiceUserProfilesV1Res.messages[0].messageCode))
                {
                    response = response1.getServiceUserProfilesV1Response;

                }
            }
            catch (Exception ex)
            {
                throw new DnpException(ex.Message);
            }
            return response;
        }

        public static GetBatchProfileV1Res GetServiceUserProfilesV1ForDante(string client, string domain)
        {
            getServiceUserProfilesV1Request1 request = null;
            getServiceUserProfilesV1Response1 serviceProfileResponse = null;

            GetBatchProfileV1Res response = null;
            try
            {
                request = new getServiceUserProfilesV1Request1();
                request.getServiceUserProfilesV1Request = new GetServiceUserProfilesV1Request();
                request.getServiceUserProfilesV1Request.standardHeader = new StandardHeaderBlock();
                request.getServiceUserProfilesV1Request.standardHeader.serviceState = new ServiceState();
                request.getServiceUserProfilesV1Request.standardHeader.serviceState.stateCode = "OK";

                request.getServiceUserProfilesV1Request.standardHeader.serviceAddressing = new ServiceAddressing();
                request.getServiceUserProfilesV1Request.standardHeader.serviceAddressing.from = "http://www.profile.com?SAASMSEO";

                request.getServiceUserProfilesV1Request.getServiceUserProfilesV1Req = new GetServiceUserProfilesV1Req();
                request.getServiceUserProfilesV1Request.getServiceUserProfilesV1Req.clientServiceInstanceSearchCriteria = new ClientServiceInstanceSearchCriteriaV1();
                request.getServiceUserProfilesV1Request.getServiceUserProfilesV1Req.clientServiceInstanceSearchCriteria.serviceIdentityDomain = domain;
                request.getServiceUserProfilesV1Request.getServiceUserProfilesV1Req.clientServiceInstanceSearchCriteria.serviceIdentityValue = client;
                request.getServiceUserProfilesV1Request.getServiceUserProfilesV1Req.responseParameter = new ResponseParameters();
                request.getServiceUserProfilesV1Request.getServiceUserProfilesV1Req.responseParameter.isClientServiceInstanceRequired = "NO";
                request.getServiceUserProfilesV1Request.getServiceUserProfilesV1Req.responseParameter.isClientServiceInstanceCharacteristicsrequired = "YES";
                request.getServiceUserProfilesV1Request.getServiceUserProfilesV1Req.responseParameter.isClientIdentitiesRequired = "YES";
                request.getServiceUserProfilesV1Request.getServiceUserProfilesV1Req.responseParameter.isLinkedProfilesRequired = "YES";
                request.getServiceUserProfilesV1Request.getServiceUserProfilesV1Req.responseParameter.isClientServiceRolesRequired = "YES";

                string basicAuthenticationCredentials = ConfigurationManager.AppSettings["DnPUser"].ToString() + ":" + ConfigurationManager.AppSettings["DnPPassword"].ToString();

                byte[] credBuffer = new UTF8Encoding().GetBytes(basicAuthenticationCredentials);
                HttpRequestMessageProperty httpRequest = new HttpRequestMessageProperty();
                httpRequest.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(credBuffer));
                using (ChannelFactory<ServiceProfileSyncPortType> factory = new ChannelFactory<ServiceProfileSyncPortType>("ServiceProfile_DnP"))
                {
                    ServiceProfileSyncPortType svc = factory.CreateChannel();
                    using (OperationContextScope scope = new OperationContextScope((IContextChannel)svc))
                    {
                        OperationContext.Current.OutgoingMessageProperties.Add(HttpRequestMessageProperty.Name, httpRequest);
                        System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                        serviceProfileResponse = svc.getServiceUserProfilesV1(request);
                    }
                }

                if (serviceProfileResponse != null
                    && serviceProfileResponse.getServiceUserProfilesV1Response != null
                    && serviceProfileResponse.getServiceUserProfilesV1Response.standardHeader != null
                    && serviceProfileResponse.getServiceUserProfilesV1Response.getServiceUserProfilesV1Res != null
                    && serviceProfileResponse.getServiceUserProfilesV1Response.getServiceUserProfilesV1Res.messages != null
                    && serviceProfileResponse.getServiceUserProfilesV1Response.getServiceUserProfilesV1Res.messages[0] != null
                    &&
                    String.IsNullOrEmpty(serviceProfileResponse.getServiceUserProfilesV1Response.getServiceUserProfilesV1Res.messages[0].messageCode))
                {
                    response = serviceProfileResponse.getServiceUserProfilesV1Response.getServiceUserProfilesV1Res;
                }
            }
            catch (Exception ex)
            {
                throw new DnpException(ex.Message);
            }

            return response;
        }

        public static ClientServiceInstanceV1[] getServiceInstanceV1(string client, string domain, string serviceCode)
        {
            WriteToFile("Inside Dnpwrapper getserviceInstanceV1");
            getServiceInstanceV1Request1 request = null;
            getServiceInstanceV1Response1 serviceProfileResponse = null;
            ClientServiceInstanceV1[] response = null;
            try
            {
                request = new getServiceInstanceV1Request1();
                request.getServiceInstanceV1Request = new GetServiceInstanceV1Request();
                request.getServiceInstanceV1Request.standardHeader = new StandardHeaderBlock();
                request.getServiceInstanceV1Request.standardHeader.serviceState = new ServiceState();
                request.getServiceInstanceV1Request.standardHeader.serviceState.stateCode = "OK";

                request.getServiceInstanceV1Request.standardHeader.serviceAddressing = new ServiceAddressing();
                request.getServiceInstanceV1Request.standardHeader.serviceAddressing.from = "http://www.profile.com?SAASMSEO";

                request.getServiceInstanceV1Request.getServiceInstanceV1Req = new GetServiceInstanceV1Req();
                request.getServiceInstanceV1Request.getServiceInstanceV1Req.clientServiceInstanceSearchCriteria = new ClientServiceInstanceSearchCriteriaV1();
                request.getServiceInstanceV1Request.getServiceInstanceV1Req.clientServiceInstanceSearchCriteria.serviceIdentityDomain = domain;
                request.getServiceInstanceV1Request.getServiceInstanceV1Req.clientServiceInstanceSearchCriteria.serviceIdentityValue = client;
                request.getServiceInstanceV1Request.getServiceInstanceV1Req.clientServiceInstanceSearchCriteria.serviceCode = serviceCode;

                string basicAuthenticationCredentials = ConfigurationManager.AppSettings["DnPUser"].ToString() + ":" + ConfigurationManager.AppSettings["DnPPassword"].ToString();

                byte[] credBuffer = new UTF8Encoding().GetBytes(basicAuthenticationCredentials);
                HttpRequestMessageProperty httpRequest = new HttpRequestMessageProperty();
                httpRequest.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(credBuffer));
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                using (ChannelFactory<ServiceProfileSyncPortType> factory = new ChannelFactory<ServiceProfileSyncPortType>("ServiceProfile_DnP"))
                {
                    ServiceProfileSyncPortType svc = factory.CreateChannel();
                    using (OperationContextScope scope = new OperationContextScope((IContextChannel)svc))
                    {
                        OperationContext.Current.OutgoingMessageProperties.Add(HttpRequestMessageProperty.Name, httpRequest);
                        System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                        WriteToFile("before executing  svc.getServiceInstanceV1");
                        serviceProfileResponse = svc.getServiceInstanceV1(request);
                        WriteToFile("before executing  svc.getServiceInstanceV1 "+ serviceProfileResponse);
                    }
                }

                if (serviceProfileResponse != null
                    && serviceProfileResponse.getServiceInstanceV1Response != null
                    && serviceProfileResponse.getServiceInstanceV1Response.standardHeader != null
                    && serviceProfileResponse.getServiceInstanceV1Response.getServiceInstanceV1Res != null
                    && serviceProfileResponse.getServiceInstanceV1Response.getServiceInstanceV1Res.messages != null
                    && serviceProfileResponse.getServiceInstanceV1Response.getServiceInstanceV1Res.messages[0] != null
                    &&
                    String.IsNullOrEmpty(serviceProfileResponse.getServiceInstanceV1Response.getServiceInstanceV1Res.messages[0].messageCode))
                {
                    response = serviceProfileResponse.getServiceInstanceV1Response.getServiceInstanceV1Res.clientServiceInstance;
                }
            }
            catch (Exception ex)
            {
                throw new DnpException(ex.Message);
            }

            return response;
        }
        public static GetClientProfileV1Res GetClientProfileV1ForThomas(string identity, string identityDomain, string orderKey)
        {
          

           WriteToFile("inside DNPwrapper  GetClientProfileV1ForThomas");
            GetClientProfileV1Res response = null;
            int retryCount = 0;
            int MaxretryCount = 5;

            getClientProfileV1Response1 profileResponse;
            getClientProfileV1Request1 profileRequest = new getClientProfileV1Request1();
            profileRequest.getClientProfileV1Request = new GetClientProfileV1Request();
            profileRequest.getClientProfileV1Request.standardHeader = new StandardHeaderBlock();
            profileRequest.getClientProfileV1Request.standardHeader.serviceState = new ServiceState();
            profileRequest.getClientProfileV1Request.standardHeader.serviceState.stateCode = "OK";
            profileRequest.getClientProfileV1Request.standardHeader.serviceAddressing = new ServiceAddressing();
            profileRequest.getClientProfileV1Request.standardHeader.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
            profileRequest.getClientProfileV1Request.getClientProfileV1Req = new GetClientProfileV1Req();
            profileRequest.getClientProfileV1Request.getClientProfileV1Req.clientSearchCriteria = new ClientSearchCriteria();
            profileRequest.getClientProfileV1Request.getClientProfileV1Req.clientSearchCriteria.identifierValue = identity;
            profileRequest.getClientProfileV1Request.getClientProfileV1Req.clientSearchCriteria.identifierDomainCode = identityDomain;
            profileRequest.getClientProfileV1Request.getClientProfileV1Req.responseParameter = new ResponseParameters();
            profileRequest.getClientProfileV1Request.getClientProfileV1Req.responseParameter.isClientIdentitiesRequired = "YES";
            profileRequest.getClientProfileV1Request.getClientProfileV1Req.responseParameter.isClientServiceInstanceRequired = "YES";
            profileRequest.getClientProfileV1Request.getClientProfileV1Req.responseParameter.isClientServiceRolesRequired = "YES";
            profileRequest.getClientProfileV1Request.getClientProfileV1Req.responseParameter.isClientServiceInstanceRequired = "YES";
            profileRequest.getClientProfileV1Request.getClientProfileV1Req.responseParameter.isLinkedProfilesRequired = "YES";
            profileRequest.getClientProfileV1Request.getClientProfileV1Req.responseParameter.isClientServiceInstanceCharacteristicsrequired = "YES";
            profileRequest.getClientProfileV1Request.getClientProfileV1Req.responseParameter.isClientServiceRoleCharacteristicRequired = "YES";

            string basicAuthenticationCredentials = ConfigurationManager.AppSettings["DnPUser"].ToString() + ":" + ConfigurationManager.AppSettings["DnPPassword"].ToString();
            byte[] credBuffer = new UTF8Encoding().GetBytes(basicAuthenticationCredentials);
            HttpRequestMessageProperty httpRequest = new HttpRequestMessageProperty();
            httpRequest.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(credBuffer));
            WriteToFile("inside DNPwrapper GetClientProfileV1 Before while (retryCount < ReadConfig.MAX_RETRY_COUNT) ");
            //Logger.Write("bptmTxnId", Guid.NewGuid(), "Inside GetClientProfileV1ForThomas in Dnpwrapper", TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", "");
            int MAX_RETRY_COUNT = Convert.ToInt32(ConfigurationManager.AppSettings["RetryCountForThomasProvide"].ToString());
            while (retryCount < MAX_RETRY_COUNT)
                {
               // Logger.Write("bptmTxnId", Guid.NewGuid(), "After GetClientProfileV1ForThomas in Dnpwrapper", TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", "");
                try
                {
                    WriteToFile("inside DNPwrapper GetClientProfileV1  while (retryCount < ReadConfig.MAX_RETRY_COUNT) ");
                    using (ChannelFactory<ClientProfileSyncPortType> factory = new ChannelFactory<ClientProfileSyncPortType>("ClientProfile_DnP"))
                    {
                        ClientProfileSyncPortType svc = factory.CreateChannel();
                        using (OperationContextScope scope = new OperationContextScope((IContextChannel)svc))
                        {
                            OperationContext.Current.OutgoingMessageProperties.Add(HttpRequestMessageProperty.Name, httpRequest);
                            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                            WriteToFile("inside DNPwrapper GetClientProfileV1 Before svc.getClientProfileV1 ");
                            profileResponse = svc.getClientProfileV1(profileRequest);
                            
                            response = profileResponse.getClientProfileV1Response.getClientProfileV1Res;
                            WriteToFile("inside DNPwrapper GetClientProfileV1 After svc.getClientProfileV1 "+ response);
                        }
                    }
                    
                    retryCount = MAX_RETRY_COUNT;
                    //retryCount = MaxretryCount;
                }
                catch (TimeoutException ex)
                {
                    WriteToFile("GetClientprofileV1ForThomas inside catch "+ex.InnerException.ToString());
                    //Trace.WriteLine(" in timeout exception");
                    //Trace.WriteLine("In Exception e" + ex.Message + " ," + retryCount);
                    retryCount++;
                    Logger.Write(orderKey + ",TimeOut Exception during GetClientProfileV1ForThomas,retrying the order ", Logger.TypeEnum.MessageTrace);
                    Logger.Write(orderKey + ",TimeOut Exception: " + ex.Message + ",retrying the order during GetClientProfileV1ForThomas", Logger.TypeEnum.ExceptionTrace);

                    System.Threading.Thread.Sleep(ReadConfig.SLEEP_TIME_OUT);
                    if (retryCount == MaxretryCount)
                    {
                        // logic to stop the service
                        //Trace.WriteLine( retryCount);
                        //Process.GetCurrentProcess().Kill();
                        //Logger.Write("TimeOut Exception during GetClientProfileV1ForThomas, reached max number of Retry , we are shutdowning service " + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.ExceptionTrace);
                        //Logger.Write("TimeOut Exception during GetClientProfileV1ForThomas, reached max number of Retry , we are shutdowning service" + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.EmailTrace);

                        Logger.Write(orderKey + ",TimeOut Exception: " + ex.Message + "during GetClientProfileV1ForThomas, reached max number of Retry: " + ReadConfig.MAX_RETRY_COUNT, Logger.TypeEnum.ExceptionTrace);
                        Logger.Write(orderKey + ",TimeOut Exception: " + ex.Message + "during GetClientProfileV1ForThomas, reached max number of Retry: " + ReadConfig.MAX_RETRY_COUNT, Logger.TypeEnum.EmailTrace);

                        throw ex;
                    }
                }

            }
            return response;
        }
        public static ClientServiceInstanceV1[] getServiceInstanceV1ForThomas(string client, string domain, string orderKey)
        {

            getServiceInstanceV1Request1 request = null;
            getServiceInstanceV1Response1 serviceProfileResponse = null;
            ClientServiceInstanceV1[] response = null;

            request = new getServiceInstanceV1Request1();
            request.getServiceInstanceV1Request = new GetServiceInstanceV1Request();
            request.getServiceInstanceV1Request.standardHeader = new StandardHeaderBlock();
            request.getServiceInstanceV1Request.standardHeader.serviceState = new ServiceState();
            request.getServiceInstanceV1Request.standardHeader.serviceState.stateCode = "OK";

            request.getServiceInstanceV1Request.standardHeader.serviceAddressing = new ServiceAddressing();
            request.getServiceInstanceV1Request.standardHeader.serviceAddressing.from = "http://www.profile.com?SAASMSEO";

            request.getServiceInstanceV1Request.getServiceInstanceV1Req = new GetServiceInstanceV1Req();
            request.getServiceInstanceV1Request.getServiceInstanceV1Req.clientServiceInstanceSearchCriteria = new ClientServiceInstanceSearchCriteriaV1();
            request.getServiceInstanceV1Request.getServiceInstanceV1Req.clientServiceInstanceSearchCriteria.serviceIdentityDomain = domain;
            request.getServiceInstanceV1Request.getServiceInstanceV1Req.clientServiceInstanceSearchCriteria.serviceIdentityValue = client;
            request.getServiceInstanceV1Request.getServiceInstanceV1Req.clientServiceInstanceSearchCriteria.serviceCode = "BTSPORT:DIGITAL";

            string basicAuthenticationCredentials = ConfigurationManager.AppSettings["DnPUser"].ToString() + ":" + ConfigurationManager.AppSettings["DnPPassword"].ToString();

            byte[] credBuffer = new UTF8Encoding().GetBytes(basicAuthenticationCredentials);
            HttpRequestMessageProperty httpRequest = new HttpRequestMessageProperty();
            int retryCount = 0;
            while (retryCount < ReadConfig.MAX_RETRY_COUNT)
            {
                try
                {
                    httpRequest.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(credBuffer));
                    using (ChannelFactory<ServiceProfileSyncPortType> factory = new ChannelFactory<ServiceProfileSyncPortType>("ServiceProfile_DnP"))
                    {
                        ServiceProfileSyncPortType svc = factory.CreateChannel();
                        using (OperationContextScope scope = new OperationContextScope((IContextChannel)svc))
                        {
                            OperationContext.Current.OutgoingMessageProperties.Add(HttpRequestMessageProperty.Name, httpRequest);
                            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                            serviceProfileResponse = svc.getServiceInstanceV1(request);
                        }
                    }
                    retryCount = ReadConfig.MAX_RETRY_COUNT;
                }
                catch (TimeoutException exp)
                {
                    retryCount++;
                    Logger.Write(orderKey + ",TimeOut Exception,retrying the order during getServiceInstanceV1ForThomas: " + exp.Message, Logger.TypeEnum.MessageTrace);
                    Logger.Write(orderKey + ",TimeOut Exception: " + exp.Message + ",retrying the order during getServiceInstanceV1ForThomas: " + exp.Message, Logger.TypeEnum.ExceptionTrace);


                    System.Threading.Thread.Sleep(ReadConfig.SLEEP_TIME_OUT);
                    if (retryCount == ReadConfig.MAX_RETRY_COUNT)
                    {
                        // logic to stop the service
                        //Process.GetCurrentProcess().Kill();
                        //Logger.Write("TimeOut Exception during getServiceInstanceV1ForThomas, reached max number of Retry , we are shutdowning service " + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.ExceptionTrace);
                        //Logger.Write("TimeOut Exception during getServiceInstanceV1ForThomas, reached max number of Retry , we are shutdowning service" + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.EmailTrace);

                        Logger.Write(orderKey + ",TimeOut Exception: " + exp.Message + "during getServiceInstanceV1ForThomas, reached max number of Retry: " + ReadConfig.MAX_RETRY_COUNT, Logger.TypeEnum.ExceptionTrace);
                        Logger.Write(orderKey + ",TimeOut Exception: " + exp.Message + "during getServiceInstanceV1ForThomas, reached max number of Retry: " + ReadConfig.MAX_RETRY_COUNT, Logger.TypeEnum.EmailTrace);
                        throw exp;
                    }
                }
            }

            if (serviceProfileResponse != null
                && serviceProfileResponse.getServiceInstanceV1Response != null
                && serviceProfileResponse.getServiceInstanceV1Response.standardHeader != null
                && serviceProfileResponse.getServiceInstanceV1Response.getServiceInstanceV1Res != null
                && serviceProfileResponse.getServiceInstanceV1Response.getServiceInstanceV1Res.messages != null
                && serviceProfileResponse.getServiceInstanceV1Response.getServiceInstanceV1Res.messages[0] != null
                &&
                String.IsNullOrEmpty(serviceProfileResponse.getServiceInstanceV1Response.getServiceInstanceV1Res.messages[0].messageCode))
            {
                response = serviceProfileResponse.getServiceInstanceV1Response.getServiceInstanceV1Res.clientServiceInstance;
            }


            return response;
        }
        public static manageClientProfileV1Response1 manageClientProfileV1Thomas(manageClientProfileV1Request1 request, string orderKey)
        {
            manageClientProfileV1Response1 profileResponse = null;
            int retryCount = 0;
            while (retryCount < ReadConfig.MAX_RETRY_COUNT)
                {
                try
                {
                    byte[] credBuffer = new UTF8Encoding().GetBytes(ConfigurationManager.AppSettings["DnPUser"].ToString() + ":" + ConfigurationManager.AppSettings["DnPPassword"].ToString());
                    HttpRequestMessageProperty httpRequest = new HttpRequestMessageProperty();
                    httpRequest.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(credBuffer));
                    using (ChannelFactory<ClientProfileSyncPortType> factory = new ChannelFactory<ClientProfileSyncPortType>("ClientProfile_DnP"))
                    {
                        ClientProfileSyncPortType svc = factory.CreateChannel();
                        using (OperationContextScope scope = new OperationContextScope((IContextChannel)svc))
                        {
                            OperationContext.Current.OutgoingMessageProperties.Add(HttpRequestMessageProperty.Name, httpRequest);
                            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                            profileResponse = svc.manageClientProfileV1(request);
                        }
                    }
                    retryCount = ReadConfig.MAX_RETRY_COUNT;
                }
                catch (TimeoutException ex)
                {
                    Logger.Write(orderKey + ",TimeOut Exception,retrying the order during manageClientProfileV1Thomas", Logger.TypeEnum.MessageTrace);
                    Logger.Write(orderKey + ",TimeOut Exception:" + ex.Message + ",retrying the order during manageClientProfileV1Thomas", Logger.TypeEnum.ExceptionTrace);

                    retryCount++;
                    System.Threading.Thread.Sleep(ReadConfig.SLEEP_TIME_OUT);
                    if (retryCount == ReadConfig.MAX_RETRY_COUNT)
                    {
                        // logic to stop the service
                        //Process.GetCurrentProcess().Kill();
                        //Logger.Write("TimeOut Exception, reached max number of Retry , we are shutdowning service " + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.ExceptionTrace);
                        //Logger.Write("TimeOut Exception, reached max number of Retry , we are shutdowning service" + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.EmailTrace);

                        Logger.Write(orderKey + ",TimeOut Exception: " + ex.Message + "during manageClientProfileV1Thomas, reached max number of Retry: " + ReadConfig.MAX_RETRY_COUNT, Logger.TypeEnum.ExceptionTrace);
                        Logger.Write(orderKey + ",TimeOut Exception: " + ex.Message + "during manageClientProfileV1Thomas, reached max number of Retry: " + ReadConfig.MAX_RETRY_COUNT, Logger.TypeEnum.EmailTrace);
                    }
                }
            }

            return profileResponse;
        }

        public static manageServiceInstanceV1Response1 manageServiceInstanceV1Thomas(manageServiceInstanceV1Request1 request, string orderkey)
        {
            WriteToFile("Inside manageServiceInstanceV1Thomas");
            manageServiceInstanceV1Response1 profileResponse = null;
            int retryCount = 0;
            WriteToFile("Before Inside  manageServiceInstanceV1Thomas retryCount < ReadConfig.MAX_RETRY_COUNT");
            while (retryCount < ReadConfig.MAX_RETRY_COUNT)
            {
                WriteToFile("Inside manageServiceInstanceV1Thomas After retryCount < ReadConfig.MAX_RETRY_COUNT");
                try
                {
                    byte[] credBuffer = new UTF8Encoding().GetBytes(ConfigurationManager.AppSettings["DnPUser"].ToString() + ":" + ConfigurationManager.AppSettings["DnPPassword"].ToString());
                    HttpRequestMessageProperty httpRequest = new HttpRequestMessageProperty();
                    httpRequest.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(credBuffer));
                    using (ChannelFactory<ServiceProfileSyncPortType> factory = new ChannelFactory<ServiceProfileSyncPortType>("ServiceProfile_DnP"))
                    {
                        ServiceProfileSyncPortType svc = factory.CreateChannel();
                        using (OperationContextScope scope = new OperationContextScope((IContextChannel)svc))
                        {
                            OperationContext.Current.OutgoingMessageProperties.Add(HttpRequestMessageProperty.Name, httpRequest);
                            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                            WriteToFile("Inside manageServiceInstanceV1Thomas Before svc.manageServiceInstanceV1");
                            profileResponse = svc.manageServiceInstanceV1(request);
                            WriteToFile("Inside manageServiceInstanceV1Thomas After svc.manageServiceInstanceV1");
                        }
                    }
                    retryCount = ReadConfig.MAX_RETRY_COUNT;
                }
                catch (TimeoutException ex)
                {
                    WriteToFile("Inside manageServiceInstanceV1Thomas in catchblock:::: "+ex.Message.ToString());
                    retryCount++;
                    Logger.Write(orderkey + ",TimeOut Exception,retrying the order during manageServiceInstanceV1Thomas", Logger.TypeEnum.MessageTrace);
                    Logger.Write(orderkey + ",TimeOut Exception: " + ex.Message + ",retrying the order during manageServiceInstanceV1Thomas", Logger.TypeEnum.ExceptionTrace);

                    retryCount++;
                    System.Threading.Thread.Sleep(ReadConfig.SLEEP_TIME_OUT);
                    if (retryCount == ReadConfig.MAX_RETRY_COUNT)
                    {
                        // logic to stop the service
                        // Process.GetCurrentProcess().Kill();
                        //Logger.Write("TimeOut Exception during manageServiceInstanceV1Thomas, reached max number of Retry , we are shutdowning service " + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.ExceptionTrace);
                        //Logger.Write("TimeOut Exception during manageServiceInstanceV1Thomas, reached max number of Retry , we are shutdowning service" + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.EmailTrace);

                        Logger.Write(orderkey + ",TimeOut Exception: " + ex.Message + "during manageServiceInstanceV1Thomas, reached max number of Retry: " + ReadConfig.MAX_RETRY_COUNT, Logger.TypeEnum.ExceptionTrace);
                        Logger.Write(orderkey + ",TimeOut Exception: " + ex.Message + "during manageServiceInstanceV1Thomas, reached max number of Retry: " + ReadConfig.MAX_RETRY_COUNT, Logger.TypeEnum.EmailTrace);
                    }

                }
            }
            return profileResponse;
        }

        public static manageClientServiceV1Response1 manageClientServiceV1(manageClientServiceV1Request1 request, string orderkey)
        {
            manageClientServiceV1Response1 profileResponse = null;

            int retryCount = 0;
            while (retryCount < ReadConfig.MAX_RETRY_COUNT)
            {
                try
                {
                    byte[] credBuffer = new UTF8Encoding().GetBytes(ConfigurationManager.AppSettings["DnPUser"].ToString() + ":" + ConfigurationManager.AppSettings["DnPPassword"].ToString());
                    HttpRequestMessageProperty httpRequest = new HttpRequestMessageProperty();
                    httpRequest.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(credBuffer));
                    using (ChannelFactory<ServiceProfileSyncPortType> factory = new ChannelFactory<ServiceProfileSyncPortType>(@"ServiceProfile_DnP"))
                    {
                        ServiceProfileSyncPortType svc = factory.CreateChannel();
                        using (OperationContextScope scope = new OperationContextScope((IContextChannel)svc))
                        {
                            OperationContext.Current.OutgoingMessageProperties.Add(HttpRequestMessageProperty.Name, httpRequest);
                            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                            profileResponse = svc.manageClientServiceV1(request);
                        }
                    }
                    retryCount = ReadConfig.MAX_RETRY_COUNT;
                }
                catch (TimeoutException ex)
                {
                    retryCount++;
                    Logger.Write(orderkey + ",TimeOut Exception,retrying the order during manageServiceInstanceV1Thomas", Logger.TypeEnum.MessageTrace);
                    Logger.Write(orderkey + ",TimeOut Exception: " + ex.Message + ",retrying the order during manageServiceInstanceV1Thomas", Logger.TypeEnum.ExceptionTrace);

                    retryCount++;
                    System.Threading.Thread.Sleep(ReadConfig.SLEEP_TIME_OUT);
                    if (retryCount == ReadConfig.MAX_RETRY_COUNT)
                    {
                        // logic to stop the service
                        // Process.GetCurrentProcess().Kill();
                        //Logger.Write("TimeOut Exception during manageServiceInstanceV1Thomas, reached max number of Retry , we are shutdowning service " + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.ExceptionTrace);
                        //Logger.Write("TimeOut Exception during manageServiceInstanceV1Thomas, reached max number of Retry , we are shutdowning service" + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.EmailTrace);

                        Logger.Write(orderkey + ",TimeOut Exception: " + ex.Message + "during manageServiceInstanceV1Thomas, reached max number of Retry: " + ReadConfig.MAX_RETRY_COUNT, Logger.TypeEnum.ExceptionTrace);
                        Logger.Write(orderkey + ",TimeOut Exception: " + ex.Message + "during manageServiceInstanceV1Thomas, reached max number of Retry: " + ReadConfig.MAX_RETRY_COUNT, Logger.TypeEnum.EmailTrace);
                    }

                }
            }
            return profileResponse;
        }

        public static void WriteToFile(string Message)
        {
            if (ConfigurationManager.AppSettings["MQLogs"] != null && ConfigurationManager.AppSettings["MQLogs"] != string.Empty)
            {
                MQLogs = ConfigurationManager.AppSettings["MQLogs"].ToString();
            }

            MQLogs = MQLogs + "\\MQLogs";
            if (!Directory.Exists(MQLogs))
            {
                Directory.CreateDirectory(MQLogs);
            }
            string filepath = MQLogs + "\\ServiceLog_" + System.DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
            if (!File.Exists(filepath))
            {
                // Create a file to write to.
                using (StreamWriter sw = File.CreateText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
        }

    }
    public class ReadConfig
    {
        //static int MAX_RETRY_COUNT = System.Int32.Parse(ConfigurationManager.AppSettings["RetryCountForThomasProvide"]);
        // public static int SLEEP_TIME_OUT = System.Int32.Parse(ConfigurationManager.AppSettings["SleepTimeoutInMilliSec"]);
        //modified CON-73882 - start
        //public static int MAX_RETRY_Count_KillSession = System.Int32.Parse(ConfigurationManager.AppSettings["RetryCountForKillSession"]);
        //modified CON-73882 - end

       public static int MAX_RETRY_COUNT = Convert.ToInt32(ConfigurationManager.AppSettings["RetryCountForThomasProvide"].ToString());
         public static int SLEEP_TIME_OUT = Convert.ToInt32(ConfigurationManager.AppSettings["SleepTimeoutInMilliSec"].ToString());
        //modified CON-73882 - start
        public static int MAX_RETRY_Count_KillSession = Convert.ToInt32(ConfigurationManager.AppSettings["RetryCountForKillSession"].ToString());
        //modified CON-73882 - end
    }



}
