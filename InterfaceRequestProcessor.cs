using System;
using System.Configuration;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using BT.SaaS.IspssAdapter;
using BT.SaaS.IspssAdapter.Dnp;
using com.bt.util.logging;

// Abstract class that need to be implemented for all the new products -- Aug -15- 2015
namespace BT.SaaS.MSEOAdapter
{
    public abstract class InterfaceRequestProcessor
    {
        public abstract bool RequestMapper(OrderRequest requestOrderRequest, ref E2ETransaction e2eData);

        protected abstract bool IsRequestValid(OrderRequest requestOrderRequest);

        /// <summary>
        /// Get Client Profile, returns client details, client identities, services and the associated roles
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="identityDomain"></param>
        /// <param name="orderkey"></param>
        /// <param name="productCode"></param>
        /// <returns></returns>
        public GetClientProfileV1Res GetClientProfile(string identity, string identityDomain, string orderkey, string productCode)
        {
            GetClientProfileV1Res response = null;
            getClientProfileV1Response1 profileResponse1;
            getClientProfileV1Request1 profileRequest = new getClientProfileV1Request1
            {
                getClientProfileV1Request = new GetClientProfileV1Request
                {
                    standardHeader = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock
                    {
                        serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState { stateCode = "OK" }
                    }
                }
            };


            profileRequest.getClientProfileV1Request.standardHeader.serviceAddressing =
                new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing { @from = "http://www.profile.com?SAASMSEO" };

            profileRequest.getClientProfileV1Request.getClientProfileV1Req = new GetClientProfileV1Req
            {
                clientSearchCriteria = new ClientSearchCriteria
                {
                    identifierValue = identity,
                    identifierDomainCode = identityDomain
                },
                responseParameter = new ResponseParameters
                {
                    isClientIdentitiesRequired = "YES",
                    isClientServiceInstanceRequired = "YES",
                    isClientServiceRolesRequired = "YES",
                    isLinkedProfilesRequired = "YES",
                    isClientServiceInstanceCharacteristicsrequired = "YES",
                    isClientServiceRoleCharacteristicRequired = "YES"
                }
            };


            //MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_IN_LOG, orderkey, profileRequest.SerializeObject(), productCode);
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
                        Logger.Write(orderkey + ",Retry, TimeOut Excp during  GCP", Logger.TypeEnum.GetClientProfilefunctinException);
                        System.Threading.Thread.Sleep(Convert.ToInt32(ConfigurationManager.AppSettings["springWaitTime"]));
                    }
                    else
                    {
                        Logger.Write(orderkey + ",Errored,TimeOut Excp during  GCP, retry reached MAX retrycount:" + ConfigurationManager.AppSettings["springRetryCount"].ToString(), Logger.TypeEnum.SpringExceptionTrace);
                        throw new DnpException(ex.Message);
                    }
                }
                catch (CommunicationException commExp)
                {
                    retryCount++;
                    if (retryCount != Convert.ToInt32(ConfigurationManager.AppSettings["springRetryCount"]))
                    {
                        Logger.Write(orderkey + ",Retry,Communication Excp during GCPV1", Logger.TypeEnum.GetClientProfilefunctinException);
                        System.Threading.Thread.Sleep(Convert.ToInt32(ConfigurationManager.AppSettings["springWaitTime"]));
                    }
                    else
                    {
                        Logger.Write(orderkey + ",Errored,Communication Excp during  GCPV1, retry reached MAX retrycount:" + ConfigurationManager.AppSettings["springRetryCount"].ToString(), Logger.TypeEnum.GetClientProfilefunctinException);
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

        /// <summary>
        /// GetServiceUser Profiles from DNP , returns services against BAC if any
        /// </summary>
        /// <param name="client">billAccountNumber</param>
        /// <param name="domain">BACID_IDENTIFER_NAMEPACE</param>
        /// <param name="orderkey">orderkey</param>
        /// <returns></returns>
        public GetBatchProfileV1Res GetServiceUserProfiles(string client, string domain, string orderkey)
        {
            getServiceUserProfilesV1Request1 request = null;
            getServiceUserProfilesV1Response1 serviceProfileResponse = null;

            GetBatchProfileV1Res response = null;
            request = new getServiceUserProfilesV1Request1
            {
                getServiceUserProfilesV1Request = new GetServiceUserProfilesV1Request
                {
                    standardHeader = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock
                    {
                        serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState { stateCode = "OK" }
                    }
                }
            };

            request.getServiceUserProfilesV1Request.standardHeader.serviceAddressing =
                new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing { @from = "http://www.profile.com?SAASMSEO" };

            request.getServiceUserProfilesV1Request.getServiceUserProfilesV1Req = new GetServiceUserProfilesV1Req
            {
                clientServiceInstanceSearchCriteria = new ClientServiceInstanceSearchCriteriaV1
                {
                    serviceIdentityDomain = domain,
                    serviceIdentityValue = client
                }
            };
            request.getServiceUserProfilesV1Request.getServiceUserProfilesV1Req.responseParameter =
                new ResponseParameters
                {
                    isClientServiceInstanceRequired = "NO",
                    isClientServiceInstanceCharacteristicsrequired = "YES"
                };

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
            return response;
        }

        /// <summary>
        /// getServiceInstance Profiles from DNP , returns services against BAC if any
        /// </summary>
        /// <param name="client">billAccountNumber</param>
        /// <param name="domain">BACID_IDENTIFER_NAMEPACE</param>
        /// <param name="serviceCode"></param>
        /// <returns></returns>
        public ClientServiceInstanceV1[] GetServiceInstance(string client, string domain, string serviceCode)
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
        /// update calls to DNP. used to manage, delete, update Service instance
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public manageServiceInstanceV1Response1 ManageServiceInstance(manageServiceInstanceV1Request1 request, string e2edata)
        {
            manageServiceInstanceV1Response1 profileResponse = null;
            string basicAuthenticationCredentials = ConfigurationManager.AppSettings["DnPUser"].ToString() + ":" + ConfigurationManager.AppSettings["DnPPassword"].ToString();
            try
            {

                request.manageServiceInstanceV1Request.standardHeader.e2e = new BT.SaaS.IspssAdapter.Dnp.E2E
                {
                    E2EDATA = e2edata
                };
                byte[] credBuffer = new UTF8Encoding().GetBytes(basicAuthenticationCredentials);
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
            }
            catch (Exception ex)
            {
                //Mye2etxn.businessError(ExceptionOccurred, ex.Message);
                //Mye2etxn.endOutboundCall(Mye2etxn.toString());

                throw;
            }
            finally
            {
                try
                {
                    //if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.standardHeader != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA))
                    //    Mye2etxn.endOutboundCall(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA);
                    //else
                    //    Mye2etxn.endOutboundCall(Mye2etxn.toString());
                }
                catch (Exception ex1)
                {
                    throw;
                }
            }
            return profileResponse;
        }

        /// <summary>
        /// update calls to DNP. used to manage, delete, update Client Profile
        /// </summary>
        /// <param name="request"></param>
        /// <param name="orderkey"></param>
        /// <returns></returns>
        public manageClientProfileV1Response1 ManageClientProfile(manageClientProfileV1Request1 request, string orderkey)
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
                        Logger.Write(orderkey + ",Retry,TimeOut Excp during MCPV1", Logger.TypeEnum.VoInfinityTrace);

                        System.Threading.Thread.Sleep(Convert.ToInt32(ConfigurationManager.AppSettings["springWaitTime"]));
                    }
                    else
                    {
                        Logger.Write(orderkey + ",Retry,TimeOut Excp during MCPV1,Retry reached MAX retrycount: " + ConfigurationManager.AppSettings["springRetryCount"].ToString(), Logger.TypeEnum.VoInfinityTrace);
                        throw ex;
                    }
                }
                catch (CommunicationException commExp)
                {
                    retryCount++;
                    if (retryCount != Convert.ToInt32(ConfigurationManager.AppSettings["springRetryCount"]))
                    {
                        Logger.Write(orderkey + ",Retry,Communication Excp during MCPV1", Logger.TypeEnum.VoInfinityTrace);

                        System.Threading.Thread.Sleep(Convert.ToInt32(ConfigurationManager.AppSettings["springWaitTime"]));
                    }
                    else
                    {
                        Logger.Write(orderkey + ",Retry,Communication Excp during MCPV1,Retry reached MAX retrycount: " + ConfigurationManager.AppSettings["springRetryCount"].ToString(), Logger.TypeEnum.VoInfinityTrace);
                        throw commExp;
                    }

                }
                catch (Exception exp)
                {
                    Logger.Write(orderkey + ",Errored,Exception during MCPV1", Logger.TypeEnum.VoInfinityTrace);
                    retryCount = Convert.ToInt32(ConfigurationManager.AppSettings["springRetryCount"]);
                    throw exp;
                }

            }
            return profileResponse;
        }
        /// <summary>
        /// update calls to DNP. used to manage, delete, update ClientIdentity. added as per BTR-83974
        /// </summary>
        /// <param name="request"></param>
        /// <param name="orderkey"></param>
        /// <returns></returns>
        public manageClientIdentityResponse1 ManageClientIdentity(manageClientIdentityRequest1 request, string orderkey)
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
    }
}