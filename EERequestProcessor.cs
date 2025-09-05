using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BT.SaaS.IspssAdapter;
using BT.SaaS.IspssAdapter.Dnp;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Configuration;
using com.bt.util.logging;
using MSEO = BT.SaaS.MSEOAdapter;
using System.Text;
using System.ServiceModel.Channels;
using System.ServiceModel;

namespace BT.SaaS.MSEOAdapter
{
    public class EERequestProcessor
    {
        #region Constants
        const string BACID_IDENTIFER_NAMEPACE = "VAS_BILLINGACCOUNT_ID";
        const string EE_MSISDN_NAMESPACE = "EE_MOBILE_MSISDN";
        const string EECONKID_NAMESPACE = "EECONKID";
        const string SPRING_GSM_SERVICECODE_NAMEPACE = "SPRING_GSM";
        const string GotResponseFromDnPWithBusinessError = "GotResponseFromDnPWithBusinessError";
        const string StartedDnPCall = "StartedDnPCall";
        const string GotResponseFromDnP = "GotResponseFromDnP";
        const string ACCOUNT_HOLDER_STATUS = "Account Holder Status";
        private const string AUTHORIZATION_TYPE_BASIC = "Basic ";
        private const string HTTP_HEADER_AUTHORIZATION = "Authorization";

        private const string IMSI = "IMSI";
        private const string RVSID = "RVSID";
        private const string MSISDN = "EEMSISDN";
        private const string ACTION_SEARCH = "SEARCH";
        private const string ACTION_UPDATE = "UPDATE";
        private const string ACTION_DELETE = "DELETE";
        private const string ACTION_INSERT = "INSERT";
        private const string ACTION_LINK = "LINK";
        private const string DEFAULT_ROLE = "DEFAULT";
        private const string ACTIVE = "ACTIVE";
        private const string ADMIN_ROLE = "ADMIN";
        private const string numSIMDispatched = "1";
        private const string ACTION_FORCE_INSERT = "FORCE_INS";

        private const string BTCOM = "BTCOM";
        private const string EEONLINEID = "EEONLINEID";

        const string Accepted = "Accepted";
        const string Ignored = "Ignored";
        const string Completed = "Completed";
        const string Failed = "Failed";
        const string Errored = "Errored";
        const string MakingDnPCall = "Making DnP call";
        const string GotResponsefromDNP = "Got Response from DNP";

        const string AcceptedNotificationSent = "Accepted Notification Sent for the Order";
        const string IgnoredNotificationSent = "Ignored Notification Sent for the Order";
        const string CompletedNotificationSent = "Completed Notification Sent for the Order";
        const string FailedNotificationSent = "Failure Notification Sent for the Order";

        const string SendingRequestToDNP = "Sending the Request to DNP";
        const string ReceivedResponsefromDNP = "Recieved Response from DNP";
        const string FailureResponseFromDnP = "Recieved failure response from DnP";
        const string NullResponseFromDnP = "Response is null from DnP";
        const string DnPAdminstratorFailedResponse = "Non Functional Exception from DNP(Administrator): ";
        #endregion

        public void EERequestMapper(OrderRequest requestOrderRequest, ref E2ETransaction e2eData)
        {
            string orderKey = string.Empty;
            string billAccountNumber = string.Empty;
            string old_rvsid = string.Empty;
            string rvsidValue = string.Empty;          

            MSEOOrderNotification notification = null;
            GetBatchProfileV1Res bacProfileResponse = null;
            GetClientProfileV1Res gcp_response = null;
            EESpringParameters dnpParameters = new EESpringParameters();
            //GetServiceInstanceV1Response gsiResponse = null;
            ClientServiceInstanceV1[] clientServiceInstance = null;

            bool isEEMsisdnExists = false;
            bool isRVSIDExists = false;
            bool isIMSIExists = false;

            try
            {
                orderKey = requestOrderRequest.Order.OrderIdentifier.Value;
                notification = new MSEOOrderNotification(requestOrderRequest);
                foreach (MSEO.OrderItem orderItem in requestOrderRequest.Order.OrderItem)
                {
                    if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("billaccountnumber", StringComparison.OrdinalIgnoreCase)))
                        billAccountNumber = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("billaccountnumber", StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Value;

                    if (!String.IsNullOrEmpty(billAccountNumber))
                    {                        
                       
                        bacProfileResponse = SpringDnpWrapper.GetServiceUserProfilesV1ForSpring(billAccountNumber, BACID_IDENTIFER_NAMEPACE, orderKey,true);
                        dnpParameters.Billingaccountnumber = billAccountNumber;
                        if (orderItem.Action.Code.Equals("create", StringComparison.OrdinalIgnoreCase))
                        {
                            if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(insChar => insChar.Name.Equals(IMSI, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(insChar.Value)))
                            {
                                dnpParameters.IMSI = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(insChar => insChar.Name.Equals(IMSI, StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Value;
                            }
                            if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(insChar => insChar.Name.Equals(RVSID, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(insChar.Value)))
                            {
                                dnpParameters.RVSID = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(insChar => insChar.Name.Equals(RVSID, StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Value;
                            }
                            if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(insChar => insChar.Name.Equals(MSISDN, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(insChar.Value)))
                            {
                                dnpParameters.MSISDN = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(insChar => insChar.Name.Equals(MSISDN, StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Value;
                            }
                            if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(insChar => insChar.Name.Equals("SimCardType", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(insChar.Value)))
                            {
                                dnpParameters.SimCardType = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(insChar => insChar.Name.Equals("SimCardType", StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Value;
                            }
                            if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(insChar => insChar.Name.Equals("SSN", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(insChar.Value)))
                            {
                                dnpParameters.SSN = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(insChar => insChar.Name.Equals("SSN", StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Value;
                            }
                            if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(insChar => insChar.Name.Equals("PUK_CODE", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(insChar.Value)))
                            {
                                dnpParameters.PukCode = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(insChar => insChar.Name.Equals("PUK_CODE", StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Value;
                            }
                            if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(insChar => insChar.Name.Equals("BACOrganisationName", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(insChar.Value)))
                            {
                                dnpParameters.BACOrganisationName = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(insChar => insChar.Name.Equals("BACOrganisationName", StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Value;
                            }
                            if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(insChar => insChar.Name.Equals("BACType", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(insChar.Value)))
                            {
                                dnpParameters.BACType = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(insChar => insChar.Name.Equals("BACType", StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Value;
                            }

                            if (bacProfileResponse != null && bacProfileResponse.clientProfileV1 != null)
                            {
                                foreach (ClientProfileV1 clientProfile in bacProfileResponse.clientProfileV1)
                                {
                                    if (clientProfile.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase) && ci.value.Equals(billAccountNumber, StringComparison.OrdinalIgnoreCase)))
                                    {
                                        dnpParameters.IsExixstingAccount = true;
                                        BT.SaaS.IspssAdapter.Dnp.ClientIdentity bacClientIdentity = clientProfile.client.clientIdentity.ToList().Where(i => i.managedIdentifierDomain.value.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase) && i.value.Equals(billAccountNumber, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                                        if (bacClientIdentity.clientIdentityValidation != null)
                                            dnpParameters.IsAHTDone = bacClientIdentity.clientIdentityValidation.ToList().Exists(i => i.name.Equals("ACCOUNTTRUSTMETHOD", StringComparison.OrdinalIgnoreCase) && ConfigurationManager.AppSettings["AccountTrustMethodStatus"].Split(',').Contains(i.value.ToLower()));
                                        
                                        if (clientProfile != null && clientProfile.client != null && clientProfile.client.clientIdentity != null)
                                        { 
                                            if(clientProfile.client.clientIdentity.ToList().Exists(ci=>ci.value.Equals(dnpParameters.MSISDN,StringComparison.OrdinalIgnoreCase)))
                                            {
                                                isEEMsisdnExists = true;
                                            }
                                            else if(clientProfile.client.clientIdentity.ToList().Exists(ci=>ci.value.Equals(dnpParameters.RVSID,StringComparison.OrdinalIgnoreCase)))
                                            {
                                                isRVSIDExists = true;                                               
                                            }
                                            else if(clientProfile.client.clientIdentity.ToList().Exists(ci=>ci.value.Equals(dnpParameters.IMSI,StringComparison.OrdinalIgnoreCase)))
                                            {
                                                isIMSIExists = true;                                                
                                            }
                                        }
                                    }
                                    if (clientProfile.clientServiceInstanceV1 != null && clientProfile.clientServiceInstanceV1.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(SPRING_GSM_SERVICECODE_NAMEPACE, StringComparison.OrdinalIgnoreCase)
                                        && si.serviceIdentity.ToList().Exists(sIden => sIden.domain.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase) && sIden.value.Equals(billAccountNumber, StringComparison.OrdinalIgnoreCase))))
                                    {
                                        dnpParameters.ServiceInstanceExists = clientProfile.clientServiceInstanceV1.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(SPRING_GSM_SERVICECODE_NAMEPACE, StringComparison.OrdinalIgnoreCase));

                                        if (clientProfile.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(EE_MSISDN_NAMESPACE, StringComparison.OrdinalIgnoreCase)))                                       
                                            dnpParameters.oldMSISDN = clientProfile.client.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(EE_MSISDN_NAMESPACE, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;

                                        if (clientProfile.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("EECONKID", StringComparison.OrdinalIgnoreCase)))
                                            dnpParameters.EEConkid = clientProfile.client.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("EECONKID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;

                                        if (clientProfile.client.clientIdentity.ToList().Exists(ci => ci.value.Equals(dnpParameters.MSISDN, StringComparison.OrdinalIgnoreCase)))
                                        {
                                            isEEMsisdnExists = true;
                                        }
                                        else if (clientProfile.client.clientIdentity.ToList().Exists(ci => ci.value.Equals(dnpParameters.RVSID, StringComparison.OrdinalIgnoreCase)))
                                        {
                                            isRVSIDExists = true;
                                        }
                                        else if (clientProfile.client.clientIdentity.ToList().Exists(ci => ci.value.Equals(dnpParameters.IMSI, StringComparison.OrdinalIgnoreCase)))
                                        {
                                            isIMSIExists = true;
                                        }
                                    }
                                }
                                if (!dnpParameters.ServiceInstanceExists)
                                {
                                    clientServiceInstance = SpringDnpWrapper.GetServiceInstanceforSpring(billAccountNumber, BACID_IDENTIFER_NAMEPACE, "SPRING_GSM");
                                    dnpParameters.IsStandaloneServiceExists = clientServiceInstance != null && clientServiceInstance.Count() > 0 && clientServiceInstance.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(SPRING_GSM_SERVICECODE_NAMEPACE, StringComparison.OrdinalIgnoreCase));
                                    //Setting service instance exists flag to avoid changes in MCP call...
                                    dnpParameters.ServiceInstanceExists = dnpParameters.IsStandaloneServiceExists;
                                }

                            }
                            else
                            {
                                gcp_response = SpringDnpWrapper.GetClientProfileV1ForSpring(billAccountNumber, BACID_IDENTIFER_NAMEPACE, orderKey, "create");
                                if (gcp_response != null && gcp_response.clientProfileV1 != null && gcp_response.clientProfileV1.client != null &&
                                    gcp_response.clientProfileV1.client.clientIdentity != null)
                                {
                                    if (gcp_response.clientProfileV1.client.clientIdentity.ToList().Exists(id => id.managedIdentifierDomain.value.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase)))
                                    {
                                        dnpParameters.IsExixstingAccount = true;
                                        var ci = gcp_response.clientProfileV1.client.clientIdentity.ToList().Where(id => id.managedIdentifierDomain.value.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                                        if (ci.clientIdentityValidation != null)
                                            dnpParameters.IsAHTDone = ci.clientIdentityValidation.ToList().Exists(i => i.name.Equals("ACCOUNTTRUSTMETHOD", StringComparison.OrdinalIgnoreCase) && ConfigurationManager.AppSettings["AccountTrustMethodStatus"].Split(',').Contains(i.value.ToLower()));
                                    }

                                    if (gcp_response.clientProfileV1.client.clientIdentity.ToList().Exists(id => id.managedIdentifierDomain.value.Equals(BTCOM, StringComparison.OrdinalIgnoreCase)))
                                    {
                                        dnpParameters.BtoneId = gcp_response.clientProfileV1.client.clientIdentity.ToList().Where(id => id.managedIdentifierDomain.value.Equals(BTCOM, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                    }
                                    else if (gcp_response.clientProfileV1.client.clientIdentity.ToList().Exists(id => id.managedIdentifierDomain.value.Equals(EEONLINEID, StringComparison.OrdinalIgnoreCase)))
                                    {
                                        dnpParameters.IsEECustomer = true;
                                        dnpParameters.BtoneId = gcp_response.clientProfileV1.client.clientIdentity.ToList().Where(id => id.managedIdentifierDomain.value.Equals(EEONLINEID, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                    }
                                }
                                else
                                {
                                    clientServiceInstance = SpringDnpWrapper.GetServiceInstanceforSpring(billAccountNumber, BACID_IDENTIFER_NAMEPACE, "SPRING_GSM");
                                    dnpParameters.IsStandaloneServiceExists = clientServiceInstance != null && clientServiceInstance.Count() > 0 && clientServiceInstance.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(SPRING_GSM_SERVICECODE_NAMEPACE, StringComparison.OrdinalIgnoreCase));
                                    //Setting service instance exists flag to avoid changes in MCP call...
                                    dnpParameters.ServiceInstanceExists = dnpParameters.IsStandaloneServiceExists;
                                }
                            }
                            if (!isEEMsisdnExists)
                            {
                                GetClientProfileV1Res msisdngcpResponse = SpringDnpWrapper.GetClientProfileV1ForSpring(dnpParameters.MSISDN, EE_MSISDN_NAMESPACE, orderKey, "create");

                                if (msisdngcpResponse != null && msisdngcpResponse.clientProfileV1 != null && msisdngcpResponse.clientProfileV1.client != null&& msisdngcpResponse.clientProfileV1.client.clientIdentity!=null)
                                {
                                    dnpParameters.IsMsisdnExists = true;

                                    if (msisdngcpResponse.clientProfileV1.clientServiceInstanceV1 != null && msisdngcpResponse.clientProfileV1.clientServiceInstanceV1.Count() > 0
                                    && msisdngcpResponse.clientProfileV1.clientServiceInstanceV1.ToList().Exists(csi => csi.clientServiceInstanceIdentifier.value.Equals(SPRING_GSM_SERVICECODE_NAMEPACE)))
                                    {
                                        ClientServiceInstanceV1 service = msisdngcpResponse.clientProfileV1.clientServiceInstanceV1.ToList().Where(csi => csi.clientServiceInstanceIdentifier.value.Equals(SPRING_GSM_SERVICECODE_NAMEPACE)).FirstOrDefault();
                                        if(service!=null&&service.clientServiceRole.ToList().Exists(csr=>csr.clientIdentity!=null&&csr.clientIdentity.Count()>0&&csr.clientIdentity.ToList().Exists(ci=>ci.value.Equals(dnpParameters.MSISDN,StringComparison.OrdinalIgnoreCase))))
                                            dnpParameters.IsMsisdnhasService = true;
                                    }
                                }
                            }
                            //need to change the logging file
                            if (Convert.ToBoolean(ConfigurationManager.AppSettings["IsAcceptedresponseRequired"])&&(isEEMsisdnExists|| isRVSIDExists|| isIMSIExists))
                            {
                                notification.sendNotification(true, true, string.Empty, string.Empty, ref e2eData, false);
                                Logger.Write(orderKey + "," + Accepted + "," + AcceptedNotificationSent, Logger.TypeEnum.EESpringMessageTrace);
                            }
                            
                            if (isEEMsisdnExists)
                            {
                                notification.sendNotification(false, false, "001", "Ignored as EE_MSISDN value " + dnpParameters.MSISDN + " is already present in the dnp profile", ref e2eData, true);
                                Logger.Write(orderKey + "," + Ignored + "," + IgnoredNotificationSent, Logger.TypeEnum.EESpringMessageTrace);
                            }
                            else if (isRVSIDExists)
                            {
                                notification.sendNotification(false, false, "001", "Ignored as RVSID value " + dnpParameters.RVSID + " is already present in the dnp profile", ref e2eData, true);
                                Logger.Write(orderKey + "," + Ignored + "," + IgnoredNotificationSent, Logger.TypeEnum.EESpringMessageTrace);
                            }
                            else if (isIMSIExists)
                            {
                                notification.sendNotification(false, false, "001", "Ignored as IMSI value " + dnpParameters.IMSI + " is already present in the dnp profile", ref e2eData, true);
                                Logger.Write(orderKey + "," + Ignored + "," + IgnoredNotificationSent, Logger.TypeEnum.EESpringMessageTrace);
                            }
                            else
                            {
                                notification.sendNotification(true, true, string.Empty, string.Empty, ref e2eData, false);
                                Logger.Write(orderKey + "," + Accepted + "," + AcceptedNotificationSent, Logger.TypeEnum.EESpringMessageTrace);

                                if (dnpParameters.IsMsisdnExists || dnpParameters.IsMsisdnhasService)
                                {
                                    AddMsisdntoService(dnpParameters, orderKey, notification, ref e2eData);
                                }                                
                                else
                                    CreateEEServiceInstance(billAccountNumber, BACID_IDENTIFER_NAMEPACE, dnpParameters, orderKey, notification, ref e2eData);
                            }

                        }
                        else if (orderItem.Action.Code.Equals("modify", StringComparison.OrdinalIgnoreCase))
                        {  
                            dnpParameters.Reason = orderItem.Action.Reason.ToString();
                            if ((dnpParameters.Reason.Equals("PortIn", StringComparison.OrdinalIgnoreCase)) && (!string.IsNullOrEmpty(dnpParameters.Reason)))
                            {
                                if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(i => i.Name.Equals("msisdn", StringComparison.OrdinalIgnoreCase)))
                                {
                                    dnpParameters.MSISDN = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("msisdn", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                                    dnpParameters.oldMSISDN = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("msisdn", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().PreviousValue;
                                }
                                notification.sendNotification(true, true, string.Empty, string.Empty, ref e2eData,false);
                                Logger.Write(orderKey + "," + Accepted + "," + AcceptedNotificationSent, Logger.TypeEnum.EESpringMessageTrace);

                                ModifyEEServiceInstance(billAccountNumber, dnpParameters, orderKey, notification, ref e2eData);
                            }
                            else
                            {
                                notification.sendNotification(false, false, "001", "Invalid Request, modify reason is not Present in the request or it is not a portin reqeust", ref e2eData,true);
                                Logger.Write(orderKey + "," + Ignored + "," + IgnoredNotificationSent, Logger.TypeEnum.EESpringMessageTrace);
                            }
                            
                        }
                        
                    }                    
                    else
                    {
                        notification.sendNotification(false, false, "001", "Invalid Request BillAccountNumber is not Present in the request", ref e2eData,true);
                        Logger.Write(orderKey + "," + Ignored + "," + IgnoredNotificationSent, Logger.TypeEnum.EESpringMessageTrace);
                    }
                }
            }
            catch (DnpException excep)
            {
                notification.sendNotification(false, false, "777", excep.Message, ref e2eData,true);

                Logger.Write(orderKey + "," + Errored + "," + FailedNotificationSent + "," + excep.Message, Logger.TypeEnum.EESpringExceptionTrace);
                Logger.Write(orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.EESpringMessageTrace);
            }
            catch (Exception ex)
            {
                notification.sendNotification(false, false, "001", ex.Message, ref e2eData,true);

                Logger.Write(orderKey + "," + Errored + "," + FailedNotificationSent + "," + ex.Message, Logger.TypeEnum.EESpringExceptionTrace);
                Logger.Write(orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.EESpringMessageTrace);
            }
            finally
            {
                dnpParameters = null;
            }
        }

        public void CreateEEServiceInstance(string identity, string identityDomain, EESpringParameters dnpParameters, string orderKey, MSEOOrderNotification notification, ref E2ETransaction e2eData)
        {
            manageClientProfileV1Response1 profileResponse1 = null;
            manageClientProfileV1Request1 profileRequest = new manageClientProfileV1Request1();
            profileRequest.manageClientProfileV1Request = new ManageClientProfileV1Request();

            try
            {
                BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock headerBlock = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
                headerBlock.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
                headerBlock.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
                headerBlock.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();

                ManageClientProfileV1Req manageClientProfileV1Req1 = new ManageClientProfileV1Req();
                manageClientProfileV1Req1.clientProfileV1 = new ClientProfileV1();

                List<ClientIdentity> clientIdentityList = new List<ClientIdentity>();

                ClientIdentity clientIdentity = null;

                List<ClientIdentityValidation> clientIdentityvalidationList = new List<ClientIdentityValidation>();
                ClientIdentityValidation clientIdentityValidation = null;

                List<ClientIdentityValidation> clientIdentityvalidationList1 = new List<ClientIdentityValidation>();
                ClientIdentityValidation clientIdentityValidation1 = null;

                List<ClientIdentityValidation> clientIdentityvalidationList2 = new List<ClientIdentityValidation>();
                ClientIdentityValidation clientIdentityValidation2 = null;

                if (!string.IsNullOrEmpty(dnpParameters.IMSI))
                {
                    clientIdentity = new ClientIdentity();
                    clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientIdentity.managedIdentifierDomain.value = IMSI;
                    clientIdentity.value = dnpParameters.IMSI;
                    clientIdentity.clientIdentityStatus = new ClientIdentityStatus();
                    clientIdentity.clientIdentityStatus.value = ACTIVE;

                    clientIdentityValidation = new ClientIdentityValidation();
                    clientIdentityValidation.name = "SSN";
                    clientIdentityValidation.value = dnpParameters.SSN;
                    clientIdentityValidation.action = ACTION_INSERT;
                    clientIdentityvalidationList.Add(clientIdentityValidation);

                    clientIdentityValidation = new ClientIdentityValidation();
                    clientIdentityValidation.name = "SIM_CARD_TYPE";
                    clientIdentityValidation.value = dnpParameters.SimCardType;
                    clientIdentityValidation.action = ACTION_INSERT;
                    clientIdentityvalidationList.Add(clientIdentityValidation);

                    clientIdentityValidation = new ClientIdentityValidation();
                    clientIdentityValidation.name = "PUK_CODE";
                    clientIdentityValidation.value = dnpParameters.PukCode;
                    clientIdentityValidation.action = ACTION_INSERT;
                    clientIdentityvalidationList.Add(clientIdentityValidation);

                    clientIdentity.clientIdentityValidation = clientIdentityvalidationList.ToArray();
                    clientIdentity.action = ACTION_INSERT;
                    clientIdentityList.Add(clientIdentity);
                }
                if (!string.IsNullOrEmpty(dnpParameters.RVSID))
                {
                    clientIdentity = new ClientIdentity();
                    clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientIdentity.managedIdentifierDomain.value = RVSID;
                    clientIdentity.value = dnpParameters.RVSID;
                    clientIdentity.clientIdentityStatus = new ClientIdentityStatus();
                    clientIdentity.clientIdentityStatus.value = ACTIVE;

                    clientIdentityValidation1 = new ClientIdentityValidation();
                    clientIdentityValidation1.name = "NUM_SIM_DISPATCHED";
                    clientIdentityValidation1.value = numSIMDispatched;
                    clientIdentityValidation1.action = ACTION_INSERT;
                    clientIdentityvalidationList1.Add(clientIdentityValidation1);

                    clientIdentityValidation1 = new ClientIdentityValidation();
                    clientIdentityValidation1.name = "PENDING_ORDER";
                    clientIdentityValidation1.value = dnpParameters.NewPendingOrderValue;
                    clientIdentityValidation1.action = ACTION_INSERT;
                    clientIdentityvalidationList1.Add(clientIdentityValidation1);

                    clientIdentityValidation1 = new ClientIdentityValidation();
                    clientIdentityValidation1.name = "INTERNATIONAL";
                    clientIdentityValidation1.value = dnpParameters.International;
                    clientIdentityValidation1.action = ACTION_INSERT;
                    clientIdentityvalidationList1.Add(clientIdentityValidation1);

                    clientIdentityValidation1 = new ClientIdentityValidation();
                    clientIdentityValidation1.name = "PREMIUM_RATE_SERVICE";
                    clientIdentityValidation1.value = dnpParameters.PremiumRateService;
                    clientIdentityValidation1.action = ACTION_INSERT;
                    clientIdentityvalidationList1.Add(clientIdentityValidation1);

                    clientIdentityValidation1 = new ClientIdentityValidation();
                    clientIdentityValidation1.name = "ROAMING";
                    clientIdentityValidation1.value = dnpParameters.Roaming;
                    clientIdentityValidation1.action = ACTION_INSERT;
                    clientIdentityvalidationList1.Add(clientIdentityValidation1);

                    clientIdentityValidation1 = new ClientIdentityValidation();
                    clientIdentityValidation1.name = "CREDIT_LIMIT";
                    clientIdentityValidation1.value = dnpParameters.CreditLimit;
                    clientIdentityValidation1.action = ACTION_INSERT;
                    clientIdentityvalidationList1.Add(clientIdentityValidation1);

                    clientIdentityValidation1 = new ClientIdentityValidation();
                    clientIdentityValidation1.name = "DATA_ROAMING_LIMIT";
                    clientIdentityValidation1.value = dnpParameters.DataRoamingLimit;
                    clientIdentityValidation1.action = ACTION_INSERT;
                    clientIdentityvalidationList1.Add(clientIdentityValidation1);

                    clientIdentityValidation1 = new ClientIdentityValidation();
                    clientIdentityValidation1.name = "PENDINGOPTIN";
                    clientIdentityValidation1.value = dnpParameters.PendingOptIn;
                    clientIdentityValidation1.action = ACTION_INSERT;
                    clientIdentityvalidationList1.Add(clientIdentityValidation1);

                    clientIdentityValidation1 = new ClientIdentityValidation();
                    clientIdentityValidation1.name = "EUOPTIN";
                    clientIdentityValidation1.value = dnpParameters.EUOPTIN;
                    clientIdentityValidation1.action = ACTION_INSERT;
                    clientIdentityvalidationList1.Add(clientIdentityValidation1);


                    clientIdentityValidation1 = new ClientIdentityValidation();
                    clientIdentityValidation1.name = "PC_PROFILE";
                    clientIdentityValidation1.value = dnpParameters.PCProfile;
                    clientIdentityValidation1.action = ACTION_INSERT;
                    clientIdentityvalidationList1.Add(clientIdentityValidation1);

                    clientIdentity.clientIdentityValidation = clientIdentityvalidationList1.ToArray();
                    clientIdentity.action = ACTION_INSERT;
                    clientIdentityList.Add(clientIdentity);
                }

                clientIdentity = new ClientIdentity();
                clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                clientIdentity.managedIdentifierDomain.value = EE_MSISDN_NAMESPACE;
                clientIdentity.value = dnpParameters.MSISDN;
                clientIdentity.clientIdentityStatus = new ClientIdentityStatus();
                clientIdentity.clientIdentityStatus.value = ACTIVE;

                clientIdentityValidation2 = new ClientIdentityValidation();
                clientIdentityValidation2.name = "AUTH_PIN";
                clientIdentityValidation2.value = "000000";
                clientIdentityValidation2.action = ACTION_INSERT;
                clientIdentityvalidationList2.Add(clientIdentityValidation2);

                clientIdentity.clientIdentityValidation = clientIdentityvalidationList2.ToArray();
                clientIdentity.action = ACTION_INSERT;
                clientIdentityList.Add(clientIdentity);

                if (!dnpParameters.ServiceInstanceExists)
                {
                    ServiceIdentity[] serviceIdentity = new ServiceIdentity[1];
                    serviceIdentity[0] = new ServiceIdentity();
                    serviceIdentity[0].domain = BACID_IDENTIFER_NAMEPACE;
                    serviceIdentity[0].value = identity;
                    serviceIdentity[0].action = ACTION_LINK;                    

                    ClientServiceInstanceV1[] clientServiceInstanceV1 = new ClientServiceInstanceV1[1];
                    clientServiceInstanceV1[0] = new ClientServiceInstanceV1();
                    clientServiceInstanceV1[0].clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                    clientServiceInstanceV1[0].clientServiceInstanceIdentifier.value = "SPRING_GSM";
                    clientServiceInstanceV1[0].clientServiceInstanceStatus = new ClientServiceInstanceStatus();
                    clientServiceInstanceV1[0].clientServiceInstanceStatus.value = ACTIVE;
                    clientServiceInstanceV1[0].serviceIdentity = serviceIdentity;
                    clientServiceInstanceV1[0].action = ACTION_INSERT;

                    //List<ClientServiceInstanceCharacteristic> Characteristiclist = new List<ClientServiceInstanceCharacteristic>();
                    //ClientServiceInstanceCharacteristic characteristic = null;

                    //characteristic = new ClientServiceInstanceCharacteristic();
                    //characteristic.name = "BACOrganisationName";
                    //characteristic.value = dnpParameters.BACOrganisationName;
                    //characteristic.action = ACTION_INSERT;
                    //Characteristiclist.Add(characteristic);

                    //characteristic = new ClientServiceInstanceCharacteristic();
                    //characteristic.name = "BACType";
                    //characteristic.value = dnpParameters.BACType;
                    //characteristic.action = ACTION_INSERT;
                    //Characteristiclist.Add(characteristic);

                    //clientServiceInstanceV1[0].clientServiceInstanceCharacteristic = Characteristiclist.ToArray();

                    if (dnpParameters.IsExixstingAccount)
                    {
                        manageClientProfileV1Req1.clientProfileV1.client = new Client();
                        manageClientProfileV1Req1.clientProfileV1.client.action = ACTION_UPDATE;

                        clientIdentity = new ClientIdentity();
                        clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                        clientIdentity.managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;
                        clientIdentity.value = identity;
                        clientIdentity.action = ACTION_SEARCH;

                        clientIdentityList.Add(clientIdentity);

                        if (dnpParameters.IsAHTDone)
                        {
                            ClientServiceRole[] clientServiceRole = new ClientServiceRole[2];

                            clientServiceRole[0] = new ClientServiceRole();
                            clientServiceRole[0].id = ADMIN_ROLE;
                            clientServiceRole[0].clientServiceRoleStatus = new ClientServiceRoleStatus();
                            clientServiceRole[0].clientServiceRoleStatus.value = ACTIVE;
                            clientServiceRole[0].clientIdentity = new ClientIdentity[1];
                            clientServiceRole[0].clientIdentity[0] = new ClientIdentity();
                            clientServiceRole[0].clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                            clientServiceRole[0].clientIdentity[0].managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;
                            clientServiceRole[0].clientIdentity[0].value = identity;
                            clientServiceRole[0].clientIdentity[0].action = ACTION_INSERT;
                            clientServiceRole[0].action = ACTION_INSERT;

                            clientServiceRole[1] = new ClientServiceRole();
                            clientServiceRole[1].id = DEFAULT_ROLE;
                            clientServiceRole[1].clientServiceRoleStatus = new ClientServiceRoleStatus();
                            clientServiceRole[1].clientServiceRoleStatus.value = ACTIVE;

                            List<ClientIdentity> roleClientIdentitylist = new List<ClientIdentity>();
                            ClientIdentity roleClientIdentity = null;
                           
                            if (!string.IsNullOrEmpty(dnpParameters.IMSI))
                            {
                                roleClientIdentity = new ClientIdentity();
                                roleClientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                                roleClientIdentity.managedIdentifierDomain.value = IMSI;
                                roleClientIdentity.value = dnpParameters.IMSI;
                                roleClientIdentity.action = ACTION_INSERT;
                                roleClientIdentitylist.Add(roleClientIdentity);
                            }
                            if (!string.IsNullOrEmpty(dnpParameters.RVSID))
                            {
                                roleClientIdentity = new ClientIdentity();
                                roleClientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                                roleClientIdentity.managedIdentifierDomain.value = RVSID;
                                roleClientIdentity.value = dnpParameters.RVSID;
                                roleClientIdentity.action = ACTION_INSERT;
                                roleClientIdentitylist.Add(roleClientIdentity);
                            }

                            roleClientIdentity = new ClientIdentity();
                            roleClientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                            roleClientIdentity.managedIdentifierDomain.value = EE_MSISDN_NAMESPACE;
                            roleClientIdentity.value = dnpParameters.MSISDN;
                            roleClientIdentity.action = ACTION_INSERT;
                            roleClientIdentitylist.Add(roleClientIdentity);

                            clientServiceRole[1].clientIdentity = roleClientIdentitylist.ToArray();
                            clientServiceRole[1].action = ACTION_INSERT;

                            clientServiceInstanceV1[0].clientServiceRole = clientServiceRole;
                        }
                        else
                        {
                            ClientServiceRole clientServiceRole = new ClientServiceRole();

                            clientServiceRole = new ClientServiceRole();
                            clientServiceRole.id = DEFAULT_ROLE;
                            clientServiceRole.clientServiceRoleStatus = new ClientServiceRoleStatus();
                            clientServiceRole.clientServiceRoleStatus.value = ACTIVE;

                            List<ClientIdentity> roleClientIdentitylist = new List<ClientIdentity>();
                            ClientIdentity roleClientIdentity = null;

                            if (!string.IsNullOrEmpty(dnpParameters.IMSI))
                            {
                                roleClientIdentity = new ClientIdentity();
                                roleClientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                                roleClientIdentity.managedIdentifierDomain.value = IMSI;
                                roleClientIdentity.value = dnpParameters.IMSI;
                                roleClientIdentity.action = ACTION_INSERT;
                                roleClientIdentitylist.Add(roleClientIdentity);
                            }
                            if (!string.IsNullOrEmpty(dnpParameters.RVSID))
                            {
                                roleClientIdentity = new ClientIdentity();
                                roleClientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                                roleClientIdentity.managedIdentifierDomain.value = RVSID;
                                roleClientIdentity.value = dnpParameters.RVSID;
                                roleClientIdentity.action = ACTION_INSERT;
                                roleClientIdentitylist.Add(roleClientIdentity);
                            }

                            roleClientIdentity = new ClientIdentity();
                            roleClientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                            roleClientIdentity.managedIdentifierDomain.value = EE_MSISDN_NAMESPACE;
                            roleClientIdentity.value = dnpParameters.MSISDN;
                            roleClientIdentity.action = ACTION_INSERT;
                            roleClientIdentitylist.Add(roleClientIdentity);

                            clientServiceRole.clientIdentity = roleClientIdentitylist.ToArray();

                            clientServiceRole.action = ACTION_INSERT;

                            clientServiceInstanceV1[0].clientServiceRole = new ClientServiceRole[1];
                            clientServiceInstanceV1[0].clientServiceRole[0] = clientServiceRole;
                        }
                    }
                    else
                    {
                        manageClientProfileV1Req1.clientProfileV1.client = new Client();
                        manageClientProfileV1Req1.clientProfileV1.client.action = ACTION_INSERT;
                        manageClientProfileV1Req1.clientProfileV1.client.clientOrganisation = new ClientOrganisation();
                        manageClientProfileV1Req1.clientProfileV1.client.clientOrganisation.id = "EEConsumer";
                        manageClientProfileV1Req1.clientProfileV1.client.type = "CUSTOMER";
                        manageClientProfileV1Req1.clientProfileV1.client.clientStatus = new ClientStatus();
                        manageClientProfileV1Req1.clientProfileV1.client.clientStatus.value = "ACTIVE";

                        ClientServiceRole clientServiceRole = new ClientServiceRole();

                        clientServiceRole = new ClientServiceRole();
                        clientServiceRole.id = DEFAULT_ROLE;
                        clientServiceRole.clientServiceRoleStatus = new ClientServiceRoleStatus();
                        clientServiceRole.clientServiceRoleStatus.value = ACTIVE;

                        List<ClientIdentity> roleClientIdentitylist = new List<ClientIdentity>();
                        ClientIdentity roleClientIdentity = null;

                        if (!string.IsNullOrEmpty(dnpParameters.IMSI))
                        {
                            roleClientIdentity = new ClientIdentity();
                            roleClientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                            roleClientIdentity.managedIdentifierDomain.value = IMSI;
                            roleClientIdentity.value = dnpParameters.IMSI;
                            roleClientIdentity.action = ACTION_INSERT;
                            roleClientIdentitylist.Add(roleClientIdentity);
                        }
                        if (!string.IsNullOrEmpty(dnpParameters.RVSID))
                        {
                            roleClientIdentity = new ClientIdentity();
                            roleClientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                            roleClientIdentity.managedIdentifierDomain.value = RVSID;
                            roleClientIdentity.value = dnpParameters.RVSID;
                            roleClientIdentity.action = ACTION_INSERT;
                            roleClientIdentitylist.Add(roleClientIdentity);
                        }

                        roleClientIdentity = new ClientIdentity();
                        roleClientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                        roleClientIdentity.managedIdentifierDomain.value = EE_MSISDN_NAMESPACE;
                        roleClientIdentity.value = dnpParameters.MSISDN;
                        roleClientIdentity.action = ACTION_INSERT;
                        roleClientIdentitylist.Add(roleClientIdentity);

                        clientServiceRole.clientIdentity = roleClientIdentitylist.ToArray();

                        clientServiceRole.action = ACTION_INSERT;

                        clientServiceInstanceV1[0].clientServiceRole = new ClientServiceRole[1];
                        clientServiceInstanceV1[0].clientServiceRole[0] = clientServiceRole;
                    }
                    manageClientProfileV1Req1.clientProfileV1.client.clientIdentity = clientIdentityList.ToArray();
                    manageClientProfileV1Req1.clientProfileV1.clientServiceInstanceV1 = clientServiceInstanceV1;
                }
                else
                {
                    //if (inputParameterCollection.ContainsKey("isahtdone") && !(inputParameterCollection["isahtdone"].Equals("true", StringComparison.OrdinalIgnoreCase)))
                    //{
                    if(!dnpParameters.IsStandaloneServiceExists)
                    {
                        manageClientProfileV1Req1.clientProfileV1.client = new Client();
                        manageClientProfileV1Req1.clientProfileV1.client.action = ACTION_UPDATE;

                        clientIdentity = new ClientIdentity();
                        clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                        if (!string.IsNullOrEmpty(dnpParameters.oldMSISDN))
                        {
                            clientIdentity.managedIdentifierDomain.value = EE_MSISDN_NAMESPACE;
                            clientIdentity.value = dnpParameters.oldMSISDN;
                        }
                        else if (!string.IsNullOrEmpty(dnpParameters.EEConkid))
                        {
                            clientIdentity.managedIdentifierDomain.value = EECONKID_NAMESPACE;
                            clientIdentity.value = dnpParameters.EEConkid;
                        }
                        clientIdentity.action = ACTION_SEARCH;
                        clientIdentityList.Add(clientIdentity);
                    }
                    else
                    {
                        manageClientProfileV1Req1.clientProfileV1.client = new Client();
                        manageClientProfileV1Req1.clientProfileV1.client.action = ACTION_INSERT;
                        manageClientProfileV1Req1.clientProfileV1.client.clientOrganisation = new ClientOrganisation();
                        manageClientProfileV1Req1.clientProfileV1.client.clientOrganisation.id = "EEConsumer";
                        manageClientProfileV1Req1.clientProfileV1.client.type = "CUSTOMER";
                        manageClientProfileV1Req1.clientProfileV1.client.clientStatus = new ClientStatus();
                        manageClientProfileV1Req1.clientProfileV1.client.clientStatus.value = "ACTIVE";
                    }

                    ServiceIdentity[] serviceIdentity = new ServiceIdentity[1];
                    serviceIdentity[0] = new ServiceIdentity();
                    serviceIdentity[0].domain = BACID_IDENTIFER_NAMEPACE;
                    serviceIdentity[0].value = identity;
                    serviceIdentity[0].action = ACTION_SEARCH;

                    ClientServiceInstanceV1[] clientServiceInstanceV1 = new ClientServiceInstanceV1[1];
                    clientServiceInstanceV1[0] = new ClientServiceInstanceV1();
                    clientServiceInstanceV1[0].clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                    clientServiceInstanceV1[0].clientServiceInstanceIdentifier.value = "SPRING_GSM";
                    clientServiceInstanceV1[0].clientServiceInstanceStatus = new ClientServiceInstanceStatus();
                    clientServiceInstanceV1[0].clientServiceInstanceStatus.value = ACTIVE;
                    clientServiceInstanceV1[0].serviceIdentity = serviceIdentity;
                    clientServiceInstanceV1[0].action = dnpParameters.IsStandaloneServiceExists? ACTION_LINK : ACTION_UPDATE;

                    ClientServiceRole clientServiceRole = new ClientServiceRole();

                    clientServiceRole = new ClientServiceRole();
                    clientServiceRole.id = DEFAULT_ROLE;
                    clientServiceRole.clientServiceRoleStatus = new ClientServiceRoleStatus();
                    clientServiceRole.clientServiceRoleStatus.value = ACTIVE;

                    List<ClientIdentity> roleClientIdentitylist = new List<ClientIdentity>();
                    ClientIdentity roleClientIdentity = null;

                    if (!string.IsNullOrEmpty(dnpParameters.IMSI))
                    {
                        roleClientIdentity = new ClientIdentity();
                        roleClientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                        roleClientIdentity.managedIdentifierDomain.value = IMSI;
                        roleClientIdentity.value = dnpParameters.IMSI;
                        roleClientIdentity.action = ACTION_INSERT;
                        roleClientIdentitylist.Add(roleClientIdentity);
                    }
                    if (!string.IsNullOrEmpty(dnpParameters.RVSID))
                    {
                        roleClientIdentity = new ClientIdentity();
                        roleClientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                        roleClientIdentity.managedIdentifierDomain.value = RVSID;
                        roleClientIdentity.value = dnpParameters.RVSID;
                        roleClientIdentity.action = ACTION_INSERT;
                        roleClientIdentitylist.Add(roleClientIdentity);
                    }

                    roleClientIdentity = new ClientIdentity();
                    roleClientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                    roleClientIdentity.managedIdentifierDomain.value = EE_MSISDN_NAMESPACE;
                    roleClientIdentity.value = dnpParameters.MSISDN;
                    roleClientIdentity.action = ACTION_INSERT;
                    roleClientIdentitylist.Add(roleClientIdentity);

                    clientServiceRole.clientIdentity = roleClientIdentitylist.ToArray();

                    clientServiceRole.action = ACTION_INSERT;

                    clientServiceInstanceV1[0].clientServiceRole = new ClientServiceRole[1];
                    clientServiceInstanceV1[0].clientServiceRole[0] = clientServiceRole;

                    manageClientProfileV1Req1.clientProfileV1.client.clientIdentity = clientIdentityList.ToArray();
                    manageClientProfileV1Req1.clientProfileV1.clientServiceInstanceV1 = clientServiceInstanceV1;

                }

                profileRequest.manageClientProfileV1Request.standardHeader = headerBlock;
                profileRequest.manageClientProfileV1Request.manageClientProfileV1Req = manageClientProfileV1Req1;

                e2eData.logMessage(StartedDnPCall, "");
                e2eData.startOutboundCall(ConfigurationManager.AppSettings["destinationDnP"].ToString(), E2ETransaction.vREQ, SPRING_GSM_SERVICECODE_NAMEPACE);

                profileRequest.manageClientProfileV1Request.standardHeader.e2e = new BT.SaaS.IspssAdapter.Dnp.E2E();
                profileRequest.manageClientProfileV1Request.standardHeader.e2e.E2EDATA = e2eData.toString();

                Logger.Write(orderKey + "," + MakingDnPCall + "," + SendingRequestToDNP, Logger.TypeEnum.EESpringMessageTrace);
                MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_IN_LOG, orderKey, profileRequest.SerializeObject(), dnpParameters.ProductCode);

                profileResponse1 = SpringDnpWrapper.manageClientProfileV1ForSpring(profileRequest, orderKey);

                Logger.Write(orderKey + "," + GotResponsefromDNP + "," + ReceivedResponsefromDNP, Logger.TypeEnum.EESpringMessageTrace);
                MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_OUT_LOG, orderKey, profileResponse1.SerializeObject(), dnpParameters.ProductCode);

                if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null
                    && profileResponse1.manageClientProfileV1Response.standardHeader != null
                    && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState != null
                    && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState.stateCode != null
                    && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState.stateCode == "0")
                {
                    e2eData.logMessage(GotResponseFromDnP, "");
                    if (profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && !string.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                    {
                        e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                    }
                    else
                        e2eData.endOutboundCall(e2eData.toString());

                    notification.sendNotification(true, false, "004", string.Empty, ref e2eData,true);
                    Logger.Write(orderKey + "," + Completed + "," + CompletedNotificationSent, Logger.TypeEnum.EESpringMessageTrace);
                }
                else
                {
                    if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null
                        && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res != null
                        && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages != null
                        && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0] != null
                        && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description != null)
                    {
                        string errorMessage = profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description;
                        e2eData.businessError(GotResponseFromDnPWithBusinessError, errorMessage);
                        if (profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && !string.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());

                        if (errorMessage.Contains("Administrator"))
                        {
                            Logger.Write(orderKey + "," + Errored + "," + DnPAdminstratorFailedResponse + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.EESpringExceptionTrace);
                            Logger.Write(orderKey + "," + Failed + "," + FailureResponseFromDnP + "," + errorMessage, Logger.TypeEnum.EESpringMessageTrace);
                        }
                        else
                        {
                            Logger.Write(orderKey + "," + Failed + "," + FailureResponseFromDnP + "," + errorMessage, Logger.TypeEnum.EESpringMessageTrace);
                        }
                        // retryieng the create order if the service instance already exists.
                        if (errorMessage.Contains("Service Identity Value[" + identity + "] and prf_domain_cd [VAS_BILLINGACCOUNT_ID] already exists in profile store with given service"))
                        {
                            dnpParameters.ServiceInstanceExists = false;
                            GetBatchProfileV1Res bacProfileResponse = null;
                            bacProfileResponse = SpringDnpWrapper.GetServiceUserProfilesV1ForSpring(identity, identityDomain, orderKey,true);
                            if (bacProfileResponse != null && bacProfileResponse.clientProfileV1 != null)
                            {
                                foreach (ClientProfileV1 clientProfile in bacProfileResponse.clientProfileV1)
                                {

                                    if (clientProfile.clientServiceInstanceV1 != null && clientProfile.clientServiceInstanceV1.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(SPRING_GSM_SERVICECODE_NAMEPACE, StringComparison.OrdinalIgnoreCase)
                                        && si.serviceIdentity.ToList().Exists(sIden => sIden.domain.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase) && sIden.value.Equals(identity, StringComparison.OrdinalIgnoreCase))))
                                    {
                                        dnpParameters.ServiceInstanceExists = clientProfile.clientServiceInstanceV1.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(SPRING_GSM_SERVICECODE_NAMEPACE, StringComparison.OrdinalIgnoreCase));

                                        if (clientProfile.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(RVSID, StringComparison.OrdinalIgnoreCase)))
                                            dnpParameters.OldRVSID = clientProfile.client.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(RVSID, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;

                                    }
                                }
                            }
                            if (dnpParameters.ServiceInstanceExists)
                            {
                                CreateEEServiceInstance(identity, identityDomain, dnpParameters, orderKey, notification, ref e2eData);
                            }
                            else
                            {
                                notification.sendNotification(false, false, "777", errorMessage, ref e2eData,true);
                                Logger.Write(orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.EESpringMessageTrace);
                            }
                        }
                        else
                        {
                            notification.sendNotification(false, false, "777", errorMessage, ref e2eData,true);
                            Logger.Write(orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.EESpringMessageTrace);
                        }
                    }
                    else
                    {
                        Logger.Write(orderKey + "," + Failed + "," + NullResponseFromDnP, Logger.TypeEnum.EESpringMessageTrace);

                        e2eData.businessError(GotResponseFromDnPWithBusinessError, "Response is null from DnP");
                        if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());

                        notification.sendNotification(false, false, "777", "Response is null from DnP", ref e2eData,true);
                        Logger.Write(orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.EESpringMessageTrace);
                    }
                }
            }
            catch (Exception ex)
            {
                e2eData.businessError(GotResponseFromDnPWithBusinessError, ex.Message);

                if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                    e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                else
                    e2eData.endOutboundCall(e2eData.toString());

                notification.sendNotification(false, false, "777", ex.Message, ref e2eData,true);
                Logger.Write(orderKey + "," + Errored + "," + FailedNotificationSent + "," + ex.Message, Logger.TypeEnum.EESpringExceptionTrace);
                Logger.Write(orderKey + "," + Failed + "," + FailureResponseFromDnP, Logger.TypeEnum.EESpringMessageTrace);
                Logger.Write(orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.EESpringMessageTrace);
            }
        }

        public void AddMsisdntoService(EESpringParameters dnpParameters, string orderKey, MSEOOrderNotification notification, ref E2ETransaction e2eData)
        {
            manageClientProfileV1Response1 profileResponse1 = null;
            manageClientProfileV1Request1 profileRequest = new manageClientProfileV1Request1();
            profileRequest.manageClientProfileV1Request = new ManageClientProfileV1Request();

            BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock headerblock = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
            headerblock.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
            headerblock.serviceAddressing.from = @"http://www.profile.com?SAASMSEO";
            headerblock.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();
            headerblock.serviceState.stateCode = "OK";
            try
            {
                ManageClientProfileV1Req manageClientProfileV1Req1 = new ManageClientProfileV1Req();
                manageClientProfileV1Req1.clientProfileV1 = new ClientProfileV1();

                manageClientProfileV1Req1.clientProfileV1.client = new Client();
                manageClientProfileV1Req1.clientProfileV1.client.action = ACTION_UPDATE;

                List<ClientServiceRole> serviceRoleList = new List<ClientServiceRole>();
                ClientServiceRole clientServcieRole = null;
                List<ClientIdentity> clientIdentityList = new List<ClientIdentity>();

                ClientIdentity clientIdentity = null;

                if (!string.IsNullOrEmpty(dnpParameters.MSISDN))
                {
                    clientIdentity = new ClientIdentity();
                    clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientIdentity.managedIdentifierDomain.value = EE_MSISDN_NAMESPACE;
                    clientIdentity.value = dnpParameters.MSISDN;
                    clientIdentity.action = ACTION_SEARCH;
                    clientIdentity.clientIdentityStatus = new ClientIdentityStatus();
                    clientIdentity.clientIdentityStatus.value = ACTIVE;

                    clientIdentityList.Add(clientIdentity);
                }

                ServiceIdentity[] serviceIdentity = new ServiceIdentity[1];
                serviceIdentity[0] = new ServiceIdentity();
                serviceIdentity[0].domain = BACID_IDENTIFER_NAMEPACE;
                serviceIdentity[0].value = dnpParameters.Billingaccountnumber;
                if(dnpParameters.ServiceInstanceExists)
                    serviceIdentity[0].action = ACTION_SEARCH;
                else
                    serviceIdentity[0].action = ACTION_LINK;

                ClientServiceInstanceV1[] clientServiceInstanceV1 = new ClientServiceInstanceV1[1];
                clientServiceInstanceV1[0] = new ClientServiceInstanceV1();
                clientServiceInstanceV1[0].clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                clientServiceInstanceV1[0].clientServiceInstanceIdentifier.value = SPRING_GSM_SERVICECODE_NAMEPACE;
                clientServiceInstanceV1[0].clientServiceInstanceStatus = new ClientServiceInstanceStatus();
                clientServiceInstanceV1[0].serviceIdentity = serviceIdentity;
                if (dnpParameters.ServiceInstanceExists)
                    clientServiceInstanceV1[0].action = ACTION_UPDATE;
                else
                    clientServiceInstanceV1[0].action = ACTION_INSERT;                
                clientServiceInstanceV1[0].clientServiceInstanceStatus = new ClientServiceInstanceStatus();
                clientServiceInstanceV1[0].clientServiceInstanceStatus.value = ACTIVE;

                clientServcieRole = new ClientServiceRole();
                clientServcieRole.id = DEFAULT_ROLE;
                clientServcieRole.clientServiceRoleStatus = new ClientServiceRoleStatus();
                clientServcieRole.clientServiceRoleStatus.value = ACTIVE;
                clientServcieRole.action = ACTION_INSERT;

                clientIdentity = new ClientIdentity();
                clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                clientIdentity.managedIdentifierDomain.value = EE_MSISDN_NAMESPACE;
                clientIdentity.value = dnpParameters.MSISDN;
                clientIdentity.action = ACTION_INSERT;
                clientIdentity.clientIdentityStatus = new ClientIdentityStatus();
                clientIdentity.clientIdentityStatus.value = ACTIVE;

                clientServcieRole.clientIdentity = new ClientIdentity[1];
                clientServcieRole.clientIdentity[0] = clientIdentity;
                serviceRoleList.Add(clientServcieRole);

                //calling this method to get the service roles
                clientServiceInstanceV1[0].clientServiceRole = serviceRoleList.ToArray();                

                manageClientProfileV1Req1.clientProfileV1.client.clientIdentity = clientIdentityList.ToArray();
                manageClientProfileV1Req1.clientProfileV1.clientServiceInstanceV1 = clientServiceInstanceV1;

                profileRequest.manageClientProfileV1Request.manageClientProfileV1Req = manageClientProfileV1Req1;
                profileRequest.manageClientProfileV1Request.standardHeader = headerblock;

                e2eData.logMessage(StartedDnPCall, "");
                e2eData.startOutboundCall(ConfigurationManager.AppSettings["destinationDnP"].ToString(), E2ETransaction.vREQ, SPRING_GSM_SERVICECODE_NAMEPACE);

                profileRequest.manageClientProfileV1Request.standardHeader.e2e = new BT.SaaS.IspssAdapter.Dnp.E2E();
                profileRequest.manageClientProfileV1Request.standardHeader.e2e.E2EDATA = e2eData.toString();

                Logger.Write(orderKey + "," + MakingDnPCall + "," + SendingRequestToDNP, Logger.TypeEnum.EESpringMessageTrace);
                MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_IN_LOG, orderKey, profileRequest.SerializeObject(), dnpParameters.ProductCode);

                profileResponse1 = SpringDnpWrapper.manageClientProfileV1ForSpring(profileRequest, orderKey);

                Logger.Write(orderKey + "," + GotResponsefromDNP + "," + ReceivedResponsefromDNP, Logger.TypeEnum.EESpringMessageTrace);
                MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_OUT_LOG, orderKey, profileResponse1.SerializeObject(), dnpParameters.ProductCode);

                if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null
                    && profileResponse1.manageClientProfileV1Response.standardHeader != null
                    && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState != null
                    && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState.stateCode != null
                    && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState.stateCode == "0")
                {
                    e2eData.logMessage(GotResponseFromDnP, "");
                    if (profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && !string.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                    {
                        e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                    }
                    else
                        e2eData.endOutboundCall(e2eData.toString());


                    if (dnpParameters.IsAHTDone)
                    {
                        CreateSpringServiceforAHTProfile(dnpParameters, orderKey, notification, ref e2eData);
                    }
                    //move msisdn identity
                    if (Convert.ToBoolean(ConfigurationManager.AppSettings["IsMsisdnIdentityMove"]))
                    {
                        if (dnpParameters.IsAHTDone && !dnpParameters.IsMsisdnhasService)
                        {
                            linkClientProfilesResponse1 linkResponse = null;

                            linkResponse = linkMsisdntoClientProfile(dnpParameters, ref e2eData);

                            if (linkResponse != null && linkResponse.linkClientProfilesResponse != null && linkResponse.linkClientProfilesResponse.standardHeader != null
                                                    && linkResponse.linkClientProfilesResponse.standardHeader.serviceState != null
                                                    && linkResponse.linkClientProfilesResponse.standardHeader.serviceState.stateCode != null &&
                                                    linkResponse.linkClientProfilesResponse.standardHeader.serviceState.stateCode == "0")
                            {
                                notification.sendNotification(true, false, "004", string.Empty, ref e2eData, true);
                                Logger.Write(orderKey + "," + Completed + "," + CompletedNotificationSent, Logger.TypeEnum.EESpringMessageTrace);
                            }
                            else
                            {
                                string errorMessage = linkResponse.linkClientProfilesResponse.messages[0].description;
                                e2eData.businessError(GotResponseFromDnPWithBusinessError, errorMessage);
                                if (linkResponse.linkClientProfilesResponse.standardHeader.e2e != null && !string.IsNullOrEmpty(linkResponse.linkClientProfilesResponse.standardHeader.e2e.E2EDATA))
                                    e2eData.endOutboundCall(linkResponse.linkClientProfilesResponse.standardHeader.e2e.E2EDATA);
                                else
                                    e2eData.endOutboundCall(e2eData.toString());

                                notification.sendNotification(false, false, "777", errorMessage, ref e2eData, true);
                                Logger.Write(orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.EESpringMessageTrace);
                            }
                        }
                    }

                    notification.sendNotification(true, false, "004", string.Empty, ref e2eData, true);
                    Logger.Write(orderKey + "," + Completed + "," + CompletedNotificationSent, Logger.TypeEnum.EESpringMessageTrace);
                }
                else
                {
                    if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null
                        && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res != null
                        && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages != null
                        && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0] != null
                        && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description != null)
                    {
                        string errorMessage = profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description;
                        e2eData.businessError(GotResponseFromDnPWithBusinessError, errorMessage);
                        if (profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && !string.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());

                        if (errorMessage.Contains("Administrator"))
                        {
                            Logger.Write(orderKey + "," + Errored + "," + DnPAdminstratorFailedResponse + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.EESpringExceptionTrace);
                            Logger.Write(orderKey + "," + Failed + "," + FailureResponseFromDnP + "," + errorMessage, Logger.TypeEnum.EESpringMessageTrace);
                        }
                        else
                        {
                            Logger.Write(orderKey + "," + Failed + "," + FailureResponseFromDnP + "," + errorMessage, Logger.TypeEnum.EESpringMessageTrace);
                        }
                        notification.sendNotification(false, false, "777", errorMessage, ref e2eData, true);
                        Logger.Write(orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.EESpringMessageTrace);
                    }
                    else
                    {
                        Logger.Write(orderKey + "," + Failed + "," + NullResponseFromDnP, Logger.TypeEnum.EESpringMessageTrace);

                        e2eData.businessError(GotResponseFromDnPWithBusinessError, "Response is null from DnP");
                        if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());

                        notification.sendNotification(false, false, "777", "Response is null from DnP", ref e2eData, true);
                        Logger.Write(orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.EESpringMessageTrace);
                    }
                }
            }
            catch (Exception ex)
            {
                e2eData.businessError(GotResponseFromDnPWithBusinessError, ex.Message);

                if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                    e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                else
                    e2eData.endOutboundCall(e2eData.toString());

                notification.sendNotification(false, false, "777", ex.Message, ref e2eData, true);
                Logger.Write(orderKey + "," + Errored + "," + FailedNotificationSent + "," + ex.Message, Logger.TypeEnum.EESpringExceptionTrace);
                Logger.Write(orderKey + "," + Failed + "," + FailureResponseFromDnP, Logger.TypeEnum.EESpringMessageTrace);
                Logger.Write(orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.EESpringMessageTrace);
            }
        }

        public void CreateSpringServiceforAHTProfile(EESpringParameters dnpParameters, string orderKey, MSEOOrderNotification notification, ref E2ETransaction e2eData)
        {
            manageClientProfileV1Response1 profileResponse1 = null;
            manageClientProfileV1Request1 profileRequest = new manageClientProfileV1Request1();
            profileRequest.manageClientProfileV1Request = new ManageClientProfileV1Request();

            BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock headerblock = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
            headerblock.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
            headerblock.serviceAddressing.from = @"http://www.profile.com?SAASMSEO";
            headerblock.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();
            headerblock.serviceState.stateCode = "OK";
            try
            {
                ManageClientProfileV1Req manageClientProfileV1Req1 = new ManageClientProfileV1Req();
                manageClientProfileV1Req1.clientProfileV1 = new ClientProfileV1();

                manageClientProfileV1Req1.clientProfileV1.client = new Client();
                manageClientProfileV1Req1.clientProfileV1.client.action = ACTION_UPDATE;

                List<ClientServiceRole> serviceRoleList = new List<ClientServiceRole>();
                ClientServiceRole clientServcieRole = null;
                List<ClientIdentity> clientIdentityList = new List<ClientIdentity>();

                ClientIdentity clientIdentity = null;

                if (!string.IsNullOrEmpty(dnpParameters.Billingaccountnumber))
                {
                    clientIdentity = new ClientIdentity();
                    clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientIdentity.managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;
                    clientIdentity.value = dnpParameters.Billingaccountnumber;
                    clientIdentity.action = ACTION_SEARCH;
                    clientIdentity.clientIdentityStatus = new ClientIdentityStatus();
                    clientIdentity.clientIdentityStatus.value = ACTIVE;

                    clientIdentityList.Add(clientIdentity);
                }

                ServiceIdentity[] serviceIdentity = new ServiceIdentity[1];
                serviceIdentity[0] = new ServiceIdentity();
                serviceIdentity[0].domain = BACID_IDENTIFER_NAMEPACE;
                serviceIdentity[0].value = dnpParameters.Billingaccountnumber;
                serviceIdentity[0].action = ACTION_SEARCH;

                ClientServiceInstanceV1[] clientServiceInstanceV1 = new ClientServiceInstanceV1[1];
                clientServiceInstanceV1[0] = new ClientServiceInstanceV1();
                clientServiceInstanceV1[0].clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                clientServiceInstanceV1[0].clientServiceInstanceIdentifier.value = SPRING_GSM_SERVICECODE_NAMEPACE;
                clientServiceInstanceV1[0].clientServiceInstanceStatus = new ClientServiceInstanceStatus();
                clientServiceInstanceV1[0].serviceIdentity = serviceIdentity;
                clientServiceInstanceV1[0].action = ACTION_UPDATE;
                clientServiceInstanceV1[0].clientServiceInstanceStatus = new ClientServiceInstanceStatus();
                clientServiceInstanceV1[0].clientServiceInstanceStatus.value = ACTIVE;

                clientServcieRole = new ClientServiceRole();
                clientServcieRole.id = ADMIN_ROLE;
                clientServcieRole.clientServiceRoleStatus = new ClientServiceRoleStatus();
                clientServcieRole.clientServiceRoleStatus.value = ACTIVE;
                clientServcieRole.action = ACTION_INSERT;

                clientIdentity = new ClientIdentity();
                clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                clientIdentity.managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;
                clientIdentity.value = dnpParameters.Billingaccountnumber;
                clientIdentity.action = ACTION_INSERT;
                clientIdentity.clientIdentityStatus = new ClientIdentityStatus();
                clientIdentity.clientIdentityStatus.value = ACTIVE;

                clientServcieRole.clientIdentity = new ClientIdentity[1];
                clientServcieRole.clientIdentity[0] = clientIdentity;
                serviceRoleList.Add(clientServcieRole);

                //calling this method to get the service roles
                clientServiceInstanceV1[0].clientServiceRole = serviceRoleList.ToArray();

                manageClientProfileV1Req1.clientProfileV1.client.clientIdentity = clientIdentityList.ToArray();
                manageClientProfileV1Req1.clientProfileV1.clientServiceInstanceV1 = clientServiceInstanceV1;

                profileRequest.manageClientProfileV1Request.manageClientProfileV1Req = manageClientProfileV1Req1;
                profileRequest.manageClientProfileV1Request.standardHeader = headerblock;

                e2eData.logMessage(StartedDnPCall, "");
                e2eData.startOutboundCall(ConfigurationManager.AppSettings["destinationDnP"].ToString(), E2ETransaction.vREQ, SPRING_GSM_SERVICECODE_NAMEPACE);

                profileRequest.manageClientProfileV1Request.standardHeader.e2e = new BT.SaaS.IspssAdapter.Dnp.E2E();
                profileRequest.manageClientProfileV1Request.standardHeader.e2e.E2EDATA = e2eData.toString();

                Logger.Write(orderKey + "," + MakingDnPCall + " to insert admin role to bac," + SendingRequestToDNP, Logger.TypeEnum.EESpringMessageTrace);
               // MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_IN_LOG, orderKey, profileRequest.SerializeObject(), dnpParameters.ProductCode);

                profileResponse1 = SpringDnpWrapper.manageClientProfileV1ForSpring(profileRequest, orderKey);

                Logger.Write(orderKey + "," + GotResponsefromDNP + "for insert admin role to bac," + ReceivedResponsefromDNP, Logger.TypeEnum.EESpringMessageTrace);
                //MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_OUT_LOG, orderKey, profileResponse1.SerializeObject(), dnpParameters.ProductCode);

                if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null
                    && profileResponse1.manageClientProfileV1Response.standardHeader != null
                    && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState != null
                    && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState.stateCode != null
                    && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState.stateCode == "0")
                {
                    e2eData.logMessage(GotResponseFromDnP, "");
                    if (profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && !string.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                    {
                        e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                    }
                    else
                        e2eData.endOutboundCall(e2eData.toString());                    
                }
                else
                {
                    if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null
                        && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res != null
                        && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages != null
                        && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0] != null
                        && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description != null)
                    {
                        string errorMessage = profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description;
                        e2eData.businessError(GotResponseFromDnPWithBusinessError, errorMessage);
                        if (profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && !string.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());
                        
                        Logger.Write(orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.EESpringMessageTrace);
                    }
                    else
                    {
                        Logger.Write(orderKey + "," + Failed + "," + NullResponseFromDnP, Logger.TypeEnum.EESpringMessageTrace);

                        e2eData.businessError(GotResponseFromDnPWithBusinessError, "Response is null from DnP");
                        if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());

                        //notification.sendNotification(false, false, "777", "Response is null from DnP", ref e2eData, true);
                        //Logger.Write(orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.EESpringMessageTrace);
                    }
                }
            }
            catch (Exception ex)
            {
                e2eData.businessError(GotResponseFromDnPWithBusinessError, ex.Message);

                if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                    e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                else
                    e2eData.endOutboundCall(e2eData.toString());

                //notification.sendNotification(false, false, "777", ex.Message, ref e2eData, true);
                Logger.Write(orderKey + "," + Errored + "," + FailedNotificationSent + "," + ex.Message, Logger.TypeEnum.EESpringExceptionTrace);
                Logger.Write(orderKey + "," + Failed + "," + FailureResponseFromDnP, Logger.TypeEnum.EESpringMessageTrace);
                Logger.Write(orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.EESpringMessageTrace);
            }
        }
       
        public linkClientProfilesResponse1 linkMsisdntoClientProfile(EESpringParameters dnpParameters, ref E2ETransaction e2eData)
        {
            linkClientProfilesResponse1 response = null;
            linkClientProfilesRequest1 request = new linkClientProfilesRequest1();
            LinkClientProfilesRequest req = new LinkClientProfilesRequest();
            try
            {
                req.primaryClientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                req.primaryClientIdentity.action = ACTION_SEARCH;
                req.primaryClientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                req.primaryClientIdentity.managedIdentifierDomain.value = dnpParameters.IsEECustomer ? EEONLINEID : BTCOM;
                req.primaryClientIdentity.value = dnpParameters.BtoneId;

                req.linkClientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                req.linkClientIdentity.action = ACTION_LINK;
                req.linkClientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                req.linkClientIdentity.managedIdentifierDomain.value = EE_MSISDN_NAMESPACE;
                req.linkClientIdentity.value = dnpParameters.MSISDN;

                req.linkClientIdentity.clientCredential = new BT.SaaS.IspssAdapter.Dnp.ClientCredential[1];
                req.linkClientIdentity.clientCredential[0] = new BT.SaaS.IspssAdapter.Dnp.ClientCredential();
                req.linkClientIdentity.clientCredential[0].credentialValue = "AHTMergeIgnorePassword";
                req.linkClientIdentity.clientCredential[0].clientCredentialStatus = new ClientCredentialStatus();
                req.linkClientIdentity.clientCredential[0].clientCredentialStatus.value = ACTIVE;

                req.action = "MERGE";

                BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock headerblock = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
                headerblock.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
                headerblock.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
                headerblock.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();
                headerblock.serviceState.stateCode = "OK";
                try
                {
                    e2eData.logMessage(StartedDnPCall, "Started... Dnp linkClientProfiles call");
                    e2eData.startOutboundCall(ConfigurationManager.AppSettings["destinationDnP"].ToString(), E2ETransaction.vREQ, ACCOUNT_HOLDER_STATUS);
                    req.standardHeader = headerblock;
                    req.standardHeader.e2e = new BT.SaaS.IspssAdapter.Dnp.E2E();
                    req.standardHeader.e2e.E2EDATA = e2eData.toString();
                    request.linkClientProfilesRequest = req;                    

                    byte[] credBuffer = new UTF8Encoding().GetBytes(ConfigurationManager.AppSettings["DnPUser"].ToString() + ":" + ConfigurationManager.AppSettings["DnPPassword"].ToString());
                    HttpRequestMessageProperty httpRequest = new HttpRequestMessageProperty();
                    httpRequest.Headers.Add(HTTP_HEADER_AUTHORIZATION, AUTHORIZATION_TYPE_BASIC + Convert.ToBase64String(credBuffer));
                    using (ChannelFactory<ClientProfileSyncPortType> factory = new ChannelFactory<ClientProfileSyncPortType>(@"ClientProfile_DnP"))
                    {
                        ClientProfileSyncPortType svc = factory.CreateChannel();
                        using (OperationContextScope scope = new OperationContextScope((IContextChannel)svc))
                        {
                            OperationContext.Current.OutgoingMessageProperties.Add(HttpRequestMessageProperty.Name, httpRequest);
                            response = svc.linkClientProfiles(request);
                        }
                    }
                    e2eData.logMessage(GotResponseFromDnP, "Ending... Dnp linkClientProfiles call");                    
                }
                catch (Exception excp)
                {
                    e2eData.downstreamError("ExceptionOccurred", excp.Message);                   
                    throw new DnpException(excp.Message);
                }
            }
            finally
            {
                if (response != null && response.linkClientProfilesResponse != null && response.linkClientProfilesResponse.standardHeader != null && response.linkClientProfilesResponse.standardHeader.e2e != null && !String.IsNullOrEmpty(response.linkClientProfilesResponse.standardHeader.e2e.E2EDATA))
                    e2eData.endOutboundCall(response.linkClientProfilesResponse.standardHeader.e2e.E2EDATA);
                else
                    e2eData.endOutboundCall(e2eData.toString());
            }
            return response;
        }

        public void ModifyEEServiceInstance(string identity, EESpringParameters dnpParameters, string orderKey, MSEOOrderNotification notification, ref E2ETransaction e2eData)
        {
            if (dnpParameters.Reason != null && dnpParameters.Reason.Equals("portin", StringComparison.OrdinalIgnoreCase))
            {
                ModifyEEClientIdentity(EE_MSISDN_NAMESPACE, dnpParameters, orderKey, notification, ref e2eData);
            }            
        }

        public void ModifyEEClientIdentity(string domain, EESpringParameters dnpParameters, string orderKey, MSEOOrderNotification notification, ref E2ETransaction e2eData)
        {
            string oldValue, newValue;
            string SPRING_DELIMITER_VALUE = ConfigurationManager.AppSettings["SpringPendingOrderDelimiter"];
            manageClientIdentityResponse1 resp = new manageClientIdentityResponse1();
            manageClientIdentityRequest1 req = new manageClientIdentityRequest1();
            ManageClientIdentityRequest manageclientidentityreq = new ManageClientIdentityRequest();
            manageclientidentityreq.manageClientIdentityReq = new ManageClientIdentityReq();
            try
            {
                if (String.Equals(domain, EE_MSISDN_NAMESPACE, StringComparison.OrdinalIgnoreCase))
                {
                    oldValue = dnpParameters.oldMSISDN;
                    newValue = dnpParameters.MSISDN;
                }
                else
                {
                    oldValue = dnpParameters.RVSID;
                    newValue = dnpParameters.RVSID;
                }

                BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock headerblock = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
                headerblock.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
                headerblock.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
                headerblock.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();
                headerblock.serviceState.stateCode = "OK";

                manageclientidentityreq.manageClientIdentityReq.clientSearchCriteria = new ClientSearchCriteria();
                manageclientidentityreq.manageClientIdentityReq.clientSearchCriteria.identifierDomainCode = domain;
                manageclientidentityreq.manageClientIdentityReq.clientSearchCriteria.identifierValue = oldValue;

                manageclientidentityreq.manageClientIdentityReq.clientIdentity = new ClientIdentity();
                manageclientidentityreq.manageClientIdentityReq.clientIdentity.action = "UPDATE";
                manageclientidentityreq.manageClientIdentityReq.clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                manageclientidentityreq.manageClientIdentityReq.clientIdentity.managedIdentifierDomain.value = domain;
                manageclientidentityreq.manageClientIdentityReq.clientIdentity.value = newValue;

                if (String.Equals(domain, RVSID, StringComparison.OrdinalIgnoreCase))
                {
                    headerblock.e2e = new BT.SaaS.IspssAdapter.Dnp.E2E();
                    headerblock.e2e.E2EDATA = e2eData.toString();

                    List<ClientIdentityValidation> clientIdentityValidationList = new List<ClientIdentityValidation>();
                    ClientIdentityValidation clientIdentityValidation = null;
                    //SIM_REPLACEMENT :: update only NUM_SIM_DISPATCHED
                    if (dnpParameters.replacementReason == true)
                    {
                        clientIdentityValidation = new ClientIdentityValidation();
                        clientIdentityValidation.name = "NUM_SIM_DISPATCHED";
                        clientIdentityValidation.value = Convert.ToString(Convert.ToUInt32(dnpParameters.NumSimDispatched) + 1);
                        clientIdentityValidation.action = ACTION_UPDATE;
                        clientIdentityValidationList.Add(clientIdentityValidation);
                    }
                    else
                    {
                        //SIM_CREDITLIMIT :: update credit limit attributes 
                        if (dnpParameters.Reason != null && dnpParameters.Reason.Equals("sim_creditlimit", StringComparison.OrdinalIgnoreCase))
                        {
                            clientIdentityValidation = new ClientIdentityValidation();
                            clientIdentityValidation.name = "INTERNATIONAL";
                            if (!string.IsNullOrEmpty(dnpParameters.International))
                                clientIdentityValidation.value = dnpParameters.International;
                            else
                                clientIdentityValidation.value = " ";
                            clientIdentityValidation.action = ACTION_UPDATE;
                            clientIdentityValidationList.Add(clientIdentityValidation);

                            clientIdentityValidation = new ClientIdentityValidation();
                            clientIdentityValidation.name = "PREMIUM_RATE_SERVICE";
                            if (!string.IsNullOrEmpty(dnpParameters.PremiumRateService))
                                clientIdentityValidation.value = dnpParameters.PremiumRateService;
                            else
                                clientIdentityValidation.value = " ";
                            clientIdentityValidation.action = ACTION_UPDATE;
                            clientIdentityValidationList.Add(clientIdentityValidation);

                            clientIdentityValidation = new ClientIdentityValidation();
                            clientIdentityValidation.name = "ROAMING";
                            if (!string.IsNullOrEmpty(dnpParameters.Roaming))
                                clientIdentityValidation.value = dnpParameters.Roaming;
                            else
                                clientIdentityValidation.value = " ";
                            clientIdentityValidation.action = ACTION_UPDATE;
                            clientIdentityValidationList.Add(clientIdentityValidation);

                            clientIdentityValidation = new ClientIdentityValidation();
                            clientIdentityValidation.name = "CREDIT_LIMIT";
                            if (!string.IsNullOrEmpty(dnpParameters.CreditLimit))
                                clientIdentityValidation.value = dnpParameters.CreditLimit;
                            else
                                clientIdentityValidation.value = " ";
                            clientIdentityValidation.action = ACTION_UPDATE;
                            clientIdentityValidationList.Add(clientIdentityValidation);

                            clientIdentityValidation = new ClientIdentityValidation();
                            clientIdentityValidation.name = "DATA_ROAMING_LIMIT";
                            if (!string.IsNullOrEmpty(dnpParameters.DataRoamingLimit))
                                clientIdentityValidation.value = dnpParameters.DataRoamingLimit;
                            else
                                clientIdentityValidation.value = " ";
                            clientIdentityValidation.action = ACTION_UPDATE;
                            clientIdentityValidationList.Add(clientIdentityValidation);
                        }
                        //update PENDING_ORDER attribute
                        //PreGSM
                        if (!string.IsNullOrEmpty(dnpParameters.NewPendingOrderValue))
                        {
                            clientIdentityValidation = new ClientIdentityValidation();
                            clientIdentityValidation.name = "PENDING_ORDER";

                            if (dnpParameters.DnpPendingOrderValue == " ")
                            {
                                clientIdentityValidation.value = dnpParameters.NewPendingOrderValue;
                            }
                            else
                            {
                                List<string> pendingOrderAttrs = dnpParameters.DnpPendingOrderValue.Split(SPRING_DELIMITER_VALUE.ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();
                                //PreGSM //Appending Pending_Order value coming in request to existence Pending_order value in DNP.
                                //If already DNP Pending_order value contains value coming in request just update the same dnp value again ..
                                //Else append the value coming in request with existence DNP value

                                if (pendingOrderAttrs.Contains(dnpParameters.NewPendingOrderValue))
                                    clientIdentityValidation.value = dnpParameters.DnpPendingOrderValue;
                                else
                                    clientIdentityValidation.value = dnpParameters.DnpPendingOrderValue + SPRING_DELIMITER_VALUE + dnpParameters.NewPendingOrderValue;
                            }
                            clientIdentityValidation.action = ACTION_UPDATE;
                            clientIdentityValidationList.Add(clientIdentityValidation);

                        }
                        //PostGSM //Removing Pending_Order Attribute coming in request from DNP.
                        else if (!string.IsNullOrEmpty(dnpParameters.OldPendingOrderValue))
                        {
                            List<string> pendingOrderAttrs = dnpParameters.DnpPendingOrderValue.Split(SPRING_DELIMITER_VALUE.ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();
                            if (pendingOrderAttrs != null && pendingOrderAttrs.Contains(dnpParameters.OldPendingOrderValue))
                            {
                                clientIdentityValidation = new ClientIdentityValidation();
                                clientIdentityValidation.name = "PENDING_ORDER";
                                string pending_order = string.Empty;
                                pendingOrderAttrs.Remove(dnpParameters.OldPendingOrderValue);
                                pending_order = string.Join(SPRING_DELIMITER_VALUE, pendingOrderAttrs.ToArray());
                                clientIdentityValidation.value = string.IsNullOrEmpty(pending_order) ? " " : pending_order;
                                clientIdentityValidation.action = ACTION_UPDATE;
                                clientIdentityValidationList.Add(clientIdentityValidation);
                            }

                        }
                        //update PENDINGOPTIN only when PENDING_ORDER is null or empty 
                        if (clientIdentityValidationList.Exists(ci => ci.name.Equals("PENDING_ORDER", StringComparison.OrdinalIgnoreCase) && ci.value != null) && string.IsNullOrEmpty(clientIdentityValidationList.Where(ci => ci.name.Equals("PENDING_ORDER", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value.Trim()))
                        {
                            clientIdentityValidation = new ClientIdentityValidation();
                            clientIdentityValidation.action = ACTION_UPDATE;
                            clientIdentityValidation.name = "PENDINGOPTIN";
                            clientIdentityValidation.value = dnpParameters.PendingOptIn;
                            clientIdentityValidationList.Add(clientIdentityValidation);
                        }
                    }

                    manageclientidentityreq.manageClientIdentityReq.clientIdentity.clientIdentityValidation = clientIdentityValidationList.ToArray();
                }

                e2eData.logMessage(StartedDnPCall, "");
                e2eData.startOutboundCall(ConfigurationManager.AppSettings["destinationDnP"].ToString(), E2ETransaction.vREQ, SPRING_GSM_SERVICECODE_NAMEPACE);

                headerblock.e2e = new BT.SaaS.IspssAdapter.Dnp.E2E();
                headerblock.e2e.E2EDATA = e2eData.toString();

                manageclientidentityreq.standardHeader = headerblock;
                req.manageClientIdentityRequest = manageclientidentityreq;

                Logger.Write(orderKey + "," + MakingDnPCall + "," + SendingRequestToDNP, Logger.TypeEnum.EESpringMessageTrace);
                MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_IN_LOG, orderKey, req.SerializeObject(), dnpParameters.ProductCode);

                resp = SpringDnpWrapper.manageClientIdentity(req, orderKey);

                Logger.Write(orderKey + "," + GotResponsefromDNP + "," + ReceivedResponsefromDNP, Logger.TypeEnum.EESpringMessageTrace);
                MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_OUT_LOG, orderKey, resp.SerializeObject(), dnpParameters.ProductCode);

                if (resp != null
                   && resp.manageClientIdentityResponse != null
                   && resp.manageClientIdentityResponse.standardHeader != null
                   && resp.manageClientIdentityResponse.standardHeader.serviceState != null
                   && resp.manageClientIdentityResponse.standardHeader.serviceState.stateCode != null
                   && resp.manageClientIdentityResponse.standardHeader.serviceState.stateCode == "0")
                {
                    e2eData.logMessage(GotResponseFromDnP, "");
                    if (resp.manageClientIdentityResponse.standardHeader.e2e != null && !string.IsNullOrEmpty(resp.manageClientIdentityResponse.standardHeader.e2e.E2EDATA))
                    {
                        e2eData.endOutboundCall(resp.manageClientIdentityResponse.standardHeader.e2e.E2EDATA);
                    }
                    else
                        e2eData.endOutboundCall(e2eData.toString());
                    
                    notification.sendNotification(true, false, "004", string.Empty, ref e2eData,true);
                    Logger.Write(orderKey + "," + Completed + "," + CompletedNotificationSent, Logger.TypeEnum.EESpringMessageTrace);
                

                }
                else
                {
                    if (resp != null && resp.manageClientIdentityResponse != null
                        && resp.manageClientIdentityResponse.manageClientIdentityRes != null
                        && resp.manageClientIdentityResponse.manageClientIdentityRes.messages != null
                        && resp.manageClientIdentityResponse.manageClientIdentityRes.messages[0].description != null)
                    {
                        string errorMessage = resp.manageClientIdentityResponse.manageClientIdentityRes.messages[0].description;
                        e2eData.businessError(GotResponseFromDnPWithBusinessError, errorMessage);
                        if (resp.manageClientIdentityResponse.standardHeader.e2e != null && !string.IsNullOrEmpty(resp.manageClientIdentityResponse.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(resp.manageClientIdentityResponse.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());

                        if (errorMessage.Contains("Administrator"))
                        {
                            Logger.Write(orderKey + "," + Errored + "," + DnPAdminstratorFailedResponse + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.EESpringExceptionTrace);
                            Logger.Write(orderKey + "," + Failed + "," + FailureResponseFromDnP + "," + errorMessage, Logger.TypeEnum.EESpringMessageTrace);
                        }
                        else
                        {
                            Logger.Write(orderKey + "," + Failed + "," + FailureResponseFromDnP + "," + errorMessage, Logger.TypeEnum.EESpringMessageTrace);
                        }
                        notification.sendNotification(false, false, "777", errorMessage, ref e2eData,true);
                        Logger.Write(orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.EESpringMessageTrace);
                    }
                    else
                    {
                        Logger.Write(orderKey + "," + Failed + "," + NullResponseFromDnP, Logger.TypeEnum.EESpringMessageTrace);
                        e2eData.businessError(GotResponseFromDnPWithBusinessError, "Response is null from DnP");
                        if (resp != null && resp.manageClientIdentityResponse != null
                            && resp.manageClientIdentityResponse.standardHeader != null
                            && resp.manageClientIdentityResponse.standardHeader.e2e != null
                            && resp.manageClientIdentityResponse.standardHeader.e2e.E2EDATA != null &&
                            !String.IsNullOrEmpty(resp.manageClientIdentityResponse.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(resp.manageClientIdentityResponse.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());

                        notification.sendNotification(false, false, "777", "Response is null from DnP", ref e2eData,true);
                        Logger.Write(orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.EESpringMessageTrace);
                    }
                }

            }
            catch (Exception ex)
            {
                e2eData.businessError(GotResponseFromDnPWithBusinessError, ex.Message);

                if (resp != null && resp.manageClientIdentityResponse != null
                    && resp.manageClientIdentityResponse.standardHeader != null &&
                    resp.manageClientIdentityResponse.standardHeader.e2e != null &&
                    resp.manageClientIdentityResponse.standardHeader.e2e.E2EDATA != null &&
                    !String.IsNullOrEmpty(resp.manageClientIdentityResponse.standardHeader.e2e.E2EDATA))

                    e2eData.endOutboundCall(resp.manageClientIdentityResponse.standardHeader.e2e.E2EDATA);
                else
                    e2eData.endOutboundCall(e2eData.toString());

                notification.sendNotification(false, false, "777", ex.Message, ref e2eData,true);
                Logger.Write(orderKey + "," + Errored + "," + FailedNotificationSent + "," + ex.Message, Logger.TypeEnum.EESpringExceptionTrace);
                Logger.Write(orderKey + "," + Failed + "," + FailureResponseFromDnP + ex.Message, Logger.TypeEnum.EESpringMessageTrace);
                Logger.Write(orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.EESpringMessageTrace);
            }
        }
    }
    public class EESpringParameters
    {
        private string BACorganisationname = string.Empty;

        public string BACOrganisationName
        {
            get { return BACorganisationname; }
            set { BACorganisationname = value; }
        }
        private string BACtype = string.Empty;

        public string BACType
        {
            get { return BACtype; }
            set { BACtype = value; }
        }

        private string btoneId = string.Empty;

        public string BtoneId
        {
            get { return btoneId; }
            set { btoneId = value; }
        }

        private string creditLimit = " ";

        public string CreditLimit
        {
            get { return creditLimit; }
            set { creditLimit = value; }
        }

        private string customerId = string.Empty;

        public string CustomerId
        {
            get { return customerId; }
            set { customerId = value; }
        }

        private string dataRoamingLimit = " ";

        public string DataRoamingLimit
        {
            get { return dataRoamingLimit; }
            set { dataRoamingLimit = value; }
        }

        private string EUOPTIN1 = " ";

        public string EUOPTIN
        {
            get { return EUOPTIN1; }
            set { EUOPTIN1 = value; }
        }

        private string IMSI1 = string.Empty;

        public string IMSI
        {
            get { return IMSI1; }
            set { IMSI1 = value; }
        }

        private string international = " ";

        public string International
        {
            get { return international; }
            set { international = value; }
        }

        private bool isAHTDone = false;

        public bool IsAHTDone
        {
            get { return isAHTDone; }
            set { isAHTDone = value; }
        }

        private bool isExixstingAccount = false;

        public bool IsExixstingAccount
        {
            get { return isExixstingAccount; }
            set { isExixstingAccount = value; }
        }

        private bool isStandaloneServiceExists = false;

        public bool IsStandaloneServiceExists
        {
            get { return isStandaloneServiceExists; }
            set { isStandaloneServiceExists = value; }
        }

        private string MSISDN1 = string.Empty;

        public string MSISDN
        {
            get { return MSISDN1; }
            set { MSISDN1 = value; }
        }

        private string numSimDispatched = string.Empty;

        public string NumSimDispatched
        {
            get { return numSimDispatched; }
            set { numSimDispatched = value; }
        }

        private string oldRVSID = string.Empty;

        public string OldRVSID
        {
            get { return oldRVSID; }
            set { oldRVSID = value; }
        }

        private string pcProfile = " ";

        public string PCProfile
        {
            get { return pcProfile; }
            set { pcProfile = value; }
        }

        private string newPendingOrderValue = " ";

        public string NewPendingOrderValue
        {
            get { return newPendingOrderValue; }
            set { newPendingOrderValue = value; }
        }
        private string dnpPendingOrderValue = " ";

        public string DnpPendingOrderValue
        {
            get { return dnpPendingOrderValue; }
            set { dnpPendingOrderValue = value; }
        }

        private string oldPendingOrderValue = string.Empty;

        public string OldPendingOrderValue
        {
            get { return oldPendingOrderValue; }
            set { oldPendingOrderValue = value; }
        }

        private string pendingOptIn = " ";

        public string PendingOptIn
        {
            get { return pendingOptIn; }
            set { pendingOptIn = value; }
        }

        private string premiumRateService = " ";

        public string PremiumRateService
        {
            get { return premiumRateService; }
            set { premiumRateService = value; }
        }

        private string productCode = "S0316296";

        public string ProductCode
        {
            get { return productCode; }
        }

        private string productFamily = "spring";

        public string ProductFamily
        {
            get { return productFamily; }
        }

        private string pukCode = string.Empty;

        public string PukCode
        {
            get { return pukCode; }
            set { pukCode = value; }
        }

        private string roaming = " ";

        public string Roaming
        {
            get { return roaming; }
            set { roaming = value; }
        }

        private string RVSID1 = string.Empty;

        public string RVSID
        {
            get { return RVSID1; }
            set { RVSID1 = value; }
        }

        private bool serviceInstanceExists = false;

        public bool ServiceInstanceExists
        {
            get { return serviceInstanceExists; }
            set { serviceInstanceExists = value; }
        }

        private string simCardType = string.Empty;

        public string SimCardType
        {
            get { return simCardType; }
            set { simCardType = value; }
        }

        private string SSN1 = string.Empty;

        public string SSN
        {
            get { return SSN1; }
            set { SSN1 = value; }
        }

        private string oldIMSIValue = string.Empty;

        public string oldIMSI
        {
            get { return oldIMSIValue; }
            set { oldIMSIValue = value; }
        }


        private string oldMSISDNValue = string.Empty;

        public string oldMSISDN
        {
            get { return oldMSISDNValue; }
            set { oldMSISDNValue = value; }
        }

        private string reason = string.Empty;

        public string Reason
        {
            get { return reason; }
            set { reason = value; }
        }

        private bool replacementReasonValue = false;

        public bool replacementReason
        {
            get { return replacementReasonValue; }
            set { replacementReasonValue = value; }
        }
        private string billingaccountnumber = string.Empty;

        public string Billingaccountnumber
        {
            get { return billingaccountnumber; }
            set { billingaccountnumber = value; }
        }
        private List<string> inviteroles;
        public List<string> Inviteroles
        {
            get { return inviteroles; }
            set { inviteroles = value; }
        }

        private bool isMsisdnExists = false;
        public bool IsMsisdnExists
        {
            get { return isMsisdnExists; }
            set { isMsisdnExists = value; }
        }

        private bool isMsisdnhasService = false;
        public bool IsMsisdnhasService
        {
            get { return isMsisdnhasService; }
            set { isMsisdnhasService = value; }
        }

        private bool isEECustomer = false;
        public bool IsEECustomer
        {
            get { return isEECustomer; }
            set { isEECustomer = value; }
        }

        private string msisdnBAC = string.Empty;

        public string MsisdnBAC
        {
            get { return msisdnBAC; }
            set { msisdnBAC = value; }
        }

        private string eeConkid = string.Empty;

        public string EEConkid
        {
            get { return eeConkid; }
            set { eeConkid = value; }
        }
    }
}
