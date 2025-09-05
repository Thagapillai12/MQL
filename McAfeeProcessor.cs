using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;
using MSEO = BT.SaaS.MSEOAdapter;
using SaaSNS = BT.SaaS.Core.Shared.Entities;
using BT.SaaS.IspssAdapter;
using BT.SaaS.Core.Shared.Entities;
using BT.SaaS.IspssAdapter.Dnp;

namespace BT.SaaS.MSEOAdapter
{
    public class McAfeeProcessor
    {
        #region Members

        private const string SECURITYBAC_IDENTIFER_NAMEPACE = "SECURITY_BAC";
        private const string STATUS_PENDING = "PENDING";
        private const string STATUS_PENDINGCEASE = "PENDING-CEASE";
        private const string STATUS_CEASED = "CEASED";
        private const string STATUS_ACTIVE = "ACTIVE";
        private const string STATUS_DISABLED = "DISABLED";

        string BillAccountNumber = string.Empty;

        #endregion

        internal void MapSecurityProduct(OrderItem orderItem, OrderActionEnum orderAction, ref RoleInstance roleInstance, string VasSubType)
        {   
                string SecurityServiceStatus = string.Empty, reason = string.Empty;
                bool isDisabled = false;

                if (orderItem.Action.Reason != null && !string.IsNullOrEmpty(orderItem.Action.Reason))
                    reason = orderItem.Action.Reason;
                    //if (orderItem.Action.Reason.Equals("disabled", StringComparison.OrdinalIgnoreCase))
                    //    isDisabled = true;           

                List<ClientServiceInstanceV1> SecurityServices = new List<ClientServiceInstanceV1>();

                if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToLower().Equals("billaccountnumber")) && !string.IsNullOrEmpty(orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("billaccountnumber")).FirstOrDefault().Value))
                {
                    BillAccountNumber = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("billaccountnumber")).FirstOrDefault().Value;
                }

                ClientServiceInstanceV1[] gsiResponse = gsiResponse = DnpWrapper.getServiceInstanceV1(BillAccountNumber, SECURITYBAC_IDENTIFER_NAMEPACE, null);
                if (gsiResponse != null && gsiResponse.Any())
                {
                    SecurityServices = (from csi in gsiResponse.ToList()
                                        where csi.clientServiceInstanceIdentifier.value.Split(':').First().Equals(ConfigurationManager.AppSettings["NPPService"], StringComparison.OrdinalIgnoreCase)
                                         || csi.clientServiceInstanceIdentifier.value.Split(':').First().Equals(ConfigurationManager.AppSettings["BasicSecurityService"], StringComparison.OrdinalIgnoreCase)
                                        select csi).ToList();
                }
                
                if(!string.IsNullOrEmpty(reason))
                    roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "REASON", Value = reason });

                if (VasSubType.Equals("Common", StringComparison.OrdinalIgnoreCase))
                {
                    if (orderAction == OrderActionEnum.amend && !string.IsNullOrEmpty(reason) && string.Equals(reason, "MMA15INTEREST", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!roleInstance.Attributes.ToList().Exists(ri => ri.Name.Equals("reason", StringComparison.OrdinalIgnoreCase)))
                            roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "Reason", Value = reason });                       
                    }
                    else
                        MapMMAService(orderAction, SecurityServices, ref roleInstance, reason);
                }
                if (VasSubType.Equals("Windows", StringComparison.OrdinalIgnoreCase))
                {
                    MapNPPWindows(orderAction, SecurityServices);
                }
        }

        internal void MapMMAService(OrderActionEnum orderAction, List<ClientServiceInstanceV1> SecurityServices,ref RoleInstance roleInstance,string reason)
        {
            bool isDisabledProfile = false;
            bool ServiceInstanceExists = false, IsExistingAccount = false;
            string CCID = string.Empty;

            if ((orderAction == OrderActionEnum.provide || orderAction == OrderActionEnum.amend) && !string.IsNullOrEmpty(reason) && string.Equals(reason,"disabled",StringComparison.OrdinalIgnoreCase))
            {
                //MMA Disabled Service                
                isDisabledProfile = true;                              
                if (SecurityServices != null && SecurityServices.Count > 0)
                {
                    IsExistingAccount = true;
                    if (SecurityServices.ToList().Exists(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["MMAService"].ToString(), StringComparison.OrdinalIgnoreCase)))
                    {
                        ServiceInstanceExists = true;
                        //Ignoring MMA Disable request if MMA service is present in DnP.
                        //if (!SecurityServices.ToList().Where(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["MMAService"].ToString(), StringComparison.OrdinalIgnoreCase)).FirstOrDefault().clientServiceInstanceStatus.value.Equals(STATUS_DISABLED, StringComparison.OrdinalIgnoreCase))
                        //{
                            throw new DnpException("Ignored as Advanced Security(MMA) Service is already present in DNP  for the given BillAccountNumber with Status " + SecurityServices.Where(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["MMAService"], StringComparison.OrdinalIgnoreCase)).FirstOrDefault().clientServiceInstanceStatus.value);
                        //}
                    }
                }
                if (!roleInstance.Attributes.ToList().Exists(ri => ri.Name.Equals("ISEXISTINGACCOUNT", StringComparison.OrdinalIgnoreCase)))
                    roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "ISEXISTINGACCOUNT", Value = IsExistingAccount.ToString() });
                else
                    roleInstance.Attributes.Where(attr => attr.Name.Equals("ISEXISTINGACCOUNT", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value = IsExistingAccount.ToString();
                roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "IsServiceInstanceExists", Value = ServiceInstanceExists.ToString() });
                roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "IsDisabledProfile", Value = isDisabledProfile.ToString() });
            }
            else if (orderAction == OrderActionEnum.amend && !string.IsNullOrEmpty(reason) && string.Equals(reason,"tradeup", StringComparison.OrdinalIgnoreCase))
            {
                if (SecurityServices != null && SecurityServices.Count > 0)
                {
                    // Ignore MMA TradeUp request when the customer has an MMA service or customer does not have Windows service 
                    if (SecurityServices.ToList().Exists(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["MMAService"].ToString(), StringComparison.OrdinalIgnoreCase)))
                        throw new DnpException("Ignored TradeUp request as Advanced Security(MMA) Service is already present in DNP");
                    else if (!SecurityServices.ToList().Exists(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["NPPWindowsService"].ToString(), StringComparison.OrdinalIgnoreCase)))
                        throw new DnpException("Ignored TradeUp request as Advanced Security(Windows) Service is not present in DNP");
                    else
                    {
                        CCID = SecurityServices.ToList().Where(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["NPPWindowsService"].ToString(), StringComparison.OrdinalIgnoreCase)).FirstOrDefault().name;
                        if (!roleInstance.Attributes.ToList().Exists(ri => ri.Name.Equals("CustomerContextId", StringComparison.OrdinalIgnoreCase)))
                        {
                            roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "CustomerContextId", Value = CCID });
                            roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "McAfeeCCID", Value = "CCIDnotpresent" });
                            //throw new DnpException("Ignored TradeUp request as CCID is not passed in the request");
                        }
                        else if (!CCID.Equals(roleInstance.Attributes.ToList().Where(ri => ri.Name.Equals("CustomerContextId", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value, StringComparison.OrdinalIgnoreCase))
                        {
                            roleInstance.Attributes.ToList().Where(ri => ri.Name.Equals("CustomerContextId", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value = CCID;
                            roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "McAfeeCCID", Value = "CCIDmismatch" });
                            //throw new DnpException("Ignored TradeUp request as CCID's do not match");
                        }
                    }
                }
                else
                    throw new DnpException("Ignored TradeUp request as no Security Service are present in DNP");
            }
            else if (orderAction == OrderActionEnum.amend || orderAction == OrderActionEnum.provide || orderAction == OrderActionEnum.vasReactivate)
            {
                // provision after disabled scenario
                if (SecurityServices != null && SecurityServices.Count > 0)
                {
                    IsExistingAccount = true;                     
                    if (!roleInstance.Attributes.ToList().Exists(ri => ri.Name.Equals("ISEXISTINGACCOUNT", StringComparison.OrdinalIgnoreCase)))
                        roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "ISEXISTINGACCOUNT", Value = IsExistingAccount.ToString() });
                    else
                        roleInstance.Attributes.Where(attr => attr.Name.Equals("ISEXISTINGACCOUNT", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value = IsExistingAccount.ToString();


                    if (SecurityServices.Exists(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["MMAService"].ToString(), StringComparison.OrdinalIgnoreCase)))
                    {                   
                        ServiceInstanceExists = true;

                        if (SecurityServices.ToList().Where(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["MMAService"].ToString(), StringComparison.OrdinalIgnoreCase)).FirstOrDefault().clientServiceInstanceStatus.value.Equals(STATUS_ACTIVE, StringComparison.OrdinalIgnoreCase))
                            throw new DnpException("Ignored as Advanced Security(MMA) Service is already present in DNP  for the given BillAccountNumber");
                        else if (SecurityServices.ToList().Where(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["MMAService"].ToString(), StringComparison.OrdinalIgnoreCase)).FirstOrDefault().clientServiceInstanceStatus.value.Equals(STATUS_ACTIVE, StringComparison.OrdinalIgnoreCase))
                            throw new DnpException("Ignored as Advanced Security(MMA) Service is already present in DNP  for the given BillAccountNumber");
                        else
                        {
                            CCID = SecurityServices.ToList().Where(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["MMAService"].ToString(), StringComparison.OrdinalIgnoreCase)).FirstOrDefault().name;
                            if (!roleInstance.Attributes.ToList().Exists(ri => ri.Name.Equals("CustomerContextId", StringComparison.OrdinalIgnoreCase)))
                                roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "CustomerContextId", Value = CCID });
                            else
                                roleInstance.Attributes.ToList().Where(ri => ri.Name.Equals("CustomerContextId", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value = CCID;
                            roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "IsServiceInstanceExists", Value = ServiceInstanceExists.ToString() });
                        }
                    }
                    else if (SecurityServices.Exists(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["NPPWindowsService"].ToString(), StringComparison.OrdinalIgnoreCase)))
                    {
                        if (SecurityServices.ToList().Where(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["NPPWindowsService"].ToString(), StringComparison.OrdinalIgnoreCase)).FirstOrDefault().clientServiceInstanceStatus.value.Equals(STATUS_ACTIVE, StringComparison.OrdinalIgnoreCase))
                            throw new DnpException("Ignored as Advanced Security(Windows) Service is already present in DNP  for the given BillAccountNumber with ACtive status");
                        // Add some attribute to delete NPP Windows in DnP if service is not in active state
                        else
                        {
                            CCID = SecurityServices.ToList().Where(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["NPPWindowsService"].ToString(), StringComparison.OrdinalIgnoreCase)).FirstOrDefault().name;
                            
                            if (!roleInstance.Attributes.ToList().Exists(ri => ri.Name.Equals("reason", StringComparison.OrdinalIgnoreCase)))
                                roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "Reason", Value = "DeleteNPPWindows:"+CCID });
                            else
                                roleInstance.Attributes.ToList().Where(ri => ri.Name.Equals("Reason", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value = "DeleteNPPWindows:" + CCID;

                            // Treating VasReactivate order as provision order(in case customer has ceased NPP Windows service in DnP(To avoid code changes in DnP and McAfee))
                            if (orderAction == OrderActionEnum.vasReactivate)
                            {
                                if (!roleInstance.Attributes.ToList().Exists(ri => ri.Name.Equals("ISVASREACTIVATE", StringComparison.OrdinalIgnoreCase)))
                                    roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "ISVASREACTIVATE", Value = "False" });
                                else
                                    roleInstance.Attributes.ToList().Where(ri => ri.Name.Equals("ISVASREACTIVATE", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value = "False";
                            }
                        }
                    }
                }
                // provision without disabled scenario
                else
                {
                    roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "IsServiceInstanceExists", Value = ServiceInstanceExists.ToString() });
                }
            }
        }

        internal void MapNPPWindows(OrderActionEnum orderAction, List<ClientServiceInstanceV1> SecurityServices)
        {
            if (orderAction == OrderActionEnum.amend)
            {
                if (SecurityServices != null && SecurityServices.Count > 0)
                    if (SecurityServices.ToList().Exists(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["MMAService"].ToString(), StringComparison.OrdinalIgnoreCase)))
                        throw new DnpException("Ignored NPP Windows Provision request as Advanced Security(MMA) Service is already present in DNP");
            }
        }
    }
}
