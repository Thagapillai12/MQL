using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BT.SaaS.IspssAdapter;
using MSEO = BT.SaaS.MSEOAdapter;
using BT.SaaS.IspssAdapter.Dnp;
using System.Configuration;
using com.bt.util.logging;
using BT.Core.ISVAdapter;
using System.Diagnostics;
using System.Net;
using System.IO;
using System.Xml.Serialization;
using System.Text;

namespace BT.SaaS.MSEOAdapter
{
    public class VoiceInfinityRequestProcessor : InterfaceRequestProcessor
    {
        #region global declare
        const string BacidIdentiferNamepace = "VAS_BILLINGACCOUNT_ID";
        //const string VOICEINF_SERVICECODE_CODE = "IP_VOICE_SERVICE";
        const string Failed = "Failed";
        const string Errored = "Errored";
        const string Accepted = "Accepted";
        const string Ignored = "Ignored";
        const string Completed = "Completed";
        const string VoiceinifnityServicecodeNamepace = "IP_VOICE_SERVICE";
        const string MakingDnPCall = "Making DnP call";
        const string GotResponsefromDNP = "Got Response from DNP";
        const string SendingRequestToDNP = "Sending the Request to DNP";
        const string ReceivedResponsefromDNP = "Recieved Response from DNP";
        const string AcceptedNotificationSent = "Accepted Notification Sent for the Order";
        const string IgnoredNotificationSent = "Ignored Notification Sent for the Order";
        const string CompletedNotificationSent = "Completed Notification Sent for the Order";
        const string FailedNotificationSent = "Failure Notification Sent for the Order";
        const string GotResponseFromDnPWithBusinessError = "GotResponseFromDnPWithBusinessError";
        const string DnPAdminstratorFailedResponse = "Non Functional Exception from DNP(Administrator): ";
        const string FailureResponseFromDnP = "Recieved failure response from DnP";
        const string NullResponseFromDnP = "Response is null from DnP";
        private const string ActionSearch = "SEARCH";
        private const string ActionUpdate = "UPDATE";
        private const string ActionDelete = "DELETE";
        private const string ActionInsert = "INSERT";
        private const string ActionLink = "LINK";
        private const string DefaultRole = "DEFAULT";
        private const string Active = "ACTIVE";
        private const string AdminRole = "ADMIN";
        private const string ActionForceInsert = "FORCE_INS";
        private const string VoiceInfinityClientIdentity = "IP_VOICE_SERVICE_DN";
        private const string VoiceInfinityRVSID = "IP_VOICE_SERVICE_RVSID";

        private const string DeviceID = "SIP_DEVICE";
        private const string SIPUID = "SIP_PRIVATE_UID";
        private const string SIPPWD = "SIP_PASSWORD";
        private const string KEYREF = "KEY_REF_MOTIVE";
        private const string DECRMTDREF = "DECRYPTION_METHOD_REF";

        private const string SIPPWDIMS = "SIP_PASSWORD_IMS";
        private const string KEYREFIMS = "KEY_REF_IMS";
        private const string DECRMTDREFIMS = "DECRYPTION_METHOD_REF_IMS";

        const string GotResponseFromDnP = "GotResponseFromDnP";
        string _orderKey = string.Empty;
        MSEOOrderNotification notification = null;
        bool IsAhtDone = false;
        string _btOneId = string.Empty;
        bool _serviceAlreadyExist = false;
        bool _isExistingAccount = false;
        //Newly Added for Handling IPVS Cancel Orders
        bool _gcp_isAHT = false;
        bool _gcp_serviceAlreadyExist = false;

        string _billAccountNumber = string.Empty;
        string _dnValue = string.Empty;
        string _clientServiceRoleName = string.Empty;
        string _rvsidValue = string.Empty;

        string _deviceidValue = string.Empty;
        string _sipUid = string.Empty;
        string _sipPwd = string.Empty;
        string _refKey = string.Empty;
        string _decrref = string.Empty;
        // added as per BTR-94940
        string _refKeyims = string.Empty;
        string _sipPwdims = string.Empty;
        string _decrrefims = string.Empty;

        #endregion
        /// <summary>
        /// Voice infinity request mapper which accepts OFS input and does rest of actions like create and cease -- Voice infinity
        /// </summary>
        /// <param name="requestOrderRequest"> request order</param>
        /// <param name="e2eData">e2e data</param>
        public override bool RequestMapper(OrderRequest requestOrderRequest, ref E2ETransaction e2eData)
        {
            #region declare
            //MSEOOrderNotification notification = null;
            //string billAccountNumber = string.Empty;
            GetClientProfileV1Res gcpResponse = null;
            //ClientServiceInstanceV1[] gsi_response = null;
            GetBatchProfileV1Res gsupResponse = null;
            string reason = string.Empty;
            bool retstat = false;
            bool createResponse = false;
            bool deleteResponse = false;
            bool modifyResponse = false;
            bool getResponse = false;
            #endregion declare
            try
            {
                _orderKey = requestOrderRequest.Order.OrderIdentifier.Value;
                notification = new MSEOOrderNotification(requestOrderRequest);

                foreach (MSEO.OrderItem orderItem in requestOrderRequest.Order.OrderItem)
                {
                    if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("billaccountnumber", StringComparison.OrdinalIgnoreCase)))
                        _billAccountNumber = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("billaccountnumber", StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Value;

                    if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("DN", StringComparison.OrdinalIgnoreCase)))
                        _dnValue = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("DN", StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Value;

                    if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("RVSID", StringComparison.OrdinalIgnoreCase)))
                        _rvsidValue = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("RVSID", StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Value;

                    if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("Unique_ID", StringComparison.OrdinalIgnoreCase)))
                        _deviceidValue = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("Unique_ID", StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Value;

                    if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("SIP_UiD", StringComparison.OrdinalIgnoreCase)))
                        _sipUid = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("SIP_UiD", StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Value;

                    if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("SIP_Password", StringComparison.OrdinalIgnoreCase)))
                        _sipPwd = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("SIP_Password", StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Value;

                    if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("Key_Ref_Motive", StringComparison.OrdinalIgnoreCase)))
                        _refKey = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("Key_Ref_Motive", StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Value;

                    if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("Decryption_Method_Ref", StringComparison.OrdinalIgnoreCase)))
                        _decrref = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("Decryption_Method_Ref", StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Value;

                    if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("KEY_REF_IMS", StringComparison.OrdinalIgnoreCase)))
                        _refKeyims = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("KEY_REF_IMS", StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Value;

                    if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("SIP_PASSWORD_IMS", StringComparison.OrdinalIgnoreCase)))
                        _sipPwdims = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("SIP_PASSWORD_IMS", StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Value;

                    if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("DECRYPTION_METHOD_REF_IMS", StringComparison.OrdinalIgnoreCase)))
                        _decrrefims = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("DECRYPTION_METHOD_REF_IMS", StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Value;

                    if (!string.IsNullOrEmpty(orderItem.Action.Reason))
                        reason = orderItem.Action.Reason;

                    if (!String.IsNullOrEmpty(_billAccountNumber))
                    {
                        // check for validation else skip and write back the error saying MSEO cannot process the request
                        if (IsRequestValid(requestOrderRequest))
                        {
                            retstat = true;
                            // Create/provide orders
                            // First step check for valid request ?
                            notification.sendNotification(true, true, string.Empty, string.Empty, ref e2eData, false);
                            Logger.Write(_orderKey + "," + Accepted + "," + AcceptedNotificationSent, Logger.TypeEnum.VoInfinityTrace);

                            //now the functionality..
                            //step -1 check if the user is AHT or NOT and check for services (if exists dont need to add again - duplicate) - call getclient profile

                            InterfaceRequestProcessor objresprs = new VoiceInfinityRequestProcessor();
                            Logger.Write(_orderKey + ",Making DNP call - start,Sending the Request to DNP - Provide journey ", Logger.TypeEnum.VoInfinityTrace);
                            gcpResponse = objresprs.GetClientProfile(_billAccountNumber, BacidIdentiferNamepace, _orderKey, "S0341469");


                            if (gcpResponse != null && gcpResponse.clientProfileV1 != null && gcpResponse.clientProfileV1.client != null && gcpResponse.clientProfileV1.client.clientIdentity != null)
                            {
                                #region check aht and service
                                if (gcpResponse.clientProfileV1.client.clientIdentity.ToList().Exists(i => i.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)))
                                {
                                    _btOneId = gcpResponse.clientProfileV1.client.clientIdentity.ToList().Where(i => i.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                }
                                if (gcpResponse.clientProfileV1.client.clientIdentity.ToList().Exists(i => i.managedIdentifierDomain.value.Equals(BacidIdentiferNamepace, StringComparison.OrdinalIgnoreCase) && i.value.Equals(_billAccountNumber, StringComparison.OrdinalIgnoreCase)))
                                {
                                    BT.SaaS.IspssAdapter.Dnp.ClientIdentity bacClientIdentity = gcpResponse.clientProfileV1.client.clientIdentity.ToList().Where(i => i.managedIdentifierDomain.value.Equals(BacidIdentiferNamepace, StringComparison.OrdinalIgnoreCase) && i.value.Equals(_billAccountNumber, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                                    if (bacClientIdentity.clientIdentityValidation != null)
                                    {
                                        //AHT
                                        IsAhtDone = bacClientIdentity.clientIdentityValidation.ToList().Exists(i => i.name.Equals("ACCOUNTTRUSTMETHOD", StringComparison.OrdinalIgnoreCase) && ConfigurationManager.AppSettings["AccountTrustMethodStatus"].Split(',').Contains(i.value.ToLower()));
                                        _gcp_isAHT = true;
                                    }
                                    _isExistingAccount = true;
                                    
                                }
                                //check if voinfinity service exists
                                if (gcpResponse.clientProfileV1.clientServiceInstanceV1 != null && gcpResponse.clientProfileV1.clientServiceInstanceV1.ToList().Exists(ip => ip.clientServiceInstanceIdentifier.value.Equals(VoiceinifnityServicecodeNamepace, StringComparison.OrdinalIgnoreCase)
                                    && ip.serviceIdentity.ToList().Exists(sIden => sIden.domain.Equals(BacidIdentiferNamepace, StringComparison.OrdinalIgnoreCase) && sIden.value.Equals(_billAccountNumber, StringComparison.OrdinalIgnoreCase))))
                                {
                                    _serviceAlreadyExist = true;
                                    _gcp_serviceAlreadyExist = true;

                                }

                                if (orderItem.Action.Code.Equals("cancel", StringComparison.OrdinalIgnoreCase))
                                {
                                    if (_gcp_isAHT || _gcp_serviceAlreadyExist)
                                    {
                                        _serviceAlreadyExist = true;
                                    }
                                }

                                #endregion
                            }
                            else
                            {
                                gsupResponse = objresprs.GetServiceUserProfiles(_billAccountNumber, BacidIdentiferNamepace, _orderKey);
                                if (gsupResponse != null && gsupResponse.clientProfileV1 != null)
                                {
                                    foreach (ClientProfileV1 clientProfile in gsupResponse.clientProfileV1)
                                    {
                                        if (clientProfile.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(BacidIdentiferNamepace, StringComparison.OrdinalIgnoreCase) && ci.value.Equals(_billAccountNumber, StringComparison.OrdinalIgnoreCase)))
                                        {
                                            _isExistingAccount = true;

                                            BT.SaaS.IspssAdapter.Dnp.ClientIdentity bacClientIdentity = clientProfile.client.clientIdentity.ToList().Where(i => i.managedIdentifierDomain.value.Equals(BacidIdentiferNamepace, StringComparison.OrdinalIgnoreCase) && i.value.Equals(_billAccountNumber, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                                            if (bacClientIdentity.clientIdentityValidation != null)
                                                IsAhtDone = bacClientIdentity.clientIdentityValidation.ToList().Exists(i => i.name.Equals("ACCOUNTTRUSTMETHOD", StringComparison.OrdinalIgnoreCase) && ConfigurationManager.AppSettings["AccountTrustMethodStatus"].Split(',').Contains(i.value.ToLower()));
                                        }
                                        if (clientProfile.clientServiceInstanceV1 != null && clientProfile.clientServiceInstanceV1.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(VoiceinifnityServicecodeNamepace, StringComparison.OrdinalIgnoreCase)
                                            && si.serviceIdentity.ToList().Exists(sIden => sIden.domain.Equals(BacidIdentiferNamepace, StringComparison.OrdinalIgnoreCase) && sIden.value.Equals(_billAccountNumber, StringComparison.OrdinalIgnoreCase))))
                                        {
                                            _serviceAlreadyExist = clientProfile.clientServiceInstanceV1.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(VoiceinifnityServicecodeNamepace, StringComparison.OrdinalIgnoreCase));
                                            _gcp_serviceAlreadyExist = true;
                                        }
                                    }
                                }
                            }
                            if ((orderItem.Action.Code.Equals("create", StringComparison.OrdinalIgnoreCase)) && (string.IsNullOrEmpty(reason)))
                            {
                                createResponse = CreateOrder(orderItem, ref e2eData);
                            }
                            else if ((orderItem.Action.Code.Equals("create", StringComparison.OrdinalIgnoreCase)) && (!string.IsNullOrEmpty(reason)) && (reason.Equals("GetSIPCredentials", StringComparison.OrdinalIgnoreCase)))
                            {
                                getResponse = GetSIPDeviceDetails(orderItem, ref e2eData);
                               
                            }
                            else if ((orderItem.Action.Code.Equals("Modify", StringComparison.OrdinalIgnoreCase)))
                            {
                                if (reason.Equals("SIPChange", StringComparison.OrdinalIgnoreCase))
                                {
                                    if (_serviceAlreadyExist)
                                        modifyResponse = ModifySIPDetails(orderItem, ref e2eData);
                                    else
                                    {
                                        //ignore
                                        notification.sendNotification(false, false, "MSEO_noDVservice_001", "IP Vocie service is not existing at DNP", ref e2eData, true);
                                        Logger.Write(_orderKey + "," + Ignored + "," + IgnoredNotificationSent, Logger.TypeEnum.SpringMessageTrace);
                                    }
                                }
                                else
                                    modifyResponse = ModifyVoiceInfinity(orderItem, ref e2eData);
                            }
                            else if ((orderItem.Action.Code.Equals("cancel", StringComparison.OrdinalIgnoreCase)))
                            {
                                deleteResponse = DeleteOrder(orderItem, ref e2eData);
                            }
                            //cancel
                            _gcp_serviceAlreadyExist = false;
                            _gcp_isAHT = false;
                        }
                        else
                        {
                            notification.sendNotification(false, false, "MSEO_malformedorder_ 002", "Invalid request, some madatory attributes missing", ref e2eData, true);
                            Logger.Write(_orderKey + "," + Ignored + "," + IgnoredNotificationSent, Logger.TypeEnum.VoInfinityTrace);
                        }
                    }
                    else
                    {
                        notification.sendNotification(false, false, "MSEO_ missingBAC_001", "Invalid request, BAC is missing ", ref e2eData, true);
                        Logger.Write(_orderKey + "," + Ignored + "," + IgnoredNotificationSent, Logger.TypeEnum.VoInfinityTrace);
                    }
                }
            }
            catch (BT.SaaS.IspssAdapter.DnpException excep)
            {
                retstat = false;
                if (excep.Message.Contains("Administrator"))
                {
                    notification.sendNotification(false, false, "777_PROFILE_-1", excep.Message, ref e2eData, true);
                }
                else if (excep.Message.Contains("connection"))
                {
                    notification.sendNotification(false, false, "DNP_connectfailed_777_01", excep.Message, ref e2eData, true);
                }
                else if (excep.Message.Contains("Anonymous"))
                {
                    notification.sendNotification(false, false, "DNP_authfailed_777_02", excep.Message, ref e2eData, true);
                }
                else
                    notification.sendNotification(false, false, "777", excep.Message, ref e2eData, true);
                Logger.Write(_orderKey + "," + Errored + "," + FailedNotificationSent + "," + excep.Message, Logger.TypeEnum.VoInfinityTrace);
                Logger.Write(_orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.VoInfinityTrace);
            }
            catch (Exception ex)
            {
                retstat = false;
                notification.sendNotification(false, false, "001", ex.Message, ref e2eData, true);
                Logger.Write(_orderKey + "," + Errored + "," + FailedNotificationSent + "," + ex.Message, Logger.TypeEnum.VoInfinityTrace);
                Logger.Write(_orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.VoInfinityTrace);
            }
            finally
            {
                //dnpParameters = null;
            }
            return retstat;
        }

        /// <summary>
        /// Provide\create a voice Infinity product
        /// </summary>
        /// <param name="orderItem"></param>
        /// <param name="e2eData"></param>
        /// <returns></returns>
        public bool CreateOrder(MSEO.OrderItem orderItem, ref E2ETransaction e2eData)
        {
            #region declare
            bool res = false;
            //GetClientProfileV1Res gcpResponse = null;
            //ClientServiceInstanceV1[] gsiResponse = null;
            //GetBatchProfileV1Res gsupResponse = null;
            //bool retstat = false;
            #endregion declare
            try
            {
                # region Create
                if ((orderItem.Action.Code.Equals("create", StringComparison.OrdinalIgnoreCase)))
                {
                    if (!_serviceAlreadyExist)
                    {
                        manageClientProfileV1Response1 profileResponse1 = null;
                        manageClientProfileV1Request1 profileRequest = new manageClientProfileV1Request1
                        {
                            manageClientProfileV1Request = new ManageClientProfileV1Request()
                        };
                        BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock headerBlock =
                            new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock
                            {
                                serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing
                                {
                                    @from = "http://www.profile.com?SAASMSEO"
                                }
                            };
                        headerBlock.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();

                        ManageClientProfileV1Req manageClientProfileV1Req1 = new ManageClientProfileV1Req
                        {
                            clientProfileV1 = new ClientProfileV1()
                        };

                        List<ClientIdentity> clientIdentityList = new List<ClientIdentity>();

                        ClientServiceRole clientSrvcRole = null;
                        List<ClientServiceRole> listClientSrvcRole = new List<ClientServiceRole>();
                        ClientIdentity clientIdentity = null;

                        clientIdentity = new ClientIdentity();
                        clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain
                        {
                            value = VoiceInfinityClientIdentity
                        };
                        clientIdentity.value = _dnValue;
                        clientIdentity.clientIdentityStatus = new ClientIdentityStatus { value = Active };
                        clientIdentity.action = ActionInsert;
                        clientIdentityList.Add(clientIdentity);

                        clientIdentity = new ClientIdentity();
                        clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain
                        {
                            value = VoiceInfinityRVSID
                        };
                        clientIdentity.value = _rvsidValue;
                        clientIdentity.clientIdentityStatus = new ClientIdentityStatus { value = Active };
                        clientIdentity.action = ActionInsert;
                        clientIdentityList.Add(clientIdentity);

                        // SIP credentials also added as client Identity.
                        List<ClientIdentityValidation> listclientidntyvalidation = new List<ClientIdentityValidation>();
                        ClientIdentityValidation clientIdentityValidation = null;

                        clientIdentity = new ClientIdentity();
                        clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain
                        {
                            value = DeviceID
                        };
                        clientIdentity.value = _deviceidValue;
                        clientIdentity.clientIdentityStatus = new ClientIdentityStatus { value = Active };
                        clientIdentity.action = ActionInsert;

                        clientIdentityValidation = new ClientIdentityValidation();
                        clientIdentityValidation.name = SIPUID;
                        clientIdentityValidation.value = _sipUid;
                        clientIdentityValidation.action = ActionInsert;
                        listclientidntyvalidation.Add(clientIdentityValidation);

                        clientIdentityValidation = new ClientIdentityValidation();
                        clientIdentityValidation.name = SIPPWD;
                        clientIdentityValidation.value = _sipPwd;
                        clientIdentityValidation.action = ActionInsert;
                        listclientidntyvalidation.Add(clientIdentityValidation);

                        clientIdentityValidation = new ClientIdentityValidation();
                        clientIdentityValidation.name = KEYREF;
                        clientIdentityValidation.value = _refKey;
                        clientIdentityValidation.action = ActionInsert;
                        listclientidntyvalidation.Add(clientIdentityValidation);

                        clientIdentityValidation = new ClientIdentityValidation();
                        clientIdentityValidation.name = DECRMTDREF;
                        clientIdentityValidation.value = _decrref;
                        clientIdentityValidation.action = ActionInsert;
                        listclientidntyvalidation.Add(clientIdentityValidation);

                        clientIdentityValidation = new ClientIdentityValidation();
                        clientIdentityValidation.name = SIPPWDIMS;
                        clientIdentityValidation.value = _sipPwdims;
                        clientIdentityValidation.action = ActionInsert;
                        listclientidntyvalidation.Add(clientIdentityValidation);

                        clientIdentityValidation = new ClientIdentityValidation();
                        clientIdentityValidation.name = KEYREFIMS;
                        clientIdentityValidation.value = _refKeyims;
                        clientIdentityValidation.action = ActionInsert;
                        listclientidntyvalidation.Add(clientIdentityValidation);

                        clientIdentityValidation = new ClientIdentityValidation();
                        clientIdentityValidation.name = DECRMTDREFIMS;
                        clientIdentityValidation.value = _decrrefims;
                        clientIdentityValidation.action = ActionInsert;
                        listclientidntyvalidation.Add(clientIdentityValidation);

                        clientIdentity.clientIdentityValidation = listclientidntyvalidation.ToArray();
                        clientIdentityList.Add(clientIdentity);

                        ServiceIdentity[] serviceIdentity = new ServiceIdentity[1];
                        serviceIdentity[0] = new ServiceIdentity
                        {
                            domain = BacidIdentiferNamepace,
                            value = _billAccountNumber,
                            action = ActionLink
                        };

                        ClientServiceInstanceV1[] clientServiceInstanceV1 = new ClientServiceInstanceV1[1];
                        clientServiceInstanceV1[0] = new ClientServiceInstanceV1
                        {
                            clientServiceInstanceIdentifier =
                                new ClientServiceInstanceIdentifier { value = VoiceinifnityServicecodeNamepace },
                            clientServiceInstanceStatus = new ClientServiceInstanceStatus
                            {
                                value = Active
                            },
                            serviceIdentity = serviceIdentity,
                            action = ActionInsert
                        };

                        if (_isExistingAccount)
                        {
                            manageClientProfileV1Req1.clientProfileV1.client = new Client { action = ActionUpdate };

                            clientIdentity = new ClientIdentity
                            {
                                managedIdentifierDomain = new ManagedIdentifierDomain
                                {
                                    value = BacidIdentiferNamepace
                                },
                                value = _billAccountNumber,
                                action = ActionSearch
                            };

                            clientIdentityList.Add(clientIdentity);

                            if (IsAhtDone)
                            {
                                clientSrvcRole = new ClientServiceRole
                                {
                                    id = AdminRole,
                                    clientServiceRoleStatus = new ClientServiceRoleStatus { value = Active },
                                    clientIdentity = new ClientIdentity[1]
                                };
                                clientSrvcRole.clientIdentity[0] = new ClientIdentity
                                {
                                    managedIdentifierDomain = new ManagedIdentifierDomain
                                    {
                                        value = BacidIdentiferNamepace
                                    },
                                    value = _billAccountNumber,
                                    action = ActionInsert
                                };
                                clientSrvcRole.action = ActionInsert;
                                listClientSrvcRole.Add(clientSrvcRole);

                            }
                        }
                        else
                        {
                            manageClientProfileV1Req1.clientProfileV1.client = new Client
                            {
                                action = ActionInsert,
                                clientOrganisation = new ClientOrganisation { id = "BTRetailConsumer" },
                                type = "CUSTOMER",
                                clientStatus = new ClientStatus
                                {
                                    value = "ACTIVE"
                                }
                            };

                        }

                        clientSrvcRole = new ClientServiceRole
                        {
                            id = DefaultRole,
                            clientServiceRoleStatus = new ClientServiceRoleStatus { value = Active },
                            clientIdentity = new ClientIdentity[3]
                        };

                        clientSrvcRole.clientIdentity[0] = new ClientIdentity
                        {
                            managedIdentifierDomain = new ManagedIdentifierDomain
                            {
                                value = VoiceInfinityClientIdentity
                            },
                            value = _dnValue,
                            action = ActionInsert
                        };
                        clientSrvcRole.clientIdentity[1] = new ClientIdentity
                        {
                            managedIdentifierDomain = new ManagedIdentifierDomain
                            {
                                value = VoiceInfinityRVSID
                            },
                            value = _rvsidValue,
                            action = ActionInsert
                        };
                        clientSrvcRole.clientIdentity[2] = new ClientIdentity
                        {
                            managedIdentifierDomain = new ManagedIdentifierDomain
                            {
                                value = DeviceID
                            },
                            value = _deviceidValue,
                            action = ActionInsert
                        };
                        clientSrvcRole.action = ActionInsert;
                        listClientSrvcRole.Add(clientSrvcRole);

                        clientServiceInstanceV1[0].clientServiceRole = listClientSrvcRole.ToArray();
                        manageClientProfileV1Req1.clientProfileV1.client.clientIdentity = clientIdentityList.ToArray();
                        manageClientProfileV1Req1.clientProfileV1.clientServiceInstanceV1 = clientServiceInstanceV1;

                        profileRequest.manageClientProfileV1Request.standardHeader = headerBlock;
                        profileRequest.manageClientProfileV1Request.manageClientProfileV1Req = manageClientProfileV1Req1;

                        // e2eData.logMessage(StartedDnPCall, "");
                        e2eData.startOutboundCall(ConfigurationManager.AppSettings["destinationDnP"].ToString(), E2ETransaction.vREQ, VoiceinifnityServicecodeNamepace);

                        profileRequest.manageClientProfileV1Request.standardHeader.e2e =
                            new BT.SaaS.IspssAdapter.Dnp.E2E { E2EDATA = e2eData.toString() };

                        Logger.Write(_orderKey + "," + MakingDnPCall + "MCP call to create order" + "," + SendingRequestToDNP, Logger.TypeEnum.VoInfinityTrace);
                        MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_IN_LOG, _orderKey, profileRequest.SerializeObject(), "S0341469");
                        InterfaceRequestProcessor objresp = new VoiceInfinityRequestProcessor();

                        //profileResponse1 = SpringDnpWrapper.manageClientProfileV1ForSpring(profileRequest, orderKey);
                        profileResponse1 = objresp.ManageClientProfile(profileRequest, _orderKey);

                        Logger.Write(_orderKey + "," + GotResponsefromDNP + "," + ReceivedResponsefromDNP, Logger.TypeEnum.VoInfinityTrace);
                        MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_OUT_LOG, _orderKey, profileResponse1.SerializeObject(), "S0341469");

                        #region response
                        if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null
                   && profileResponse1.manageClientProfileV1Response.standardHeader != null
                   && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState != null
                   && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState.stateCode != null
                   && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState.stateCode == "0")
                        {
                            res = true;
                            e2eData.logMessage(GotResponseFromDnP, "");
                            if (profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && !string.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                            {
                                e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                            }
                            else
                                e2eData.endOutboundCall(e2eData.toString());

                            notification.sendNotification(true, false, "004", string.Empty, ref e2eData, true);
                            Logger.Write(_orderKey + "," + Completed + "," + CompletedNotificationSent, Logger.TypeEnum.VoInfinityTrace);
                        }
                        else
                        {
                            if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null
                                && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res != null
                                && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages != null
                                && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0] != null
                                && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description != null)
                            {
                                string errorcode = string.Empty;
                                string errorMessage = profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description;
                                if (profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].messageCode != null)
                                    errorcode = profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].messageCode;
                                e2eData.businessError(GotResponseFromDnPWithBusinessError, errorMessage);
                                if (profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && !string.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                                    e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                                else
                                    e2eData.endOutboundCall(e2eData.toString());

                                if (errorMessage.Contains("Administrator"))
                                {
                                    Logger.Write(_orderKey + "," + Errored + "," + DnPAdminstratorFailedResponse + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.VoInfinityTrace);
                                    Logger.Write(_orderKey + "," + Failed + "," + FailureResponseFromDnP + "," + errorMessage, Logger.TypeEnum.VoInfinityTrace);
                                    notification.sendNotification(false, false, "777_PROFILE_-1", errorMessage, ref e2eData, true);
                                    Logger.Write(_orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.VoInfinityTrace);
                                }
                                else if (errorMessage.Contains("connection"))
                                {
                                    Logger.Write(_orderKey + "," + Errored + "," + DnPAdminstratorFailedResponse + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.VoInfinityTrace);
                                    Logger.Write(_orderKey + "," + Failed + "," + FailureResponseFromDnP + "," + errorMessage, Logger.TypeEnum.VoInfinityTrace);
                                    notification.sendNotification(false, false, "DNP_connectfailed_777_01", errorMessage, ref e2eData, true);
                                    Logger.Write(_orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.VoInfinityTrace);
                                }
                                else if (errorMessage.Contains("Anonymous"))
                                {
                                    Logger.Write(_orderKey + "," + Errored + "," + DnPAdminstratorFailedResponse + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.VoInfinityTrace);
                                    Logger.Write(_orderKey + "," + Failed + "," + FailureResponseFromDnP + "," + errorMessage, Logger.TypeEnum.VoInfinityTrace);
                                    notification.sendNotification(false, false, "DNP_authfailed_777_02", errorMessage, ref e2eData, true);
                                    Logger.Write(_orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.VoInfinityTrace);
                                }
                                else
                                {
                                    Logger.Write(_orderKey + "," + Failed + "," + FailureResponseFromDnP + "," + errorMessage, Logger.TypeEnum.VoInfinityTrace);
                                    notification.sendNotification(false, false, "777_" + errorcode, errorMessage, ref e2eData, true);
                                    Logger.Write(_orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.VoInfinityTrace);
                                }
                                //notification.sendNotification(false, false, "777", errorMessage, ref e2eData,true);
                                //Logger.Write(_orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.VoInfinityTrace);
                            }
                            else
                            {
                                Logger.Write(_orderKey + "," + Failed + "," + NullResponseFromDnP, Logger.TypeEnum.VoInfinityTrace);

                                e2eData.businessError(GotResponseFromDnPWithBusinessError, "Response is null from DnP");
                                if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                                    e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                                else
                                    e2eData.endOutboundCall(e2eData.toString());

                                notification.sendNotification(false, false, "MSEO_nullDNPresponse_777", "Response is null from DnP", ref e2eData, true);
                                Logger.Write(_orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.VoInfinityTrace);
                            }
                        }
                        #endregion response

                    }
                    else
                    {
                        //ignore
                        notification.sendNotification(false, false, "DNP_svcalreadyexists_001", "IP Voice service already in the DNP, so cannot do provide on the BAC", ref e2eData, true);
                        Logger.Write(_orderKey + "," + Ignored + "," + IgnoredNotificationSent, Logger.TypeEnum.VoInfinityTrace);
                    }
                }
                # endregion Create
            }
            catch (BT.SaaS.IspssAdapter.DnpException excep)
            {
                res = false;
                if (excep.Message.Contains("Administrator"))
                {
                    notification.sendNotification(false, false, "777_PROFILE_-1", excep.Message, ref e2eData, true);
                }
                else if (excep.Message.Contains("connection"))
                {
                    notification.sendNotification(false, false, "DNP_connectfailed_777_01", excep.Message, ref e2eData, true);
                }
                else if (excep.Message.Contains("Anonymous"))
                {
                    notification.sendNotification(false, false, "DNP_authfailed_777_02", excep.Message, ref e2eData, true);
                }
                else
                    notification.sendNotification(false, false, "777", excep.Message, ref e2eData, true);
                Logger.Write(_orderKey + "," + Errored + "," + FailedNotificationSent + "," + excep.Message, Logger.TypeEnum.VoInfinityTrace);
                Logger.Write(_orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.VoInfinityTrace);
            }
            catch (Exception ex)
            {
                res = false;
                notification.sendNotification(false, false, "001", ex.Message, ref e2eData, true);
                Logger.Write(_orderKey + "," + Errored + "," + FailedNotificationSent + "," + ex.Message, Logger.TypeEnum.VoInfinityTrace);
                Logger.Write(_orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.VoInfinityTrace);
            }
            finally
            {
                //dnpParameters = null;
            }
            return res;
        }

        /// <summary>
        /// Delete\create a voice Infinity product
        /// </summary>
        /// <param name="orderItem"></param>
        /// <param name="e2eData"></param>
        /// <returns></returns>
        public bool DeleteOrder(MSEO.OrderItem orderItem, ref E2ETransaction e2eData)
        {
            bool res = false;
            manageClientProfileV1Response1 profileResponse1;
            manageClientProfileV1Request1 profileRequest = new manageClientProfileV1Request1
            {
                manageClientProfileV1Request = new ManageClientProfileV1Request()
            };

            BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock headerBlock =
                new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock
                {
                    serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing
                    {
                        @from = "http://www.profile.com?SAASMSEO"
                    },
                    serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState()
                };
            List<ClientServiceInstanceV1> clientserviceinstanceList = new List<ClientServiceInstanceV1>();
            List<ClientIdentity> clientIdentityList = new List<ClientIdentity>();
            try
            {
                if ((orderItem.Action.Code.Equals("cancel", StringComparison.OrdinalIgnoreCase)))
                {
                    if (_serviceAlreadyExist)
                    {
                        ManageClientProfileV1Req manageClientProfileV1Req = new ManageClientProfileV1Req
                        {
                            clientProfileV1 = new ClientProfileV1()
                        };
                        ClientIdentity clientIdentity = null;
                        //delete service
                        #region ahtdone
                        manageClientProfileV1Req.clientProfileV1.client = new Client();

                        //Newly added here for IPVS deletion 07-11-2024
                        //For both BAC and the DN are not available in the same profile and also Standalone profile
                        if (!_gcp_isAHT || !_gcp_serviceAlreadyExist)//Gsup call
                        {
                            manageClientProfileV1Req.clientProfileV1.client.action = ActionDelete;
                            clientIdentity = new ClientIdentity
                            {
                                value = _dnValue,
                                managedIdentifierDomain = new ManagedIdentifierDomain
                                {
                                    value = VoiceInfinityClientIdentity
                                },
                                action = ActionSearch
                            };
                            clientIdentityList.Add(clientIdentity);


                            ServiceIdentity[] serviceIdentity1 = new ServiceIdentity[1];
                            serviceIdentity1[0] = new ServiceIdentity
                            {
                                domain = BacidIdentiferNamepace,
                                value = _billAccountNumber,
                                action = ActionSearch
                            };

                            ClientServiceInstanceV1 clientServiceInstance1V1 = new ClientServiceInstanceV1();
                            clientServiceInstance1V1 = new ClientServiceInstanceV1
                            {
                                clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier
                                {
                                    value = VoiceinifnityServicecodeNamepace
                                },
                                serviceIdentity = serviceIdentity1,
                                action = ActionDelete
                            };
                            clientserviceinstanceList.Add(clientServiceInstance1V1);
                            
                            //Here we need to delete MCP call with DN

                            //DNP call for MCP using DN
                            manageClientProfileV1Req.clientProfileV1.client.clientIdentity = clientIdentityList.ToArray();
                            manageClientProfileV1Req.clientProfileV1.clientServiceInstanceV1 = clientserviceinstanceList.ToArray();

                            profileRequest.manageClientProfileV1Request.manageClientProfileV1Req = manageClientProfileV1Req;
                            profileRequest.manageClientProfileV1Request.standardHeader = headerBlock;
                            InterfaceRequestProcessor objrep1 = new VoiceInfinityRequestProcessor();

                            Logger.Write(_orderKey + "," + MakingDnPCall + "ManageClientProfile to deleteOrder using DN" + "," + SendingRequestToDNP, Logger.TypeEnum.VoInfinityTrace);
                            MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_IN_LOG, _orderKey, profileRequest.SerializeObject(), "S0341469");

                            profileResponse1 = objrep1.ManageClientProfile(profileRequest, _orderKey);

                            Logger.Write(_orderKey + "," + GotResponsefromDNP + "," + ReceivedResponsefromDNP, Logger.TypeEnum.VoInfinityTrace);
                            MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_OUT_LOG, _orderKey, profileResponse1.SerializeObject(), "S0341469");

                            if (profileResponse1 != null
                                && profileResponse1.manageClientProfileV1Response != null
                                && profileResponse1.manageClientProfileV1Response.standardHeader != null
                                && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState != null
                                && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState.stateCode != null
                                && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState.stateCode == "0")

                            {
                                res = true;

                                e2eData.logMessage(GotResponseFromDnP, "");
                                if (profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && !string.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                                {
                                    e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                                }
                                else
                                {
                                    e2eData.endOutboundCall(e2eData.toString());
                                }

                                // for Service deletion
                                MSIForServiceInstanceDeletion("IP_VOICE_SERVICE", e2eData);
                            }//MSI Call failed with DN
                            else
                            {
                                //
                                if (profileResponse1.manageClientProfileV1Response != null
                                && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res != null
                                && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages != null
                                && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0] != null
                                && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description != null)
                                {
                                    string errorcode = string.Empty;
                                    string errorMessage = profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description;

                                    // Here already client Identities deleted still service is for that we are using MSI ==>for Service deletion 
                                    MSIForServiceInstanceDeletion("IP_VOICE_SERVICE", e2eData);
                                    if (profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].messageCode != null)
                                        errorcode = profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].messageCode;
                                    e2eData.businessError(GotResponseFromDnPWithBusinessError, errorMessage);
                                    if (profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && !string.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                                        e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                                    else
                                        e2eData.endOutboundCall(e2eData.toString());
                                }

                            }

                        }

                        //ccp 78 - MD2 changes
                        //if (IsAhtDone || _isExistingAccount)
                        //if (IsAhtDone) //Single Profile with AHT and IPVS 
                        else if (_gcp_isAHT && _gcp_serviceAlreadyExist)
                         {
                            manageClientProfileV1Req.clientProfileV1.client.action = ActionUpdate;

                            clientIdentity = new ClientIdentity
                            {
                                value = _billAccountNumber,
                                managedIdentifierDomain = new ManagedIdentifierDomain
                                {
                                    value = BacidIdentiferNamepace
                                },
                                action = ActionSearch
                            };
                            clientIdentityList.Add(clientIdentity);

                            clientIdentity = new ClientIdentity
                            {
                                managedIdentifierDomain = new ManagedIdentifierDomain
                                {
                                    value = VoiceInfinityClientIdentity
                                },
                                value = _dnValue,
                                clientIdentityStatus = new ClientIdentityStatus { value = Active },
                                action = ActionDelete
                            };
                            clientIdentityList.Add(clientIdentity);

                            clientIdentity = new ClientIdentity
                            {
                                managedIdentifierDomain = new ManagedIdentifierDomain
                                {
                                    value = VoiceInfinityRVSID
                                },
                                value = _rvsidValue,
                                clientIdentityStatus = new ClientIdentityStatus { value = Active },
                                action = ActionDelete
                            };
                            clientIdentityList.Add(clientIdentity);

                            if (!string.IsNullOrEmpty(_deviceidValue))
                            {
                                clientIdentity = new ClientIdentity
                                {
                                    managedIdentifierDomain = new ManagedIdentifierDomain
                                    {
                                        value = DeviceID
                                    },
                                    value = _deviceidValue,
                                    clientIdentityStatus = new ClientIdentityStatus { value = Active },
                                    action = ActionDelete
                                };
                                clientIdentityList.Add(clientIdentity);
                            }



                            // else
                            //{                        
                            //    //delete stand alone profile
                            //    manageClientProfileV1Req.clientProfileV1.client.action = ActionDelete;
                            //    clientIdentity = new ClientIdentity
                            //    {
                            //        value = _dnValue,
                            //        managedIdentifierDomain = new ManagedIdentifierDomain
                            //        {
                            //            value = VoiceInfinityClientIdentity
                            //        },
                            //        action = ActionSearch
                            //    };
                            //    clientIdentityList.Add(clientIdentity);
                            //}

                            ServiceIdentity[] serviceIdentity = new ServiceIdentity[1];
                            serviceIdentity[0] = new ServiceIdentity
                            {
                                domain = BacidIdentiferNamepace,
                                value = _billAccountNumber,
                                action = ActionSearch
                            };

                            ClientServiceInstanceV1 clientServiceInstanceV1 = new ClientServiceInstanceV1();
                            clientServiceInstanceV1 = new ClientServiceInstanceV1
                            {
                                clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier
                                {
                                    value = VoiceinifnityServicecodeNamepace
                                },
                                serviceIdentity = serviceIdentity,
                                action = ActionDelete
                            };
                            clientserviceinstanceList.Add(clientServiceInstanceV1);

                            //List<ClientServiceRole> clientServiceRoleList = new List<ClientServiceRole>();
                            //ClientServiceRole clientServiceRole = new ClientServiceRole();
                            //;
                            #endregion



                            #region Call DnP
                            manageClientProfileV1Req.clientProfileV1.client.clientIdentity = clientIdentityList.ToArray();
                            manageClientProfileV1Req.clientProfileV1.clientServiceInstanceV1 = clientserviceinstanceList.ToArray();

                            profileRequest.manageClientProfileV1Request.manageClientProfileV1Req = manageClientProfileV1Req;
                            profileRequest.manageClientProfileV1Request.standardHeader = headerBlock;
                            InterfaceRequestProcessor objrep = new VoiceInfinityRequestProcessor();

                            Logger.Write(_orderKey + "," + MakingDnPCall + "ManageClientProfile to deleteOrder" + "," + SendingRequestToDNP, Logger.TypeEnum.VoInfinityTrace);
                            MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_IN_LOG, _orderKey, profileRequest.SerializeObject(), "S0341469");

                            profileResponse1 = objrep.ManageClientProfile(profileRequest, _orderKey);

                            Logger.Write(_orderKey + "," + GotResponsefromDNP + "," + ReceivedResponsefromDNP, Logger.TypeEnum.VoInfinityTrace);
                            MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_OUT_LOG, _orderKey, profileResponse1.SerializeObject(), "S0341469");

                            if (profileResponse1 != null
                                && profileResponse1.manageClientProfileV1Response != null
                                && profileResponse1.manageClientProfileV1Response.standardHeader != null
                                && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState != null
                                && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState.stateCode != null
                                && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState.stateCode == "0")
                            {
                                res = true;

                                e2eData.logMessage(GotResponseFromDnP, "");
                                if (profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && !string.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                                {
                                    e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                                }
                                else
                                {
                                    e2eData.endOutboundCall(e2eData.toString());
                                }

                                //MCP call with search DN
                                //if (!IsAhtDone)
                                //{
                                //    if (MSIForServiceInstanceDeletion("IP_VOICE_SERVICE", e2eData))
                                //    {
                                //    }
                                //}
                                //else
                                //{
                                notification.sendNotification(true, false, "004", string.Empty, ref e2eData, true);
                                Logger.Write(_orderKey + "," + Completed + "," + CompletedNotificationSent+"," + _gcp_serviceAlreadyExist+","+ _gcp_isAHT, Logger.TypeEnum.VoInfinityTrace);
                                //}

                                _gcp_serviceAlreadyExist = false;
                                _gcp_isAHT = false;
                            }
                            else
                            {
                                #region response
                                if (profileResponse1.manageClientProfileV1Response != null
                                    && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res != null
                                    && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages != null
                                    && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0] != null
                                    && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description != null)
                                {
                                    string errorcode = string.Empty;
                                    _gcp_serviceAlreadyExist = false;
                                    _gcp_isAHT = false;
                                    string errorMessage = profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description;
                                    if (profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].messageCode != null)
                                        errorcode = profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].messageCode;
                                    e2eData.businessError(GotResponseFromDnPWithBusinessError, errorMessage);
                                    if (profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && !string.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                                        e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                                    else
                                        e2eData.endOutboundCall(e2eData.toString());

                                    if (errorMessage.Contains("Administrator"))
                                    {
                                        Logger.Write(_orderKey + "," + Errored + "," + DnPAdminstratorFailedResponse + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.VoInfinityTrace);
                                        Logger.Write(_orderKey + "," + Failed + "," + FailureResponseFromDnP + "," + errorMessage, Logger.TypeEnum.VoInfinityTrace);
                                        notification.sendNotification(false, false, "777_PROFILE_-1", errorMessage, ref e2eData, true);
                                        Logger.Write(_orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.VoInfinityTrace);
                                    }
                                    else if (errorMessage.Contains("connection"))
                                    {
                                        Logger.Write(_orderKey + "," + Errored + "," + DnPAdminstratorFailedResponse + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.VoInfinityTrace);
                                        Logger.Write(_orderKey + "," + Failed + "," + FailureResponseFromDnP + "," + errorMessage, Logger.TypeEnum.VoInfinityTrace);
                                        notification.sendNotification(false, false, "DNP_connectfailed_777_01", errorMessage, ref e2eData, true);
                                        Logger.Write(_orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.VoInfinityTrace);
                                    }
                                    else if (errorMessage.Contains("Anonymous"))
                                    {
                                        Logger.Write(_orderKey + "," + Errored + "," + DnPAdminstratorFailedResponse + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.VoInfinityTrace);
                                        Logger.Write(_orderKey + "," + Failed + "," + FailureResponseFromDnP + "," + errorMessage, Logger.TypeEnum.VoInfinityTrace);
                                        notification.sendNotification(false, false, "DNP_authfailed_777_02", errorMessage, ref e2eData, true);
                                        Logger.Write(_orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.VoInfinityTrace);
                                    }
                                    else
                                    {
                                        Logger.Write(_orderKey + "," + Failed + "," + FailureResponseFromDnP + "," + errorMessage, Logger.TypeEnum.VoInfinityTrace);
                                        notification.sendNotification(false, false, "777_" + errorcode, errorMessage, ref e2eData, true);
                                        Logger.Write(_orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.VoInfinityTrace);
                                    }
                                }
                                else
                                {
                                    Logger.Write(_orderKey + "," + Failed + "," + NullResponseFromDnP, Logger.TypeEnum.VoInfinityTrace);

                                    e2eData.businessError(GotResponseFromDnPWithBusinessError, "Response is null from DnP");
                                    if (profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                                        e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                                    else
                                        e2eData.endOutboundCall(e2eData.toString());

                                    notification.sendNotification(false, false, "MSEO_nullDNPresponse_777", "Response is null from DnP", ref e2eData, true);
                                    Logger.Write(_orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.VoInfinityTrace);
                                }

                                #endregion response
                            }
                            #endregion Call DnP
                        }
                    }
                    else
                    {
                        //ignore
                        notification.sendNotification(false, false, "MSEO_noDVservice_001", "IP Vocie service is not existing at DNP", ref e2eData, true);
                        Logger.Write(_orderKey + "," + Ignored + "," + IgnoredNotificationSent, Logger.TypeEnum.SpringMessageTrace);
                    }
                }
                else
                {
                    res = false;
                    return res;
                }
                _gcp_serviceAlreadyExist = false;
                _gcp_isAHT = false;
            }
            catch (BT.SaaS.IspssAdapter.DnpException excep)
            {
                res = false;
                if (excep.Message.Contains("Administrator"))
                {
                    notification.sendNotification(false, false, "777_PROFILE_-1", excep.Message, ref e2eData, true);
                }
                else if (excep.Message.Contains("connection"))
                {
                    notification.sendNotification(false, false, "DNP_connectfailed_777_01", excep.Message, ref e2eData, true);
                }
                else if (excep.Message.Contains("Anonymous"))
                {
                    notification.sendNotification(false, false, "DNP_authfailed_777_02", excep.Message, ref e2eData, true);
                }
                else
                    notification.sendNotification(false, false, "777", excep.Message, ref e2eData, true);
                Logger.Write(_orderKey + "," + Errored + "," + FailedNotificationSent + "," + excep.Message, Logger.TypeEnum.VoInfinityTraceException);
                Logger.Write(_orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.VoInfinityTraceException);
            }
            catch (Exception ex)
            {
                res = false;
                notification.sendNotification(false, false, "001", ex.Message, ref e2eData, true);
                Logger.Write(_orderKey + "," + Errored + "," + FailedNotificationSent + "," + ex.Message, Logger.TypeEnum.VoInfinityTraceException);
                Logger.Write(_orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.VoInfinityTraceException);
            }
            finally
            {
                //dnpParameters = null;
            }
            return res;
        }

        public bool ModifyVoiceInfinity(MSEO.OrderItem orderItem, ref E2ETransaction e2eData)
        {
            bool isModifySuccess = false;
            string oldDNValue = string.Empty;
            string newDNValue = string.Empty;

            manageClientIdentityResponse1 profileResponse = new manageClientIdentityResponse1();
            manageClientIdentityRequest1 profileRequest = new manageClientIdentityRequest1();
            ManageClientIdentityRequest manageclientidentityreq = new ManageClientIdentityRequest();
            manageclientidentityreq.manageClientIdentityReq = new ManageClientIdentityReq();

            try
            {
                if ((orderItem.Action.Code.Equals("modify", StringComparison.OrdinalIgnoreCase)))
                {
                    if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(insChar => insChar.Name.Equals("DN", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(insChar.Value)))
                    {
                        newDNValue = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("DN", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                        oldDNValue = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("DN", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().PreviousValue;
                    }

                    BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock headerblock = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
                    headerblock.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
                    headerblock.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
                    headerblock.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();
                    headerblock.serviceState.stateCode = "OK";

                    manageclientidentityreq.manageClientIdentityReq.clientSearchCriteria = new ClientSearchCriteria();
                    manageclientidentityreq.manageClientIdentityReq.clientSearchCriteria.identifierDomainCode = VoiceInfinityClientIdentity;
                    manageclientidentityreq.manageClientIdentityReq.clientSearchCriteria.identifierValue = oldDNValue;

                    manageclientidentityreq.manageClientIdentityReq.clientIdentity = new ClientIdentity();
                    manageclientidentityreq.manageClientIdentityReq.clientIdentity.action = "UPDATE";
                    manageclientidentityreq.manageClientIdentityReq.clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                    manageclientidentityreq.manageClientIdentityReq.clientIdentity.managedIdentifierDomain.value = VoiceInfinityClientIdentity;
                    manageclientidentityreq.manageClientIdentityReq.clientIdentity.value = newDNValue;

                    headerblock.e2e = new BT.SaaS.IspssAdapter.Dnp.E2E();
                    headerblock.e2e.E2EDATA = e2eData.toString();

                    manageclientidentityreq.standardHeader = headerblock;
                    profileRequest.manageClientIdentityRequest = manageclientidentityreq;
                    InterfaceRequestProcessor objrep = new VoiceInfinityRequestProcessor();

                    Logger.Write(_orderKey + "," + MakingDnPCall + "ManageClientProfile to deleteOrder" + "," + SendingRequestToDNP, Logger.TypeEnum.VoInfinityTrace);
                    MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_IN_LOG, _orderKey, profileRequest.SerializeObject(), "S0341469");

                    profileResponse = objrep.ManageClientIdentity(profileRequest, _orderKey);

                    Logger.Write(_orderKey + "," + GotResponsefromDNP + "," + ReceivedResponsefromDNP, Logger.TypeEnum.VoInfinityTrace);
                    MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_OUT_LOG, _orderKey, profileResponse.SerializeObject(), "S0341469");
                    if (profileResponse != null
                       && profileResponse.manageClientIdentityResponse != null
                       && profileResponse.manageClientIdentityResponse.standardHeader != null
                       && profileResponse.manageClientIdentityResponse.standardHeader.serviceState != null
                       && profileResponse.manageClientIdentityResponse.standardHeader.serviceState.stateCode != null
                       && profileResponse.manageClientIdentityResponse.standardHeader.serviceState.stateCode == "0")
                    {
                        isModifySuccess = true;
                        e2eData.logMessage(GotResponseFromDnP, "");
                        if (profileResponse.manageClientIdentityResponse.standardHeader.e2e != null && !string.IsNullOrEmpty(profileResponse.manageClientIdentityResponse.standardHeader.e2e.E2EDATA))
                        {
                            e2eData.endOutboundCall(profileResponse.manageClientIdentityResponse.standardHeader.e2e.E2EDATA);
                        }
                        else
                            e2eData.endOutboundCall(e2eData.toString());

                        notification.sendNotification(true, false, "004", string.Empty, ref e2eData, true);
                        Logger.Write(_orderKey + "," + Completed + "," + CompletedNotificationSent, Logger.TypeEnum.SpringMessageTrace);
                    }
                    else
                    {
                        if (profileResponse != null && profileResponse.manageClientIdentityResponse != null
                            && profileResponse.manageClientIdentityResponse.manageClientIdentityRes != null
                            && profileResponse.manageClientIdentityResponse.manageClientIdentityRes.messages != null
                            && profileResponse.manageClientIdentityResponse.manageClientIdentityRes.messages[0].description != null)
                        {
                            string errorcode = string.Empty;
                            string errorMessage = profileResponse.manageClientIdentityResponse.manageClientIdentityRes.messages[0].description;
                            if (profileResponse.manageClientIdentityResponse.manageClientIdentityRes.messages[0].messageCode != null)
                                errorcode = profileResponse.manageClientIdentityResponse.manageClientIdentityRes.messages[0].messageCode;
                            e2eData.businessError(GotResponseFromDnPWithBusinessError, errorMessage);
                            if (profileResponse.manageClientIdentityResponse.standardHeader.e2e != null && !string.IsNullOrEmpty(profileResponse.manageClientIdentityResponse.standardHeader.e2e.E2EDATA))
                                e2eData.endOutboundCall(profileResponse.manageClientIdentityResponse.standardHeader.e2e.E2EDATA);
                            else
                                e2eData.endOutboundCall(e2eData.toString());

                            if (errorMessage.Contains("Administrator"))
                            {
                                Logger.Write(_orderKey + "," + Errored + "," + DnPAdminstratorFailedResponse + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.VoInfinityTrace);
                                Logger.Write(_orderKey + "," + Failed + "," + FailureResponseFromDnP + "," + errorMessage, Logger.TypeEnum.VoInfinityTrace);
                                notification.sendNotification(false, false, "777_PROFILE_-1", errorMessage, ref e2eData, true);
                                Logger.Write(_orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.VoInfinityTrace);
                            }
                            else if (errorMessage.Contains("connection"))
                            {
                                Logger.Write(_orderKey + "," + Errored + "," + DnPAdminstratorFailedResponse + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.VoInfinityTrace);
                                Logger.Write(_orderKey + "," + Failed + "," + FailureResponseFromDnP + "," + errorMessage, Logger.TypeEnum.VoInfinityTrace);
                                notification.sendNotification(false, false, "DNP_connectfailed_777_01", errorMessage, ref e2eData, true);
                                Logger.Write(_orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.VoInfinityTrace);
                            }
                            else if (errorMessage.Contains("Anonymous"))
                            {
                                Logger.Write(_orderKey + "," + Errored + "," + DnPAdminstratorFailedResponse + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.VoInfinityTrace);
                                Logger.Write(_orderKey + "," + Failed + "," + FailureResponseFromDnP + "," + errorMessage, Logger.TypeEnum.VoInfinityTrace);
                                notification.sendNotification(false, false, "DNP_authfailed_777_02", errorMessage, ref e2eData, true);
                                Logger.Write(_orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.VoInfinityTrace);
                            }
                            else
                            {
                                Logger.Write(_orderKey + "," + Failed + "," + FailureResponseFromDnP + "," + errorMessage, Logger.TypeEnum.VoInfinityTrace);
                                notification.sendNotification(false, false, "777_" + errorcode, errorMessage, ref e2eData, true);
                                Logger.Write(_orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.VoInfinityTrace);
                            }
                        }
                        else
                        {
                            Logger.Write(_orderKey + "," + Failed + "," + NullResponseFromDnP, Logger.TypeEnum.SpringMessageTrace);
                            e2eData.businessError(GotResponseFromDnPWithBusinessError, "Response is null from DnP");
                            if (profileResponse != null && profileResponse.manageClientIdentityResponse != null
                                && profileResponse.manageClientIdentityResponse.standardHeader != null
                                && profileResponse.manageClientIdentityResponse.standardHeader.e2e != null
                                && profileResponse.manageClientIdentityResponse.standardHeader.e2e.E2EDATA != null &&
                                !String.IsNullOrEmpty(profileResponse.manageClientIdentityResponse.standardHeader.e2e.E2EDATA))
                                e2eData.endOutboundCall(profileResponse.manageClientIdentityResponse.standardHeader.e2e.E2EDATA);
                            else
                                e2eData.endOutboundCall(e2eData.toString());

                            notification.sendNotification(false, false, "MSEO_nullDNPresponse_777", "Response is null from DnP", ref e2eData, true);
                            Logger.Write(_orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.SpringMessageTrace);
                        }
                    }
                }
                else
                {
                    isModifySuccess = false;
                    return isModifySuccess;
                }

            }
            catch (BT.SaaS.IspssAdapter.DnpException excep)
            {
                isModifySuccess = false;
                if (excep.Message.Contains("Administrator"))
                {
                    notification.sendNotification(false, false, "777_PROFILE_-1", excep.Message, ref e2eData, true);
                }
                else if (excep.Message.Contains("connection"))
                {
                    notification.sendNotification(false, false, "DNP_connectfailed_777_01", excep.Message, ref e2eData, true);
                }
                else if (excep.Message.Contains("Anonymous"))
                {
                    notification.sendNotification(false, false, "DNP_authfailed_777_02", excep.Message, ref e2eData, true);
                }
                else
                    notification.sendNotification(false, false, "777", excep.Message, ref e2eData, true);
                Logger.Write(_orderKey + "," + Errored + "," + FailedNotificationSent + "," + excep.Message, Logger.TypeEnum.VoInfinityTraceException);
                Logger.Write(_orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.VoInfinityTraceException);
            }
            catch (Exception ex)
            {
                isModifySuccess = false;
                notification.sendNotification(false, false, "001", ex.Message, ref e2eData, true);
                Logger.Write(_orderKey + "," + Errored + "," + FailedNotificationSent + "," + ex.Message, Logger.TypeEnum.VoInfinityTraceException);
                Logger.Write(_orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.VoInfinityTraceException);
            }
            finally
            {
                //dnpParameters = null;
            }
            return isModifySuccess;
        }

        /// <summary>
        /// to update the SIPdevice details in DnP.
        /// </summary>
        /// <param name="orderItem"></param>
        /// <param name="e2eData"></param>
        /// <returns></returns>
        public bool ModifySIPDetails(MSEO.OrderItem orderItem, ref E2ETransaction e2eData)
        {
            bool isModifySuccess = false;

            manageClientProfileV1Response1 profileResponse1 = null;
            manageClientProfileV1Request1 profileRequest = new manageClientProfileV1Request1();
            profileRequest.manageClientProfileV1Request = new ManageClientProfileV1Request();
            ManageClientProfileV1Req manageClientProfileV1Req = new ManageClientProfileV1Req();

            try
            {
                BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock headerBlock =
                    new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock
                    {
                        serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing
                        {
                            @from = "http://www.profile.com?SAASMSEO"
                        },
                        serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState()
                    };

                manageClientProfileV1Req.clientProfileV1 = new ClientProfileV1();
                manageClientProfileV1Req.clientProfileV1.client = new Client();
                manageClientProfileV1Req.clientProfileV1.client.action = ActionUpdate;

                manageClientProfileV1Req.clientProfileV1.client.clientIdentity = new ClientIdentity[2];
                manageClientProfileV1Req.clientProfileV1.client.clientIdentity[0] = new ClientIdentity();
                manageClientProfileV1Req.clientProfileV1.client.clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                manageClientProfileV1Req.clientProfileV1.client.clientIdentity[0].managedIdentifierDomain.value = "SIP_DEVICE";
                manageClientProfileV1Req.clientProfileV1.client.clientIdentity[0].value = _deviceidValue;
                manageClientProfileV1Req.clientProfileV1.client.clientIdentity[0].action = ActionSearch;

                manageClientProfileV1Req.clientProfileV1.client.clientIdentity[1] = new ClientIdentity();
                manageClientProfileV1Req.clientProfileV1.client.clientIdentity[1].managedIdentifierDomain = new ManagedIdentifierDomain();
                manageClientProfileV1Req.clientProfileV1.client.clientIdentity[1].managedIdentifierDomain.value = "SIP_DEVICE";
                manageClientProfileV1Req.clientProfileV1.client.clientIdentity[1].value = _deviceidValue;
                manageClientProfileV1Req.clientProfileV1.client.clientIdentity[1].action = ActionUpdate;

                List<ClientIdentityValidation> clientValidationList = new List<ClientIdentityValidation>();

                ClientIdentityValidation clientValidation = null;

                if (!string.IsNullOrEmpty(_decrref))
                {
                    clientValidation = new ClientIdentityValidation();
                    clientValidation.name = "DECRYPTION_METHOD_REF";
                    clientValidation.value = _decrref;
                    clientValidation.action = ActionForceInsert;
                    clientValidationList.Add(clientValidation);
                }

                if (!string.IsNullOrEmpty(_decrrefims))
                {
                    clientValidation = new ClientIdentityValidation();
                    clientValidation.name = "DECRYPTION_METHOD_REF_IMS";
                    clientValidation.value = _decrrefims;
                    clientValidation.action = ActionForceInsert;
                    clientValidationList.Add(clientValidation);
                }
                if (!string.IsNullOrEmpty(_refKeyims))
                {
                    clientValidation = new ClientIdentityValidation();
                    clientValidation.name = "KEY_REF_IMS";
                    clientValidation.value = _refKeyims;
                    clientValidation.action = ActionForceInsert;
                    clientValidationList.Add(clientValidation);
                }

                if (!string.IsNullOrEmpty(_refKey))
                {
                    clientValidation = new ClientIdentityValidation();
                    clientValidation.name = "KEY_REF_MOTIVE";
                    clientValidation.value = _refKey;
                    clientValidation.action = ActionForceInsert;
                    clientValidationList.Add(clientValidation);
                }
                if (!string.IsNullOrEmpty(_sipPwd))
                {
                    clientValidation = new ClientIdentityValidation();
                    clientValidation.name = "SIP_PASSWORD";
                    clientValidation.value = _sipPwd;
                    clientValidation.action = ActionForceInsert;
                    clientValidationList.Add(clientValidation);
                }
                if (!string.IsNullOrEmpty(_sipPwdims))
                {
                    clientValidation = new ClientIdentityValidation();
                    clientValidation.name = "SIP_PASSWORD_IMS";
                    clientValidation.value = _sipPwdims;
                    clientValidation.action = ActionForceInsert;
                    clientValidationList.Add(clientValidation);
                }

                if (!string.IsNullOrEmpty(_sipUid))
                {
                    clientValidation = new ClientIdentityValidation();
                    clientValidation.name = "SIP_PRIVATE_UID";
                    clientValidation.value = _sipUid;
                    clientValidation.action = ActionForceInsert;
                    clientValidationList.Add(clientValidation);
                }
                manageClientProfileV1Req.clientProfileV1.client.clientIdentity[1].clientIdentityValidation = clientValidationList.ToArray();

                profileRequest.manageClientProfileV1Request.standardHeader = headerBlock;
                profileRequest.manageClientProfileV1Request.manageClientProfileV1Req = manageClientProfileV1Req;

                // e2eData.logMessage(StartedDnPCall, "");
                e2eData.startOutboundCall(ConfigurationManager.AppSettings["destinationDnP"].ToString(), E2ETransaction.vREQ, VoiceinifnityServicecodeNamepace);

                profileRequest.manageClientProfileV1Request.standardHeader.e2e =
                    new BT.SaaS.IspssAdapter.Dnp.E2E { E2EDATA = e2eData.toString() };


                Logger.Write(_orderKey + "," + MakingDnPCall + "MCP call to create order" + "," + SendingRequestToDNP, Logger.TypeEnum.VoInfinityTrace);
                MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_IN_LOG, _orderKey, profileRequest.SerializeObject(), "S0341469");
                InterfaceRequestProcessor objresp = new VoiceInfinityRequestProcessor();

                //profileResponse1 = SpringDnpWrapper.manageClientProfileV1ForSpring(profileRequest, orderKey);
                profileResponse1 = objresp.ManageClientProfile(profileRequest, _orderKey);

                Logger.Write(_orderKey + "," + GotResponsefromDNP + "," + ReceivedResponsefromDNP, Logger.TypeEnum.VoInfinityTrace);
                MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_OUT_LOG, _orderKey, profileResponse1.SerializeObject(), "S0341469");

                if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null
           && profileResponse1.manageClientProfileV1Response.standardHeader != null
           && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState != null
           && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState.stateCode != null
           && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState.stateCode == "0")
                {
                    isModifySuccess = true;
                    e2eData.logMessage(GotResponseFromDnP, "");
                    if (profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && !string.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                    {
                        e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                    }
                    else
                        e2eData.endOutboundCall(e2eData.toString());

                    notification.sendNotification(true, false, "004", string.Empty, ref e2eData, true);
                    Logger.Write(_orderKey + "," + Completed + "," + CompletedNotificationSent, Logger.TypeEnum.VoInfinityTrace);
                }
                else
                {
                    if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null
                        && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res != null
                        && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages != null
                        && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0] != null
                        && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description != null)
                    {
                        string errorcode = string.Empty;
                        string errorMessage = profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description;
                        if (profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].messageCode != null)
                            errorcode = profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].messageCode;

                        e2eData.businessError(GotResponseFromDnPWithBusinessError, errorMessage);
                        if (profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && !string.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());

                        if (errorMessage.Contains("Administrator"))
                        {
                            Logger.Write(_orderKey + "," + Errored + "," + DnPAdminstratorFailedResponse + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.VoInfinityTrace);
                            Logger.Write(_orderKey + "," + Failed + "," + FailureResponseFromDnP + "," + errorMessage, Logger.TypeEnum.VoInfinityTrace);
                            notification.sendNotification(false, false, "777_PROFILE_-1", errorMessage, ref e2eData, true);
                            Logger.Write(_orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.VoInfinityTrace);
                        }
                        else if (errorMessage.Contains("connection"))
                        {
                            Logger.Write(_orderKey + "," + Errored + "," + DnPAdminstratorFailedResponse + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.VoInfinityTrace);
                            Logger.Write(_orderKey + "," + Failed + "," + FailureResponseFromDnP + "," + errorMessage, Logger.TypeEnum.VoInfinityTrace);
                            notification.sendNotification(false, false, "DNP_connectfailed_777_01", errorMessage, ref e2eData, true);
                            Logger.Write(_orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.VoInfinityTrace);
                        }
                        else if (errorMessage.Contains("Anonymous"))
                        {
                            Logger.Write(_orderKey + "," + Errored + "," + DnPAdminstratorFailedResponse + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.VoInfinityTrace);
                            Logger.Write(_orderKey + "," + Failed + "," + FailureResponseFromDnP + "," + errorMessage, Logger.TypeEnum.VoInfinityTrace);
                            notification.sendNotification(false, false, "DNP_authfailed_777_02", errorMessage, ref e2eData, true);
                            Logger.Write(_orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.VoInfinityTrace);
                        }
                        else
                        {
                            Logger.Write(_orderKey + "," + Failed + "," + FailureResponseFromDnP + "," + errorMessage, Logger.TypeEnum.VoInfinityTrace);
                            notification.sendNotification(false, false, "777_" + errorcode, errorMessage, ref e2eData, true);
                            Logger.Write(_orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.VoInfinityTrace);
                        }
                    }
                    else
                    {
                        Logger.Write(_orderKey + "," + Failed + "," + NullResponseFromDnP, Logger.TypeEnum.VoInfinityTrace);

                        e2eData.businessError(GotResponseFromDnPWithBusinessError, "Response is null from DnP");
                        if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());

                        notification.sendNotification(false, false, "MSEO_nullDNPresponse_777", "Response is null from DnP", ref e2eData, true);
                        Logger.Write(_orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.VoInfinityTrace);
                    }
                }
            }
            catch (BT.SaaS.IspssAdapter.DnpException excep)
            {
                isModifySuccess = false;
                if (excep.Message.Contains("Administrator"))
                {
                    notification.sendNotification(false, false, "777_PROFILE_-1", excep.Message, ref e2eData, true);
                }
                else if (excep.Message.Contains("connection"))
                {
                    notification.sendNotification(false, false, "DNP_connectfailed_777_01", excep.Message, ref e2eData, true);
                }
                else if (excep.Message.Contains("Anonymous"))
                {
                    notification.sendNotification(false, false, "DNP_authfailed_777_02", excep.Message, ref e2eData, true);
                }
                else
                    notification.sendNotification(false, false, "777", excep.Message, ref e2eData, true);
                Logger.Write(_orderKey + "," + Errored + "," + FailedNotificationSent + "," + excep.Message, Logger.TypeEnum.VoInfinityTraceException);
                Logger.Write(_orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.VoInfinityTraceException);
            }
            catch (Exception ex)
            {
                isModifySuccess = false;
                notification.sendNotification(false, false, "001", ex.Message, ref e2eData, true);
                Logger.Write(_orderKey + "," + Errored + "," + FailedNotificationSent + "," + ex.Message, Logger.TypeEnum.VoInfinityTraceException);
                Logger.Write(_orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.VoInfinityTraceException);
            }
            finally
            {
                //dnpParameters = null;
            }

            return isModifySuccess;
        }

        /// <summary>
        /// as per BTR-94940 Get SIP Device details using uniqueid 
        /// </summary>
        /// <param name="orderItem">orderiteam</param>
        /// <param name="e2eData">e2eData</param>
        /// <returns>boolean value</returns>
        public bool GetSIPDeviceDetails(MSEO.OrderItem orderItem, ref E2ETransaction e2eData)
        {

            bool res = false;
            try
            {
                string urlKey = string.Empty;
                BT.DNP.Rest.getClientProfileV1Res getSIPDeviceResponse = new BT.DNP.Rest.getClientProfileV1Res();

                Instance instance = new Instance();
                List<InstanceCharacteristic> lstInstanceChar = new List<InstanceCharacteristic>();
                InstanceCharacteristic instanceChar = null;

                if (!(string.IsNullOrEmpty(_deviceidValue)))
                {
                    urlKey = ConfigurationManager.AppSettings["VoiceGetSIPurl"].Replace("{identifier_Value}", _deviceidValue).ToString();
                    getSIPDeviceResponse = GetSIPDetailsRestCall(urlKey);
                }
                if (getSIPDeviceResponse != null && getSIPDeviceResponse.clientProfileV1 != null && getSIPDeviceResponse.clientProfileV1[0].client != null && getSIPDeviceResponse.clientProfileV1[0].client.clientIdentity != null)
                {
                    foreach (BT.DNP.Rest.ClientIdentity clientIdentity in getSIPDeviceResponse.clientProfileV1[0].client.clientIdentity)
                    {
                        instanceChar = new InstanceCharacteristic();
                        instanceChar.Name = clientIdentity.domain;
                        instanceChar.Value = clientIdentity.value;
                        lstInstanceChar.Add(instanceChar);
                        if (clientIdentity.characteristic != null)
                        {
                            foreach (BT.DNP.Rest.Characteristic characteristic in clientIdentity.characteristic)
                            {
                                instanceChar = new InstanceCharacteristic();
                                instanceChar.Name = characteristic.name.ToUpper().ToString();
                                instanceChar.Value = characteristic.value;
                                lstInstanceChar.Add(instanceChar);
                            }
                        }
                    }
                    instance.InstanceCharacteristic = lstInstanceChar.ToArray();

                    instance.Specification1 = new Specification();
                    instance.Specification1 = orderItem.Instance[0].Specification1;

                    instance.InstanceIdentifier = new InstanceIdentifier();
                    instance.InstanceIdentifier = orderItem.Instance[0].InstanceIdentifier;

                    orderItem.Instance[0] = instance;

                    notification.sendNotification(true, false, "004", string.Empty, ref e2eData, true);
                    Logger.Write(_orderKey + "," + Completed + "," + CompletedNotificationSent, Logger.TypeEnum.VoInfinityTrace);

                    res = true;
                }
                else
                {
                    notification.sendNotification(false, false, "MSEO_nullDNPresponse_777", "No Data found for the given Unique_ID", ref e2eData, true);
                    Logger.Write(_orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.VoInfinityTrace);
                }
            }
            catch (Exception dnpException)
            {
                throw new DnpException("DNPException: " + dnpException.ToString());
            }

            return res;
        }
        /// <summary>
        /// restcall to get the sip device value from DnP
        /// </summary>
        /// <param name="url">url to get the clientidentity and characteristics </param>
        /// <returns>getClientProfileV1Res object</returns>
        public BT.DNP.Rest.getClientProfileV1Res GetSIPDetailsRestCall(string url)
        {
            BT.DNP.Rest.getClientProfileV1Res getSIPDeviceResponse = new BT.DNP.Rest.getClientProfileV1Res();
            try
            {
                string responseXml;
                HttpWebRequest webrequest;

                webrequest = (HttpWebRequest)WebRequest.Create(url);
                webrequest.Method = "GET";
                webrequest.ContentType = "text/xml";
                webrequest.Accept = "application/xml";
                webrequest.MediaType = "application/xml";

                webrequest.Headers.Add("SystemCd", "SAASMSEO");

                webrequest.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["CMPSTimeOut"].ToString());

                //CredentialCache cred = new CredentialCache();
                //NetworkCredential mycred = new NetworkCredential(ConfigurationManager.AppSettings["DnPUser"].ToString(), ConfigurationManager.AppSettings["DnPPassword"].ToString());
                //cred.Add(new Uri(url), "Basic", mycred);
                //webrequest.Credentials = cred;
                NetworkCredential mycred = new NetworkCredential(ConfigurationManager.AppSettings["DnPUser"].ToString(), ConfigurationManager.AppSettings["DnPPassword"].ToString());
                if (mycred != null && !string.IsNullOrEmpty(mycred.UserName) && !string.IsNullOrEmpty(mycred.Password))
                {
                    webrequest.Headers.Add(HttpRequestHeader.Authorization, "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(mycred.UserName + ":" + mycred.Password)));
                }

                using (HttpWebResponse webResponse = (HttpWebResponse)webrequest.GetResponse())
                {
                    using (StreamReader readStream = new StreamReader(webResponse.GetResponseStream()))
                    {
                        responseXml = readStream.ReadToEnd();
                        readStream.Close();
                    }
                    webResponse.Close();
                }

                using (StringReader reader = new StringReader(responseXml))
                {
                    XmlSerializer serailizer = new XmlSerializer(typeof(BT.DNP.Rest.getClientProfileV1Res));
                    getSIPDeviceResponse = (BT.DNP.Rest.getClientProfileV1Res)serailizer.Deserialize(reader);
                    reader.Close();
                }
            }
            catch (WebException DnpEx)
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
            }
            catch (Exception dnpException)
            {

                throw new DnpException("DNPException: " + dnpException.ToString());
            }
            return getSIPDeviceResponse;
        }


        /// <summary>
        /// Check whether the request is valid or not
        /// </summary>
        /// <param name="requestOrderRequest"></param>
        /// <returns></returns>
        protected override bool IsRequestValid(OrderRequest requestOrderRequest)
        {
            bool isValid = false;
            try
            {
                //Mandotory ---> Code; s-code; BillAccountNumber; DN; serviceName
                if (requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToUpper() == "BILLACCOUNTNUMBER"))
                    isValid = true;
                else
                    isValid = false;
                if (requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToUpper() == "DN"))
                    isValid = true;
                else
                    isValid = false;
                if (((requestOrderRequest.Order.OrderItem[0].Action != null) && (requestOrderRequest.Order.OrderItem[0].Action.Code != null)) && (requestOrderRequest.Order.OrderItem[0].Action.Code.ToUpper() == "CREATE") || (requestOrderRequest.Order.OrderItem[0].Action.Code.ToUpper() == "CANCEL"))
                    isValid = true;
                else
                    isValid = false;
                if ((requestOrderRequest.Order.OrderItem[0].Specification[0].Identifier.Value1 != null))
                    isValid = true;
                else
                    isValid = false;

            }
            catch (Exception ex)
            {
                isValid = false;
                return isValid;
            }
            return isValid;
        }


        /// <summary>
        /// Service Instance deletion
        /// </summary>
        /// <param name="serviceCode"></param>
        /// <param name="e2edataforMSI"></param>
        /// <returns></returns>
        public bool MSIForServiceInstanceDeletion(string serviceCode, E2ETransaction e2edataforMSI)
        {
            bool ret = false;
            manageServiceInstanceV1Response1 profileResponse;
            manageServiceInstanceV1Request1 profileRequest = new manageServiceInstanceV1Request1
            {
                manageServiceInstanceV1Request = new ManageServiceInstanceV1Request()
            };


            BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock headerBlock =
                new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock
                {
                    serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing
                    {
                        @from = "http://www.profile.com?SAASMSEO"
                    },
                    serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState()
                };

            ManageServiceInstanceV1Req manageServiceInstanceV1Req1 = new ManageServiceInstanceV1Req();

            ServiceIdentity[] serviceIdentity = new ServiceIdentity[1];
            serviceIdentity[0] = new ServiceIdentity
            {
                domain = BacidIdentiferNamepace,
                value = _billAccountNumber,
                action = ActionSearch
            };

            ClientServiceInstanceV1 clientServiceInstanceV1 = new ClientServiceInstanceV1();

            clientServiceInstanceV1 = new ClientServiceInstanceV1
            {
                clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier
                {
                    value = serviceCode
                },
                serviceIdentity = serviceIdentity,
                action = ActionDelete
            };

            manageServiceInstanceV1Req1.clientServiceInstanceV1 = clientServiceInstanceV1;

            profileRequest.manageServiceInstanceV1Request.manageServiceInstanceV1Req = manageServiceInstanceV1Req1;
            profileRequest.manageServiceInstanceV1Request.standardHeader = headerBlock;
            profileRequest.manageServiceInstanceV1Request.manageServiceInstanceV1Req = manageServiceInstanceV1Req1;

            profileResponse = ManageServiceInstance(profileRequest, e2edataforMSI.toString());
            if (profileResponse != null
                && profileResponse.manageServiceInstanceV1Response != null
                && profileResponse.manageServiceInstanceV1Response.standardHeader != null
                && profileResponse.manageServiceInstanceV1Response.standardHeader.serviceState != null
                && profileResponse.manageServiceInstanceV1Response.standardHeader.serviceState.stateCode != null
                && profileResponse.manageServiceInstanceV1Response.standardHeader.serviceState.stateCode == "0")
            {
                e2edataforMSI.logMessage(GotResponseFromDnP, "");
                if (profileResponse.manageServiceInstanceV1Response.standardHeader.e2e != null && !string.IsNullOrEmpty(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA))
                {
                    e2edataforMSI.endOutboundCall(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA);
                }
                else
                    e2edataforMSI.endOutboundCall(e2edataforMSI.toString());

                notification.sendNotification(true, false, "004", string.Empty, ref e2edataforMSI, true);
                //notification.sendNotification(true, true, string.Empty, string.Empty, ref e2eData);
                Logger.Write(_orderKey + "," + Completed + "," + CompletedNotificationSent, Logger.TypeEnum.SpringMessageTrace);
                ret = true;
            }
            else
            {
                if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null
     && profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res != null
     && profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res.messages != null
     && profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res.messages[0] != null
     && profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res.messages[0].description != null)
                {
                    string errorcode = string.Empty;
                    string errorMessage = profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res.messages[0].description;
                    if (profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res.messages[0].messageCode != null)
                        errorcode = profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res.messages[0].messageCode;

                    e2edataforMSI.businessError(GotResponseFromDnPWithBusinessError, errorMessage);
                    if (profileResponse.manageServiceInstanceV1Response.standardHeader != null && (profileResponse.manageServiceInstanceV1Response.standardHeader.e2e != null && !string.IsNullOrEmpty(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA)))
                        e2edataforMSI.endOutboundCall(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA);
                    else
                        e2edataforMSI.endOutboundCall(e2edataforMSI.toString());

                    if (errorMessage.Contains("Administrator"))
                    {
                        Logger.Write(_orderKey + "," + Errored + "," + DnPAdminstratorFailedResponse + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.VoInfinityTrace);
                        Logger.Write(_orderKey + "," + Failed + "," + FailureResponseFromDnP + "," + errorMessage, Logger.TypeEnum.VoInfinityTrace);
                        notification.sendNotification(false, false, "777_PROFILE_-1", errorMessage, ref e2edataforMSI, true);
                        Logger.Write(_orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.VoInfinityTrace);
                    }
                    else if (errorMessage.Contains("connection"))
                    {
                        Logger.Write(_orderKey + "," + Errored + "," + DnPAdminstratorFailedResponse + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.VoInfinityTrace);
                        Logger.Write(_orderKey + "," + Failed + "," + FailureResponseFromDnP + "," + errorMessage, Logger.TypeEnum.VoInfinityTrace);
                        notification.sendNotification(false, false, "DNP_connectfailed_777_01", errorMessage, ref e2edataforMSI, true);
                        Logger.Write(_orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.VoInfinityTrace);
                    }
                    else if (errorMessage.Contains("Anonymous"))
                    {
                        Logger.Write(_orderKey + "," + Errored + "," + DnPAdminstratorFailedResponse + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.VoInfinityTrace);
                        Logger.Write(_orderKey + "," + Failed + "," + FailureResponseFromDnP + "," + errorMessage, Logger.TypeEnum.VoInfinityTrace);
                        notification.sendNotification(false, false, "DNP_authfailed_777_02", errorMessage, ref e2edataforMSI, true);
                        Logger.Write(_orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.VoInfinityTrace);
                    }
                    else
                    {
                        Logger.Write(_orderKey + "," + Failed + "," + FailureResponseFromDnP + "," + errorMessage, Logger.TypeEnum.VoInfinityTrace);
                        notification.sendNotification(false, false, "777_" + errorcode, errorMessage, ref e2edataforMSI, true);
                        Logger.Write(_orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.VoInfinityTrace);
                    }
                }
                else
                {
                    Logger.Write(_orderKey + "," + Failed + "," + NullResponseFromDnP, Logger.TypeEnum.SpringMessageTrace);

                    e2edataforMSI.businessError(GotResponseFromDnPWithBusinessError, "Response is null from DnP");
                    if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.standardHeader != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA))
                        e2edataforMSI.endOutboundCall(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA);
                    else
                        e2edataforMSI.endOutboundCall(e2edataforMSI.toString());

                    notification.sendNotification(false, false, "MSEO_nullDNPresponse_777", "Response is null from DnP", ref e2edataforMSI, true);
                    Logger.Write(_orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.SpringMessageTrace);
                }
                ret = false;
            }
            return ret;
        }
    }
}
