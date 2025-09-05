using System;
using System.Collections.Generic;
using System.Globalization;
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

namespace BT.SaaS.MSEOAdapter
{
    public class HCSRequestProcessor
    {
        #region Constants
        const string BACID_IDENTIFER_NAMEPACE = "VAS_BILLINGACCOUNT_ID";
        const string BTCOM = "BTCOM";
        const string HCS_SERVICECODE_NAMEPACE = "CONTRACTANDWARRANTY";
        const string GotResponseFromDnPWithBusinessError = "GotResponseFromDnPWithBusinessError";
        const string StartedDnPCall = "StartedDnPCall";
        const string GotResponseFromDnP = "GotResponseFromDnP";

        private const string ACTION_SEARCH = "SEARCH";
        private const string ACTION_UPDATE = "UPDATE";
        private const string ACTION_DELETE = "DELETE";
        private const string ACTION_INSERT = "INSERT";
        private const string ACTION_LINK = "LINK";
        private const string DEFAULT_ROLE = "DEFAULT";
        private const string ACTIVE = "ACTIVE";
        private const string ADMIN_ROLE = "ADMIN";
        
        const string Accepted = "Accepted";
        const string Ignored = "Ignored";
        const string Completed = "Completed";
        const string Failed = "Failed";
        const string Errored = "Errored";
        const string MakingDnPCall = "Making DnP call";      

        const string AcceptedNotificationSent = "Accepted Notification Sent for the Order";
        const string IgnoredNotificationSent = "Ignored Notification Sent for the Order";
        const string CompletedNotificationSent = "Completed Notification Sent for the Order";
        const string FailedNotificationSent = "Failure Notification Sent for the Order";
        const string FailedErrorMessage = "Order failed with error messgae: ";
        const string SendingRequestToDNP = "Sending the Request to DNP";
        const string ReceivedResponsefromDNP = "Recieved Response from DNP";
        const string FailureResponseFromDnP = "Recieved failure response from DnP";
        const string NullResponseFromDnP = "Response is null from DnP";
        const string DnPAdminstratorFailedResponse = "Non Functional Exception from DNP(Administrator): ";
        #endregion

        public void HCSRequestMapper(OrderRequest requestOrderRequest, ref E2ETransaction e2eData)
        {
            MSEOOrderNotification notification = null;
            bool IsExsitingBacID = false;
            bool isAHTDone = false;
            string action = string.Empty;
            string reason = string.Empty;
            

            HCSParameters hcsParameters = new HCSParameters();
            try
            {
                hcsParameters.OrderKey = requestOrderRequest.Order.OrderIdentifier.Value;
                notification = new MSEOOrderNotification(requestOrderRequest);

                if (requestOrderRequest.Order.OrderItem[0].Action != null && requestOrderRequest.Order.OrderItem[0].Action.Code != null)
                {
                    action = requestOrderRequest.Order.OrderItem[0].Action.Code.ToString();
                    hcsParameters.OrderAction = action;
                    if (requestOrderRequest.Order.OrderItem[0].Action.Reason != null)
                    {
                        reason = requestOrderRequest.Order.OrderItem[0].Action.Reason.ToString();
                    }
                }
                

                foreach (InstanceCharacteristic insChar in requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic)
                {
                    if ((insChar.Name.Equals("BillAccountNumber", StringComparison.OrdinalIgnoreCase)) && !(string.IsNullOrEmpty(insChar.Value)))
                    {
                        hcsParameters.BillAccountNumber = insChar.Value;
                    }
                    else if ((insChar.Name.Equals("CustomerId", StringComparison.OrdinalIgnoreCase)) && !(string.IsNullOrEmpty(insChar.Value)))
                    {
                        hcsParameters.CustomerID = insChar.Value;
                    }
                    else if ((insChar.Name.Equals("ContractRefNum", StringComparison.OrdinalIgnoreCase)) && !(string.IsNullOrEmpty(insChar.Value)))
                    {
                        hcsParameters.ContarctRefNum = insChar.Value;
                    }
                    else if ((insChar.Name.Equals("AssetIntegrationId", StringComparison.OrdinalIgnoreCase)) && !(string.IsNullOrEmpty(insChar.Value)))
                    {
                        hcsParameters.AssetIntegartioID = insChar.Value;
                    }
                    else if ((insChar.Name.Equals("Warranty_period", StringComparison.OrdinalIgnoreCase)) && !(string.IsNullOrEmpty(insChar.Value)))
                    {
                        //hcsParameters.WarrantyPeriod = insChar.Value;

                        hcsParameters.WarrantyPeriod = Int32.Parse(insChar.Value);

                        System.DateTime sedate = System.DateTime.Now;
                        sedate = sedate.AddDays(30);
                        sedate = sedate.AddMonths(hcsParameters.WarrantyPeriod);

                        hcsParameters.ServiceExpairyDate = sedate.ToString();
                    }
                    else if ((insChar.Name.Equals("Warranty_unit", StringComparison.OrdinalIgnoreCase)) && !(string.IsNullOrEmpty(insChar.Value)))
                    {
                        hcsParameters.WarrantyUnit = insChar.Value;
                        
                        //System.DateTime sedate = System.DateTime.Now;
                        //sedate = sedate.AddDays(30);
                        //sedate = sedate.AddMonths(hcsParameters.WarrantyUnit);

                        //hcsParameters.ServiceExpairyDate = sedate.ToString();
                    }
                    else if ((insChar.Name.Equals("SaleType", StringComparison.OrdinalIgnoreCase)) && !(string.IsNullOrEmpty(insChar.Value)))
                    {
                        hcsParameters.SaleType = insChar.Value;
                    }
                }

                //if (!string.IsNullOrEmpty(requestOrderRequest.StandardHeader.E2e.E2EDATA))
                //    e2eData = requestOrderRequest.StandardHeader.E2e.E2EDATA;
              
                if (!String.IsNullOrEmpty(hcsParameters.BillAccountNumber))
                {
                    GetClientProfileV1Res bacProfileResponse = null;                   
                    ClientServiceInstanceV1[] bacServiceResponse = null;

                    bacProfileResponse = DnpWrapper.GetClientProfileV1ForThomas(hcsParameters.BillAccountNumber, BACID_IDENTIFER_NAMEPACE, hcsParameters.OrderKey);

                    bacServiceResponse = DnpWrapper.getServiceInstanceV1(hcsParameters.BillAccountNumber, BACID_IDENTIFER_NAMEPACE, HCS_SERVICECODE_NAMEPACE);
                   
                    if (bacProfileResponse != null && bacProfileResponse.clientProfileV1 != null)
                    {
                        IsExsitingBacID = bacProfileResponse.clientProfileV1.client.clientIdentity.ToList().Exists(i => i.managedIdentifierDomain.value.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase) && i.value.Equals(hcsParameters.BillAccountNumber, StringComparison.OrdinalIgnoreCase));
                        if (IsExsitingBacID)
                        {
                            BT.SaaS.IspssAdapter.Dnp.ClientIdentity bacClientIdentity = bacProfileResponse.clientProfileV1.client.clientIdentity.ToList().Where(i => i.managedIdentifierDomain.value.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase) && i.value.Equals(hcsParameters.BillAccountNumber, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                            if (bacClientIdentity.clientIdentityValidation != null)
                                hcsParameters.isAHT = bacClientIdentity.clientIdentityValidation.ToList().Exists(i => i.name.Equals("ACCOUNTTRUSTMETHOD", StringComparison.OrdinalIgnoreCase) && ConfigurationManager.AppSettings["AccountTrustMethodStatus"].Split(',').Contains(i.value.ToLower()));
                        }
                        if (bacProfileResponse.clientProfileV1.client.clientIdentity.ToList().Exists(i => i.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)))
                        {
                            hcsParameters.BtOneId = bacProfileResponse.clientProfileV1.client.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                        }
                    }

                    ClientServiceInstanceV1[] serviceIntances = null;                        

                    if (bacServiceResponse != null && bacServiceResponse.Length > 0)
                    {
                        serviceIntances = bacServiceResponse;
                    }

                    if ((serviceIntances != null) && (serviceIntances.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals("CONTRACTANDWARRANTY", StringComparison.OrdinalIgnoreCase))))
                    {
                        hcsParameters.isServiceAlreadyExist = true;
                    }

                    if (action.Equals("create", StringComparison.OrdinalIgnoreCase) && reason.Equals("CreateContractAndWarranty", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!hcsParameters.isServiceAlreadyExist)
                        {
                            //sending accepted notification 
                            notification.sendNotification(true, true, string.Empty, string.Empty, ref e2eData,false);
                            Logger.Write(hcsParameters.OrderKey + "," + Accepted + "," + AcceptedNotificationSent, Logger.TypeEnum.HCSWarrantyMessageTrace);
                            if (hcsParameters.isAHT)
                                CreateAHTHCSServiceInstance(hcsParameters, notification, ref e2eData);
                            else
                                CreateHCSServiceInstance(hcsParameters, notification, ref e2eData);
                        }
                        else
                        {
                            notification.sendNotification(false, false, "001", "The given bac already have HCSWarranty Service", ref e2eData,true);
                            Logger.Write(hcsParameters.OrderKey + "," + Ignored + "," + IgnoredNotificationSent, Logger.TypeEnum.HCSWarrantyMessageTrace);                                                   
                        }
                      
                    }
                    else if (action.Equals("cancel", StringComparison.OrdinalIgnoreCase))                           
                    {
                        if (hcsParameters.isServiceAlreadyExist)
                        {
                            //sending accepted notification 
                            notification.sendNotification(true, true, string.Empty, string.Empty, ref e2eData,false);
                            Logger.Write(hcsParameters.OrderKey + "," + Accepted + "," + AcceptedNotificationSent, Logger.TypeEnum.HCSWarrantyMessageTrace);
                            if (hcsParameters.isAHT)
                                CeaseAHTHCSWarrantyService(hcsParameters, notification, ref e2eData);
                            else
                                CeaseHCSServiceInstance(hcsParameters, notification, ref e2eData);
                        }
                        else
                        {
                            notification.sendNotification(false, false, "001", "The given bac doesn't have HCSWarranty Service", ref e2eData,true);
                            Logger.Write(hcsParameters.OrderKey + "," + Ignored + "," + IgnoredNotificationSent, Logger.TypeEnum.HCSWarrantyMessageTrace);
                        }
                    }
                    else
                    {
                        notification.sendNotification(false, false, "001", "Bad Request from MQService", ref e2eData,true);
                        Logger.Write(hcsParameters.OrderKey + "," + Ignored + "," + IgnoredNotificationSent, Logger.TypeEnum.HCSWarrantyMessageTrace);
                    }
                }              
            }

            catch (DnpException exception)
            {
                notification.sendNotification(false, false, "001", exception.Message, ref e2eData,true);
                Logger.Write(hcsParameters.OrderKey + "," + Ignored + "," + IgnoredNotificationSent + exception.Message, Logger.TypeEnum.HCSWarrantyMessageTrace);
                Logger.Write(hcsParameters.OrderKey + "," + Ignored + "," + IgnoredNotificationSent + exception.Message, Logger.TypeEnum.HCSWarrantyExceptionTrace);

            }
            catch (Exception ex)
            {
                notification.sendNotification(false, false, "001", ex.Message, ref e2eData,true);
                Logger.Write(hcsParameters.OrderKey + "," + Ignored + "," + IgnoredNotificationSent + ex.Message, Logger.TypeEnum.HCSWarrantyMessageTrace);
                Logger.Write(hcsParameters.OrderKey + "," + Ignored + "," + IgnoredNotificationSent + ex.Message, Logger.TypeEnum.HCSWarrantyExceptionTrace);
            }

        }
        public void CreateHCSServiceInstance(HCSParameters parameters, MSEOOrderNotification notification, ref E2ETransaction e2eData)
        {
            manageServiceInstanceV1Request1 request = new manageServiceInstanceV1Request1();
            manageServiceInstanceV1Response1 profileResponse = null;

            request.manageServiceInstanceV1Request = new ManageServiceInstanceV1Request();
            request.manageServiceInstanceV1Request.standardHeader = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
            request.manageServiceInstanceV1Request.standardHeader.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
            request.manageServiceInstanceV1Request.standardHeader.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
            request.manageServiceInstanceV1Request.standardHeader.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();

            List<ClientServiceInstanceCharacteristic> listClientSrvcInstnceChar = new List<ClientServiceInstanceCharacteristic>();
            ClientServiceInstanceCharacteristic clientSrvcChar = null;

            try
            {
                ClientServiceInstanceV1[] serviceInstance = new ClientServiceInstanceV1[1];
                serviceInstance[0] = new ClientServiceInstanceV1();

                serviceInstance[0].clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                serviceInstance[0].clientServiceInstanceIdentifier.value = HCS_SERVICECODE_NAMEPACE;
                serviceInstance[0].clientServiceInstanceStatus = new ClientServiceInstanceStatus();
                serviceInstance[0].clientServiceInstanceStatus.value = ACTIVE;
                serviceInstance[0].action = ACTION_INSERT;
                List<ServiceIdentity> listServiceIdentity = new List<ServiceIdentity>();
                ServiceIdentity serviceIdentity = new ServiceIdentity();
                serviceIdentity.domain = BACID_IDENTIFER_NAMEPACE;
                serviceIdentity.value = parameters.BillAccountNumber;
                serviceIdentity.action = ACTION_LINK;

                listServiceIdentity.Add(serviceIdentity);

                serviceInstance[0].serviceIdentity = listServiceIdentity.ToArray();

                clientSrvcChar = new ClientServiceInstanceCharacteristic();
                clientSrvcChar.name = "CUSTOMERID";
                clientSrvcChar.value = parameters.CustomerID;
                clientSrvcChar.action = ACTION_INSERT;
                listClientSrvcInstnceChar.Add(clientSrvcChar);

                clientSrvcChar = new ClientServiceInstanceCharacteristic();
                clientSrvcChar.name = "CONTRACTREF";
                clientSrvcChar.value = string.IsNullOrEmpty(parameters.ContarctRefNum)?" ":parameters.ContarctRefNum;
                clientSrvcChar.action = ACTION_INSERT;
                listClientSrvcInstnceChar.Add(clientSrvcChar);

                clientSrvcChar = new ClientServiceInstanceCharacteristic();
                clientSrvcChar.name = "ASSETINTEGRATIONID";
                clientSrvcChar.value = parameters.AssetIntegartioID;
                clientSrvcChar.action = ACTION_INSERT;
                listClientSrvcInstnceChar.Add(clientSrvcChar);

                clientSrvcChar = new ClientServiceInstanceCharacteristic();
                clientSrvcChar.name = "SERVICEEXPIRYDATE";
                clientSrvcChar.value = parameters.ServiceExpairyDate;
                clientSrvcChar.action = ACTION_INSERT;
                listClientSrvcInstnceChar.Add(clientSrvcChar);

                clientSrvcChar = new ClientServiceInstanceCharacteristic();
                clientSrvcChar.name = "ACT_TYPE";
                clientSrvcChar.value = parameters.SaleType;
                clientSrvcChar.action = ACTION_INSERT;
                listClientSrvcInstnceChar.Add(clientSrvcChar);

                serviceInstance[0].clientServiceInstanceCharacteristic = listClientSrvcInstnceChar.ToArray();
    
                ManageServiceInstanceV1Req manageServiceInstanceV1Req1 = new ManageServiceInstanceV1Req();
                manageServiceInstanceV1Req1.clientServiceInstanceV1 = serviceInstance[0];
                
                request.manageServiceInstanceV1Request.manageServiceInstanceV1Req = manageServiceInstanceV1Req1;

                e2eData.logMessage(StartedDnPCall, "");
                e2eData.startOutboundCall(ConfigurationManager.AppSettings["destinationDnP"].ToString(), E2ETransaction.vREQ, HCS_SERVICECODE_NAMEPACE);
                
                request.manageServiceInstanceV1Request.standardHeader.e2e = new BT.SaaS.IspssAdapter.Dnp.E2E();
                request.manageServiceInstanceV1Request.standardHeader.e2e.E2EDATA = e2eData.toString();

                Logger.Write(parameters.OrderKey + "," + MakingDnPCall + "," + SendingRequestToDNP, Logger.TypeEnum.HCSWarrantyMessageTrace);
                MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_IN_LOG, parameters.OrderKey, request.SerializeObject(), ConfigurationManager.AppSettings["HCSProductCode"].ToString());

                profileResponse = DnpWrapper.manageServiceInstanceV1Thomas(request, parameters.OrderKey);

                MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_OUT_LOG, parameters.OrderKey, profileResponse.SerializeObject(), ConfigurationManager.AppSettings["HCSProductCode"].ToString());
                Logger.Write(parameters.OrderKey + "," + GotResponseFromDnP + "," + GotResponseFromDnP, Logger.TypeEnum.HCSWarrantyMessageTrace);

                if (profileResponse != null
                    && profileResponse.manageServiceInstanceV1Response != null
                    && profileResponse.manageServiceInstanceV1Response.standardHeader != null
                    && profileResponse.manageServiceInstanceV1Response.standardHeader.serviceState != null
                    && profileResponse.manageServiceInstanceV1Response.standardHeader.serviceState.stateCode != null
                    && profileResponse.manageServiceInstanceV1Response.standardHeader.serviceState.stateCode == "0")
                {
                    //sending completed notification
                    //notification.NotificationResponse.StandardHeader.E2e.E2EDATA = profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA;
                    e2eData.logMessage(GotResponseFromDnP, "");
                    if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.standardHeader != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA))
                        e2eData.endOutboundCall(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA);
                    else
                        e2eData.endOutboundCall(e2eData.toString());
                   
                    notification.sendNotification(true, false, "004", string.Empty, ref e2eData,true);
                    Logger.Write(parameters.OrderKey + "," + Completed + "," + CompletedNotificationSent, Logger.TypeEnum.HCSWarrantyMessageTrace);                
                }
                else
                {
                    if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res != null && profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res.messages != null && profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res.messages[0].description != null)
                    {
                        string errorMessage = profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res.messages[0].description;
                        Logger.Write(parameters.OrderKey + "," + Failed + "," + FailedErrorMessage + errorMessage, Logger.TypeEnum.HCSWarrantyExceptionTrace);
                        if (errorMessage.Contains("Administrator"))
                        {
                            Logger.Write(parameters.OrderKey + "," + Errored + "," + DnPAdminstratorFailedResponse + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.HCSWarrantyExceptionTrace);
                            Logger.Write(parameters.OrderKey + "," + Failed + "," + FailureResponseFromDnP + "," + errorMessage, Logger.TypeEnum.HCSWarrantyMessageTrace);
                        }
                        else
                        {
                            Logger.Write(parameters.OrderKey + "," + Failed + "," + FailureResponseFromDnP + errorMessage, Logger.TypeEnum.HCSWarrantyMessageTrace);

                            e2eData.businessError(GotResponseFromDnPWithBusinessError, errorMessage);
                            if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.standardHeader != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA))
                                e2eData.endOutboundCall(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA);
                            else
                                e2eData.endOutboundCall(e2eData.toString());
                            //sending failure notification
                            notification.sendNotification(false, false, "777", errorMessage, ref e2eData,true);
                            Logger.Write(parameters.OrderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.HCSWarrantyMessageTrace);
                        }
                    }
                    else
                    {
                        Logger.Write(parameters.OrderKey + "," + Failed + "," + NullResponseFromDnP, Logger.TypeEnum.HCSWarrantyMessageTrace);

                        e2eData.businessError(GotResponseFromDnPWithBusinessError, "Response is null from DnP");
                        if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.standardHeader != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());
                        //sending failure notification
                        notification.sendNotification(false, false, "777", "Response is null from DnP", ref e2eData,true);
                        Logger.Write(parameters.OrderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.HCSWarrantyMessageTrace);
                    }
                }

            }
            catch(Exception ex)
            {
                Logger.Write(parameters.OrderKey + "," + Failed + "," + FailureResponseFromDnP + ex.Message, Logger.TypeEnum.HCSWarrantyMessageTrace);

                //sending failure notification
                //notification.NotificationResponse.StandardHeader.E2e.E2EDATA = e2eData;
                e2eData.businessError(GotResponseFromDnPWithBusinessError, ex.Message);
                if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.standardHeader != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA))
                    e2eData.endOutboundCall(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA);
                else
                    e2eData.endOutboundCall(e2eData.toString());

                notification.sendNotification(false, false, "777", ex.Message, ref e2eData,true);
                Logger.Write(parameters.OrderKey + "," + Failed + "," + FailedErrorMessage + ex.Message, Logger.TypeEnum.HCSWarrantyExceptionTrace);
                Logger.Write(parameters.OrderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.HCSWarrantyMessageTrace);
            }
            finally
            {
                listClientSrvcInstnceChar = null;
            }
        }
        public void CreateAHTHCSServiceInstance(HCSParameters parameters, MSEOOrderNotification notification, ref E2ETransaction e2eData)
        {
            manageClientProfileV1Response1 profileResponse1 = null;
            manageClientProfileV1Request1 profileRequest1 = new manageClientProfileV1Request1();
            profileRequest1.manageClientProfileV1Request = new ManageClientProfileV1Request();

            BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock headerBlock = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
            headerBlock.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
            headerBlock.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
            headerBlock.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();

            ManageClientProfileV1Req manageClientProfileV1Req1 = new ManageClientProfileV1Req();
            manageClientProfileV1Req1.clientProfileV1 = new ClientProfileV1();
           
            List<ClientServiceInstanceCharacteristic> listClientSrvcInstnceChar = new List<ClientServiceInstanceCharacteristic>();
            ClientServiceInstanceCharacteristic clientSrvcChar = null;

            try
            {
                ClientServiceInstanceV1[] serviceInstance = new ClientServiceInstanceV1[1];               
                serviceInstance[0]=new ClientServiceInstanceV1();

                serviceInstance[0].clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                serviceInstance[0].clientServiceInstanceIdentifier.value = HCS_SERVICECODE_NAMEPACE;
                serviceInstance[0].clientServiceInstanceStatus = new ClientServiceInstanceStatus();
                serviceInstance[0].clientServiceInstanceStatus.value = ACTIVE;
                serviceInstance[0].action = ACTION_INSERT;

                if (parameters.isAHT)
                {
                    manageClientProfileV1Req1.clientProfileV1.client = new Client();
                    manageClientProfileV1Req1.clientProfileV1.client.action = ACTION_UPDATE;

                    ClientIdentity[] clientIdentity=new ClientIdentity[1];
                    clientIdentity[0] = new ClientIdentity();

                    clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientIdentity[0].managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;
                    clientIdentity[0].value = parameters.BillAccountNumber;
                    clientIdentity[0].action = ACTION_SEARCH;

                    ClientIdentityValidation[] clientidentityvalidation = new ClientIdentityValidation[1];
                    clientidentityvalidation[0] = new ClientIdentityValidation();

                    clientidentityvalidation[0].name = "ACT_TYPE";
                    clientidentityvalidation[0].value = parameters.SaleType;
                    clientidentityvalidation[0].action = ACTION_INSERT;

                    clientIdentity[0].clientIdentityValidation = clientidentityvalidation;

                    manageClientProfileV1Req1.clientProfileV1.client.clientIdentity = clientIdentity;

                    ClientServiceRole[] serviceRole = new ClientServiceRole[2];
                    serviceRole[0] = new ClientServiceRole();
                    serviceRole[0].id = ADMIN_ROLE;
                    serviceRole[0].clientServiceRoleStatus = new ClientServiceRoleStatus();
                    serviceRole[0].clientServiceRoleStatus.value = ACTIVE;
                    serviceRole[0].clientIdentity = new ClientIdentity[1];
                    serviceRole[0].clientIdentity[0] = new ClientIdentity();
                    serviceRole[0].clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                    serviceRole[0].clientIdentity[0].managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;
                    serviceRole[0].clientIdentity[0].value = parameters.BillAccountNumber;
                    serviceRole[0].clientIdentity[0].action = ACTION_INSERT;
                    serviceRole[0].action = ACTION_INSERT;

                    serviceRole[1] = new ClientServiceRole();
                    serviceRole[1].id = DEFAULT_ROLE;
                    serviceRole[1].clientServiceRoleStatus = new ClientServiceRoleStatus();
                    serviceRole[1].clientServiceRoleStatus.value = ACTIVE;
                    serviceRole[1].clientIdentity = new ClientIdentity[1];
                    serviceRole[1].clientIdentity[0] = new ClientIdentity();
                    serviceRole[1].clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                    serviceRole[1].clientIdentity[0].managedIdentifierDomain.value = BTCOM;
                    serviceRole[1].clientIdentity[0].value = parameters.BtOneId;
                    serviceRole[1].clientIdentity[0].action = ACTION_INSERT;
                    serviceRole[1].action = ACTION_INSERT;

                    serviceInstance[0].clientServiceRole = serviceRole;
                }

                List<ServiceIdentity> listServiceIdentity = new List<ServiceIdentity>();
                ServiceIdentity serviceIdentity = new ServiceIdentity();
                serviceIdentity.domain = BACID_IDENTIFER_NAMEPACE;
                serviceIdentity.value = parameters.BillAccountNumber;
                serviceIdentity.action = ACTION_LINK;

                listServiceIdentity.Add(serviceIdentity);

                serviceInstance[0].serviceIdentity = listServiceIdentity.ToArray();

                clientSrvcChar = new ClientServiceInstanceCharacteristic();
                clientSrvcChar.name = "CUSTOMERID";
                clientSrvcChar.value = parameters.CustomerID;
                clientSrvcChar.action = ACTION_INSERT;
                listClientSrvcInstnceChar.Add(clientSrvcChar);

                clientSrvcChar = new ClientServiceInstanceCharacteristic();
                clientSrvcChar.name = "CONTRACTREF";
                clientSrvcChar.value = string.IsNullOrEmpty(parameters.ContarctRefNum)? " " :parameters.ContarctRefNum;
                clientSrvcChar.action = ACTION_INSERT;
                listClientSrvcInstnceChar.Add(clientSrvcChar);

                clientSrvcChar = new ClientServiceInstanceCharacteristic();
                clientSrvcChar.name = "ASSETINTEGRATIONID";
                clientSrvcChar.value = parameters.AssetIntegartioID;
                clientSrvcChar.action = ACTION_INSERT;
                listClientSrvcInstnceChar.Add(clientSrvcChar);

                clientSrvcChar = new ClientServiceInstanceCharacteristic();
                clientSrvcChar.name = "SERVICEEXPIRYDATE";
                clientSrvcChar.value = parameters.ServiceExpairyDate;
                clientSrvcChar.action = ACTION_INSERT;
                listClientSrvcInstnceChar.Add(clientSrvcChar);

                clientSrvcChar = new ClientServiceInstanceCharacteristic();
                clientSrvcChar.name = "ACT_TYPE";
                clientSrvcChar.value = parameters.SaleType;
                clientSrvcChar.action = ACTION_INSERT;
                listClientSrvcInstnceChar.Add(clientSrvcChar);

                serviceInstance[0].clientServiceInstanceCharacteristic = listClientSrvcInstnceChar.ToArray();               

                manageClientProfileV1Req1.clientProfileV1.clientServiceInstanceV1 = serviceInstance;                

                profileRequest1.manageClientProfileV1Request.standardHeader = headerBlock;
                profileRequest1.manageClientProfileV1Request.manageClientProfileV1Req = manageClientProfileV1Req1;

                e2eData.logMessage(StartedDnPCall, "");
                e2eData.startOutboundCall(ConfigurationManager.AppSettings["destinationDnP"].ToString(), E2ETransaction.vREQ, HCS_SERVICECODE_NAMEPACE);

                profileRequest1.manageClientProfileV1Request.standardHeader.e2e = new BT.SaaS.IspssAdapter.Dnp.E2E();
                profileRequest1.manageClientProfileV1Request.standardHeader.e2e.E2EDATA = e2eData.toString();

                //need to add log details 
                Logger.Write(parameters.OrderKey + "," + MakingDnPCall + "," + SendingRequestToDNP, Logger.TypeEnum.HCSWarrantyMessageTrace);// need to add message trace 
                MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_IN_LOG, parameters.OrderKey, profileRequest1.SerializeObject(), ConfigurationManager.AppSettings["HCSProductCode"].ToString());

                profileResponse1 = DnpWrapper.manageClientProfileV1Thomas(profileRequest1, parameters.OrderKey);

                MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_OUT_LOG, parameters.OrderKey, profileResponse1.SerializeObject(), ConfigurationManager.AppSettings["HCSProductCode"].ToString());
                Logger.Write(parameters.OrderKey + "," + GotResponseFromDnP + "," + ReceivedResponsefromDNP, Logger.TypeEnum.HCSWarrantyMessageTrace);

                if (profileResponse1 != null
                   && profileResponse1.manageClientProfileV1Response != null
                   && profileResponse1.manageClientProfileV1Response.standardHeader != null
                   && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState != null
                   && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState.stateCode != null
                   && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState.stateCode == "0")
                {
                    e2eData.logMessage(GotResponseFromDnP, "");
                    if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                        e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                    else
                        e2eData.endOutboundCall(e2eData.toString());
                    
                    notification.sendNotification(true, false, "004", string.Empty, ref e2eData,true);
                    Logger.Write(parameters.OrderKey + "," + Completed + "," + CompletedNotificationSent, Logger.TypeEnum.HCSWarrantyMessageTrace);                    
                }
                else
                {
                    if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res != null && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages != null && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description != null)
                    {
                        string errorMessage = profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description;
                        Logger.Write(parameters.OrderKey + "," + Failed + "," + FailedErrorMessage + errorMessage, Logger.TypeEnum.HCSWarrantyExceptionTrace);
                        if (errorMessage.Contains("Administrator"))
                        {
                            Logger.Write(parameters.OrderKey + "," + Errored + "," + DnPAdminstratorFailedResponse + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.HCSWarrantyExceptionTrace);
                            Logger.Write(parameters.OrderKey + "," + Failed + "," + FailureResponseFromDnP + "," + errorMessage, Logger.TypeEnum.HCSWarrantyMessageTrace);                       
                        }
                        else
                        {
                            Logger.Write(parameters.OrderKey + "," + Failed + "," + FailureResponseFromDnP + errorMessage, Logger.TypeEnum.HCSWarrantyMessageTrace);

                            e2eData.businessError(GotResponseFromDnPWithBusinessError, errorMessage);
                            if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                                e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                            else
                                e2eData.endOutboundCall(e2eData.toString());
                            //sending failure notification
                            notification.sendNotification(false, false, "777", errorMessage, ref e2eData,true);
                            Logger.Write(parameters.OrderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.HCSWarrantyMessageTrace);
                        }
                    }
                    else
                    {
                        Logger.Write(parameters.OrderKey + "," + Failed + "," + NullResponseFromDnP, Logger.TypeEnum.HCSWarrantyMessageTrace);

                        e2eData.businessError(GotResponseFromDnPWithBusinessError, "Response is null from DnP");
                        if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());
                        //sending failure notification
                        notification.sendNotification(false, false, "777", "Response is null from DnP", ref e2eData,true);
                        Logger.Write(parameters.OrderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.HCSWarrantyMessageTrace);
                    }
                }

            }
            catch (Exception ex)
            {
                Logger.Write(parameters.OrderKey + "," + Failed + "," + FailureResponseFromDnP + ex.Message, Logger.TypeEnum.HCSWarrantyMessageTrace);

                //sending failure notification
                //notification.NotificationResponse.StandardHeader.E2e.E2EDATA = e2eData;
                e2eData.businessError(GotResponseFromDnPWithBusinessError, ex.Message);
                if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                    e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                else
                    e2eData.endOutboundCall(e2eData.toString());

                notification.sendNotification(false, false, "777", ex.Message, ref e2eData,true);
                Logger.Write(parameters.OrderKey + "," + Failed + "," + FailedErrorMessage + ex.Message, Logger.TypeEnum.HCSWarrantyExceptionTrace);
                Logger.Write(parameters.OrderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.HCSWarrantyMessageTrace);
            }
            finally
            {
                listClientSrvcInstnceChar = null;
            }
        }

        public void CeaseHCSServiceInstance(HCSParameters parameters, MSEOOrderNotification notification, ref E2ETransaction e2eData)
        {
            manageServiceInstanceV1Request1 request = new manageServiceInstanceV1Request1();
            manageServiceInstanceV1Response1 profileResponse = null;

            request.manageServiceInstanceV1Request = new ManageServiceInstanceV1Request();
            request.manageServiceInstanceV1Request.standardHeader = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
            request.manageServiceInstanceV1Request.standardHeader.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
            request.manageServiceInstanceV1Request.standardHeader.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
            request.manageServiceInstanceV1Request.standardHeader.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();
            
            ManageServiceInstanceV1Req manageServiceInstanceV1Req1 = new ManageServiceInstanceV1Req();
            
            try
            {
                ClientServiceInstanceV1[] serviceInstance = new ClientServiceInstanceV1[1];
                serviceInstance[0] = new ClientServiceInstanceV1();

                serviceInstance[0].clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                serviceInstance[0].clientServiceInstanceIdentifier.value = HCS_SERVICECODE_NAMEPACE;
                serviceInstance[0].clientServiceInstanceStatus = new ClientServiceInstanceStatus();
                serviceInstance[0].clientServiceInstanceStatus.value = ACTIVE;
                

                List<ServiceIdentity> listServiceIdentity = new List<ServiceIdentity>();
                ServiceIdentity serviceIdentity = new ServiceIdentity();
                serviceIdentity.domain = BACID_IDENTIFER_NAMEPACE;
                serviceIdentity.value = parameters.BillAccountNumber;
                serviceIdentity.action = ACTION_SEARCH;

                serviceInstance[0].action = ACTION_DELETE;

                listServiceIdentity.Add(serviceIdentity);

                serviceInstance[0].serviceIdentity = listServiceIdentity.ToArray();                
                                
                manageServiceInstanceV1Req1.clientServiceInstanceV1 = serviceInstance[0];

                request.manageServiceInstanceV1Request.manageServiceInstanceV1Req = manageServiceInstanceV1Req1;

                e2eData.logMessage(StartedDnPCall, "");
                e2eData.startOutboundCall(ConfigurationManager.AppSettings["destinationDnP"].ToString(), E2ETransaction.vREQ, HCS_SERVICECODE_NAMEPACE);

                request.manageServiceInstanceV1Request.standardHeader.e2e = new BT.SaaS.IspssAdapter.Dnp.E2E();
                request.manageServiceInstanceV1Request.standardHeader.e2e.E2EDATA = e2eData.toString();

                Logger.Write(parameters.OrderKey + "," + MakingDnPCall + "," + SendingRequestToDNP, Logger.TypeEnum.HCSWarrantyMessageTrace);
                MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_IN_LOG, parameters.OrderKey, request.SerializeObject(), ConfigurationManager.AppSettings["HCSProductCode"].ToString());

                profileResponse = DnpWrapper.manageServiceInstanceV1Thomas(request, parameters.OrderKey);

                MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_OUT_LOG, parameters.OrderKey, profileResponse.SerializeObject(), ConfigurationManager.AppSettings["HCSProductCode"].ToString());
                Logger.Write(parameters.OrderKey + "," + GotResponseFromDnP + "," + GotResponseFromDnP, Logger.TypeEnum.HCSWarrantyMessageTrace);

                if (profileResponse != null
                    && profileResponse.manageServiceInstanceV1Response != null
                    && profileResponse.manageServiceInstanceV1Response.standardHeader != null
                    && profileResponse.manageServiceInstanceV1Response.standardHeader.serviceState != null
                    && profileResponse.manageServiceInstanceV1Response.standardHeader.serviceState.stateCode != null
                    && profileResponse.manageServiceInstanceV1Response.standardHeader.serviceState.stateCode == "0")
                {
                    //sending completed notification
                    //notification.NotificationResponse.StandardHeader.E2e.E2EDATA = profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA;
                    e2eData.logMessage(GotResponseFromDnP, "");
                    if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.standardHeader != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA))
                        e2eData.endOutboundCall(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA);
                    else
                        e2eData.endOutboundCall(e2eData.toString());

                    notification.sendNotification(true, false, "004", string.Empty, ref e2eData,true);
                    Logger.Write(parameters.OrderKey + "," + Completed + "," + CompletedNotificationSent, Logger.TypeEnum.HCSWarrantyMessageTrace);
                }
                else
                {
                    if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res != null && profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res.messages != null && profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res.messages[0].description != null)
                    {
                        string errorMessage = profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res.messages[0].description;
                        Logger.Write(parameters.OrderKey + "," + Failed + "," + FailedErrorMessage + errorMessage, Logger.TypeEnum.HCSWarrantyExceptionTrace);
                        if (errorMessage.Contains("Administrator"))
                        {
                            Logger.Write(parameters.OrderKey + "," + Errored + "," + DnPAdminstratorFailedResponse + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.HCSWarrantyExceptionTrace);
                            Logger.Write(parameters.OrderKey + "," + Failed + "," + FailureResponseFromDnP + "," + errorMessage, Logger.TypeEnum.HCSWarrantyMessageTrace);
                        }
                        else
                        {
                            Logger.Write(parameters.OrderKey + "," + Failed + "," + FailureResponseFromDnP + errorMessage, Logger.TypeEnum.HCSWarrantyMessageTrace);

                            e2eData.businessError(GotResponseFromDnPWithBusinessError, errorMessage);
                            if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.standardHeader != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA))
                                e2eData.endOutboundCall(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA);
                            else
                                e2eData.endOutboundCall(e2eData.toString());
                            //sending failure notification
                            notification.sendNotification(false, false, "777", errorMessage, ref e2eData,true);
                            Logger.Write(parameters.OrderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.HCSWarrantyMessageTrace);
                        }
                    }
                    else
                    {
                        Logger.Write(parameters.OrderKey + "," + Failed + "," + NullResponseFromDnP, Logger.TypeEnum.HCSWarrantyMessageTrace);

                        e2eData.businessError(GotResponseFromDnPWithBusinessError, "Response is null from DnP");
                        if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.standardHeader != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());
                        //sending failure notification
                        notification.sendNotification(false, false, "777", "Response is null from DnP", ref e2eData,true);
                        Logger.Write(parameters.OrderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.HCSWarrantyMessageTrace);
                    }
                }

            }
            catch (Exception ex)
            {
                Logger.Write(parameters.OrderKey + "," + Failed + "," + FailureResponseFromDnP + ex.Message, Logger.TypeEnum.HCSWarrantyMessageTrace);

                //sending failure notification
                //notification.NotificationResponse.StandardHeader.E2e.E2EDATA = e2eData;
                e2eData.businessError(GotResponseFromDnPWithBusinessError, ex.Message);
                if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.standardHeader != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA))
                    e2eData.endOutboundCall(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA);
                else
                    e2eData.endOutboundCall(e2eData.toString());

                notification.sendNotification(false, false, "777", ex.Message, ref e2eData,true);
                Logger.Write(parameters.OrderKey + "," + Failed + "," + FailedErrorMessage + ex.Message, Logger.TypeEnum.HCSWarrantyExceptionTrace);
                Logger.Write(parameters.OrderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.HCSWarrantyMessageTrace);
            }           
        }

        public void CeaseAHTHCSWarrantyService(HCSParameters parameters, MSEOOrderNotification notification, ref E2ETransaction e2eData)
        {
            manageClientProfileV1Response1 profileResponse1 = null;
            manageClientProfileV1Request1 profileRequest1 = new manageClientProfileV1Request1();
            profileRequest1.manageClientProfileV1Request = new ManageClientProfileV1Request();

            BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock headerBlock = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
            headerBlock.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
            headerBlock.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
            headerBlock.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();

            ManageClientProfileV1Req manageClientProfileV1Req1 = new ManageClientProfileV1Req();
            manageClientProfileV1Req1.clientProfileV1 = new ClientProfileV1();

            try
            {
                manageClientProfileV1Req1.clientProfileV1.client = new Client();
                manageClientProfileV1Req1.clientProfileV1.client.action = ACTION_UPDATE;

                ClientIdentity[] clientIdentity = new ClientIdentity[1];
                clientIdentity[0] = new ClientIdentity();

                clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                clientIdentity[0].managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;
                clientIdentity[0].value = parameters.BillAccountNumber;
                clientIdentity[0].action = ACTION_SEARCH;

                ClientIdentityValidation[] clientidentityvalidation = new ClientIdentityValidation[1];
                clientidentityvalidation[0] = new ClientIdentityValidation();

                clientidentityvalidation[0].name = "ACT_TYPE";
                clientidentityvalidation[0].action = ACTION_DELETE;

                clientIdentity[0].clientIdentityValidation = clientidentityvalidation;

                manageClientProfileV1Req1.clientProfileV1.client.clientIdentity = clientIdentity;

                ClientServiceInstanceV1[] serviceInstance = new ClientServiceInstanceV1[1];
                serviceInstance[0] = new ClientServiceInstanceV1();

                serviceInstance[0].clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                serviceInstance[0].clientServiceInstanceIdentifier.value = HCS_SERVICECODE_NAMEPACE;
                serviceInstance[0].clientServiceInstanceStatus = new ClientServiceInstanceStatus();
                serviceInstance[0].clientServiceInstanceStatus.value = ACTIVE;

                List<ServiceIdentity> listServiceIdentity = new List<ServiceIdentity>();
                ServiceIdentity serviceIdentity = new ServiceIdentity();
                serviceIdentity.domain = BACID_IDENTIFER_NAMEPACE;
                serviceIdentity.value = parameters.BillAccountNumber;
                serviceIdentity.action = ACTION_SEARCH;

                if (parameters.isAHT)
                {
                    ClientServiceRole[] serviceRole = new ClientServiceRole[2];
                    serviceRole[0] = new ClientServiceRole();
                    serviceRole[0].id = ADMIN_ROLE;
                    serviceRole[0].action = ACTION_DELETE;

                    serviceRole[1] = new ClientServiceRole();
                    serviceRole[1].id = DEFAULT_ROLE;
                    serviceRole[1].action = ACTION_DELETE;

                    serviceInstance[0].clientServiceRole = serviceRole;
                }
                serviceInstance[0].action = ACTION_DELETE;

                listServiceIdentity.Add(serviceIdentity);

                serviceInstance[0].serviceIdentity = listServiceIdentity.ToArray();

                manageClientProfileV1Req1.clientProfileV1.clientServiceInstanceV1 = serviceInstance;

                profileRequest1.manageClientProfileV1Request.standardHeader = headerBlock;
                profileRequest1.manageClientProfileV1Request.manageClientProfileV1Req = manageClientProfileV1Req1;

                e2eData.logMessage(StartedDnPCall, "");
                e2eData.startOutboundCall(ConfigurationManager.AppSettings["destinationDnP"].ToString(), E2ETransaction.vREQ, HCS_SERVICECODE_NAMEPACE);

                profileRequest1.manageClientProfileV1Request.standardHeader.e2e = new BT.SaaS.IspssAdapter.Dnp.E2E();
                profileRequest1.manageClientProfileV1Request.standardHeader.e2e.E2EDATA = e2eData.toString();

                //need to add log details 
                Logger.Write(parameters.OrderKey + "," + MakingDnPCall + "," + SendingRequestToDNP, Logger.TypeEnum.HCSWarrantyMessageTrace);// need to add message trace 
                MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_IN_LOG, parameters.OrderKey, profileRequest1.SerializeObject(), ConfigurationManager.AppSettings["HCSProductCode"].ToString());

                profileResponse1 = DnpWrapper.manageClientProfileV1Thomas(profileRequest1, parameters.OrderKey);

                MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_OUT_LOG, parameters.OrderKey, profileResponse1.SerializeObject(), ConfigurationManager.AppSettings["HCSProductCode"].ToString());
                Logger.Write(parameters.OrderKey + "," + GotResponseFromDnP + "," + ReceivedResponsefromDNP, Logger.TypeEnum.HCSWarrantyMessageTrace);

                if (profileResponse1 != null
                   && profileResponse1.manageClientProfileV1Response != null
                   && profileResponse1.manageClientProfileV1Response.standardHeader != null
                   && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState != null
                   && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState.stateCode != null
                   && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState.stateCode == "0")
                {
                    e2eData.logMessage(GotResponseFromDnP, "");
                    if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                        e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                    else
                        e2eData.endOutboundCall(e2eData.toString());

                    notification.sendNotification(true, false, "004", string.Empty, ref e2eData,true);
                    Logger.Write(parameters.OrderKey + "," + Completed + "," + CompletedNotificationSent, Logger.TypeEnum.HCSWarrantyMessageTrace);
                }
                else
                {
                    if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res != null && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages != null && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description != null)
                    {
                        string errorMessage = profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description;
                        Logger.Write(parameters.OrderKey + "," + Failed + "," + FailedErrorMessage + errorMessage, Logger.TypeEnum.HCSWarrantyExceptionTrace);
                        if (errorMessage.Contains("Administrator"))
                        {
                            Logger.Write(parameters.OrderKey + "," + Errored + "," + DnPAdminstratorFailedResponse + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.HCSWarrantyExceptionTrace);
                            Logger.Write(parameters.OrderKey + "," + Failed + "," + FailureResponseFromDnP + "," + errorMessage, Logger.TypeEnum.HCSWarrantyMessageTrace);
                        }
                        else
                        {
                            Logger.Write(parameters.OrderKey + "," + Failed + "," + FailureResponseFromDnP + errorMessage, Logger.TypeEnum.HCSWarrantyMessageTrace);

                            e2eData.businessError(GotResponseFromDnPWithBusinessError, errorMessage);
                            if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                                e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                            else
                                e2eData.endOutboundCall(e2eData.toString());
                            //sending failure notification
                            notification.sendNotification(false, false, "777", errorMessage, ref e2eData,true);
                            Logger.Write(parameters.OrderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.HCSWarrantyMessageTrace);
                        }
                    }
                    else
                    {
                        Logger.Write(parameters.OrderKey + "," + Failed + "," + NullResponseFromDnP, Logger.TypeEnum.HCSWarrantyMessageTrace);

                        e2eData.businessError(GotResponseFromDnPWithBusinessError, "Response is null from DnP");
                        if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());
                        //sending failure notification
                        notification.sendNotification(false, false, "777", "Response is null from DnP", ref e2eData,true);
                        Logger.Write(parameters.OrderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.HCSWarrantyMessageTrace);
                    }
                }

            }
            catch (Exception ex)
            {
                Logger.Write(parameters.OrderKey + "," + Failed + "," + FailureResponseFromDnP + ex.Message, Logger.TypeEnum.HCSWarrantyMessageTrace);

                //sending failure notification
                //notification.NotificationResponse.StandardHeader.E2e.E2EDATA = e2eData;
                e2eData.businessError(GotResponseFromDnPWithBusinessError, ex.Message);
                if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                    e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                else
                    e2eData.endOutboundCall(e2eData.toString());

                notification.sendNotification(false, false, "777", ex.Message, ref e2eData,true);
                Logger.Write(parameters.OrderKey + "," + Failed + "," + FailedErrorMessage + ex.Message, Logger.TypeEnum.HCSWarrantyExceptionTrace);
                Logger.Write(parameters.OrderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.HCSWarrantyMessageTrace);
            }            
        }
    }
    public class HCSParameters
    {
        private string orderkey = string.Empty;

        public string OrderKey
        {
            get { return orderkey; }
            set { orderkey = value; }
        }

        private string orderAction = string.Empty;

        public string OrderAction
        {
            get { return orderAction; }
            set { orderAction = value; }
        }

        private bool isaht = false;

        public bool isAHT
        {
            get { return isaht; }
            set { isaht = value; }
        }

        private string btoneid = string.Empty;

        public string BtOneId
        {
            get { return btoneid; }
            set { btoneid = value; }
        }

        private string billAccountNumber = string.Empty;

        public string BillAccountNumber
        {
            get { return billAccountNumber; }
            set { billAccountNumber = value; }
        }

        private string customerID = string.Empty;

        public string CustomerID
        {
            get { return customerID; }
            set { customerID = value; }
        }

        private string AssetIntegrationId = string.Empty;

        public string AssetIntegartioID
        {
            get { return AssetIntegrationId; }
            set { AssetIntegrationId = value; }
        }

        private string contractrefnum = string.Empty;

        public string ContarctRefNum
        {
            get { return contractrefnum; }
            set { contractrefnum = value; }
        }

        private string saleType = string.Empty;

        public string SaleType
        {
            get { return saleType; }
            set { saleType = value; }
        }

        private int warrantyPeriod = 0;

        public int WarrantyPeriod
        {
            get { return warrantyPeriod; }
            set { warrantyPeriod = value; }
        }

        private string warrantyUnit = string.Empty;

        public string WarrantyUnit
        {
            get { return warrantyUnit; }
            set { warrantyUnit = value; }
        }

        private string serviceExpairyDate = string.Empty;

        public string ServiceExpairyDate
        {
            get { return serviceExpairyDate; }
            set { serviceExpairyDate = value; }
        }

        private bool isserviceAlreadyExist = false;

        public bool isServiceAlreadyExist
        {
            get { return isserviceAlreadyExist; }
            set { isserviceAlreadyExist = value; }
        }
    }
}
