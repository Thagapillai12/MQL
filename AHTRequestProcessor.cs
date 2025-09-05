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
using System.ServiceModel.Channels;
using System.Text;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.Xml.Linq;
using System.ServiceModel;
using BT.SaaS.Core.Shared.ErrorManagement;
using BT.SaaS.Core.Shared.Entities;
using BT.Helpers;

namespace BT.SaaS.MSEOAdapter
{
    public class AHTRequestProcessor
    {
        private const string AUTHORIZATION_TYPE_BASIC = "Basic ";
        private const string DNP_USER_KEY = "DnPUser";
        private const string DNP_PWD_KEY = "DnPPassword";
        private const string HTTP_HEADER_AUTHORIZATION = "Authorization";
        private const string ACTIVE = "ACTIVE";
        private const string INACTIVE = "INACTIVE";
        private const string CEASED = "CEASED";
        private const string SUSPENDED = "SUSPENDED";
        private const string ACTION_SEARCH = "SEARCH";
        private const string ACTION_UPDATE = "UPDATE";
        private const string ACTION_DELETE = "DELETE";
        private const string ACTION_INSERT = "INSERT";
        private const string ACTION_LINK = "LINK";
        private const string ACTION_MERGE = "MERGE";
        private const string ACTION_FORCE_INSERT = "FORCE_INS";
        private const string DEFAULT_ROLE = "DEFAULT";
        private const string ADMIN_ROLE = "ADMIN";
        private const string BACID_IDENTIFER_NAMEPACE = "VAS_BILLINGACCOUNT_ID";
        private const string BTIEMAILID = "BTIEMAILID";
        private const string BTIEMAIL_IDENTIFER_NAMEPACE = "BTIEMAIL:DEFAULT";
        private const string BTIEMAIL_NAMEPACE = "BTIEMAILID";
        private const string RVSID = "RVSID";
        private const string IMSI = "IMSI";
        private const string MOBILE_MSISDN_NAMESPACE = "MOBILE_MSISDN";
        private const string EE_MOBILE_MSISDN_NAMESPACE = "EE_MOBILE_MSISDN";
        private const string SPRING_GSM = "SPRING_GSM";
        private const string INVITE_ROLE = "INVITE";
        private const string BTCOM = "BTCOM";
        private const string EEONLINEID = "EEONLINEID";
        private const string ExecuteSuccessResponse = "ExecuteSuccessResponse";
        private const string VOICEINIFNITY_SERVICECODE_NAMEPACE = "IP_VOICE_SERVICE";
        private const string VoiceInfnity_ClientIdentity = "IP_VOICE_SERVICE_DN";
        private const string ExecuteFailedResponse = "ExecuteFailedResponse";
        private const string ExceptionOccurred = "ExceptionOccurred";
        private const string MSISDN_NAMESPACE = "MOBILE_MSISDN";
        private const string ACCOUNTTRUSTMETHOD = "ACCOUNTTRUSTMETHOD";
        private const string OTA_DEVICE_ID = "OTA_DEVICE_ID";
        const string StartedDnPCall = "StartedDnPCall";
        const string GotResponseFromDnPWithBusinessError = "GotResponseFromDnPWithBusinessError";
        const string GotResponseFromDnP = "GotResponseFromDnP";
        const string ACCOUNT_HOLDER_STATUS = "Account Holder Status";
        const string MakingDnPcall = "Making DnP call";
        private string e2eDataPassed = string.Empty;

        System.DateTime ActivityStartTime = System.DateTime.Now; Guid guid = new Guid(); string bptmTxnId = string.Empty;

        public void AHTRequestMapper(OrderRequest requestOrderRequest, string BillAccountNumber, string BtoneId, string AhtValue, ref E2ETransaction e2eData, ref MSEOOrderNotification notification, bool isEECustomer, System.DateTime ActivityStartedTime)
        {
            string msisdnValue = string.Empty;
            bool serviceExists = false;
            string status = string.Empty;
            string delegateRoleDetails = string.Empty;

            GetServiceInstanceV1Response gsiResponse = null;
            ClientServiceInstanceV1[] serviceInstances = null;
            ClientServiceInstanceV1[] gsiResponseDelegate = null;
            getClientProfileV1Request1 profileRequest = null;
            getClientProfileV1Response1 gcp = new getClientProfileV1Response1();
            //List<ClientServiceInstance> clientServiceInstanceList = new List<ClientServiceInstance>();
            try
            {
                this.bptmTxnId = BPTMHelper.GetTxnIdFromE2eData(e2eData.toString());
                this.ActivityStartTime = ActivityStartedTime;
                guid = new Guid(requestOrderRequest.Order.OrderIdentifier.Value);

                e2eData.logMessage(StartedDnPCall, "Started... Dnp GetClientProfileV1 call");
                e2eData.startOutboundCall(ConfigurationManager.AppSettings["destinationDnP"].ToString(), E2ETransaction.vREQ, ACCOUNT_HOLDER_STATUS);
                gcp = GetClientProfileV1(BillAccountNumber, BACID_IDENTIFER_NAMEPACE, e2eData.toString(), requestOrderRequest, ref profileRequest);

                LogHelper.LogActivityDetails(bptmTxnId, guid, MakingDnPcall + " for GCP call", System.DateTime.Now - ActivityStartTime, "AHTLogTrace", guid, profileRequest.SerializeObject());
                LogHelper.LogActivityDetails(bptmTxnId, guid, GotResponseFromDnP + " for GCP call", System.DateTime.Now - ActivityStartTime, "AHTLogTrace", guid, gcp.SerializeObject());

                if (gcp != null && gcp.getClientProfileV1Response != null && gcp.getClientProfileV1Response.standardHeader != null && gcp.getClientProfileV1Response.standardHeader.e2e != null && gcp.getClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(gcp.getClientProfileV1Response.standardHeader.e2e.E2EDATA))
                {
                    e2eData.logMessage(GotResponseFromDnP, "Ending... Dnp GetClientProfileV1 call");
                    e2eDataPassed = gcp.getClientProfileV1Response.standardHeader.e2e.E2EDATA;
                    e2eData.endOutboundCall(e2eDataPassed);
                }
                else
                {
                    e2eData.logMessage(GotResponseFromDnP, "Ending... Dnp GetClientProfileV1 call");
                    e2eData.endOutboundCall(e2eData.toString());
                }

                if (requestOrderRequest.Order.OrderItem[0].Action.Code.Equals("create", StringComparison.OrdinalIgnoreCase))
                {
                    if (gcp != null && gcp.getClientProfileV1Response != null && gcp.getClientProfileV1Response.getClientProfileV1Res != null && gcp.getClientProfileV1Response.getClientProfileV1Res.clientProfileV1 != null && gcp.getClientProfileV1Response.getClientProfileV1Res.clientProfileV1.client != null && gcp.getClientProfileV1Response.getClientProfileV1Res.clientProfileV1.client.clientIdentity != null)
                    {
                        if (gcp.getClientProfileV1Response.getClientProfileV1Res.clientProfileV1.clientServiceInstanceV1 != null)
                        {
                            serviceInstances = gcp.getClientProfileV1Response.getClientProfileV1Res.clientProfileV1.clientServiceInstanceV1.ToArray();
                        }
                        //bac exist and linked to SAME BTOneID
                        if (gcp.getClientProfileV1Response.getClientProfileV1Res.clientProfileV1.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(BTCOM) && ci.value.Equals(BtoneId, StringComparison.OrdinalIgnoreCase)))
                        {
                            updateBacToSameBTOneID(BillAccountNumber, AhtValue, serviceInstances, requestOrderRequest, ref e2eData, ref notification);
                        }
                        //bac exist but linked to ANOTHER or NO BTOneID
                        else
                        {
                            linkUnlinkBacFromBtOneID(BillAccountNumber, BtoneId, AhtValue, serviceInstances, gcp, requestOrderRequest, ref e2eData, ref notification, isEECustomer);
                        }
                    }
                    else
                    {
                        bool isWarrantyServiceExists = false;
                        string acttype = string.Empty;
                        e2eData.logMessage(StartedDnPCall, "Started... Dnp getClientServiceInstance call");
                        e2eData.startOutboundCall(ConfigurationManager.AppSettings["destinationDnP"].ToString(), E2ETransaction.vREQ, ACCOUNT_HOLDER_STATUS);
                        serviceExists = getClientServiceInstance(BillAccountNumber, BACID_IDENTIFER_NAMEPACE, string.Empty, ref gsiResponse, requestOrderRequest, e2eData.toString());
                        if (gsiResponse != null && gsiResponse.standardHeader != null && gsiResponse.standardHeader.e2e != null && gsiResponse.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(gsiResponse.standardHeader.e2e.E2EDATA))
                        {
                            e2eData.logMessage(GotResponseFromDnP, "Ending... Dnp getClientServiceInstance call");
                            e2eDataPassed = gsiResponse.standardHeader.e2e.E2EDATA;
                            e2eData.endOutboundCall(e2eDataPassed);
                        }
                        else
                        {
                            e2eData.logMessage(GotResponseFromDnP, "Ending... Dnp getClientServiceInstance call");
                            e2eData.endOutboundCall(e2eData.toString());
                        }

                        if (serviceExists)
                        {
                            //checking the warranty service
                            if (gsiResponse.getServiceInstanceV1Res.clientServiceInstance.ToList().Exists(csi => csi.clientServiceInstanceIdentifier.value.Equals("CONTRACTANDWARRANTY", StringComparison.OrdinalIgnoreCase)))
                            {
                                foreach (ClientServiceInstanceV1 serviceInstance in gsiResponse.getServiceInstanceV1Res.clientServiceInstance)
                                {
                                    if (serviceInstance.clientServiceInstanceIdentifier.value.Equals("CONTRACTANDWARRANTY", StringComparison.OrdinalIgnoreCase))
                                    {
                                        isWarrantyServiceExists = true;
                                        if (serviceInstance.clientServiceInstanceCharacteristic.ToList().Exists(chr => chr.name.Equals("ACT_TYPE", StringComparison.OrdinalIgnoreCase)))
                                        {
                                            acttype = serviceInstance.clientServiceInstanceCharacteristic.ToList().Where(chr => chr.name.Equals("ACT_TYPE", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                        }
                                    }
                                }
                            }
                            BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock headerblock = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
                            headerblock.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
                            headerblock.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
                            headerblock.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();
                            headerblock.serviceState.stateCode = "OK";

                            manageClientProfileV1Response1 response;
                            manageClientProfileV1Request1 request = new manageClientProfileV1Request1();
                            request.manageClientProfileV1Request = new ManageClientProfileV1Request();
                            ManageClientProfileV1Req manageClientProfileV1Req = new ManageClientProfileV1Req();

                            manageClientProfileV1Req.clientProfileV1 = new ClientProfileV1();
                            manageClientProfileV1Req.clientProfileV1.client = new Client();
                            manageClientProfileV1Req.clientProfileV1.client.action = ACTION_UPDATE;

                            BT.SaaS.IspssAdapter.Dnp.ClientIdentity[] clientIdentity = null;
                            clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity[2];

                            clientIdentity[0] = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                            clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                            clientIdentity[0].managedIdentifierDomain.value = isEECustomer ? EEONLINEID : BTCOM;
                            clientIdentity[0].value = BtoneId;
                            clientIdentity[0].action = ACTION_SEARCH;

                            clientIdentity[1] = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                            clientIdentity[1].managedIdentifierDomain = new ManagedIdentifierDomain();
                            clientIdentity[1].managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;
                            clientIdentity[1].value = BillAccountNumber;
                            clientIdentity[1].clientIdentityStatus = new ClientIdentityStatus();
                            clientIdentity[1].clientIdentityStatus.value = ACTIVE;

                            List<ClientIdentityValidation> listClientIdentityValidation = new List<ClientIdentityValidation>();
                            ClientIdentityValidation clientIdentityValidation = null;

                            clientIdentityValidation = new ClientIdentityValidation();
                            clientIdentityValidation.name = ACCOUNTTRUSTMETHOD;
                            clientIdentityValidation.value = AhtValue;
                            clientIdentityValidation.action = ACTION_INSERT;
                            listClientIdentityValidation.Add(clientIdentityValidation);

                            if (isWarrantyServiceExists && !(string.IsNullOrEmpty(acttype)))
                            {
                                clientIdentityValidation = new ClientIdentityValidation();
                                clientIdentityValidation.name = "ACT_TYPE";
                                clientIdentityValidation.value = acttype;
                                clientIdentityValidation.action = ACTION_INSERT;
                                listClientIdentityValidation.Add(clientIdentityValidation);
                            }

                            clientIdentity[1].clientIdentityValidation = listClientIdentityValidation.ToArray();

                            clientIdentity[1].action = ACTION_INSERT;

                            List<ClientServiceInstanceV1> clientServiceInstanceList1 = new List<ClientServiceInstanceV1>();
                            ClientServiceInstanceV1 clientServiceInstance = null;

                            ServiceIdentity[] serviceidentity = new ServiceIdentity[1];
                            serviceidentity[0] = new ServiceIdentity();
                            serviceidentity[0].domain = BACID_IDENTIFER_NAMEPACE;
                            serviceidentity[0].value = BillAccountNumber;
                            serviceidentity[0].action = ACTION_SEARCH;

                            List<ClientServiceRole> clientServiceRoleList = null;
                            ClientServiceRole clientServiceRole = null;
                            string[] defaultRoleServices = ConfigurationManager.AppSettings["serviceslist"].ToString().Split(',');
                            string[] adminRoleCheckServices = ConfigurationManager.AppSettings["adminrolecheckservices"].ToString().Split(',');
                            foreach (ClientServiceInstanceV1 cservInst in gsiResponse.getServiceInstanceV1Res.clientServiceInstance)
                            {
                                if (!cservInst.serviceIdentity.ToList().Exists(si => si.domain.Equals(BTCOM, StringComparison.OrdinalIgnoreCase)))
                                {
                                    status = cservInst.clientServiceInstanceStatus.value.ToUpper();

                                    clientServiceRoleList = new List<ClientServiceRole>();
                                    clientServiceInstance = new ClientServiceInstanceV1();
                                    clientServiceInstance.clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                                    clientServiceInstance.clientServiceInstanceIdentifier.value = cservInst.clientServiceInstanceIdentifier.value;
                                    clientServiceInstance.clientServiceInstanceStatus = new ClientServiceInstanceStatus();
                                    clientServiceInstance.clientServiceInstanceStatus.value = cservInst.clientServiceInstanceStatus.value;
                                    clientServiceInstance.action = ACTION_UPDATE;

                                    //for wifi && thomas
                                    if (defaultRoleServices.ToList().Contains(cservInst.clientServiceInstanceIdentifier.value))
                                    {
                                        if (cservInst.clientServiceRole == null || (cservInst.clientServiceRole != null && !cservInst.clientServiceRole.ToList().Exists(csr => csr.clientIdentity != null && csr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase) && ci.value.Equals(BtoneId, StringComparison.OrdinalIgnoreCase)))))
                                        {
                                            clientServiceRole = new ClientServiceRole();
                                            clientServiceRole.id = DEFAULT_ROLE;
                                            clientServiceRole.clientServiceRoleStatus = new ClientServiceRoleStatus();
                                            clientServiceRole.clientServiceRoleStatus.value = (status.Equals(ACTIVE) || status.Equals(SUSPENDED)) ? ACTIVE : INACTIVE;//Suspended status for Thomas Debt Management
                                            clientServiceRole.clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity[1];
                                            clientServiceRole.clientIdentity[0] = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                                            clientServiceRole.clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                                            clientServiceRole.clientIdentity[0].managedIdentifierDomain.value = isEECustomer ? EEONLINEID : BTCOM;
                                            clientServiceRole.clientIdentity[0].value = BtoneId;
                                            clientServiceRole.clientIdentity[0].action = ACTION_INSERT;
                                            clientServiceRole.action = ACTION_INSERT;

                                            clientServiceRoleList.Add(clientServiceRole);
                                        }

                                        clientServiceRole = new ClientServiceRole();
                                        clientServiceRole.id = ADMIN_ROLE;
                                        clientServiceRole.clientServiceRoleStatus = new ClientServiceRoleStatus();
                                        clientServiceRole.clientServiceRoleStatus.value = (status == ACTIVE || status.Equals(SUSPENDED)) ? ACTIVE : INACTIVE;//Suspended status for Thomas Debt Management
                                        clientServiceRole.clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity[1];
                                        clientServiceRole.clientIdentity[0] = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                                        clientServiceRole.clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                                        clientServiceRole.clientIdentity[0].managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;
                                        clientServiceRole.clientIdentity[0].value = BillAccountNumber;
                                        clientServiceRole.clientIdentity[0].action = ACTION_INSERT;
                                        clientServiceRole.action = ACTION_INSERT;
                                        clientServiceRoleList.Add(clientServiceRole);

                                    }
                                    //for email,Spring,CF disabled,BT:TV&&Message_manager
                                    else
                                    {
                                        clientServiceRole = new ClientServiceRole();
                                        clientServiceRole.id = ADMIN_ROLE;
                                        clientServiceRole.clientServiceRoleStatus = new ClientServiceRoleStatus();
                                        if (adminRoleCheckServices.Contains(cservInst.clientServiceInstanceIdentifier.value, StringComparer.OrdinalIgnoreCase))
                                            clientServiceRole.clientServiceRoleStatus.value = (status == ACTIVE) ? ACTIVE : INACTIVE;
                                        else clientServiceRole.clientServiceRoleStatus.value = ACTIVE;
                                        clientServiceRole.clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity[1];
                                        clientServiceRole.clientIdentity[0] = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                                        clientServiceRole.clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                                        clientServiceRole.clientIdentity[0].managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;
                                        clientServiceRole.clientIdentity[0].value = BillAccountNumber;
                                        clientServiceRole.clientIdentity[0].action = ACTION_INSERT;
                                        clientServiceRole.action = ACTION_INSERT;
                                        clientServiceRoleList.Add(clientServiceRole);
                                    }

                                    clientServiceInstance.clientServiceRole = clientServiceRoleList.ToArray();
                                    clientServiceInstance.serviceIdentity = serviceidentity;
                                    clientServiceInstanceList1.Add(clientServiceInstance);
                                }

                            }

                            manageClientProfileV1Req.clientProfileV1.clientServiceInstanceV1 = clientServiceInstanceList1.ToArray();
                            manageClientProfileV1Req.clientProfileV1.client.clientIdentity = clientIdentity;
                            request.manageClientProfileV1Request.manageClientProfileV1Req = manageClientProfileV1Req;
                            request.manageClientProfileV1Request.standardHeader = headerblock;

                            e2eData.logMessage(StartedDnPCall, "Started... Dnp manageClientProfileV1 call");
                            e2eData.startOutboundCall(ConfigurationManager.AppSettings["destinationDnP"].ToString(), E2ETransaction.vREQ, ACCOUNT_HOLDER_STATUS);

                            request.manageClientProfileV1Request.standardHeader.e2e = new BT.SaaS.IspssAdapter.Dnp.E2E();
                            request.manageClientProfileV1Request.standardHeader.e2e.E2EDATA = e2eData.toString();

                            LogHelper.LogActivityDetails(bptmTxnId, guid, MakingDnPcall + " to create Admin & Default roles ", System.DateTime.Now - ActivityStartTime, "AHTLogTrace", guid, request.SerializeObject());

                            response = manageClientProfileV1(request, requestOrderRequest);

                            LogHelper.LogActivityDetails(bptmTxnId, guid, GotResponseFromDnP + " to create Admin & Default roles ", System.DateTime.Now - ActivityStartTime, "AHTLogTrace", guid, response.SerializeObject());

                            if (response != null
                                && response.manageClientProfileV1Response != null
                                && response.manageClientProfileV1Response.standardHeader != null
                                && response.manageClientProfileV1Response.standardHeader.e2e != null
                                && response.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null
                                && !String.IsNullOrEmpty(response.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                            {
                                e2eData.logMessage(GotResponseFromDnP, "Ending... Dnp manageClientProfileV1 call");
                                e2eDataPassed = response.manageClientProfileV1Response.standardHeader.e2e.E2EDATA;
                                e2eData.endOutboundCall(e2eDataPassed);
                            }
                            else
                            {
                                e2eData.logMessage(GotResponseFromDnP, "Ending... Dnp manageClientProfileV1 call");
                                e2eData.endOutboundCall(e2eData.toString());
                            }

                            if (response != null
                               && response.manageClientProfileV1Response != null
                               && response.manageClientProfileV1Response.standardHeader != null
                               && response.manageClientProfileV1Response.standardHeader.serviceState != null
                               && response.manageClientProfileV1Response.standardHeader.serviceState.stateCode != null
                               && response.manageClientProfileV1Response.standardHeader.serviceState.stateCode == "0")
                            {
                                bool linkClientProfileFailed = false;
                                string[] defaultRoleServices2 = ConfigurationManager.AppSettings["serviceslist2"].ToString().Split(',');
                                string request1 = string.Empty;
                                linkClientProfilesResponse1 linkResponse = null;
                                foreach (ClientServiceInstanceV1 csi in gsiResponse.getServiceInstanceV1Res.clientServiceInstance.ToArray())
                                {
                                    if (!csi.serviceIdentity.ToList().Exists(si => si.domain.Equals(BTCOM, StringComparison.OrdinalIgnoreCase)))
                                    {
                                        if (defaultRoleServices2.ToList().Contains(csi.clientServiceInstanceIdentifier.value))
                                        {
                                            if (csi.clientServiceInstanceIdentifier.value.Equals(BTIEMAIL_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase))
                                            {
                                                if (csi.clientServiceInstanceCharacteristic != null && (!csi.clientServiceInstanceCharacteristic.ToList().Exists(cs => cs.name.Equals("ACC_ID", StringComparison.OrdinalIgnoreCase)) || csi.clientServiceInstanceCharacteristic.ToList().Exists(csic => csic.name.Equals("ACC_ID", StringComparison.OrdinalIgnoreCase) && String.IsNullOrEmpty(csic.value.Trim()))))
                                                {
                                                    string EmailName = csi.serviceIdentity.ToList().Where(si => si.domain.Equals(BTIEMAILID, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                                    linkResponse = linkClientProfile(BTIEMAIL_NAMEPACE, BtoneId, EmailName, ref request1, requestOrderRequest, ref e2eData, isEECustomer);
                                                }
                                            }
                                            //pavan
                                            if (csi.clientServiceInstanceIdentifier.value.Equals(VOICEINIFNITY_SERVICECODE_NAMEPACE, StringComparison.OrdinalIgnoreCase))
                                            {
                                                string DNName = string.Empty;
                                                foreach (ClientServiceRole role in csi.clientServiceRole.ToList())
                                                {
                                                    if (role.id.ToUpper() == DEFAULT_ROLE && role.clientIdentity != null)
                                                    {
                                                        if (role.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(VoiceInfnity_ClientIdentity, StringComparison.OrdinalIgnoreCase)))
                                                        {
                                                            DNName = role.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(VoiceInfnity_ClientIdentity, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                                            break;
                                                        }
                                                    }
                                                }

                                                linkResponse = linkClientProfile(VoiceInfnity_ClientIdentity, BtoneId, DNName, ref request1, requestOrderRequest, ref e2eData, isEECustomer);
                                            }
                                            //end
                                            if (csi.clientServiceInstanceIdentifier.value.Equals(SPRING_GSM, StringComparison.OrdinalIgnoreCase) && csi.clientServiceRole != null)
                                            {
                                                foreach (ClientServiceRole role in csi.clientServiceRole.ToList())
                                                {
                                                    if (role.id.ToUpper() == DEFAULT_ROLE && role.clientIdentity != null)
                                                    {
                                                        if (role.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(MSISDN_NAMESPACE, StringComparison.OrdinalIgnoreCase)))
                                                        {
                                                            msisdnValue = role.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(MSISDN_NAMESPACE, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                                            break;
                                                        }                                                    
                                                    }
                                                }
                                                if (!string.IsNullOrEmpty(msisdnValue))
                                                    linkResponse = linkClientProfile(MSISDN_NAMESPACE, BtoneId, msisdnValue, ref request1, requestOrderRequest, ref e2eData, isEECustomer);
                                                else
                                                {
                                                    foreach (ClientServiceRole role in csi.clientServiceRole.ToList())
                                                    {
                                                        if (role.id.ToUpper() == DEFAULT_ROLE && role.clientIdentity != null)
                                                        {
                                                            if (role.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(EE_MOBILE_MSISDN_NAMESPACE, StringComparison.OrdinalIgnoreCase)))
                                                            {
                                                                msisdnValue = role.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(EE_MOBILE_MSISDN_NAMESPACE, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                                                break;
                                                            }
                                                        }
                                                    }

                                                    linkResponse = linkClientProfile(EE_MOBILE_MSISDN_NAMESPACE, BtoneId, msisdnValue, ref request1, requestOrderRequest, ref e2eData, isEECustomer);
                                                }
                                            }
                                            if (csi.clientServiceInstanceIdentifier.value.Equals("BT_CONSUMER_BROADBAND", StringComparison.OrdinalIgnoreCase) && csi.clientServiceRole != null)
                                            {
                                                string identity = string.Empty;
                                                string identityDomain = "MOBILE";
                                                foreach (ClientServiceRole role in csi.clientServiceRole.ToList())
                                                {
                                                    if (role.id.ToUpper() == DEFAULT_ROLE && role.clientIdentity != null && role.clientIdentity.Count() > 0)
                                                    {
                                                        if (role.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(identityDomain, StringComparison.OrdinalIgnoreCase)))
                                                        {
                                                            identity = role.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(identityDomain, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                                            break;
                                                        }
                                                    }
                                                }
                                                linkResponse = linkClientProfile(identityDomain, BtoneId, identity, ref request1, requestOrderRequest, ref e2eData, isEECustomer);
                                            }
                                            if (linkResponse != null && linkResponse.linkClientProfilesResponse != null && linkResponse.linkClientProfilesResponse.standardHeader != null
                                                && linkResponse.linkClientProfilesResponse.standardHeader.serviceState != null
                                                && linkResponse.linkClientProfilesResponse.standardHeader.serviceState.stateCode != null &&
                                                linkResponse.linkClientProfilesResponse.standardHeader.serviceState.stateCode == "1")
                                            {
                                                linkClientProfileFailed = true;
                                                break;

                                            }
                                        }
                                    }
                                }
                                if (linkClientProfileFailed && linkResponse != null)
                                {
                                    EventMapper.RaiseEvnt(requestOrderRequest, ExecuteFailedResponse, OrderStatusEnum.failed, request1);
                                    InboundQueueDAL.UpdateQueueRawXML("777", linkResponse.linkClientProfilesResponse.messages[0].description, requestOrderRequest.Order.OrderIdentifier.Value);
                                    notification.sendNotification(false, false, "777", linkResponse.linkClientProfilesResponse.messages[0].description, ref e2eData);
                                }
                                else if (linkResponse != null)
                                {
                                    EventMapper.RaiseEvnt(requestOrderRequest, ExecuteSuccessResponse, OrderStatusEnum.success, request1);
                                    notification.sendNotification(true, false, "004", string.Empty, ref e2eData);
                                }
                                else if (linkResponse == null)
                                {
                                    EventMapper.RaiseEvnt(requestOrderRequest, ExecuteSuccessResponse, OrderStatusEnum.success, request.SerializeObject());
                                    notification.sendNotification(true, false, "004", string.Empty, ref e2eData);
                                }
                            }
                            else
                            {
                                // RaiseEvent
                                EventMapper.RaiseEvnt(requestOrderRequest, ExecuteFailedResponse, OrderStatusEnum.failed, request.SerializeObject());
                                InboundQueueDAL.UpdateQueueRawXML("777", response.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description, requestOrderRequest.Order.OrderIdentifier.Value);
                                notification.sendNotification(false, false, "777", response.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description, ref e2eData);
                            }
                        }
                        else
                        {
                            manageClientIdentityResponse1 Response2 = new manageClientIdentityResponse1();
                            string Request = null;
                            e2eData.logMessage(StartedDnPCall, "Started... Dnp modifyClientIdentities call");
                            e2eData.startOutboundCall(ConfigurationManager.AppSettings["destinationDnP"].ToString(), E2ETransaction.vREQ, ACCOUNT_HOLDER_STATUS);

                            Response2 = modifyClientIdentities(BillAccountNumber, AhtValue, BtoneId, ref Request, requestOrderRequest, e2eData.toString(), isEECustomer);

                            LogHelper.LogActivityDetails(bptmTxnId, guid, MakingDnPcall + " to link BAC & BTID only", System.DateTime.Now - ActivityStartTime, "AHTLogTrace", guid, Request);
                            LogHelper.LogActivityDetails(bptmTxnId, guid, GotResponseFromDnP + " to link BAC & BTID only", System.DateTime.Now - ActivityStartTime, "AHTLogTrace", guid, Response2.SerializeObject());

                            if (Response2 != null
                                       && Response2.manageClientIdentityResponse != null
                                       && Response2.manageClientIdentityResponse.standardHeader != null
                                       && Response2.manageClientIdentityResponse.standardHeader.e2e != null
                                       && Response2.manageClientIdentityResponse.standardHeader.e2e.E2EDATA != null
                                       && !String.IsNullOrEmpty(Response2.manageClientIdentityResponse.standardHeader.e2e.E2EDATA))
                            {
                                e2eData.endOutboundCall(Response2.manageClientIdentityResponse.standardHeader.e2e.E2EDATA);
                            }
                            else
                            {
                                e2eData.endOutboundCall(e2eData.toString());
                            }
                            if (Response2 != null
                                       && Response2.manageClientIdentityResponse != null
                                       && Response2.manageClientIdentityResponse.standardHeader != null
                                       && Response2.manageClientIdentityResponse.standardHeader.serviceState != null
                                       && Response2.manageClientIdentityResponse.standardHeader.serviceState.stateCode != null
                                       && Response2.manageClientIdentityResponse.standardHeader.serviceState.stateCode == "0")
                            {
                                //raiseevent
                                EventMapper.RaiseEvnt(requestOrderRequest, ExecuteSuccessResponse, OrderStatusEnum.success, Request);
                                notification.sendNotification(true, false, "004", string.Empty, ref e2eData);

                            }
                            else
                            {
                                EventMapper.RaiseEvnt(requestOrderRequest, ExecuteFailedResponse, OrderStatusEnum.failed, Request);
                                InboundQueueDAL.UpdateQueueRawXML("777", Response2.manageClientIdentityResponse.manageClientIdentityRes.messages[0].description, requestOrderRequest.Order.OrderIdentifier.Value);
                                notification.sendNotification(false, false, "777", Response2.manageClientIdentityResponse.manageClientIdentityRes.messages[0].description, ref e2eData);
                                //raiseevent
                            }
                        }
                    }
                }
                else if (requestOrderRequest.Order.OrderItem[0].Action.Code.Equals("cancel", StringComparison.OrdinalIgnoreCase))
                {
                    Dictionary<string, string> roleIdentities = new Dictionary<string, string>();
                    bool isDelegateRolesDeletionSuccessful = false;
                    // check weather bac has the delegateroles or not if it has then delete the delegate roles and to further delink
                    gsiResponseDelegate = DnpWrapper.getServiceInstanceV1(BillAccountNumber, BACID_IDENTIFER_NAMEPACE, string.Empty);

                    LogHelper.LogActivityDetails(bptmTxnId, guid, GotResponseFromDnP + " for GSI call", System.DateTime.Now - ActivityStartTime, "AHTLogTrace", guid, gsiResponseDelegate.SerializeObject());

                    if (IsDelegateRolesExists(gsiResponseDelegate, ref delegateRoleDetails, ref roleIdentities))
                    {
                        LogHelper.LogActivityDetails(bptmTxnId, guid, GotResponseFromDnP + " delegateRoleDetails", System.DateTime.Now - ActivityStartTime, "AHTLogTrace", guid, delegateRoleDetails);

                        if (DeleteDelegateRoles(delegateRoleDetails, BillAccountNumber, requestOrderRequest, ref e2eData, ref notification))
                            isDelegateRolesDeletionSuccessful = true;
                    }
                    if (gcp != null && gcp.getClientProfileV1Response != null && gcp.getClientProfileV1Response.getClientProfileV1Res != null && gcp.getClientProfileV1Response.getClientProfileV1Res.clientProfileV1 != null
                        && gcp.getClientProfileV1Response.getClientProfileV1Res.clientProfileV1.clientServiceInstanceV1 != null)
                    {
                        manageClientProfileV1Response1 response = null;
                        manageClientProfileV1Request1 profileReq = null;
                        ClientServiceInstanceV1[] clientServiceInstances = gcp.getClientProfileV1Response.getClientProfileV1Res.clientProfileV1.clientServiceInstanceV1.ToList().Where(csi => csi.serviceIdentity != null && csi.serviceIdentity.ToList().Exists(si => si.domain.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase) && si.value.Equals(BillAccountNumber, StringComparison.OrdinalIgnoreCase))).ToArray();

                        if (gcp.getClientProfileV1Response.getClientProfileV1Res.clientProfileV1.clientServiceInstanceV1.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.ToUpper().Equals("HOMEAWAY") && si.name.Equals(BillAccountNumber, StringComparison.OrdinalIgnoreCase)))
                        {
                            clientServiceInstances = clientServiceInstances.Concat(gcp.getClientProfileV1Response.getClientProfileV1Res.clientProfileV1.clientServiceInstanceV1.ToList().Where(si => si.clientServiceInstanceIdentifier.value.ToUpper().Equals("HOMEAWAY") && si.name.Equals(BillAccountNumber, StringComparison.OrdinalIgnoreCase))).ToArray();
                        }

                        //call to delete AHT from BAC and delete default roles for btwifi/btsports/CA, admin role for HOMEAWAY
                        response = delinkAHTvalidation(clientServiceInstances, BillAccountNumber, requestOrderRequest, ref e2eData, ref profileReq);

                        LogHelper.LogActivityDetails(bptmTxnId, guid, MakingDnPcall + " to delete AHT from BAC and delete default roles", System.DateTime.Now - ActivityStartTime, "AHTLogTrace", guid, profileReq.SerializeObject());
                        LogHelper.LogActivityDetails(bptmTxnId, guid, GotResponseFromDnP + " to delete AHT from BAC and delete default roles", System.DateTime.Now - ActivityStartTime, "AHTLogTrace", guid, response.SerializeObject());

                        if (response != null
                           && response.manageClientProfileV1Response != null
                           && response.manageClientProfileV1Response.standardHeader != null
                           && response.manageClientProfileV1Response.standardHeader.serviceState != null
                           && response.manageClientProfileV1Response.standardHeader.serviceState.stateCode != null
                           && response.manageClientProfileV1Response.standardHeader.serviceState.stateCode == "0")
                        {
                            moveClientIdentitiesResponse1 moveCiResponse = null;
                            moveClientIdentitiesRequest1 moveCIRequest1 = null;

                            // move services along with identities
                            moveCiResponse = moveServiceWithRoles(clientServiceInstances, BillAccountNumber, BtoneId, requestOrderRequest, ref e2eData, ref moveCIRequest1, isEECustomer);

                            LogHelper.LogActivityDetails(bptmTxnId, guid, MakingDnPcall + " to move services along with identities", System.DateTime.Now - ActivityStartTime, "AHTLogTrace", guid, moveCIRequest1.SerializeObject());
                            LogHelper.LogActivityDetails(bptmTxnId, guid, GotResponseFromDnP + " to move services along with identities", System.DateTime.Now - ActivityStartTime, "AHTLogTrace", guid, moveCiResponse.SerializeObject());

                            if (moveCiResponse != null && moveCiResponse.moveClientIdentitiesResponse != null
                                && moveCiResponse.moveClientIdentitiesResponse.standardHeader != null
                                && moveCiResponse.moveClientIdentitiesResponse.standardHeader.serviceState != null
                                && moveCiResponse.moveClientIdentitiesResponse.standardHeader.serviceState.stateCode != null
                                && moveCiResponse.moveClientIdentitiesResponse.standardHeader.serviceState.stateCode == "0")
                            {
                                if (isEECustomer && isDelegateRolesDeletionSuccessful)
                                {
                                    AddDeletedDelegateRoles(roleIdentities, BillAccountNumber, ref e2eData, requestOrderRequest);
                                }
                                //call to insert DEFAULT role for CA service/ADMIN role for HOMEAWAY
                                if (clientServiceInstances.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.ToUpper().Equals(ConfigurationManager.AppSettings["CAService"]))
                                    || clientServiceInstances.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.ToUpper().Equals("HOMEAWAY")))
                                {
                                    manageClientProfileV1Response1 profileResponse = null;
                                    manageClientProfileV1Request1 profileReqst = null;

                                    profileResponse = linkRoleRequest(clientServiceInstances, BillAccountNumber, requestOrderRequest, ref e2eData, ref profileReqst);

                                    LogHelper.LogActivityDetails(bptmTxnId, guid, MakingDnPcall + " to insert DEFAULT role for CA service/ADMIN role for HOMEAWAY", System.DateTime.Now - ActivityStartTime, "AHTLogTrace", guid, profileReqst.SerializeObject());
                                    LogHelper.LogActivityDetails(bptmTxnId, guid, GotResponseFromDnP + " to insert DEFAULT role for CA service/ADMIN role for HOMEAWAY", System.DateTime.Now - ActivityStartTime, "AHTLogTrace", guid, profileResponse.SerializeObject());

                                    if (profileResponse != null
                                       && profileResponse.manageClientProfileV1Response != null
                                       && profileResponse.manageClientProfileV1Response.standardHeader != null
                                       && profileResponse.manageClientProfileV1Response.standardHeader.serviceState != null
                                       && profileResponse.manageClientProfileV1Response.standardHeader.serviceState.stateCode != null
                                       && profileResponse.manageClientProfileV1Response.standardHeader.serviceState.stateCode == "0")
                                    {

                                        if (clientServiceInstances.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"].ToString(), StringComparison.OrdinalIgnoreCase)))
                                        {
                                            if (!manageEmailService(clientServiceInstances.ToList(), BtoneId, BillAccountNumber, requestOrderRequest, ref notification, ref e2eData, guid, ActivityStartTime, bptmTxnId))
                                            {
                                                //raisevent
                                                EventMapper.RaiseEvnt(requestOrderRequest, ExecuteSuccessResponse, OrderStatusEnum.success, profileReqst.SerializeObject());
                                                notification.sendNotification(true, false, "004", string.Empty, ref e2eData);
                                            }

                                        }
                                        else
                                        {
                                            EventMapper.RaiseEvnt(requestOrderRequest, ExecuteSuccessResponse, OrderStatusEnum.success, profileReqst.SerializeObject());
                                            notification.sendNotification(true, false, "004", string.Empty, ref e2eData);
                                        }
                                    }
                                    else
                                    {
                                        EventMapper.RaiseEvnt(requestOrderRequest, ExecuteFailedResponse, OrderStatusEnum.failed, profileReqst.SerializeObject());
                                        InboundQueueDAL.UpdateQueueRawXML("777", profileResponse.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description, requestOrderRequest.Order.OrderIdentifier.Value);
                                        notification.sendNotification(false, false, "777", profileResponse.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description, ref e2eData);
                                    }
                                }
                                else if (clientServiceInstances.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"].ToString(), StringComparison.OrdinalIgnoreCase)))
                                {
                                    if (!manageEmailService(clientServiceInstances.ToList(), BtoneId, BillAccountNumber, requestOrderRequest, ref notification, ref e2eData, guid, ActivityStartTime, bptmTxnId))
                                    {
                                        //raisevent
                                        EventMapper.RaiseEvnt(requestOrderRequest, ExecuteSuccessResponse, OrderStatusEnum.success, moveCIRequest1.SerializeObject());
                                        notification.sendNotification(true, false, "004", string.Empty, ref e2eData);
                                    }
                                }
                                else
                                {
                                    EventMapper.RaiseEvnt(requestOrderRequest, ExecuteSuccessResponse, OrderStatusEnum.success, moveCIRequest1.SerializeObject());
                                    notification.sendNotification(true, false, "004", string.Empty, ref e2eData);
                                }
                            }
                            else
                            {
                                EventMapper.RaiseEvnt(requestOrderRequest, ExecuteFailedResponse, OrderStatusEnum.failed, moveCIRequest1.SerializeObject());
                                InboundQueueDAL.UpdateQueueRawXML("777", "moveClientIdentity request for BillAccountNumber: " + BillAccountNumber + " failed at DnP!!" + moveCiResponse.moveClientIdentitiesResponse.moveClientIdentitiesRes.messages[0].description, requestOrderRequest.Order.OrderIdentifier.Value);
                                notification.sendNotification(false, false, "777", "moveClientIdentity request for BillAccountNumber: " + BillAccountNumber + " failed at DnP!!" + moveCiResponse.moveClientIdentitiesResponse.moveClientIdentitiesRes.messages[0].description, ref e2eData);
                            }
                        }
                        else
                        {
                            EventMapper.RaiseEvnt(requestOrderRequest, ExecuteFailedResponse, OrderStatusEnum.failed, profileReq.SerializeObject());
                            InboundQueueDAL.UpdateQueueRawXML("777", "manageClientProfile request for delinking default roles failed at DnP!!" + response.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description, requestOrderRequest.Order.OrderIdentifier.Value);
                            notification.sendNotification(false, false, "777", "manageClientProfile request for delinking default roles failed at DnP!!" + response.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description, ref e2eData);
                        }
                    }
                    else if (gcp != null && gcp.getClientProfileV1Response != null && gcp.getClientProfileV1Response.getClientProfileV1Res != null && gcp.getClientProfileV1Response.getClientProfileV1Res.clientProfileV1 != null
                        && gcp.getClientProfileV1Response.getClientProfileV1Res.clientProfileV1.client != null && gcp.getClientProfileV1Response.getClientProfileV1Res.clientProfileV1.client.clientIdentity != null && gcp.getClientProfileV1Response.getClientProfileV1Res.clientProfileV1.client.clientIdentity.Count() > 0)
                    {
                        manageClientProfileV1Response1 response = null;
                        manageClientProfileV1Request1 request = null;

                        response = UnlinkBACwithBtCom(BillAccountNumber, BtoneId, requestOrderRequest, ref e2eData, ref request);

                        LogHelper.LogActivityDetails(bptmTxnId, guid, MakingDnPcall + " to UnlinkBACwithBtCom", System.DateTime.Now - ActivityStartTime, "AHTLogTrace", guid, request.SerializeObject());
                        LogHelper.LogActivityDetails(bptmTxnId, guid, GotResponseFromDnP + " to UnlinkBACwithBtCom", System.DateTime.Now - ActivityStartTime, "AHTLogTrace", guid, response.SerializeObject());

                        if (response != null
                           && response.manageClientProfileV1Response != null
                           && response.manageClientProfileV1Response.standardHeader != null
                           && response.manageClientProfileV1Response.standardHeader.serviceState != null
                           && response.manageClientProfileV1Response.standardHeader.serviceState.stateCode != null
                           && response.manageClientProfileV1Response.standardHeader.serviceState.stateCode == "0")
                        {
                            moveClientIdentitiesResponse1 moveCiResponse = null;
                            moveClientIdentitiesRequest1 moveCIRequest1 = null;

                            // move services along with identities
                            moveCiResponse = moveBACfromBtOneID(BillAccountNumber, BtoneId, requestOrderRequest, ref e2eData, ref moveCIRequest1, isEECustomer);

                            LogHelper.LogActivityDetails(bptmTxnId, guid, MakingDnPcall + " to moveBACfromBtOneID", System.DateTime.Now - ActivityStartTime, "AHTLogTrace", guid, moveCIRequest1.SerializeObject());
                            LogHelper.LogActivityDetails(bptmTxnId, guid, GotResponseFromDnP + " to moveBACfromBtOneID", System.DateTime.Now - ActivityStartTime, "AHTLogTrace", guid, moveCiResponse.SerializeObject());

                            if (moveCiResponse != null && moveCiResponse.moveClientIdentitiesResponse != null
                                && moveCiResponse.moveClientIdentitiesResponse.standardHeader != null
                                && moveCiResponse.moveClientIdentitiesResponse.standardHeader.serviceState != null
                                && moveCiResponse.moveClientIdentitiesResponse.standardHeader.serviceState.stateCode != null
                                && moveCiResponse.moveClientIdentitiesResponse.standardHeader.serviceState.stateCode == "0")
                            {
                                EventMapper.RaiseEvnt(requestOrderRequest, ExecuteSuccessResponse, OrderStatusEnum.success, request.SerializeObject());

                                EventMapper.RaiseEvnt(requestOrderRequest, ExecuteSuccessResponse, OrderStatusEnum.success, moveCIRequest1.SerializeObject());
                                notification.sendNotification(true, false, "004", string.Empty, ref e2eData);
                            }
                            else
                            {
                                EventMapper.RaiseEvnt(requestOrderRequest, ExecuteFailedResponse, OrderStatusEnum.failed, moveCIRequest1.SerializeObject());
                                InboundQueueDAL.UpdateQueueRawXML("777", "moveClientIdentity request for BillAccountNumber: " + BillAccountNumber + " failed at DnP!!" + moveCiResponse.moveClientIdentitiesResponse.moveClientIdentitiesRes.messages[0].description, requestOrderRequest.Order.OrderIdentifier.Value);
                                notification.sendNotification(false, false, "777", "moveClientIdentity request for BillAccountNumber: " + BillAccountNumber + " failed at DnP!!" + moveCiResponse.moveClientIdentitiesResponse.moveClientIdentitiesRes.messages[0].description, ref e2eData);
                            }
                        }
                        else
                        {
                            EventMapper.RaiseEvnt(requestOrderRequest, ExecuteFailedResponse, OrderStatusEnum.failed, request.SerializeObject());
                            InboundQueueDAL.UpdateQueueRawXML("777", "manageClientProfile request for delinking default roles failed at DnP!!" + response.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description, requestOrderRequest.Order.OrderIdentifier.Value);
                            notification.sendNotification(false, false, "777", "manageClientProfile request for delinking default roles failed at DnP!!" + response.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description, ref e2eData);
                        }
                    }
                    else
                    {
                        EventMapper.RaiseEvnt(requestOrderRequest, ExecuteFailedResponse, OrderStatusEnum.failed, profileRequest.SerializeObject());
                        InboundQueueDAL.UpdateQueueRawXML("777", "Given BAC not found in the DnP Profile", requestOrderRequest.Order.OrderIdentifier.Value);
                        notification.sendNotification(false, false, "777", "Given BAC not found in the DnP Profile", ref e2eData);
                    }
                }
            }
            catch (DnpException exp)
            {
                throw exp;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                gsiResponse = null;
                gcp = null;
                //clientServiceInstanceList = null;
                BillAccountNumber = null;
                BtoneId = null;
                AhtValue = null;
                msisdnValue = null;
                serviceInstances = null;
            }
        }

        public void AddDeletedDelegateRoles(Dictionary<string, string> roleIdentities, string bacId, ref E2ETransaction e2eData, OrderRequest request)
        {
            List<ClientProfileV1> lstClietProfile = new List<ClientProfileV1>();
            ClientProfileV1 clinetprofile = null;
            manageBatchProfilesV1Request1 batchProfileRequest = null;
            manageBatchProfilesV1Response1 batchResponse = null;
            try
            {
                batchProfileRequest = new manageBatchProfilesV1Request1();
                batchProfileRequest.manageBatchProfilesV1Request = new ManageBatchProfilesV1Request();
                BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock standardHeader = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
                standardHeader.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
                standardHeader.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
                standardHeader.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();
                batchProfileRequest.manageBatchProfilesV1Request.standardHeader = standardHeader;


                foreach (KeyValuePair<string, string> roleIdentity in roleIdentities)
                {
                    if (!string.IsNullOrEmpty(roleIdentity.Value))
                    {
                        string[] roleIdentityList = roleIdentity.Value.ToString().Split(';');

                        if (roleIdentityList != null && roleIdentityList.Count() > 0)
                        {
                            List<BT.SaaS.IspssAdapter.Dnp.ClientIdentity> ciDelegateList = new List<BT.SaaS.IspssAdapter.Dnp.ClientIdentity>();
                            BT.SaaS.IspssAdapter.Dnp.ClientIdentity ciDelegate = null;
                            List<ClientServiceInstanceV1> csiDelegateList = new List<ClientServiceInstanceV1>();
                            ClientServiceInstanceV1 csiDelegate = null;

                            ciDelegate = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                            ciDelegate.managedIdentifierDomain = new ManagedIdentifierDomain();
                            foreach (string Identity in roleIdentityList)
                            {
                                string[] identityDetails = Identity.Split(',');
                                if (!identityDetails[0].Equals(EE_MOBILE_MSISDN_NAMESPACE, StringComparison.OrdinalIgnoreCase))
                                {
                                    ciDelegate.managedIdentifierDomain.value = identityDetails[0];
                                    ciDelegate.value = identityDetails[1].ToString();
                                    break;
                                }
                            }
                            ciDelegate.action = ACTION_SEARCH;
                            ciDelegateList.Add(ciDelegate);

                            csiDelegate = new ClientServiceInstanceV1();

                            csiDelegate = InsertServiceinstanceobject(ACTION_INSERT, roleIdentityList, bacId);//pass the values correctly.

                            csiDelegateList.Add(csiDelegate);

                            clinetprofile = new ClientProfileV1();
                            clinetprofile.client = new Client();
                            clinetprofile.client.action = ACTION_UPDATE;
                            clinetprofile.client.clientIdentity = ciDelegateList.ToArray();
                            clinetprofile.clientServiceInstanceV1 = csiDelegateList.ToArray();
                            lstClietProfile.Add(clinetprofile);
                        }
                    }
                }
                batchProfileRequest.manageBatchProfilesV1Request.ManageBatchProfilesV1Req = lstClietProfile.ToArray();

                batchProfileRequest.manageBatchProfilesV1Request.standardHeader.e2e = new BT.SaaS.IspssAdapter.Dnp.E2E();
                batchProfileRequest.manageBatchProfilesV1Request.standardHeader.e2e.E2EDATA = e2eData.toString();

                LogHelper.LogActivityDetails(bptmTxnId, guid, MakingDnPcall + " to Add deleted Delegate roles", System.DateTime.Now - ActivityStartTime, "AHTLogTrace", guid, batchProfileRequest.SerializeObject());

                batchResponse = manageBatchProfilesV1(batchProfileRequest, request);

                LogHelper.LogActivityDetails(bptmTxnId, guid, GotResponseFromDnP + " to Add deleted Delegate roles", System.DateTime.Now - ActivityStartTime, "AHTLogTrace", guid, batchResponse.SerializeObject());

                if (batchResponse != null
                      && batchResponse.manageBatchProfilesV1Response != null
                      && batchResponse.manageBatchProfilesV1Response.standardHeader != null
                      && batchResponse.manageBatchProfilesV1Response.standardHeader.serviceState != null
                      && batchResponse.manageBatchProfilesV1Response.standardHeader.serviceState.stateCode != null
                      && batchResponse.manageBatchProfilesV1Response.standardHeader.serviceState.stateCode == "0")
                {
                    e2eData.logMessage(GotResponseFromDnP, "Ending... Dnp managebatchProfileV1 call success while adding deleted DelegateRoles");
                    e2eDataPassed = batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA;
                    e2eData.endOutboundCall(e2eDataPassed);
                }
                else
                {
                    e2eData.logMessage(GotResponseFromDnP, "Ending... Dnp manageClientProfileV1 call failed while adding deleted delegateroles");
                    e2eData.endOutboundCall(e2eData.toString());
                }
            }

            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                roleIdentities = null;
            }
        }
        public ClientServiceInstanceV1 InsertServiceinstanceobject(string action, string[] roleIdentityList, string bac)
        {
            string roleid = string.Empty;

            ServiceIdentity[] serviceIdentity = new ServiceIdentity[1];
            serviceIdentity[0] = new ServiceIdentity();
            serviceIdentity[0].domain = BACID_IDENTIFER_NAMEPACE;
            serviceIdentity[0].value = bac;
            serviceIdentity[0].action = ACTION_SEARCH;

            ClientServiceInstanceV1 clientServiceInstanceV1 = new ClientServiceInstanceV1();

            clientServiceInstanceV1 = new ClientServiceInstanceV1();
            clientServiceInstanceV1.clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
            clientServiceInstanceV1.clientServiceInstanceIdentifier.value = SPRING_GSM;
            clientServiceInstanceV1.clientServiceInstanceStatus = new ClientServiceInstanceStatus();
            clientServiceInstanceV1.clientServiceInstanceStatus.value = ACTIVE;
            clientServiceInstanceV1.serviceIdentity = serviceIdentity;
            clientServiceInstanceV1.action = ACTION_UPDATE;

            ClientServiceRole clientServiceRole = new ClientServiceRole();
            clientServiceRole = new ClientServiceRole();
            
            IspssAdapter.Dnp.ClientIdentity clientidentiy = null;
            List<IspssAdapter.Dnp.ClientIdentity> ClinetIdnetityList = new List<IspssAdapter.Dnp.ClientIdentity>();

            foreach (string Identity in roleIdentityList)
            {
                string[] identities = Identity.Split(',');

                clientidentiy = new IspssAdapter.Dnp.ClientIdentity();
                clientidentiy.action = action;
                clientidentiy.managedIdentifierDomain = new ManagedIdentifierDomain();
                clientidentiy.managedIdentifierDomain.value = identities[0];
                clientidentiy.clientIdentityStatus = new ClientIdentityStatus();
                clientidentiy.clientIdentityStatus.value = ACTIVE;
                clientidentiy.value = identities[1];

                ClinetIdnetityList.Add(clientidentiy);

                roleid = identities[2];
            }

            clientServiceRole.id = roleid;
            clientServiceRole.action = action;
            clientServiceRole.clientServiceRoleStatus = new ClientServiceRoleStatus();
            clientServiceRole.clientServiceRoleStatus.value = ACTIVE;

            clientServiceRole.clientIdentity = ClinetIdnetityList.ToArray();

            clientServiceInstanceV1.clientServiceRole = new ClientServiceRole[1];
            clientServiceInstanceV1.clientServiceRole[0] = clientServiceRole;

            return clientServiceInstanceV1;
        }
        public bool manageEmailService(List<ClientServiceInstanceV1> serviceInstances, string BtoneId, string BillAccountNumber, OrderRequest requestOrderRequest, ref MSEOOrderNotification notification, ref E2ETransaction e2eData, Guid guid, System.DateTime startedTime, string bptmTxnId)
        {
            moveClientIdentitiesRequest1 moveCIRequest1 = null;
            manageBatchProfilesV1Response1 batchResponse = null;
            manageBatchProfilesV1Request1 batchProfileRequest = null;
            List<string> emailIdentities = new List<string>();
            string errormessage = string.Empty;
            bool notificationSent = false;
            try
            {
                ClientServiceInstanceV1 srvcInstance = serviceInstances.ToList().Where(si => si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"].ToString(), StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                if (srvcInstance.clientServiceRole != null)
                {
                    foreach (ClientServiceRole csr in srvcInstance.clientServiceRole.ToList().Where(sr => sr.id.Equals("default", StringComparison.OrdinalIgnoreCase)).ToList())
                    {
                        if (csr != null && csr.clientIdentity != null)
                        {
                            emailIdentities.Add(csr.clientIdentity.FirstOrDefault().value);
                        }
                    }
                }
                if (emailIdentities != null && emailIdentities.Count > 0)
                {
                    notificationSent = true;
                    errormessage = moveEmailIdentities(BtoneId, emailIdentities, requestOrderRequest, ref e2eData, ref moveCIRequest1, guid, startedTime, bptmTxnId);

                    if (string.IsNullOrEmpty(errormessage))
                    {
                        batchResponse = changeEmailDefaultRoleStatus(emailIdentities, BillAccountNumber, requestOrderRequest, ref e2eData, ref batchProfileRequest);

                        LogHelper.LogActivityDetails(bptmTxnId, guid, MakingDnPcall + " to changeEmailDefaultRoleStatus", System.DateTime.Now - startedTime, "AHTLogTrace", guid, batchProfileRequest.SerializeObject());
                        LogHelper.LogActivityDetails(bptmTxnId, guid, GotResponseFromDnP + " to changeEmailDefaultRoleStatus", System.DateTime.Now - startedTime, "AHTLogTrace", guid, batchResponse.SerializeObject());

                        if (batchResponse != null
                            && batchResponse.manageBatchProfilesV1Response != null
                            && batchResponse.manageBatchProfilesV1Response.standardHeader != null
                            && batchResponse.manageBatchProfilesV1Response.standardHeader.serviceState != null
                            && batchResponse.manageBatchProfilesV1Response.standardHeader.serviceState.stateCode != null
                            && batchResponse.manageBatchProfilesV1Response.standardHeader.serviceState.stateCode == "0")
                        {
                            e2eData.logMessage(StartedDnPCall, "Ending... Dnp manageBatchProfilesV1 call");
                            //raisevent
                            EventMapper.RaiseEvnt(requestOrderRequest, ExecuteSuccessResponse, OrderStatusEnum.success, batchProfileRequest.SerializeObject());
                            notification.sendNotification(true, false, "004", string.Empty, ref e2eData);
                        }
                        else
                        {
                            e2eData.logMessage(StartedDnPCall, "Ending... Dnp manageBatchProfilesV1 call");
                            //raisevent
                            EventMapper.RaiseEvnt(requestOrderRequest, ExecuteFailedResponse, OrderStatusEnum.failed, batchProfileRequest.SerializeObject());
                            InboundQueueDAL.UpdateQueueRawXML("777", "manageBatch call for updating email default role status failed at DnP!!" + batchResponse.manageBatchProfilesV1Response.manageBatchProfilesV1Res.messages[0].description, requestOrderRequest.Order.OrderIdentifier.Value);
                            notification.sendNotification(false, false, "777", "manageBatch call for updating email default role status failed at DnP!!" + batchResponse.manageBatchProfilesV1Response.manageBatchProfilesV1Res.messages[0].description, ref e2eData);
                        }
                    }
                    else
                    {
                        //raisevent
                        EventMapper.RaiseEvnt(requestOrderRequest, ExecuteFailedResponse, OrderStatusEnum.failed, moveCIRequest1.SerializeObject());
                        InboundQueueDAL.UpdateQueueRawXML("777", errormessage, requestOrderRequest.Order.OrderIdentifier.Value);
                        notification.sendNotification(false, false, "777", errormessage, ref e2eData);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                emailIdentities = null;
            }
            return notificationSent;
        }
        public manageBatchProfilesV1Response1 changeEmailDefaultRoleStatus(List<string> emailIdentities, string BillAccountNumber, OrderRequest requestOrderRequest, ref E2ETransaction e2eData, ref manageBatchProfilesV1Request1 request)
        {
            List<ClientProfileV1> clientProfileList = new List<ClientProfileV1>();
            ClientProfileV1 clientProfile = null;
            manageBatchProfilesV1Request1 batchProfileRequest = null;
            manageBatchProfilesV1Response1 batchResponse = null;
            try
            {
                batchProfileRequest = new manageBatchProfilesV1Request1();
                batchProfileRequest.manageBatchProfilesV1Request = new ManageBatchProfilesV1Request();
                BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock standardHeader = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
                standardHeader.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
                standardHeader.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
                standardHeader.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();
                batchProfileRequest.manageBatchProfilesV1Request.standardHeader = standardHeader;
                if (emailIdentities != null && emailIdentities.Count > 0)
                {
                    foreach (string emailIdentity in emailIdentities)
                    {
                        if (emailIdentity != null)
                        {
                            clientProfile = new ClientProfileV1();
                            clientProfile.client = new Client();
                            clientProfile.client.action = ACTION_UPDATE;

                            BT.SaaS.IspssAdapter.Dnp.ClientIdentity[] clientIden = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity[1];
                            clientIden[0] = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                            clientIden[0].value = emailIdentity;
                            clientIden[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                            clientIden[0].managedIdentifierDomain.value = BTIEMAIL_NAMEPACE;
                            clientIden[0].action = ACTION_SEARCH;

                            ClientServiceInstanceV1 emailService = new ClientServiceInstanceV1();
                            emailService.clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                            emailService.clientServiceInstanceIdentifier.value = ConfigurationManager.AppSettings["DanteIdentifier"].ToString();
                            emailService.action = ACTION_UPDATE;
                            emailService.clientServiceRole = new ClientServiceRole[1];
                            emailService.clientServiceRole[0] = new ClientServiceRole();
                            emailService.clientServiceRole[0].id = DEFAULT_ROLE;
                            emailService.clientServiceRole[0].action = ACTION_UPDATE;
                            emailService.clientServiceRole[0].clientServiceRoleStatus = new ClientServiceRoleStatus();
                            emailService.clientServiceRole[0].clientServiceRoleStatus.value = "UNRESOLVED";
                            emailService.clientServiceRole[0].clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity[1];
                            emailService.clientServiceRole[0].clientIdentity[0] = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                            emailService.clientServiceRole[0].clientIdentity[0].action = ACTION_SEARCH;
                            emailService.clientServiceRole[0].clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                            emailService.clientServiceRole[0].clientIdentity[0].managedIdentifierDomain.value = BTIEMAIL_NAMEPACE;
                            emailService.clientServiceRole[0].clientIdentity[0].value = emailIdentity;
                            emailService.serviceIdentity = new ServiceIdentity[1];
                            emailService.serviceIdentity[0] = new ServiceIdentity();
                            emailService.serviceIdentity[0].action = ACTION_SEARCH;
                            emailService.serviceIdentity[0].value = BillAccountNumber;
                            emailService.serviceIdentity[0].domain = BACID_IDENTIFER_NAMEPACE;

                            clientProfile.client.clientIdentity = clientIden;
                            clientProfile.clientServiceInstanceV1 = new ClientServiceInstanceV1[1];
                            clientProfile.clientServiceInstanceV1[0] = emailService;

                            clientProfileList.Add(clientProfile);

                        }
                    }
                }
                batchProfileRequest.manageBatchProfilesV1Request.ManageBatchProfilesV1Req = clientProfileList.ToArray();
                e2eData.logMessage(StartedDnPCall, "Started... Dnp manageBatchProfilesV1 call");
                e2eData.startOutboundCall(ConfigurationManager.AppSettings["destinationDnP"].ToString(), E2ETransaction.vREQ, ACCOUNT_HOLDER_STATUS);
                batchProfileRequest.manageBatchProfilesV1Request.standardHeader.e2e = new BT.SaaS.IspssAdapter.Dnp.E2E();
                batchProfileRequest.manageBatchProfilesV1Request.standardHeader.e2e.E2EDATA = e2eData.toString();
                request = batchProfileRequest;

                batchResponse = manageBatchProfilesV1(batchProfileRequest, requestOrderRequest);

                if (batchResponse != null
                     && batchResponse.manageBatchProfilesV1Response != null
                     && batchResponse.manageBatchProfilesV1Response.standardHeader != null
                     && batchResponse.manageBatchProfilesV1Response.standardHeader.e2e != null
                     && batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA != null
                     && !String.IsNullOrEmpty(batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA))
                {
                    e2eDataPassed = batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA;
                    e2eData.endOutboundCall(e2eDataPassed);
                }
                else
                {
                    e2eData.endOutboundCall(e2eData.toString());
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                clientProfileList = null;
            }
            return batchResponse;

        }
        public string moveEmailIdentities(string BToneID, List<string> emailIdList, OrderRequest requestOrderRequest, ref E2ETransaction e2eData, ref moveClientIdentitiesRequest1 request, Guid guid, System.DateTime startedTime, string bptmTxnId)
        {
            string errorMessage = string.Empty;
            try
            {
                foreach (string email in emailIdList)
                {
                    moveClientIdentitiesResponse1 moveCiResponse = null;
                    BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock headerBlock = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
                    headerBlock.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
                    headerBlock.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
                    headerBlock.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();

                    moveClientIdentitiesRequest1 moveCIRequest1 = new moveClientIdentitiesRequest1();

                    MoveClientIdentitiesRequest moveCIRequest = new MoveClientIdentitiesRequest();
                    MoveClientIdentitiesReq moveCIReq = new MoveClientIdentitiesReq();
                    moveCIReq.isMoveServiceRole = "YES";
                    moveCIReq.mciSourceSearchCriteria = new ClientSearchCriteria();
                    moveCIReq.mciSourceSearchCriteria.identifierDomainCode = BTCOM;
                    moveCIReq.mciSourceSearchCriteria.identifierValue = BToneID;

                    moveCIReq.mciDestinationSearchCriteria = new ClientSearchCriteria();
                    moveCIReq.mciDestinationSearchCriteria.identifierDomainCode = BTCOM;
                    moveCIReq.mciDestinationSearchCriteria.identifierValue = "dummy_move_dest_user";

                    BT.SaaS.IspssAdapter.Dnp.ClientIdentity[] clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity[1];

                    clientIdentity[0] = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                    clientIdentity[0].value = email;
                    clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientIdentity[0].managedIdentifierDomain.value = BTIEMAIL_NAMEPACE;

                    moveCIReq.clientIdentity = clientIdentity;

                    moveCIRequest.moveClientIdentitiesReq = moveCIReq;
                    moveCIRequest.standardHeader = headerBlock;
                    moveCIRequest.standardHeader.e2e = new BT.SaaS.IspssAdapter.Dnp.E2E();
                    moveCIRequest.standardHeader.e2e.E2EDATA = e2eData.toString();
                    moveCIRequest1.moveClientIdentitiesRequest = moveCIRequest;

                    request = moveCIRequest1;
                    e2eData.logMessage(StartedDnPCall, "Started... Dnp moveClientIdentities call");

                    e2eData.startOutboundCall(ConfigurationManager.AppSettings["destinationDnP"].ToString(), E2ETransaction.vREQ, ACCOUNT_HOLDER_STATUS);

                    LogHelper.LogActivityDetails(bptmTxnId, guid, MakingDnPcall + " to moveEmailIdentities", System.DateTime.Now - startedTime, "AHTLogTrace", guid, moveCIRequest1.SerializeObject());

                    moveCiResponse = moveClientIdentities(moveCIRequest1, requestOrderRequest);

                    LogHelper.LogActivityDetails(bptmTxnId, guid, GotResponseFromDnP + " to moveEmailIdentities", System.DateTime.Now - startedTime, "AHTLogTrace", guid, moveCiResponse.SerializeObject());

                    if (moveCiResponse != null
                                   && moveCiResponse.moveClientIdentitiesResponse != null
                                   && moveCiResponse.moveClientIdentitiesResponse.standardHeader != null
                                   && moveCiResponse.moveClientIdentitiesResponse.standardHeader.e2e != null
                                   && moveCiResponse.moveClientIdentitiesResponse.standardHeader.e2e.E2EDATA != null
                                   && !String.IsNullOrEmpty(moveCiResponse.moveClientIdentitiesResponse.standardHeader.e2e.E2EDATA))
                    {
                        e2eData.logMessage(GotResponseFromDnP, "Ending... Dnp moveClientIdentities call");
                        e2eDataPassed = moveCiResponse.moveClientIdentitiesResponse.standardHeader.e2e.E2EDATA;
                        e2eData.endOutboundCall(e2eDataPassed);
                    }
                    else
                    {
                        e2eData.logMessage(GotResponseFromDnP, "Ending... Dnp moveClientIdentities call");
                        e2eData.endOutboundCall(e2eData.toString());
                    }
                    if (moveCiResponse != null && moveCiResponse.moveClientIdentitiesResponse != null
                               && moveCiResponse.moveClientIdentitiesResponse.standardHeader != null
                               && moveCiResponse.moveClientIdentitiesResponse.standardHeader.serviceState != null
                               && moveCiResponse.moveClientIdentitiesResponse.standardHeader.serviceState.stateCode != null
                               && moveCiResponse.moveClientIdentitiesResponse.standardHeader.serviceState.stateCode == "0")
                    {
                        continue;
                    }
                    else
                    {
                        errorMessage = "move client idenity request for emailname: " + email + "failed at Dnp!!" + moveCiResponse.moveClientIdentitiesResponse.moveClientIdentitiesRes.messages[0].description;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return errorMessage;
        }

        public static getClientProfileV1Response1 GetClientProfileV1(string identity, string identityDomain, string e2eData, OrderRequest requestOrderRequest, ref getClientProfileV1Request1 profileRequest)
        {
            getClientProfileV1Response1 profileResponse;
            try
            {
                profileRequest = new getClientProfileV1Request1();
                profileRequest.getClientProfileV1Request = new GetClientProfileV1Request();
                profileRequest.getClientProfileV1Request.standardHeader = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
                profileRequest.getClientProfileV1Request.standardHeader.e2e = new BT.SaaS.IspssAdapter.Dnp.E2E();
                profileRequest.getClientProfileV1Request.standardHeader.e2e.E2EDATA = e2eData;
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
                        profileResponse = svc.getClientProfileV1(profileRequest);
                    }
                }
            }
            catch (Exception ex)
            {
                EventMapper.RaiseEvnt(requestOrderRequest, ExecuteFailedResponse, OrderStatusEnum.failed, profileRequest.SerializeObject());
                throw new DnpException(ex.Message);
            }
            return profileResponse;
        }


        private static bool getClientServiceInstance(string identity, string identityDomain, string serviceCode, ref GetServiceInstanceV1Response service, OrderRequest requestOrderRequest, string e2eData)
        {
            getServiceInstanceV1Request1 request = null;
            getServiceInstanceV1Response1 serviceProfileResponse = null;

            request = new getServiceInstanceV1Request1();
            request.getServiceInstanceV1Request = new GetServiceInstanceV1Request();
            request.getServiceInstanceV1Request.standardHeader = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
            request.getServiceInstanceV1Request.standardHeader.e2e = new BT.SaaS.IspssAdapter.Dnp.E2E();
            request.getServiceInstanceV1Request.standardHeader.e2e.E2EDATA = e2eData;
            request.getServiceInstanceV1Request.standardHeader.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();
            request.getServiceInstanceV1Request.standardHeader.serviceState.stateCode = "OK";

            request.getServiceInstanceV1Request.standardHeader.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
            request.getServiceInstanceV1Request.standardHeader.serviceAddressing.from = "http://www.profile.com?SAASMSEO";

            request.getServiceInstanceV1Request.getServiceInstanceV1Req = new GetServiceInstanceV1Req();
            request.getServiceInstanceV1Request.getServiceInstanceV1Req.clientServiceInstanceSearchCriteria = new ClientServiceInstanceSearchCriteriaV1();
            request.getServiceInstanceV1Request.getServiceInstanceV1Req.clientServiceInstanceSearchCriteria.serviceIdentityDomain = identityDomain;
            request.getServiceInstanceV1Request.getServiceInstanceV1Req.clientServiceInstanceSearchCriteria.serviceIdentityValue = identity;
            request.getServiceInstanceV1Request.getServiceInstanceV1Req.clientServiceInstanceSearchCriteria.serviceCode = serviceCode;

            byte[] credBuffer = new UTF8Encoding().GetBytes(ConfigurationManager.AppSettings["DnPUser"].ToString() + ":" + ConfigurationManager.AppSettings["DnPPassword"].ToString());
            HttpRequestMessageProperty httpRequest = new HttpRequestMessageProperty();
            httpRequest.Headers.Add(HTTP_HEADER_AUTHORIZATION, AUTHORIZATION_TYPE_BASIC + Convert.ToBase64String(credBuffer));
            try
            {
                using (ChannelFactory<ServiceProfileSyncPortType> factory = new ChannelFactory<ServiceProfileSyncPortType>("ServiceProfile_DnP"))
                {
                    ServiceProfileSyncPortType svc = factory.CreateChannel();
                    using (OperationContextScope scope = new OperationContextScope((IContextChannel)svc))
                    {
                        OperationContext.Current.OutgoingMessageProperties.Add(HttpRequestMessageProperty.Name, httpRequest);
                        serviceProfileResponse = svc.getServiceInstanceV1(request);
                    }


                    if (serviceProfileResponse != null && serviceProfileResponse.getServiceInstanceV1Response != null &&
                        serviceProfileResponse.getServiceInstanceV1Response.standardHeader != null &&
                        serviceProfileResponse.getServiceInstanceV1Response.standardHeader.serviceState != null &&
                        serviceProfileResponse.getServiceInstanceV1Response.standardHeader.serviceState.stateCode != null &&
                        serviceProfileResponse.getServiceInstanceV1Response.standardHeader.serviceState.stateCode == "0")
                    {
                        if (serviceProfileResponse.getServiceInstanceV1Response.getServiceInstanceV1Res != null &&
                            serviceProfileResponse.getServiceInstanceV1Response.getServiceInstanceV1Res.clientServiceInstance != null)
                        {
                            service = serviceProfileResponse.getServiceInstanceV1Response;
                            return true;
                        }
                    }

                    else
                    {
                        if (serviceProfileResponse != null && serviceProfileResponse.getServiceInstanceV1Response != null && serviceProfileResponse.getServiceInstanceV1Response.getServiceInstanceV1Res != null && serviceProfileResponse.getServiceInstanceV1Response.getServiceInstanceV1Res.messages != null)
                        {
                            service = serviceProfileResponse.getServiceInstanceV1Response;
                            SaaSExceptionManager.OutputTrace("DnP Adapter get service instance for identity : " + identity + " and domain " + identityDomain + " failed with error :" + serviceProfileResponse.getServiceInstanceV1Response.getServiceInstanceV1Res.messages[0].description);
                        }
                        else
                        {
                            SaaSExceptionManager.OutputTrace("DnP Adapter get service instance for identity returned a NULL");
                        }
                        return false;
                    }

                }
            }
            catch (Exception exp)
            {
                EventMapper.RaiseEvnt(requestOrderRequest, ExecuteFailedResponse, OrderStatusEnum.failed, request.SerializeObject());
                throw new DnpException(exp.Message.ToString());
            }
            return false;
        }

        public static manageClientProfileResponse1 manageClientProfile(manageClientProfileRequest1 request, OrderRequest requestOrderRequest)
        {
            manageClientProfileResponse1 response = null;
            try
            {
                byte[] credBuffer = new UTF8Encoding().GetBytes(ConfigurationManager.AppSettings["DnPUser"].ToString() + ":" + ConfigurationManager.AppSettings["DnPPassword"].ToString());
                HttpRequestMessageProperty httpRequest = new HttpRequestMessageProperty();
                httpRequest.Headers.Add(HTTP_HEADER_AUTHORIZATION, AUTHORIZATION_TYPE_BASIC + Convert.ToBase64String(credBuffer));
                using (ChannelFactory<ClientProfileSyncPortType> factory = new ChannelFactory<ClientProfileSyncPortType>(@"ClientProfile_DnP"))
                {
                    ClientProfileSyncPortType svc = factory.CreateChannel();
                    using (OperationContextScope scope = new OperationContextScope((IContextChannel)svc))
                    {
                        OperationContext.Current.OutgoingMessageProperties.Add(HttpRequestMessageProperty.Name, httpRequest);
                        response = svc.manageClientProfile(request);
                    }
                }
            }
            catch (Exception ex)
            {
                EventMapper.RaiseEvnt(requestOrderRequest, ExecuteFailedResponse, OrderStatusEnum.failed, request.SerializeObject());
                throw new DnpException(ex.Message);
            }
            return response;
        }

        public static manageClientProfileV1Response1 manageClientProfileV1(manageClientProfileV1Request1 request, OrderRequest requestOrderRequest)
        {
            manageClientProfileV1Response1 response = null;
            try
            {
                byte[] credBuffer = new UTF8Encoding().GetBytes(ConfigurationManager.AppSettings["DnPUser"].ToString() + ":" + ConfigurationManager.AppSettings["DnPPassword"].ToString());
                HttpRequestMessageProperty httpRequest = new HttpRequestMessageProperty();
                httpRequest.Headers.Add(HTTP_HEADER_AUTHORIZATION, AUTHORIZATION_TYPE_BASIC + Convert.ToBase64String(credBuffer));
                using (ChannelFactory<ClientProfileSyncPortType> factory = new ChannelFactory<ClientProfileSyncPortType>(@"ClientProfile_DnP"))
                {
                    ClientProfileSyncPortType svc = factory.CreateChannel();
                    using (OperationContextScope scope = new OperationContextScope((IContextChannel)svc))
                    {
                        OperationContext.Current.OutgoingMessageProperties.Add(HttpRequestMessageProperty.Name, httpRequest);
                        response = svc.manageClientProfileV1(request);
                    }
                }
            }
            catch (Exception excep)
            {
                EventMapper.RaiseEvnt(requestOrderRequest, ExecuteFailedResponse, OrderStatusEnum.failed, request.SerializeObject());
                throw new DnpException(excep.Message);
            }
            return response;
        }

        public static manageClientProfileV1Response1 manageClientProfileV1(manageClientProfileV1Request1 request)
        {
            manageClientProfileV1Response1 response = null;
            try
            {
                byte[] credBuffer = new UTF8Encoding().GetBytes(ConfigurationManager.AppSettings["DnPUser"].ToString() + ":" + ConfigurationManager.AppSettings["DnPPassword"].ToString());
                HttpRequestMessageProperty httpRequest = new HttpRequestMessageProperty();
                httpRequest.Headers.Add(HTTP_HEADER_AUTHORIZATION, AUTHORIZATION_TYPE_BASIC + Convert.ToBase64String(credBuffer));
                using (ChannelFactory<ClientProfileSyncPortType> factory = new ChannelFactory<ClientProfileSyncPortType>(@"ClientProfile_DnP"))
                {
                    ClientProfileSyncPortType svc = factory.CreateChannel();
                    using (OperationContextScope scope = new OperationContextScope((IContextChannel)svc))
                    {
                        OperationContext.Current.OutgoingMessageProperties.Add(HttpRequestMessageProperty.Name, httpRequest);
                        response = svc.manageClientProfileV1(request);
                    }
                }
            }
            catch (Exception excep)
            {
                throw new DnpException(excep.Message);
            }
            return response;
        }

        public static manageBatchProfilesV1Response1 manageBatchProfilesV1(manageBatchProfilesV1Request1 request, OrderRequest requestOrderRequest)
        {
            manageBatchProfilesV1Response1 profileResponse = null;
            try
            {

                byte[] credBuffer = new UTF8Encoding().GetBytes(ConfigurationManager.AppSettings["DnPUser"].ToString() + ":" + ConfigurationManager.AppSettings["DnPPassword"].ToString());

                HttpRequestMessageProperty httpRequest = new HttpRequestMessageProperty();
                httpRequest.Headers.Add(HTTP_HEADER_AUTHORIZATION, AUTHORIZATION_TYPE_BASIC + Convert.ToBase64String(credBuffer));
                using (ChannelFactory<ClientProfileSyncPortType> factory = new ChannelFactory<ClientProfileSyncPortType>(@"ClientProfile_DnP"))
                {
                    ClientProfileSyncPortType svc = factory.CreateChannel();
                    using (OperationContextScope scope = new OperationContextScope((IContextChannel)svc))
                    {
                        OperationContext.Current.OutgoingMessageProperties.Add(HttpRequestMessageProperty.Name, httpRequest);
                        profileResponse = svc.manageBatchProfilesV1(request);
                        SaaSExceptionManager.OutputTrace("Response from Dnp" + profileResponse.SerializeDataContract());
                    }
                }
            }
            catch (Exception ex)
            {
                EventMapper.RaiseEvnt(requestOrderRequest, ExecuteFailedResponse, OrderStatusEnum.failed, request.SerializeObject());
                throw new DnpException(ex.Message);
            }
            return profileResponse;
        }

        public static manageClientIdentityResponse1 modifyClientIdentities(string BillAccountNumber, string AhtValue, string BtoneId, ref string Req, OrderRequest requestOrderRequest, string e2eData, bool isEECustomer)
        {

            manageClientIdentityResponse1 Response = new manageClientIdentityResponse1();
            manageClientIdentityRequest1 Request = new manageClientIdentityRequest1();
            ManageClientIdentityRequest manageclientidentityreq = new ManageClientIdentityRequest();
            manageclientidentityreq.manageClientIdentityReq = new ManageClientIdentityReq();
            try
            {
                BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock headerblock = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
                headerblock.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
                headerblock.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
                headerblock.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();
                headerblock.serviceState.stateCode = "OK";
                headerblock.e2e = new BT.SaaS.IspssAdapter.Dnp.E2E();
                headerblock.e2e.E2EDATA = e2eData;
                manageclientidentityreq.manageClientIdentityReq.clientSearchCriteria = new ClientSearchCriteria();
                manageclientidentityreq.manageClientIdentityReq.clientSearchCriteria.identifierDomainCode = isEECustomer ? EEONLINEID : BTCOM;
                manageclientidentityreq.manageClientIdentityReq.clientSearchCriteria.identifierValue = BtoneId;

                manageclientidentityreq.manageClientIdentityReq.clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                manageclientidentityreq.manageClientIdentityReq.clientIdentity.action = ACTION_INSERT;
                manageclientidentityreq.manageClientIdentityReq.clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                manageclientidentityreq.manageClientIdentityReq.clientIdentity.managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;
                manageclientidentityreq.manageClientIdentityReq.clientIdentity.clientIdentityStatus = new ClientIdentityStatus();
                manageclientidentityreq.manageClientIdentityReq.clientIdentity.clientIdentityStatus.value = ACTIVE;
                manageclientidentityreq.manageClientIdentityReq.clientIdentity.value = BillAccountNumber;

                manageclientidentityreq.manageClientIdentityReq.clientIdentity.clientIdentityValidation = new ClientIdentityValidation[1];
                manageclientidentityreq.manageClientIdentityReq.clientIdentity.clientIdentityValidation[0] = new ClientIdentityValidation();
                manageclientidentityreq.manageClientIdentityReq.clientIdentity.clientIdentityValidation[0].name = ACCOUNTTRUSTMETHOD;
                manageclientidentityreq.manageClientIdentityReq.clientIdentity.clientIdentityValidation[0].value = AhtValue;
                manageclientidentityreq.manageClientIdentityReq.clientIdentity.clientIdentityValidation[0].action = ACTION_INSERT;


                manageclientidentityreq.standardHeader = headerblock;
                Request.manageClientIdentityRequest = manageclientidentityreq;

                byte[] credBuffer = new UTF8Encoding().GetBytes(ConfigurationManager.AppSettings["DnPUser"].ToString() + ":" + ConfigurationManager.AppSettings["DnPPassword"].ToString());
                HttpRequestMessageProperty httpRequest = new HttpRequestMessageProperty();
                httpRequest.Headers.Add(HTTP_HEADER_AUTHORIZATION, AUTHORIZATION_TYPE_BASIC + Convert.ToBase64String(credBuffer));
                using (ChannelFactory<ClientProfileSyncPortType> factory = new ChannelFactory<ClientProfileSyncPortType>(@"ClientProfile_DnP"))
                {
                    ClientProfileSyncPortType svc = factory.CreateChannel();
                    using (OperationContextScope scope = new OperationContextScope((IContextChannel)svc))
                    {
                        OperationContext.Current.OutgoingMessageProperties.Add(HttpRequestMessageProperty.Name, httpRequest);
                        Req = Request.SerializeSerializable();
                        Response = svc.manageClientIdentity(Request);
                    }
                }
            }
            catch (Exception exp)
            {
                EventMapper.RaiseEvnt(requestOrderRequest, ExecuteFailedResponse, OrderStatusEnum.failed, Req);
                throw new DnpException(exp.Message);
            }
            return Response;
        }
        public static moveClientIdentitiesResponse1 moveClientIdentities(moveClientIdentitiesRequest1 request, OrderRequest requestOrderRequest)
        {
            moveClientIdentitiesResponse1 moveCiResponse = null;
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
                        moveCiResponse = svc.moveClientIdentities(request);
                    }
                }
            }
            catch (Exception excep)
            {
                EventMapper.RaiseEvnt(requestOrderRequest, ExecuteFailedResponse, OrderStatusEnum.failed, request.SerializeObject());
                throw new DnpException(excep.Message);
            }
            return moveCiResponse;
        }

        public linkClientProfilesResponse1 linkClientProfile(string domainName, string btID, string value, ref string reqst, OrderRequest requestOrderRequest, ref E2ETransaction e2eData, bool isEECustomer)
        {
            linkClientProfilesResponse1 response = null;
            linkClientProfilesRequest1 request = new linkClientProfilesRequest1();
            LinkClientProfilesRequest req = new LinkClientProfilesRequest();
            try
            {
                req.primaryClientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                req.primaryClientIdentity.action = ACTION_SEARCH;
                req.primaryClientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                req.primaryClientIdentity.managedIdentifierDomain.value = isEECustomer ? EEONLINEID : BTCOM;
                req.primaryClientIdentity.value = btID;

                req.linkClientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                req.linkClientIdentity.action = ACTION_LINK;
                req.linkClientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                req.linkClientIdentity.managedIdentifierDomain.value = domainName;
                req.linkClientIdentity.value = value;

                req.linkClientIdentity.clientCredential = new BT.SaaS.IspssAdapter.Dnp.ClientCredential[1];
                req.linkClientIdentity.clientCredential[0] = new BT.SaaS.IspssAdapter.Dnp.ClientCredential();
                req.linkClientIdentity.clientCredential[0].credentialValue = "AHTMergeIgnorePassword";
                req.linkClientIdentity.clientCredential[0].clientCredentialStatus = new ClientCredentialStatus();
                req.linkClientIdentity.clientCredential[0].clientCredentialStatus.value = ACTIVE;

                req.action = ACTION_MERGE;

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
                    reqst = req.SerializeObject();

                    LogHelper.LogActivityDetails(bptmTxnId, guid, MakingDnPcall + " to linkClientProfiles", System.DateTime.Now - ActivityStartTime, "AHTLogTrace", guid, request.SerializeObject());

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

                            LogHelper.LogActivityDetails(bptmTxnId, guid, GotResponseFromDnP + " linkClientProfiles", System.DateTime.Now - ActivityStartTime, "AHTLogTrace", guid, response.SerializeObject());

                        }
                    }
                    e2eData.logMessage(GotResponseFromDnP, "Ending... Dnp linkClientProfiles call");
                }
                catch (Exception excp)
                {
                    e2eData.downstreamError(ExceptionOccurred, excp.Message);
                    EventMapper.RaiseEvnt(requestOrderRequest, ExecuteFailedResponse, OrderStatusEnum.failed, reqst);
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

        public manageClientProfileV1Response1 delinkAHTvalidation(ClientServiceInstanceV1[] clientServiceInstances, string BillAccountNumber, OrderRequest requestOrderRequest, ref E2ETransaction e2eData, ref manageClientProfileV1Request1 mcpRequest)
        {
            manageClientProfileV1Response1 response;
            bool isHCSWarrantyExists = false;
            List<ClientServiceInstanceV1> serviceInstanceList = null;
            List<ClientServiceRole> clientServiceRoleList = null;
            List<BT.SaaS.IspssAdapter.Dnp.ClientIdentity> clientIdentityList = new List<BT.SaaS.IspssAdapter.Dnp.ClientIdentity>();
            try
            {
                string[] defaultRoleServices = ConfigurationManager.AppSettings["AHTDefaultRoleDelinkServices"].ToString().Split(',');
                string[] wifiDefaultRoleIdentities = ConfigurationManager.AppSettings["BTWifiRoleIdentities"].ToString().Split(',');

                serviceInstanceList = new List<ClientServiceInstanceV1>();
                ClientServiceInstanceV1 serviceInstance = null;
                ServiceIdentity[] serviceIdentity = null;
                BT.SaaS.IspssAdapter.Dnp.ClientIdentity clientIdentity = null;
                if (clientServiceInstances != null)
                {
                    foreach (ClientServiceInstanceV1 srvcInstance in clientServiceInstances)
                    {
                        clientServiceRoleList = new List<ClientServiceRole>();
                        ClientServiceRole clientServiceRole = null;

                        if (defaultRoleServices.ToList().Contains(srvcInstance.clientServiceInstanceIdentifier.value))
                        {
                            if (srvcInstance.clientServiceRole != null && srvcInstance.clientServiceRole.ToList().Exists(csr => csr.id.ToUpper().Equals(DEFAULT_ROLE)))
                            {
                                foreach (ClientServiceRole role in srvcInstance.clientServiceRole.ToList().Where(csr => csr.id.ToUpper().Equals(DEFAULT_ROLE)))
                                {
                                    if (srvcInstance.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["WifiIdentifier"], StringComparison.OrdinalIgnoreCase))
                                    {
                                        if ((role.clientIdentity != null && role.clientIdentity.Count() > 0 && (wifiDefaultRoleIdentities.ToList().Contains(role.clientIdentity[0].managedIdentifierDomain.value.ToUpper()))))
                                        {
                                            clientServiceRole = new ClientServiceRole();
                                            clientServiceRole.id = role.id;
                                            clientServiceRole.name = role.name;
                                            clientServiceRole.action = ACTION_DELETE;
                                            clientServiceRoleList.Add(clientServiceRole);
                                        }
                                    }
                                    else
                                    {
                                        clientServiceRole = new ClientServiceRole();
                                        clientServiceRole.id = role.id;
                                        clientServiceRole.name = role.name;
                                        clientServiceRole.action = ACTION_DELETE;
                                        clientServiceRoleList.Add(clientServiceRole);
                                    }
                                }
                                serviceIdentity = new ServiceIdentity[1];
                                serviceIdentity[0] = new ServiceIdentity();
                                serviceIdentity[0].domain = BACID_IDENTIFER_NAMEPACE;
                                serviceIdentity[0].value = BillAccountNumber;
                                serviceIdentity[0].action = ACTION_SEARCH;

                                serviceInstance = new ClientServiceInstanceV1();
                                serviceInstance.clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                                serviceInstance.clientServiceInstanceIdentifier.value = srvcInstance.clientServiceInstanceIdentifier.value;
                                serviceInstance.action = ACTION_UPDATE;
                                serviceInstance.serviceIdentity = serviceIdentity;
                                serviceInstance.clientServiceRole = clientServiceRoleList.ToArray();
                                serviceInstanceList.Add(serviceInstance);
                            }

                        }
                        else if (srvcInstance.clientServiceInstanceIdentifier.value.ToUpper().Equals("HOMEAWAY"))
                        {
                            clientServiceRole = new ClientServiceRole();
                            clientServiceRole.id = ADMIN_ROLE;
                            clientServiceRole.action = ACTION_DELETE;
                            clientServiceRoleList.Add(clientServiceRole);

                            serviceInstance = new ClientServiceInstanceV1();
                            serviceInstance.clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                            serviceInstance.clientServiceInstanceIdentifier.value = srvcInstance.clientServiceInstanceIdentifier.value;
                            serviceInstance.action = ACTION_UPDATE;
                            serviceInstance.name = BillAccountNumber;
                            serviceInstance.clientServiceRole = clientServiceRoleList.ToArray();
                            serviceInstanceList.Add(serviceInstance);
                        }

                        if (srvcInstance.clientServiceInstanceIdentifier.value.ToUpper().Equals("CONTRACTANDWARRANTY"))
                        {
                            isHCSWarrantyExists = true;
                        }

                    }
                }
                //BT.SaaS.IspssAdapter.Dnp.ClientIdentity[] clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity[1];
                if (isHCSWarrantyExists)
                {
                    clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                    clientIdentity.value = BillAccountNumber;
                    clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientIdentity.managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;
                    clientIdentity.clientIdentityValidation = new ClientIdentityValidation[2];
                    clientIdentity.clientIdentityValidation[0] = new ClientIdentityValidation();
                    clientIdentity.clientIdentityValidation[0].name = ACCOUNTTRUSTMETHOD;
                    clientIdentity.clientIdentityValidation[0].action = ACTION_DELETE;
                    clientIdentity.clientIdentityValidation[1] = new ClientIdentityValidation();
                    clientIdentity.clientIdentityValidation[1].name = "ACT_TYPE";
                    clientIdentity.clientIdentityValidation[1].action = ACTION_DELETE;
                    clientIdentity.action = ACTION_SEARCH;
                    clientIdentityList.Add(clientIdentity);
                }
                else
                {
                    clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                    clientIdentity.value = BillAccountNumber;
                    clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientIdentity.managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;
                    clientIdentity.clientIdentityValidation = new ClientIdentityValidation[1];
                    clientIdentity.clientIdentityValidation[0] = new ClientIdentityValidation();
                    clientIdentity.clientIdentityValidation[0].name = ACCOUNTTRUSTMETHOD;
                    clientIdentity.clientIdentityValidation[0].action = ACTION_DELETE;
                    clientIdentity.action = ACTION_SEARCH;

                    clientIdentityList.Add(clientIdentity);
                }


                manageClientProfileV1Request1 profileReq = new manageClientProfileV1Request1();
                profileReq.manageClientProfileV1Request = new ManageClientProfileV1Request();

                ManageClientProfileV1Req manageClientProfileV1Req = new ManageClientProfileV1Req();
                manageClientProfileV1Req.clientProfileV1 = new ClientProfileV1();
                manageClientProfileV1Req.clientProfileV1.client = new Client();
                manageClientProfileV1Req.clientProfileV1.client.action = ACTION_UPDATE;
                manageClientProfileV1Req.clientProfileV1.client.clientIdentity = clientIdentityList.ToArray();

                if (serviceInstanceList.Count > 0)
                    manageClientProfileV1Req.clientProfileV1.clientServiceInstanceV1 = serviceInstanceList.ToArray();

                profileReq.manageClientProfileV1Request.manageClientProfileV1Req = manageClientProfileV1Req;

                BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock headerBlock = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
                headerBlock.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
                headerBlock.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
                headerBlock.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();
                profileReq.manageClientProfileV1Request.standardHeader = headerBlock;
                profileReq.manageClientProfileV1Request.standardHeader.e2e = new BT.SaaS.IspssAdapter.Dnp.E2E();
                profileReq.manageClientProfileV1Request.standardHeader.e2e.E2EDATA = e2eData.toString();
                mcpRequest = profileReq;
                e2eData.logMessage(StartedDnPCall, "Started... Dnp manageClientProfileV1 call");
                e2eData.startOutboundCall(ConfigurationManager.AppSettings["destinationDnP"].ToString(), E2ETransaction.vREQ, ACCOUNT_HOLDER_STATUS);

                response = manageClientProfileV1(profileReq, requestOrderRequest);

                if (response != null
                          && response.manageClientProfileV1Response != null
                          && response.manageClientProfileV1Response.standardHeader != null
                          && response.manageClientProfileV1Response.standardHeader.e2e != null
                          && response.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null
                          && !String.IsNullOrEmpty(response.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                {
                    e2eData.logMessage(GotResponseFromDnP, "Ending... Dnp manageClientProfileV1 call");
                    e2eDataPassed = response.manageClientProfileV1Response.standardHeader.e2e.E2EDATA;
                    e2eData.endOutboundCall(e2eDataPassed);
                }
                else
                {
                    e2eData.logMessage(GotResponseFromDnP, "Ending... Dnp manageClientProfileV1 call");
                    e2eData.endOutboundCall(e2eData.toString());
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                clientIdentityList = null;
                serviceInstanceList = null;
                clientServiceRoleList = null;
            }
            return response;
        }

        public moveClientIdentitiesResponse1 moveServiceWithRoles(ClientServiceInstanceV1[] clientServiceInstances, string BillAccountNumber, string BtoneId, OrderRequest requestOrderRequest, ref E2ETransaction e2eData, ref moveClientIdentitiesRequest1 request, bool isEECustomer)
        {
            Dictionary<string, string> identites = new Dictionary<string, string>();
            List<BT.SaaS.IspssAdapter.Dnp.ClientIdentity> MoveClientIdentities = new List<BT.SaaS.IspssAdapter.Dnp.ClientIdentity>();
            BT.SaaS.IspssAdapter.Dnp.ClientIdentity moveCI = null;
            moveClientIdentitiesResponse1 moveCiResponse = null;
            try
            {
                BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock headerBlock = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
                headerBlock.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
                headerBlock.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
                headerBlock.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();

                moveClientIdentitiesRequest1 moveCIRequest1 = new moveClientIdentitiesRequest1();

                MoveClientIdentitiesRequest moveCIRequest = new MoveClientIdentitiesRequest();
                MoveClientIdentitiesReq moveCIReq = new MoveClientIdentitiesReq();
                moveCIReq.isMoveServiceRole = "YES";
                moveCIReq.mciSourceSearchCriteria = new ClientSearchCriteria();
                moveCIReq.mciSourceSearchCriteria.identifierDomainCode = isEECustomer ? EEONLINEID : BTCOM;
                moveCIReq.mciSourceSearchCriteria.identifierValue = BtoneId;

                moveCIReq.mciDestinationSearchCriteria = new ClientSearchCriteria();
                moveCIReq.mciDestinationSearchCriteria.identifierDomainCode = BTCOM;
                moveCIReq.mciDestinationSearchCriteria.identifierValue = "dummy_move_dest_user";

                clientServiceInstances.ToList().ForEach(x =>
                {
                    if (x.clientServiceRole != null)
                    {
                        x.clientServiceRole.ToList().ForEach(y =>
                        {
                            if (y.id.ToUpper().Equals(DEFAULT_ROLE) && y.clientIdentity != null)
                            {
                                y.clientIdentity.ToList().ForEach(z =>
                                {
                                    if (z.managedIdentifierDomain != null && z.managedIdentifierDomain.value != null
                                        && !(ConfigurationManager.AppSettings["AHTDeinkRoleIdentities"].Split(',').Contains(z.managedIdentifierDomain.value)))
                                    {
                                        identites.Add(z.value, z.managedIdentifierDomain.value);
                                    }
                                });
                            }
                        }
                        );
                    }
                });

                // including billaccount number
                identites.Add(BillAccountNumber, BACID_IDENTIFER_NAMEPACE);
                foreach (string iden in identites.Keys)
                {
                    moveCI = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                    moveCI.value = iden;
                    moveCI.managedIdentifierDomain = new ManagedIdentifierDomain();
                    moveCI.managedIdentifierDomain.value = identites[iden];

                    MoveClientIdentities.Add(moveCI);
                }
                moveCIReq.clientIdentity = MoveClientIdentities.ToArray();

                moveCIRequest.moveClientIdentitiesReq = moveCIReq;
                moveCIRequest.standardHeader = headerBlock;
                moveCIRequest.standardHeader.e2e = new BT.SaaS.IspssAdapter.Dnp.E2E();
                moveCIRequest.standardHeader.e2e.E2EDATA = e2eData.toString();
                moveCIRequest1.moveClientIdentitiesRequest = moveCIRequest;

                request = moveCIRequest1;
                e2eData.logMessage(StartedDnPCall, "Started... Dnp moveClientIdentities call");
                e2eData.startOutboundCall(ConfigurationManager.AppSettings["destinationDnP"].ToString(), E2ETransaction.vREQ, ACCOUNT_HOLDER_STATUS);

                moveCiResponse = moveClientIdentities(moveCIRequest1, requestOrderRequest);

                if (moveCiResponse != null
                               && moveCiResponse.moveClientIdentitiesResponse != null
                               && moveCiResponse.moveClientIdentitiesResponse.standardHeader != null
                               && moveCiResponse.moveClientIdentitiesResponse.standardHeader.e2e != null
                               && moveCiResponse.moveClientIdentitiesResponse.standardHeader.e2e.E2EDATA != null
                               && !String.IsNullOrEmpty(moveCiResponse.moveClientIdentitiesResponse.standardHeader.e2e.E2EDATA))
                {
                    e2eData.logMessage(GotResponseFromDnP, "Ending... Dnp moveClientIdentities call");
                    e2eDataPassed = moveCiResponse.moveClientIdentitiesResponse.standardHeader.e2e.E2EDATA;
                    e2eData.endOutboundCall(e2eDataPassed);
                }
                else
                {
                    e2eData.logMessage(GotResponseFromDnP, "Ending... Dnp moveClientIdentities call");
                    e2eData.endOutboundCall(e2eData.toString());
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                identites = null;
                MoveClientIdentities = null;
            }
            return moveCiResponse;
        }

        public manageClientProfileV1Response1 linkRoleRequest(ClientServiceInstanceV1[] clientServiceInstances, string BillAccountNumber, OrderRequest requestOrderRequest, ref E2ETransaction e2eData, ref manageClientProfileV1Request1 mcpRequest)
        {
            manageClientProfileV1Response1 response;
            List<ClientServiceInstanceV1> serviceInstanceList = null;
            List<ClientServiceRole> clientServiceRoleList = null;
            try
            {
                BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock headerBlock = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
                headerBlock.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
                headerBlock.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
                headerBlock.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();

                manageClientProfileV1Request1 profileReq = new manageClientProfileV1Request1();
                profileReq.manageClientProfileV1Request = new ManageClientProfileV1Request();
                profileReq.manageClientProfileV1Request.standardHeader = headerBlock;
                profileReq.manageClientProfileV1Request.standardHeader.e2e = new BT.SaaS.IspssAdapter.Dnp.E2E();
                profileReq.manageClientProfileV1Request.standardHeader.e2e.E2EDATA = e2eData.toString();

                ManageClientProfileV1Req manageClientProfileV1Req = new ManageClientProfileV1Req();
                manageClientProfileV1Req.clientProfileV1 = new ClientProfileV1();
                manageClientProfileV1Req.clientProfileV1.client = new Client();
                manageClientProfileV1Req.clientProfileV1.client.action = ACTION_UPDATE;

                BT.SaaS.IspssAdapter.Dnp.ClientIdentity[] clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity[1];
                clientIdentity[0] = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                clientIdentity[0].value = BillAccountNumber;
                clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                clientIdentity[0].managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;
                clientIdentity[0].action = ACTION_SEARCH;

                serviceInstanceList = new List<ClientServiceInstanceV1>();
                ClientServiceInstanceV1 serviceInstance = null;
                ServiceIdentity[] serviceIdentity = null;
                if (clientServiceInstances != null)
                {
                    foreach (ClientServiceInstanceV1 srvcInstance in clientServiceInstances)
                    {
                        clientServiceRoleList = new List<ClientServiceRole>();
                        ClientServiceRole clientServiceRole = null;

                        if (srvcInstance.clientServiceInstanceIdentifier.value.ToUpper().Equals(ConfigurationManager.AppSettings["CAService"]))
                        {
                            clientServiceRole = new ClientServiceRole();
                            clientServiceRole.id = DEFAULT_ROLE;
                            clientServiceRole.clientServiceRoleStatus = new ClientServiceRoleStatus();
                            clientServiceRole.clientServiceRoleStatus.value = "ACTIVE";
                            clientServiceRole.action = ACTION_INSERT;
                            clientServiceRoleList.Add(clientServiceRole);

                            serviceIdentity = new ServiceIdentity[1];
                            serviceIdentity[0] = new ServiceIdentity();
                            serviceIdentity[0].domain = BACID_IDENTIFER_NAMEPACE;
                            serviceIdentity[0].value = BillAccountNumber;
                            serviceIdentity[0].action = ACTION_SEARCH;

                            serviceInstance = new ClientServiceInstanceV1();
                            serviceInstance.clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                            serviceInstance.clientServiceInstanceIdentifier.value = srvcInstance.clientServiceInstanceIdentifier.value;
                            serviceInstance.action = ACTION_UPDATE;
                            serviceInstance.serviceIdentity = serviceIdentity;

                            serviceInstance.clientServiceRole = clientServiceRoleList.ToArray();
                            serviceInstanceList.Add(serviceInstance);
                        }
                        else if (srvcInstance.clientServiceInstanceIdentifier.value.ToUpper().Equals("HOMEAWAY"))
                        {
                            clientServiceRole = new ClientServiceRole();
                            clientServiceRole.id = ADMIN_ROLE;
                            clientServiceRole.clientServiceRoleStatus = new ClientServiceRoleStatus();
                            clientServiceRole.clientServiceRoleStatus.value = "ACTIVE";
                            clientServiceRole.action = ACTION_INSERT;
                            clientServiceRoleList.Add(clientServiceRole);

                            serviceInstance = new ClientServiceInstanceV1();
                            serviceInstance.name = BillAccountNumber;
                            serviceInstance.clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                            serviceInstance.clientServiceInstanceIdentifier.value = srvcInstance.clientServiceInstanceIdentifier.value;
                            serviceInstance.action = ACTION_UPDATE;

                            serviceInstance.clientServiceRole = clientServiceRoleList.ToArray();
                            serviceInstanceList.Add(serviceInstance);
                        }
                    }
                }
                manageClientProfileV1Req.clientProfileV1.client.clientIdentity = clientIdentity;
                manageClientProfileV1Req.clientProfileV1.clientServiceInstanceV1 = serviceInstanceList.ToArray();

                profileReq.manageClientProfileV1Request.manageClientProfileV1Req = manageClientProfileV1Req;

                mcpRequest = profileReq;
                e2eData.logMessage(StartedDnPCall, "Started... Dnp manageClientProfileV1 call");
                e2eData.startOutboundCall(ConfigurationManager.AppSettings["destinationDnP"].ToString(), E2ETransaction.vREQ, ACCOUNT_HOLDER_STATUS);

                response = manageClientProfileV1(profileReq, requestOrderRequest);

                if (response != null
                          && response.manageClientProfileV1Response != null
                          && response.manageClientProfileV1Response.standardHeader != null
                          && response.manageClientProfileV1Response.standardHeader.e2e != null
                          && response.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null
                          && !String.IsNullOrEmpty(response.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                {
                    e2eData.logMessage(GotResponseFromDnP, "Ending... Dnp manageClientProfileV1 call");
                    e2eDataPassed = response.manageClientProfileV1Response.standardHeader.e2e.E2EDATA;
                    e2eData.endOutboundCall(e2eDataPassed);
                }
                else
                {
                    e2eData.logMessage(GotResponseFromDnP, "Ending... Dnp manageClientProfileV1 call");
                    e2eData.endOutboundCall(e2eData.toString());
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                serviceInstanceList = null;
                clientServiceRoleList = null;
            }

            return response;
        }
        //2nd scenario of AHS Provision
        public void updateBacToSameBTOneID(string BillAccountNumber, string AhtValue, ClientServiceInstanceV1[] serviceInstances, OrderRequest requestOrderRequest, ref E2ETransaction e2eData, ref MSEOOrderNotification notification)
        {
            manageClientProfileResponse1 response = null;
            manageClientProfileRequest1 manageRequest = new manageClientProfileRequest1();
            ManageClientProfileRequest request = new ManageClientProfileRequest();
            List<ClientServiceInstance> clientServiceInstanceList = new List<ClientServiceInstance>();
            try
            {
                request.manageClientProfileReq = new ManageClientProfileReq();
                request.manageClientProfileReq.clientProfile = new ClientProfile();
                request.manageClientProfileReq.clientProfile.client = new Client();
                request.manageClientProfileReq.clientProfile.client.action = ACTION_UPDATE;

                BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock headerblock = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
                headerblock.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
                headerblock.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
                headerblock.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();
                headerblock.serviceState.stateCode = "OK";

                BT.SaaS.IspssAdapter.Dnp.ClientIdentity[] clientIdentity = null;
                clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity[1];

                clientIdentity[0] = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                clientIdentity[0].managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;
                clientIdentity[0].value = BillAccountNumber;
                clientIdentity[0].action = ACTION_SEARCH;

                ClientIdentityValidation[] clientIdentityValidation = new ClientIdentityValidation[1];
                clientIdentityValidation[0] = new ClientIdentityValidation();
                clientIdentityValidation[0].name = ACCOUNTTRUSTMETHOD;
                clientIdentityValidation[0].value = AhtValue;
                clientIdentityValidation[0].action = ACTION_FORCE_INSERT;

                clientIdentity[0].clientIdentityValidation = clientIdentityValidation;

                if (serviceInstances != null && serviceInstances.ToList().Count > 0)
                {
                    foreach (ClientServiceInstanceV1 servcInstance in serviceInstances)
                    {
                        if ((servcInstance.serviceIdentity != null && servcInstance.serviceIdentity.ToList().Exists(si => si.domain.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase) && si.value.Equals(BillAccountNumber, StringComparison.OrdinalIgnoreCase))) || servcInstance.name.Equals(BillAccountNumber, StringComparison.OrdinalIgnoreCase))
                        {
                            if (servcInstance.clientServiceRole != null && servcInstance.clientServiceRole.ToList().Exists(si => si.id.Equals("ADMIN", StringComparison.OrdinalIgnoreCase) && si.clientServiceRoleStatus.value.Equals("UNRESOLVED", StringComparison.OrdinalIgnoreCase)))
                            {
                                request.manageClientProfileReq.clientProfile.clientServiceInstance = new ClientServiceInstance[1];
                                request.manageClientProfileReq.clientProfile.clientServiceInstance[0] = new ClientServiceInstance();
                                request.manageClientProfileReq.clientProfile.clientServiceInstance[0].clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                                request.manageClientProfileReq.clientProfile.clientServiceInstance[0].clientServiceInstanceIdentifier.value = servcInstance.clientServiceInstanceIdentifier.value;
                                request.manageClientProfileReq.clientProfile.clientServiceInstance[0].clientServiceInstanceStatus = new ClientServiceInstanceStatus();
                                request.manageClientProfileReq.clientProfile.clientServiceInstance[0].clientServiceInstanceStatus.value = servcInstance.clientServiceInstanceStatus.value;
                                request.manageClientProfileReq.clientProfile.clientServiceInstance[0].name = servcInstance.name;
                                request.manageClientProfileReq.clientProfile.clientServiceInstance[0].action = ACTION_UPDATE;

                                ClientServiceRole[] clientServiceRole = null;
                                clientServiceRole = new ClientServiceRole[1];
                                clientServiceRole[0] = new ClientServiceRole();
                                clientServiceRole[0].id = ADMIN_ROLE;
                                clientServiceRole[0].clientServiceRoleStatus = new ClientServiceRoleStatus();
                                clientServiceRole[0].clientServiceRoleStatus.value = ACTIVE;

                                if (!servcInstance.clientServiceInstanceIdentifier.value.Equals("HOMEAWAY", StringComparison.OrdinalIgnoreCase))
                                {
                                    clientServiceRole[0].clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity[1];
                                    clientServiceRole[0].clientIdentity[0] = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                                    clientServiceRole[0].clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                                    clientServiceRole[0].clientIdentity[0].managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;
                                    clientServiceRole[0].clientIdentity[0].value = BillAccountNumber;
                                    clientServiceRole[0].clientIdentity[0].action = ACTION_SEARCH;
                                }
                                clientServiceRole[0].action = ACTION_UPDATE;

                                request.manageClientProfileReq.clientProfile.clientServiceInstance[0].clientServiceRole = clientServiceRole;
                                clientServiceInstanceList.Add(request.manageClientProfileReq.clientProfile.clientServiceInstance[0]);
                            }
                        }
                    }
                    request.manageClientProfileReq.clientProfile.clientServiceInstance = clientServiceInstanceList.ToArray();
                }

                request.manageClientProfileReq.clientProfile.client.clientIdentity = clientIdentity;

                e2eData.logMessage(StartedDnPCall, "Started... Dnp manageClientProfile call");
                e2eData.startOutboundCall(ConfigurationManager.AppSettings["destinationDnP"].ToString(), E2ETransaction.vREQ, ACCOUNT_HOLDER_STATUS);

                request.standardHeader = headerblock;
                request.standardHeader.e2e = new BT.SaaS.IspssAdapter.Dnp.E2E();
                request.standardHeader.e2e.E2EDATA = e2eData.toString();

                manageRequest.manageClientProfileRequest = request;

                LogHelper.LogActivityDetails(bptmTxnId, guid, MakingDnPcall + " to updateBacToSameBTOneID", System.DateTime.Now - ActivityStartTime, "AHTLogTrace", guid, manageRequest.SerializeObject());

                response = manageClientProfile(manageRequest, requestOrderRequest);

                LogHelper.LogActivityDetails(bptmTxnId, guid, GotResponseFromDnP + " to updateBacToSameBTOneID", System.DateTime.Now - ActivityStartTime, "AHTLogTrace", guid, response.SerializeObject());

                if (response != null
                    && response.manageClientProfileResponse != null
                    && response.manageClientProfileResponse.standardHeader != null
                    && response.manageClientProfileResponse.standardHeader.serviceState != null
                    && response.manageClientProfileResponse.standardHeader.serviceState.stateCode != null
                    && response.manageClientProfileResponse.standardHeader.serviceState.stateCode == "0")
                {
                    //raiseevent
                    LogHelper.LogActivityDetails(bptmTxnId, guid, "Sucess in updateBacToSameBTOneID", System.DateTime.Now - ActivityStartTime, "AHTLogTrace", guid, manageRequest.SerializeObject());

                    e2eData.logMessage(GotResponseFromDnP, "Ending... Dnp manageClientProfile call");
                    EventMapper.RaiseEvnt(requestOrderRequest, ExecuteSuccessResponse, OrderStatusEnum.success, manageRequest.SerializeObject());
                    notification.sendNotification(true, false, "004", string.Empty, ref e2eData);
                }
                else
                {
                    LogHelper.LogActivityDetails(bptmTxnId, guid, GotResponseFromDnPWithBusinessError + " Sucess in updateBacToSameBTOneID", System.DateTime.Now - ActivityStartTime, "AHTLogTrace", guid, manageRequest.SerializeObject());

                    e2eData.logMessage(GotResponseFromDnPWithBusinessError, "Ending... Dnp manageClientProfile call");
                    //raiseevent
                    EventMapper.RaiseEvnt(requestOrderRequest, ExecuteFailedResponse, OrderStatusEnum.failed, manageRequest.SerializeObject());
                    InboundQueueDAL.UpdateQueueRawXML("777", response.manageClientProfileResponse.manageClientProfileRes.messages[0].description, requestOrderRequest.Order.OrderIdentifier.Value);
                    notification.sendNotification(false, false, "777", response.manageClientProfileResponse.manageClientProfileRes.messages[0].description, ref e2eData);
                }
                if (response != null
                    && response.manageClientProfileResponse != null
                    && response.manageClientProfileResponse.standardHeader != null
                    && response.manageClientProfileResponse.standardHeader.e2e != null
                    && response.manageClientProfileResponse.standardHeader.e2e.E2EDATA != null
                    && !String.IsNullOrEmpty(response.manageClientProfileResponse.standardHeader.e2e.E2EDATA))
                {
                    e2eDataPassed = response.manageClientProfileResponse.standardHeader.e2e.E2EDATA;
                    e2eData.endOutboundCall(e2eDataPassed);
                }
                else
                {
                    e2eData.endOutboundCall(e2eData.toString());
                }
            }
            catch (DnpException DnPExcp)
            {
                throw DnPExcp;
            }
            catch (Exception Excp)
            {
                throw Excp;
            }
            finally
            {
                response = null;
                manageRequest = null;
                clientServiceInstanceList = null;
            }
        }
        //3rd and 4th scenario of AHS Provision
        public void linkUnlinkBacFromBtOneID(string BillAccountNumber, string BtoneId, string AhtValue, ClientServiceInstanceV1[] serviceInstances, getClientProfileV1Response1 gcp, OrderRequest requestOrderRequest, ref E2ETransaction e2eData, ref MSEOOrderNotification notification, bool isEECustomer)
        {
            string status = string.Empty;

            List<ClientProfileV1> clientProfileList = null;
            List<BT.SaaS.IspssAdapter.Dnp.ClientIdentity> clientIdentityListDeletion = null;
            List<BT.SaaS.IspssAdapter.Dnp.ClientIdentity> clientIdentityListInsertion = null;
            List<ClientServiceInstanceV1> CSIListForRolesDeletion = null;
            List<ClientServiceInstanceV1> CSIListForRolesInsertion = null;
            List<ClientServiceRole> clientServiceRoleDeletionList = null;
            List<ClientServiceRole> clientServiceRoleInsertionList = null;
            List<BT.SaaS.IspssAdapter.Dnp.ClientIdentity> roleIdentity = null;
            List<ClientIdentityValidation> clientIdentityValidationList = null;

            //----------Murali----------
            List<InviteRoleParameters> InviteParameterslist = new List<InviteRoleParameters>();
            InviteRoleParameters Parameters = new InviteRoleParameters();
            string url = "GetinviterolesbyUser";

            ClientServiceInstanceV1[] gsiResponse = null;
            List<InviteRoleParameters> delegateRoleParameterlist = new List<InviteRoleParameters>();
            InviteRoleParameters delegateRoleParameters = new InviteRoleParameters();
            gsiResponse = DnpWrapper.getServiceInstanceV1(BillAccountNumber, BACID_IDENTIFER_NAMEPACE, string.Empty);
            //-------------
            bool isHCSWarranty = false;
            string acttype = string.Empty;

            ClientIdentityValidation validation = null;
            ClientServiceInstanceV1 csi = null;
            ClientServiceRole clientServiceRole = null;
            BT.SaaS.IspssAdapter.Dnp.ClientIdentity clientIdentity = null;

            manageBatchProfilesV1Request1 batchProfileRequest = null;
            manageBatchProfilesV1Response1 batchResponse = null;
            batchProfileRequest = new manageBatchProfilesV1Request1();
            batchProfileRequest.manageBatchProfilesV1Request = new ManageBatchProfilesV1Request();

            try
            {
                BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock standardHeader = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
                standardHeader.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
                standardHeader.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
                standardHeader.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();
                batchProfileRequest.manageBatchProfilesV1Request.standardHeader = standardHeader;

                ClientProfileV1 clientProfile = null;
                clientProfileList = new List<ClientProfileV1>();
                //no sevices
                if (serviceInstances == null)
                {
                    clientProfile = new ClientProfileV1();
                    clientProfile.client = new Client();
                    clientProfile.client.action = ACTION_UPDATE;

                    clientIdentityListDeletion = new List<BT.SaaS.IspssAdapter.Dnp.ClientIdentity>();

                    clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                    clientIdentity.value = BillAccountNumber;
                    clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientIdentity.managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;
                    clientIdentity.action = ACTION_SEARCH;
                    clientIdentityListDeletion.Add(clientIdentity);

                    clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                    clientIdentity.value = BillAccountNumber;
                    clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientIdentity.managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;
                    clientIdentity.action = ACTION_DELETE;
                    clientIdentityListDeletion.Add(clientIdentity);

                    clientProfile.client.clientIdentity = clientIdentityListDeletion.ToArray();
                    clientProfileList.Add(clientProfile);

                    clientProfile = new ClientProfileV1();
                    clientProfile.client = new Client();
                    clientProfile.client.action = ACTION_UPDATE;

                    clientIdentityListInsertion = new List<BT.SaaS.IspssAdapter.Dnp.ClientIdentity>();

                    clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                    clientIdentity.value = BtoneId;
                    clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientIdentity.managedIdentifierDomain.value = isEECustomer ? EEONLINEID : BTCOM;
                    clientIdentity.action = ACTION_SEARCH;
                    clientIdentityListInsertion.Add(clientIdentity);

                    clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                    clientIdentity.value = BillAccountNumber;
                    clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientIdentity.managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;
                    clientIdentity.clientIdentityStatus = new ClientIdentityStatus();
                    clientIdentity.clientIdentityStatus.value = ACTIVE;

                    clientIdentity.clientIdentityValidation = new ClientIdentityValidation[1];
                    clientIdentity.clientIdentityValidation[0] = new ClientIdentityValidation();
                    clientIdentity.clientIdentityValidation[0].name = ACCOUNTTRUSTMETHOD;
                    clientIdentity.clientIdentityValidation[0].value = AhtValue;
                    clientIdentity.clientIdentityValidation[0].action = ACTION_INSERT;
                    clientIdentity.action = ACTION_INSERT;
                    clientIdentityListInsertion.Add(clientIdentity);

                    clientProfile.client.clientIdentity = clientIdentityListInsertion.ToArray();
                    clientProfileList.Add(clientProfile);
                }

                else if (serviceInstances != null && serviceInstances.ToList().Count > 0)
                {
                    clientIdentityListDeletion = new List<BT.SaaS.IspssAdapter.Dnp.ClientIdentity>();
                    clientIdentityListInsertion = new List<BT.SaaS.IspssAdapter.Dnp.ClientIdentity>();
                    CSIListForRolesDeletion = new List<ClientServiceInstanceV1>();
                    CSIListForRolesInsertion = new List<ClientServiceInstanceV1>();

                    for (int i = 0; i < serviceInstances.Length; i++)
                    {
                        clientServiceRoleDeletionList = new List<ClientServiceRole>();
                        clientServiceRoleInsertionList = new List<ClientServiceRole>();

                        status = serviceInstances[i].clientServiceInstanceStatus.value.ToUpper();

                        if ((serviceInstances[i].serviceIdentity != null && serviceInstances[i].serviceIdentity.ToList().Exists(si => si.domain.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase) && si.value.Equals(BillAccountNumber, StringComparison.OrdinalIgnoreCase))) || (serviceInstances[i].name != null && serviceInstances[i].name.Equals(BillAccountNumber, StringComparison.OrdinalIgnoreCase)))
                        {
                            #region actionOnAdminRole
                            if (serviceInstances[i].clientServiceRole != null && serviceInstances[i].clientServiceRole.ToList().Exists(csr => csr.id.Equals("admin", StringComparison.OrdinalIgnoreCase)))
                            {
                                string adminRoleStatus = serviceInstances[i].clientServiceRole.ToList().Where(csr => csr.id.Equals("admin", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().clientServiceRoleStatus.value;
                                if (serviceInstances[i].clientServiceInstanceIdentifier.value.Equals("HOMEAWAY", StringComparison.OrdinalIgnoreCase))
                                {
                                    clientServiceRole = new ClientServiceRole();
                                    clientServiceRole.id = ADMIN_ROLE;
                                    clientServiceRole.action = ACTION_DELETE;
                                    clientServiceRoleDeletionList.Add(clientServiceRole);

                                    clientServiceRole = new ClientServiceRole();
                                    clientServiceRole.id = ADMIN_ROLE;
                                    clientServiceRole.clientServiceRoleStatus = new ClientServiceRoleStatus();
                                    clientServiceRole.clientServiceRoleStatus.value = adminRoleStatus == "UNRESOLVED" ? ACTIVE : adminRoleStatus;
                                    clientServiceRole.action = ACTION_INSERT;
                                    clientServiceRoleInsertionList.Add(clientServiceRole);
                                }
                                else
                                {
                                    clientServiceRole = new ClientServiceRole();
                                    clientServiceRole.id = ADMIN_ROLE;
                                    clientServiceRole.clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity[1];
                                    clientServiceRole.clientIdentity[0] = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                                    clientServiceRole.clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                                    clientServiceRole.clientIdentity[0].managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;
                                    clientServiceRole.clientIdentity[0].value = BillAccountNumber;
                                    clientServiceRole.clientIdentity[0].action = ACTION_SEARCH;
                                    clientServiceRole.action = ACTION_DELETE;
                                    clientServiceRoleDeletionList.Add(clientServiceRole);

                                    clientServiceRole = new ClientServiceRole();
                                    clientServiceRole.id = ADMIN_ROLE;
                                    clientServiceRole.clientServiceRoleStatus = new ClientServiceRoleStatus();
                                    clientServiceRole.clientServiceRoleStatus.value = adminRoleStatus == "UNRESOLVED" ? ACTIVE : adminRoleStatus;
                                    clientServiceRole.clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity[1];
                                    clientServiceRole.clientIdentity[0] = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                                    clientServiceRole.clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                                    clientServiceRole.clientIdentity[0].managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;
                                    clientServiceRole.clientIdentity[0].value = BillAccountNumber;
                                    clientServiceRole.clientIdentity[0].clientIdentityStatus = new ClientIdentityStatus();
                                    clientServiceRole.clientIdentity[0].clientIdentityStatus.value = ACTIVE;
                                    clientServiceRole.clientIdentity[0].action = ACTION_INSERT;
                                    clientServiceRole.action = ACTION_INSERT;
                                    clientServiceRoleInsertionList.Add(clientServiceRole);
                                }
                            }
                            #endregion

                            #region HOMEAWAY

                            if (serviceInstances[i].clientServiceInstanceIdentifier.value.Equals("HOMEAWAY", StringComparison.OrdinalIgnoreCase)
                                && serviceInstances[i].clientServiceRole != null)
                            {
                                foreach (ClientServiceRole csr in serviceInstances[i].clientServiceRole.ToList())
                                {
                                    if (csr != null && csr.clientIdentity != null && csr.id.Equals("default", StringComparison.OrdinalIgnoreCase))
                                    {
                                        // ICE-4089 clint identities and default role should not move.

                                        //clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                                        //clientIdentity.value = csr.clientIdentity.FirstOrDefault().value;
                                        //clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                                        //clientIdentity.managedIdentifierDomain.value = csr.clientIdentity.FirstOrDefault().managedIdentifierDomain.value;
                                        //clientIdentity.action = ACTION_DELETE;
                                        //clientIdentityListDeletion.Add(clientIdentity);

                                        //clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                                        //clientIdentity.value = csr.clientIdentity.FirstOrDefault().value;
                                        //clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                                        //clientIdentity.managedIdentifierDomain.value = csr.clientIdentity.FirstOrDefault().managedIdentifierDomain.value;
                                        //clientIdentity.action = ACTION_INSERT;
                                        //clientIdentity.clientIdentityStatus = new ClientIdentityStatus();
                                        //clientIdentity.clientIdentityStatus.value = ACTIVE;
                                        //clientIdentityListInsertion.Add(clientIdentity);

                                        //clientServiceRole = new ClientServiceRole();
                                        //clientServiceRole.id = DEFAULT_ROLE;
                                        //clientServiceRole.clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity[1];
                                        //clientServiceRole.clientIdentity[0] = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                                        //clientServiceRole.clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                                        //clientServiceRole.clientIdentity[0].managedIdentifierDomain.value = csr.clientIdentity.FirstOrDefault().managedIdentifierDomain.value;
                                        //clientServiceRole.clientIdentity[0].value = csr.clientIdentity.FirstOrDefault().value;
                                        //clientServiceRole.clientIdentity[0].action = ACTION_SEARCH;
                                        //clientServiceRole.action = ACTION_DELETE;
                                        //clientServiceRoleDeletionList.Add(clientServiceRole);

                                        //clientServiceRole = new ClientServiceRole();
                                        //clientServiceRole.id = DEFAULT_ROLE;
                                        //clientServiceRole.clientServiceRoleStatus = new ClientServiceRoleStatus();
                                        //clientServiceRole.clientServiceRoleStatus.value = csr.clientServiceRoleStatus.value;
                                        //clientServiceRole.clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity[1];
                                        //clientServiceRole.clientIdentity[0] = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                                        //clientServiceRole.clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                                        //clientServiceRole.clientIdentity[0].managedIdentifierDomain.value = csr.clientIdentity.FirstOrDefault().managedIdentifierDomain.value; ;
                                        //clientServiceRole.clientIdentity[0].value = csr.clientIdentity.FirstOrDefault().value;
                                        //clientServiceRole.clientIdentity[0].action = ACTION_INSERT;
                                        //clientServiceRole.action = ACTION_INSERT;
                                        //clientServiceRoleInsertionList.Add(clientServiceRole);
                                    }
                                }
                            }
                            #endregion

                            #region CONTENTFILTERING
                            if (serviceInstances[i].clientServiceInstanceIdentifier.value.Equals("CONTENTFILTERING:DEFAULT", StringComparison.OrdinalIgnoreCase)
                                && serviceInstances[i].clientServiceRole != null)
                            {
                                foreach (ClientServiceRole csr in serviceInstances[i].clientServiceRole.ToList())
                                {
                                    if (csr != null && csr.id.Equals("default", StringComparison.OrdinalIgnoreCase))
                                    {
                                        clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                                        clientIdentity.value = csr.clientIdentity.FirstOrDefault().value;
                                        clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                                        clientIdentity.managedIdentifierDomain.value = csr.clientIdentity.FirstOrDefault().managedIdentifierDomain.value;
                                        clientIdentity.action = ACTION_DELETE;
                                        clientIdentityListDeletion.Add(clientIdentity);

                                        clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                                        clientIdentity.value = csr.clientIdentity.FirstOrDefault().value;
                                        clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                                        clientIdentity.managedIdentifierDomain.value = csr.clientIdentity.FirstOrDefault().managedIdentifierDomain.value;
                                        clientIdentity.action = ACTION_INSERT;
                                        clientIdentity.clientIdentityStatus = new ClientIdentityStatus();
                                        clientIdentity.clientIdentityStatus.value = ACTIVE;
                                        clientIdentityListInsertion.Add(clientIdentity);

                                        clientServiceRole = new ClientServiceRole();
                                        clientServiceRole.id = DEFAULT_ROLE;
                                        clientServiceRole.clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity[1];
                                        clientServiceRole.clientIdentity[0] = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                                        clientServiceRole.clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                                        clientServiceRole.clientIdentity[0].managedIdentifierDomain.value = csr.clientIdentity.FirstOrDefault().managedIdentifierDomain.value;
                                        clientServiceRole.clientIdentity[0].value = csr.clientIdentity.FirstOrDefault().value;
                                        clientServiceRole.clientIdentity[0].action = ACTION_SEARCH;
                                        clientServiceRole.action = ACTION_DELETE;
                                        clientServiceRoleDeletionList.Add(clientServiceRole);

                                        clientServiceRole = new ClientServiceRole();
                                        clientServiceRole.id = DEFAULT_ROLE;
                                        clientServiceRole.clientServiceRoleStatus = new ClientServiceRoleStatus();
                                        clientServiceRole.clientServiceRoleStatus.value = csr.clientServiceRoleStatus.value;
                                        clientServiceRole.clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity[1];
                                        clientServiceRole.clientIdentity[0] = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                                        clientServiceRole.clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                                        clientServiceRole.clientIdentity[0].managedIdentifierDomain.value = csr.clientIdentity.FirstOrDefault().managedIdentifierDomain.value; ;
                                        clientServiceRole.clientIdentity[0].value = csr.clientIdentity.FirstOrDefault().value;
                                        clientServiceRole.clientIdentity[0].action = ACTION_INSERT;
                                        clientServiceRole.action = ACTION_INSERT;
                                        clientServiceRoleInsertionList.Add(clientServiceRole);
                                    }
                                }
                            }
                            #endregion

                            #region SPRING_GSM
                            if (serviceInstances[i].clientServiceInstanceIdentifier.value.Equals("SPRING_GSM", StringComparison.OrdinalIgnoreCase)
                                && serviceInstances[i].clientServiceRole != null)
                            {
                                foreach (ClientServiceRole csr in serviceInstances[i].clientServiceRole.ToList())
                                {
                                    if (csr != null && csr.clientIdentity != null && csr.id.Equals("default", StringComparison.OrdinalIgnoreCase))
                                    {
                                        clientServiceRole = new ClientServiceRole();
                                        clientServiceRole.id = DEFAULT_ROLE;
                                        clientServiceRole.clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity[1];
                                        clientServiceRole.clientIdentity[0] = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                                        clientServiceRole.clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                                        clientServiceRole.clientIdentity[0].managedIdentifierDomain.value = csr.clientIdentity.FirstOrDefault().managedIdentifierDomain.value;
                                        clientServiceRole.clientIdentity[0].value = csr.clientIdentity.FirstOrDefault().value;
                                        clientServiceRole.clientIdentity[0].action = ACTION_SEARCH;
                                        clientServiceRole.action = ACTION_DELETE;
                                        clientServiceRoleDeletionList.Add(clientServiceRole);

                                        clientServiceRole = new ClientServiceRole();
                                        clientServiceRole.id = DEFAULT_ROLE;
                                        clientServiceRole.clientServiceRoleStatus = new ClientServiceRoleStatus();
                                        clientServiceRole.clientServiceRoleStatus.value = csr.clientServiceRoleStatus.value;

                                        roleIdentity = new List<BT.SaaS.IspssAdapter.Dnp.ClientIdentity>();

                                        foreach (BT.SaaS.IspssAdapter.Dnp.ClientIdentity cid in csr.clientIdentity)
                                        {
                                            if (cid.managedIdentifierDomain.value == "MOBILE_MSISDN")
                                            {
                                                InviteParameterslist = GetInviteParameters(url, BillAccountNumber, cid.value, InviteParameterslist);
                                            }

                                            if (cid.managedIdentifierDomain.value == "RVSID")
                                            {
                                                delegateRoleParameterlist = GetDelegateroleParametersList(BillAccountNumber, gsiResponse, cid.value, delegateRoleParameterlist);
                                            }
                                            clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                                            clientIdentity.value = cid.value;
                                            clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                                            clientIdentity.managedIdentifierDomain.value = cid.managedIdentifierDomain.value;
                                            clientIdentity.action = ACTION_DELETE;
                                            clientIdentityListDeletion.Add(clientIdentity);

                                            clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                                            clientIdentity.value = cid.value;
                                            clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                                            clientIdentity.managedIdentifierDomain.value = cid.managedIdentifierDomain.value;
                                            clientIdentity.clientIdentityStatus = new ClientIdentityStatus();
                                            clientIdentity.clientIdentityStatus.value = ACTIVE;
                                            clientIdentity.action = ACTION_INSERT;

                                            roleIdentity.Add(clientIdentity);

                                            clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                                            clientIdentity.value = cid.value;
                                            clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                                            clientIdentity.managedIdentifierDomain.value = cid.managedIdentifierDomain.value;
                                            clientIdentity.clientIdentityStatus = new ClientIdentityStatus();
                                            clientIdentity.clientIdentityStatus.value = ACTIVE;
                                            clientIdentity.action = ACTION_INSERT;

                                            clientIdentityValidationList = new List<ClientIdentityValidation>();

                                            if (gcp.getClientProfileV1Response.getClientProfileV1Res.clientProfileV1.client.clientIdentity.ToList().Where(ci => ci.value.Equals(cid.value) && ci.managedIdentifierDomain.value.Equals(cid.managedIdentifierDomain.value)).FirstOrDefault().clientIdentityValidation != null)
                                            {
                                                foreach (ClientIdentityValidation validation1 in gcp.getClientProfileV1Response.getClientProfileV1Res.clientProfileV1.client.clientIdentity.ToList().Where(ci => ci.value.Equals(cid.value) && ci.managedIdentifierDomain.value.Equals(cid.managedIdentifierDomain.value)).FirstOrDefault().clientIdentityValidation)
                                                {
                                                    validation = new ClientIdentityValidation();
                                                    validation.action = ACTION_INSERT;
                                                    validation.name = validation1.name;
                                                    validation.value = validation1.value;
                                                    clientIdentityValidationList.Add(validation);
                                                }
                                            }
                                            clientIdentity.clientIdentityValidation = clientIdentityValidationList.ToArray();
                                            clientIdentityListInsertion.Add(clientIdentity);
                                        }
                                        clientServiceRole.clientIdentity = roleIdentity.ToArray();
                                        clientServiceRole.action = ACTION_INSERT;
                                        clientServiceRoleInsertionList.Add(clientServiceRole);
                                    }
                                }
                            }
                            #endregion

                            #region CHO Service
                            if (serviceInstances[i].clientServiceInstanceIdentifier.value.Equals("BT_CONSUMER_BROADBAND", StringComparison.OrdinalIgnoreCase)
                                && serviceInstances[i].clientServiceRole != null)
                            {
                                foreach (ClientServiceRole csr in serviceInstances[i].clientServiceRole.ToList())
                                {
                                    if (csr != null && csr.clientIdentity != null && csr.id.Equals("default", StringComparison.OrdinalIgnoreCase))
                                    {
                                        clientServiceRole = new ClientServiceRole();
                                        clientServiceRole.id = DEFAULT_ROLE;
                                        clientServiceRole.clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity[1];
                                        clientServiceRole.clientIdentity[0] = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                                        clientServiceRole.clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                                        clientServiceRole.clientIdentity[0].managedIdentifierDomain.value = csr.clientIdentity.FirstOrDefault().managedIdentifierDomain.value;
                                        clientServiceRole.clientIdentity[0].value = csr.clientIdentity.FirstOrDefault().value;
                                        clientServiceRole.clientIdentity[0].action = ACTION_SEARCH;
                                        clientServiceRole.action = ACTION_DELETE;
                                        clientServiceRoleDeletionList.Add(clientServiceRole);

                                        clientServiceRole = new ClientServiceRole();
                                        clientServiceRole.id = DEFAULT_ROLE;
                                        clientServiceRole.clientServiceRoleStatus = new ClientServiceRoleStatus();
                                        clientServiceRole.clientServiceRoleStatus.value = csr.clientServiceRoleStatus.value;

                                        roleIdentity = new List<BT.SaaS.IspssAdapter.Dnp.ClientIdentity>();

                                        foreach (BT.SaaS.IspssAdapter.Dnp.ClientIdentity cid in csr.clientIdentity)
                                        {
                                            clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                                            clientIdentity.value = cid.value;
                                            clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                                            clientIdentity.managedIdentifierDomain.value = cid.managedIdentifierDomain.value;
                                            clientIdentity.action = ACTION_DELETE;
                                            clientIdentityListDeletion.Add(clientIdentity);

                                            clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                                            clientIdentity.value = cid.value;
                                            clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                                            clientIdentity.managedIdentifierDomain.value = cid.managedIdentifierDomain.value;
                                            clientIdentity.clientIdentityStatus = new ClientIdentityStatus();
                                            clientIdentity.clientIdentityStatus.value = ACTIVE;
                                            clientIdentity.action = ACTION_INSERT;

                                            roleIdentity.Add(clientIdentity);

                                            clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                                            clientIdentity.value = cid.value;
                                            clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                                            clientIdentity.managedIdentifierDomain.value = cid.managedIdentifierDomain.value;
                                            clientIdentity.clientIdentityStatus = new ClientIdentityStatus();
                                            clientIdentity.clientIdentityStatus.value = ACTIVE;
                                            clientIdentity.action = ACTION_INSERT;

                                            clientIdentityValidationList = new List<ClientIdentityValidation>();

                                            if (gcp.getClientProfileV1Response.getClientProfileV1Res.clientProfileV1.client.clientIdentity.ToList().Where(ci => ci.value.Equals(cid.value) && ci.managedIdentifierDomain.value.Equals(cid.managedIdentifierDomain.value)).FirstOrDefault().clientIdentityValidation != null)
                                            {
                                                foreach (ClientIdentityValidation validation1 in gcp.getClientProfileV1Response.getClientProfileV1Res.clientProfileV1.client.clientIdentity.ToList().Where(ci => ci.value.Equals(cid.value) && ci.managedIdentifierDomain.value.Equals(cid.managedIdentifierDomain.value)).FirstOrDefault().clientIdentityValidation)
                                                {
                                                    validation = new ClientIdentityValidation();
                                                    validation.action = ACTION_INSERT;
                                                    validation.name = validation1.name;
                                                    validation.value = validation1.value;
                                                    clientIdentityValidationList.Add(validation);
                                                }
                                            }
                                            clientIdentity.clientIdentityValidation = clientIdentityValidationList.ToArray();
                                            clientIdentityListInsertion.Add(clientIdentity);
                                        }
                                        clientServiceRole.clientIdentity = roleIdentity.ToArray();
                                        clientServiceRole.action = ACTION_INSERT;
                                        clientServiceRoleInsertionList.Add(clientServiceRole);
                                    }
                                }
                            }
                            #endregion

                            #region CONTENTANYWHERE
                            if (serviceInstances[i].clientServiceInstanceIdentifier.value.Equals(@"CONTENTANYWHERE:DEFAULT", StringComparison.OrdinalIgnoreCase))
                            {
                                if (serviceInstances[i].clientServiceRole != null && serviceInstances[i].clientServiceRole.ToList().Exists(csr => csr.id.Equals("DEFAULT", StringComparison.OrdinalIgnoreCase)))
                                {
                                    clientServiceRole = new ClientServiceRole();
                                    clientServiceRole.id = DEFAULT_ROLE;
                                    clientServiceRole.action = ACTION_DELETE;
                                    clientServiceRoleDeletionList.Add(clientServiceRole);
                                    clientServiceRole = new ClientServiceRole();
                                    clientServiceRole.id = DEFAULT_ROLE;
                                    clientServiceRole.clientServiceRoleStatus = new ClientServiceRoleStatus();
                                    clientServiceRole.clientServiceRoleStatus.value = status == ACTIVE ? ACTIVE : INACTIVE;
                                    clientServiceRole.action = ACTION_INSERT;
                                    clientServiceRoleInsertionList.Add(clientServiceRole);
                                }
                            }
                            #endregion

                            #region actionOnBTOneIDDefaultRole
                            if (serviceInstances[i].clientServiceRole != null && serviceInstances[i].clientServiceRole.ToList().Exists(csr => csr.clientIdentity != null && csr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)))
                                || serviceInstances[i].clientServiceRole != null && serviceInstances[i].clientServiceRole.ToList().Exists(csr => csr.clientIdentity != null && csr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("EEONLINEID", StringComparison.OrdinalIgnoreCase))))
                            {
                                clientServiceRole = new ClientServiceRole();
                                clientServiceRole.id = DEFAULT_ROLE;
                                clientServiceRole.clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity[1];
                                clientServiceRole.clientIdentity[0] = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                                clientServiceRole.clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                                clientServiceRole.clientIdentity[0].managedIdentifierDomain.value = BTCOM;
                                if (isEECustomer)
                                    clientServiceRole.clientIdentity[0].value = serviceInstances[i].clientServiceRole.ToList().Where(csr => csr.clientIdentity != null && csr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("EEONLINEID", StringComparison.OrdinalIgnoreCase))).FirstOrDefault().clientIdentity.ToList().Where(clintId => clintId.managedIdentifierDomain.value.Equals("EEONLINEID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                else
                                    clientServiceRole.clientIdentity[0].value = serviceInstances[i].clientServiceRole.ToList().Where(csr => csr.clientIdentity != null && csr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase))).FirstOrDefault().clientIdentity.ToList().Where(clintId => clintId.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                clientServiceRole.clientIdentity[0].action = ACTION_SEARCH;
                                clientServiceRole.action = ACTION_DELETE;
                                clientServiceRoleDeletionList.Add(clientServiceRole);

                                clientServiceRole = new ClientServiceRole();
                                clientServiceRole.id = DEFAULT_ROLE;
                                clientServiceRole.clientServiceRoleStatus = new ClientServiceRoleStatus();
                                if (isEECustomer)
                                    clientServiceRole.clientServiceRoleStatus.value = serviceInstances[i].clientServiceRole.ToList().Where(csr => csr.clientIdentity != null && csr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("EEONLINEID", StringComparison.OrdinalIgnoreCase))).FirstOrDefault().clientServiceRoleStatus.value;
                                else
                                    clientServiceRole.clientServiceRoleStatus.value = serviceInstances[i].clientServiceRole.ToList().Where(csr => csr.clientIdentity != null && csr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase))).FirstOrDefault().clientServiceRoleStatus.value;
                                clientServiceRole.clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity[1];
                                clientServiceRole.clientIdentity[0] = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                                clientServiceRole.clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                                clientServiceRole.clientIdentity[0].managedIdentifierDomain.value = BTCOM;
                                clientServiceRole.clientIdentity[0].value = BtoneId;
                                clientServiceRole.clientIdentity[0].action = ACTION_INSERT;
                                clientServiceRole.action = ACTION_INSERT;
                                clientServiceRoleInsertionList.Add(clientServiceRole);
                            }
                            else
                            {
                                if (ConfigurationManager.AppSettings["serviceslist"].ToString().Split(',').ToList().Contains(serviceInstances[i].clientServiceInstanceIdentifier.value))
                                {
                                    clientServiceRole = new ClientServiceRole();
                                    clientServiceRole.id = DEFAULT_ROLE;
                                    clientServiceRole.clientServiceRoleStatus = new ClientServiceRoleStatus();
                                    clientServiceRole.clientServiceRoleStatus.value = status == CEASED ? INACTIVE : ACTIVE;
                                    clientServiceRole.clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity[1];
                                    clientServiceRole.clientIdentity[0] = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                                    clientServiceRole.clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                                    clientServiceRole.clientIdentity[0].managedIdentifierDomain.value = isEECustomer ? EEONLINEID : BTCOM;
                                    clientServiceRole.clientIdentity[0].value = BtoneId;
                                    clientServiceRole.clientIdentity[0].action = ACTION_INSERT;
                                    clientServiceRole.action = ACTION_INSERT;
                                    clientServiceRoleInsertionList.Add(clientServiceRole);
                                }
                            }
                            #endregion

                            #region BTWIFI
                            if (serviceInstances[i].clientServiceInstanceIdentifier.value.Equals("BTWIFI:DEFAULT", StringComparison.OrdinalIgnoreCase))
                            {
                                if (serviceInstances[i].clientServiceRole != null && serviceInstances[i].clientServiceRole.ToList().Exists(cs => cs.id.Equals("default", StringComparison.OrdinalIgnoreCase)))
                                {
                                    foreach (ClientServiceRole csr1 in serviceInstances[i].clientServiceRole.ToList())
                                    {
                                        //Considering Roles Other than BTCOM Deafult Role as it is taken care of in above code and BTIEMAILID Default role because of Copy synch Process.
                                        if (csr1.clientIdentity != null && csr1.id.Equals("default", StringComparison.OrdinalIgnoreCase) && !(csr1.clientIdentity.ToList().Exists(cil => (cil.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase) || cil.managedIdentifierDomain.value.Equals("EEONLINEID", StringComparison.OrdinalIgnoreCase)))
                                            && !(csr1.clientIdentity.ToList().Exists(ci1 => ci1.managedIdentifierDomain.value.Equals("BTIEMAILID", StringComparison.OrdinalIgnoreCase)))))
                                        {
                                            clientServiceRole = new ClientServiceRole();
                                            clientServiceRole.id = DEFAULT_ROLE;
                                            clientServiceRole.clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity[1];
                                            clientServiceRole.clientIdentity[0] = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                                            clientServiceRole.clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                                            clientServiceRole.clientIdentity[0].managedIdentifierDomain.value = csr1.clientIdentity.FirstOrDefault().managedIdentifierDomain.value;
                                            clientServiceRole.clientIdentity[0].value = csr1.clientIdentity.FirstOrDefault().value;
                                            clientServiceRole.clientIdentity[0].action = ACTION_SEARCH;
                                            clientServiceRole.action = ACTION_DELETE;
                                            clientServiceRoleDeletionList.Add(clientServiceRole);
                                        }
                                    }
                                }
                            }
                            #endregion

                            //pavan
                            #region
                            if (serviceInstances[i].clientServiceInstanceIdentifier.value.Equals("IP_VOICE_SERVICE", StringComparison.OrdinalIgnoreCase)
                                && serviceInstances[i].clientServiceRole != null)
                            {
                                foreach (ClientServiceRole csr in serviceInstances[i].clientServiceRole.ToList())
                                {
                                    if (csr != null && csr.clientIdentity != null && csr.id.Equals("default", StringComparison.OrdinalIgnoreCase))
                                    {
                                        clientServiceRole = new ClientServiceRole();
                                        clientServiceRole.id = DEFAULT_ROLE;
                                        clientServiceRole.clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity[1];
                                        clientServiceRole.clientIdentity[0] = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                                        clientServiceRole.clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                                        clientServiceRole.clientIdentity[0].managedIdentifierDomain.value = csr.clientIdentity.FirstOrDefault().managedIdentifierDomain.value;
                                        clientServiceRole.clientIdentity[0].value = csr.clientIdentity.FirstOrDefault().value;
                                        clientServiceRole.clientIdentity[0].action = ACTION_SEARCH;
                                        clientServiceRole.action = ACTION_DELETE;
                                        clientServiceRoleDeletionList.Add(clientServiceRole);

                                        clientServiceRole = new ClientServiceRole();
                                        clientServiceRole.id = DEFAULT_ROLE;
                                        clientServiceRole.clientServiceRoleStatus = new ClientServiceRoleStatus();
                                        clientServiceRole.clientServiceRoleStatus.value = csr.clientServiceRoleStatus.value;

                                        roleIdentity = new List<BT.SaaS.IspssAdapter.Dnp.ClientIdentity>();

                                        foreach (BT.SaaS.IspssAdapter.Dnp.ClientIdentity cid in csr.clientIdentity)
                                        {
                                            clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                                            clientIdentity.value = cid.value;
                                            clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                                            clientIdentity.managedIdentifierDomain.value = cid.managedIdentifierDomain.value;
                                            clientIdentity.action = ACTION_DELETE;
                                            clientIdentityListDeletion.Add(clientIdentity);

                                            clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                                            clientIdentity.value = cid.value;
                                            clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                                            clientIdentity.managedIdentifierDomain.value = cid.managedIdentifierDomain.value;
                                            clientIdentity.clientIdentityStatus = new ClientIdentityStatus();
                                            clientIdentity.clientIdentityStatus.value = ACTIVE;
                                            clientIdentity.action = ACTION_INSERT;

                                            roleIdentity.Add(clientIdentity);

                                            clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                                            clientIdentity.value = cid.value;
                                            clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                                            clientIdentity.managedIdentifierDomain.value = cid.managedIdentifierDomain.value;
                                            clientIdentity.clientIdentityStatus = new ClientIdentityStatus();
                                            clientIdentity.clientIdentityStatus.value = ACTIVE;
                                            clientIdentity.action = ACTION_INSERT;

                                            clientIdentityValidationList = new List<ClientIdentityValidation>();

                                            if (gcp.getClientProfileV1Response.getClientProfileV1Res.clientProfileV1.client.clientIdentity.ToList().Where(ci => ci.value.Equals(cid.value) && ci.managedIdentifierDomain.value.Equals(cid.managedIdentifierDomain.value)).FirstOrDefault().clientIdentityValidation != null)
                                            {
                                                foreach (ClientIdentityValidation validation1 in gcp.getClientProfileV1Response.getClientProfileV1Res.clientProfileV1.client.clientIdentity.ToList().Where(ci => ci.value.Equals(cid.value) && ci.managedIdentifierDomain.value.Equals(cid.managedIdentifierDomain.value)).FirstOrDefault().clientIdentityValidation)
                                                {
                                                    validation = new ClientIdentityValidation();
                                                    validation.action = ACTION_INSERT;
                                                    validation.name = validation1.name;
                                                    validation.value = validation1.value;
                                                    clientIdentityValidationList.Add(validation);
                                                }
                                            }
                                            clientIdentity.clientIdentityValidation = clientIdentityValidationList.ToArray();
                                            clientIdentityListInsertion.Add(clientIdentity);
                                        }
                                        clientServiceRole.clientIdentity = roleIdentity.ToArray();
                                        clientServiceRole.action = ACTION_INSERT;
                                        clientServiceRoleInsertionList.Add(clientServiceRole);
                                    }
                                }
                            }
                            #endregion
                            //pavan

                            #region HCSWarranty
                            if (serviceInstances[i].clientServiceInstanceIdentifier.value.Equals("CONTRACTANDWARRANTY", StringComparison.OrdinalIgnoreCase)
                                && serviceInstances[i].clientServiceRole != null)
                            {
                                if (serviceInstances[i].clientServiceInstanceCharacteristic != null && serviceInstances[i].clientServiceInstanceCharacteristic.ToList().Exists(chr => chr.name.Equals("ACT_TYPE", StringComparison.OrdinalIgnoreCase)))
                                {
                                    isHCSWarranty = true;
                                    acttype = serviceInstances[i].clientServiceInstanceCharacteristic.ToList().Where(chr => chr.name.Equals("ACT_TYPE", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                }
                            }
                            #endregion

                            csi = new ClientServiceInstanceV1();
                            csi.clientServiceRole = clientServiceRoleDeletionList.ToArray();

                            if (serviceInstances[i].clientServiceInstanceIdentifier.value.Equals("HOMEAWAY", StringComparison.OrdinalIgnoreCase))
                            {
                                csi.name = BillAccountNumber;
                            }
                            else
                            {
                                csi.serviceIdentity = new ServiceIdentity[1];
                                csi.serviceIdentity[0] = new ServiceIdentity();
                                csi.serviceIdentity[0].action = ACTION_SEARCH;
                                csi.serviceIdentity[0].domain = BACID_IDENTIFER_NAMEPACE;
                                csi.serviceIdentity[0].value = BillAccountNumber;
                            }
                            csi.clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                            csi.clientServiceInstanceIdentifier.value = serviceInstances[i].clientServiceInstanceIdentifier.value;
                            csi.action = ACTION_UPDATE;

                            CSIListForRolesDeletion.Add(csi);

                            csi = new ClientServiceInstanceV1();
                            csi.clientServiceRole = clientServiceRoleInsertionList.ToArray();
                            if (serviceInstances[i].clientServiceInstanceIdentifier.value.Equals("HOMEAWAY", StringComparison.OrdinalIgnoreCase))
                            {
                                csi.name = BillAccountNumber;
                            }
                            else
                            {
                                csi.serviceIdentity = new ServiceIdentity[1];
                                csi.serviceIdentity[0] = new ServiceIdentity();
                                csi.serviceIdentity[0].action = ACTION_SEARCH;
                                csi.serviceIdentity[0].domain = BACID_IDENTIFER_NAMEPACE;
                                csi.serviceIdentity[0].value = BillAccountNumber;
                            }
                            csi.clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                            csi.clientServiceInstanceIdentifier.value = serviceInstances[i].clientServiceInstanceIdentifier.value;
                            csi.clientServiceInstanceStatus = new ClientServiceInstanceStatus();
                            csi.clientServiceInstanceStatus.value = serviceInstances[i].clientServiceInstanceStatus.value;
                            csi.action = ACTION_LINK;

                            CSIListForRolesInsertion.Add(csi);
                        }
                    }

                    //deleting roles from bac profile(1st CP)
                    clientProfile = new ClientProfileV1();
                    clientProfile.client = new Client();
                    clientProfile.client.action = ACTION_UPDATE;
                    clientProfile.client.clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity[1];
                    clientProfile.client.clientIdentity[0] = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                    clientProfile.client.clientIdentity[0].value = BillAccountNumber;
                    clientProfile.client.clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientProfile.client.clientIdentity[0].managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;
                    clientProfile.client.clientIdentity[0].action = ACTION_SEARCH;

                    clientProfile.clientServiceInstanceV1 = new ClientServiceInstanceV1[serviceInstances.Length];
                    clientProfile.clientServiceInstanceV1 = CSIListForRolesDeletion.ToArray();
                    clientProfileList.Add(clientProfile);

                    //deleting client identities from bac profile(2nd CP)
                    //Preparing this ClientProfile only if there are any ClienIdentites required to be deleted.
                    if (clientIdentityListDeletion != null && clientIdentityListDeletion.Count > 0)
                    {
                        clientProfile = new ClientProfileV1();
                        clientProfile.client = new Client();
                        clientProfile.client.action = ACTION_UPDATE;

                        clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                        clientIdentity.value = BillAccountNumber;
                        clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                        clientIdentity.managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;
                        clientIdentity.action = ACTION_SEARCH;
                        clientIdentityListDeletion.Add(clientIdentity);

                        clientProfile.client.clientIdentity = clientIdentityListDeletion.ToArray();

                        clientProfileList.Add(clientProfile);
                    }
                    //deleting client identity BAC(3rd CP)
                    if (gcp.getClientProfileV1Response.getClientProfileV1Res.clientProfileV1.client.clientIdentity.ToList().Exists(clId => clId.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)))
                    {
                        clientProfile = new ClientProfileV1();
                        clientProfile.client = new Client();
                        clientProfile.client.action = ACTION_UPDATE;

                        clientProfile.client.clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity[2];
                        clientProfile.client.clientIdentity[0] = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                        clientProfile.client.clientIdentity[0].value = BillAccountNumber;
                        clientProfile.client.clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                        clientProfile.client.clientIdentity[0].managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;
                        clientProfile.client.clientIdentity[0].action = ACTION_SEARCH;

                        clientProfile.client.clientIdentity[1] = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                        clientProfile.client.clientIdentity[1].value = BillAccountNumber;
                        clientProfile.client.clientIdentity[1].managedIdentifierDomain = new ManagedIdentifierDomain();
                        clientProfile.client.clientIdentity[1].managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;
                        clientProfile.client.clientIdentity[1].action = ACTION_DELETE;

                        clientProfileList.Add(clientProfile);
                    }
                    else
                    {
                        clientProfile = new ClientProfileV1();
                        clientProfile.client = new Client();
                        clientProfile.client.action = ACTION_DELETE;

                        clientProfile.client.clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity[1];
                        clientProfile.client.clientIdentity[0] = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                        clientProfile.client.clientIdentity[0].value = BillAccountNumber;
                        clientProfile.client.clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                        clientProfile.client.clientIdentity[0].managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;
                        clientProfile.client.clientIdentity[0].action = ACTION_SEARCH;

                        clientProfileList.Add(clientProfile);
                    }

                    //inserting roles and identities to BTOneID profile(4th CP)
                    clientProfile = new ClientProfileV1();
                    clientProfile.client = new Client();
                    clientProfile.client.action = ACTION_UPDATE;

                    clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                    clientIdentity.value = BtoneId;
                    clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientIdentity.managedIdentifierDomain.value = isEECustomer ? EEONLINEID : BTCOM;
                    clientIdentity.action = ACTION_SEARCH;
                    clientIdentityListInsertion.Add(clientIdentity);

                    clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                    clientIdentity.value = BillAccountNumber;
                    clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientIdentity.managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;

                    if (isHCSWarranty)
                    {
                        clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                        clientIdentity.value = BillAccountNumber;
                        clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                        clientIdentity.managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;
                        clientIdentity.clientIdentityValidation = new ClientIdentityValidation[2];
                        clientIdentity.clientIdentityValidation[0] = new ClientIdentityValidation();
                        clientIdentity.clientIdentityValidation[0].name = ACCOUNTTRUSTMETHOD;
                        clientIdentity.clientIdentityValidation[0].value = AhtValue;
                        clientIdentity.clientIdentityValidation[0].action = ACTION_INSERT;
                        clientIdentity.clientIdentityValidation[1] = new ClientIdentityValidation();
                        clientIdentity.clientIdentityValidation[1].name = "ACT_TYPE";
                        clientIdentity.clientIdentityValidation[1].value = acttype;
                        clientIdentity.clientIdentityValidation[1].action = ACTION_INSERT;
                        clientIdentity.action = ACTION_INSERT;
                        clientIdentity.clientIdentityStatus = new ClientIdentityStatus();
                        clientIdentity.clientIdentityStatus.value = ACTIVE;

                        clientIdentityListInsertion.Add(clientIdentity);
                    }
                    else
                    {
                        clientIdentity.clientIdentityStatus = new ClientIdentityStatus();
                        clientIdentity.clientIdentityStatus.value = ACTIVE;
                        clientIdentity.clientIdentityValidation = new ClientIdentityValidation[1];
                        clientIdentity.clientIdentityValidation[0] = new ClientIdentityValidation();
                        clientIdentity.clientIdentityValidation[0].name = ACCOUNTTRUSTMETHOD;
                        clientIdentity.clientIdentityValidation[0].value = AhtValue;
                        clientIdentity.clientIdentityValidation[0].action = ACTION_INSERT;
                        clientIdentity.action = ACTION_INSERT;

                        clientIdentityListInsertion.Add(clientIdentity);
                    }

                    clientProfile.client.clientIdentity = clientIdentityListInsertion.ToArray();

                    clientProfile.clientServiceInstanceV1 = new ClientServiceInstanceV1[serviceInstances.Length];
                    clientProfile.clientServiceInstanceV1 = CSIListForRolesInsertion.ToArray();

                    clientProfileList.Add(clientProfile);
                }

                batchProfileRequest.manageBatchProfilesV1Request.ManageBatchProfilesV1Req = clientProfileList.ToArray();

                e2eData.logMessage(StartedDnPCall, "Started... Dnp manageBatchProfilesV1 call");
                e2eData.startOutboundCall(ConfigurationManager.AppSettings["destinationDnP"].ToString(), E2ETransaction.vREQ, ACCOUNT_HOLDER_STATUS);

                batchProfileRequest.manageBatchProfilesV1Request.standardHeader.e2e = new BT.SaaS.IspssAdapter.Dnp.E2E();
                batchProfileRequest.manageBatchProfilesV1Request.standardHeader.e2e.E2EDATA = e2eData.toString();

                LogHelper.LogActivityDetails(bptmTxnId, guid, MakingDnPcall + " to linkUnlinkBacFromBtOneID", System.DateTime.Now - ActivityStartTime, "AHTLogTrace", guid, batchProfileRequest.SerializeObject());

                batchResponse = manageBatchProfilesV1(batchProfileRequest, requestOrderRequest);

                LogHelper.LogActivityDetails(bptmTxnId, guid, GotResponseFromDnP + " to linkUnlinkBacFromBtOneID", System.DateTime.Now - ActivityStartTime, "AHTLogTrace", guid, batchResponse.SerializeObject());

                if (batchResponse != null
                      && batchResponse.manageBatchProfilesV1Response != null
                      && batchResponse.manageBatchProfilesV1Response.standardHeader != null
                      && batchResponse.manageBatchProfilesV1Response.standardHeader.serviceState != null
                      && batchResponse.manageBatchProfilesV1Response.standardHeader.serviceState.stateCode != null
                      && batchResponse.manageBatchProfilesV1Response.standardHeader.serviceState.stateCode == "0")
                {
                    e2eData.logMessage(StartedDnPCall, "Ending... Dnp manageBatchProfilesV1 call");
                    //raisevent
                    EventMapper.RaiseEvnt(requestOrderRequest, ExecuteSuccessResponse, OrderStatusEnum.success, batchProfileRequest.SerializeObject());
                    if (InviteParameterslist.Count() > 0)
                    {
                        string oldbtoneid = gcp.getClientProfileV1Response.getClientProfileV1Res.clientProfileV1.client.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(BTCOM)).FirstOrDefault().value;
                        ManageInviteServiceInstanceInDNP("SPRING_GSM", InviteParameterslist, oldbtoneid, ref e2eData, notification, requestOrderRequest);
                    }

                    if (delegateRoleParameterlist.Count() > 0)
                    {
                        ManageDelegareRoleServiceInstanceInDNP(delegateRoleParameterlist, ref e2eData, notification, requestOrderRequest);
                    }
                    else
                    {
                        notification.sendNotification(true, false, "004", string.Empty, ref e2eData);
                    }
                }
                else
                {
                    e2eData.logMessage(StartedDnPCall, "Ending... Dnp manageBatchProfilesV1 call");
                    //raisevent
                    EventMapper.RaiseEvnt(requestOrderRequest, ExecuteFailedResponse, OrderStatusEnum.failed, batchProfileRequest.SerializeObject());
                    InboundQueueDAL.UpdateQueueRawXML("777", batchResponse.manageBatchProfilesV1Response.manageBatchProfilesV1Res.messages[0].description, requestOrderRequest.Order.OrderIdentifier.Value);
                    notification.sendNotification(false, false, "777", batchResponse.manageBatchProfilesV1Response.manageBatchProfilesV1Res.messages[0].description, ref e2eData);
                }

                if (batchResponse != null
                     && batchResponse.manageBatchProfilesV1Response != null
                     && batchResponse.manageBatchProfilesV1Response.standardHeader != null
                     && batchResponse.manageBatchProfilesV1Response.standardHeader.e2e != null
                     && batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA != null
                     && !String.IsNullOrEmpty(batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA))
                {
                    e2eDataPassed = batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA;
                    e2eData.endOutboundCall(e2eDataPassed);
                }
                else
                {
                    e2eData.endOutboundCall(e2eData.toString());
                }
            }
            catch (DnpException dnpex)
            {
                throw dnpex;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                clientProfileList = null;
                clientIdentityListDeletion = null;
                clientIdentityListInsertion = null;
                CSIListForRolesDeletion = null;
                CSIListForRolesInsertion = null;
                clientServiceRoleDeletionList = null;
                clientServiceRoleInsertionList = null;
                batchProfileRequest = null;
                batchResponse = null;
                roleIdentity = null;
                clientIdentityValidationList = null;
                validation = null;
                csi = null;
                clientServiceRole = null;
                clientIdentity = null;
            }
        }
        public void ManageInviteServiceInstanceInDNP(string servicename, List<InviteRoleParameters> parameterslist, string oldbtoneid, ref E2ETransaction e2eData, MSEOOrderNotification notification, OrderRequest requestOrderRequest)
        {
            manageBatchProfilesV1Request1 batchProfileRequest = null;
            manageBatchProfilesV1Response1 batchResponse = null;
            batchProfileRequest = new manageBatchProfilesV1Request1();
            batchProfileRequest.manageBatchProfilesV1Request = new ManageBatchProfilesV1Request();

            List<ClientProfileV1> clientProfileList = null;
            try
            {
                BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock standardHeader = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
                standardHeader.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
                standardHeader.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
                standardHeader.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();
                batchProfileRequest.manageBatchProfilesV1Request.standardHeader = standardHeader;

                ClientProfileV1 clientProfile = null;
                clientProfileList = new List<ClientProfileV1>();

                List<BT.SaaS.IspssAdapter.Dnp.ClientIdentity> clientIdentityList = new List<BT.SaaS.IspssAdapter.Dnp.ClientIdentity>();
                List<BT.SaaS.IspssAdapter.Dnp.ClientIdentity> srClientIdentityList = new List<BT.SaaS.IspssAdapter.Dnp.ClientIdentity>();

                List<BT.SaaS.IspssAdapter.Dnp.ClientIdentity> deleteClientIdentityList = new List<BT.SaaS.IspssAdapter.Dnp.ClientIdentity>();
                List<BT.SaaS.IspssAdapter.Dnp.ClientIdentity> deleteRoleIdentityList = new List<BT.SaaS.IspssAdapter.Dnp.ClientIdentity>();

                List<ClientServiceInstanceV1> listclientServiceInstanceV1 = new List<ClientServiceInstanceV1>();
                List<ClientServiceInstanceV1> deleteListclientServiceInstanceV1 = new List<ClientServiceInstanceV1>();
                ClientServiceInstanceV1 clientServiceInstanceV1 = new ClientServiceInstanceV1();
                clientProfile = new ClientProfileV1();
                ClientServiceRole clientServiceRole = new ClientServiceRole();

                BT.SaaS.IspssAdapter.Dnp.ClientIdentity clientIdentity = null;

                clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                clientIdentity.managedIdentifierDomain.value = BTCOM;
                clientIdentity.value = oldbtoneid;
                clientIdentity.action = ACTION_SEARCH;
                deleteClientIdentityList.Add(clientIdentity);

                foreach (InviteRoleParameters Parameters in parameterslist)
                {
                    ServiceIdentity[] serviceIdentity = new ServiceIdentity[1];
                    serviceIdentity[0] = new ServiceIdentity();
                    serviceIdentity[0].domain = BACID_IDENTIFER_NAMEPACE;
                    serviceIdentity[0].value = Parameters.SpownerBac;
                    serviceIdentity[0].action = ACTION_SEARCH;


                    clientServiceInstanceV1 = new ClientServiceInstanceV1();
                    clientServiceInstanceV1.clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                    clientServiceInstanceV1.clientServiceInstanceIdentifier.value = servicename;
                    clientServiceInstanceV1.clientServiceInstanceStatus = new ClientServiceInstanceStatus();
                    clientServiceInstanceV1.clientServiceInstanceStatus.value = ACTIVE;
                    clientServiceInstanceV1.serviceIdentity = serviceIdentity;
                    clientServiceInstanceV1.action = ACTION_UPDATE;

                    clientServiceRole = new ClientServiceRole();
                    clientServiceRole.id = INVITE_ROLE;
                    clientServiceRole.name = Parameters.RoleKey;
                    clientServiceRole.action = ACTION_DELETE;

                    clientServiceInstanceV1.clientServiceRole = new ClientServiceRole[1];
                    clientServiceInstanceV1.clientServiceRole[0] = clientServiceRole;
                    deleteListclientServiceInstanceV1.Add(clientServiceInstanceV1);
                }
                clientProfile = new ClientProfileV1();
                clientProfile.client = new Client();
                clientProfile.client.action = ACTION_UPDATE;

                clientProfile.client.clientIdentity = deleteClientIdentityList.ToArray();
                clientProfile.clientServiceInstanceV1 = deleteListclientServiceInstanceV1.ToArray();

                clientProfileList.Add(clientProfile);

                clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                clientIdentity.managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;
                clientIdentity.value = parameterslist.ToArray()[0].InviteeBac;
                clientIdentity.action = ACTION_SEARCH;
                clientIdentityList.Add(clientIdentity);



                foreach (InviteRoleParameters Parameters in parameterslist)
                {
                    srClientIdentityList = new List<BT.SaaS.IspssAdapter.Dnp.ClientIdentity>();
                    ServiceIdentity[] serviceIdentity = new ServiceIdentity[1];
                    serviceIdentity[0] = new ServiceIdentity();
                    serviceIdentity[0].domain = BACID_IDENTIFER_NAMEPACE;
                    serviceIdentity[0].value = Parameters.SpownerBac;
                    serviceIdentity[0].action = ACTION_SEARCH;


                    clientServiceInstanceV1 = new ClientServiceInstanceV1();
                    clientServiceInstanceV1.clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                    clientServiceInstanceV1.clientServiceInstanceIdentifier.value = servicename;
                    clientServiceInstanceV1.clientServiceInstanceStatus = new ClientServiceInstanceStatus();
                    clientServiceInstanceV1.serviceIdentity = serviceIdentity;
                    clientServiceInstanceV1.action = ACTION_UPDATE;


                    clientServiceRole = new ClientServiceRole();
                    clientServiceRole.id = INVITE_ROLE;
                    clientServiceRole.clientServiceRoleStatus = new ClientServiceRoleStatus();
                    clientServiceRole.clientServiceRoleStatus.value = Parameters.RoleStatus;
                    clientServiceRole.action = ACTION_INSERT;


                    BT.SaaS.IspssAdapter.Dnp.ClientIdentity srClientIdentity_bac = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                    srClientIdentity_bac.managedIdentifierDomain = new ManagedIdentifierDomain();
                    srClientIdentity_bac.managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;
                    srClientIdentity_bac.value = Parameters.InviteeBac;
                    srClientIdentity_bac.action = ACTION_INSERT;
                    srClientIdentityList.Add(srClientIdentity_bac);

                    BT.SaaS.IspssAdapter.Dnp.ClientIdentity srClientIdentity_imsi = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                    srClientIdentity_imsi.managedIdentifierDomain = new ManagedIdentifierDomain();
                    srClientIdentity_imsi.managedIdentifierDomain.value = IMSI;
                    srClientIdentity_imsi.value = Parameters.IMSI;
                    srClientIdentity_imsi.action = ACTION_INSERT;
                    srClientIdentityList.Add(srClientIdentity_imsi);

                    BT.SaaS.IspssAdapter.Dnp.ClientIdentity srClientIdentity_rvsid = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                    srClientIdentity_rvsid.managedIdentifierDomain = new ManagedIdentifierDomain();
                    srClientIdentity_rvsid.managedIdentifierDomain.value = RVSID;
                    srClientIdentity_rvsid.value = Parameters.RVSID;
                    srClientIdentity_rvsid.action = ACTION_INSERT;
                    srClientIdentityList.Add(srClientIdentity_rvsid);

                    BT.SaaS.IspssAdapter.Dnp.ClientIdentity srClientIdentity_msisdn = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                    srClientIdentity_msisdn.managedIdentifierDomain = new ManagedIdentifierDomain();
                    srClientIdentity_msisdn.managedIdentifierDomain.value = MOBILE_MSISDN_NAMESPACE;
                    srClientIdentity_msisdn.value = Parameters.MSISDN;
                    srClientIdentity_msisdn.action = ACTION_INSERT;
                    srClientIdentityList.Add(srClientIdentity_msisdn);

                    clientServiceRole.clientIdentity = srClientIdentityList.ToArray();

                    clientServiceInstanceV1.clientServiceRole = new ClientServiceRole[1];
                    clientServiceInstanceV1.clientServiceRole[0] = clientServiceRole;
                    listclientServiceInstanceV1.Add(clientServiceInstanceV1);
                }
                clientProfile = new ClientProfileV1();
                clientProfile.client = new Client();
                clientProfile.client.action = ACTION_UPDATE;

                clientProfile.client.clientIdentity = clientIdentityList.ToArray();
                clientProfile.clientServiceInstanceV1 = listclientServiceInstanceV1.ToArray();

                clientProfileList.Add(clientProfile);
                batchProfileRequest.manageBatchProfilesV1Request.ManageBatchProfilesV1Req = clientProfileList.ToArray();
                batchProfileRequest.manageBatchProfilesV1Request.standardHeader.e2e = new BT.SaaS.IspssAdapter.Dnp.E2E();
                batchProfileRequest.manageBatchProfilesV1Request.standardHeader.e2e.E2EDATA = e2eData.toString();

                batchResponse = manageBatchProfilesV1(batchProfileRequest, requestOrderRequest);
                if (batchResponse != null
                      && batchResponse.manageBatchProfilesV1Response != null
                      && batchResponse.manageBatchProfilesV1Response.standardHeader != null
                      && batchResponse.manageBatchProfilesV1Response.standardHeader.serviceState != null
                      && batchResponse.manageBatchProfilesV1Response.standardHeader.serviceState.stateCode != null
                      && batchResponse.manageBatchProfilesV1Response.standardHeader.serviceState.stateCode == "0")
                {
                    e2eData.logMessage(StartedDnPCall, "Ending... Dnp manageBatchProfilesV1 call");
                    //raisevent                   
                    // EventMapper.RaiseEvnt(requestOrderRequest, ExecuteSuccessResponse, OrderStatusEnum.success, batchProfileRequest.SerializeObject());
                    notification.sendNotification(true, false, "004", string.Empty, ref e2eData);
                }
                else
                {
                    e2eData.logMessage(StartedDnPCall, "Ending... Dnp manageBatchProfilesV1 call");
                    //raisevent
                    //EventMapper.RaiseEvnt(requestOrderRequest, ExecuteFailedResponse, OrderStatusEnum.failed, batchProfileRequest.SerializeObject());
                    InboundQueueDAL.UpdateQueueRawXML("777", batchResponse.manageBatchProfilesV1Response.manageBatchProfilesV1Res.messages[0].description, requestOrderRequest.Order.OrderIdentifier.Value);
                    notification.sendNotification(false, false, "777", batchResponse.manageBatchProfilesV1Response.manageBatchProfilesV1Res.messages[0].description, ref e2eData);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                parameterslist = null;
            }
        }

        public void ManageDelegareRoleServiceInstanceInDNP(List<InviteRoleParameters> delegareRoleParametersList, ref E2ETransaction e2eData, MSEOOrderNotification notification, OrderRequest requestOrderRequest)
        {
            manageBatchProfilesV1Request1 batchProfileRequest = null;
            manageBatchProfilesV1Response1 batchResponse = null;
            batchProfileRequest = new manageBatchProfilesV1Request1();
            batchProfileRequest.manageBatchProfilesV1Request = new ManageBatchProfilesV1Request();

            try
            {
                BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock standardHeader = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
                standardHeader.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
                standardHeader.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
                standardHeader.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();
                batchProfileRequest.manageBatchProfilesV1Request.standardHeader = standardHeader;

                List<ClientProfileV1> clientProfileList = new List<ClientProfileV1>();

                foreach (InviteRoleParameters Parameters in delegareRoleParametersList)
                {
                    ClientProfileV1 clientProfile = null;
                    List<BT.SaaS.IspssAdapter.Dnp.ClientIdentity> clientIdentityList = new List<BT.SaaS.IspssAdapter.Dnp.ClientIdentity>();
                    BT.SaaS.IspssAdapter.Dnp.ClientIdentity clientIdentity = null;

                    List<BT.SaaS.IspssAdapter.Dnp.ClientIdentity> srClientIdentityList = new List<BT.SaaS.IspssAdapter.Dnp.ClientIdentity>();

                    List<ClientServiceInstanceV1> clientServiceInstanceList = new List<ClientServiceInstanceV1>();

                    clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                    clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientIdentity.managedIdentifierDomain.value = BTCOM;
                    clientIdentity.value = Parameters.BtCom;
                    clientIdentity.action = ACTION_SEARCH;
                    clientIdentityList.Add(clientIdentity);

                    srClientIdentityList = new List<BT.SaaS.IspssAdapter.Dnp.ClientIdentity>();
                    ServiceIdentity[] serviceIdentity = new ServiceIdentity[1];
                    serviceIdentity[0] = new ServiceIdentity();
                    serviceIdentity[0].domain = BACID_IDENTIFER_NAMEPACE;
                    serviceIdentity[0].value = Parameters.InviteeBac;
                    serviceIdentity[0].action = ACTION_SEARCH;

                    ClientServiceInstanceV1 clientServiceInstance = new ClientServiceInstanceV1();
                    clientServiceInstance.clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                    clientServiceInstance.clientServiceInstanceIdentifier.value = Parameters.ServiceCode;
                    clientServiceInstance.clientServiceInstanceStatus = new ClientServiceInstanceStatus();
                    clientServiceInstance.serviceIdentity = serviceIdentity;
                    clientServiceInstance.action = ACTION_UPDATE;

                    ClientServiceRole clientServiceRole = new ClientServiceRole();
                    clientServiceRole.id = Parameters.RoleName;
                    clientServiceRole.clientServiceRoleStatus = new ClientServiceRoleStatus();
                    clientServiceRole.clientServiceRoleStatus.value = Parameters.RoleStatus;
                    clientServiceRole.name = Parameters.RoleKey;
                    clientServiceRole.action = ACTION_UPDATE;

                    BT.SaaS.IspssAdapter.Dnp.ClientIdentity srClientIdentity_rvsid = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                    srClientIdentity_rvsid.managedIdentifierDomain = new ManagedIdentifierDomain();
                    srClientIdentity_rvsid.managedIdentifierDomain.value = RVSID;
                    srClientIdentity_rvsid.value = Parameters.RVSID;
                    srClientIdentity_rvsid.action = ACTION_INSERT;
                    srClientIdentityList.Add(srClientIdentity_rvsid);

                    if (!string.IsNullOrEmpty(Parameters.MSISDN))
                    {
                        BT.SaaS.IspssAdapter.Dnp.ClientIdentity srClientIdentity_msisdn = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                        srClientIdentity_msisdn.managedIdentifierDomain = new ManagedIdentifierDomain();
                        srClientIdentity_msisdn.managedIdentifierDomain.value = MOBILE_MSISDN_NAMESPACE;
                        srClientIdentity_msisdn.value = Parameters.MSISDN;
                        srClientIdentity_msisdn.action = ACTION_INSERT;
                        srClientIdentityList.Add(srClientIdentity_msisdn);
                    }
                    if (!string.IsNullOrEmpty(Parameters.IMSI))
                    {
                        BT.SaaS.IspssAdapter.Dnp.ClientIdentity srClientIdentity_imsi = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                        srClientIdentity_imsi.managedIdentifierDomain = new ManagedIdentifierDomain();
                        srClientIdentity_imsi.managedIdentifierDomain.value = IMSI;
                        srClientIdentity_imsi.value = Parameters.IMSI;
                        srClientIdentity_imsi.action = ACTION_INSERT;
                        srClientIdentityList.Add(srClientIdentity_imsi);
                    }
                    if (!string.IsNullOrEmpty(Parameters.Ota_Device_Id))
                    {
                        BT.SaaS.IspssAdapter.Dnp.ClientIdentity srClientIdentity_ota_device_id = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                        srClientIdentity_ota_device_id.managedIdentifierDomain = new ManagedIdentifierDomain();
                        srClientIdentity_ota_device_id.managedIdentifierDomain.value = OTA_DEVICE_ID;
                        srClientIdentity_ota_device_id.value = Parameters.Ota_Device_Id;
                        srClientIdentity_ota_device_id.action = ACTION_INSERT;
                        srClientIdentityList.Add(srClientIdentity_ota_device_id);
                    }

                    clientServiceRole.clientIdentity = srClientIdentityList.ToArray();
                    clientServiceInstance.clientServiceRole = new ClientServiceRole[1];
                    clientServiceInstance.clientServiceRole[0] = clientServiceRole;
                    clientServiceInstanceList.Add(clientServiceInstance);

                    clientProfile = new ClientProfileV1();
                    clientProfile.client = new Client();
                    clientProfile.client.action = ACTION_UPDATE;
                    clientProfile.client.clientIdentity = clientIdentityList.ToArray();
                    clientProfile.clientServiceInstanceV1 = clientServiceInstanceList.ToArray();
                    clientProfileList.Add(clientProfile);

                }

                batchProfileRequest.manageBatchProfilesV1Request.ManageBatchProfilesV1Req = clientProfileList.ToArray();
                batchProfileRequest.manageBatchProfilesV1Request.standardHeader.e2e = new BT.SaaS.IspssAdapter.Dnp.E2E();
                batchProfileRequest.manageBatchProfilesV1Request.standardHeader.e2e.E2EDATA = e2eData.toString();

                batchResponse = manageBatchProfilesV1(batchProfileRequest, requestOrderRequest);
                if (batchResponse != null
                      && batchResponse.manageBatchProfilesV1Response != null
                      && batchResponse.manageBatchProfilesV1Response.standardHeader != null
                      && batchResponse.manageBatchProfilesV1Response.standardHeader.serviceState != null
                      && batchResponse.manageBatchProfilesV1Response.standardHeader.serviceState.stateCode != null
                      && batchResponse.manageBatchProfilesV1Response.standardHeader.serviceState.stateCode == "0")
                {
                    e2eData.logMessage(StartedDnPCall, "Ending... Dnp manageBatchProfilesV1 call");
                    //raisevent                   
                    // EventMapper.RaiseEvnt(requestOrderRequest, ExecuteSuccessResponse, OrderStatusEnum.success, batchProfileRequest.SerializeObject());
                    notification.sendNotification(true, false, "004", string.Empty, ref e2eData);
                }
                else
                {
                    e2eData.logMessage(StartedDnPCall, "Ending... Dnp manageBatchProfilesV1 call");
                    //raisevent
                    //EventMapper.RaiseEvnt(requestOrderRequest, ExecuteFailedResponse, OrderStatusEnum.failed, batchProfileRequest.SerializeObject());
                    InboundQueueDAL.UpdateQueueRawXML("777", batchResponse.manageBatchProfilesV1Response.manageBatchProfilesV1Res.messages[0].description, requestOrderRequest.Order.OrderIdentifier.Value);
                    notification.sendNotification(false, false, "777", batchResponse.manageBatchProfilesV1Response.manageBatchProfilesV1Res.messages[0].description, ref e2eData);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                delegareRoleParametersList = null;
            }
        }

        public List<InviteRoleParameters> GetInviteParameters(string urlKey, string BacID, string Msisdn, List<InviteRoleParameters> parameterlist)
        {

            serviceResponse serviceresponse = new serviceResponse();
            //string Msisdn;

            string url = ConfigurationManager.AppSettings[urlKey].ToString();
            if (!string.IsNullOrEmpty(Msisdn))
            {
                url = url + "MOBILE_MSISDN/" + Msisdn + "/services?fields=serviceIdentity,serviceRoleIdentity,createdDTime,updatedDTime&serviceCodes=spring_gsm&roleCodes=INVITE&roleIdentity=" + Msisdn + "%23MOBILE_MSISDN";
            }

            serviceresponse = DnpRestCallWrapper.GetInviterolesfromDNP(url);
            if (serviceresponse != null)
            {
                foreach (serviceInstance srvinstc in serviceresponse.serviceInstance)
                {

                    foreach (serviceRole srvcrole in srvinstc.serviceRole)
                    {
                        InviteRoleParameters parameters = new InviteRoleParameters();
                        parameters.SpownerBac = srvinstc.serviceIdentity.ToList().Where(si => si.identifierDomain.Equals("VAS_BILLINGACCOUNT_ID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().identifierValue;
                        parameters.RoleKey = srvcrole.roleKey;
                        parameters.MSISDN = srvcrole.roleIdentity.ToList().Find(ri => ri.identifierDomain == "MOBILE_MSISDN").identifierValue;
                        parameters.RVSID = srvcrole.roleIdentity.ToList().Find(ri => ri.identifierDomain == "RVSID").identifierValue;
                        parameters.IMSI = srvcrole.roleIdentity.ToList().Find(ri => ri.identifierDomain == "IMSI").identifierValue;
                        parameters.InviteeBac = BacID;
                        parameters.RoleStatus = srvcrole.status;
                        parameterlist.Add(parameters);
                    }

                }
            }

            return parameterlist;
        }
        public List<InviteRoleParameters> GetDelegateroleParametersList(string bacId, ClientServiceInstanceV1[] gsiResponse, string Rvsid, List<InviteRoleParameters> delegateroleParameterlist)
        {
            try
            {
                string servicename = string.Empty;

                if (gsiResponse != null)
                {
                    foreach (ClientServiceInstanceV1 serviceIntance in gsiResponse)
                    {
                        servicename = serviceIntance.clientServiceInstanceIdentifier.value;
                        if (serviceIntance.clientServiceRole != null && serviceIntance.clientServiceRole.Count() > 0)
                        {
                            foreach (ClientServiceRole role in serviceIntance.clientServiceRole.Where(csr => csr.id.Equals("SERVICE_USER", StringComparison.OrdinalIgnoreCase) || csr.id.Equals("SERVICE_MANAGER", StringComparison.OrdinalIgnoreCase)))
                            {

                                if (role.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("RVSID", StringComparison.OrdinalIgnoreCase) && ci.value.Equals(Rvsid, StringComparison.OrdinalIgnoreCase)))
                                {
                                    if (servicename == "SPRING_GSM")
                                    {
                                        InviteRoleParameters delegateRoleParameters = new InviteRoleParameters();
                                        delegateRoleParameters.ServiceCode = servicename;
                                        delegateRoleParameters.InviteeBac = bacId;
                                        delegateRoleParameters.RVSID = role.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("RVSID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                        delegateRoleParameters.BtCom = role.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;

                                        if (role.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("IMSI", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(ci.value)))
                                        {
                                            delegateRoleParameters.IMSI = role.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("IMSI", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                        }
                                        if (role.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("MOBILE_MSISDN", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(ci.value)))
                                        {
                                            delegateRoleParameters.MSISDN = role.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("MOBILE_MSISDN", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                        }
                                        if (role.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("OTA_DEVICE_ID", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(ci.value)))
                                        {
                                            delegateRoleParameters.Ota_Device_Id = role.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("OTA_DEVICE_ID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                        }

                                        delegateRoleParameters.RoleKey = role.name;
                                        delegateRoleParameters.RoleName = role.id;
                                        delegateRoleParameters.RoleStatus = role.clientServiceRoleStatus.value;
                                        delegateroleParameterlist.Add(delegateRoleParameters);
                                    }
                                    else if (servicename == "BTWIFI:DEFAULT")
                                    {
                                        InviteRoleParameters delegateRoleParameters = new InviteRoleParameters();
                                        delegateRoleParameters.ServiceCode = servicename;
                                        delegateRoleParameters.InviteeBac = bacId;
                                        delegateRoleParameters.RVSID = role.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("RVSID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                        delegateRoleParameters.BtCom = role.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;

                                        if (role.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("IMSI", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(ci.value)))
                                        {
                                            delegateRoleParameters.IMSI = role.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("IMSI", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                        }
                                        if (role.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("MOBILE_MSISDN", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(ci.value)))
                                        {
                                            delegateRoleParameters.MSISDN = role.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("MOBILE_MSISDN", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                        }
                                        if (role.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("OTA_DEVICE_ID", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(ci.value)))
                                        {
                                            delegateRoleParameters.Ota_Device_Id = role.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("OTA_DEVICE_ID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                        }

                                        delegateRoleParameters.RoleKey = role.name;
                                        delegateRoleParameters.RoleName = role.id;
                                        delegateRoleParameters.RoleStatus = role.clientServiceRoleStatus.value;
                                        delegateroleParameterlist.Add(delegateRoleParameters);
                                    }
                                    else if (servicename == "BTSPORT:DIGITAL")
                                    {
                                        InviteRoleParameters delegateRoleParameters = new InviteRoleParameters();
                                        delegateRoleParameters.ServiceCode = servicename;
                                        delegateRoleParameters.InviteeBac = bacId;
                                        delegateRoleParameters.RVSID = role.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("RVSID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                        delegateRoleParameters.BtCom = role.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;

                                        if (role.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("IMSI", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(ci.value)))
                                        {
                                            delegateRoleParameters.IMSI = role.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("IMSI", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                        }
                                        if (role.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("MOBILE_MSISDN", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(ci.value)))
                                        {
                                            delegateRoleParameters.MSISDN = role.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("MOBILE_MSISDN", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                        }
                                        if (role.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("OTA_DEVICE_ID", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(ci.value)))
                                        {
                                            delegateRoleParameters.Ota_Device_Id = role.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("OTA_DEVICE_ID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                        }

                                        delegateRoleParameters.RoleKey = role.name;
                                        delegateRoleParameters.RoleName = role.id;
                                        delegateRoleParameters.RoleStatus = role.clientServiceRoleStatus.value;
                                        delegateroleParameterlist.Add(delegateRoleParameters);
                                    }
                                }
                            }
                        }
                    }
                }

                return delegateroleParameterlist;

            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
        /// <summary>
        /// to checks wheather the profile has delegateroles or not
        /// </summary>
        /// <param name="gsiResponse">gsi response</param>
        /// <param name="roleDetails">string </param>
        /// <returns>based the roles it returns true or false</returns>
        public bool IsDelegateRolesExists(ClientServiceInstanceV1[] gsiResponse, ref string roleDetails, ref Dictionary<string, string> roleIdentities)
        {
            bool isDelegateExists = false;
            //Dictionary<string, string> roleIdentities = new Dictionary<string, string>();
            try
            {
                if (gsiResponse != null)
                {
                    foreach (ClientServiceInstanceV1 serviceIntance in gsiResponse)
                    {
                        string serviceName = serviceIntance.clientServiceInstanceIdentifier.value;
                        if (serviceIntance.clientServiceRole != null && serviceIntance.clientServiceRole.Count() > 0)
                        {
                            foreach (ClientServiceRole role in serviceIntance.clientServiceRole.Where(csr => csr.id != null && csr.id.Equals("SERVICE_USER", StringComparison.OrdinalIgnoreCase) || csr.id.Equals("SERVICE_MANAGER", StringComparison.OrdinalIgnoreCase)))
                            {
                                
                                if (role.clientIdentity != null && role.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(MOBILE_MSISDN_NAMESPACE, StringComparison.OrdinalIgnoreCase)))
                                {
                                    isDelegateExists = true;

                                    string roleName = role.id;
                                    string roleId = role.name;
                                    string BTComValue = string.Empty;

                                    if (role.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)))
                                    {
                                        BTComValue = role.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;

                                        roleDetails += roleName + ";" + roleId + ";" + BTComValue + ";" + serviceName + ";" + "BTCOM" + ",";
                                    }

                                }
                                else if (role.clientIdentity != null && role.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(RVSID, StringComparison.OrdinalIgnoreCase)))
                                {
                                    isDelegateExists = true;

                                    string roleName = role.id;
                                    string roleId = role.name;
                                    string BTComValue = string.Empty;

                                    if (role.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)))
                                    {
                                        BTComValue = role.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;

                                        roleDetails += roleName + ";" + roleId + ";" + BTComValue + ";" + serviceName + ";" + "BTCOM" + ",";
                                    }
                                }
                                else if (role.clientIdentity != null && role.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(EE_MOBILE_MSISDN_NAMESPACE, StringComparison.OrdinalIgnoreCase)))
                                {
                                    isDelegateExists = true;

                                    string roleName = role.id;
                                    string roleId = role.name;
                                    string identity = string.Empty; string identityDomain = string.Empty;
                                    List<string> identitydetails = new List<string>();

                                    if (role.clientIdentity != null && role.clientIdentity.Count() > 0)
                                    {
                                        IspssAdapter.Dnp.ClientIdentity roleIdentity = role.clientIdentity.ToList().Where(ci => !ci.managedIdentifierDomain.value.Equals(EE_MOBILE_MSISDN_NAMESPACE, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                                        if (roleIdentity != null)
                                        {
                                            identity = roleIdentity.value;
                                            identityDomain = roleIdentity.managedIdentifierDomain.value;
                                            //Fetch role details to delete delete roles..
                                            roleDetails += roleName + ";" + roleId + ";" + identity + ";" + serviceName + ";" + identityDomain + ",";
                                        }
                                        foreach (IspssAdapter.Dnp.ClientIdentity Identity in role.clientIdentity)
                                        {
                                            //Fetch all role identity detais to insert the deleted delegate role..
                                            identitydetails.Add(Identity.managedIdentifierDomain.value + "," + Identity.value+","+role.id);
                                        }
                                        roleIdentities.Add(role.name, string.Join(";", identitydetails.ToArray()));
                                    }

                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return isDelegateExists;
        }
        /// <summary>
        /// if delegate roles are exists then using this method will delete the delegate roles while doing the delink with AHT.
        /// </summary>
        /// <param name="serviceRoleDetails">if delegate roles exists pass the roleid,rolename,btcom and servicename</param>
        /// <param name="bacID">bacid</param>
        /// <param name="requestOrderRequest"></param>
        /// <param name="e2eData"></param>
        /// <param name="notification"></param>
        public bool DeleteDelegateRoles(string serviceRoleDetails, string bacID, OrderRequest requestOrderRequest, ref E2ETransaction e2eData, ref MSEOOrderNotification notification)
        {
            List<ClientProfileV1> lstClietProfile = new List<ClientProfileV1>();
            ClientProfileV1 clinetprofile = null;
            manageBatchProfilesV1Request1 batchProfileRequest = null;
            manageBatchProfilesV1Response1 batchResponse = null;
            try
            {
                batchProfileRequest = new manageBatchProfilesV1Request1();
                batchProfileRequest.manageBatchProfilesV1Request = new ManageBatchProfilesV1Request();
                BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock standardHeader = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
                standardHeader.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
                standardHeader.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
                standardHeader.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();
                batchProfileRequest.manageBatchProfilesV1Request.standardHeader = standardHeader;

                string[] delegateRoleDetails = serviceRoleDetails.ToString().Split(',');
                foreach (string roleDetails in delegateRoleDetails)
                {
                    if (!string.IsNullOrEmpty(roleDetails))
                    {
                        string[] delegateRoleList = roleDetails.ToString().Split(';');

                        List<BT.SaaS.IspssAdapter.Dnp.ClientIdentity> ciDelegateList = new List<BT.SaaS.IspssAdapter.Dnp.ClientIdentity>();
                        BT.SaaS.IspssAdapter.Dnp.ClientIdentity ciDelegate = null;
                        List<ClientServiceInstanceV1> csiDelegateList = new List<ClientServiceInstanceV1>();
                        ClientServiceInstanceV1 csiDelegate = null;

                        ciDelegate = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                        ciDelegate.managedIdentifierDomain = new ManagedIdentifierDomain();
                        ciDelegate.managedIdentifierDomain.value = delegateRoleList[4];
                        ciDelegate.value = delegateRoleList[2].ToString();
                        ciDelegate.action = ACTION_SEARCH;
                        ciDelegateList.Add(ciDelegate);

                        csiDelegate = new ClientServiceInstanceV1();

                        csiDelegate = SpringRequestProcessor.CreatedelegateServiceinstanceobject(ACTION_DELETE, delegateRoleList[0].ToString(), delegateRoleList[1].ToString(), delegateRoleList[3].ToString(), bacID);//pass the values correctly.

                        csiDelegateList.Add(csiDelegate);

                        clinetprofile = new ClientProfileV1();
                        clinetprofile.client = new Client();
                        clinetprofile.client.action = ACTION_UPDATE;
                        clinetprofile.client.clientIdentity = ciDelegateList.ToArray();
                        clinetprofile.clientServiceInstanceV1 = csiDelegateList.ToArray();
                        lstClietProfile.Add(clinetprofile);
                    }
                }
                batchProfileRequest.manageBatchProfilesV1Request.ManageBatchProfilesV1Req = lstClietProfile.ToArray();

                batchProfileRequest.manageBatchProfilesV1Request.standardHeader.e2e = new BT.SaaS.IspssAdapter.Dnp.E2E();
                batchProfileRequest.manageBatchProfilesV1Request.standardHeader.e2e.E2EDATA = e2eData.toString();

                LogHelper.LogActivityDetails(bptmTxnId, guid, MakingDnPcall + " to Delete Delegate roles", System.DateTime.Now - ActivityStartTime, "AHTLogTrace", guid, batchProfileRequest.SerializeObject());

                batchResponse = manageBatchProfilesV1(batchProfileRequest, requestOrderRequest);

                LogHelper.LogActivityDetails(bptmTxnId, guid, GotResponseFromDnP + " to Delete Delegate roles", System.DateTime.Now - ActivityStartTime, "AHTLogTrace", guid, batchResponse.SerializeObject());

                if (batchResponse != null
                      && batchResponse.manageBatchProfilesV1Response != null
                      && batchResponse.manageBatchProfilesV1Response.standardHeader != null
                      && batchResponse.manageBatchProfilesV1Response.standardHeader.serviceState != null
                      && batchResponse.manageBatchProfilesV1Response.standardHeader.serviceState.stateCode != null
                      && batchResponse.manageBatchProfilesV1Response.standardHeader.serviceState.stateCode == "0")
                {
                    e2eData.logMessage(GotResponseFromDnP, "Ending... Dnp manageClientProfileV1 call success while deleting DelegateRoles");
                    e2eDataPassed = batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA;
                    e2eData.endOutboundCall(e2eDataPassed);

                    return true;
                }
                else
                {
                    e2eData.logMessage(GotResponseFromDnP, "Ending... Dnp manageClientProfileV1 call failed while deleting delegateroles");
                    e2eData.endOutboundCall(e2eData.toString());

                    return false;
                }
            }

            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                serviceRoleDetails = null;
            }
        }

        public manageClientProfileV1Response1 UnlinkBACwithBtCom(string BAC, string Btcom, OrderRequest requestOrderRequest, ref E2ETransaction e2eData, ref manageClientProfileV1Request1 profileReq)
        {
            manageClientProfileV1Response1 response = new manageClientProfileV1Response1();

            profileReq = new manageClientProfileV1Request1();
            profileReq.manageClientProfileV1Request = new ManageClientProfileV1Request();
            profileReq.manageClientProfileV1Request.manageClientProfileV1Req = new ManageClientProfileV1Req();
            profileReq.manageClientProfileV1Request.manageClientProfileV1Req.clientProfileV1 = new ClientProfileV1();
            profileReq.manageClientProfileV1Request.manageClientProfileV1Req.clientProfileV1.client = new Client();
            profileReq.manageClientProfileV1Request.manageClientProfileV1Req.clientProfileV1.client.action = ACTION_UPDATE;

            BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock headerBlock = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
            headerBlock.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
            headerBlock.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
            headerBlock.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();
            profileReq.manageClientProfileV1Request.standardHeader = headerBlock;
            profileReq.manageClientProfileV1Request.standardHeader.e2e = new BT.SaaS.IspssAdapter.Dnp.E2E();
            profileReq.manageClientProfileV1Request.standardHeader.e2e.E2EDATA = e2eData.toString();

            List<BT.SaaS.IspssAdapter.Dnp.ClientIdentity> clientIdentityList = new List<BT.SaaS.IspssAdapter.Dnp.ClientIdentity>();

            BT.SaaS.IspssAdapter.Dnp.ClientIdentity clientIdentity = null;

            clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
            clientIdentity.value = BAC;
            clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
            clientIdentity.managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;
            clientIdentity.clientIdentityValidation = new ClientIdentityValidation[1];
            clientIdentity.clientIdentityValidation[0] = new ClientIdentityValidation();
            clientIdentity.clientIdentityValidation[0].name = ACCOUNTTRUSTMETHOD;
            clientIdentity.clientIdentityValidation[0].action = ACTION_DELETE;
            clientIdentity.action = ACTION_SEARCH;
            clientIdentityList.Add(clientIdentity);

            profileReq.manageClientProfileV1Request.manageClientProfileV1Req.clientProfileV1.client.clientIdentity = clientIdentityList.ToArray();

            response = manageClientProfileV1(profileReq, requestOrderRequest);

            if (response != null
                      && response.manageClientProfileV1Response != null
                      && response.manageClientProfileV1Response.standardHeader != null
                      && response.manageClientProfileV1Response.standardHeader.e2e != null
                      && response.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null
                      && !String.IsNullOrEmpty(response.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
            {
                e2eData.logMessage(GotResponseFromDnP, "Ending... Dnp manageClientProfileV1 call");
                e2eDataPassed = response.manageClientProfileV1Response.standardHeader.e2e.E2EDATA;
                e2eData.endOutboundCall(e2eDataPassed);
            }
            else
            {
                e2eData.logMessage(GotResponseFromDnP, "Ending... Dnp manageClientProfileV1 call");
                e2eData.endOutboundCall(e2eData.toString());
            }
            return response;
        }

        public moveClientIdentitiesResponse1 moveBACfromBtOneID(string BillAccountNumber, string BtoneId, OrderRequest requestOrderRequest, ref E2ETransaction e2eData, ref moveClientIdentitiesRequest1 request, bool isEECustomer)
        {
            Dictionary<string, string> identites = new Dictionary<string, string>();
            List<BT.SaaS.IspssAdapter.Dnp.ClientIdentity> MoveClientIdentities = new List<BT.SaaS.IspssAdapter.Dnp.ClientIdentity>();
            BT.SaaS.IspssAdapter.Dnp.ClientIdentity moveCI = null;
            moveClientIdentitiesResponse1 moveCiResponse = null;
            try
            {
                BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock headerBlock = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
                headerBlock.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
                headerBlock.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
                headerBlock.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();

                moveClientIdentitiesRequest1 moveCIRequest1 = new moveClientIdentitiesRequest1();

                MoveClientIdentitiesRequest moveCIRequest = new MoveClientIdentitiesRequest();
                MoveClientIdentitiesReq moveCIReq = new MoveClientIdentitiesReq();
                moveCIReq.isMoveServiceRole = "YES";
                moveCIReq.mciSourceSearchCriteria = new ClientSearchCriteria();
                moveCIReq.mciSourceSearchCriteria.identifierDomainCode = isEECustomer ? EEONLINEID : BTCOM;
                moveCIReq.mciSourceSearchCriteria.identifierValue = BtoneId;

                moveCIReq.mciDestinationSearchCriteria = new ClientSearchCriteria();
                moveCIReq.mciDestinationSearchCriteria.identifierDomainCode = BTCOM;
                moveCIReq.mciDestinationSearchCriteria.identifierValue = "dummy_move_dest_user";

                // including billaccount number
                identites.Add(BillAccountNumber, BACID_IDENTIFER_NAMEPACE);
                foreach (string iden in identites.Keys)
                {
                    moveCI = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                    moveCI.value = iden;
                    moveCI.managedIdentifierDomain = new ManagedIdentifierDomain();
                    moveCI.managedIdentifierDomain.value = identites[iden];

                    MoveClientIdentities.Add(moveCI);
                }
                moveCIReq.clientIdentity = MoveClientIdentities.ToArray();

                moveCIRequest.moveClientIdentitiesReq = moveCIReq;
                moveCIRequest.standardHeader = headerBlock;
                moveCIRequest.standardHeader.e2e = new BT.SaaS.IspssAdapter.Dnp.E2E();
                moveCIRequest.standardHeader.e2e.E2EDATA = e2eData.toString();
                moveCIRequest1.moveClientIdentitiesRequest = moveCIRequest;

                request = moveCIRequest1;
                e2eData.logMessage(StartedDnPCall, "Started... Dnp moveClientIdentities call");
                e2eData.startOutboundCall(ConfigurationManager.AppSettings["destinationDnP"].ToString(), E2ETransaction.vREQ, ACCOUNT_HOLDER_STATUS);

                moveCiResponse = moveClientIdentities(moveCIRequest1, requestOrderRequest);

                if (moveCiResponse != null
                               && moveCiResponse.moveClientIdentitiesResponse != null
                               && moveCiResponse.moveClientIdentitiesResponse.standardHeader != null
                               && moveCiResponse.moveClientIdentitiesResponse.standardHeader.e2e != null
                               && moveCiResponse.moveClientIdentitiesResponse.standardHeader.e2e.E2EDATA != null
                               && !String.IsNullOrEmpty(moveCiResponse.moveClientIdentitiesResponse.standardHeader.e2e.E2EDATA))
                {
                    e2eData.logMessage(GotResponseFromDnP, "Ending... Dnp moveClientIdentities call");
                    e2eDataPassed = moveCiResponse.moveClientIdentitiesResponse.standardHeader.e2e.E2EDATA;
                    e2eData.endOutboundCall(e2eDataPassed);
                }
                else
                {
                    e2eData.logMessage(GotResponseFromDnP, "Ending... Dnp moveClientIdentities call");
                    e2eData.endOutboundCall(e2eData.toString());
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                identites = null;
                MoveClientIdentities = null;
            }
            return moveCiResponse;
        }

        public static bool DoAHTforReinstatement(string BtOneID, string BillAccountNumber, string e2eData)
        {
            string downStreamError = string.Empty;

            createEBillingRoleinBtCom(BillAccountNumber, "Paper Free", BtOneID, ref downStreamError);

            BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock headerblock = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
            headerblock.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
            headerblock.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
            headerblock.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();
            headerblock.serviceState.stateCode = "OK";

            manageClientProfileV1Response1 response;
            manageClientProfileV1Request1 request = new manageClientProfileV1Request1();
            request.manageClientProfileV1Request = new ManageClientProfileV1Request();
            ManageClientProfileV1Req manageClientProfileV1Req = new ManageClientProfileV1Req();

            manageClientProfileV1Req.clientProfileV1 = new ClientProfileV1();
            manageClientProfileV1Req.clientProfileV1.client = new Client();
            manageClientProfileV1Req.clientProfileV1.client.action = ACTION_UPDATE;

            BT.SaaS.IspssAdapter.Dnp.ClientIdentity[] clientIdentity = null;
            clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity[2];

            clientIdentity[0] = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
            clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
            clientIdentity[0].managedIdentifierDomain.value = BTCOM;
            clientIdentity[0].value = BtOneID;
            clientIdentity[0].action = ACTION_SEARCH;

            clientIdentity[1] = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
            clientIdentity[1].managedIdentifierDomain = new ManagedIdentifierDomain();
            clientIdentity[1].managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;
            clientIdentity[1].value = BillAccountNumber;
            clientIdentity[1].clientIdentityStatus = new ClientIdentityStatus();
            clientIdentity[1].clientIdentityStatus.value = ACTIVE;

            List<ClientIdentityValidation> listClientIdentityValidation = new List<ClientIdentityValidation>();
            ClientIdentityValidation clientIdentityValidation = null;

            clientIdentityValidation = new ClientIdentityValidation();
            clientIdentityValidation.name = ACCOUNTTRUSTMETHOD;
            clientIdentityValidation.value = "ProvedOffline";
            clientIdentityValidation.action = ACTION_INSERT;
            listClientIdentityValidation.Add(clientIdentityValidation);

            clientIdentity[1].clientIdentityValidation = listClientIdentityValidation.ToArray();

            clientIdentity[1].action = ACTION_INSERT;

            manageClientProfileV1Req.clientProfileV1.client.clientIdentity = clientIdentity;
            request.manageClientProfileV1Request.manageClientProfileV1Req = manageClientProfileV1Req;
            request.manageClientProfileV1Request.standardHeader = headerblock;

            request.manageClientProfileV1Request.standardHeader.e2e = new BT.SaaS.IspssAdapter.Dnp.E2E();
            request.manageClientProfileV1Request.standardHeader.e2e.E2EDATA = e2eData;
            response = manageClientProfileV1(request);

            if (response != null
                              && response.manageClientProfileV1Response != null
                              && response.manageClientProfileV1Response.standardHeader != null
                              && response.manageClientProfileV1Response.standardHeader.serviceState != null
                              && response.manageClientProfileV1Response.standardHeader.serviceState.stateCode != null
                              && response.manageClientProfileV1Response.standardHeader.serviceState.stateCode == "0")
                return true;
            else
                return false;
        }

        public static bool createEBillingRoleinBtCom(string billingAccountNumber, string ebillLevel, string ebillerID, ref string downStreamError)
        {
            bool isCreateEbillerRoleSuccess = false;
            try
            {
                BT.BTCOM.ManageEBilling.createEbillingServiceResponse1 createEbillingResponse = null;
                downStreamError = string.Empty;
                //preparing the request

                BT.BTCOM.ManageEBilling.createEbillingServiceRequest1 createEbillingRequest = new BT.BTCOM.ManageEBilling.createEbillingServiceRequest1();
                createEbillingRequest.createEbillingServiceRequest = new BT.BTCOM.ManageEBilling.CreateEbillingServiceRequest();
                createEbillingRequest.createEbillingServiceRequest.standardHeader = new BT.BTCOM.ManageEBilling.StandardHeaderBlock();

                BT.BTCOM.ManageEBilling.StandardHeaderBlock standardHeader = new BT.BTCOM.ManageEBilling.StandardHeaderBlock();
                standardHeader.e2e = new BT.BTCOM.ManageEBilling.E2E();
                standardHeader.e2e.E2EDATA = "";

                standardHeader.serviceState = new BT.BTCOM.ManageEBilling.ServiceState();
                standardHeader.serviceState.stateCode = "0K";
                standardHeader.serviceState.resendIndicator = true;
                standardHeader.serviceState.resendIndicatorSpecified = true;
                standardHeader.serviceState.retriesRemaining = "0";

                string messageID = Guid.NewGuid().ToString();

                standardHeader.serviceAddressing = new BT.BTCOM.ManageEBilling.ServiceAddressing();
                standardHeader.serviceAddressing.from = @"http://ccm.intra.bt.com/VasFulfilment";
                standardHeader.serviceAddressing.to = new BT.BTCOM.ManageEBilling.AddressReference();
                standardHeader.serviceAddressing.to.address = @"http://ccm.intra.bt.com/ManageEbilling";
                standardHeader.serviceAddressing.serviceName = @"http://capabilities.nat.bt.com/wsdl/2009/09/ManageEbilling";
                standardHeader.serviceAddressing.messageId = messageID;
                standardHeader.serviceAddressing.action = @"http://capabilities.nat.bt.com/wsdl/2009/09/ManageEbilling#getEbillingUsers";
                standardHeader.serviceAddressing.replyTo = new BT.BTCOM.ManageEBilling.AddressReference();
                standardHeader.serviceAddressing.replyTo.address = @"http://ccm.intra.bt.com/ManageEbilling";
                standardHeader.serviceAddressing.faultTo = new BT.BTCOM.ManageEBilling.AddressReference();
                standardHeader.serviceAddressing.faultTo.address = @"http://ccm.intra.bt.com/ManageEbilling";
                standardHeader.serviceAddressing.relatesTo = @"http://ccm.intra.bt.com/ManageEbilling";

                createEbillingRequest.createEbillingServiceRequest.standardHeader = standardHeader;

                createEbillingRequest.createEbillingServiceRequest.referrer = new BT.BTCOM.ManageEBilling.ReferrerBlock();
                createEbillingRequest.createEbillingServiceRequest.referrer.role = "Consumer_Strategic";

                createEbillingRequest.createEbillingServiceRequest.billingAccount = new BT.BTCOM.ManageEBilling.BillingAccountBlock();
                createEbillingRequest.createEbillingServiceRequest.billingAccount.accountNumber = billingAccountNumber;
                createEbillingRequest.createEbillingServiceRequest.billingAccount.accountType = "Consumer";
                createEbillingRequest.createEbillingServiceRequest.billingAccount.ebillLevel = ebillLevel;

                createEbillingRequest.createEbillingServiceRequest.partyUser = new BT.BTCOM.ManageEBilling.PartyUserBlock();
                createEbillingRequest.createEbillingServiceRequest.partyUser.userName = ebillerID;
                //createEbillingRequest.createEbillingServiceRequest.partyUser.emailAddress = "BTID or Primary Email";


                LogHelper.LogActivityDetails(messageID, Guid.NewGuid(), "CreateEBiller", TimeSpan.Zero, "VASEligilityTrace", XmlHelper.SerializeObject<BT.BTCOM.ManageEBilling.createEbillingServiceRequest1>(createEbillingRequest));


                using (ChannelFactory<BT.BTCOM.ManageEBilling.ManageEbillingPortType> factory = new ChannelFactory<BT.BTCOM.ManageEBilling.ManageEbillingPortType>(@"ManageEbilling"))
                {
                    BT.BTCOM.ManageEBilling.ManageEbillingPortType svc = factory.CreateChannel();
                    createEbillingResponse = svc.createEbillingService(createEbillingRequest);
                    LogHelper.LogActivityDetails(messageID, Guid.NewGuid(), "CreateEBillerResponse", TimeSpan.Zero, "VASEligilityTrace", XmlHelper.SerializeObject<BT.BTCOM.ManageEBilling.createEbillingServiceResponse1>(createEbillingResponse));
                }

                if (createEbillingResponse != null && createEbillingResponse.createEbillingServiceResponse != null && createEbillingResponse.createEbillingServiceResponse.standardHeader != null
                    && createEbillingResponse.createEbillingServiceResponse.standardHeader.serviceState != null && createEbillingResponse.createEbillingServiceResponse.standardHeader.serviceState.stateCode != null
                    && createEbillingResponse.createEbillingServiceResponse.standardHeader.serviceState.stateCode.Equals("Ok", StringComparison.OrdinalIgnoreCase))
                {
                    isCreateEbillerRoleSuccess = true;
                }
                else
                {
                    if (createEbillingResponse != null && createEbillingResponse.createEbillingServiceResponse != null && createEbillingResponse.createEbillingServiceResponse.standardHeader != null
                             && createEbillingResponse.createEbillingServiceResponse.standardHeader.serviceState != null
                             && !string.IsNullOrEmpty(createEbillingResponse.createEbillingServiceResponse.standardHeader.serviceState.errorCode))
                    {
                        downStreamError = string.Format("Errorcode: {0}, ErrorMessage:{1}", createEbillingResponse.createEbillingServiceResponse.standardHeader.serviceState.errorCode, createEbillingResponse.createEbillingServiceResponse.standardHeader.serviceState.errorText);
                        //MCIR.Logger.LogHelper.LogErrorMessage(downStreamError, wfVariables.BPTME2eTxnId, "CreateEbillerRoleinBtCom");
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Trace.Write("Create EBIller Exception" + ex.Message);
            }
            return isCreateEbillerRoleSuccess;
        }
    }

    public class InviteRoleParameters
    {
        private string inviteeBac = string.Empty;
        public string InviteeBac
        {
            get { return inviteeBac; }
            set { inviteeBac = value; }
        }
        private string MSISDN1 = string.Empty;
        public string MSISDN
        {
            get { return MSISDN1; }
            set { MSISDN1 = value; }
        }
        private string RVSID1 = string.Empty;
        public string RVSID
        {
            get { return RVSID1; }
            set { RVSID1 = value; }
        }
        private string IMSI1 = string.Empty;
        public string IMSI
        {
            get { return IMSI1; }
            set { IMSI1 = value; }
        }
        private string roleKey = string.Empty;
        public string RoleKey
        {
            get { return roleKey; }
            set { roleKey = value; }
        }
        private string roleStatus = string.Empty;
        public string RoleStatus
        {
            get { return roleStatus; }
            set { roleStatus = value; }
        }
        private string spownerBac = string.Empty;
        public string SpownerBac
        {
            get { return spownerBac; }
            set { spownerBac = value; }
        }
        //************* for delegate role purpose******
        private string btCom = string.Empty;
        public string BtCom
        {
            get { return btCom; }
            set { btCom = value; }
        }
        private string serviceCode;
        public string ServiceCode
        {
            get { return serviceCode; }
            set { serviceCode = value; }
        }
        private string roleName;
        public string RoleName
        {
            get { return roleName; }
            set { roleName = value; }
        }
        private string ota_Device_Id;
        public string Ota_Device_Id
        {
            get { return ota_Device_Id; }
            set { ota_Device_Id = value; }
        }

    }
}
