using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;
using MSEO = BT.SaaS.MSEOAdapter;
using SaaSNS = BT.SaaS.Core.Shared.Entities;
using BT.SaaS.IspssAdapter;
using BT.SaaS.Core.MDMAPI.Entities;
using BT.SaaS.Core.MDMAPI.Entities.Product;
using BT.SaaS.Core.Shared.Entities;
using BT.SaaS.IspssAdapter.Dnp;
using com.bt.util.logging;

namespace BT.SaaS.MSEOAdapter
{
    public class NominumProductProcessor
    {
        #region Members

        private const string BACID_IDENTIFER_NAMEPACE = "VAS_BILLINGACCOUNT_ID";
        private const string STATUS_PENDING = "PENDING";
        private const string STATUS_PENDINGCEASE = "PENDING-CEASE";
        private const string STATUS_CEASED = "CEASED";
        private const string STATUS_ACTIVE = "ACTIVE";
        private const string STATUS_DISABLED = "DISABLED";

        string BillAccountNumber = string.Empty;
        string deviceId = string.Empty;        
        bool isBUTcalltoUpdatePCChoice = false;
        #endregion

        internal void MapNominumProduct(OrderItem orderItem, OrderActionEnum orderAction, ref RoleInstance roleInstance, string ProductCode, string OrderFrom, ref string action)
        {
            string cfsid = string.Empty;
            string NominumServiceStatus = string.Empty;
            
            List<ClientServiceInstanceV1> NominumServices = new List<ClientServiceInstanceV1>();

            if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToLower().Equals("billaccountnumber")) && !string.IsNullOrEmpty(orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("billaccountnumber")).FirstOrDefault().Value))
            {
                BillAccountNumber = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("billaccountnumber")).FirstOrDefault().Value;
            }
            if (string.IsNullOrEmpty(BillAccountNumber))
            {
                if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToLower().Equals("deviceid")) && !string.IsNullOrEmpty(orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("deviceid")).FirstOrDefault().Value))
                {
                    deviceId = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("deviceid")).FirstOrDefault().Value;
                }
                if (!string.IsNullOrEmpty(deviceId))
                { 
                    //call ESB using querycumster get call to get the bac & customerid.
                    //BillAccountNumber 
                    BT.ESB.RoBT.ManageCustomer.QueryCustomerResponse Response = ESBRestCallWrapper.GetQueryCustomer(orderItem);

                    if (Response != null && Response.customerAccount != null && Response.customerAccount.billingAccountList != null && Response.customerAccount.billingAccountList.Count() > 0
                        && Response.customerAccount.billingAccountList[0].accountNumber != null && !string.IsNullOrEmpty(Response.customerAccount.billingAccountList[0].accountNumber))
                    {
                        BillAccountNumber = Response.customerAccount.billingAccountList[0].accountNumber.ToString();

                        roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "billaccountnumber", Value = BillAccountNumber.ToString() });

                        if (!string.IsNullOrEmpty(Response.customerAccount.id))
                        {                          
                            roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "CUSTOMERID", Value = Response.customerAccount.id.ToString() });
                        }
                    }
                    else
                    {
                        throw new DnpException("Ignored " + ProductCode + " " + orderAction.ToString() + " action request as we haven't received BAC from ESB for the deviceid:"+deviceId+"received from BUT");                            
                    }
                }               

                isBUTcalltoUpdatePCChoice = true;                
                roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "isBUTcalltoUpdatePCChoice", Value = isBUTcalltoUpdatePCChoice.ToString() });
            }
            roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "orderAction", Value = action });
            ClientServiceInstanceV1[] gsiResponse = gsiResponse = DnpWrapper.getServiceInstanceV1(BillAccountNumber, BACID_IDENTIFER_NAMEPACE, null);
            if (gsiResponse != null)
            {
                //NominumServices = (from csi in gsiResponse.ToList()
                //                   where csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["CFService"], StringComparison.OrdinalIgnoreCase)
                //                    || csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["MessageManagerService"], StringComparison.OrdinalIgnoreCase)
                //                    || csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["NetworkSecurityService"], StringComparison.OrdinalIgnoreCase)
                //                   select csi).ToList();

                //ccp 78
                NominumServices = (from csi in gsiResponse.ToList()
                                   where csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["CFService"], StringComparison.OrdinalIgnoreCase)
                                    || csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["MessageManagerService"], StringComparison.OrdinalIgnoreCase)
                                    || csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["NetworkSecurityService"], StringComparison.OrdinalIgnoreCase)
                                    || csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["SFService"], StringComparison.OrdinalIgnoreCase)
                                   select csi).ToList();

                if (NominumServices != null && NominumServices.Count > 0)
                {
                    //Ignore the request if CF or NS Service is in pending or pending-cease state.
                    if (NominumServices.ToList().Exists(a => (a.clientServiceInstanceStatus.value.Equals(STATUS_PENDING, StringComparison.OrdinalIgnoreCase) || a.clientServiceInstanceStatus.value.Equals(STATUS_PENDINGCEASE, StringComparison.OrdinalIgnoreCase) && !a.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["MessageManagerService"], StringComparison.OrdinalIgnoreCase))))
                    {                      
                        var nominumService = NominumServices.ToList().Where(a => (a.clientServiceInstanceStatus.value.Equals(STATUS_PENDING, StringComparison.OrdinalIgnoreCase) || a.clientServiceInstanceStatus.value.Equals(STATUS_PENDINGCEASE, StringComparison.OrdinalIgnoreCase))).FirstOrDefault();
                        //Ignoring Message manager Service status in cases of CF disabled profile creation and CF cease.
                        if (!((orderAction.Equals(OrderActionEnum.cease) || (orderAction.Equals(OrderActionEnum.provide))) &&
                            (ProductCode.Equals(ConfigurationManager.AppSettings["CFProdCode"], StringComparison.OrdinalIgnoreCase)) &&
                            (nominumService.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["MessageManagerService"], StringComparison.OrdinalIgnoreCase))))
                        {
                            throw new DnpException("Ignored " + ProductCode + " " + orderAction.ToString() + " action request as " + nominumService.clientServiceInstanceIdentifier.value + " is in " + nominumService.clientServiceInstanceStatus.value + " state.");
                        }
                    }

                    //Get CFSID from any Nominum service.                   
                    cfsid = GetCFSIDFromNominumService(NominumServices, orderAction.ToString());
                }
            }
            //To check if cfsid is already added(for Cease scenarios)
            if (!roleInstance.Attributes.ToList().Exists(ri => ri.Name.Equals("CFSID", StringComparison.OrdinalIgnoreCase)))
            {
                roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "cfsid", Value = cfsid });
            }
            if (ProductCode.Equals(ConfigurationManager.AppSettings["CFProdCode"], StringComparison.OrdinalIgnoreCase))                
            {
                MapContentFilteringService(orderAction, NominumServices, orderItem, ref roleInstance);
            }
            //ccp 78
            if (ProductCode.Equals(ConfigurationManager.AppSettings["SSProdCode"], StringComparison.OrdinalIgnoreCase))
            {                
                 MapSafeSearchService(orderAction, NominumServices, orderItem, ref roleInstance);
            }
            else if (ProductCode.Equals(ConfigurationManager.AppSettings["MessageManagerProdCode"], StringComparison.OrdinalIgnoreCase))
            {
                MapMessageMangerService(orderAction, NominumServices, ref roleInstance, OrderFrom, ref action);
            }
            else if (ProductCode.Equals(ConfigurationManager.AppSettings["NetworkSecurityProdCode"], StringComparison.OrdinalIgnoreCase))
            {
                MapNetworkSecurityService(orderAction, NominumServices, ref roleInstance, ref action, OrderFrom);
            }
        }

        internal void MapMessageMangerService(OrderActionEnum orderAction, List<ClientServiceInstanceV1> NominumServices, ref RoleInstance roleInstance, string OrderFrom, ref string action)
        {
            bool isVASReactivate = false;
            bool IsAHTDone = false;
            bool actionOnCompleteNominumSubscription = false;
            string updateCFSIDAtRBSC = "false";

            GetClientProfileV1Res gcpresponse = DnpWrapper.GetClientProfileV1(BillAccountNumber, BACID_IDENTIFER_NAMEPACE);
            IsAHTDone = MSEOSaaSMapper.IsAHTDONE(gcpresponse, BillAccountNumber);
            roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "isAHTDone", Value = IsAHTDone.ToString() });

            if (orderAction == OrderActionEnum.amend)
            {
                if (NominumServices != null && NominumServices.Count > 0)
                {
                    if (NominumServices.ToList().Exists(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["CFService"].ToString(), StringComparison.OrdinalIgnoreCase)))
                    {
                        throw new DnpException("Ignored MessageManager amend request as ContentFiltering Service is present in DnP with BAC " + BillAccountNumber);
                    }
                    else if (NominumServices.ToList().Exists(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["MessageManagerService"].ToString(), StringComparison.OrdinalIgnoreCase)))
                    {
                        if (NominumServices.ToList().Where(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["MessageManagerService"].ToString(), StringComparison.OrdinalIgnoreCase)).FirstOrDefault().clientServiceInstanceStatus.value.Equals(STATUS_CEASED, StringComparison.OrdinalIgnoreCase))
                        {
                            isVASReactivate = true;
                            //SET WORKFLOW AS VAS REACTIVATE
                            action = OrderActionEnum.vasReactivate.ToString();
                            roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "ISVASREACTIVATE", Value = isVASReactivate.ToString() });

                        }
                        else
                        {
                            throw new DnpException("Ignored MessageManager amend request as Message Manager Service is present in DnP with BAC " + BillAccountNumber + " in Non Ceased state");
                        }
                    }
                    //Descision to act on complete nominum subscription or not.
                    //Update CFSID at RBSC if no nominum serivice is in active.
                    if (NominumServices.ToList().Exists(csi => csi.clientServiceInstanceStatus.value.Equals(STATUS_ACTIVE, StringComparison.OrdinalIgnoreCase)))
                    {
                        updateCFSIDAtRBSC = "false";
                        actionOnCompleteNominumSubscription = false;
                    }
                    else
                    {
                        updateCFSIDAtRBSC = "true";
                        actionOnCompleteNominumSubscription = true;
                    }
                }
                // Setting the attr to true to create complete subscription as there are no other services
                else
                {
                    updateCFSIDAtRBSC = "true";
                    actionOnCompleteNominumSubscription = true;
                }
                roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "UpdateCFSIDAtRBSC", Value = updateCFSIDAtRBSC.ToString() });
                roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "ActionOnCompleteNominumSubscription", Value = actionOnCompleteNominumSubscription.ToString() });


            }
            else if (orderAction == OrderActionEnum.cease)
            {
                if (NominumServices != null && NominumServices.Count > 0)
                {
                    if (NominumServices.ToList().Exists(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["MessageManagerService"].ToString(), StringComparison.OrdinalIgnoreCase)))
                    {
                        if (!NominumServices.ToList().Where(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["MessageManagerService"].ToString(), StringComparison.OrdinalIgnoreCase)).FirstOrDefault().clientServiceInstanceStatus.value.Equals(STATUS_ACTIVE, StringComparison.OrdinalIgnoreCase))
                        {
                            throw new DnpException("Ignored MessageManager cease request as the service is present in DnP for BAC: " + BillAccountNumber + " with status in Non Active Status");
                        }
                        else
                        {
                            roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "OrderFrom", Value = OrderFrom });
                            IEnumerable<string> ActiveServices = NominumServices.ToList().Where(x => x.clientServiceInstanceStatus.value.Equals(STATUS_ACTIVE, StringComparison.OrdinalIgnoreCase)).Select(y => y.clientServiceInstanceIdentifier.value);

                            // Delete subscription at Nominum only if 1  nominum service is linked to that bak at DNP.
                            if (ActiveServices.Count().Equals(1) && ActiveServices.ToList().FirstOrDefault().Equals(ConfigurationManager.AppSettings["MessageManagerService"], StringComparison.OrdinalIgnoreCase))
                            {
                                actionOnCompleteNominumSubscription = true;
                                updateCFSIDAtRBSC = "true";
                            }
                            roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "UpdateCFSIDAtRBSC", Value = updateCFSIDAtRBSC.ToString() });
                            roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "ActionOnCompleteNominumSubscription", Value = actionOnCompleteNominumSubscription.ToString() });
                        }
                    }
                    else
                    {
                        throw new DnpException("Ignored MessageManager cease request as there is no service present in DnP for BAC: " + BillAccountNumber);
                    }
                }
                else
                {
                    throw new DnpException("Ignored MessageManager cease request as there is no service present in DnP for BAC: " + BillAccountNumber);
                }
            }

        }

        internal void MapContentFilteringService(OrderActionEnum orderAction, List<ClientServiceInstanceV1> NominumServices, OrderItem orderItem, ref RoleInstance roleInstance)
        {
            bool isDisabledProfile = false;
            bool ServiceInstanceExists = false;
            bool isAHTDone = false;
            GetClientProfileV1Res bacProfileResponse = null;
            List<string> OldrbsidList = new List<string>();
            bool isVASReactivate = false;
            bool actionOnCompleteNominumSubscription = false;            
            string updateCFSIDAtRBSC = "false";
            string rbsid = string.Empty;
         
            if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(chr => chr.Name.Equals("OrganisationName", StringComparison.OrdinalIgnoreCase)))
            {
                string organisationName = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(chr => chr.Name.Equals("OrganisationName", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "OrganizationName", Value = organisationName.ToString() });
            }

            if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(chr => chr.Name.Equals("RBSID", StringComparison.OrdinalIgnoreCase)))
            {
                rbsid = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(chr => chr.Name.Equals("RBSID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
            }
            //added on 27/06/23 NAYANAGL-30065
            ClientServiceInstanceV1[] clientServiceInstanceV1 = null;
            clientServiceInstanceV1 = DnpWrapper.getServiceInstanceV1(BillAccountNumber, "VAS_BILLINGACCOUNT_ID", String.Empty);
            if (clientServiceInstanceV1 != null && clientServiceInstanceV1.Count() > 0)
            {
                if (clientServiceInstanceV1.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals("MANAGE_ACCOUNT", StringComparison.OrdinalIgnoreCase) && si.clientServiceInstanceCharacteristic != null && si.clientServiceInstanceCharacteristic.ToList().Exists(cschar => cschar.name.Equals("BACORGANISATIONNAME", StringComparison.OrdinalIgnoreCase) && cschar.value.Equals("EE", StringComparison.OrdinalIgnoreCase))))
                {
                    string organisationName = "EE";
                    roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "OrganizationName", Value = organisationName.ToString() });
                }
            }

            if (orderAction == OrderActionEnum.provide)
            {
                //CF Disabled Service
                roleInstance.RoleType = "SERVICEMANAGE";
                isDisabledProfile = true;
                if (NominumServices != null && NominumServices.Count > 0)
                {
                    if (NominumServices.ToList().Exists(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["CFService"].ToString(), StringComparison.OrdinalIgnoreCase)))
                    {
                        ServiceInstanceExists = true;
                        //Ignoring PC Disable request if PC status is other than Disable.
                        if (!NominumServices.ToList().Where(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["CFService"].ToString(), StringComparison.OrdinalIgnoreCase)).FirstOrDefault().clientServiceInstanceStatus.value.Equals(STATUS_DISABLED, StringComparison.OrdinalIgnoreCase))
                        {
                            throw new DnpException("Ignored as ContentFiltering Service is already present in DNP  for the given BillAccountNumber with Status " + NominumServices.Where(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["CFService"], StringComparison.OrdinalIgnoreCase)).FirstOrDefault().clientServiceInstanceStatus.value);
                        }
                    }
                }

                bacProfileResponse = DnpWrapper.GetClientProfileV1(BillAccountNumber, BACID_IDENTIFER_NAMEPACE);
                isAHTDone = MSEOSaaSMapper.IsAHTDONE(bacProfileResponse, BillAccountNumber);

                roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "ServiceInstanceExists", Value = ServiceInstanceExists.ToString() });
                roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "IsAHTDone", Value = isAHTDone.ToString() });
                roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "IsDisabledProfile", Value = isDisabledProfile.ToString() });

            }
            else if (orderAction == OrderActionEnum.amend)
            {
                if (NominumServices != null && NominumServices.Count > 0)
                {
                    //added aditional condition also if CF service exists as per btr-106505 to checkt he PCChoice exist
                    if (isBUTcalltoUpdatePCChoice && NominumServices.Exists(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["CFService"].ToString(), StringComparison.OrdinalIgnoreCase) && csi.clientServiceInstanceCharacteristic.ToList().Exists(siChar => siChar.name.Equals("INITIAL_PC_ACTIVE_CHOICE", StringComparison.OrdinalIgnoreCase))))
                    {
                        throw new DnpException("Ignored as ContentFiltering Service is already present in DNP with PCChoice for the given BillAccountNumber");
                    }
                    else if (isBUTcalltoUpdatePCChoice && NominumServices.Exists(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["CFService"].ToString(), StringComparison.OrdinalIgnoreCase)))
                    {
                        ServiceInstanceExists = true;                        
                    }
                    else if (NominumServices.Exists(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["CFService"].ToString(), StringComparison.OrdinalIgnoreCase)))
                    {
                        throw new DnpException("Ignored as ContentFiltering Service is already present in DNP  for the given BillAccountNumber");
                    }
                    //Descision to act on complete nominum subscription or not.
                    if (NominumServices.ToList().Exists(csi => csi.clientServiceInstanceStatus.value.Equals(STATUS_ACTIVE, StringComparison.OrdinalIgnoreCase)))
                    {
                        actionOnCompleteNominumSubscription = false;
                    }
                    else
                    {
                        actionOnCompleteNominumSubscription = true;
                    }
                    //Updating RBSC only in 3 cases i.e.,
                    //1.Only MM(Any state) is linked to that Bak.
                    //2.only NS is linked to that Bak and in ceased state.
                    //3.MM and NS but NS is in ceased state.
                    //No need to check  if CF exist as it will be ignored.
                    if ((NominumServices.Count.Equals(1) && (NominumServices.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["MessageManagerService"], StringComparison.OrdinalIgnoreCase)))
                         || (NominumServices.Exists(si => si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["NetworkSecurityService"], StringComparison.OrdinalIgnoreCase) && si.clientServiceInstanceStatus.value.Equals(STATUS_CEASED, StringComparison.OrdinalIgnoreCase))))
                         || (NominumServices.Exists(si => si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["NetworkSecurityService"], StringComparison.OrdinalIgnoreCase) && si.clientServiceInstanceStatus.value.Equals(STATUS_CEASED, StringComparison.OrdinalIgnoreCase))))
                    {
                        updateCFSIDAtRBSC = "true";
                    }
                    else
                    {
                        updateCFSIDAtRBSC = "false";
                    }
                }
                else
                {
                    actionOnCompleteNominumSubscription = true;
                    updateCFSIDAtRBSC = "true";
                }
                if (IsRBSIDExistsinDNP(rbsid))
                {
                    roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "isRBSIDExists", Value = true.ToString() });
                }

                roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "ServiceInstanceExists", Value = ServiceInstanceExists.ToString() });
                roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "ActionOnCompleteNominumSubscription", Value = actionOnCompleteNominumSubscription.ToString() });
                roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "UpdateCFSIDAtRBSC", Value = updateCFSIDAtRBSC.ToString() });                
            }
            else if (orderAction == OrderActionEnum.cease)
            {
                if (NominumServices != null && NominumServices.Count > 0)
                {
                    //Ignoring PC Cease request if PC service present in DNP with not Active Status for  given BAK.
                    if (NominumServices.ToList().Exists(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["CFService"], StringComparison.OrdinalIgnoreCase)))//  .clientServiceInstanceStatus.value.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!NominumServices.ToList().Where(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["CFService"], StringComparison.OrdinalIgnoreCase)).FirstOrDefault().clientServiceInstanceStatus.value.Equals(STATUS_ACTIVE, StringComparison.OrdinalIgnoreCase))
                        {
                            throw new DnpException("Ignored as ContentFiltering Service is already present in DNP  for the given BillAccountNumber with Status " + NominumServices.Where(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["CFService"], StringComparison.OrdinalIgnoreCase)).FirstOrDefault().clientServiceInstanceStatus.value);
                        }
                        else
                        {
                            IEnumerable<string> ActiveServices = NominumServices.ToList().Where(x => x.clientServiceInstanceStatus.value.Equals(STATUS_ACTIVE, StringComparison.OrdinalIgnoreCase)).Select(y => y.clientServiceInstanceIdentifier.value);
                            // Delete subscription at Nominum only if 1 mominum service is exist to that bak.
                            if (ActiveServices.Count().Equals(1) && ActiveServices.ToList().FirstOrDefault().Equals(ConfigurationManager.AppSettings["CFService"], StringComparison.OrdinalIgnoreCase))
                            {
                                actionOnCompleteNominumSubscription = true;
                                updateCFSIDAtRBSC = "true";
                            }
                            // Update CFSID with flag value at RBSC only when MM is active other than NS.
                            else if (ActiveServices.Contains(ConfigurationManager.AppSettings["MessageManagerService"], StringComparer.OrdinalIgnoreCase) &&
                                    !(ActiveServices.Contains(ConfigurationManager.AppSettings["NetworkSecurityService"], StringComparer.OrdinalIgnoreCase)))
                            {
                                updateCFSIDAtRBSC = "UpdateMMFlagValue";
                            }                         
                            roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "UpdateCFSIDAtRBSC", Value = updateCFSIDAtRBSC.ToString() });
                            roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "ActionOnCompleteNominumSubscription", Value = actionOnCompleteNominumSubscription.ToString() });
                        }
                    }
                    else
                    {
                        throw new DnpException("Ignored as ContentFiltering Service is not present in DNP  for the given BillAccountNumber");
                    }
                }
                else
                {
                    throw new DnpException("Ignored as ContentFiltering Service is not present in DNP  for the given BillAccountNumber");
                }
            }
            else if (orderAction == OrderActionEnum.vasReactivate)
            {
                isVASReactivate = true;
                updateCFSIDAtRBSC = "true";
                string oldRbsid = string.Empty;

                if (NominumServices != null && NominumServices.Count > 0)
                {
                    if (NominumServices.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["CFService"].ToString(), StringComparison.OrdinalIgnoreCase)))
                    {
                        ClientServiceInstanceV1 serviceInstance = NominumServices.ToList().Where(si => si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["CFService"].ToString(), StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                        if (serviceInstance != null)
                        {
                            if (serviceInstance.clientServiceInstanceStatus.value.Equals(STATUS_CEASED, StringComparison.OrdinalIgnoreCase) || serviceInstance.clientServiceInstanceStatus.value.Equals(STATUS_DISABLED, StringComparison.OrdinalIgnoreCase))
                            {
                                if (serviceInstance.clientServiceInstanceStatus.value.Equals(STATUS_CEASED, StringComparison.OrdinalIgnoreCase))
                                    isDisabledProfile = false;
                                else
                                    isDisabledProfile = true;

                                if (!orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("rvsid", StringComparison.OrdinalIgnoreCase)))
                                {
                                    if (serviceInstance.clientServiceRole != null)
                                    {
                                        foreach (BT.SaaS.IspssAdapter.Dnp.ClientServiceRole sr in serviceInstance.clientServiceRole.Where(role => role.id.Equals("default", StringComparison.OrdinalIgnoreCase)))
                                        {
                                            OldrbsidList.Add(sr.clientIdentity.ToList().Where(id => id.managedIdentifierDomain.value.Equals("rbsid", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value);
                                            oldRbsid = sr.clientIdentity.ToList().Where(id => id.managedIdentifierDomain.value.Equals("rbsid", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                        }
                                    }
                                }
                                if (NominumServices.ToList().Exists(csi => csi.clientServiceInstanceStatus.value.Equals(STATUS_ACTIVE, StringComparison.OrdinalIgnoreCase)))
                                {
                                    actionOnCompleteNominumSubscription = false;
                                }
                                else
                                {
                                    actionOnCompleteNominumSubscription = true;
                                }

                            }
                            else
                            {
                                throw new DnpException("Ignored as ContentFiltering Service is present in DNP  for the given BillAccountNumber with Status " + serviceInstance.clientServiceInstanceStatus.value);
                            }
                        }
                        if (!oldRbsid.Equals(rbsid,StringComparison.OrdinalIgnoreCase) && IsRBSIDExistsinDNP(rbsid))
                        {
                            roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "isRBSIDExists", Value = true.ToString() });
                        }

                        roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "ActionOnCompleteNominumSubscription", Value = actionOnCompleteNominumSubscription.ToString() });
                        roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "ISVASREACTIVATE", Value = isVASReactivate.ToString() });
                        roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "OLDRBSIDLIST", Value = string.Join(",", OldrbsidList.ToArray()) });
                        roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "IsDisabledProfile", Value = isDisabledProfile.ToString() });
                    }
                    else
                    {
                        throw new DnpException("Ignored as ContentFiltering Service is not present in DNP  for the given BillAccountNumber");
                    }
                }
                else
                {
                    throw new DnpException("Ignored as ContentFiltering Service is not present in DNP  for the given BillAccountNumber");
                }
                roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "UpdateCFSIDAtRBSC", Value = updateCFSIDAtRBSC.ToString() });
            }
        }

        //ccp 78      
        internal void MapSafeSearchService(OrderActionEnum orderAction, List<ClientServiceInstanceV1> NominumServices, OrderItem orderItem, ref RoleInstance roleInstance)
        {
            bool ServiceInstanceExists = false;
            List<string> OldrbsidList = new List<string>();
            bool actionOnCompleteNominumSubscription = false;
            string updateCFSIDAtRBSC = "false";
            string rbsid = string.Empty;
            //NAYANAGL - 30038
            bool isSafeSearchReactivate = false;

            if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(chr => chr.Name.Equals("OrganisationName", StringComparison.OrdinalIgnoreCase)))
            {
                string organisationName = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(chr => chr.Name.Equals("OrganisationName", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "OrganizationName", Value = organisationName.ToString() });
            }
            roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "issafesearch", Value = "true" });

            if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(chr => chr.Name.Equals("RBSID", StringComparison.OrdinalIgnoreCase)))
            {
                rbsid = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(chr => chr.Name.Equals("RBSID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
            }

            if (orderAction == OrderActionEnum.amend)
            {
                if (NominumServices != null && NominumServices.Count > 0)
                {                   
                    if (NominumServices.Exists(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["SFService"].ToString(), StringComparison.OrdinalIgnoreCase)))
                    {
                        //NAYANAGL - 30038
                        //throw new DnpException("Ignored as SafeSearch Service is already present in DNP  for the given BillAccountNumber");
                        isSafeSearchReactivate = true;
                    }   
                   
                    //Descision to act on complete nominum subscription or not.
                    if (NominumServices.ToList().Exists(csi => csi.clientServiceInstanceStatus.value.Equals(STATUS_ACTIVE, StringComparison.OrdinalIgnoreCase)))
                    {
                        actionOnCompleteNominumSubscription = false;
                    }
                    else
                    {
                        actionOnCompleteNominumSubscription = true;
                    }
                    //Updating RBSC only in 3 cases i.e.,
                    //1.Only MM(Any state) is linked to that Bak.
                    //2.only NS is linked to that Bak and in ceased state.
                    //3.MM and NS but NS is in ceased state.
                    if ((NominumServices.Count.Equals(1) && (NominumServices.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["MessageManagerService"], StringComparison.OrdinalIgnoreCase)))
                         || (NominumServices.Exists(si => si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["NetworkSecurityService"], StringComparison.OrdinalIgnoreCase) && si.clientServiceInstanceStatus.value.Equals(STATUS_CEASED, StringComparison.OrdinalIgnoreCase))))
                         || (NominumServices.Exists(si => si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["CFService"], StringComparison.OrdinalIgnoreCase) && si.clientServiceInstanceStatus.value.Equals(STATUS_CEASED, StringComparison.OrdinalIgnoreCase))))
                    {
                        updateCFSIDAtRBSC = "true";
                    }
                    else
                    {
                        updateCFSIDAtRBSC = "false";
                    }
                }
                else
                {
                    actionOnCompleteNominumSubscription = true;
                    updateCFSIDAtRBSC = "true";
                }
                if (IsRBSIDExistsinDNP(rbsid))
                {
                    roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "isRBSIDExists", Value = true.ToString() });
                }
                //Content filtering activation check
                if (NominumServices.Exists(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["CFService"].ToString(), StringComparison.OrdinalIgnoreCase) && csi.clientServiceInstanceStatus.value.Equals(STATUS_ACTIVE, StringComparison.OrdinalIgnoreCase)))
                {
                    roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "isCFActive", Value = true.ToString() });
                }
                //NAYANAGL - 30038
                roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "ISVASREACTIVATE", Value = isSafeSearchReactivate.ToString() });
                roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "ServiceInstanceExists", Value = ServiceInstanceExists.ToString() });
                roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "ActionOnCompleteNominumSubscription", Value = actionOnCompleteNominumSubscription.ToString() });
                roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "UpdateCFSIDAtRBSC", Value = updateCFSIDAtRBSC.ToString() });
            }
            else if (orderAction == OrderActionEnum.cease)
            {
                if (NominumServices != null && NominumServices.Count > 0)
                {
                    if (NominumServices.ToList().Exists(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["SFService"], StringComparison.OrdinalIgnoreCase)))
                    {
                        if (!NominumServices.ToList().Where(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["SFService"], StringComparison.OrdinalIgnoreCase)).FirstOrDefault().clientServiceInstanceStatus.value.Equals(STATUS_ACTIVE, StringComparison.OrdinalIgnoreCase))
                        {
                            throw new DnpException("Ignored as SafeSearch Service is already present in DNP  for the given BillAccountNumber with Status " + NominumServices.Where(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["CFService"], StringComparison.OrdinalIgnoreCase)).FirstOrDefault().clientServiceInstanceStatus.value);
                        }
                        else
                        {
                            IEnumerable<string> ActiveServices = NominumServices.ToList().Where(x => x.clientServiceInstanceStatus.value.Equals(STATUS_ACTIVE, StringComparison.OrdinalIgnoreCase)).Select(y => y.clientServiceInstanceIdentifier.value);
                            
                            if (ActiveServices.Count().Equals(1) && ActiveServices.ToList().FirstOrDefault().Equals(ConfigurationManager.AppSettings["SFService"], StringComparison.OrdinalIgnoreCase))
                            {
                                actionOnCompleteNominumSubscription = true;
                                updateCFSIDAtRBSC = "true";
                            }
                            // Update CFSID with flag value at RBSC only when MM is active other than NS & CFS.
                            else if (ActiveServices.Contains(ConfigurationManager.AppSettings["MessageManagerService"], StringComparer.OrdinalIgnoreCase) &&
                                    !(ActiveServices.Contains(ConfigurationManager.AppSettings["NetworkSecurityService"], StringComparer.OrdinalIgnoreCase)) &&
                                    !(ActiveServices.Contains(ConfigurationManager.AppSettings["CFService"], StringComparer.OrdinalIgnoreCase)))
                            {
                                updateCFSIDAtRBSC = "UpdateMMFlagValue";
                            }
                            roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "issafesearchcease", Value = "true" });
                            roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "UpdateCFSIDAtRBSC", Value = updateCFSIDAtRBSC.ToString() });
                            roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "ActionOnCompleteNominumSubscription", Value = actionOnCompleteNominumSubscription.ToString() });
                        }
                    }
                    else
                    {
                        throw new DnpException("Ignored as SafeSearch Service is not present in DNP  for the given BillAccountNumber");
                    }
                }
                else
                {
                    throw new DnpException("Ignored as SafeSearch Service is not present in DNP  for the given BillAccountNumber");
                }
            }
        }

        internal void MapNetworkSecurityService(OrderActionEnum orderAction, List<ClientServiceInstanceV1> NominumServices, ref RoleInstance roleInstance, ref string action, string OrderFrom)
        {
            bool isDisabledProfile = false;
            bool isDisabledState = false;
            bool ServiceInstanceExists = false;
            bool isAHTDone = false;
            string rbsid = string.Empty;
            string promotionId = string.Empty;
            GetClientProfileV1Res bacProfileResponse = null;
            ClientServiceInstanceV1 clientServiceInstance = null;
            bool actionOnCompleteNominumSubscription = false;
            bool isVASReactivate = false;
            string updateCFSIDAtRBSC = "false";

            //NAYANAGL - 36213
            ClientServiceInstanceV1[] clientServiceInstanceV1 = null;
            clientServiceInstanceV1 = DnpWrapper.getServiceInstanceV1(BillAccountNumber, "VAS_BILLINGACCOUNT_ID", String.Empty);
            if (clientServiceInstanceV1 != null && clientServiceInstanceV1.Count() > 0)
            {
                if (clientServiceInstanceV1.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals("MANAGE_ACCOUNT", StringComparison.OrdinalIgnoreCase) && si.clientServiceInstanceCharacteristic != null && si.clientServiceInstanceCharacteristic.ToList().Exists(cschar => cschar.name.Equals("BACORGANISATIONNAME", StringComparison.OrdinalIgnoreCase) && cschar.value.Equals("EE", StringComparison.OrdinalIgnoreCase))))
                {
                    string organisationName = "EE";
                    roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "organizationname", Value = organisationName.ToString() });
                }
            }

            // added if condition for checking the disabled scenario--- BTR-85891-----
            if (orderAction == OrderActionEnum.provide)
            {
                //Networksecurity Disabled Service    
                roleInstance.RoleType = "SERVICEMANAGE";
                isDisabledProfile = true;
                if (NominumServices != null && NominumServices.Count > 0)
                {
                    if (NominumServices.ToList().Exists(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["NetworkSecurityService"].ToString(), StringComparison.OrdinalIgnoreCase)))
                    {
                        ServiceInstanceExists = true;
                        //Ignoring the request if BAC has NetworkSecurity service.
                        if (NominumServices.ToList().Exists(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["NetworkSecurityService"].ToString(), StringComparison.OrdinalIgnoreCase)))
                        {
                            throw new DnpException("Ignored as NetworkSecurity Service is already present in DNP  for the given BillAccountNumber with Status " + NominumServices.Where(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["NetworkSecurityService"], StringComparison.OrdinalIgnoreCase)).FirstOrDefault().clientServiceInstanceStatus.value);
                        }
                    }
                }

                bacProfileResponse = DnpWrapper.GetClientProfileV1(BillAccountNumber, BACID_IDENTIFER_NAMEPACE);
                isAHTDone = MSEOSaaSMapper.IsAHTDONE(bacProfileResponse, BillAccountNumber);

                roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "ServiceInstanceExists", Value = ServiceInstanceExists.ToString() });
                roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "IsAHTDone", Value = isAHTDone.ToString() });
                roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "isDisabledProfile", Value = isDisabledProfile.ToString() });

            }
            else if (orderAction == OrderActionEnum.amend)
            {
                if (NominumServices != null && NominumServices.Count > 0)
                {                  
                    if ((roleInstance != null) && (roleInstance.Attributes.ToList().Exists(attr => attr.Name.Equals("RBSID", StringComparison.OrdinalIgnoreCase)))) 
                    {
                        rbsid = roleInstance.Attributes.ToList().Where(attr => attr.Name.Equals("RBSID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;                       
                    }
                    if ((roleInstance != null) && (roleInstance.Attributes.ToList().Exists(attr => attr.Name.Equals("PromotionIntegrationId", StringComparison.OrdinalIgnoreCase))))
                    {
                        promotionId = roleInstance.Attributes.ToList().Where(attr => attr.Name.Equals("PromotionIntegrationId", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                    }

                    if (NominumServices.ToList().Exists(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["NetworkSecurityService"].ToString(), StringComparison.OrdinalIgnoreCase)))
                    {
                        if (NominumServices.ToList().Where(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["NetworkSecurityService"].ToString(), StringComparison.OrdinalIgnoreCase)).FirstOrDefault().clientServiceInstanceStatus.value.Equals(STATUS_DISABLED, StringComparison.OrdinalIgnoreCase))
                        {
                            clientServiceInstance = NominumServices.ToList().Where(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["NetworkSecurityService"].ToString(), StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                            if (clientServiceInstance != null && clientServiceInstance.clientServiceInstanceCharacteristic != null && clientServiceInstance.clientServiceInstanceCharacteristic.Count() > 0)
                            {
                                if ((!string.IsNullOrEmpty(rbsid)) && (!string.IsNullOrEmpty(promotionId)))
                                {
                                    if (NominumServices.ToList().Where(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["NetworkSecurityService"].ToString(), StringComparison.OrdinalIgnoreCase)).FirstOrDefault().clientServiceInstanceCharacteristic.ToList().Where(chart => chart.name.Equals("RBSID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value.Equals(rbsid, StringComparison.OrdinalIgnoreCase)
                                        && NominumServices.ToList().Where(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["NetworkSecurityService"].ToString(), StringComparison.OrdinalIgnoreCase)).FirstOrDefault().clientServiceInstanceCharacteristic.ToList().Where(chart => chart.name.Equals("PROMOTION_ID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value.Equals(promotionId, StringComparison.OrdinalIgnoreCase))
                                    {
                                        isDisabledState = true;
                                        roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "isDisabledState", Value = isDisabledState.ToString() });
                                    }
                                    else
                                    {
                                        throw new DnpException("Ignored as Network Security amend request as it is already present in DNP with the Bac " + BillAccountNumber + " with different RBSID or PromotionID values");
                                    }
                                }
                            }
                        }
                        else
                        {
                            throw new DnpException("Ignored as Network Security amend request as it is already present in DNP with the Bac " + BillAccountNumber + "with state of " + NominumServices.ToList().Where(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["NetworkSecurityService"].ToString(), StringComparison.OrdinalIgnoreCase)).FirstOrDefault().clientServiceInstanceStatus.value);
                        }
                    }
					
                    //Descision to act on complete nominum subscription or not.
                    if (NominumServices.ToList().Exists(csi => csi.clientServiceInstanceStatus.value.Equals(STATUS_ACTIVE, StringComparison.OrdinalIgnoreCase)))
                    {
                        actionOnCompleteNominumSubscription = false;
                    }
                    else
                    {
                        actionOnCompleteNominumSubscription = true;
                    }
                    //Updating RBSC only in 3 cases i.e.,
                    //1.Only MM(Any state) is linked to that Bak.
                    //2.only CF is linked to that Bak and in ceased state.
                    //3.MM and CF but CF is in ceased state.
                    //No need to check  if NS exist as it will be ignored.
                    if ((NominumServices.Count.Equals(1) && (NominumServices.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["MessageManagerService"], StringComparison.OrdinalIgnoreCase)))
                         || (NominumServices.Exists(si => si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["CFService"], StringComparison.OrdinalIgnoreCase) && si.clientServiceInstanceStatus.value.Equals(STATUS_CEASED, StringComparison.OrdinalIgnoreCase))))
                         || (NominumServices.Exists(si => si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["CFService"], StringComparison.OrdinalIgnoreCase) && si.clientServiceInstanceStatus.value.Equals(STATUS_CEASED, StringComparison.OrdinalIgnoreCase)))
                         || (NominumServices.Exists(si => si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["CFService"], StringComparison.OrdinalIgnoreCase) && si.clientServiceInstanceStatus.value.Equals(STATUS_DISABLED, StringComparison.OrdinalIgnoreCase))))
                    {
                        updateCFSIDAtRBSC = "true";
                    }
                    else
                    {
                        updateCFSIDAtRBSC = "false";
                    }
                }
                else
                {
                    //=>no Nominumservice linked to that bill account number.
                    actionOnCompleteNominumSubscription = true;
                    updateCFSIDAtRBSC = "true";
                }
                if (!string.IsNullOrEmpty(OrderFrom))
                    roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "OrderFrom", Value = OrderFrom.ToString() });
                roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "ActionOnCompleteNominumSubscription", Value = actionOnCompleteNominumSubscription.ToString() });
                roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "UpdateCFSIDAtRBSC", Value = updateCFSIDAtRBSC.ToString() });
            }
            else if (orderAction == OrderActionEnum.cease)
            {
                if (NominumServices != null && NominumServices.Count > 0)
                {
                    //Ignoring NS Cease request if NS service present in DNP with not Active Status for  given BAK.
                    if (NominumServices.ToList().Exists(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["NetworkSecurityService"], StringComparison.OrdinalIgnoreCase)))//  .clientServiceInstanceStatus.value.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!NominumServices.ToList().Where(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["NetworkSecurityService"], StringComparison.OrdinalIgnoreCase)).FirstOrDefault().clientServiceInstanceStatus.value.Equals(STATUS_ACTIVE, StringComparison.OrdinalIgnoreCase))
                        {
                            throw new DnpException("Ignored as NetworkSecurity Service is already present in DNP  for the given BillAccountNumber with Status " + NominumServices.Where(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["NetworkSecurityService"], StringComparison.OrdinalIgnoreCase)).FirstOrDefault().clientServiceInstanceStatus.value);
                        }
                        else
                        {
                            roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "OrderFrom", Value = OrderFrom });
                            //var activeSericesCount = NominumServices.ToList().Where(x => x.clientServiceInstanceStatus.value.Equals("Active", StringComparison.OrdinalIgnoreCase)).Count();
                            IEnumerable<string> ActiveServices = NominumServices.ToList().Where(x => x.clientServiceInstanceStatus.value.Equals(STATUS_ACTIVE, StringComparison.OrdinalIgnoreCase)).Select(y => y.clientServiceInstanceIdentifier.value);
                            // Delete subscription at Nominum and 
                            if (ActiveServices.Count().Equals(1) && ActiveServices.ToList().FirstOrDefault().Equals(ConfigurationManager.AppSettings["NetworkSecurityService"], StringComparison.OrdinalIgnoreCase))
                            {
                                actionOnCompleteNominumSubscription = true;
                                updateCFSIDAtRBSC = "true";
                            }
                            // Update CFSID with flag value at RBSC only when MM is active other than NS.
                            else if (ActiveServices.Contains(ConfigurationManager.AppSettings["MessageManagerService"], StringComparer.OrdinalIgnoreCase) &&
                                    !(ActiveServices.Contains(ConfigurationManager.AppSettings["CFService"], StringComparer.OrdinalIgnoreCase)))
                            {
                                updateCFSIDAtRBSC = "UpdateMMFlagValue";
                            }
                            roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "UpdateCFSIDAtRBSC", Value = updateCFSIDAtRBSC.ToString() });
                            roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "ActionOnCompleteNominumSubscription", Value = actionOnCompleteNominumSubscription.ToString() });
                        }
                    }
                    else
                        throw new DnpException("Ignored as NetworkSecurity Service is not present in DNP  for the given BillAccountNumber:" + BillAccountNumber);
                }
                else
                    throw new DnpException("Ignored cease request as NetworkSecurity Service is not present in DNP  for the given BAC: " + BillAccountNumber);

            }
            else if (orderAction == OrderActionEnum.vasReactivate)
            {
                //If vas reactivate update RBSID.
                updateCFSIDAtRBSC = "true";
                if (NominumServices != null && NominumServices.Count > 0)
                {
                    if (NominumServices.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["NetworkSecurityService"].ToString(), StringComparison.OrdinalIgnoreCase)))
                    {
                        ClientServiceInstanceV1 serviceInstance = NominumServices.ToList().Where(si => si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["NetworkSecurityService"].ToString(), StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                        if (serviceInstance != null)
                        {
                            if (serviceInstance.clientServiceInstanceStatus.value.Equals(STATUS_CEASED, StringComparison.OrdinalIgnoreCase) || serviceInstance.clientServiceInstanceStatus.value.Equals(STATUS_DISABLED, StringComparison.OrdinalIgnoreCase))
                            {
                                isVASReactivate = true;
                                roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "ISVASREACTIVATE", Value = isVASReactivate.ToString() });
                                if (NominumServices.ToList().Exists(csi => csi.clientServiceInstanceStatus.value.Equals(STATUS_ACTIVE, StringComparison.OrdinalIgnoreCase)))
                                {
                                    actionOnCompleteNominumSubscription = false;
                                }
                                else
                                {
                                    actionOnCompleteNominumSubscription = true;
                                }
                                roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "ActionOnCompleteNominumSubscription", Value = actionOnCompleteNominumSubscription.ToString() });
                            }
                            else
                            {
                                throw new DnpException("Ignored as Network security Service is present in DNP  for the given BillAccountNumber with Status " + serviceInstance.clientServiceInstanceStatus.value);
                            }
                        }
                    }
                    else
                    {
                        throw new DnpException("Ignored as NetworkSecurity Service is not present in DNP  for the given BillAccountNumber");
                    }

                }
                else
                {
                    throw new DnpException("Ignored as NetworkSecurity Service is not present in DNP  for the given BillAccountNumber");
                }
                roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "UpdateCFSIDAtRBSC", Value = updateCFSIDAtRBSC.ToString() });
            }
        }

        public static string GetCFSIDFromNominumService(List<ClientServiceInstanceV1> NominumServices, string BillAccountNumber)
        {
            ClientServiceInstanceV1 NominumService = null;
            string cfsid = string.Empty;

            //get the CFSID from Nominum Service.
            if (NominumServices.ToList().Exists(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["CFService"], StringComparison.OrdinalIgnoreCase)))
            {
                NominumService = NominumServices.ToList().Where(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["CFService"], StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                cfsid = NominumService.clientServiceInstanceCharacteristic.ToList().Where(csic => csic.name.Equals("CFSID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
            }
            //ccp 78
            else if (NominumServices.ToList().Exists(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["SFService"], StringComparison.OrdinalIgnoreCase)))
            {
                NominumService = NominumServices.ToList().Where(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["SFService"], StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                cfsid = NominumService.clientServiceInstanceCharacteristic.ToList().Where(csic => csic.name.Equals("CFSID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
            }
            else
            {
                NominumService = NominumServices.ToList().Where(csi => (csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["MessageManagerService"], StringComparison.OrdinalIgnoreCase)
                                                                               || csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["NetworkSecurityService"], StringComparison.OrdinalIgnoreCase))).FirstOrDefault();
                cfsid = NominumService.serviceIdentity.ToList().Where(si => si.domain.Equals("cfsid", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
            }
            return cfsid;
        }
         
        #region Methods for Home Move
        public SaaSNS.Order MapHomeMoveRequest(MSEO.OrderRequest request, ref E2ETransaction e2eData)
        {
            BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("Mapping VAS Hoem Move the request");
            SaaSNS.Order response = new BT.SaaS.Core.Shared.Entities.Order();
            List<ProductOrderItem> productOrderItemsList = new List<ProductOrderItem>();
            System.DateTime orderDate = new System.DateTime();
            try
            {
                int productorderItemCount = 0;
                response.Header.CeaseReason = request.SerializeObject();

                response.Header.OrderKey = request.Order.OrderIdentifier.Value;
                response.Header.ServiceProviderID = Settings1.Default.ConsumerServiceProviderId;

                if (request.Order.OrderItem[0].RequiredDateTime != null && request.Order.OrderItem[0].RequiredDateTime.DateTime1 != null)
                {
                    if (MSEOSaaSMapper.convertDateTime(request.Order.OrderItem[0].RequiredDateTime.DateTime1, ref orderDate))
                    {
                        response.Header.EffectiveDateTime = orderDate;
                    }
                    else
                    {
                        response.Header.EffectiveDateTime = System.DateTime.Now;
                    }
                }
                else
                {
                    response.Header.EffectiveDateTime = System.DateTime.Now;
                }
                response.Header.OrderDateTime = System.DateTime.Now;

                response.Header.Status = BT.SaaS.Core.Shared.Entities.OrderStatusEnum.@new;

                response.Header.Customer = new BT.SaaS.Core.Shared.Entities.Customer();
                response.Header.Customer.Status = BT.SaaS.Core.Shared.Entities.DataStatusEnum.done;

                response.Header.Users = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.User>(1);
                SaaSNS.User user = new BT.SaaS.Core.Shared.Entities.User();
                user.Status = BT.SaaS.Core.Shared.Entities.DataStatusEnum.done;
                response.Header.Users.Add(user);

                if (request.Order.OrderItem[0].Action.Code.ToLower().Equals("modify", StringComparison.OrdinalIgnoreCase))
                {
                    response.Header.Action = BT.SaaS.Core.Shared.Entities.OrderActionEnum.modifyService;
                }

                response.ProductOrderItems = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.ProductOrderItem>(request.Order.OrderItem.Length);

                if (request.Order.OrderItem.ToList().Exists(oi => oi.Action.Code.Equals("Modify", StringComparison.OrdinalIgnoreCase) && oi.Instance.ToList().Exists(inst => inst.InstanceCharacteristic.ToList().Exists(instchar => instchar.Name.Equals("BTWSID", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(instchar.Value))))
                        && request.Order.OrderItem.ToList().Exists(oi => oi.Action.Code.Equals("Modify", StringComparison.OrdinalIgnoreCase) && oi.Instance.ToList().Exists(inst => inst.InstanceCharacteristic.ToList().Exists(instchar => instchar.Name.Equals("ServiceCode", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(instchar.Value) && instchar.Value.Equals("BT_CONSUMER_BROADBAND", StringComparison.OrdinalIgnoreCase)))))
                {
                    BTCONSUMERBROADBANDProcessor choProcessor = new BTCONSUMERBROADBANDProcessor();
                    choProcessor.ModifyBBSIDMapper(request, ref e2eData);
                }
                if (request.Order.OrderItem.ToList().Exists(oi => oi.Action.Code.Equals("Modify", StringComparison.OrdinalIgnoreCase) && oi.Instance.ToList().Exists(inst => inst.InstanceCharacteristic.ToList().Exists(instchar => instchar.Name.Equals("HCStatus", StringComparison.OrdinalIgnoreCase) && instchar.Value.Equals("Active", StringComparison.OrdinalIgnoreCase))))
                    && request.Order.OrderItem.ToList().Exists(oi => oi.Action.Code.Equals("Modify", StringComparison.OrdinalIgnoreCase) && oi.Instance.ToList().Exists(inst => inst.InstanceCharacteristic.ToList().Exists(instchar => instchar.Name.Equals("ServiceCode", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(instchar.Value) && instchar.Value.Equals("BT_CONSUMER_BROADBAND", StringComparison.OrdinalIgnoreCase))))
                    && request.Order.OrderItem.ToList().Exists(oi => oi.Action.Code.Equals("Modify", StringComparison.OrdinalIgnoreCase) && oi.Instance.ToList().Exists(inst => inst.InstanceCharacteristic.ToList().Exists(instchar => instchar.Name.Equals("RBSID", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(instchar.Value)))))
                {
                    BTCONSUMERBROADBANDProcessor choProcessor = new BTCONSUMERBROADBANDProcessor();
                    choProcessor.ModifyRBSIDMapperforCHOP(request, ref e2eData);
                }

                foreach (MSEO.OrderItem orderItem in request.Order.OrderItem)
                {
                    //string billAccountID = string.Empty;
                    string vasClassID = string.Empty;
                    string cfsid = string.Empty;
                    List<string> vasClassList = new List<string>();
                    ClientServiceInstanceV1[] serviceInstances = null;
                    List<string> CMPSCodes = null;

                    BillAccountNumber = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(a => a.Name.Equals("billaccountnumber", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                    vasClassID = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(insChar => insChar.Name.Equals("vasclass", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                    vasClassList.Add(vasClassID);

                    List<ProductVasClass> productVASClassList = MSEOSaaSMapper.FindAllActiveInactiveProductVasClass(vasClassList, BillAccountNumber, "VAS_BILLINGACCOUNT_ID", string.Empty, ref serviceInstances, ref CMPSCodes, ref e2eData);

                    prepareProductOrderItemsForHomeMove(request, orderItem, productVASClassList, ref productOrderItemsList, ref productorderItemCount);
                    response.ProductOrderItems.AddRange(productOrderItemsList);
                }
            }
            catch (MdmException Mdmexception)
            {
                throw Mdmexception;
            }
            catch (Exception ex)
            {
                BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter Mapping VAS Home Move Exception : " + ex.ToString());
                throw ex;
            }

            return response;
        }

        public void prepareProductOrderItemsForHomeMove(MSEO.OrderRequest request, OrderItem orderItem, List<ProductVasClass> productVASClassList, ref List<ProductOrderItem> productOrderItems, ref int productorderItemCount)
        {
            ClientServiceInstanceV1[] serviceInstances = null;
            List<ClientServiceInstanceV1> NominumServices = new List<ClientServiceInstanceV1>();
            string dummy = string.Empty;
            serviceInstances = DnpWrapper.getServiceInstanceV1(BillAccountNumber, "VAS_BILLINGACCOUNT_ID", null);

            if (serviceInstances != null && serviceInstances.Length > 0)
            {
                NominumServices = (from csi in serviceInstances.ToList()
                                   where csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["CFService"], StringComparison.OrdinalIgnoreCase)
                                    || csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["NetworkSecurityService"], StringComparison.OrdinalIgnoreCase)
                                   select csi).ToList();
            }
            else
            {
                throw new DnpException("Ignored Home Move request as no Nominum service is associated to BillAccount ID :" + BillAccountNumber + " in ProfileStore");
            }

            if (NominumServices != null && NominumServices.Count() > 0)
            {
                if (!NominumServices.Exists(csi => csi.clientServiceInstanceStatus.value.Equals("Active", StringComparison.OrdinalIgnoreCase)))
                {
                    //var NominumService = NominumServices.Where(csi => !csi.clientServiceInstanceStatus.value.Equals("Active", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                    throw new DnpException("Ignored Home Move request as no Nominum service is in Acive state for the BAC: " + BillAccountNumber);
                }

                if (productVASClassList != null && productVASClassList.Count > 0)
                {
                    foreach (ClientServiceInstanceV1 userService in NominumServices)
                    {
                        if (userService.clientServiceInstanceStatus.value.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase))
                        {
                            foreach (ProductVasClass productVasClass in productVASClassList)
                            {
                                if (ConfigurationManager.AppSettings["NominumVasServices"].ToString().Split(',').Contains(productVasClass.VasProductFamily, StringComparer.OrdinalIgnoreCase)
                                   && userService.clientServiceInstanceIdentifier.value.Equals(productVasClass.VasProductFamily + ":" + productVasClass.vasSubType, StringComparison.OrdinalIgnoreCase)
                                   && !(productOrderItems.ToList().Exists(poi => poi.Header.ProductCode.ToString().Equals(productVasClass.VasProductFamily))))
                                {
                                    SaaSNS.ProductOrderItem productOrderItem = new BT.SaaS.Core.Shared.Entities.ProductOrderItem();
                                    productOrderItem = new BT.SaaS.Core.Shared.Entities.ProductOrderItem();
                                    productOrderItem.Header.Status = BT.SaaS.Core.Shared.Entities.DataStatusEnum.notDone;
                                    productOrderItem.Header.Quantity = "1";
                                    productOrderItem.XmlIdKey = "PO-" + productorderItemCount.ToString();

                                    productOrderItem.Header.OrderItemKey = orderItem.Identifier.Id;
                                    productOrderItem.Header.ProductCode = productVasClass.VasProductFamily;

                                    System.Collections.Generic.List<string> inputProductDefinitionList = new System.Collections.Generic.List<string>();
                                    inputProductDefinitionList.Add(productOrderItem.Header.ProductCode);

                                    System.Collections.Generic.List<ProductDefinition> productDefinition = MdmWrapper.getSaaSProductDefs(inputProductDefinitionList, Settings1.Default.ConsumerServiceProviderId);

                                    if (productDefinition.Count == 0)
                                    {
                                        orderItem.Status = Settings1.Default.IgnoredStatus;
                                        Logger.Debug("Product Code not found in Order : " + request.Order.OrderIdentifier.Value + " for order item " + orderItem.Identifier.Id);
                                    }
                                    else
                                    {
                                        productOrderItem.RoleInstances = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.RoleInstance>(1);
                                        productOrderItem.ServiceInstances = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.ServiceInstance>();
                                        SaaSNS.RoleInstance roleInstance = new BT.SaaS.Core.Shared.Entities.RoleInstance();

                                        roleInstance = new BT.SaaS.Core.Shared.Entities.RoleInstance();
                                        roleInstance.Action = BT.SaaS.Core.Shared.Entities.RoleInstanceActionEnum.add;
                                        roleInstance.RoleType = "ADMIN";
                                        roleInstance.XmlIdKey = "RI-" + productorderItemCount.ToString();


                                        SaaSNS.ServiceInstance serviceInstance = new BT.SaaS.Core.Shared.Entities.ServiceInstance();
                                        serviceInstance.ServiceRoles = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.ServiceRole>();
                                        SaaSNS.ServiceRole serviceRole = new BT.SaaS.Core.Shared.Entities.ServiceRole();
                                        serviceRole.roleAttributes = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.Attribute>();

                                        foreach (MSEO.Instance instance in orderItem.Instance)
                                        {
                                            foreach (MSEO.InstanceCharacteristic instanceCharacteristic in instance.InstanceCharacteristic)
                                            {
                                                if (instanceCharacteristic.Name.Equals("rbsid", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    SaaSNS.Attribute newRBSIDAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                                    newRBSIDAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                                    newRBSIDAttribute.Name = "NEWRBSID";
                                                    newRBSIDAttribute.Value = instanceCharacteristic.Value;
                                                    roleInstance.Attributes.Add(newRBSIDAttribute);

                                                    if (IsRBSIDExistsinDNP(instanceCharacteristic.Value))
                                                    {
                                                        roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "isRBSIDExists", Value = true.ToString() });
                                                    }

                                                    SaaSNS.Attribute oldRBSIDAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                                    oldRBSIDAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                                    oldRBSIDAttribute.Name = "OLDRBSID";
                                                    oldRBSIDAttribute.Value = instanceCharacteristic.PreviousValue;
                                                    roleInstance.Attributes.Add(oldRBSIDAttribute);
                                                }
                                                else
                                                {
                                                    SaaSNS.Attribute attribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                                    attribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                                    attribute.Name = instanceCharacteristic.Name;
                                                    attribute.Value = instanceCharacteristic.Value;
                                                    roleInstance.Attributes.Add(attribute);
                                                }
                                            }
                                        }

                                        if (!roleInstance.Attributes.Exists(ri => ri.Name.Equals("VasSubType", StringComparison.OrdinalIgnoreCase)))
                                        {
                                            roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { Name = "VasSubType", Value = "Default", action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });
                                        }

                                        SaaSNS.Attribute promotionChangeAttr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                        promotionChangeAttr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                        promotionChangeAttr.Name = "ISPROMOTIONCHANGE";
                                        promotionChangeAttr.Value = "false";
                                        roleInstance.Attributes.Add(promotionChangeAttr);

                                        SaaSNS.Attribute updaterbsidAttr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                        updaterbsidAttr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                        updaterbsidAttr.Name = "UpdateCFSIDAtRBSC";
                                        updaterbsidAttr.Value = "True";
                                        roleInstance.Attributes.Add(updaterbsidAttr);

                                        string cfsid = GetCFSIDFromNominumService(NominumServices, BillAccountNumber);

                                        SaaSNS.Attribute cfsidAttr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                        cfsidAttr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                        cfsidAttr.Name = "CFSID";
                                        cfsidAttr.Value = cfsid.ToString();
                                        roleInstance.Attributes.Add(cfsidAttr);

                                        serviceInstance.ServiceRoles.Add(serviceRole);
                                        productOrderItem.ServiceInstances.Add(serviceInstance);
                                        productOrderItem.RoleInstances.Add(roleInstance);
                                        productOrderItem.RoleInstances[0].Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "E2EDATA", Value = request.StandardHeader.E2e.E2EDATA.ToString(), action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });

                                        if (!(productOrderItems.ToList().Exists(poi => poi.Header.ProductCode.ToString().Equals(productOrderItem.Header.ProductCode))))
                                        {
                                            productorderItemCount++;
                                            productOrderItems.Add(productOrderItem);
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                throw new DnpException("Ignored Home Move request as no Nominum service is associated to BillAccount ID :" + BillAccountNumber + " in ProfileStore");
            }
        }
        public static bool IsRBSIDExistsinDNP(string rbsid)
        {
            bool result = false;

            if (!string.IsNullOrEmpty(rbsid))
            {
                GetClientProfileV1Res getprofileResponse1 = new GetClientProfileV1Res();

                getprofileResponse1 = DnpWrapper.GetClientProfileV1(rbsid, "RBSID");
                if (getprofileResponse1 != null && getprofileResponse1.clientProfileV1 != null && getprofileResponse1.clientProfileV1.client != null && getprofileResponse1.clientProfileV1.client.clientIdentity != null
                    && getprofileResponse1.clientProfileV1.client.clientIdentity.Count() > 0 && getprofileResponse1.clientProfileV1.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("RBSID", StringComparison.OrdinalIgnoreCase) && ci.value.Equals(rbsid, StringComparison.OrdinalIgnoreCase)))
                {
                    result = true;
                }
            }
            return result;
        }
        #endregion
    }
}
