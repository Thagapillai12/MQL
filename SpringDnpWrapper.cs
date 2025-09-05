using System;
using System.Configuration;
using BT.SaaS.IspssAdapter;
using BT.SaaS.IspssAdapter.Dnp;
using BT.SaaS.Core.MDMAPI.Entities.Customer;
using System.Collections.Generic;
using System.ServiceModel.Channels;
using System.ServiceModel;
using System.Text;
using System.Linq;
using System.Diagnostics;

namespace BT.SaaS.MSEOAdapter
{
    public sealed class SpringDnpWrapper
    {
        
        public static GetClientProfileV1Res GetClientProfileV1ForSpring(string identity, string identityDomain, string orderkey, string action)
        {
            GetClientProfileV1Res response = null;
            getClientProfileV1Response1 profileResponse1;
            getClientProfileV1Request1 profileRequest = new getClientProfileV1Request1();
            profileRequest.getClientProfileV1Request = new GetClientProfileV1Request();
            profileRequest.getClientProfileV1Request.standardHeader = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
            profileRequest.getClientProfileV1Request.standardHeader.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();
            profileRequest.getClientProfileV1Request.standardHeader.serviceState.stateCode = "OK";
            profileRequest.getClientProfileV1Request.standardHeader.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
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

            if (action.Equals("provide", StringComparison.OrdinalIgnoreCase))
            {
                Logger.Write(orderkey + ",Making DNP call,Sending the Request to DNP", Logger.TypeEnum.SpringMessageTrace);
                MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_IN_LOG, orderkey, profileRequest.SerializeObject(), ConfigurationManager.AppSettings["BTSpringProductCode"].ToString());
            }
            string basicAuthenticationCredentials = ConfigurationManager.AppSettings["DnPUser"].ToString() + ":" + ConfigurationManager.AppSettings["DnPPassword"].ToString();
            byte[] credBuffer = new UTF8Encoding().GetBytes(basicAuthenticationCredentials);
            HttpRequestMessageProperty httpRequest = new HttpRequestMessageProperty();
            httpRequest.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(credBuffer));
            int retryCount = 0;
            while (retryCount < Convert.ToInt32(ConfigurationManager.AppSettings["springRetryCount"]))
            {
                try
                {
                    using (ChannelFactory<ClientProfileSyncPortType> factory = new ChannelFactory<ClientProfileSyncPortType>("ClientProfile_DnP"))
                    {
                        ClientProfileSyncPortType svc = factory.CreateChannel();
                        using (OperationContextScope scope = new OperationContextScope((IContextChannel)svc))
                        {
                            OperationContext.Current.OutgoingMessageProperties.Add(HttpRequestMessageProperty.Name, httpRequest);
                            profileResponse1 = svc.getClientProfileV1(profileRequest);
                            response = profileResponse1.getClientProfileV1Response.getClientProfileV1Res;
                        }
                    }
                    retryCount = Convert.ToInt32(ConfigurationManager.AppSettings["springRetryCount"]);
                }
                catch (TimeoutException ex)
                {
                    retryCount++;
                    if (retryCount != Convert.ToInt32(ConfigurationManager.AppSettings["springRetryCount"]))
                    {
                        Logger.Write(orderkey + ",Retry, TimeOut Excp during  GCPV1", Logger.TypeEnum.SpringMessageTrace);
                        Logger.Write(orderkey + ",Retry,TimeOut Excp during  GCPV1", Logger.TypeEnum.SpringExceptionTrace);

                        System.Threading.Thread.Sleep(Convert.ToInt32(ConfigurationManager.AppSettings["springWaitTime"]));
                    }
                    else
                    {
                        Logger.Write(orderkey + ",Retry,TimeOut Excp during  GCPV1, retry reached MAX retrycount:" + ConfigurationManager.AppSettings["springRetryCount"].ToString(), Logger.TypeEnum.SpringMessageTrace);
                        Logger.Write(orderkey + ",Errored,TimeOut Excp during  GCPV1, retry reached MAX retrycount:" + ConfigurationManager.AppSettings["springRetryCount"].ToString(), Logger.TypeEnum.SpringExceptionTrace);
                        throw new DnpException(ex.Message);
                    }
                }
                catch (CommunicationException commExp)
                {
                        retryCount++;
                        if (retryCount != Convert.ToInt32(ConfigurationManager.AppSettings["springRetryCount"]))
                        {
                            Logger.Write(orderkey + ",Retry,Communication Excp during GCPV1" , Logger.TypeEnum.SpringMessageTrace);
                            Logger.Write(orderkey + ",Retry,Communication Excp during GCPV1" , Logger.TypeEnum.SpringExceptionTrace);

                            System.Threading.Thread.Sleep(Convert.ToInt32(ConfigurationManager.AppSettings["springWaitTime"]));
                        }
                        else
                        {
                            Logger.Write(orderkey + ",Retry,Communication Excp during  GCPV1, retry reached MAX retrycount:" + ConfigurationManager.AppSettings["springRetryCount"].ToString(), Logger.TypeEnum.SpringMessageTrace);
                            Logger.Write(orderkey + ",Errored,Communication Excp during  GCPV1, retry reached MAX retrycount:" + ConfigurationManager.AppSettings["springRetryCount"].ToString(), Logger.TypeEnum.SpringExceptionTrace);
                            throw new DnpException(commExp.Message);
                        }

                }
                catch (Exception exp)
                {
                    Logger.Write(orderkey + ",Failed,Exception during  GCPV1", Logger.TypeEnum.SpringMessageTrace);
                    Logger.Write(orderkey + ",Errored,Exception during  GCPV1", Logger.TypeEnum.SpringExceptionTrace);
                    retryCount = Convert.ToInt32(ConfigurationManager.AppSettings["springRetryCount"]);
                    throw new DnpException(exp.Message);
                }
            }
            return response;
        }
        public static GetBatchProfileV1Res GetServiceUserProfilesV1ForSpring(string client, string domain, string orderkey,bool isEEspring=false)
        {
            getServiceUserProfilesV1Request1 request = null;
            getServiceUserProfilesV1Response1 serviceProfileResponse = null;

            GetBatchProfileV1Res response = null;
            request = new getServiceUserProfilesV1Request1();
            request.getServiceUserProfilesV1Request = new GetServiceUserProfilesV1Request();
            request.getServiceUserProfilesV1Request.standardHeader = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
            request.getServiceUserProfilesV1Request.standardHeader.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();
            request.getServiceUserProfilesV1Request.standardHeader.serviceState.stateCode = "OK";

            request.getServiceUserProfilesV1Request.standardHeader.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
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
            int retryCount = 0;
            while (retryCount < Convert.ToInt32(ConfigurationManager.AppSettings["springRetryCount"]))
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
                            serviceProfileResponse = svc.getServiceUserProfilesV1(request);
                        }
                    }
                    retryCount = Convert.ToInt32(ConfigurationManager.AppSettings["springRetryCount"]);
                }
                catch (TimeoutException ex)
                {
                    retryCount++;
                    if (retryCount != Convert.ToInt32(ConfigurationManager.AppSettings["springRetryCount"]))
                    {
                        Logger.Write(orderkey + ",Retry,TimeOut Excp during GSUPV1", Logger.TypeEnum.SpringMessageTrace);
                        Logger.Write(orderkey + ",Retry,TimeOut Excp during GSUPV1", Logger.TypeEnum.SpringExceptionTrace);

                        System.Threading.Thread.Sleep(Convert.ToInt32(ConfigurationManager.AppSettings["springWaitTime"]));
                    }
                    else
                    {
                        Logger.Write(orderkey + ",Retry,TimeOut Excp during  GSUPV1, Retry reached MAX retrycount: " + ConfigurationManager.AppSettings["springRetryCount"].ToString(), Logger.TypeEnum.SpringMessageTrace);
                        Logger.Write(orderkey + ",Retry,TimeOut Excp during  GSUPV1,Retry reached MAX retrycount: " + ConfigurationManager.AppSettings["springRetryCount"].ToString(), Logger.TypeEnum.SpringExceptionTrace);
                        throw new DnpException(ex.Message);
                    }

                }
                catch (CommunicationException commExp)
                {
                     retryCount++;
                     if (retryCount != Convert.ToInt32(ConfigurationManager.AppSettings["springRetryCount"]))
                     {
                         Logger.Write(orderkey + ",Retry, Communication Excp during GSUPV1", Logger.TypeEnum.SpringMessageTrace);
                         Logger.Write(orderkey + ",Retry,Communication Excp during  GSUPV1", Logger.TypeEnum.SpringExceptionTrace);

                         System.Threading.Thread.Sleep(Convert.ToInt32(ConfigurationManager.AppSettings["springWaitTime"]));
                     }
                     else
                     {
                        Logger.Write(orderkey + ",Retry,Communication Excp during  GSUPV1,Retry reached MAX retrycount: " + ConfigurationManager.AppSettings["springRetryCount"].ToString(), Logger.TypeEnum.SpringMessageTrace);
                        Logger.Write(orderkey + ",Retry,Communication Excp during  GSUPV1,Retry reached MAX retrycount: " + ConfigurationManager.AppSettings["springRetryCount"].ToString(), Logger.TypeEnum.SpringExceptionTrace);
                        throw new DnpException(commExp.Message);
                    }

                }
                catch (Exception excep)
                {
                    Logger.Write(orderkey + ",Failed,Exception during GSUPV1", Logger.TypeEnum.SpringMessageTrace);
                    Logger.Write(orderkey + ",Errored,Exception during GSUPV1", Logger.TypeEnum.SpringExceptionTrace);
                    retryCount = Convert.ToInt32(ConfigurationManager.AppSettings["springRetryCount"]);
                    throw new DnpException(excep.Message);

                }
            }
                if (serviceProfileResponse != null
                && serviceProfileResponse.getServiceUserProfilesV1Response != null
                && serviceProfileResponse.getServiceUserProfilesV1Response.standardHeader != null
                && serviceProfileResponse.getServiceUserProfilesV1Response.getServiceUserProfilesV1Res != null
                && serviceProfileResponse.getServiceUserProfilesV1Response.getServiceUserProfilesV1Res.messages != null
                && serviceProfileResponse.getServiceUserProfilesV1Response.getServiceUserProfilesV1Res.messages[0] != null)
                {
                    response = serviceProfileResponse.getServiceUserProfilesV1Response.getServiceUserProfilesV1Res;
                }
            if (!isEEspring)
                response = DelegateResponse(response);

            return response;
        }
        public static manageClientProfileV1Response1 manageClientProfileV1ForSpring(manageClientProfileV1Request1 request, string orderkey)
        {
            manageClientProfileV1Response1 profileResponse = null;
            int retryCount = 0;
            while (retryCount < Convert.ToInt32(ConfigurationManager.AppSettings["springRetryCount"]))
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
                            profileResponse = svc.manageClientProfileV1(request);
                        }
                    }
                    retryCount = Convert.ToInt32(ConfigurationManager.AppSettings["springRetryCount"]);
                }
                catch (TimeoutException ex)
                {
                    retryCount++;
                    if (retryCount != Convert.ToInt32(ConfigurationManager.AppSettings["springRetryCount"]))
                    {
                        Logger.Write(orderkey + ",Retry,TimeOut Excp during MCPV1", Logger.TypeEnum.SpringMessageTrace);
                        Logger.Write(orderkey + ",Retry,TimeOut Excp during MCPV1", Logger.TypeEnum.SpringExceptionTrace);

                        System.Threading.Thread.Sleep(Convert.ToInt32(ConfigurationManager.AppSettings["springWaitTime"]));
                    }
                    else
                    {
                        Logger.Write(orderkey + ",Retry,TimeOut Excp during MCPV1,Retry reached MAX retrycount: " +ConfigurationManager.AppSettings["springRetryCount"].ToString(), Logger.TypeEnum.SpringMessageTrace);
                        Logger.Write(orderkey + ",Retry,TimeOut Excp during MCPV1,Retry reached MAX retrycount: " + ConfigurationManager.AppSettings["springRetryCount"].ToString(), Logger.TypeEnum.SpringExceptionTrace);
                        throw ex;
                    }
                }
                catch (CommunicationException commExp)
                {
                     retryCount++;
                     if (retryCount != Convert.ToInt32(ConfigurationManager.AppSettings["springRetryCount"]))
                     {
                         Logger.Write(orderkey + ",Retry, Communication Excp during MCPV1", Logger.TypeEnum.SpringMessageTrace);
                         Logger.Write(orderkey + ",Retry,Communication Excp during MCPV1", Logger.TypeEnum.SpringExceptionTrace);

                         System.Threading.Thread.Sleep(Convert.ToInt32(ConfigurationManager.AppSettings["springWaitTime"]));
                     }
                     else
                     {
                         Logger.Write(orderkey + ",Retry,Communication Excp during MCPV1,Retry reached MAX retrycount: " + ConfigurationManager.AppSettings["springRetryCount"].ToString(), Logger.TypeEnum.SpringMessageTrace);
                         Logger.Write(orderkey + ",Retry,Communication Excp during MCPV1,Retry reached MAX retrycount: " + ConfigurationManager.AppSettings["springRetryCount"].ToString(), Logger.TypeEnum.SpringExceptionTrace);
                         throw commExp;
                     }

                }
                catch (Exception exp)
                {
                    Logger.Write(orderkey + ",Failed,Exception during MCPV1", Logger.TypeEnum.SpringMessageTrace);
                    Logger.Write(orderkey + ",Errored,Exception during MCPV1", Logger.TypeEnum.SpringExceptionTrace);
                    retryCount = Convert.ToInt32(ConfigurationManager.AppSettings["springRetryCount"]);
                    throw exp;
                }

            }
            return profileResponse;
        }

        public static manageClientIdentityResponse1 manageClientIdentity(manageClientIdentityRequest1 request, string orderkey)
        {
            manageClientIdentityResponse1 profileResponse = null;
            int retryCount = 0;
            while (retryCount < Convert.ToInt32(ConfigurationManager.AppSettings["springRetryCount"]))
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
                            profileResponse = svc.manageClientIdentity(request);
                        }
                    }
                    retryCount = Convert.ToInt32(ConfigurationManager.AppSettings["springRetryCount"]);
                }
                catch (TimeoutException ex)
                {
                    retryCount++;
                    if (retryCount != Convert.ToInt32(ConfigurationManager.AppSettings["springRetryCount"]))
                    {
                        Logger.Write(orderkey + ",Retry,TimeOut Excp during manageClientIdentity", Logger.TypeEnum.SpringMessageTrace);
                        Logger.Write(orderkey + ",Retry,TimeOut Excp during manageClientIdentity", Logger.TypeEnum.SpringExceptionTrace);

                        System.Threading.Thread.Sleep(Convert.ToInt32(ConfigurationManager.AppSettings["springWaitTime"]));
                    }
                    else
                    {
                        Logger.Write(orderkey + ",Retry,TimeOut Excp during manageClientIdentity,Retry reached MAX retrycount: " + ConfigurationManager.AppSettings["springRetryCount"].ToString(), Logger.TypeEnum.SpringMessageTrace);
                        Logger.Write(orderkey + ",Retry,TimeOut Excp during manageClientIdentity,Retry reached MAX retrycount: " + ConfigurationManager.AppSettings["springRetryCount"].ToString(), Logger.TypeEnum.SpringExceptionTrace);
                        throw ex;
                    }
                }
                catch (CommunicationException commExp)
                {
                    retryCount++;
                    if (retryCount != Convert.ToInt32(ConfigurationManager.AppSettings["springRetryCount"]))
                    {
                        Logger.Write(orderkey + ",Retry, Communication Excp during  manageClientIdentity", Logger.TypeEnum.SpringMessageTrace);
                        Logger.Write(orderkey + ",Retry,Communication Excp during  manageClientIdentity", Logger.TypeEnum.SpringExceptionTrace);

                        System.Threading.Thread.Sleep(Convert.ToInt32(ConfigurationManager.AppSettings["springWaitTime"]));
                    }
                    else
                    {
                        Logger.Write(orderkey + ",Retry,Communication Excp during  manageClientIdentity,Retry reached MAX retrycount: " + ConfigurationManager.AppSettings["springRetryCount"].ToString(), Logger.TypeEnum.SpringMessageTrace);
                        Logger.Write(orderkey + ",Retry,Communication Excp during  manageClientIdentity,Retry reached MAX retrycount: " + ConfigurationManager.AppSettings["springRetryCount"].ToString(), Logger.TypeEnum.SpringExceptionTrace);
                        throw commExp;
                    }

                }
                catch (Exception exp)
                {
                    Logger.Write(orderkey + ",Failed,Exception during manageClientIdentity", Logger.TypeEnum.SpringMessageTrace);
                    Logger.Write(orderkey + ",Errored, Exception during manageClientIdentity", Logger.TypeEnum.SpringExceptionTrace);
                    retryCount = Convert.ToInt32(ConfigurationManager.AppSettings["springRetryCount"]);
                    throw exp;
                }
            }
          
            return profileResponse;

        }

        public static manageBatchProfilesV1Response1 manageBatchProfilesV1(manageBatchProfilesV1Request1 request, string orderkey)
        {
            manageBatchProfilesV1Response1 profileResponse = null;
            int retryCount = 0;
            while (retryCount < Convert.ToInt32(ConfigurationManager.AppSettings["springRetryCount"]))
            {
                try
                {
                    byte[] credBuffer = new UTF8Encoding().GetBytes(ConfigurationManager.AppSettings["DnPUser"].ToString() + ":" + ConfigurationManager.AppSettings["DnPPassword"].ToString());

                    HttpRequestMessageProperty httpRequest = new HttpRequestMessageProperty();
                    httpRequest.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(credBuffer));
                    using (ChannelFactory<ClientProfileSyncPortType> factory = new ChannelFactory<ClientProfileSyncPortType>(@"ClientProfile_DnP"))
                    {
                        ClientProfileSyncPortType svc = factory.CreateChannel();
                        using (OperationContextScope scope = new OperationContextScope((IContextChannel)svc))
                        {
                            OperationContext.Current.OutgoingMessageProperties.Add(HttpRequestMessageProperty.Name, httpRequest);
                            profileResponse = svc.manageBatchProfilesV1(request);
                        }
                    }
                    retryCount = Convert.ToInt32(ConfigurationManager.AppSettings["springRetryCount"]);
                }
                catch (TimeoutException ex)
                {
                    retryCount++;
                    if (retryCount != Convert.ToInt32(ConfigurationManager.AppSettings["springRetryCount"]))
                    {
                        Logger.Write(orderkey + ",Retry,TimeOut Excp during manageBatchProfile", Logger.TypeEnum.SpringMessageTrace);
                        Logger.Write(orderkey + ",Retry,TimeOut Excp during manageBatchProfile", Logger.TypeEnum.SpringExceptionTrace);

                        System.Threading.Thread.Sleep(Convert.ToInt32(ConfigurationManager.AppSettings["springWaitTime"]));
                    }
                    else
                    {
                        Logger.Write(orderkey + ",Retry,TimeOut Excp during manageBatchProfile,Retry reached MAX retrycount: " + ConfigurationManager.AppSettings["springRetryCount"].ToString(), Logger.TypeEnum.SpringMessageTrace);
                        Logger.Write(orderkey + ",Retry,TimeOut Excp during manageBatchProfile,Retry reached MAX retrycount: " + ConfigurationManager.AppSettings["springRetryCount"].ToString(), Logger.TypeEnum.SpringExceptionTrace);
                        throw ex;
                    }
                }
                catch (CommunicationException commExp)
                {
                    retryCount++;
                    if (retryCount != Convert.ToInt32(ConfigurationManager.AppSettings["springRetryCount"]))
                    {
                        Logger.Write(orderkey + ",Retry, Communication Excp during  manageBatchProfile", Logger.TypeEnum.SpringMessageTrace);
                        Logger.Write(orderkey + ",Retry,Communication Excp during  manageBatchProfile", Logger.TypeEnum.SpringExceptionTrace);

                        System.Threading.Thread.Sleep(Convert.ToInt32(ConfigurationManager.AppSettings["springWaitTime"]));
                    }
                    else
                    {
                        Logger.Write(orderkey + ",Retry,Communication Excp during  manageBatchProfile,Retry reached MAX retrycount: " + ConfigurationManager.AppSettings["springRetryCount"].ToString(), Logger.TypeEnum.SpringMessageTrace);
                        Logger.Write(orderkey + ",Retry,Communication Excp during  manageBatchProfile,Retry reached MAX retrycount: " + ConfigurationManager.AppSettings["springRetryCount"].ToString(), Logger.TypeEnum.SpringExceptionTrace);
                        throw commExp;
                    }

                }
                catch (Exception exp)
                {
                    Logger.Write(orderkey + ",Failed,Exception during manageBatchProfile", Logger.TypeEnum.SpringMessageTrace);
                    Logger.Write(orderkey + ",Errored,Exception during manageBatchProfile", Logger.TypeEnum.SpringExceptionTrace);
                    retryCount = Convert.ToInt32(ConfigurationManager.AppSettings["springRetryCount"]);
                    throw exp;
                }
            }
            
            return profileResponse;
        }
        public static GetBatchProfileV1Res DelegateResponse(GetBatchProfileV1Res response)
        {
            GetBatchProfileV1Res delegatereponse = new GetBatchProfileV1Res();            
            List<ClientProfileV1> listclientProfileV1 = new List<ClientProfileV1>();

            if (response != null && response.clientProfileV1 != null)
            {   
                foreach (ClientProfileV1 profile in response.clientProfileV1)
                { 
                    bool delegateroleexists = false;
                    foreach (ClientServiceInstanceV1 serviceInstance in profile.clientServiceInstanceV1)
                    {
                        if (serviceInstance.clientServiceRole != null && serviceInstance.clientServiceRole.Count() > 0)
                        {
                            if (serviceInstance.clientServiceRole.ToList().Exists(a => (a.id.Equals("SERVICE_MANAGER", StringComparison.InvariantCulture)) || (a.id.Equals("SERVICE_USER", StringComparison.InvariantCulture))))
                            {
                                string siBacNumber = serviceInstance.serviceIdentity.ToList().Where(sid => sid.domain.Equals("VAS_BILLINGACCOUNT_ID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;

                                if (!profile.client.clientIdentity.ToList().Exists(a => (a.managedIdentifierDomain.value.Equals("VAS_BILLINGACCOUNT_ID", StringComparison.InvariantCulture) && (a.value.Equals(siBacNumber, StringComparison.OrdinalIgnoreCase)))))
                                {
                                    delegateroleexists = true;
                                    break;
                                }
                            }
                        }
                        if (serviceInstance != null && serviceInstance.clientServiceInstanceIdentifier.value == "MANAGE_ACCOUNT")
                        {
                            if (profile.client.clientIdentity.Count() == 1 && (profile.client.clientIdentity[0].managedIdentifierDomain.value == "CONKID" || profile.client.clientIdentity[0].managedIdentifierDomain.value == "EECONKID"))
                            {
                                delegateroleexists = true;
                            }
                        }
                    }
                    
                    if (!delegateroleexists)
                    {
                        listclientProfileV1.Add(profile);
                    }
                }
                delegatereponse.clientProfileV1 = listclientProfileV1.ToArray();
            }
            return delegatereponse;
        }

        public static manageServiceInstanceV1Response1 manageServiceInstanceV1(manageServiceInstanceV1Request1 request,string orderkey)
        {
            manageServiceInstanceV1Response1 profileResponse = null;
            int retryCount = 0;
            while (retryCount < Convert.ToInt32(ConfigurationManager.AppSettings["springRetryCount"]))
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
                            profileResponse = svc.manageServiceInstanceV1(request);
                        }
                    }
                    retryCount = Convert.ToInt32(ConfigurationManager.AppSettings["springRetryCount"]);
                }
                catch (TimeoutException ex)
                {
                    retryCount++;
                    if (retryCount != Convert.ToInt32(ConfigurationManager.AppSettings["springRetryCount"]))
                    {
                        Logger.Write(orderkey + ",Retry,TimeOut Excp during manageBatchProfile", Logger.TypeEnum.SpringMessageTrace);
                        Logger.Write(orderkey + ",Retry,TimeOut Excp during manageBatchProfile", Logger.TypeEnum.SpringExceptionTrace);

                        System.Threading.Thread.Sleep(Convert.ToInt32(ConfigurationManager.AppSettings["springWaitTime"]));
                    }
                    else
                    {
                        Logger.Write(orderkey + ",Retry,TimeOut Excp during manageBatchProfile,Retry reached MAX retrycount: " + ConfigurationManager.AppSettings["springRetryCount"].ToString(), Logger.TypeEnum.SpringMessageTrace);
                        Logger.Write(orderkey + ",Retry,TimeOut Excp during manageBatchProfile,Retry reached MAX retrycount: " + ConfigurationManager.AppSettings["springRetryCount"].ToString(), Logger.TypeEnum.SpringExceptionTrace);
                        throw ex;
                    }
                }
                catch (CommunicationException commExp)
                {
                    retryCount++;
                    if (retryCount != Convert.ToInt32(ConfigurationManager.AppSettings["springRetryCount"]))
                    {
                        Logger.Write(orderkey + ",Retry, Communication Excp during  manageBatchProfile", Logger.TypeEnum.SpringMessageTrace);
                        Logger.Write(orderkey + ",Retry,Communication Excp during  manageBatchProfile", Logger.TypeEnum.SpringExceptionTrace);

                        System.Threading.Thread.Sleep(Convert.ToInt32(ConfigurationManager.AppSettings["springWaitTime"]));
                    }
                    else
                    {
                        Logger.Write(orderkey + ",Retry,Communication Excp during  manageBatchProfile,Retry reached MAX retrycount: " + ConfigurationManager.AppSettings["springRetryCount"].ToString(), Logger.TypeEnum.SpringMessageTrace);
                        Logger.Write(orderkey + ",Retry,Communication Excp during  manageBatchProfile,Retry reached MAX retrycount: " + ConfigurationManager.AppSettings["springRetryCount"].ToString(), Logger.TypeEnum.SpringExceptionTrace);
                        throw commExp;
                    }

                }
                catch (Exception exp)
                {
                    Logger.Write(orderkey + ",Failed,Exception during manageBatchProfile", Logger.TypeEnum.SpringMessageTrace);
                    Logger.Write(orderkey + ",Errored,Exception during manageBatchProfile", Logger.TypeEnum.SpringExceptionTrace);
                    retryCount = Convert.ToInt32(ConfigurationManager.AppSettings["springRetryCount"]);
                    throw exp;
                }
            }

            return profileResponse;
        }

        /// <summary>
        /// getServiceInstance Profiles from DNP , returns services against BAC if any
        /// </summary>
        /// <param name="client">billAccountNumber</param>
        /// <param name="domain">BACID_IDENTIFER_NAMEPACE</param>
        /// <param name="serviceCode"></param>
        /// <returns></returns>
        public static ClientServiceInstanceV1[] GetServiceInstanceforSpring(string client, string domain, string serviceCode)
        {
            getServiceInstanceV1Request1 request = null;
            getServiceInstanceV1Response1 serviceProfileResponse = null;
            ClientServiceInstanceV1[] response = null;
            try
            {
                request = new getServiceInstanceV1Request1
                {
                    getServiceInstanceV1Request = new GetServiceInstanceV1Request
                    {
                        standardHeader = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock
                        {
                            serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState { stateCode = "OK" }
                        }
                    }
                };

                request.getServiceInstanceV1Request.standardHeader.serviceAddressing =
                    new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing { @from = "http://www.profile.com?SAASMSEO" };

                request.getServiceInstanceV1Request.getServiceInstanceV1Req = new GetServiceInstanceV1Req();
                request.getServiceInstanceV1Request.getServiceInstanceV1Req.clientServiceInstanceSearchCriteria =
                    new ClientServiceInstanceSearchCriteriaV1
                    {
                        serviceIdentityDomain = domain,
                        serviceIdentityValue = client,
                        serviceCode = serviceCode
                    };

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
                        serviceProfileResponse = svc.getServiceInstanceV1(request);
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

        /// <summary>
        /// Method used to do DNP ManageClientProfileV1 call
        /// </summary>      
        /// <param name="request">Request to DNP</param>
        /// <returns>Response returned from DNP</returns>
        public static linkClientProfilesResponse1 LinkClientProfileV1Call(linkClientProfilesRequest1 lcpRequest)
        {
            linkClientProfilesResponse1 lcpResponse = null;

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
                        lcpResponse = svc.linkClientProfiles(lcpRequest);
                    }
                }
            }
            catch (Exception exp)
            {
                throw exp;
            }


            return lcpResponse;
        }
    }
}
