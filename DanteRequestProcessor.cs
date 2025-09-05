using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BT.SaaS.IspssAdapter;
using BT.SaaS.IspssAdapter.PE;
using BT.SaaS.IspssAdapter.Dnp;
using BT.SaaS.Core.MDMAPI.Entities.Product;
using MSEO = BT.SaaS.MSEOAdapter;
using SAASPE = BT.SaaS.Core.Shared.Entities;
using System.Configuration;
using BT.ESB.RoBT.ManageCustomer;
using com.bt.util.logging;

namespace BT.SaaS.MSEOAdapter
{
    public class DanteRequestProcessor
    {
        public static string bac_Identity_Domain = "VAS_BILLINGACCOUNT_ID";
        public static string OWM_Email_Supplier = ConfigurationManager.AppSettings["OWM_Email_Supplier"];
        public static string yahoo_Email_Supplier = ConfigurationManager.AppSettings["yahoo_Email_Supplier"];
        public static string email_identity_domain = "BTIEMAILID";
        private const string MARKED_INACTIVE = "MARKED-INACTIVE";
        public static string BTOnedomain = "BTCOM";
        private const string BACID_IDENTIFER_NAMEPACE = "VAS_BILLINGACCOUNT_ID";
        private const string ACTIVE = "ACTIVE";
        private const string ACTION_CEASED = "CEASED";
        private const string ACTION_SEARCH = "SEARCH";
        private const string ACTION_LINK = "LINK";
        private const string ACTION_UPDATE = "UPDATE";
        private const string ACTION_DELETE = "DELETE";
        private const string ACTION_UNLINK = "UNLINK";
        private const string ACTION_INSERT = "INSERT";
        private const string ACTION_EXPIRED = "EXPIRED";
        private const string ACTION_FORCE_INSERT = "FORCE_INS";
        const string BTEMAIL_SERVICECODE_NAMEPACE = "BTIEMAIL:DEFAULT";


        public static List<string> GetListOfEmailsOnPremiumAccountHolder(string BillAccountID, ref ClientServiceInstanceV1 emailServiceInstance)
        {
            List<string> ListOfEmails = new List<string>();
            List<string> pending_inactiveList = new List<string>();
            List<string> cease_inactiveList = new List<string>();
            Dictionary<string, string> pending_inactive_Dictionary = new Dictionary<string, string>();
            Dictionary<string, string> cease_inactive_Dictionary = new Dictionary<string, string>();
            try
            {
                GetBatchProfileV1Res response1 = DnpWrapper.GetServiceUserProfilesV1ForDante(BillAccountID, bac_Identity_Domain);
                if (response1 != null && response1.clientProfileV1 != null && response1.clientProfileV1.Count() > 0)
                {
                    foreach (ClientProfileV1 profile in response1.clientProfileV1)
                    {

                        if (profile.clientServiceInstanceV1 != null && profile.clientServiceInstanceV1.ToList().Exists(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase) && csi.clientServiceInstanceStatus.value.ToLower() == "pending-cease"))
                        {
                            emailServiceInstance = (from si in profile.clientServiceInstanceV1
                                                    where si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase)
                                                    && si.serviceIdentity.ToList().Exists(sIden => sIden.domain.Equals(bac_Identity_Domain, StringComparison.OrdinalIgnoreCase)
                                                    && sIden.value.Equals(BillAccountID, StringComparison.OrdinalIgnoreCase)) && si.clientServiceInstanceStatus.value.ToLower() == "pending-cease"
                                                    select si).FirstOrDefault();
                            List<string> PE_IN_EmailList = new List<string>();
                            List<string> CE_IN_EmailList = new List<string>();
                            foreach (ClientServiceRole sr in emailServiceInstance.clientServiceRole.Where(sr => sr.id.ToLower() == "default"))
                            {
                                if (sr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && ci.clientIdentityStatus.value.ToLower() == "pending-inactive"))
                                {
                                    PE_IN_EmailList.Add(sr.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && ci.clientIdentityStatus.value.ToLower() == "pending-inactive").FirstOrDefault().value);
                                }
                                if (sr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && ci.clientIdentityStatus.value.ToLower() == "cease-inactive"))
                                {
                                    CE_IN_EmailList.Add(sr.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && ci.clientIdentityStatus.value.ToLower() == "cease-inactive").FirstOrDefault().value);
                                }

                            }
                            if (PE_IN_EmailList.Count > 0)
                            {
                                pending_inactive_Dictionary.Add(PE_IN_EmailList.First(), string.Join(",", PE_IN_EmailList.ToArray()));
                            }
                            if (CE_IN_EmailList.Count > 0)
                            {
                                cease_inactive_Dictionary.Add(CE_IN_EmailList.First(), string.Join(",", CE_IN_EmailList.ToArray()));
                            }
                        }
                    }
                }

                foreach (string key in pending_inactive_Dictionary.Keys)
                {
                    string abc = key + ":" + pending_inactive_Dictionary[key];
                    pending_inactiveList.Add(abc);
                }
                foreach (string key in cease_inactive_Dictionary.Keys)
                {
                    string abc = key + ":" + cease_inactive_Dictionary[key];
                    cease_inactiveList.Add(abc);
                }
                if (pending_inactiveList != null)
                {
                    ListOfEmails.Add(string.Join(";", pending_inactiveList.ToArray()));
                }
                if (cease_inactiveList != null)
                {
                    ListOfEmails.Add(string.Join(";", cease_inactiveList.ToArray()));
                }   
            }
            finally
            {
                if (pending_inactive_Dictionary != null)
                {
                    pending_inactive_Dictionary.Clear();
                }
                if (cease_inactive_Dictionary != null)
                {
                    cease_inactive_Dictionary.Clear();
                }
                pending_inactiveList = null;
                cease_inactiveList = null;
            }
            return ListOfEmails;
        }

        public static List<string> GetListOfEmailsOnPremiumProvideAffliate(ClientProfileV1 clientProfile, string sistatus, string bakid)
        {
            List<string> ListOfEmails = new List<string>();
            List<string> pending_inactiveList = new List<string>();
            List<string> cease_inactiveList = new List<string>();
            try
            {
                if (sistatus.ToLower() != "active")
                {
                    ClientServiceInstanceV1 emailServiceInstance = (from si in clientProfile.clientServiceInstanceV1
                                                                    where si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase)
                                                                    && si.clientServiceInstanceStatus.value.ToLower() != "active" &&
                                                                    si.serviceIdentity.ToList().Where(sid => sid.domain.Equals(bac_Identity_Domain, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value.Equals(bakid, StringComparison.OrdinalIgnoreCase)
                                                                    select si).FirstOrDefault();

                    foreach (ClientServiceRole sr in emailServiceInstance.clientServiceRole.Where(sr => sr.id.ToLower() == "default"))
                    {
                        if (sr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && ci.clientIdentityStatus.value.ToLower() == "pending-inactive"))
                        {
                            pending_inactiveList.Add(sr.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && ci.clientIdentityStatus.value.ToLower() == "pending-inactive").FirstOrDefault().value);
                        }
                        if (sr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && ci.clientIdentityStatus.value.ToLower() == "cease-inactive"))
                        {
                            cease_inactiveList.Add(sr.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && ci.clientIdentityStatus.value.ToLower() == "cease-inactive").FirstOrDefault().value);
                        }
                    }
                }
                else
                {
                    ClientServiceInstanceV1 emailServiceInstance = (from si in clientProfile.clientServiceInstanceV1
                                                                    where si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase)
                                                                    && si.clientServiceInstanceStatus.value.ToLower() == "active" &&
                                                                    si.serviceIdentity.ToList().Where(sid => sid.domain.Equals(bac_Identity_Domain, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value.Equals(bakid, StringComparison.OrdinalIgnoreCase)
                                                                    select si).FirstOrDefault();

                    foreach (ClientServiceRole sr in emailServiceInstance.clientServiceRole.Where(sr => sr.id.ToLower() == "default"))
                    {
                        if (sr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && ci.clientIdentityStatus.value.ToLower() == "active"))
                        {
                            pending_inactiveList.Add(sr.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && ci.clientIdentityStatus.value.ToLower() == "active").FirstOrDefault().value);
                        }
                        if (sr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && ci.clientIdentityStatus.value.ToLower() == "inactive"))
                        {
                            cease_inactiveList.Add(sr.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && ci.clientIdentityStatus.value.ToLower() == "inactive").FirstOrDefault().value);
                        }
                    }
                }
                if (pending_inactiveList != null && pending_inactiveList.Count > 0)
                {
                    ListOfEmails.Add(pending_inactiveList[0] + ":" + string.Join(",", pending_inactiveList.ToArray()));
                }
                else
                {
                    ListOfEmails.Add(string.Empty);
                }

                if (cease_inactiveList != null && cease_inactiveList.Count > 0)
                {
                    ListOfEmails.Add(cease_inactiveList[0] + ":" + string.Join(",", cease_inactiveList.ToArray()));
                }
                else
                {
                    ListOfEmails.Add(string.Empty);
                }
            }
            finally
            {
                pending_inactiveList = null;
                cease_inactiveList = null;
            }

            return ListOfEmails;
        }

        public static List<string> GetListOfEmailsOnCease(string identity, string emailName, string ceaseType, string orderType, ref string DataStore, ref string emailSupplier, ref string vasProductID,ref Dictionary<string,string> listofSupplierMailboxes)
        {
            List<string> ListOfEmails = new List<string>();
            List<string> activeList = new List<string>();
            List<string> proposedList = new List<string>();
            List<string> inactiveList = new List<string>();
            List<string> unmergedList = new List<string>();
            List<string> affiliateList = new List<string>();
            Dictionary<string, string> activeDictionary = new Dictionary<string, string>();
            Dictionary<string, string> proposedDictionary = new Dictionary<string, string>();
            Dictionary<string, string> inactiveDictionary = new Dictionary<string, string>();
            Dictionary<string, string> unmergedDictionary = new Dictionary<string, string>();
            Dictionary<string, string> affiliateDictionary = new Dictionary<string, string>();
            GetBatchProfileV1Res response1 = null;string emailsList = string.Empty;
            try
            {
                //active ----------> abc:abc,adsad   asd:asd,asdad,asda,asd ;
                //propsed -------> def:def,adsa,asdas ; xyz:xyz,asda ;
                // GSUPv1 assuming bak will cum in the request 
                // pick all the identities not in expired status
                // will recive many cline tptofiles 

                bool isDelinkBTREnabled = Convert.ToBoolean(ConfigurationManager.AppSettings["IsBTRCE191515enabled"]);

                response1 = DnpWrapper.GetServiceUserProfilesV1ForDante(identity, bac_Identity_Domain);
                if (response1 != null && response1.clientProfileV1 != null && response1.clientProfileV1.Count() > 0)
                {
                    bool isSIExist = false;
                    bool isEmailExist = false;
                    string accountId = string.Empty;
                    foreach (ClientProfileV1 profile in response1.clientProfileV1)
                    {
                        if (profile.clientServiceInstanceV1 != null && profile.clientServiceInstanceV1.ToList().Exists(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase) && csi.clientServiceInstanceStatus.value.ToLower() == "active"))                           
                        {
                            if (ceaseType.Equals("ispcease", StringComparison.OrdinalIgnoreCase))
                            {
                                isSIExist = true;
                                if (profile != null && profile.client != null && profile.client.clientIdentity != null)
                                {
                                        if (profile.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && ci.value.Equals(emailName, StringComparison.OrdinalIgnoreCase)))
                                        {
                                            isEmailExist = true;
                                        }
                                    DataStore = GenerateEmailDataStore(profile, null, identity, ref emailSupplier, ref vasProductID);
                                }
                            }
                            else if (ceaseType.Equals("vascease", StringComparison.OrdinalIgnoreCase))
                            {
                                DataStore = GenerateEmailDataStore(profile, null, identity, ref emailSupplier, ref vasProductID);
                            }
                            if (DataStore.Equals("D&P"))
                            {
                                var emailServiceInstance = (from si in profile.clientServiceInstanceV1
                                                            where si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase) && si.clientServiceInstanceStatus.value.ToLower() == "active" && si.serviceIdentity.ToList().Exists(sIden => sIden.domain.Equals(bac_Identity_Domain, StringComparison.OrdinalIgnoreCase) && sIden.value.Equals(identity, StringComparison.OrdinalIgnoreCase))
                                                            select si).FirstOrDefault();
                                List<string> activeEmailList = new List<string>();
                                List<string> proposedEmailList = new List<string>();
                                List<string> inactiveEmailList = new List<string>();
                                List<string> unmergedEmailList = new List<string>();
                                List<string> affiliateEmailList = new List<string>();
                                // for BB Email cease
                                if (orderType.Equals("EMAILCEASE", StringComparison.OrdinalIgnoreCase) || orderType.Equals("EMAILMODIFYCEASE", StringComparison.OrdinalIgnoreCase) || orderType.Equals("EMAILREGRADE",StringComparison.OrdinalIgnoreCase))
                                {
                                    //bool isAffiliateProfile = false;
                                    //if (isDelinkBTREnabled)
                                    //{
                                    //    if (!profile.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(bac_Identity_Domain, StringComparison.OrdinalIgnoreCase) && ci.value.Equals(identity)))
                                    //    {
                                    //        isAffiliateProfile = true;
                                    //    }
                                    //}

                                    foreach (ClientServiceRole sr in emailServiceInstance.clientServiceRole.Where(sr => sr.id.ToLower() == "default"))
                                    {
                                        if (sr.clientIdentity != null && sr.clientIdentity.Any())
                                        {
                                            if (sr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && (ci.clientIdentityStatus.value.ToLower() == "active" || ci.clientIdentityStatus.value.ToLower() == "transferring")))
                                            {
                                                activeEmailList.Add(sr.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && (ci.clientIdentityStatus.value.ToLower() == "active" || ci.clientIdentityStatus.value.ToLower() == "transferring")).FirstOrDefault().value);
                                                //if (isDelinkBTREnabled && isAffiliateProfile)
                                                //    affiliateEmailList.Add(sr.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && (ci.clientIdentityStatus.value.ToLower() == "active" || ci.clientIdentityStatus.value.ToLower() == "transferring")).FirstOrDefault().value);
                                            }
                                            else if (sr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && ci.clientIdentityStatus.value.ToLower() == "proposed"))
                                            {
                                                proposedEmailList.Add(sr.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && ci.clientIdentityStatus.value.ToLower() == "proposed").FirstOrDefault().value);
                                            }
                                            else if (sr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && (ci.clientIdentityStatus.value.ToLower() == "marked-inactive" || ci.clientIdentityStatus.value.ToLower() == "barred-abuse" || ci.clientIdentityStatus.value.ToLower() == "inactive")))
                                            {
                                                if(!orderType.Equals("EMAILREGRADE", StringComparison.OrdinalIgnoreCase))                                                    
                                                    inactiveEmailList.Add(sr.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && (ci.clientIdentityStatus.value.ToLower() == "marked-inactive" || ci.clientIdentityStatus.value.ToLower() == "barred-abuse" || ci.clientIdentityStatus.value.ToLower() == "inactive")).FirstOrDefault().value);
                                                else if (sr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && (ci.clientIdentityStatus.value.ToLower() == "marked-inactive")))
                                                    inactiveEmailList.Add(sr.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && (ci.clientIdentityStatus.value.ToLower() == "marked-inactive")).FirstOrDefault().value);
                                                //if (isDelinkBTREnabled && isAffiliateProfile)
                                                //    affiliateEmailList.Add(sr.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && (ci.clientIdentityStatus.value.ToLower() == "marked-inactive" || ci.clientIdentityStatus.value.ToLower() == "barred-abuse" || ci.clientIdentityStatus.value.ToLower() == "inactive")).FirstOrDefault().value);
                                            }
                                            else if (sr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && ci.clientIdentityStatus.value.ToLower() != "expired" && (sr.clientServiceRoleStatus.value.Equals("UNRESOLVED", StringComparison.OrdinalIgnoreCase))))
                                            {
                                                //Do not pick expired mailboxes
                                                unmergedEmailList.Add(sr.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && (sr.clientServiceRoleStatus.value.ToLower() == "unresolved")).FirstOrDefault().value);
                                            }
                                        }
                                        else
                                            throw new DnpException("Default Role Identities missing for email service on email cease");
                                    }

                                    if (activeEmailList != null && activeEmailList.Count() > 0)
                                    {
                                        activeDictionary.Add(activeEmailList.First(), string.Join(",", activeEmailList.ToArray()));
                                    }
                                    //if (affiliateEmailList != null && affiliateEmailList.Count() > 0)
                                    //{
                                    //    affiliateDictionary.Add(affiliateEmailList.First(), string.Join(",", affiliateEmailList.ToArray()));
                                    //}
                                    if (proposedEmailList != null && proposedEmailList.Count() > 0)
                                    {
                                        proposedDictionary.Add(proposedEmailList.First(), string.Join(",", proposedEmailList.ToArray()));
                                    }
                                    if (inactiveEmailList != null && inactiveEmailList.Count() > 0)
                                    {
                                        inactiveDictionary.Add(inactiveEmailList.First(), string.Join(",", inactiveEmailList.ToArray()));
                                    }
                                    if (unmergedEmailList != null && unmergedEmailList.Count() > 0)
                                    {
                                        unmergedDictionary.Add(unmergedEmailList.First(), string.Join(",", unmergedEmailList.ToArray()));
                                    }
                                }
                                // For Premium Email cease
                                else if (orderType.Equals("PREMIUMEMAILCEASE", StringComparison.OrdinalIgnoreCase))
                                {
                                    foreach (ClientServiceRole sr in emailServiceInstance.clientServiceRole.Where(sr => sr.id.ToLower() == "default"))
                                    {
                                        if (sr.clientIdentity != null && sr.clientIdentity.Any())
                                        {
                                            if (sr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && (ConfigurationManager.AppSettings["PremiumEmailCeaseStatus"].Split(',').Contains(ci.clientIdentityStatus.value.ToLower()))))
                                            {
                                                activeEmailList.Add(sr.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && (ConfigurationManager.AppSettings["PremiumEmailCeaseStatus"].Split(',').Contains(ci.clientIdentityStatus.value.ToLower()))).FirstOrDefault().value);
                                            }
                                            else if (sr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && (ConfigurationManager.AppSettings["PremiumEmailRoleDeleteStatuses"].Split(',').Contains(ci.clientIdentityStatus.value.ToLower()))))
                                            {
                                                proposedEmailList.Add(sr.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && (ConfigurationManager.AppSettings["PremiumEmailRoleDeleteStatuses"].Split(',').Contains(ci.clientIdentityStatus.value.ToLower()))).FirstOrDefault().value);
                                            }
                                        }
                                        else
                                            throw new DnpException("Default Role Identities missing for email service on premiun email cease");
                                    }

                                    if (activeEmailList != null && activeEmailList.Count() > 0)
                                    {
                                        activeDictionary.Add(activeEmailList.First(), string.Join(",", activeEmailList.ToArray()));
                                    }
                                    if (proposedEmailList != null && proposedEmailList.Count() > 0)
                                    {
                                        proposedDictionary.Add(proposedEmailList.First(), string.Join(",", proposedEmailList.ToArray()));
                                    }
                                }
                            }
                        }
                    }
                    if (ceaseType.Equals("ispcease")) 
                    {
                        if (!isSIExist)
                            throw new DnpException("No ACTIVE Email Service is mapped for this billingaccountID in DnP ");
                        else if (!isEmailExist)
                            throw new DnpException("The Email is not linked to the billingaccountID in DnP ");
                    }
                    if ((orderType.Equals("EMAILCEASE", StringComparison.OrdinalIgnoreCase) || orderType.Equals("PREMIUMEMAILCEASE", StringComparison.OrdinalIgnoreCase)) && isDelinkBTREnabled && DataStore.Equals("D&P"))
                    {
                        affiliateDictionary = GetListOfAffiliateEmails(identity, emailName, orderType, response1);
                    }
                }
                else
                {
                    throw new DnpException("No active profile found for this billingaccountID in DnP");
                }

                foreach (string key in activeDictionary.Keys)
                {
                    string abc = key + ":" + activeDictionary[key];
                    activeList.Add(abc);
                    emailsList = emailsList + ";" + abc;
                }

                foreach (string key in affiliateDictionary.Keys)
                {
                    string abc = key + ":" + affiliateDictionary[key];
                    affiliateList.Add(abc);
                    emailsList = emailsList + ";" + abc;
                }

                foreach (string key in proposedDictionary.Keys)
                {
                    string abc = key + ":" + proposedDictionary[key];
                    proposedList.Add(abc);
                    emailsList = emailsList + ";" + abc;
                }
                foreach (string key in inactiveDictionary.Keys)
                {
                    string abc = key + ":" + inactiveDictionary[key];
                    inactiveList.Add(abc);
                    emailsList = emailsList + ";" + abc;
                }
                foreach (string key in unmergedDictionary.Keys)
                {
                    string abc = key + ":" + unmergedDictionary[key];
                    unmergedList.Add(abc);
                    emailsList = emailsList + ";" + abc;
                }

                if (activeList != null)
                {
                    ListOfEmails.Add(string.Join(";", activeList.ToArray()));
                }
                else
                {
                    ListOfEmails.Add(" ");
                }
                if (proposedList != null)
                {
                    ListOfEmails.Add(string.Join(";", proposedList.ToArray()));
                }
                else
                {
                    ListOfEmails.Add(" ");
                }
                if (inactiveList != null)
                {
                    ListOfEmails.Add(string.Join(";", inactiveList.ToArray()));
                }
                else
                {
                    ListOfEmails.Add(" ");
                }
                if (unmergedList != null)
                {
                    ListOfEmails.Add(string.Join(";", unmergedList.ToArray()));
                }
                else
                {
                    ListOfEmails.Add(" ");
                }
                if (affiliateList != null)
                {
                    ListOfEmails.Add(string.Join(";", affiliateList.ToArray()));
                }
                else
                {
                    ListOfEmails.Add(" ");
                }
                if (Convert.ToBoolean(ConfigurationManager.AppSettings["isCrossProvisioningEnabled"]))
                    listofSupplierMailboxes = GetListOfSuplierEmails(identity, emailName, orderType, response1,emailsList);
            }
            finally
            {
                if (proposedDictionary != null)
                {
                    proposedDictionary.Clear();
                }
                if (activeDictionary != null)
                {
                    activeDictionary.Clear();
                }
                if (inactiveDictionary != null)
                {
                    inactiveDictionary.Clear();
                }
                if (unmergedDictionary != null)
                {
                    unmergedDictionary.Clear();
                }
                if (affiliateDictionary != null)
                {
                    affiliateDictionary.Clear();
                }
                activeList = null;
                proposedList = null;
                inactiveList = null;
                unmergedList = null;
                affiliateList = null;
                response1 = null;
            }

            return ListOfEmails;
        }

        public static List<string> GetListOfEmailsOnNayanCease(string identity, string ceaseType, string orderType, ref string emailSupplier, ref string vasProductID, ref Dictionary<string, string> listofSupplierMailboxes)
        {
            List<string> ListOfEmails = new List<string>();
            List<string> activeList = new List<string>();
            List<string> proposedList = new List<string>();
            List<string> inactiveList = new List<string>();
            List<string> unmergedList = new List<string>();
            List<string> affiliateList = new List<string>();
            Dictionary<string, string> activeDictionary = new Dictionary<string, string>();
            Dictionary<string, string> proposedDictionary = new Dictionary<string, string>();
            Dictionary<string, string> inactiveDictionary = new Dictionary<string, string>();
            Dictionary<string, string> unmergedDictionary = new Dictionary<string, string>();
            Dictionary<string, string> affiliateDictionary = new Dictionary<string, string>();
            GetBatchProfileV1Res response1 = null; string emailsList = string.Empty;
            
            try
            {
                
                response1 = DnpWrapper.GetServiceUserProfilesV1ForDante(identity, bac_Identity_Domain);
                
                if (response1 != null && response1.clientProfileV1 != null && response1.clientProfileV1.Count() > 0)
                {
                    bool isSIExist = false;
                    string accountId = string.Empty;
                    foreach (ClientProfileV1 profile in response1.clientProfileV1)
                    {
                        if (profile.clientServiceInstanceV1 != null && profile.clientServiceInstanceV1.ToList().Exists(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase) && csi.clientServiceInstanceStatus.value.ToLower() == "active"))
                        {
                            if (ceaseType.Equals("nayanemailcease", StringComparison.OrdinalIgnoreCase))
                            {
                                isSIExist = true;                                
                            }                           
                            var emailServiceInstance = (from si in profile.clientServiceInstanceV1
                                                            where si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase) && si.clientServiceInstanceStatus.value.ToLower() == "active" && si.serviceIdentity.ToList().Exists(sIden => sIden.domain.Equals(bac_Identity_Domain, StringComparison.OrdinalIgnoreCase) && sIden.value.Equals(identity, StringComparison.OrdinalIgnoreCase))
                                                            select si).FirstOrDefault();
                            //var n = (from si in profile.clientServiceInstanceV1 where si.clientServiceInstanceCharacteristic.ToList().Exists(csi => csi.name.Equals("VAS_PRODUCT_ID", StringComparison.OrdinalIgnoreCase))
                            //         select si).FirstOrDefault();
                           if(emailServiceInstance.clientServiceInstanceCharacteristic.ToList().Exists(csi => csi.name.Equals("VAS_PRODUCT_ID")))
                            {
                                vasProductID = emailServiceInstance.clientServiceInstanceCharacteristic.ToList().Where(ic => ic.name.Equals("VAS_PRODUCT_ID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                            }

                             List<string> activeEmailList = new List<string>();
                             List<string> proposedEmailList = new List<string>();
                             List<string> inactiveEmailList = new List<string>();
                             List<string> unmergedEmailList = new List<string>();
                             List<string> affiliateEmailList = new List<string>();
                             // for BB Email cease
                             if (orderType.Equals("EMAILCEASE", StringComparison.OrdinalIgnoreCase))
                             {
                                    foreach (ClientServiceRole sr in emailServiceInstance.clientServiceRole.Where(sr => sr.id.ToLower() == "default"))
                                    {
                                        if (sr.clientIdentity != null && sr.clientIdentity.Any())
                                        {
                                            if (sr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && (ci.clientIdentityStatus.value.ToLower() == "active" || ci.clientIdentityStatus.value.ToLower() == "transferring")))
                                            {
                                                activeEmailList.Add(sr.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && (ci.clientIdentityStatus.value.ToLower() == "active" || ci.clientIdentityStatus.value.ToLower() == "transferring")).FirstOrDefault().value);                                                                                                
                                            }
                                            else if (sr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && ci.clientIdentityStatus.value.ToLower() == "proposed"))
                                            {
                                                proposedEmailList.Add(sr.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && ci.clientIdentityStatus.value.ToLower() == "proposed").FirstOrDefault().value);
                                            }
                                            else if (sr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && (ci.clientIdentityStatus.value.ToLower() == "marked-inactive" || ci.clientIdentityStatus.value.ToLower() == "barred-abuse" || ci.clientIdentityStatus.value.ToLower() == "inactive")))
                                            {
                                                if (!orderType.Equals("EMAILREGRADE", StringComparison.OrdinalIgnoreCase))
                                                    inactiveEmailList.Add(sr.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && (ci.clientIdentityStatus.value.ToLower() == "marked-inactive" || ci.clientIdentityStatus.value.ToLower() == "barred-abuse" || ci.clientIdentityStatus.value.ToLower() == "inactive")).FirstOrDefault().value);
                                                else if (sr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && (ci.clientIdentityStatus.value.ToLower() == "marked-inactive")))
                                                    inactiveEmailList.Add(sr.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && (ci.clientIdentityStatus.value.ToLower() == "marked-inactive")).FirstOrDefault().value);                                                                                                
                                            }
                                            else if (sr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && ci.clientIdentityStatus.value.ToLower() != "expired" && (sr.clientServiceRoleStatus.value.Equals("UNRESOLVED", StringComparison.OrdinalIgnoreCase))))
                                            {
                                                //Do not pick expired mailboxes
                                                unmergedEmailList.Add(sr.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && (sr.clientServiceRoleStatus.value.ToLower() == "unresolved")).FirstOrDefault().value);
                                            }
                                        }
                                        else
                                            throw new DnpException("Default Role Identities missing for email service on email cease");
                                    }

                                    if (activeEmailList != null && activeEmailList.Count() > 0)
                                    {
                                        activeDictionary.Add(activeEmailList.First(), string.Join(",", activeEmailList.ToArray()));
                                    }                               
                                    if (proposedEmailList != null && proposedEmailList.Count() > 0)
                                    {
                                        proposedDictionary.Add(proposedEmailList.First(), string.Join(",", proposedEmailList.ToArray()));
                                    }
                                    if (inactiveEmailList != null && inactiveEmailList.Count() > 0)
                                    {
                                        inactiveDictionary.Add(inactiveEmailList.First(), string.Join(",", inactiveEmailList.ToArray()));
                                    }
                                    if (unmergedEmailList != null && unmergedEmailList.Count() > 0)
                                    {
                                        unmergedDictionary.Add(unmergedEmailList.First(), string.Join(",", unmergedEmailList.ToArray()));
                                    }
                             }
                                // For Premium Email cease
                              else if (orderType.Equals("PREMIUMEMAILCEASE", StringComparison.OrdinalIgnoreCase))
                              {
                                    foreach (ClientServiceRole sr in emailServiceInstance.clientServiceRole.Where(sr => sr.id.ToLower() == "default"))
                                    {
                                        if (sr.clientIdentity != null && sr.clientIdentity.Any())
                                        {
                                            if (sr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && (ConfigurationManager.AppSettings["PremiumEmailCeaseStatus"].Split(',').Contains(ci.clientIdentityStatus.value.ToLower()))))
                                            {
                                                activeEmailList.Add(sr.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && (ConfigurationManager.AppSettings["PremiumEmailCeaseStatus"].Split(',').Contains(ci.clientIdentityStatus.value.ToLower()))).FirstOrDefault().value);
                                            }
                                            else if (sr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && (ConfigurationManager.AppSettings["PremiumEmailRoleDeleteStatuses"].Split(',').Contains(ci.clientIdentityStatus.value.ToLower()))))
                                            {
                                                proposedEmailList.Add(sr.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && (ConfigurationManager.AppSettings["PremiumEmailRoleDeleteStatuses"].Split(',').Contains(ci.clientIdentityStatus.value.ToLower()))).FirstOrDefault().value);
                                            }
                                        }
                                        else
                                            throw new DnpException("Default Role Identities missing for email service on premiun email cease");
                                    }

                                    if (activeEmailList != null && activeEmailList.Count() > 0)
                                    {
                                        activeDictionary.Add(activeEmailList.First(), string.Join(",", activeEmailList.ToArray()));
                                    }
                                    if (proposedEmailList != null && proposedEmailList.Count() > 0)
                                    {
                                        proposedDictionary.Add(proposedEmailList.First(), string.Join(",", proposedEmailList.ToArray()));
                                    }
                              }
                        }
                    }
                    if (ceaseType.Equals("nayanemailcease"))
                    {
                        if (!isSIExist)
                            throw new DnpException("No ACTIVE Email Service is mapped for this billingaccountID in DnP ");
                    }
                    //if ((orderType.Equals("EMAILCEASE", StringComparison.OrdinalIgnoreCase) || orderType.Equals("PREMIUMEMAILCEASE", StringComparison.OrdinalIgnoreCase)))
                    //{
                    //    affiliateDictionary = GetListOfAffiliateEmails(identity, string.Empty, orderType, response1);
                    //}
                }
                else
                {
                    throw new DnpException("No active profile found for this billingaccountID in DnP");
                }

                foreach (string key in activeDictionary.Keys)
                {
                    string abc = key + ":" + activeDictionary[key];
                    activeList.Add(abc);
                    emailsList = emailsList + ";" + abc;
                }

                //foreach (string key in affiliateDictionary.Keys)
                //{
                //    string abc = key + ":" + affiliateDictionary[key];
                //    affiliateList.Add(abc);
                //    emailsList = emailsList + ";" + abc;
                //}

                foreach (string key in proposedDictionary.Keys)
                {
                    string abc = key + ":" + proposedDictionary[key];
                    proposedList.Add(abc);
                    emailsList = emailsList + ";" + abc;
                }
                foreach (string key in inactiveDictionary.Keys)
                {
                    string abc = key + ":" + inactiveDictionary[key];
                    inactiveList.Add(abc);
                    emailsList = emailsList + ";" + abc;
                }
                foreach (string key in unmergedDictionary.Keys)
                {
                    string abc = key + ":" + unmergedDictionary[key];
                    unmergedList.Add(abc);
                    emailsList = emailsList + ";" + abc;
                }

                if (activeList != null)
                {
                    ListOfEmails.Add(string.Join(";", activeList.ToArray()));
                }
                else
                {
                    ListOfEmails.Add(" ");
                }
                if (proposedList != null)
                {
                    ListOfEmails.Add(string.Join(";", proposedList.ToArray()));
                }
                else
                {
                    ListOfEmails.Add(" ");
                }
                if (inactiveList != null)
                {
                    ListOfEmails.Add(string.Join(";", inactiveList.ToArray()));
                }
                else
                {
                    ListOfEmails.Add(" ");
                }
                if (unmergedList != null)
                {
                    ListOfEmails.Add(string.Join(";", unmergedList.ToArray()));
                }
                else
                {
                    ListOfEmails.Add(" ");
                }
                //if (affiliateList != null)
                //{
                //    ListOfEmails.Add(string.Join(";", affiliateList.ToArray()));
                //}
                //else
                //{
                //    ListOfEmails.Add(" ");
                //}
                if (Convert.ToBoolean(ConfigurationManager.AppSettings["isCrossProvisioningEnabled"]))
                    listofSupplierMailboxes = GetListOfSuplierEmails(identity, string.Empty, orderType, response1, emailsList);
            }
            finally
            {
                if (proposedDictionary != null)
                {
                    proposedDictionary.Clear();
                }
                if (activeDictionary != null)
                {
                    activeDictionary.Clear();
                }
                if (inactiveDictionary != null)
                {
                    inactiveDictionary.Clear();
                }
                if (unmergedDictionary != null)
                {
                    unmergedDictionary.Clear();
                }
                //if (affiliateDictionary != null)
                //{
                //    affiliateDictionary.Clear();
                //}
                activeList = null;
                proposedList = null;
                inactiveList = null;
                unmergedList = null;
                //affiliateList = null;
                response1 = null;
            }

            return ListOfEmails;
        }
        public static Dictionary<string, string> GetListOfAffiliateEmails(string identity, string emailName, string orderType, GetBatchProfileV1Res response1)
        {
            Dictionary<string, string> affiliateDictionary = new Dictionary<string, string>(); ClientProfileV1 accHolderProfile = null;

            foreach (ClientProfileV1 profile in response1.clientProfileV1)
            {
                if (profile.clientServiceInstanceV1 != null && profile.clientServiceInstanceV1.ToList().Exists(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase) && csi.clientServiceInstanceStatus.value.ToLower() == "active"))
                {
                    var emailServiceInstance = (from si in profile.clientServiceInstanceV1
                                                where si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase) && si.clientServiceInstanceStatus.value.ToLower() == "active" && si.serviceIdentity.ToList().Exists(sIden => sIden.domain.Equals(bac_Identity_Domain, StringComparison.OrdinalIgnoreCase) && sIden.value.Equals(identity, StringComparison.OrdinalIgnoreCase))
                                                select si).FirstOrDefault();
                    string primaryEmail = emailServiceInstance.serviceIdentity != null && emailServiceInstance.serviceIdentity.ToList().Exists(x => x.domain.Equals("BTIEMAILID") && !string.IsNullOrEmpty(x.value)) ? emailServiceInstance.serviceIdentity.ToList().Where(x => x.domain.Equals("BTIEMAILID")).FirstOrDefault().value : string.Empty;

                    bool isAffiliateProfile = false;

                    if (!profile.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(bac_Identity_Domain, StringComparison.OrdinalIgnoreCase) && ci.value.Equals(identity))
                        && profile.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase))
                        && !emailServiceInstance.clientServiceRole.ToList().Exists(csr=>csr.clientServiceRoleStatus.value.Equals("unresolved", StringComparison.OrdinalIgnoreCase)))
                    {
                        isAffiliateProfile = true;
                    }
                    else
                        accHolderProfile = profile;

                    if (isAffiliateProfile)
                    {
                        List<string> affiliateEmailList = new List<string>();
                        // for BB Email cease
                        foreach (ClientServiceRole sr in emailServiceInstance.clientServiceRole.Where(sr => sr.id.ToLower() == "default"))
                        {
                            if (orderType.Equals("EMAILCEASE", StringComparison.OrdinalIgnoreCase))
                            {
                                if (sr.clientIdentity != null && sr.clientIdentity.Any() && sr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && !(sr.clientServiceRoleStatus.value.Equals("UNRESOLVED", StringComparison.OrdinalIgnoreCase))))
                                {
                                    //Pick only active and inactive affiliate mailboxes
                                    if (sr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && (ci.clientIdentityStatus.value.ToLower() == "active" || ci.clientIdentityStatus.value.ToLower() == "transferring" || ci.clientIdentityStatus.value.ToLower() == "marked-inactive" || ci.clientIdentityStatus.value.ToLower() == "barred-abuse" || ci.clientIdentityStatus.value.ToLower() == "inactive")))
                                    {
                                        affiliateEmailList.Add(sr.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && (ci.clientIdentityStatus.value.ToLower() == "active" || ci.clientIdentityStatus.value.ToLower() == "transferring" || ci.clientIdentityStatus.value.ToLower() == "marked-inactive" || ci.clientIdentityStatus.value.ToLower() == "barred-abuse" || ci.clientIdentityStatus.value.ToLower() == "inactive")).FirstOrDefault().value);
                                    }
                                }
                            }
                            // For Premium Email cease
                            else if (orderType.Equals("PREMIUMEMAILCEASE", StringComparison.OrdinalIgnoreCase))
                            {
                                if (sr.clientIdentity != null && sr.clientIdentity.Any())
                                {
                                    if (sr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && !(sr.clientServiceRoleStatus.value.Equals("UNRESOLVED", StringComparison.OrdinalIgnoreCase))))
                                    {
                                        if (sr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && (!ConfigurationManager.AppSettings["PremiumEmailCeaseStatus"].Split(',').Contains(ci.clientIdentityStatus.value.ToLower()))))
                                        {
                                            affiliateEmailList.Add(sr.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && (!ConfigurationManager.AppSettings["PremiumEmailCeaseStatus"].Split(',').Contains(ci.clientIdentityStatus.value.ToLower()))).FirstOrDefault().value);
                                        }
                                    }
                                }
                            }
                        }
                        string BtOneId = profile.client.clientIdentity != null && profile.client.clientIdentity.ToList().Exists(x => x.managedIdentifierDomain.value.Equals("BTCOM")) ? profile.client.clientIdentity.ToList().Where(x => x.managedIdentifierDomain.value.Equals("BTCOM")).FirstOrDefault().value : string.Empty;
                        string CONKID = profile.client.clientIdentity != null && profile.client.clientIdentity.ToList().Exists(x => x.managedIdentifierDomain.value.Equals("CONKID")) ? profile.client.clientIdentity.ToList().Where(x => x.managedIdentifierDomain.value.Equals("CONKID")).FirstOrDefault().value : string.Empty;
                        string primaryContactEmail = profile.client.clientCharacteristic != null && profile.client.clientCharacteristic.Count() > 0 && profile.client.clientCharacteristic.ToList().Exists(chr => chr.name.Equals("PRIMARY_EMAIL", StringComparison.OrdinalIgnoreCase)) ? profile.client.clientCharacteristic.ToList().Where(chr => chr.name.Equals("PRIMARY_EMAIL", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value.ToString() : string.Empty;

                        string kciDetails = BtOneId + "," + CONKID + "," + primaryContactEmail;
                        kciDetails = string.Concat(BtOneId, ",", CONKID, ",", primaryContactEmail);

                        if (affiliateEmailList != null && affiliateEmailList.Count() > 0)
                        {
                            affiliateDictionary.Add(kciDetails, string.Join(",", affiliateEmailList.ToArray()));
                        }
                    }
                }
            }
            return affiliateDictionary;
        }
        
        public static Dictionary<string, string> GetListOfSuplierEmails(string identity, string emailName, string orderType, GetBatchProfileV1Res response1,string listofEmails)
        {
            Dictionary<string, string> supplierMailboxesList = new Dictionary<string, string>();
            List<string> mxMailboxesList = new List<string>(); List<string> yahooMailboxesList = new List<string>(); List<string> cpmsMailboxesList = new List<string>();
            string emailSrvcSupplier = string.Empty;

            foreach (ClientProfileV1 profile in response1.clientProfileV1)
            {
                if(profile.clientServiceInstanceV1 != null&& profile.clientServiceInstanceV1.ToList().Exists(si=> si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase)))
                {
                    var emailServiceInstance = (from si in profile.clientServiceInstanceV1
                                                where si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase) && si.serviceIdentity.ToList().Exists(sIden => sIden.domain.Equals(bac_Identity_Domain, StringComparison.OrdinalIgnoreCase) && sIden.value.Equals(identity, StringComparison.OrdinalIgnoreCase))
                                                select si).FirstOrDefault();

                    if(emailServiceInstance !=null)
                        emailSrvcSupplier = emailServiceInstance.clientServiceInstanceCharacteristic != null && emailServiceInstance.clientServiceInstanceCharacteristic.ToList().Exists(x => x.name.Equals("EMAIL_SUPPLIER",StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(x.value)) ? emailServiceInstance.clientServiceInstanceCharacteristic.ToList().Where(x => x.name.Equals("EMAIL_SUPPLIER", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value : ConfigurationManager.AppSettings["VASEmailSupplierSwitch"];
                }
                if (Convert.ToBoolean(ConfigurationManager.AppSettings["IsdefaultemailsupplierMX"]))
                {
                    emailSrvcSupplier = "MX";
                }
                if (profile.client != null && profile.client.clientIdentity != null && profile.client.clientIdentity.Count() > 0 && profile.client.clientIdentity.ToList().Exists(x => x.managedIdentifierDomain!= null && x.managedIdentifierDomain.value.Equals("BTIEMAILID",StringComparison.OrdinalIgnoreCase)))
                {
                    var emailIdentitiesList = (from ci in profile.client.clientIdentity
                                               where ci.managedIdentifierDomain.value.Equals("BTIEMAILID", StringComparison.OrdinalIgnoreCase)
                                               select ci).ToList();

                    foreach(ClientIdentity ci in emailIdentitiesList)
                    {
                        if(!string.IsNullOrEmpty(ci.value) && listofEmails.Contains(ci.value))
                        {
                            var emailSupplier = ci.clientIdentityValidation != null && ci.clientIdentityValidation.ToList().Exists(civ => civ.name != null && civ.name.Equals("EMAIL_SUPPLIER", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(civ.value)) ? ci.clientIdentityValidation.ToList().Where(civ => civ.name != null && civ.name.Equals("EMAIL_SUPPLIER", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value : emailSrvcSupplier;

                            if (emailSupplier.Equals("MX", StringComparison.OrdinalIgnoreCase))
                                mxMailboxesList.Add(ci.value);
                            else if (emailSupplier.Equals("CriticalPath", StringComparison.OrdinalIgnoreCase))
                                cpmsMailboxesList.Add(ci.value);
                            else
                                yahooMailboxesList.Add(ci.value);
                        }                            
                    }
                }
            }

            if (mxMailboxesList != null && mxMailboxesList.Count() > 0)
                supplierMailboxesList.Add("MXMailboxes",string.Join(",",mxMailboxesList.ToArray()));
            if (yahooMailboxesList != null && yahooMailboxesList.Count() > 0)
                supplierMailboxesList.Add("Yahoomailboxes", string.Join(",", yahooMailboxesList.ToArray()));
            if (cpmsMailboxesList != null && cpmsMailboxesList.Count() > 0)
                supplierMailboxesList.Add("CPMSMailboxes", string.Join(",", cpmsMailboxesList.ToArray()));

            return supplierMailboxesList;
        }

        #region commented code for premium email cease
        //public static List<string> GetListOfEmailsOnPremiumDanteCease(string identity, ref string dataStore, string emailName, string ceaseType)
        //{
        //    string acc_id = string.Empty;
        //    string email_Supplier = string.Empty;
        //    List<string> ListOfEmails = new List<string>();
        //    List<string> activeList = new List<string>();
        //    List<string> proposedList = new List<string>();
        //    Dictionary<string, string> activeDictionary = new Dictionary<string, string>();
        //    Dictionary<string, string> proposedDictionary = new Dictionary<string, string>();

        //    try
        //    {
        //        //active ----------> abc:abc,adsad   asd:asd,asdad,asda,asd ;
        //        //propsed -------> def:def,adsa,asdas ; xyz:xyz,asda ;
        //        // GSUPv1 assuming bak will cum in the request 
        //        // pick all the identities not in expired status
        //        // will recive many cline tptofiles 

        //        GetBatchProfileV1Res response1 = DnpWrapper.GetServiceUserProfilesV1ForDante(identity, bac_Identity_Domain);
        //        if (response1 != null && response1.clientProfileV1 != null && response1.clientProfileV1.Count() > 0)
        //        {
        //            bool isSIExist = false;
        //            bool isEmailExist = false;
        //            foreach (ClientProfileV1 profile in response1.clientProfileV1)
        //            {
        //                if (profile.clientServiceInstanceV1 != null && profile.clientServiceInstanceV1.ToList().Exists(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase) && csi.clientServiceInstanceStatus.value.ToLower() == "active"))
        //                {
        //                    isSIExist = true;
        //                    if (ceaseType.Equals("ispcease", StringComparison.OrdinalIgnoreCase))
        //                    {
        //                        if (profile != null && profile.client != null && profile.client.clientIdentity != null
        //                           && profile.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && ci.value.Equals(emailName, StringComparison.OrdinalIgnoreCase)))
        //                        {
        //                            isEmailExist = true;
        //                            var accountId = from si in profile.clientServiceInstanceV1
        //                                            where (si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase) && si.serviceIdentity.ToList().Exists(sIden => sIden.domain.Equals(bac_Identity_Domain, StringComparison.OrdinalIgnoreCase) && sIden.value.Equals(identity, StringComparison.OrdinalIgnoreCase)) && si.clientServiceInstanceCharacteristic.ToList().Exists(sic => sic.name.Equals("ACC_ID", StringComparison.OrdinalIgnoreCase)))
        //                                            select si.clientServiceInstanceCharacteristic.ToList().Where(sic => sic.name.Equals("ACC_ID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
        //                            if (accountId != null && accountId.FirstOrDefault() != null && !string.IsNullOrEmpty(accountId.ToList().FirstOrDefault().Trim()))
        //                            {
        //                                acc_id = accountId.ToList().FirstOrDefault().Trim();
        //                                if (profile.clientServiceInstanceV1.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase) && si.serviceIdentity.ToList().Exists(sIden => sIden.domain.Equals(bac_Identity_Domain, StringComparison.OrdinalIgnoreCase) && sIden.value.Equals(identity, StringComparison.OrdinalIgnoreCase)) && si.clientServiceInstanceCharacteristic.ToList().Exists(sic => sic.name.Equals("EMAIL_SUPPLIER", StringComparison.OrdinalIgnoreCase) && sic.value.Equals("CriticalPath", StringComparison.OrdinalIgnoreCase)))
        //                                    || !profile.clientServiceInstanceV1.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase) && si.serviceIdentity.ToList().Exists(sIden => sIden.domain.Equals(bac_Identity_Domain, StringComparison.OrdinalIgnoreCase) && sIden.value.Equals(identity, StringComparison.OrdinalIgnoreCase)) && si.clientServiceInstanceCharacteristic.ToList().Exists(sic => sic.name.Equals("EMAIL_SUPPLIER", StringComparison.OrdinalIgnoreCase))))
        //                                {
        //                                    dataStore = "D&P";
        //                                }
        //                                else
        //                                {
        //                                    dataStore = "CISP";
        //                                }
        //                            }
        //                            else
        //                            {
        //                                dataStore = "D&P";
        //                            }
        //                        }
        //                    }
        //                    else if (ceaseType.Equals("vascease", StringComparison.OrdinalIgnoreCase))
        //                    {
        //                        var accountId = from si in profile.clientServiceInstanceV1
        //                                        where (si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase) && si.serviceIdentity.ToList().Exists(sIden => sIden.domain.Equals(bac_Identity_Domain, StringComparison.OrdinalIgnoreCase) && sIden.value.Equals(identity, StringComparison.OrdinalIgnoreCase)) && si.clientServiceInstanceCharacteristic.ToList().Exists(sic => sic.name.Equals("ACC_ID", StringComparison.OrdinalIgnoreCase)))
        //                                        select si.clientServiceInstanceCharacteristic.ToList().Where(sic => sic.name.Equals("ACC_ID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
        //                        if (accountId != null && accountId.FirstOrDefault() != null && !string.IsNullOrEmpty(accountId.ToList().FirstOrDefault().Trim()))
        //                        {
        //                            acc_id = accountId.ToList().FirstOrDefault().Trim();
        //                            if (profile.clientServiceInstanceV1.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase) && si.serviceIdentity.ToList().Exists(sIden => sIden.domain.Equals(bac_Identity_Domain, StringComparison.OrdinalIgnoreCase) && sIden.value.Equals(identity, StringComparison.OrdinalIgnoreCase)) && si.clientServiceInstanceCharacteristic.ToList().Exists(sic => sic.name.Equals("EMAIL_SUPPLIER", StringComparison.OrdinalIgnoreCase) && sic.value.Equals("criticalpath", StringComparison.OrdinalIgnoreCase)))
        //                                || !profile.clientServiceInstanceV1.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase) && si.serviceIdentity.ToList().Exists(sIden => sIden.domain.Equals(bac_Identity_Domain, StringComparison.OrdinalIgnoreCase) && sIden.value.Equals(identity, StringComparison.OrdinalIgnoreCase)) && si.clientServiceInstanceCharacteristic.ToList().Exists(sic => sic.name.Equals("EMAIL_SUPPLIER", StringComparison.OrdinalIgnoreCase))))
        //                            {
        //                                dataStore = "D&P";
        //                            }
        //                            else
        //                            {
        //                                dataStore = "CISP";
        //                            }
        //                        }
        //                        else
        //                        {
        //                            dataStore = "D&P";
        //                        }
        //                    }
        //                    if (dataStore == "D&P")
        //                    {
        //                        var emailServiceInstance = (from si in profile.clientServiceInstanceV1
        //                                                    where si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase) && si.clientServiceInstanceStatus.value.ToLower() == "active" && si.serviceIdentity.ToList().Exists(sIden => sIden.domain.Equals(bac_Identity_Domain, StringComparison.OrdinalIgnoreCase) && sIden.value.Equals(identity, StringComparison.OrdinalIgnoreCase))
        //                                                    select si).FirstOrDefault();
        //                        List<string> activeEmailList = new List<string>();
        //                        List<string> proposedEmailList = new List<string>();
        //                        foreach (ClientServiceRole sr in emailServiceInstance.clientServiceRole.Where(sr => sr.id.ToLower() == "default"))
        //                        {
        //                            if (sr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && (ci.clientIdentityStatus.value.ToLower() != "proposed" || ci.clientIdentityStatus.value.ToLower() != "expired")))
        //                            {
        //                                activeEmailList.Add(sr.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && (ci.clientIdentityStatus.value.ToLower() != "proposed" || ci.clientIdentityStatus.value.ToLower() != "expired")).FirstOrDefault().value);
        //                            }
        //                            else if (sr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && ci.clientIdentityStatus.value.ToLower() == "proposed"))
        //                            {
        //                                proposedEmailList.Add(sr.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && ci.clientIdentityStatus.value.ToLower() == "proposed").FirstOrDefault().value);
        //                            }
        //                        }

        //                        if (activeEmailList != null && activeEmailList.Count() > 0)
        //                        {
        //                            activeDictionary.Add(activeEmailList.First(), string.Join(",", activeEmailList.ToArray()));
        //                        }
        //                        if (proposedEmailList != null && proposedEmailList.Count() > 0)
        //                        {
        //                            proposedDictionary.Add(proposedEmailList.First(), string.Join(",", proposedEmailList.ToArray()));
        //                        }
        //                    }
        //                }
        //            }
        //            if (ceaseType.Equals("ispcease"))
        //            {
        //                if (!isSIExist)
        //                    throw new DnpException("No ACTIVE Email Service is mapped for this billingaccountID in DnP ");
        //                else if (!isEmailExist)
        //                    throw new DnpException("EmailName : " + emailName + "is not linked to billingaccountID : " + identity + " in DnP ");
        //            }
        //        }
        //        else
        //        {
        //            throw new DnpException("No active profile found for this billingaccountID in DnP");
        //        }

        //        foreach (string key in activeDictionary.Keys)
        //        {
        //            string abc = key + ":" + activeDictionary[key];
        //            activeList.Add(abc);
        //        }

        //        foreach (string key in proposedDictionary.Keys)
        //        {
        //            string abc = key + ":" + proposedDictionary[key];
        //            proposedList.Add(abc);
        //        }
        //        if (activeList != null)
        //        {
        //            ListOfEmails.Add(string.Join(";", activeList.ToArray()));
        //        }
        //        else
        //        {
        //            ListOfEmails.Add(" ");
        //        }
        //        if (proposedList != null)
        //        {
        //            ListOfEmails.Add(string.Join(";", proposedList.ToArray()));
        //        }
        //        else
        //        {
        //            ListOfEmails.Add(" ");
        //        }
        //    }
        //    finally
        //    {
        //        if (proposedDictionary != null)
        //        {
        //            proposedDictionary.Clear();
        //        }
        //        if (activeDictionary != null)
        //        {
        //            activeDictionary.Clear();
        //        }
        //        activeList = null;
        //        proposedList = null;
        //    }

        //    return ListOfEmails;
        //}
        #endregion

        public static List<string> GetBBMTEmailList(string identity, ref string orderType, ref string emailSupplier)
        {
            List<string> ListOfEmails = new List<string>();
            List<string> activeList = new List<string>();
            List<string> proposedList = new List<string>();
            List<string> inactiveList = new List<string>();
            Dictionary<string, string> activeDictionary = new Dictionary<string, string>();
            Dictionary<string, string> proposedDictionary = new Dictionary<string, string>();
            Dictionary<string, string> inactiveDictionary = new Dictionary<string, string>();
            ClientServiceInstanceV1 emailServiceInstance = null;
            string vasProductID = string.Empty;
            try
            {
                //active ----------> abc:abc,adsad   asd:asd,asdad,asda,asd ;
                //propsed -------> def:def,adsa,asdas ; xyz:xyz,asda ;
                // GSUPv1 assuming bak will cum in the request 
                // pick all the identities not in expired status
                // will recive many cline tptofiles 

                GetBatchProfileV1Res response1 = DnpWrapper.GetServiceUserProfilesV1ForDante(identity, bac_Identity_Domain);
                if (response1 != null && response1.clientProfileV1 != null && response1.clientProfileV1.Count() > 0)
                {
                    foreach (ClientProfileV1 profile in response1.clientProfileV1)
                    {
                        if (profile.clientServiceInstanceV1 != null &&
                            profile.clientServiceInstanceV1.ToList().Exists(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase)
                                && csi.clientServiceInstanceStatus.value.ToLower() == "active" && csi.serviceIdentity.ToList().Exists(sIden => sIden.domain.Equals(bac_Identity_Domain, StringComparison.OrdinalIgnoreCase)
                                    && sIden.value.Equals(identity, StringComparison.OrdinalIgnoreCase) && csi.clientServiceInstanceStatus.value.ToLower() == "active")))
                        {
                            emailServiceInstance = (from si in profile.clientServiceInstanceV1
                                                    where si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase) && si.clientServiceInstanceStatus.value.ToLower() == "active" && si.serviceIdentity.ToList().Exists(sIden => sIden.domain.Equals(bac_Identity_Domain, StringComparison.OrdinalIgnoreCase) && sIden.value.Equals(identity, StringComparison.OrdinalIgnoreCase))
                                                    select si).FirstOrDefault();

                            if (string.IsNullOrEmpty(vasProductID))
                            {
                                if (emailServiceInstance.clientServiceInstanceCharacteristic != null)
                                {
                                    vasProductID = (from si in emailServiceInstance.clientServiceInstanceCharacteristic
                                                    where (si.name.Equals("vas_product_id", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(si.value))
                                                    select si.value).FirstOrDefault();
                                }
                                // Added for BTRCE-108554
                                GetEmailSupplier(true, ref emailSupplier, emailServiceInstance);
                                if (!string.IsNullOrEmpty(vasProductID))
                                {
                                    if (vasProductID == "VPEM000010")
                                    {
                                        orderType = "EmailCease";
                                    }
                                    else if (vasProductID == "VPEM000030" || vasProductID == "VPEM000060")
                                    {
                                        orderType = "PremiumEmailCease";
                                    }                                    
                                }
                                else
                                {
                                    throw new DnpException("vasProductID required for processing of BBMT cease is not present in DnP");
                                }
                            }
                            List<string> activeEmailList = new List<string>();
                            List<string> proposedEmailList = new List<string>();
                            List<string> inactiveEmailList = new List<string>();
                            // for BB Email cease
                            if (orderType.Equals("EmailCease", StringComparison.OrdinalIgnoreCase))
                            {

                                foreach (ClientServiceRole sr in emailServiceInstance.clientServiceRole.Where(sr => sr.id.ToLower() == "default"))
                                {
                                    if (sr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && (ci.clientIdentityStatus.value.ToLower() == "active" || ci.clientIdentityStatus.value.ToLower() == "transferring")))
                                    {
                                        activeEmailList.Add(sr.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && (ci.clientIdentityStatus.value.ToLower() == "active" || ci.clientIdentityStatus.value.ToLower() == "transferring")).FirstOrDefault().value);
                                    }
                                    else if (sr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && ci.clientIdentityStatus.value.ToLower() == "proposed"))
                                    {
                                        proposedEmailList.Add(sr.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && ci.clientIdentityStatus.value.ToLower() == "proposed").FirstOrDefault().value);
                                    }
                                    else if (sr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && (ci.clientIdentityStatus.value.ToLower() == "marked-inactive" || ci.clientIdentityStatus.value.ToLower() == "barred-abuse" || ci.clientIdentityStatus.value.ToLower() == "inactive")))
                                    {
                                        inactiveEmailList.Add(sr.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && (ci.clientIdentityStatus.value.ToLower() == "marked-inactive" || ci.clientIdentityStatus.value.ToLower() == "barred-abuse" || ci.clientIdentityStatus.value.ToLower() == "inactive")).FirstOrDefault().value);
                                    }
                                }

                                if (activeEmailList != null && activeEmailList.Count() > 0)
                                {
                                    activeDictionary.Add(activeEmailList.First(), string.Join(",", activeEmailList.ToArray()));
                                }
                                if (proposedEmailList != null && proposedEmailList.Count() > 0)
                                {
                                    proposedDictionary.Add(proposedEmailList.First(), string.Join(",", proposedEmailList.ToArray()));
                                }
                                if (inactiveEmailList != null && inactiveEmailList.Count() > 0)
                                {
                                    inactiveDictionary.Add(inactiveEmailList.First(), string.Join(",", inactiveEmailList.ToArray()));
                                }

                            }
                            // For Premium Email cease
                            else if (orderType.Equals("PremiumEmailCease", StringComparison.OrdinalIgnoreCase))
                            {
                                foreach (ClientServiceRole sr in emailServiceInstance.clientServiceRole.Where(sr => sr.id.ToLower() == "default"))
                                {
                                    if (sr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && (ConfigurationManager.AppSettings["PremiumEmailCeaseStatus"].Split(',').Contains(ci.clientIdentityStatus.value.ToLower()))))
                                    {
                                        //activeEmailList.Add(sr.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && (ci.clientIdentityStatus.value.ToLower() != "proposed" || ci.clientIdentityStatus.value.ToLower() != "expired")).FirstOrDefault().value);
                                        activeEmailList.Add(sr.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && (ConfigurationManager.AppSettings["PremiumEmailCeaseStatus"].Split(',').Contains(ci.clientIdentityStatus.value.ToLower()))).FirstOrDefault().value);
                                    }
                                    //else if (sr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && ci.clientIdentityStatus.value.ToLower() == "proposed"))
                                    else if (sr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && (ConfigurationManager.AppSettings["PremiumEmailRoleDeleteStatuses"].Split(',').Contains(ci.clientIdentityStatus.value.ToLower()))))
                                    {
                                        //proposedEmailList.Add(sr.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && ci.clientIdentityStatus.value.ToLower() == "proposed").FirstOrDefault().value);
                                        proposedEmailList.Add(sr.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && (ConfigurationManager.AppSettings["PremiumEmailRoleDeleteStatuses"].Split(',').Contains(ci.clientIdentityStatus.value.ToLower()))).FirstOrDefault().value);
                                    }
                                }

                                if (activeEmailList != null && activeEmailList.Count() > 0)
                                {
                                    activeDictionary.Add(activeEmailList.First(), string.Join(",", activeEmailList.ToArray()));
                                }
                                if (proposedEmailList != null && proposedEmailList.Count() > 0)
                                {
                                    proposedDictionary.Add(proposedEmailList.First(), string.Join(",", proposedEmailList.ToArray()));
                                }
                            }
                        }
                        else
                        {
                            throw new DnpException("No Active email service is mapped for given BillAccountNumber in DnP");
                        }
                    }
                }
                else
                {
                    throw new DnpException("No active profile found for this billingaccountID in DnP");
                }

                foreach (string key in activeDictionary.Keys)
                {
                    string abc = key + ":" + activeDictionary[key];
                    activeList.Add(abc);
                }

                foreach (string key in proposedDictionary.Keys)
                {
                    string abc = key + ":" + proposedDictionary[key];
                    proposedList.Add(abc);
                }
                foreach (string key in inactiveDictionary.Keys)
                {
                    string abc = key + ":" + inactiveDictionary[key];
                    inactiveList.Add(abc);
                }

                if (activeList != null)
                {
                    ListOfEmails.Add(string.Join(";", activeList.ToArray()));
                }
                else
                {
                    ListOfEmails.Add(" ");
                }
                if (proposedList != null)
                {
                    ListOfEmails.Add(string.Join(";", proposedList.ToArray()));
                }
                else
                {
                    ListOfEmails.Add(" ");
                }
                if (inactiveList != null)
                {
                    ListOfEmails.Add(string.Join(";", inactiveList.ToArray()));
                }
                else
                {
                    ListOfEmails.Add(" ");
                }

            }
            finally
            {
                if (proposedDictionary != null)
                {
                    proposedDictionary.Clear();
                }
                if (activeDictionary != null)
                {
                    activeDictionary.Clear();
                }
                if (inactiveDictionary != null)
                {
                    inactiveDictionary.Clear();
                }
                activeList = null;
                proposedList = null;
                inactiveList = null;
            }

            return ListOfEmails;
        }

        public static string GenerateEmailDataStore(ClientProfileV1 profile, string orderType, string identity, ref string emailSupplier)
        {
            string accountId = string.Empty;
            string email_Supplier = string.Empty;
            string emailDataStore = "CISP";
            ClientServiceInstanceCharacteristic[] srvcChars = null;
            try
            {
                // For BB email/Premium email cease check is made based on BillAccountNumber passed in request
                if (string.IsNullOrEmpty(orderType))
                {
                    srvcChars = (from si in profile.clientServiceInstanceV1
                                 where (si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase)
                                 && si.serviceIdentity.ToList().Exists(sIden => sIden.domain.Equals(bac_Identity_Domain, StringComparison.OrdinalIgnoreCase) && sIden.value.Equals(identity, StringComparison.OrdinalIgnoreCase))
                                 && si.clientServiceInstanceCharacteristic != null)
                                 select si.clientServiceInstanceCharacteristic).FirstOrDefault();
                }
                // for premium email provision check is made based on EmailName passed in request
                else
                {
                    srvcChars = (from si in profile.clientServiceInstanceV1
                                 where (si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase)
                                 && si.clientServiceRole.ToList().Exists(sr => sr.clientIdentity!=null&&sr.clientIdentity.ToList().Exists(sri => sri.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && sri.value.Equals(identity, StringComparison.OrdinalIgnoreCase)))
                                 && si.clientServiceInstanceCharacteristic != null)
                                 select si.clientServiceInstanceCharacteristic).FirstOrDefault();
                }
                if (srvcChars != null)
                {
                    srvcChars.ToList().ForEach(x =>
                    {
                        if (x.name.Equals("ACC_ID", StringComparison.OrdinalIgnoreCase))
                        {
                            accountId = x.value;
                        }
                        else if (x.name.Equals("EMAIL_SUPPLIER", StringComparison.OrdinalIgnoreCase))
                        {
                            email_Supplier = x.value;
                        }
                    }
                    );
                    emailSupplier = email_Supplier;
                    if (!String.IsNullOrEmpty(accountId.Trim()))
                    {
                        if (string.IsNullOrEmpty(email_Supplier.Trim()) || ConfigurationManager.AppSettings["DnPManagedEmailSupplier"].Split(',').Contains(email_Supplier, StringComparer.OrdinalIgnoreCase))
                        {
                            emailDataStore = "D&P";
                        }
                        else
                        {
                            emailDataStore = "CISP";
                        }
                    }
                    else
                    {
                        emailDataStore = "D&P";
                    }
                }

                if (Convert.ToBoolean(ConfigurationManager.AppSettings["IsdefaultemailsupplierMX"]))
                {
                    emailSupplier = "MX";
                }
            }
            finally
            {
                srvcChars = null;
            }
            #region old code
            //if (getprofileResponse1 != null && getprofileResponse1.clientProfileV1 != null && getprofileResponse1.clientProfileV1.clientServiceInstanceV1 != null)
            //{
            //    if (getprofileResponse1.clientProfileV1.clientServiceInstanceV1.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase)))
            //    {
            //        ClientServiceInstanceV1 serviceInstance = (from si in getprofileResponse1.clientProfileV1.clientServiceInstanceV1
            //                                                   where si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase) && si.clientServiceRole.ToList().Exists(sr => sr.clientIdentity.ToList().Exists(sri => sri.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && sri.value.Equals(identity, StringComparison.OrdinalIgnoreCase)))
            //                                                   select si).FirstOrDefault();
            //        if (serviceInstance != null && serviceInstance.clientServiceInstanceCharacteristic != null)
            //        {
            //            if (serviceInstance.clientServiceInstanceCharacteristic.ToList().Exists(sic => sic.name.Equals("ACC_ID", StringComparison.OrdinalIgnoreCase)))
            //            {
            //                acc_id = serviceInstance.clientServiceInstanceCharacteristic.ToList().Where(sic => sic.name.Equals("ACC_ID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
            //            }
            //            if (acc_id != null && !string.IsNullOrEmpty(acc_id))
            //            {
            //                if (serviceInstance.clientServiceInstanceCharacteristic.ToList().Exists(sic => sic.name.Equals("EMAIL_SUPPLIER", StringComparison.OrdinalIgnoreCase) && sic.value.Equals("criticalpath", StringComparison.OrdinalIgnoreCase)) || !serviceInstance.clientServiceInstanceCharacteristic.ToList().Exists(sic => sic.name.Equals("EMAIL_SUPPLIER", StringComparison.OrdinalIgnoreCase)))
            //                {
            //                    acc_id = string.Empty;
            //                    //emailDataStore = "D&P";
            //                }

            //            }
            //            if (String.IsNullOrEmpty(acc_id))
            //            {
            //                emailDataStore = "D&P";
            //            }
            //        }
            //    }
            //}
            #endregion

            return emailDataStore;
        }

        public static string GenerateEmailDataStore(ClientProfileV1 profile, string orderType, string identity, ref string emailSupplier, ref string vasproductid)
        {
            string accountId = string.Empty;
            string email_Supplier = string.Empty;
            string emailDataStore = "CISP"; string Vas_ProdId = string.Empty;
            ClientServiceInstanceCharacteristic[] srvcChars = null;
            try
            {
                // For BB email/Premium email cease check is made based on BillAccountNumber passed in request
                if (string.IsNullOrEmpty(orderType))
                {
                    srvcChars = (from si in profile.clientServiceInstanceV1
                                 where (si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase)
                                 && si.serviceIdentity.ToList().Exists(sIden => sIden.domain.Equals(bac_Identity_Domain, StringComparison.OrdinalIgnoreCase) && sIden.value.Equals(identity, StringComparison.OrdinalIgnoreCase))
                                 && si.clientServiceInstanceCharacteristic != null)
                                 select si.clientServiceInstanceCharacteristic).FirstOrDefault();
                }
                // for premium email provision check is made based on EmailName passed in request
                else
                {
                    srvcChars = (from si in profile.clientServiceInstanceV1
                                 where (si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase)
                                 && si.clientServiceRole.ToList().Exists(sr => sr.clientIdentity.ToList().Exists(sri => sri.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && sri.value.Equals(identity, StringComparison.OrdinalIgnoreCase)))
                                 && si.clientServiceInstanceCharacteristic != null)
                                 select si.clientServiceInstanceCharacteristic).FirstOrDefault();
                }
                if (srvcChars != null)
                {
                    srvcChars.ToList().ForEach(x =>
                    {
                        if (x.name.Equals("ACC_ID", StringComparison.OrdinalIgnoreCase))
                        {
                            accountId = x.value;
                        }
                        else if (x.name.Equals("EMAIL_SUPPLIER", StringComparison.OrdinalIgnoreCase))
                        {
                            email_Supplier = x.value;
                        }
                        else if (x.name.Equals("VAS_PRODUCT_ID", StringComparison.OrdinalIgnoreCase))
                        {
                            Vas_ProdId = x.value;
                        }
                    }
                    );
                    emailSupplier = email_Supplier;
                    vasproductid = Vas_ProdId;
                    if (!String.IsNullOrEmpty(accountId.Trim()))
                    {
                        if (string.IsNullOrEmpty(email_Supplier.Trim()) || ConfigurationManager.AppSettings["DnPManagedEmailSupplier"].Split(',').Contains(email_Supplier, StringComparer.OrdinalIgnoreCase))
                        {
                            emailDataStore = "D&P";
                        }
                        else
                        {
                            emailDataStore = "CISP";
                        }
                    }
                    else
                    {
                        emailDataStore = "D&P";
                    }
                }

                if (Convert.ToBoolean(ConfigurationManager.AppSettings["IsdefaultemailsupplierMX"]))
                {
                    emailSupplier = "MX";
                }
            }
            finally
            {
                srvcChars = null;
            }
            return emailDataStore;
        }

        public static List<string> GetListOfDefaultRoleIdentities(ClientServiceInstanceV1 servcInstance)
        {
            List<string> roleIdentitylist = new List<string>();
            List<string> RoleKeyList = new List<string>();
            Dictionary<string, string> roleKey_Dictionary = new Dictionary<string, string>();
            try
            {
                if (servcInstance.clientServiceRole != null && servcInstance.clientServiceRole.ToList().Exists(servRole => servRole.id.ToLower() == "default"))
                {
                    foreach (ClientServiceRole servRole in servcInstance.clientServiceRole.Where(servRole => servRole.id.ToLower() == "default"))
                    {
                        roleKey_Dictionary.Add(servRole.clientIdentity.FirstOrDefault().value, servRole.name);
                    }
                    foreach (string key in roleKey_Dictionary.Keys)
                    {
                        string abc = key + ";" + roleKey_Dictionary[key];
                        RoleKeyList.Add(abc);
                    }
                    if (RoleKeyList != null)
                    {
                        roleIdentitylist.Add(string.Join(",", RoleKeyList.ToArray()));
                    }
                    else
                    {
                        roleIdentitylist.Add(" ");
                    }
                }
            }
            finally
            {
                if (roleKey_Dictionary != null)
                {
                    roleKey_Dictionary.Clear();
                }
                RoleKeyList = null;
            }
            return roleIdentitylist;
        }

        public static void MapDanteProvisionRequest(MSEO.OrderItem orderItem, ref SAASPE.RoleInstance roleInstance, string serviceAddresingFrom, ref string emailSupplier)
        {
            bool isPrimaryEmailProvision = false;
            string reservationID = string.Empty;
            string SIBacNumber = string.Empty;
            string identity = string.Empty;
            string identityDomain = string.Empty;
            string bakId = string.Empty;
            string EmailName = string.Empty;
            bool activeEmailService = false;
            bool isExsitingBacID = false;
            bool isAHTClient = false;
            bool isReserved = false;
            bool serviceInstanceExists = false;
            string ISP_Service_Class = string.Empty;
            string btOneID_identity_domain = "BTCOM";
            string OWM_mailClass = "ENABLED";
            string yahoo_mailClass = "ACTIVE";
            bool isRetryOrder = false;
            string InputBTOneID = string.Empty; // btone ID from input XML
            string isAccountHolderAffiliateEmail = string.Empty;  // btone ID from input XML
            //ccp 79
            bool nayanProfile = false;
            string nayanID = string.Empty;

            ClientServiceInstanceV1 srvcInstance = null;
            ClientServiceInstanceV1 srvcInstanceAffliate = null;

            BT.SaaS.IspssAdapter.Dnp.ClientIdentity emailClientIdentity = null;
            GetBatchProfileV1Response batchprofileresponse = new GetBatchProfileV1Response();
            GetClientProfileV1Res getprofileResponse1 = new GetClientProfileV1Res();

            if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToLower().Equals("emailname")) && !string.IsNullOrEmpty(orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("emailname")).FirstOrDefault().Value))
            {
                EmailName = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("emailname")).FirstOrDefault().Value;
            }
            if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToLower().Equals("billaccountnumber")) && !string.IsNullOrEmpty(orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("billaccountnumber")).FirstOrDefault().Value))
            {
                bakId = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("billaccountnumber")).FirstOrDefault().Value;
            }
            //changes as per new req - affliate account holder email - activation
            if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToLower().Equals("btoneid")) && !string.IsNullOrEmpty(orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("btoneid")).FirstOrDefault().Value))
            {
                InputBTOneID = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("btoneid")).FirstOrDefault().Value;
            }
            //ISP leg || BBMT || First email creation from BTEmailManagementPage || First email creation for an affliate
            if (serviceAddresingFrom.Contains(Settings1.Default.MQstring) || serviceAddresingFrom.Equals(ConfigurationManager.AppSettings["BBMTServiceAddress"]) || (!string.IsNullOrEmpty(orderItem.Action.Reason)
                               && orderItem.Action.Reason.Equals("First_email", StringComparison.OrdinalIgnoreCase)))
            {
                isPrimaryEmailProvision = true;
                identity = bakId;
                identityDomain = bac_Identity_Domain;
            }
            else if (!string.IsNullOrEmpty(orderItem.Action.Reason) && orderItem.Action.Reason.Equals("AffiliateClaim", StringComparison.OrdinalIgnoreCase))
            {
                identity = EmailName;
                identityDomain = email_identity_domain;
            }
            //secondary email creation|| propose email || first email creation for affiliate
            else if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToLower() == "btoneid" && (ic.Value != null)))
            {
                identity = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("btoneid")).FirstOrDefault().Value;
                identityDomain = btOneID_identity_domain;
            }
            if ((DnpWrapper.getBatchProfileV1(identity, identityDomain, EmailName, email_identity_domain, ref batchprofileresponse)))
            {
                if ((batchprofileresponse != null && batchprofileresponse.getBatchProfileV1Res != null && batchprofileresponse.getBatchProfileV1Res.clientProfileV1 != null))
                {
                    // Email profile details
                    if ((batchprofileresponse.getBatchProfileV1Res.clientProfileV1[1] != null))
                    {
                        if (!string.IsNullOrEmpty(orderItem.Action.Reason) && orderItem.Action.Reason.Equals("AffiliateClaim", StringComparison.OrdinalIgnoreCase))
                        {
                            //if (!roleInstance.Attributes.ToList().Exists(attr => attr.Name.Equals("SIBACNUMBER", StringComparison.OrdinalIgnoreCase)))
                            if ((batchprofileresponse.getBatchProfileV1Res.clientProfileV1[1].clientServiceInstanceV1 != null && batchprofileresponse.getBatchProfileV1Res.clientProfileV1[1].clientServiceInstanceV1.Count() > 0))
                            {
                                bool isBacExist = false;
                                for (int loop = 0; loop < batchprofileresponse.getBatchProfileV1Res.clientProfileV1[1].clientServiceInstanceV1.Length; loop++)
                                {
                                    var clientServiceInstanceV1 = batchprofileresponse.getBatchProfileV1Res.clientProfileV1[1].clientServiceInstanceV1[loop];
                                    if (clientServiceInstanceV1.clientServiceInstanceIdentifier.value == "BTIEMAIL:DEFAULT" && (!(string.IsNullOrEmpty(clientServiceInstanceV1.clientServiceInstanceIdentifier.value))))
                                    {
                                        for (int loopServiceRole = 0; loopServiceRole < clientServiceInstanceV1.clientServiceRole.Length; loopServiceRole++)
                                        {
                                            var vrServiceRole = clientServiceInstanceV1.clientServiceRole[loopServiceRole];
                                            if (vrServiceRole.id == "DEFAULT" && (!(string.IsNullOrEmpty(vrServiceRole.id))) && vrServiceRole.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BTIEMAILID", StringComparison.OrdinalIgnoreCase) && ci.value.Equals(EmailName)))
                                            {
                                                foreach (var serviceIdentity in clientServiceInstanceV1.serviceIdentity.ToList())
                                                {
                                                    if (serviceIdentity.domain == "VAS_BILLINGACCOUNT_ID" && (!string.IsNullOrEmpty(serviceIdentity.domain)))
                                                    {
                                                        bakId = serviceIdentity.value;
                                                        isBacExist = true;
                                                        break;
                                                    }
                                                }
                                            }                                            
                                        }
                                    }
                                }
                                if (!isBacExist)
                                {
                                    throw new DnpException("No BAC exists at the Service Identity level in DnP");
                                }

                            }
                        }
                        if ((batchprofileresponse.getBatchProfileV1Res.clientProfileV1[1].client.clientIdentity != null && batchprofileresponse.getBatchProfileV1Res.clientProfileV1[1].client.clientIdentity.Count() > 0))
                        {
                            if ((batchprofileresponse.getBatchProfileV1Res.clientProfileV1[1].client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && ci.value.Equals(EmailName, StringComparison.OrdinalIgnoreCase) && ConfigurationManager.AppSettings["BTIEmailIdentityStatus"].Split(',').Contains(ci.clientIdentityStatus.value.ToLower()))))
                            {
                                isReserved = true;
                            }
                            // for new/retry of primary email
                            else if (serviceAddresingFrom.Contains(Settings1.Default.MQstring) || serviceAddresingFrom.Equals(ConfigurationManager.AppSettings["BBMTServiceAddress"]))
                            {
                                if ((batchprofileresponse.getBatchProfileV1Res.clientProfileV1[1].client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && ci.value.Equals(EmailName, StringComparison.OrdinalIgnoreCase) && ci.clientIdentityStatus.value.ToLower().Equals("ACTIVE", StringComparison.OrdinalIgnoreCase))))
                                {
                                    throw new DnpException("Ignored Email Provision as Email identity is already ACTIVE in DnP");
                                }
                                else if ((batchprofileresponse.getBatchProfileV1Res.clientProfileV1[1].client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && ci.value.Equals(EmailName, StringComparison.OrdinalIgnoreCase) && ci.clientIdentityStatus.value.ToLower().Equals("PENDING", StringComparison.OrdinalIgnoreCase))))
                                {
                                    isRetryOrder = true;
                                    emailClientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                                    emailClientIdentity = batchprofileresponse.getBatchProfileV1Res.clientProfileV1[1].client.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && ci.value.Equals(EmailName, StringComparison.OrdinalIgnoreCase) && ci.clientIdentityStatus.value.ToLower().Equals("PENDING", StringComparison.OrdinalIgnoreCase)).FirstOrDefault<BT.SaaS.IspssAdapter.Dnp.ClientIdentity>();
                                    roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "ISSHOWUSERNEED", Value = "true" });
                                }
                            }
                            else
                            {
                                throw new DnpException("Ignored the request as Email identity already exist in DnP ");
                            }
                        }
                    }
                  
                    //Bac || BTOneID profile details
                    if (batchprofileresponse.getBatchProfileV1Res.clientProfileV1[0] != null)
                    {
                        //added in case of affliate propose scenario
                        srvcInstanceAffliate = GetEmailService(bakId, bac_Identity_Domain);

                        if (Convert.ToBoolean(ConfigurationManager.AppSettings["IsVasSupplierSwitchforEmailProvisioningEnabled"]))
                            emailSupplier = ConfigurationManager.AppSettings["VASEmailSupplierSwitch"];
                        else
                            GetEmailSupplier(true, ref emailSupplier, srvcInstanceAffliate);

                        if (batchprofileresponse.getBatchProfileV1Res.clientProfileV1[0].client.clientIdentity != null && batchprofileresponse.getBatchProfileV1Res.clientProfileV1[0].client.clientIdentity.Count() > 0)
                        {
                            if (batchprofileresponse.getBatchProfileV1Res.clientProfileV1[0].client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(bac_Identity_Domain, StringComparison.OrdinalIgnoreCase)
                                && ci.value.Equals(bakId, StringComparison.OrdinalIgnoreCase) &&
                                ci.clientIdentityValidation != null && ci.clientIdentityValidation.Count() > 0 && ci.clientIdentityValidation.ToList().Exists(civ => civ.name.Equals("ACCOUNTTRUSTMETHOD", StringComparison.OrdinalIgnoreCase)
                                    && ConfigurationManager.AppSettings["AccountTrustMethodStatus"].Split(',').Contains(civ.value.ToLower()))))
                            {
                                isAHTClient = true;

                                isExsitingBacID = true;
                            }
                            //ccp 79 - new condition
                            //for secondary mail box
                            else if(!(serviceAddresingFrom.Contains(Settings1.Default.MQstring) || serviceAddresingFrom.Equals(ConfigurationManager.AppSettings["BBMTServiceAddress"]) || (!string.IsNullOrEmpty(orderItem.Action.Reason)
                               && (orderItem.Action.Reason.Equals("First_email", StringComparison.OrdinalIgnoreCase) || orderItem.Action.Reason.Equals("AffiliateClaim", StringComparison.OrdinalIgnoreCase)))))
                            {
                                ClientServiceInstanceV1[] serviceInstances = null;
                                serviceInstances = DnpWrapper.getServiceInstanceV1(bakId, "VAS_BILLINGACCOUNT_ID", String.Empty);
                                if (serviceInstances != null && serviceInstances.Count() > 0)
                                {                                    
                                    if (serviceInstances.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals("MANAGE_ACCOUNT", StringComparison.OrdinalIgnoreCase) && si.clientServiceInstanceCharacteristic != null && si.clientServiceInstanceCharacteristic.ToList().Exists(cschar => cschar.name.Equals("BACORGANISATIONNAME", StringComparison.OrdinalIgnoreCase) && cschar.value.Equals("EE", StringComparison.OrdinalIgnoreCase))))
                                    {                         
                                        if (serviceInstances.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals("BTIEMAIL:DEFAULT", StringComparison.OrdinalIgnoreCase) && si.clientServiceRole != null && si.clientServiceRole.Count() > 1 && si.clientServiceRole.ToList().Exists(csr => csr.id.Equals("SERVICE_MANAGER", StringComparison.OrdinalIgnoreCase))))
                                        {
                                            nayanProfile = true;
                                            isAHTClient = true;
                                            if (batchprofileresponse.getBatchProfileV1Res.clientProfileV1[0].client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(bac_Identity_Domain, StringComparison.OrdinalIgnoreCase)))
                                            {
                                                isExsitingBacID = true;
                                            }

                                            var serviceRole = (from si in serviceInstances
                                                               where (si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase) && si.clientServiceRole != null && si.clientServiceRole.ToList().Exists(sr => sr.id.Equals("SERVICE_MANAGER", StringComparison.OrdinalIgnoreCase)))
                                                               select si.clientServiceRole).FirstOrDefault();

                                            if (serviceRole != null)
                                            {
                                                var clientidentitydetails = (from sr in serviceRole
                                                                             where (sr.id.Equals("SERVICE_MANAGER", StringComparison.OrdinalIgnoreCase))
                                                                             select sr.clientIdentity).FirstOrDefault();
                                                if (clientidentitydetails != null)
                                                {
                                                    if(clientidentitydetails.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BTCOM",StringComparison.OrdinalIgnoreCase)))
                                                    {
                                                        nayanID = clientidentitydetails.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                                    }
                                                    
                                                }
                                            }
                                        }
                                    }
                                    if (nayanProfile && !string.IsNullOrEmpty(nayanID))
                                    {
                                        if (roleInstance.Attributes.ToList().Exists(attr => attr.Name.Equals("BTONEID", StringComparison.OrdinalIgnoreCase)))
                                        {
                                            if (!roleInstance.Attributes.ToList().Exists(attr => attr.Name.Equals("BTONEID", StringComparison.OrdinalIgnoreCase) && attr.Value.Equals(nayanID, StringComparison.OrdinalIgnoreCase)))
                                                roleInstance.Attributes.ToList().Where(attr => attr.Name.Equals("BTONEID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value = nayanID;
                                        }
                                        else
                                        {
                                            SAASPE.Attribute nayanBTID = new BT.SaaS.Core.Shared.Entities.Attribute();
                                            nayanBTID.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                            nayanBTID.Name = "BTONEID";
                                            nayanBTID.Value = nayanID;
                                            roleInstance.Attributes.Add(nayanBTID);
                                        }
                                    }
                                }
                            }
                            
                            //changes as per new req - affliate account holder email - activation 1111
                            foreach (var ci in batchprofileresponse.getBatchProfileV1Res.clientProfileV1[0].client.clientIdentity)
                            {
                                if (ci.managedIdentifierDomain.value != null && ci.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase))
                                {
                                    if (InputBTOneID.Equals(ci.value, StringComparison.OrdinalIgnoreCase))
                                    {
                                        isAccountHolderAffiliateEmail = "true";
                                    }
                                }
                            }
                        }
                        if ((batchprofileresponse.getBatchProfileV1Res.clientProfileV1[0].clientServiceInstanceV1 != null && batchprofileresponse.getBatchProfileV1Res.clientProfileV1[0].clientServiceInstanceV1.Count() > 0))
                        {
                            if (batchprofileresponse.getBatchProfileV1Res.clientProfileV1[0].clientServiceInstanceV1.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase) && si.serviceIdentity.ToList().Exists(sIden => sIden.domain.Equals(bac_Identity_Domain, StringComparison.OrdinalIgnoreCase) && sIden.value.Equals(bakId, StringComparison.OrdinalIgnoreCase))))
                            {
                                SIBacNumber = bakId;
                                srvcInstance = batchprofileresponse.getBatchProfileV1Res.clientProfileV1[0].clientServiceInstanceV1.ToList().Where(si => si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase) && si.serviceIdentity != null && si.serviceIdentity.ToList().Exists(sIden => sIden.value.Equals(bakId, StringComparison.OrdinalIgnoreCase))).FirstOrDefault();

                                if (isPrimaryEmailProvision)
                                {
                                    activeEmailService = srvcInstance.clientServiceInstanceStatus.value.Equals("active", StringComparison.OrdinalIgnoreCase) ? true : false;

                                    if (activeEmailService)
                                    {
                                        throw new DnpException("Email Product is already mapped for this BillAccountNumber");
                                    }
                                }
                                else
                                {
                                    serviceInstanceExists = true;

                                    if (Convert.ToBoolean(ConfigurationManager.AppSettings["IsVasSupplierSwitchforEmailProvisioningEnabled"]))
                                        emailSupplier = ConfigurationManager.AppSettings["VASEmailSupplierSwitch"];
                                    else
                                        GetEmailSupplier(serviceInstanceExists, ref emailSupplier, srvcInstance);

                                    //emailSupplier = ConfigurationManager.AppSettings["VASEmailSupplierSwitch"];
                                }
                            }
                        }
                    }
                }
                else if (isPrimaryEmailProvision)
                {
                    //GS instead of BatchProfile to get email service
                    srvcInstance = GetEmailService(bakId, bac_Identity_Domain);
                    if (srvcInstance != null)
                    {
                        if (srvcInstance.clientServiceInstanceStatus.value.Equals("PENDING", StringComparison.OrdinalIgnoreCase)
                            || srvcInstance.clientServiceInstanceStatus.value.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase))
                        {
                            //productOrderItem.Header.Status = BT.SaaS.Core.Shared.Entities.DataStatusEnum.done;
                            throw new DnpException("Email Product is already mapped for this BillAccountNumber");
                        }
                    }
                }
            }

            // during isp leg provision VASPRODUCTID should be retrived from MDM
            if (isPrimaryEmailProvision)
            {
                if (serviceAddresingFrom.Contains(Settings1.Default.MQstring))
                {
                    //Added for BTR BTR-74854
                    if (ConfigurationManager.AppSettings["MigrationSwitch"].Equals("OFF", StringComparison.OrdinalIgnoreCase))
                    {
                        // for retry of  primary email (non AHT bac)
                        if (isRetryOrder && srvcInstance == null)
                        {
                            srvcInstance = GetEmailService(bakId, bac_Identity_Domain);
                        }
                        GetEmailSupplier(isRetryOrder, ref emailSupplier, srvcInstance);
                    }
                    SAASPE.Attribute isearlyactivation = new BT.SaaS.Core.Shared.Entities.Attribute();
                    isearlyactivation.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                    isearlyactivation.Name = "isearlyactivation";
                    isearlyactivation.Value = "TRUE";
                    roleInstance.Attributes.Add(isearlyactivation);
                }
                else if (serviceAddresingFrom.Equals(ConfigurationManager.AppSettings["BBMTServiceAddress"]))
                {
                    // for retry of  primary email (non AHT bac)
                    if (isRetryOrder && srvcInstance == null)
                    {
                        srvcInstance = GetEmailService(bakId, bac_Identity_Domain);
                    }
                    GetEmailSupplier(isRetryOrder, ref emailSupplier, srvcInstance);
                }
                // BTRCE-119359 && BTRCE-119368 
                // Get the email supplier for primary email provision based on switch values
                else if ((!string.IsNullOrEmpty(orderItem.Action.Reason)
                               && orderItem.Action.Reason.Equals("First_email", StringComparison.OrdinalIgnoreCase)))
                {
                    GetEmailSpplierForPrimaryEmail(ref emailSupplier, orderItem);
                }


                if (!roleInstance.Attributes.ToList().Exists(attr => attr.Name.Equals("VASPRODUCTID", StringComparison.OrdinalIgnoreCase)))
                {
                    SAASPE.Attribute VASProduct_ID = new BT.SaaS.Core.Shared.Entities.Attribute();
                    VASProduct_ID.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                    VASProduct_ID.Name = "VASPRODUCTID";
                    List<string> vasClassIDList = new List<string>();
                    if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("ISPServiceClass", StringComparison.OrdinalIgnoreCase)))
                    {
                        string[] TBB_ISPSC = ConfigurationManager.AppSettings["Email-Strategic_TBB_ISPSC"].Split(new char[] { ';' });
                        string[] INF_ISPSC = ConfigurationManager.AppSettings["Email-Strategic_INF_ISPSC"].Split(new char[] { ';' });
                        ISP_Service_Class = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("ispserviceclass")).FirstOrDefault().Value;

                        if (TBB_ISPSC.Contains(ISP_Service_Class))
                        {
                            vasClassIDList.Add(ConfigurationManager.AppSettings["Email-Strategic TBB_VASClass"]);
                        }
                        else if (INF_ISPSC.Contains(ISP_Service_Class))
                        {
                            vasClassIDList.Add(ConfigurationManager.AppSettings["Email-Strategic INF_VASClass"]);
                        }

                    }
                    List<ProductVasClass> vasDefinitionList = new List<ProductVasClass>();
                    vasDefinitionList = MdmWrapper.getSaaSVASDefs(vasClassIDList);
                    if (vasDefinitionList != null && vasDefinitionList.Count > 0)
                    {
                        roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "REASON", Value = "MailVASProvision" });
                        if (vasDefinitionList.ToList().Exists(vpn => vpn.VasProductFamily.Equals(ConfigurationManager.AppSettings["DanteEMailProdCode"], StringComparison.OrdinalIgnoreCase) && !vpn.VasProductName.Equals("Premium Email", StringComparison.OrdinalIgnoreCase)))
                        {
                            VASProduct_ID.Value = vasDefinitionList.ToList().Where(vpn => vpn.VasProductFamily.Equals(ConfigurationManager.AppSettings["DanteEMailProdCode"], StringComparison.OrdinalIgnoreCase) && !vpn.VasProductName.Equals("Premium Email", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().VasProductId;
                        }

                    }
                    roleInstance.Attributes.Add(VASProduct_ID);
                }
            }


            //Added for BTR BTR-74854
            SAASPE.Attribute mailClassAttr = new BT.SaaS.Core.Shared.Entities.Attribute();
            mailClassAttr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
            mailClassAttr.Name = "MAILCLASS";
            if (emailSupplier.Equals(OWM_Email_Supplier, StringComparison.OrdinalIgnoreCase))
            {
                mailClassAttr.Value = OWM_mailClass;
                //for retry of primary email creation
                if (isRetryOrder && emailClientIdentity != null)
                {
                    if (roleInstance.Attributes.ToList().Exists(roleInstAttrb => roleInstAttrb.Name.Equals("Password", StringComparison.OrdinalIgnoreCase)))
                    {
                        roleInstance.Attributes.ToList().Where(roleInstAttrb => roleInstAttrb.Name.Equals("Password", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value = emailClientIdentity.clientCredential[0].credentialValue;
                    }
                    else
                    {
                        roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "Password", Value = emailClientIdentity.clientCredential[0].credentialValue });
                    }
                    roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "PWDHASHSALT", Value = emailClientIdentity.clientIdentityValidation.ToList().Where(civ => civ.name.Equals("SALT_VALUE")).FirstOrDefault().value });
                }
            }
            else
                mailClassAttr.Value = yahoo_mailClass;

            roleInstance.Attributes.Add(mailClassAttr);
            //ignore this addition of SIBACNUMBER in the Afflicate Claim Journey this already has been added in the above section
            if (!roleInstance.Attributes.ToList().Exists(attr => attr.Name.Equals("SIBACNUMBER", StringComparison.OrdinalIgnoreCase)))
            {
                SAASPE.Attribute siBacNumber = new BT.SaaS.Core.Shared.Entities.Attribute();
                siBacNumber.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                siBacNumber.Name = "SIBACNUMBER";
                if (!string.IsNullOrEmpty(SIBacNumber))
                    siBacNumber.Value = SIBacNumber.ToString();
                else
                    siBacNumber.Value = bakId;
                roleInstance.Attributes.Add(siBacNumber);
            }

            SAASPE.Attribute isserviceInstanceExists = new BT.SaaS.Core.Shared.Entities.Attribute();
            isserviceInstanceExists.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
            isserviceInstanceExists.Name = "serviceInstanceExists";
            isserviceInstanceExists.Value = serviceInstanceExists.ToString();
            roleInstance.Attributes.Add(isserviceInstanceExists);

            SAASPE.Attribute is_reserved = new BT.SaaS.Core.Shared.Entities.Attribute();
            is_reserved.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
            is_reserved.Name = "ISRESERVED";
            is_reserved.Value = isReserved.ToString();
            roleInstance.Attributes.Add(is_reserved);

            SAASPE.Attribute is_AHTClient = new BT.SaaS.Core.Shared.Entities.Attribute();
            is_AHTClient.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
            is_AHTClient.Name = "ISAHTCLIENT";
            is_AHTClient.Value = isAHTClient.ToString();
            roleInstance.Attributes.Add(is_AHTClient);

            SAASPE.Attribute is_OFSProvide = new BT.SaaS.Core.Shared.Entities.Attribute();
            is_OFSProvide.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
            is_OFSProvide.Name = "isofsprovide";
            is_OFSProvide.Value = isPrimaryEmailProvision.ToString();
            roleInstance.Attributes.Add(is_OFSProvide);

            SAASPE.Attribute isExistingAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
            isExistingAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
            isExistingAttribute.Name = "ISEXISTINGACCOUNT";
            isExistingAttribute.Value = isExsitingBacID.ToString();
            roleInstance.Attributes.Add(isExistingAttribute);

            if (!String.IsNullOrEmpty(orderItem.Action.Reason))
            {
                SAASPE.Attribute reasonAttirbute = new BT.SaaS.Core.Shared.Entities.Attribute();
                reasonAttirbute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                reasonAttirbute.Name = "REASON";
                reasonAttirbute.Value = orderItem.Action.Reason.ToString();
                roleInstance.Attributes.Add(reasonAttirbute);
            }
            //changes as per new req - affliate account holder email - activation 1111
            if (isAccountHolderAffiliateEmail.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                SAASPE.Attribute reasonAttirbute = new BT.SaaS.Core.Shared.Entities.Attribute();
                reasonAttirbute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                reasonAttirbute.Name = "isAccountHolderAffiliateEmail";
                reasonAttirbute.Value = "true";
                roleInstance.Attributes.Add(reasonAttirbute);
            }
        }
        //Added for BTR BTR-74854
        public static void GetEmailSupplier(bool isEmailExists, ref string emailSupplier, ClientServiceInstanceV1 serviceInstance)
        {
            // for new provision pick the value from switch
            if (!isEmailExists)
            {
                emailSupplier = ConfigurationManager.AppSettings["VASEmailSupplierSwitch"];
            }
            //if service already exists pick the value from service instance characteristic
            else
            {
                if (Convert.ToBoolean(ConfigurationManager.AppSettings["IsdefaultemailsupplierMX"]))
                {
                    emailSupplier = "MX";
                }
                else
                {
                    if (serviceInstance != null && serviceInstance.clientServiceInstanceCharacteristic != null
                    && serviceInstance.clientServiceInstanceCharacteristic.ToList().Exists(sic => sic.name.Equals("email_supplier", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(sic.value)))
                    {
                        var emailSupp = from si in serviceInstance.clientServiceInstanceCharacteristic
                                        where si.name.Equals("email_supplier", StringComparison.OrdinalIgnoreCase)
                                        select si.value;
                        emailSupplier = emailSupp.FirstOrDefault();
                    }
                }
            }
        }

        public static void GetEmailSupplier(ref string emailSupplier, ClientIdentity clientIdentity)
        {
            if (clientIdentity != null && clientIdentity.clientIdentityValidation != null
               && clientIdentity.clientIdentityValidation.ToList().Exists(sic => sic.name.Equals("email_supplier", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(sic.value)))
            {
                var emailSupp = from civ in clientIdentity.clientIdentityValidation
                                where civ.name.Equals("email_supplier", StringComparison.OrdinalIgnoreCase)
                                select civ.value;
                emailSupplier = emailSupp.FirstOrDefault();
            }
            else if (Convert.ToBoolean(ConfigurationManager.AppSettings["IsdefaultemailsupplierMX"]))
            {
                emailSupplier = "MX";
            }
        }

        public static void GetEmailSpplierForPrimaryEmail(ref string emailSupplier, MSEO.OrderItem orderItem)
        {
            if (ConfigurationManager.AppSettings["VASMigrationSwitch"].Equals("OFF", StringComparison.OrdinalIgnoreCase))
            {
                if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToLower().Equals("emailsupplier", StringComparison.OrdinalIgnoreCase)) && !string.IsNullOrEmpty(orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("emailsupplier", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value))
                    emailSupplier = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("emailsupplier", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                else
                    emailSupplier = ConfigurationManager.AppSettings["VASEmailSupplierSwitch"];

            }
            else
            {
                if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToLower().Equals("emailsupplier", StringComparison.OrdinalIgnoreCase)))
                    emailSupplier = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("emailsupplier", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
            }
        }

        public static ClientServiceInstanceV1 GetEmailService(string backID, string bac_identity_domain)
        {

            ClientServiceInstanceV1[] serviceInstances = null;
            ClientServiceInstanceV1 srvcInst = null;
            try
            {
                serviceInstances = DnpWrapper.getServiceInstanceV1(backID, bac_identity_domain, string.Empty);

                if (serviceInstances != null)
                {
                    if (serviceInstances.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase)))
                    {
                        srvcInst = serviceInstances.ToList().Where(si => si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                    }
                }
            }
            finally
            {
                serviceInstances = null;
            }
            return srvcInst;

        }
        public static void MapBBMTCeaseRequest(MSEO.OrderItem orderItem, ref SAASPE.RoleInstance roleInstance, ref string emailSupplier)
        {
            List<string> ListOfEmails = null;
            string orderType = string.Empty;
            string BakId = string.Empty;

            if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToLower().Equals("billaccountnumber")) && !string.IsNullOrEmpty(orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("billaccountnumber")).FirstOrDefault().Value))
            {
                BakId = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("billaccountnumber")).FirstOrDefault().Value;
            }

            ListOfEmails = GetBBMTEmailList(BakId, ref orderType, ref emailSupplier);

            SAASPE.Attribute reasonAttirbute = new BT.SaaS.Core.Shared.Entities.Attribute();
            reasonAttirbute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
            reasonAttirbute.Name = "REASON";
            reasonAttirbute.Value = orderType.ToString();
            roleInstance.Attributes.Add(reasonAttirbute);


            if (ListOfEmails[0] != null)
            {
                SAASPE.Attribute emails = new BT.SaaS.Core.Shared.Entities.Attribute();
                emails.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                emails.Name = "ListOfEmails";
                emails.Value = ListOfEmails[0];
                roleInstance.Attributes.Add(emails);
            }
            if (ListOfEmails[1] != null)
            {
                SAASPE.Attribute email = new BT.SaaS.Core.Shared.Entities.Attribute();
                email.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                email.Name = "ProposedEmailList";
                email.Value = ListOfEmails[1];
                roleInstance.Attributes.Add(email);
            }
            if (ListOfEmails[2] != null)
            {
                SAASPE.Attribute email = new BT.SaaS.Core.Shared.Entities.Attribute();
                email.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                email.Name = "InactiveEmailList";
                email.Value = ListOfEmails[2];
                roleInstance.Attributes.Add(email);
            }

            SAASPE.Attribute siBacNumber = new BT.SaaS.Core.Shared.Entities.Attribute();
            siBacNumber.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
            siBacNumber.Name = "SIBACNUMBER";
            siBacNumber.Value = BakId;
            roleInstance.Attributes.Add(siBacNumber);
        }

        public static void MapISPEmailCeaseRequest(MSEO.OrderItem orderItem, ref SAASPE.RoleInstance roleInstance, ref string emailSupplier)
        {

            string emailDataStore = "D&P";
            string EmailName = string.Empty;
            string bakId = string.Empty;
            string orderType = string.Empty;
            string BACceaseResult = string.Empty; string vasProdId = string.Empty;           

            if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToLower().Equals("emailname")) && !string.IsNullOrEmpty(orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("emailname")).FirstOrDefault().Value))
            {
                EmailName = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("emailname")).FirstOrDefault().Value;
            }
            if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToLower().Equals("billaccountnumber")) && !string.IsNullOrEmpty(orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("billaccountnumber")).FirstOrDefault().Value))
            {
                bakId = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("billaccountnumber")).FirstOrDefault().Value;
            }            
            //For Premium Email           
            if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(inst => inst.Name.ToLower().Equals("ispserviceclass") && inst.Value.ToUpper().Equals("ISP_SC_PM")) ||
         orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(inst => inst.Name.ToLower().Equals("ispserviceclass") && inst.Value.ToUpper().Equals("ISP_SC_BM")))
            {
                orderType = "PremiumEmailCease";

                // BTR-106921(CCP53) here we are checking if bac has anyother service except emails service and based on that we doing BAC cease

                ClientServiceInstanceV1[] gsiResponse = DnpWrapper.getServiceInstanceV1(bakId, "VAS_BILLINGACCOUNT_ID", string.Empty);
                int gsiCeasedServiceCount = 0;
                //BTR-106921(CCP53) if bac contains any services then we check for service states otherwise simple call baccease.
                if (gsiResponse != null && gsiResponse.Count() > 0)
                {
                    foreach (ClientServiceInstanceV1 siResponse in gsiResponse)
                    {
                        if (siResponse.clientServiceInstanceStatus.value != "ACTIVE")
                            gsiCeasedServiceCount++;
                        else if (siResponse.clientServiceInstanceIdentifier.value.Equals("MANAGE_ACCOUNT", StringComparison.OrdinalIgnoreCase))
                            gsiCeasedServiceCount++;
                    }
                    if (gsiCeasedServiceCount == gsiResponse.Count())
                    {
                        BACceaseResult = pmBACCease(bakId);
                    }
                }
                else
                {
                    throw new DnpException("Ignored as BAC doesn't have any services");
                }                    

                // Here Adding attribute of baccease method result to call the order to PE or not
                SAASPE.Attribute bacCeaseResultAttirbute = new BT.SaaS.Core.Shared.Entities.Attribute();
                bacCeaseResultAttirbute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                bacCeaseResultAttirbute.Name = "BACCeaseResult";
                bacCeaseResultAttirbute.Value = BACceaseResult.ToString();
                roleInstance.Attributes.Add(bacCeaseResultAttirbute);                
            }
            // For BB Email
            else if (ConfigurationManager.AppSettings["EmailScodes"].Split(',').Contains(orderItem.Specification[0].Identifier.Value1))      
            {
                orderType = "EmailCease";

            }
            SAASPE.Attribute reasonAttirbute = new BT.SaaS.Core.Shared.Entities.Attribute();
            reasonAttirbute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
            reasonAttirbute.Name = "REASON";
            reasonAttirbute.Value = orderType.ToString();
            roleInstance.Attributes.Add(reasonAttirbute);

            // BTR-106921(CCP53) if BAC is not ceased then we need list of emails 
            if (string.IsNullOrEmpty(BACceaseResult))
            {
                Dictionary<string, string> supplierList = new Dictionary<string, string>();
                List<string> ListOfEmails = DanteRequestProcessor.GetListOfEmailsOnCease(bakId, EmailName, "ispcease", orderType, ref emailDataStore, ref emailSupplier, ref vasProdId,ref supplierList);
               
                if (ListOfEmails[0] != null)
                {
                    SAASPE.Attribute emails = new BT.SaaS.Core.Shared.Entities.Attribute();
                    emails.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                    emails.Name = "ListOfEmails";
                    emails.Value = ListOfEmails[0];
                    roleInstance.Attributes.Add(emails);
                }
                if (ListOfEmails[1] != null)
                {
                    SAASPE.Attribute email = new BT.SaaS.Core.Shared.Entities.Attribute();
                    email.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                    email.Name = "ProposedEmailList";
                    email.Value = ListOfEmails[1];
                    roleInstance.Attributes.Add(email);
                }
                if (ListOfEmails[2] != null)
                {
                    SAASPE.Attribute ianctive_email = new BT.SaaS.Core.Shared.Entities.Attribute();
                    ianctive_email.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                    ianctive_email.Name = "InactiveEmailList";
                    ianctive_email.Value = ListOfEmails[2];
                    roleInstance.Attributes.Add(ianctive_email);
                }
                if (ListOfEmails[4] != null)
                {
                    roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "AffiliateEmailList", Value = ListOfEmails[4] });
                }
                if (supplierList != null && supplierList.Count() > 0)
                {
                    if (supplierList.ContainsKey("MXMailboxes") && !string.IsNullOrEmpty(supplierList["MXMailboxes"]))
                        roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "MXMailboxes", Value = supplierList["MXMailboxes"] });
                    if (supplierList.ContainsKey("Yahoomailboxes") && !string.IsNullOrEmpty(supplierList["Yahoomailboxes"]))
                        roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "Yahoomailboxes", Value = supplierList["Yahoomailboxes"] });
                    if (supplierList.ContainsKey("CPMSMailboxes") && !string.IsNullOrEmpty(supplierList["CPMSMailboxes"]))
                        roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "CPMSMailboxes", Value = supplierList["CPMSMailboxes"] });
                }
            }
            if (!roleInstance.Attributes.ToList().Exists(attr => attr.Name.Equals("VASPRODUCTID", StringComparison.OrdinalIgnoreCase)))
            {
                SAASPE.Attribute ceaseReasonAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                ceaseReasonAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                ceaseReasonAttribute.Name = "VASPRODUCTID";
                ceaseReasonAttribute.Value = string.IsNullOrEmpty(vasProdId) ? "VPEM000010" : vasProdId;
                roleInstance.Attributes.Add(ceaseReasonAttribute);
            }

            if (!roleInstance.Attributes.ToList().Exists(attr => attr.Name.Equals("CEASE_REASON", StringComparison.OrdinalIgnoreCase)))
            {
                SAASPE.Attribute ceaseReasonAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                ceaseReasonAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                ceaseReasonAttribute.Name = "CEASE_REASON";
                ceaseReasonAttribute.Value = orderItem.Action.Reason.ToString();
                roleInstance.Attributes.Add(ceaseReasonAttribute);
            }

            SAASPE.Attribute siBacNumber = new BT.SaaS.Core.Shared.Entities.Attribute();
            siBacNumber.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
            siBacNumber.Name = "SIBACNUMBER";
            siBacNumber.Value = bakId;
            roleInstance.Attributes.Add(siBacNumber);
        }
        public static void MapNayanEmailCeaseRequest(MSEO.OrderItem orderItem, ref SAASPE.RoleInstance roleInstance, ref string emailSupplier)
        {            
            string EmailName = string.Empty;
            string bakId = string.Empty;
            string orderType = string.Empty;
            string BACceaseResult = string.Empty; string vasProdId = string.Empty;

            if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToLower().Equals("billaccountnumber")) && !string.IsNullOrEmpty(orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("billaccountnumber")).FirstOrDefault().Value))
            {
                bakId = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("billaccountnumber")).FirstOrDefault().Value;
            }
            //For Premium Email           
            if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(inst => inst.Name.ToLower().Equals("emailtype") && inst.Value.ToUpper().Equals("PREMIUM")) ||
         orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(inst => inst.Name.ToLower().Equals("emailtype") && inst.Value.ToUpper().Equals("BASIC")))
            {
                orderType = "PremiumEmailCease";

                // BTR-106921(CCP53) here we are checking if bac has anyother service except emails service and based on that we doing BAC cease

                ClientServiceInstanceV1[] gsiResponse = DnpWrapper.getServiceInstanceV1(bakId, "VAS_BILLINGACCOUNT_ID", string.Empty);
                int gsiCeasedServiceCount = 0;
                //BTR-106921(CCP53) if bac contains any services then we check for service states otherwise simple call baccease.
                if (gsiResponse != null && gsiResponse.Count() > 0)
                {
                    foreach (ClientServiceInstanceV1 siResponse in gsiResponse)
                    {
                        if (siResponse.clientServiceInstanceStatus.value != "ACTIVE")
                            gsiCeasedServiceCount++;
                        else if (siResponse.clientServiceInstanceIdentifier.value.Equals("MANAGE_ACCOUNT", StringComparison.OrdinalIgnoreCase))
                            gsiCeasedServiceCount++;
                    }
                    if (gsiCeasedServiceCount == gsiResponse.Count())
                    {
                        BACceaseResult = pmBACCease(bakId);
                    }
                }
                else
                {
                    throw new DnpException("Ignored as BAC doesn't have any services");
                }

                // Here Adding attribute of baccease method result to call the order to PE or not
                SAASPE.Attribute bacCeaseResultAttirbute = new BT.SaaS.Core.Shared.Entities.Attribute();
                bacCeaseResultAttirbute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                bacCeaseResultAttirbute.Name = "BACCeaseResult";
                bacCeaseResultAttirbute.Value = BACceaseResult.ToString();
                roleInstance.Attributes.Add(bacCeaseResultAttirbute);
            }
            // For BB Email
            else if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(inst => inst.Name.ToLower().Equals("emailtype") && inst.Value.ToUpper().Equals("STANDARD")))
            {
                orderType = "EmailCease";

            }
            SAASPE.Attribute reasonAttirbute = new BT.SaaS.Core.Shared.Entities.Attribute();
            reasonAttirbute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
            reasonAttirbute.Name = "REASON";
            reasonAttirbute.Value = orderType.ToString();
            roleInstance.Attributes.Add(reasonAttirbute);

            // BTR-106921(CCP53) if BAC is not ceased then we need list of emails 
            if (string.IsNullOrEmpty(BACceaseResult))
            {
                Dictionary<string, string> supplierList = new Dictionary<string, string>();
                List<string> ListOfEmails = DanteRequestProcessor.GetListOfEmailsOnNayanCease(bakId, "nayanemailcease", orderType, ref emailSupplier, ref vasProdId, ref supplierList);

                if (ListOfEmails[0] != null)
                {
                    SAASPE.Attribute emails = new BT.SaaS.Core.Shared.Entities.Attribute();
                    emails.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                    emails.Name = "ListOfEmails";
                    emails.Value = ListOfEmails[0];
                    roleInstance.Attributes.Add(emails);
                }
                if (ListOfEmails[1] != null)
                {
                    SAASPE.Attribute email = new BT.SaaS.Core.Shared.Entities.Attribute();
                    email.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                    email.Name = "ProposedEmailList";
                    email.Value = ListOfEmails[1];
                    roleInstance.Attributes.Add(email);
                }
                if (ListOfEmails[2] != null)
                {
                    SAASPE.Attribute ianctive_email = new BT.SaaS.Core.Shared.Entities.Attribute();
                    ianctive_email.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                    ianctive_email.Name = "InactiveEmailList";
                    ianctive_email.Value = ListOfEmails[2];
                    roleInstance.Attributes.Add(ianctive_email);
                }
                //if (ListOfEmails[4] != null)
                //{
                //    roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "AffiliateEmailList", Value = ListOfEmails[4] });
                //}
                if (supplierList != null && supplierList.Count() > 0)
                {
                    if (supplierList.ContainsKey("MXMailboxes") && !string.IsNullOrEmpty(supplierList["MXMailboxes"]))
                        roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "MXMailboxes", Value = supplierList["MXMailboxes"] });
                    if (supplierList.ContainsKey("Yahoomailboxes") && !string.IsNullOrEmpty(supplierList["Yahoomailboxes"]))
                        roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "Yahoomailboxes", Value = supplierList["Yahoomailboxes"] });
                    if (supplierList.ContainsKey("CPMSMailboxes") && !string.IsNullOrEmpty(supplierList["CPMSMailboxes"]))
                        roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "CPMSMailboxes", Value = supplierList["CPMSMailboxes"] });
                }
            }
            if (!roleInstance.Attributes.ToList().Exists(attr => attr.Name.Equals("VASPRODUCTID", StringComparison.OrdinalIgnoreCase)))
            {
                SAASPE.Attribute ceaseReasonAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                ceaseReasonAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                ceaseReasonAttribute.Name = "VASPRODUCTID";
                ceaseReasonAttribute.Value = string.IsNullOrEmpty(vasProdId) ? "VPEM000010" : vasProdId;
                roleInstance.Attributes.Add(ceaseReasonAttribute);
            }

            if (!roleInstance.Attributes.ToList().Exists(attr => attr.Name.Equals("CEASE_REASON", StringComparison.OrdinalIgnoreCase)))
            {
                SAASPE.Attribute ceaseReasonAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                ceaseReasonAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                ceaseReasonAttribute.Name = "CEASE_REASON";
                ceaseReasonAttribute.Value = orderItem.Action.Reason.ToString();
                roleInstance.Attributes.Add(ceaseReasonAttribute);
            }

            SAASPE.Attribute siBacNumber = new BT.SaaS.Core.Shared.Entities.Attribute();
            siBacNumber.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
            siBacNumber.Name = "SIBACNUMBER";
            siBacNumber.Value = bakId;
            roleInstance.Attributes.Add(siBacNumber);
        }
        public static bool IsPAYGCustomer(ClientProfileV1 profile, string identity)
        {
            bool isPAYGCustomer = false;
            ClientServiceInstanceCharacteristic[] srvcChars = null;
            string Vas_Product_Id = string.Empty;

            srvcChars = (from si in profile.clientServiceInstanceV1
                         where (si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase) && si.clientServiceRole != null
                         && si.clientServiceRole.ToList().Exists(sr => sr.clientIdentity != null && sr.clientIdentity.ToList().Exists(sri => sri.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && sri.value.Equals(identity, StringComparison.OrdinalIgnoreCase)))
                         && si.clientServiceInstanceCharacteristic != null)
                         select si.clientServiceInstanceCharacteristic).FirstOrDefault();

            if (srvcChars != null)
            {
                srvcChars.ToList().ForEach(x =>
                {
                    if (x.name.Equals("VAS_PRODUCT_ID", StringComparison.OrdinalIgnoreCase))
                    {
                        Vas_Product_Id = x.value;
                    }
                });
                if (ConfigurationManager.AppSettings["PaygProductId"].Split(',').Contains(Vas_Product_Id))
                    isPAYGCustomer = true;
            }

            //Check if delinked mailbox if not PayG
            if (!isPAYGCustomer)
            {
                if (profile.clientServiceInstanceV1 != null && profile.clientServiceInstanceV1.ToList().Count() > 0 && profile.clientServiceInstanceV1.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase) && si.clientServiceRole != null
                         && si.clientServiceRole.ToList().Exists(sr => sr.clientIdentity != null && sr.clientIdentity.ToList().Exists(sri => sri.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && sri.value.Equals(identity, StringComparison.OrdinalIgnoreCase)) && sr.clientServiceRoleStatus.value.Equals("Delinked", StringComparison.OrdinalIgnoreCase))))
                    isPAYGCustomer = true;
            }
            return isPAYGCustomer;
        }
        public static bool IsPendingReinstateOrder(ClientProfileV1 profile, string identity, ref string reason,ref bool isCeasedBBmailbox)
        {
            bool isPendingReinstateOrder = false;

            var idenChar = (from ci in profile.client.clientIdentity
                            where (ci.managedIdentifierDomain.value.Equals("BTIEMAILID", StringComparison.OrdinalIgnoreCase) && ci.value.Equals(identity,StringComparison.OrdinalIgnoreCase)) && ci.clientIdentityValidation != null
                            && ci.clientIdentityValidation.ToList().Exists(civ => civ.name.Equals("CEASE_REASON", StringComparison.OrdinalIgnoreCase)) && !string.IsNullOrEmpty(ci.clientIdentityValidation.ToList().Where(civ => civ.name.Equals("CEASE_REASON", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value)
                            select ci.clientIdentityValidation.ToList().Where(civ => civ.name.Equals("CEASE_REASON", StringComparison.OrdinalIgnoreCase)).FirstOrDefault());

            string idencharValue = idenChar.Any() ? idenChar.FirstOrDefault().value : string.Empty;

            if (!string.IsNullOrEmpty(idencharValue) && (idencharValue.Equals("awaiting reinstatement order fulfilment", StringComparison.OrdinalIgnoreCase) || idencharValue.Equals("awaiting basicemail reactivate order fulfilment", StringComparison.OrdinalIgnoreCase) || idencharValue.Equals("Basic mail Downgrade order inprocess", StringComparison.OrdinalIgnoreCase)))
            {
                isPendingReinstateOrder = true;
                //reason = idencharValue.Equals("awaiting reinstatement order fulfilment", StringComparison.OrdinalIgnoreCase) ? "reinstatebasic" : "basicemailreactivate";
                reason = idencharValue.Equals("awaiting reinstatement order fulfilment", StringComparison.OrdinalIgnoreCase) ? "reinstatebasic" : idencharValue.Equals("Basic mail Downgrade order inprocess", StringComparison.OrdinalIgnoreCase) ? "basicemaildowngrade" : "basicemailreactivate";
            }
            else if (!string.IsNullOrEmpty(idencharValue) && idencharValue.Equals("Cease BB mail Downgrade order inprocess", StringComparison.OrdinalIgnoreCase))
            {
                isCeasedBBmailbox = true;
            }

            return isPendingReinstateOrder;
        }

        public static string GetNewPrimaryEmailforMailboxMove(GetBatchProfileV1Res responsegsup, string[] listofEmails)
        {
            string newprimaryemailname = string.Empty;
            try
            {
                // getting new primary email name in case of primary email move based on first created date
                System.DateTime newcreateDate = System.DateTime.MaxValue;

                foreach (ClientProfileV1 clp in responsegsup.clientProfileV1)
                {
                    var defaulttroles = from si in clp.clientServiceInstanceV1
                                        where (si.clientServiceInstanceIdentifier.value.Equals("BTIEMAIL:DEFAULT", StringComparison.OrdinalIgnoreCase))
                                        select si.clientServiceRole.ToList().Where(sri => sri.id.Equals("DEFAULT")).ToList();

                    foreach (List<ClientServiceRole> csr in defaulttroles)
                    {
                        foreach (ClientServiceRole csri in csr)
                        {
                            System.DateTime createdate = System.DateTime.ParseExact(csri.createdDate, "ddMMyyyyHHmmss", System.Globalization.CultureInfo.InvariantCulture);

                            if (!listofEmails.Contains(csri.clientIdentity[0].value, StringComparer.OrdinalIgnoreCase))
                            {
                                if (createdate < newcreateDate)
                                {
                                    newcreateDate = createdate;
                                    newprimaryemailname = csri.clientIdentity[0].value;
                                }
                            }
                        }
                    }
                }
            }
            finally
            {

            }
            return newprimaryemailname;
        }

        //ccp 79 - Adding another parameter for nayan profile
        public static List<string> GetListOfMailboxestoMove(GetBatchProfileV1Res responsegsup, string[] listofEmails, string sourceBillingAccountID, string SourcePrimayemailId, bool isNayanprofile)
        {
            List<string> ListOfEmails = new List<string>();
            List<string> ListOfEmailsToMove = new List<string>();
            List<string> UnresolvedEmailsList = new List<string>();
            List<string> UnmergedEmailsList = new List<string>();
            List<string> AffiliateEmailsList = new List<string>();
            List<string> MarkedInactiveMailboxList = new List<string>();
            List<string> PremiumEmailDebtBarredList = new List<string>();

            List<string> ListOfActiveEmails = new List<string>();
            List<string> ListOfInActiveEmails = new List<string>();
            List<string> ListOfEXPIREDPWDEmails = new List<string>();
            List<string> ListOfCOMPROMISEDEmails = new List<string>();
            List<string> ListOfTRANSFERRINGEmails = new List<string>();
            string[] allowImportMailboxes= ConfigurationManager.AppSettings["ImportAllowedStatus"].Split(',');
            string[] activeImportMailboxes = ConfigurationManager.AppSettings["ImportActiveAllowedStatus"].Split(',');
            string[] inactiveImportMailboxes = ConfigurationManager.AppSettings["ImportInactiveAllowedStatus"].Split(',');

            int inputEmailCount = listofEmails.Count();
            try
            {
                foreach (ClientProfileV1 clp in responsegsup.clientProfileV1)
                {
                    // Check if Email service exists in the profile
                    if (clp.clientServiceInstanceV1.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(BTEMAIL_SERVICECODE_NAMEPACE) &&
                                   si.serviceIdentity.ToList().Exists(sri => sri.domain != null && sri.domain.Equals("BTIEMAILID") && sri.value.Equals(SourcePrimayemailId, StringComparison.OrdinalIgnoreCase))))
                    {
                        var emailSrvcInstance = clp.clientServiceInstanceV1.ToList().Where
                                     (si => si.clientServiceInstanceIdentifier.value.Equals(BTEMAIL_SERVICECODE_NAMEPACE) && si.clientServiceRole != null &&
                                      si.serviceIdentity.ToList().Exists(sri => sri.domain != null && sri.domain.Equals("BTIEMAILID") && sri.value.Equals(SourcePrimayemailId, StringComparison.OrdinalIgnoreCase)));

                        if (emailSrvcInstance.Any())
                        {
                            ClientServiceInstanceV1 srvcIntance = emailSrvcInstance.FirstOrDefault();

                            foreach (string EmailName in listofEmails)
                            {
                                if (srvcIntance != null && srvcIntance.clientServiceRole!=null && srvcIntance.clientServiceRole.Count() > 0 && srvcIntance.clientServiceRole.ToList().Exists(csr => csr.id.Equals("DEFAULT", StringComparison.OrdinalIgnoreCase))
                                    && srvcIntance.clientServiceRole.ToList().Exists(csr => csr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BTIEMAILID", StringComparison.OrdinalIgnoreCase) && ci.value.Equals(EmailName, StringComparison.OrdinalIgnoreCase)&& allowImportMailboxes.Contains(ci.clientIdentityStatus.value.ToUpper()))))
                                {
                                    //09/03/2019 Need to verify logic for PayG with E2E design.
                                    if (!(clp.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)))
                                        && (srvcIntance.clientServiceInstanceCharacteristic.ToList().Exists(siChar => siChar.name.Equals("VAS_PRODUCT_ID", StringComparison.OrdinalIgnoreCase) && (siChar.value.Equals("VPEM000020", StringComparison.OrdinalIgnoreCase) || siChar.value.Equals("VPEM000050", StringComparison.OrdinalIgnoreCase)))))
                                    {
                                        //PAYG profile
                                        foreach (ClientServiceRole serviceRole1 in srvcIntance.clientServiceRole)
                                        {
                                            if (serviceRole1.clientIdentity != null && serviceRole1.clientIdentity.Count() > 0 && serviceRole1.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BTIEMAILID") && ci.value.Equals(EmailName, StringComparison.OrdinalIgnoreCase) && allowImportMailboxes.Contains(ci.clientIdentityStatus.value.ToUpper())))
                                            {
                                                if (serviceRole1.clientServiceRoleStatus.value.Equals("UNRESOLVED", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    //Do not pick expired mailboxes
                                                    //UnresolvedEmailsList.Add(serviceRole1.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && (serviceRole1.clientServiceRoleStatus.value.ToLower() == "unresolved")).FirstOrDefault().value);
                                                    UnresolvedEmailsList.Add(EmailName);
                                                    listofEmails = listofEmails.Where(val => val != EmailName).ToArray();

                                                    if (clp.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && ci.value.Equals(EmailName, StringComparison.OrdinalIgnoreCase)))
                                                    {
                                                        var emailIdentity = clp.client.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && ci.value.Equals(EmailName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                                                        
                                                        if (CheckMailboxStatus(emailIdentity, EmailName, ref MarkedInactiveMailboxList, ref PremiumEmailDebtBarredList,ref ListOfActiveEmails,ref ListOfInActiveEmails))
                                                        {
                                                            UnresolvedEmailsList.Remove(EmailName);                                                            
                                                        }
                                                        //if (emailIdentity.clientIdentityStatus.value.Equals(MARKED_INACTIVE, StringComparison.OrdinalIgnoreCase))
                                                        //{
                                                        //    MarkedInactiveMailboxList.Add(EmailName);
                                                        //}
                                                        //else if (emailIdentity.clientIdentityStatus.value.Equals("BARRED-ABUSE", StringComparison.OrdinalIgnoreCase))
                                                        //{
                                                        //    if (emailIdentity.clientIdentityValidation.ToList().Exists(civ => civ.name.Equals("BARRED_REASON", StringComparison.OrdinalIgnoreCase) && civ.value.Equals("PM_DebtBarred", StringComparison.OrdinalIgnoreCase)))
                                                        //    {
                                                        //        PremiumEmailDebtBarredList.Add(EmailName);
                                                        //    }
                                                        //}

                                                    }
                                                    continue;
                                                }
                                                else
                                                {
                                                    ListOfEmailsToMove.Add(EmailName);
                                                    listofEmails = listofEmails.Where(val => val != EmailName).ToArray();
                                                    if (clp.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && ci.value.Equals(EmailName, StringComparison.OrdinalIgnoreCase)))
                                                    {
                                                        var emailIdentity = clp.client.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && ci.value.Equals(EmailName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

                                                        if (CheckMailboxStatus(emailIdentity, EmailName, ref MarkedInactiveMailboxList, ref PremiumEmailDebtBarredList, ref ListOfActiveEmails, ref ListOfInActiveEmails,true))
                                                        {
                                                            ListOfEmailsToMove.Remove(EmailName);
                                                        }
                                                        //if (emailIdentity.clientIdentityStatus.value.Equals(MARKED_INACTIVE, StringComparison.OrdinalIgnoreCase))
                                                        //{
                                                        //    MarkedInactiveMailboxList.Add(EmailName);
                                                        //}
                                                        //else if (emailIdentity.clientIdentityStatus.value.Equals("BARRED-ABUSE", StringComparison.OrdinalIgnoreCase))
                                                        //{
                                                        //    if (emailIdentity.clientIdentityValidation.ToList().Exists(civ => civ.name.Equals("BARRED_REASON", StringComparison.OrdinalIgnoreCase) && civ.value.Equals("PM_DebtBarred", StringComparison.OrdinalIgnoreCase)))
                                                        //    {
                                                        //        PremiumEmailDebtBarredList.Add(EmailName);
                                                        //    }
                                                        //}

                                                    }
                                                    continue;
                                                }
                                            }
                                        }
                                    }
                                    else if (!(clp.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase))))
                                    {
                                        //checking email box status for unresolved state for unmerged profile.
                                        foreach (ClientServiceRole serviceRole1 in srvcIntance.clientServiceRole)
                                        {
                                            if (serviceRole1.clientIdentity != null && serviceRole1.clientIdentity.Count() > 0 && serviceRole1.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BTIEMAILID") && ci.value.Equals(EmailName, StringComparison.OrdinalIgnoreCase) && allowImportMailboxes.Contains(ci.clientIdentityStatus.value.ToUpper())))
                                            {
                                                if (serviceRole1.clientServiceRoleStatus.value.Equals("UNRESOLVED", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    //Do not pick expired mailboxes
                                                    //UnresolvedEmailsList.Add(serviceRole1.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && (serviceRole1.clientServiceRoleStatus.value.ToLower() == "unresolved")).FirstOrDefault().value);
                                                    UnresolvedEmailsList.Add(EmailName);
                                                    listofEmails = listofEmails.Where(val => val != EmailName).ToArray();
                                                    if (clp.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && ci.value.Equals(EmailName, StringComparison.OrdinalIgnoreCase)))
                                                    {
                                                        var emailIdentity = clp.client.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && ci.value.Equals(EmailName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

                                                        if (CheckMailboxStatus(emailIdentity, EmailName, ref MarkedInactiveMailboxList, ref PremiumEmailDebtBarredList, ref ListOfActiveEmails, ref ListOfInActiveEmails))
                                                        {
                                                            UnresolvedEmailsList.Remove(EmailName);
                                                        }

                                                        //if (emailIdentity.clientIdentityStatus.value.Equals(MARKED_INACTIVE, StringComparison.OrdinalIgnoreCase))
                                                        //{
                                                        //    MarkedInactiveMailboxList.Add(EmailName);
                                                        //}
                                                        //else if (emailIdentity.clientIdentityStatus.value.Equals("BARRED-ABUSE", StringComparison.OrdinalIgnoreCase))
                                                        //{
                                                        //    if (emailIdentity.clientIdentityValidation.ToList().Exists(civ => civ.name.Equals("BARRED_REASON", StringComparison.OrdinalIgnoreCase) && civ.value.Equals("PM_DebtBarred", StringComparison.OrdinalIgnoreCase)))
                                                        //    {
                                                        //        PremiumEmailDebtBarredList.Add(EmailName);
                                                        //    }
                                                        //}
                                                    }
                                                    continue;
                                                }
                                                else
                                                {
                                                    UnmergedEmailsList.Add(EmailName);
                                                    listofEmails = listofEmails.Where(val => val != EmailName).ToArray();
                                                    if (clp.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && ci.value.Equals(EmailName, StringComparison.OrdinalIgnoreCase)))
                                                    {
                                                        var emailIdentity = clp.client.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && ci.value.Equals(EmailName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

                                                        if (CheckMailboxStatus(emailIdentity, EmailName, ref MarkedInactiveMailboxList, ref PremiumEmailDebtBarredList, ref ListOfActiveEmails, ref ListOfInActiveEmails))
                                                        {
                                                            UnmergedEmailsList.Remove(EmailName);
                                                        }
                                                    }

                                                }

                                            }
                                        }
                                    }
                                    //ccp 79
                                    else if (isNayanprofile && srvcIntance.clientServiceRole.ToList().Exists(csr => (csr.id.Equals("SERVICE_MANAGER", StringComparison.OrdinalIgnoreCase))))
                                    {
                                        if (srvcIntance.clientServiceRole.ToList().Exists(csr => (csr.clientIdentity != null && csr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BTIEMAILID", StringComparison.OrdinalIgnoreCase) && ci.value.Equals(EmailName, StringComparison.OrdinalIgnoreCase)))))
                                            ListOfEmailsToMove.Add(EmailName);
                                    }
                                    else if (!srvcIntance.clientServiceRole.ToList().Exists(csr => (csr.clientIdentity != null && csr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase) && ci.value.Equals(sourceBillingAccountID)))))
                                    {
                                        //Do not move affiliate mailboxes
                                        //if (serviceRole1.clientIdentity != null && serviceRole1.clientIdentity.Count() > 0 && serviceRole1.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BTIEMAILID") && ci.value.Equals(EmailName, StringComparison.OrdinalIgnoreCase) && allowImportMailboxes.Contains(ci.clientIdentityStatus.value.ToUpper())))
                                        AffiliateEmailsList.Add(EmailName);
                                        listofEmails = listofEmails.Where(val => val != EmailName).ToArray();
                                        if (inputEmailCount == 1)
                                        {
                                            AffiliateEmailsList=GetAffilicateMailList(srvcIntance.clientServiceRole, ref MarkedInactiveMailboxList, ref PremiumEmailDebtBarredList, ref ListOfActiveEmails, ref ListOfInActiveEmails);
                                        }
                                        else
                                        {
                                            if (clp.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && ci.value.Equals(EmailName, StringComparison.OrdinalIgnoreCase)))
                                            {
                                                var emailIdentity = clp.client.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && ci.value.Equals(EmailName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

                                                if (CheckMailboxStatus(emailIdentity, EmailName, ref MarkedInactiveMailboxList, ref PremiumEmailDebtBarredList, ref ListOfActiveEmails, ref ListOfInActiveEmails))
                                                {
                                                    AffiliateEmailsList.Remove(EmailName);
                                                }

                                                //if (emailIdentity.clientIdentityStatus.value.Equals(MARKED_INACTIVE, StringComparison.OrdinalIgnoreCase))
                                                //{
                                                //    MarkedInactiveMailboxList.Add(EmailName);
                                                //}
                                                //else if (emailIdentity.clientIdentityStatus.value.Equals("BARRED-ABUSE", StringComparison.OrdinalIgnoreCase))
                                                //{
                                                //    if (emailIdentity.clientIdentityValidation.ToList().Exists(civ => civ.name.Equals("BARRED_REASON", StringComparison.OrdinalIgnoreCase) && civ.value.Equals("PM_DebtBarred", StringComparison.OrdinalIgnoreCase)))
                                                //    {
                                                //        PremiumEmailDebtBarredList.Add(EmailName);
                                                //    }
                                                //}
                                            }
                                        }
                                        continue;
                                    }
                                    else
                                    {
                                        ListOfEmailsToMove.Add(EmailName);
                                        listofEmails = listofEmails.Where(val => val != EmailName).ToArray();
                                        if (clp.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && ci.value.Equals(EmailName, StringComparison.OrdinalIgnoreCase)))
                                        {
                                            var emailIdentity = clp.client.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase) && ci.value.Equals(EmailName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

                                            if (CheckMailboxStatus(emailIdentity, EmailName, ref MarkedInactiveMailboxList, ref PremiumEmailDebtBarredList, ref ListOfActiveEmails, ref ListOfInActiveEmails))
                                            {
                                                ListOfEmailsToMove.Remove(EmailName);
                                            }

                                            //if (emailIdentity.clientIdentityStatus.value.Equals(MARKED_INACTIVE, StringComparison.OrdinalIgnoreCase))
                                            //{
                                            //    MarkedInactiveMailboxList.Add(EmailName);
                                            //}
                                            //else if (emailIdentity.clientIdentityStatus.value.Equals("BARRED-ABUSE", StringComparison.OrdinalIgnoreCase))
                                            //{
                                            //    if (emailIdentity.clientIdentityValidation.ToList().Exists(civ => civ.name.Equals("BARRED_REASON", StringComparison.OrdinalIgnoreCase) && civ.value.Equals("PM_DebtBarred", StringComparison.OrdinalIgnoreCase)))
                                            //    {
                                            //        PremiumEmailDebtBarredList.Add(EmailName);
                                            //    }
                                            //}
                                        }
                                        continue;
                                    }
                                }
                            }
                        }
                    }
                }

                if (ListOfEmailsToMove != null && ListOfEmailsToMove.Count > 0)
                {
                    ListOfEmails.Add(string.Join(";", ListOfEmailsToMove.ToArray()));
                }
                else
                {
                    ListOfEmails.Add(string.Empty);
                }

                if (UnresolvedEmailsList != null && UnresolvedEmailsList.Count > 0)
                {
                    ListOfEmails.Add(string.Join(";", UnresolvedEmailsList.ToArray()));
                }
                else
                {
                    ListOfEmails.Add(string.Empty);
                }

                if (AffiliateEmailsList != null && AffiliateEmailsList.Count() > 0)
                {
                    ListOfEmails.Add(string.Join(";", AffiliateEmailsList.ToArray()));
                }
                else
                {
                    ListOfEmails.Add(string.Empty);
                }

                if (MarkedInactiveMailboxList != null && MarkedInactiveMailboxList.Count() > 0)
                {
                    ListOfEmails.Add(string.Join(";", MarkedInactiveMailboxList.ToArray()));
                }
                else
                {
                    ListOfEmails.Add(string.Empty);
                }

                if (PremiumEmailDebtBarredList != null && PremiumEmailDebtBarredList.Count() > 0)
                {
                    ListOfEmails.Add(string.Join(";", PremiumEmailDebtBarredList.ToArray()));
                }
                else
                {
                    ListOfEmails.Add(string.Empty);
                }
                if (ListOfActiveEmails != null && ListOfActiveEmails.Count() > 0)
                {
                    ListOfEmails.Add(string.Join(";", ListOfActiveEmails.ToArray()));
                }
                else
                {
                    ListOfEmails.Add(string.Empty);
                }
                if (ListOfInActiveEmails != null && ListOfInActiveEmails.Count() > 0)
                {
                    ListOfEmails.Add(string.Join(";", ListOfInActiveEmails.ToArray()));
                }
                else
                {
                    ListOfEmails.Add(string.Empty);
                }
                if (UnmergedEmailsList != null && UnmergedEmailsList.Count() > 0)
                {
                    ListOfEmails.Add(string.Join(";", UnmergedEmailsList.ToArray()));
                }
                else
                {
                    ListOfEmails.Add(string.Empty);
                }
            }
            finally
            {
                ListOfEmailsToMove = null;
                UnresolvedEmailsList = null;
                AffiliateEmailsList = null;
                PremiumEmailDebtBarredList = null;
                MarkedInactiveMailboxList = null;
            }

            return ListOfEmails;
        }
        public static List<string> GetAffilicateMailList(ClientServiceRole[] affilicateRolelist, ref List<string> MarkedInactiveMailboxList, ref List<string> PremiumEmailDebtBarredList, ref List<string> ListOfActiveEmails, ref List<string> ListOfInActiveEmails)
        {            
            List<string> AffiliateEmailsList = new List<string>();

            foreach (ClientServiceRole emailRole in affilicateRolelist)
            {
                if (emailRole != null && emailRole.clientIdentity != null & emailRole.clientIdentity.Count() > 0 && emailRole.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(email_identity_domain, StringComparison.OrdinalIgnoreCase)))
                {
                    if (!CheckMailboxStatus(emailRole.clientIdentity[0], emailRole.clientIdentity[0].value, ref MarkedInactiveMailboxList, ref PremiumEmailDebtBarredList, ref ListOfActiveEmails, ref ListOfInActiveEmails))
                        AffiliateEmailsList.Add(emailRole.clientIdentity[0].value);
                }
            }           

            return AffiliateEmailsList;
        }
        public static bool CheckMailboxStatus(ClientIdentity emailIdentity, string EmailName, ref List<string> MarkedInactiveMailboxList, ref List<string> PremiumEmailDebtBarredList, ref List<string> ListOfActiveEmails,ref List<string> ListOfInActiveEmails, bool isUnmeregedmailbox=false)
        {
            bool removemailfromlist = false;
            string[] allowImportMailboxes = null;
            if(isUnmeregedmailbox)
                allowImportMailboxes = ConfigurationManager.AppSettings["ImportUnmergedAllowedStatus"].Split(',');
            else
                allowImportMailboxes = ConfigurationManager.AppSettings["ImportAllowedStatus"].Split(',');
            string[] activeImportMailboxes = ConfigurationManager.AppSettings["ImportActiveAllowedStatus"].Split(',');
            string[] inactiveImportMailboxes = ConfigurationManager.AppSettings["ImportInactiveAllowedStatus"].Split(',');

            if (allowImportMailboxes.Contains(emailIdentity.clientIdentityStatus.value.ToUpper()))
            {
                if (emailIdentity.clientIdentityStatus.value.Equals(MARKED_INACTIVE, StringComparison.OrdinalIgnoreCase) || emailIdentity.clientIdentityStatus.value.Equals("BARRED-ABUSE", StringComparison.OrdinalIgnoreCase))
                {
                    if (emailIdentity.clientIdentityStatus.value.Equals(MARKED_INACTIVE, StringComparison.OrdinalIgnoreCase))
                    {
                        MarkedInactiveMailboxList.Add(EmailName);
                        ListOfActiveEmails.Add(EmailName);
                    }
                    else if (emailIdentity.clientIdentityStatus.value.Equals("BARRED-ABUSE", StringComparison.OrdinalIgnoreCase))
                    {
                        if (emailIdentity.clientIdentityValidation.ToList().Exists(civ => civ.name.Equals("BARRED_REASON", StringComparison.OrdinalIgnoreCase) && civ.value.Equals("PM_DebtBarred", StringComparison.OrdinalIgnoreCase)))
                        {
                            PremiumEmailDebtBarredList.Add(EmailName);
                            ListOfActiveEmails.Add(EmailName);
                        }
                        else
                            removemailfromlist = true;
                    }
                }
                else
                {
                    if (activeImportMailboxes.Contains(emailIdentity.clientIdentityStatus.value.ToUpper()))
                        ListOfActiveEmails.Add(EmailName);
                    if (inactiveImportMailboxes.Contains(emailIdentity.clientIdentityStatus.value.ToUpper()))
                        ListOfInActiveEmails.Add(EmailName);
                }
            }
            else
                removemailfromlist = true;

            return removemailfromlist;
        }

        public static bool GetMarkedandAbuseMails(ClientIdentity emailIdentity,string EmailName,ref List<string> MarkedInactiveMailboxList,ref List<string> PremiumEmailDebtBarredList, ref List<string> ListOfActiveEmails)
        {
            bool removemailfromlist=false;
            if (emailIdentity.clientIdentityStatus.value.Equals(MARKED_INACTIVE, StringComparison.OrdinalIgnoreCase))
            {
                MarkedInactiveMailboxList.Add(EmailName);
                ListOfActiveEmails.Add(EmailName);
            }
            else if (emailIdentity.clientIdentityStatus.value.Equals("BARRED-ABUSE", StringComparison.OrdinalIgnoreCase))
            {
                if (emailIdentity.clientIdentityValidation.ToList().Exists(civ => civ.name.Equals("BARRED_REASON", StringComparison.OrdinalIgnoreCase) && civ.value.Equals("PM_DebtBarred", StringComparison.OrdinalIgnoreCase)))
                {
                    PremiumEmailDebtBarredList.Add(EmailName);
                    ListOfActiveEmails.Add(EmailName);
                }
                else
                    removemailfromlist = true;
            }

            return removemailfromlist;
        }

        #region Premium BACCease
        public static string pmBACCease(string BACid)
        {
            string result = string.Empty;
            manageClientProfileV1Response1 profileResponse;
            manageClientProfileV1Request1 profileRequest = new manageClientProfileV1Request1();
            profileRequest.manageClientProfileV1Request = new ManageClientProfileV1Request();

            BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock headerBlock = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
            headerBlock.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
            headerBlock.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
            headerBlock.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();

            ManageClientProfileV1Req manageClientProfileV1Req1 = new ManageClientProfileV1Req();
            manageClientProfileV1Req1.clientProfileV1 = new ClientProfileV1();

            List<ClientIdentity> clientIdentityList = new List<ClientIdentity>();

            ClientIdentity clientIdentity = null;

            clientIdentity = new ClientIdentity();
            clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
            clientIdentity.managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;
            clientIdentity.clientIdentityStatus = new ClientIdentityStatus();
            clientIdentity.clientIdentityStatus.value = ACTIVE;
            clientIdentity.value = BACid;
            clientIdentity.action = ACTION_SEARCH;
            clientIdentityList.Add(clientIdentity);

            clientIdentity = new ClientIdentity();
            clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
            clientIdentity.managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;
            clientIdentity.value = BACid;
            clientIdentity.clientIdentityStatus = new ClientIdentityStatus();
            clientIdentity.clientIdentityStatus.value = ACTION_CEASED;
            clientIdentity.action = ACTION_UPDATE;
            clientIdentityList.Add(clientIdentity);

            manageClientProfileV1Req1.clientProfileV1.client = new Client();
            manageClientProfileV1Req1.clientProfileV1.client.action = ACTION_UPDATE;
            manageClientProfileV1Req1.clientProfileV1.client.clientIdentity = clientIdentityList.ToArray();
            profileRequest.manageClientProfileV1Request.standardHeader = headerBlock;

            profileRequest.manageClientProfileV1Request.manageClientProfileV1Req = manageClientProfileV1Req1;
            #region Call DnP

            profileResponse = DnpWrapper.manageClientProfileV1Thomas(profileRequest, "orderKey");

            if (profileResponse != null
                && profileResponse.manageClientProfileV1Response != null
                && profileResponse.manageClientProfileV1Response.standardHeader != null
                && profileResponse.manageClientProfileV1Response.standardHeader.serviceState != null
                && profileResponse.manageClientProfileV1Response.standardHeader.serviceState.stateCode != null
                && profileResponse.manageClientProfileV1Response.standardHeader.serviceState.stateCode == "0")
            {
                result = "success";
            }

            else
            {
                result = "BAC cease failed at DnP with error msg as " + profileResponse.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description;
            }
            #endregion
            return result;
        }
        #endregion

        public static bool isLastEmailmoved(string identity, string domain, string emailName, bool isPrimaryEmail, ref bool isAffiliateEmail, ref string newprimaryemail,string reason,ref int emailCount)
        {
            bool result = false;
            int assignedemails = 0;
            //string newprimaryemail=string.Empty;
            string[] email = new string[1];
            //email[0] = new string();
            email[0] = emailName;
            GetBatchProfileV1Res responsegsup = new GetBatchProfileV1Res();
            GetClientProfileV1Res getprofileResponse = null;
            
            responsegsup = DnpWrapper.GetServiceUserProfilesV1ForDante(identity, domain);            

            if (responsegsup != null && responsegsup.clientProfileV1 != null && responsegsup.clientProfileV1.Count() > 0)
            {
                foreach (ClientProfileV1 clp in responsegsup.clientProfileV1)
                {
                    //Todo : Move the if conditions to lamdba
                    if (clp.clientServiceInstanceV1 != null)
                    {
                        var defaulttroles = from si in clp.clientServiceInstanceV1
                                            where (si.clientServiceInstanceIdentifier.value.Equals("BTIEMAIL:DEFAULT", StringComparison.OrdinalIgnoreCase))
                                            select si.clientServiceRole.ToList().Where(sri => sri.id.Equals("DEFAULT")).ToList();
                        if (defaulttroles.Any())
                        {
                            assignedemails = assignedemails + defaulttroles.ToList().FirstOrDefault().Count();
                        }
                    }
                }
                foreach (ClientProfileV1 clp in responsegsup.clientProfileV1)
                {
                    //To check the email is affiliate or not.
                    if (clp.client != null && clp.client.clientIdentity != null && clp.client.clientIdentity.Count() > 0)
                    {
                        if (clp.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(BACID_IDENTIFER_NAMEPACE) && ci.value.Equals(identity)))
                        {
                            if (clp.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BTIEMAILID") && ci.value.Equals(emailName)))
                            {
                                isAffiliateEmail = false;
                            }
                            else
                                isAffiliateEmail = true;

                            break;
                        }

                    }

                }
                if (isPrimaryEmail)
                {
                    newprimaryemail = GetNewPrimaryEmailforMailboxMove(responsegsup, email);
                }

                if (assignedemails > 1)
                    result = false;
                else if (assignedemails == 1)
                {
                    result = true;
                }
                emailCount = assignedemails;
            }
            else if (reason.Equals("reinstatebasic", StringComparison.OrdinalIgnoreCase))
            {
                getprofileResponse = new GetClientProfileV1Res();
                getprofileResponse = DnpWrapper.GetClientProfileV1(emailName, email_identity_domain);

               //Todo : Move the if conditions to lamdba
                if (getprofileResponse != null && getprofileResponse.clientProfileV1 != null && getprofileResponse.clientProfileV1.clientServiceInstanceV1 != null)
                    {
                        var defaulttroles = from si in getprofileResponse.clientProfileV1.clientServiceInstanceV1
                                            where (si.clientServiceInstanceIdentifier.value.Equals("BTIEMAIL:DEFAULT", StringComparison.OrdinalIgnoreCase))
                                            select si.clientServiceRole.ToList().Where(sri => sri.id.Equals("DEFAULT")).ToList();
                        if (defaulttroles.Any())
                        {
                            assignedemails = assignedemails + defaulttroles.ToList().FirstOrDefault().Count();
                        }
                    }
                             
                if (isPrimaryEmail)
                {
                    newprimaryemail = GetNewPrimaryEmailforMailboxMove(responsegsup, email);
                }

                if (assignedemails > 1)
                    result = false;
                else if (assignedemails == 1)
                {
                    result = true;
                }
                emailCount = assignedemails;
            }
            else
            {
                throw new DnpException("BAC received in the request doesn't have email service in dnp profile.");
            }

            return result;
        }

        public static bool reactivateTargetBACcase(string SIBacNumber, string primaryemail)
        {
            bool islastemail = false;
            int assignedemails = 0;

            if (!string.IsNullOrEmpty(SIBacNumber))
            {
                GetBatchProfileV1Res responsegsup = new GetBatchProfileV1Res();
                responsegsup = DnpWrapper.GetServiceUserProfilesV1ForDante(SIBacNumber, BACID_IDENTIFER_NAMEPACE);

                if (responsegsup != null && responsegsup.clientProfileV1 != null && responsegsup.clientProfileV1.Count() > 0)
                {
                    foreach (ClientProfileV1 clp in responsegsup.clientProfileV1)
                    {
                        //Todo : Move the if conditions to lamdba
                        if (clp.clientServiceInstanceV1 != null)
                        {
                            var defaulttroles = from si in clp.clientServiceInstanceV1
                                                where (si.clientServiceInstanceIdentifier.value.Equals("BTIEMAIL:DEFAULT", StringComparison.OrdinalIgnoreCase))
                                                select si.clientServiceRole.ToList().Where(sri => sri.id.Equals("DEFAULT")).ToList();
                            if (defaulttroles.Any())
                            {
                                assignedemails = assignedemails + defaulttroles.ToList().FirstOrDefault().Count();
                            }
                        }
                    }
                }

                if (assignedemails > 1)
                {
                    islastemail = false;
                }
                else if (assignedemails == 1)
                {
                    islastemail = true;
                }
            }
            else if (!String.IsNullOrEmpty(primaryemail))
            {
                GetBatchProfileV1Res responsegsup = new GetBatchProfileV1Res();
                responsegsup = DnpWrapper.GetServiceUserProfilesV1ForDante(primaryemail, "BTIEMAILID");

                if (responsegsup != null && responsegsup.clientProfileV1 != null && responsegsup.clientProfileV1.Count() > 0)
                {
                    foreach (ClientProfileV1 clp in responsegsup.clientProfileV1)
                    {
                        //Todo : Move the if conditions to lamdba
                        if (clp.clientServiceInstanceV1 != null)
                        {
                            var defaulttroles = from si in clp.clientServiceInstanceV1
                                                where (si.clientServiceInstanceIdentifier.value.Equals("BTIEMAIL:DEFAULT", StringComparison.OrdinalIgnoreCase))
                                                select si.clientServiceRole.ToList().Where(sri => sri.id.Equals("DEFAULT")).ToList();
                            if (defaulttroles.Any())
                            {
                                assignedemails = assignedemails + defaulttroles.ToList().FirstOrDefault().Count();
                            }
                        }
                    }
                }

                if (assignedemails > 1)
                {
                    islastemail = false;
                }
                else if (assignedemails == 1)
                {
                    islastemail = true;
                }
            }
            return islastemail;
        }

        public static void ReactivateReinstateMapper(string BAC, string TargetBAC, string EmailName, string reason, string serviceAddressingFrom, ref BT.SaaS.Core.Shared.Entities.RoleInstance roleInstance, ref string emailSupplier)
        {
            bool isStaretegicProductModelLaunched = false;
            isStaretegicProductModelLaunched = bool.Parse(ConfigurationManager.AppSettings["isStaretegicProductModelLaunched"]);
            if (!string.IsNullOrEmpty(reason) && reason.Equals("Reinstate", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(BAC))
                    DanteRequestProcessor.ReinstateonSameBAC(BAC, EmailName, ref roleInstance, ref emailSupplier);
                else if (!string.IsNullOrEmpty(TargetBAC))
                    DanteRequestProcessor.ReinstateonTargetBAC(TargetBAC, EmailName, ref roleInstance, ref emailSupplier);
                else if (isStaretegicProductModelLaunched)
                {
                    DanteRequestProcessor.ValidateReinstateOrder(BAC, EmailName, ref emailSupplier);
                }
                else
                {
                    throw new DnpException("StaretegicProductModel switch if Off");
                }
            }
            else if (!string.IsNullOrEmpty(BAC))
                DanteRequestProcessor.ReactivateonSameBAC(BAC, EmailName, ref roleInstance, ref emailSupplier);
            else if (!string.IsNullOrEmpty(TargetBAC))
                DanteRequestProcessor.ReactivateonTargetBAC(TargetBAC, EmailName, ref roleInstance, ref emailSupplier);
            else if (serviceAddressingFrom.Equals(ConfigurationManager.AppSettings["BBMTServiceAddress"], StringComparison.OrdinalIgnoreCase)) // as no BAC would be passed from Resolve for Reactivate orders
                DanteRequestProcessor.ReactivateonSameBAC(BAC, EmailName, ref roleInstance, ref emailSupplier);
            else if (isStaretegicProductModelLaunched)
            {
                DanteRequestProcessor.ValidateReinstateOrder(string.Empty, EmailName, ref emailSupplier);
            }
            else
            {
                throw new DnpException("StaretegicProductModel switch if Off");
            }
        }

        public static void ValidateReinstateOrder(string BtOneID, string EmailName, ref string emailSupplier)
        {
            BT.SaaS.MSEOAdapter.ESB.ID.IdentityDetailType idDetails = null; string downStreamError = string.Empty; string e2eData = string.Empty; Guid guid = Guid.NewGuid();
            if (ESBRestCallWrapper.GetIdentityDetails(BtOneID, "BTID", ref downStreamError, ref e2eData, out idDetails, guid))
            {
                //Do nothing return success to BtCom BAC and Basic Email asset creation will triggered asynchronously.
            }
            else
            {
                throw new DnpException("Get Identity Details call failed" + downStreamError);
            }
        }

        public static void ReinstateAsBasicMail(string BtOneID, string EmailName, ref string emailSupplier,ref E2ETransaction Mye2etxn)
        {
            // need to trigger ESB calls to create BAC.
            //CreateCustomerResponse custResponse = new CreateCustomerResponse();
            BT.SaaS.MSEOAdapter.ESB.GCD.V9.ContactDetailsResponse contactDetailsResponse = null;
            BT.SaaS.MSEOAdapter.ESB.ID.IdentityDetailType idDetails = null;
            string conkId = string.Empty;
            string custid = string.Empty;
            string bac = string.Empty; string downStreamError = string.Empty; string e2eData = string.Empty;
            bool isnewConkid = false;
            Guid guid = Guid.NewGuid();
            string bptmTxnId = BPTMHelper.GetTxnIdFromE2eData(Mye2etxn.toString());

            if (ESBRestCallWrapper.GetIdentityDetails(BtOneID, "BTID", ref downStreamError, ref e2eData, out idDetails, guid))
            {
                if (IsStrategicIdentity(idDetails, ref conkId))
                {
                    if (string.IsNullOrEmpty(conkId))
                        throw new DnpException("DI Issue No CONK found for Strategic BTID");
                }
                else
                {
                    if (string.IsNullOrEmpty(conkId))
                    {
                        if (ESBRestCallWrapper.CreateConkidV1(idDetails, ref conkId, guid, bptmTxnId, ref downStreamError, ref Mye2etxn))
                            isnewConkid = true;
                        else
                            throw new DnpException("CONK creation failed at ESB end: " + downStreamError);
                    }
                }

                if (isnewConkid)
                {
                    if (!ESBRestCallWrapper.CreateCustomerid(conkId, ref downStreamError, out custid, guid, bptmTxnId, ref Mye2etxn))
                        throw new DnpException("CAK creation failed:" + downStreamError);
                }
                else
                {
                    if (ESBRestCallWrapper.GetContactDetails(conkId, ref downStreamError, ref e2eData, out contactDetailsResponse, guid))
                    {
                        custid = GetCAKfromCONK(contactDetailsResponse);
                        if (string.IsNullOrEmpty(custid))
                        {
                            if (!ESBRestCallWrapper.CreateCustomerid(conkId, ref downStreamError, out custid, guid, bptmTxnId,ref Mye2etxn))
                                throw new DnpException("CAK creation failed:" + downStreamError);
                        }
                    }
                    else
                        throw new DnpException("GetContactDetails call failed " + downStreamError);
                }

                if (ESBRestCallWrapper.CreateBillingAccountV1(custid, conkId, ref bac, idDetails, guid, bptmTxnId, ref downStreamError, ref Mye2etxn))
                {
                    if (ESBRestCallWrapper.CreateBACandBasicEmailcalltoESB(bac, EmailName, custid, guid, bptmTxnId,ref Mye2etxn))
                    {
                        ReinstateEmailbox(EmailName);
                    }
                    else
                        throw new DnpException("BASIC email Asset creation failed");
                }
                else
                    throw new DnpException("BAC creation failed: " + downStreamError);
            }
            else
            {
                throw new DnpException("Get Identity Details call failed" + downStreamError);
            }
        }
        public static void CeaseBasicorPremiumMail(string BtOneID, string EmailName, string BAC, string vasproductid)
        {            
            string downStreamError = string.Empty; string e2eData = string.Empty;
            string emailType = vasproductid.Equals("VPEM000060") ? "Basic" : "Premium";
            Guid guid = Guid.NewGuid();

            BT.ESB.RoBT.ManageCustomer.QueryCustomerResponse customerResponse = ESBRestCallWrapper.GetQueryCustomerDetailswithBAC(BAC, guid, ref downStreamError);

            if (customerResponse != null && customerResponse.customerAccount != null)
            {
                if (!ESBRestCallWrapper.CeaseBACandEmailServicecalltoESB(customerResponse, guid, emailType, ref downStreamError))
                {
                    throw new DnpException("Email Asset cease failed with error as : " + downStreamError);
                }               
            }
            else
            {
                throw new DnpException("received null response in querycustomer response from ESB to cease the BAC : " + downStreamError);
            }            
        }
        public static bool IsStrategicIdentity(BT.SaaS.MSEOAdapter.ESB.ID.IdentityDetailType idDetails, ref string CONK)
        {
            if (idDetails != null && idDetails.Identifiers != null && idDetails.Identifiers.Count() > 0 && idDetails.Identifiers.ToList().Exists(id => id.IDType.Equals(BT.SaaS.MSEOAdapter.ESB.ID.IdentifierIDType.CONKID) && !string.IsNullOrEmpty(id.ID) && !id.Status.Equals("Inactive",StringComparison.OrdinalIgnoreCase)))
                CONK = idDetails.Identifiers.ToList().Where(id => id.IDType.Equals(BT.SaaS.MSEOAdapter.ESB.ID.IdentifierIDType.CONKID)).FirstOrDefault().ID;
            if (idDetails != null && idDetails.ContactDetails != null && idDetails.ContactDetails.IdentityDataSource.Equals(1))            
                return true;                        
            return false;
        }

        public static string GetCAKfromCONK(BT.SaaS.MSEOAdapter.ESB.GCD.V9.ContactDetailsResponse contactDetailsResponse)
        {
            string CAK = string.Empty;
            if (contactDetailsResponse != null && contactDetailsResponse.CustomerContact != null && contactDetailsResponse.CustomerContact.ListOfAccounts != null && contactDetailsResponse.CustomerContact.ListOfAccounts.Count() > 0 &&
                !string.IsNullOrEmpty(contactDetailsResponse.CustomerContact.ListOfAccounts[0].AccountId))
                CAK = contactDetailsResponse.CustomerContact.ListOfAccounts[0].AccountId;
            return CAK;
        }

        public static void ReactivateonSameBAC(string BAC, string EmailName, ref BT.SaaS.Core.Shared.Entities.RoleInstance roleInstance, ref string emailSupplier)
        {

            //CCP83 changes
            string strSAFSwitchONforReactivate = string.Empty;
            if (ConfigurationManager.AppSettings["EMAIL_VASF_SAF_PH2"] != null && ConfigurationManager.AppSettings["EMAIL_VASF_SAF_PH2"] != string.Empty)
                strSAFSwitchONforReactivate = ConfigurationManager.AppSettings["EMAIL_VASF_SAF_PH2"].ToString();

            Dictionary<string, string> roleAttributesList = new Dictionary<string, string>();
            GetClientProfileV1Res emailGCPResponse = new GetClientProfileV1Res();
            emailGCPResponse = DnpWrapper.GetClientProfileV1(EmailName, "BTIEMAILID");

            if (emailGCPResponse != null && emailGCPResponse.clientProfileV1 != null && emailGCPResponse.clientProfileV1.client != null
                && emailGCPResponse.clientProfileV1.client.clientIdentity != null && emailGCPResponse.clientProfileV1.client.clientIdentity.Count() > 0)
            {
                string PrimaryEmail = string.Empty;
                string SIBACNumber = string.Empty;

                #region FetchEmailSupplier
                var emailIdentity = emailGCPResponse.clientProfileV1.client.clientIdentity.ToList().Where(
                                                                            ci => ci.value.Equals(EmailName, StringComparison.OrdinalIgnoreCase) && ci.managedIdentifierDomain.value.Equals("BTIEMAILID", StringComparison.OrdinalIgnoreCase));
                if (emailIdentity.Any())
                {
                    if (emailIdentity.FirstOrDefault().clientIdentityStatus.value.Equals("Inactive", StringComparison.OrdinalIgnoreCase) || emailIdentity.FirstOrDefault().clientIdentityStatus.value.Equals("MARKED-INACTIVE", StringComparison.OrdinalIgnoreCase))
                        GetEmailSupplier(ref emailSupplier, emailIdentity.FirstOrDefault());
                    else
                        throw new DnpException("Email id is not in desired state on DnP for Re-Activate");

                    if (emailIdentity.FirstOrDefault().clientIdentityStatus.value.Equals("MARKED-INACTIVE", StringComparison.OrdinalIgnoreCase))
                    {                        
                        roleAttributesList.Add("STATUS", "MARKED-INACTIVE");
                    }

                    //CCP83 Changes
                    if (strSAFSwitchONforReactivate.Equals("ON", StringComparison.OrdinalIgnoreCase))
                    {
                        if (emailIdentity.FirstOrDefault().clientIdentityStatus.value.Equals("INACTIVE", StringComparison.OrdinalIgnoreCase))
                        {
                            roleAttributesList.Add("STATUS", "INACTIVE");
                        }
                    }
                }
                #endregion

                #region fetchSIBACandPrimaryEmail
                if (emailGCPResponse.clientProfileV1.clientServiceInstanceV1 != null && emailGCPResponse.clientProfileV1.clientServiceInstanceV1.Count() > 0)
                {
                    var serviceInst = (from si in emailGCPResponse.clientProfileV1.clientServiceInstanceV1
                                       where (si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase) && si.clientServiceRole != null && si.clientServiceRole.ToList().Exists(sr => sr.clientIdentity != null && sr.clientIdentity.ToList().Exists(sri => sri.value.Equals(EmailName, StringComparison.OrdinalIgnoreCase))))
                                       select si).FirstOrDefault();

                    if (serviceInst != null && serviceInst.serviceIdentity != null && serviceInst.serviceIdentity.Count() > 0 && serviceInst.serviceIdentity.ToList().Exists(si => si.domain.Equals("BTIEMAILID", StringComparison.OrdinalIgnoreCase)))
                    {
                        PrimaryEmail = serviceInst.serviceIdentity.ToList().Where(si => si.domain.Equals("BTIEMAILID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;

                        if (!roleAttributesList.ContainsKey("PrimaryEmail"))
                            roleAttributesList.Add("PrimaryEmail", PrimaryEmail);
                    }
                    if (serviceInst != null && serviceInst.serviceIdentity != null && serviceInst.serviceIdentity.Count() > 0 && serviceInst.serviceIdentity.ToList().Exists(si => si.domain.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase)))
                    {
                        SIBACNumber = serviceInst.serviceIdentity.ToList().Where(si => si.domain.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;

                        if (!roleAttributesList.ContainsKey("SIBACNUMBER"))
                            roleAttributesList.Add("SIBACNUMBER", SIBACNumber);
                    }
                    if (serviceInst != null && serviceInst.clientServiceRole != null && serviceInst.clientServiceRole.ToList().Exists(x => x.id.Equals("Default", StringComparison.OrdinalIgnoreCase))
                        && serviceInst.clientServiceRole.ToList().Exists(sr => sr.clientIdentity != null && sr.id.Equals("Default", StringComparison.OrdinalIgnoreCase) && sr.clientIdentity.ToList().Exists(sri => sri.value.Equals(EmailName, StringComparison.OrdinalIgnoreCase))))
                    {
                        var roleStatus = serviceInst.clientServiceRole.ToList().Where(sr => sr.clientIdentity != null && sr.id.Equals("Default", StringComparison.OrdinalIgnoreCase) && sr.clientIdentity.ToList().Exists(sri => sri.value.Equals(EmailName, StringComparison.OrdinalIgnoreCase))).FirstOrDefault().clientServiceRoleStatus.value;
                        roleAttributesList.Add("rolestatus", roleStatus);
                    }
                    if (serviceInst != null && serviceInst.clientServiceInstanceCharacteristic != null && serviceInst.clientServiceInstanceCharacteristic.Count() > 0
                        && serviceInst.clientServiceInstanceCharacteristic.ToList().Exists(csiChar => csiChar.name.Equals("VAS_PRODUCT_ID", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(csiChar.value)))
                    {
                        var vasProductId = serviceInst.clientServiceInstanceCharacteristic.ToList().Where(csiChar => csiChar.name.Equals("VAS_PRODUCT_ID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                        
                        MSEOSaaSMapper.AddorUpdateProductAttributes("VasProductId", vasProductId, ref roleInstance, BT.SaaS.Core.Shared.Entities.DataActionEnum.change);
                    }
                }
                #endregion

                if (roleAttributesList.Count > 0)
                    PrepareRoleAttributes(roleAttributesList, ref roleInstance);
            }
            else
            {
                throw new DnpException("Email id is not present at DnP end or dnp send null response ");
            }
        }

        public static void ReinstateonSameBAC(string BAC, string EmailName, ref BT.SaaS.Core.Shared.Entities.RoleInstance roleInstance, ref string emailSupplier)
        {
            int emailCount = 0;
            bool isPrimaryEmail = false;
            bool isAffiliateEmail = false;
            string newprimaryEmail = string.Empty;

            GetClientProfileV1Res emailGCPResponse = new GetClientProfileV1Res();
            emailGCPResponse = DnpWrapper.GetClientProfileV1(EmailName, "BTIEMAILID");

            if (emailGCPResponse != null && emailGCPResponse.clientProfileV1 != null && emailGCPResponse.clientProfileV1.client != null
                && emailGCPResponse.clientProfileV1.client.clientIdentity != null && emailGCPResponse.clientProfileV1.client.clientIdentity.Count() > 0)
            {
                string PrimaryEmail = string.Empty;
                string SIBACNumber = string.Empty;

                if (emailGCPResponse.clientProfileV1.clientServiceInstanceV1 != null && emailGCPResponse.clientProfileV1.clientServiceInstanceV1.Count() > 0)
                {
                    var serviceInst = (from si in emailGCPResponse.clientProfileV1.clientServiceInstanceV1
                                       where (si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase) && si.clientServiceRole != null && si.clientServiceRole.ToList().Exists(sr => sr.clientIdentity != null && sr.clientIdentity.ToList().Exists(sri => sri.value.Equals(EmailName, StringComparison.OrdinalIgnoreCase))))
                                       select si).FirstOrDefault();

                    if (serviceInst != null && serviceInst.serviceIdentity != null && serviceInst.serviceIdentity.Count() > 0 && serviceInst.serviceIdentity.ToList().Exists(si => si.domain.Equals("BTIEMAILID", StringComparison.OrdinalIgnoreCase)))
                    {
                        PrimaryEmail = serviceInst.serviceIdentity.ToList().Where(si => si.domain.Equals("BTIEMAILID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;

                        BT.SaaS.Core.Shared.Entities.Attribute primaryemail = new BT.SaaS.Core.Shared.Entities.Attribute();
                        primaryemail.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                        primaryemail.Name = "PrimaryEmail";
                        primaryemail.Value = PrimaryEmail;
                        roleInstance.Attributes.Add(primaryemail);
                    }
                    if (serviceInst != null && serviceInst.serviceIdentity != null && serviceInst.serviceIdentity.Count() > 0 && serviceInst.serviceIdentity.ToList().Exists(si => si.domain.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase)))
                    {
                        SIBACNumber = serviceInst.serviceIdentity.ToList().Where(si => si.domain.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;

                        BT.SaaS.Core.Shared.Entities.Attribute primaryemail = new BT.SaaS.Core.Shared.Entities.Attribute();
                        primaryemail.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                        primaryemail.Name = "SIBACNUMBER";
                        primaryemail.Value = SIBACNumber;
                        roleInstance.Attributes.Add(primaryemail);
                    }
                }
                // Need to get the email count
                if (!string.IsNullOrEmpty(BAC))
                {
                    if (isLastEmailmoved(BAC, BACID_IDENTIFER_NAMEPACE, EmailName, isPrimaryEmail, ref isAffiliateEmail, ref newprimaryEmail, "reinstate", ref emailCount)) { }
                }
                else if(!string.IsNullOrEmpty(PrimaryEmail))
                {
                    if (isLastEmailmoved(PrimaryEmail, "BTIEMAILID", EmailName, isPrimaryEmail, ref isAffiliateEmail, ref newprimaryEmail, "reinstate", ref emailCount)) { }
                }
                emailCount = 11 - emailCount;

                BT.SaaS.Core.Shared.Entities.Attribute emailCountAttr = new BT.SaaS.Core.Shared.Entities.Attribute();
                emailCountAttr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                emailCountAttr.Name = "EmailCount";
                emailCountAttr.Value = emailCount.ToString();
                roleInstance.Attributes.Add(emailCountAttr);

                // Need to remove this check for expired mailboxes read pwd/pwdsalt and use it for mailbox creation for rest of the statues check for product model launched conditions
                if (emailGCPResponse.clientProfileV1.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BTIEMAILID") && ci.value.Equals(EmailName) && (ci.clientIdentityStatus.value.Equals("EXPIRED", StringComparison.OrdinalIgnoreCase) || ci.clientIdentityStatus.value.Equals("CEASE-INACTIVE", StringComparison.OrdinalIgnoreCase))))
                {
                    ClientIdentity cIdentity = emailGCPResponse.clientProfileV1.client.clientIdentity.ToList().Where(ci => ci.value.Equals(EmailName) && ci.managedIdentifierDomain.value.Equals("BTIEMAILID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

                    #region FetchEmailSupplier
                    if (cIdentity != null)
                    {
                        if (cIdentity.clientIdentityStatus.value.Equals("Expired", StringComparison.OrdinalIgnoreCase) || cIdentity.clientIdentityStatus.value.Equals("CEASE-INACTIVE", StringComparison.OrdinalIgnoreCase))
                            GetEmailSupplier(ref emailSupplier, cIdentity);
                        else
                            throw new DnpException("Email id is not in desired state on DnP for Re-Instate");
                    }
                    #endregion

                    //Todo Check with Murali around the logic for clientIdentityStatus char and remove below piece of code if not required
                    BT.SaaS.Core.Shared.Entities.Attribute cIdentitystatus = new BT.SaaS.Core.Shared.Entities.Attribute();
                    cIdentitystatus.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                    cIdentitystatus.Name = "clientIdentityStatus";
                    cIdentitystatus.Value = cIdentity.clientIdentityStatus.value;
                    roleInstance.Attributes.Add(cIdentitystatus);

                    if (cIdentity != null && cIdentity.clientCredential != null && cIdentity.clientCredential.ToList().Exists(x => x.credentialType.Equals("SHA256_V1", StringComparison.OrdinalIgnoreCase)))
                    {
                        BT.SaaS.Core.Shared.Entities.Attribute Password = new BT.SaaS.Core.Shared.Entities.Attribute();
                        Password.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                        Password.Name = "Password";
                        Password.Value = cIdentity.clientCredential.ToList().Where(x => x.credentialType.Equals("SHA256_V1", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().credentialValue;
                        roleInstance.Attributes.Add(Password);
                    }
                    if (cIdentity != null && cIdentity.clientCredential != null && cIdentity.clientCredential.ToList().Exists(x => x.credentialType.Equals("BASE64_MD5", StringComparison.OrdinalIgnoreCase)))
                    {
                        BT.SaaS.Core.Shared.Entities.Attribute Password = new BT.SaaS.Core.Shared.Entities.Attribute();
                        Password.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                        Password.Name = "Base64Password";
                        Password.Value = cIdentity.clientCredential.ToList().Where(x => x.credentialType.Equals("BASE64_MD5", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().credentialValue;
                        roleInstance.Attributes.Add(Password);
                    }
                    if (cIdentity != null && cIdentity.clientIdentityValidation != null && cIdentity.clientIdentityValidation.ToList().Exists(civ => civ.name.Equals("SALT_VALUE")))
                    {
                        BT.SaaS.Core.Shared.Entities.Attribute PasswordSalt = new BT.SaaS.Core.Shared.Entities.Attribute();
                        PasswordSalt.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                        PasswordSalt.Name = "PWDHASHSALT";
                        PasswordSalt.Value = cIdentity.clientIdentityValidation.ToList().Where(civ => civ.name.Equals("SALT_VALUE")).FirstOrDefault().value;
                        roleInstance.Attributes.Add(PasswordSalt);
                    }
                }
                else
                {

                    throw new DnpException("Email id is not in pending_cease or expired state in DnP");
                }
            }
            else
            {
                throw new DnpException("Email id is not present at DnP end or dnp send null response ");
            }
        }

        //Todo : check if it's a valid UC and remove if not required
        public static void ReactivateonTargetBAC(string TargetBAC, string EmailName, ref BT.SaaS.Core.Shared.Entities.RoleInstance roleInstance, ref string emailSupplier)
        {
            // Need to get the email count

            int emailCount = 0;
            bool isPrimaryEmail = false;
            bool isAffiliateEmail = false;
            string newprimaryEmail = string.Empty;

            if (isLastEmailmoved(TargetBAC, BACID_IDENTIFER_NAMEPACE, EmailName, isPrimaryEmail, ref isAffiliateEmail, ref newprimaryEmail, "reinstate", ref emailCount)) { }

            emailCount = 11 - emailCount;

            BT.SaaS.Core.Shared.Entities.Attribute emailCountAttr = new BT.SaaS.Core.Shared.Entities.Attribute();
            emailCountAttr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
            emailCountAttr.Name = "EmailCount";
            emailCountAttr.Value = emailCount.ToString();
            roleInstance.Attributes.Add(emailCountAttr);

            Dictionary<string, string> roleAttributesList = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(TargetBAC))
            {
                //Todo Update GCP to GSUP to eliminate another GCP on Email(for affiliate scenarios)
                GetClientProfileV1Res bacGCPResponse = new GetClientProfileV1Res();
                bacGCPResponse = DnpWrapper.GetClientProfileV1(TargetBAC, BACID_IDENTIFER_NAMEPACE);

                if (bacGCPResponse != null && bacGCPResponse.clientProfileV1 != null && bacGCPResponse.clientProfileV1.client != null
                    && bacGCPResponse.clientProfileV1.client.clientIdentity != null && bacGCPResponse.clientProfileV1.client.clientIdentity.Count() > 0
                    && bacGCPResponse.clientProfileV1.clientServiceInstanceV1 != null && bacGCPResponse.clientProfileV1.clientServiceInstanceV1.Count() > 0
                    && bacGCPResponse.clientProfileV1.clientServiceInstanceV1.ToList().Exists(csi => csi.clientServiceInstanceIdentifier.value.Equals(BTEMAIL_SERVICECODE_NAMEPACE) && csi.serviceIdentity.ToList().Exists(si => si.domain.Equals(BACID_IDENTIFER_NAMEPACE) && si.value.Equals(TargetBAC)) && csi.clientServiceInstanceStatus.value.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase)))
                {
                    //Do Nothing
                    //var serviceInst = bacGCPResponse.clientProfileV1.clientServiceInstanceV1.ToList().Where(csi => csi.clientServiceInstanceIdentifier.value.Equals(BTEMAIL_SERVICECODE_NAMEPACE) && csi.serviceIdentity.ToList().Exists(si => si.domain.Equals(BACID_IDENTIFER_NAMEPACE) && si.value.Equals(BAC))).FirstOrDefault();
                    //GetEmailSupplier(true, ref emailSupplier, serviceInst);
                    //if (!roleAttributesList.ContainsKey("BillAccountNUmber"))
                    //    roleAttributesList.Add("BillAccountNUmber", TargetBAC);
                }
                else
                    throw new DnpException("BillingAccountnumber doesn't have email service/Active email service in DnP profile");

            }

            GetClientProfileV1Res emailGCPResponse = new GetClientProfileV1Res();
            emailGCPResponse = DnpWrapper.GetClientProfileV1(EmailName, "BTIEMAILID");

            if (emailGCPResponse != null && emailGCPResponse.clientProfileV1 != null && emailGCPResponse.clientProfileV1.client != null
                && emailGCPResponse.clientProfileV1.client.clientIdentity != null && emailGCPResponse.clientProfileV1.client.clientIdentity.Count() > 0)
            {
                string PrimaryEmail = string.Empty;
                string SIBACNumber = string.Empty;

                if (emailGCPResponse.clientProfileV1.clientServiceInstanceV1 != null && emailGCPResponse.clientProfileV1.clientServiceInstanceV1.Count() > 0)
                {
                    var serviceInst = (from si in emailGCPResponse.clientProfileV1.clientServiceInstanceV1
                                       where (si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase) && si.clientServiceRole != null && si.clientServiceRole.ToList().Exists(sr => sr.clientIdentity != null && sr.clientIdentity.ToList().Exists(sri => sri.value.Equals(EmailName, StringComparison.OrdinalIgnoreCase))))
                                       select si).FirstOrDefault();

                    if (serviceInst != null && serviceInst.serviceIdentity != null && serviceInst.serviceIdentity.Count() > 0 && serviceInst.serviceIdentity.ToList().Exists(si => si.domain.Equals("BTIEMAILID", StringComparison.OrdinalIgnoreCase)))
                    {
                        PrimaryEmail = serviceInst.serviceIdentity.ToList().Where(si => si.domain.Equals("BTIEMAILID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                        roleAttributesList.Add("PrimaryEmail", PrimaryEmail);
                    }
                    if (serviceInst != null && serviceInst.serviceIdentity != null && serviceInst.serviceIdentity.Count() > 0 && serviceInst.serviceIdentity.ToList().Exists(si => si.domain.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase)))
                    {
                        SIBACNumber = serviceInst.serviceIdentity.ToList().Where(si => si.domain.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                        roleAttributesList.Add("SIBACNUMBER", SIBACNumber);
                    }
                    roleAttributesList.Add("ISLASTEMAILID", reactivateTargetBACcase(SIBACNumber, PrimaryEmail) ? "true" : "false");

                    if (serviceInst != null && serviceInst.clientServiceRole != null && serviceInst.clientServiceRole.ToList().Exists(x => x.id.Equals("Default", StringComparison.OrdinalIgnoreCase))
                        && serviceInst.clientServiceRole.ToList().Exists(sr => sr.clientIdentity != null && sr.id.Equals("Default", StringComparison.OrdinalIgnoreCase) && sr.clientIdentity.ToList().Exists(sri => sri.value.Equals(EmailName, StringComparison.OrdinalIgnoreCase))))
                    {
                        var roleStatus = serviceInst.clientServiceRole.ToList().Where(sr => sr.clientIdentity != null && sr.id.Equals("Default", StringComparison.OrdinalIgnoreCase) && sr.clientIdentity.ToList().Exists(sri => sri.value.Equals(EmailName, StringComparison.OrdinalIgnoreCase))).FirstOrDefault().clientServiceRoleStatus.value;
                        roleAttributesList.Add("rolestatus", roleStatus);
                    }
                }
                // Need to remove this check for expired mailboxes read pwd/pwdsalt and use it for mailbox creation for rest of the statues check for product model launched conditions
                if (emailGCPResponse.clientProfileV1.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BTIEMAILID") && ci.value.Equals(EmailName) && (ci.clientIdentityStatus.value.Equals("EXPIRED", StringComparison.OrdinalIgnoreCase) || ci.clientIdentityStatus.value.Equals("CEASE-INACTIVE", StringComparison.OrdinalIgnoreCase))))
                {
                    ClientIdentity cIdentity = emailGCPResponse.clientProfileV1.client.clientIdentity.ToList().Where(ci => ci.value.Equals(EmailName) && ci.clientCredential != null).FirstOrDefault();
                    roleAttributesList.Add("clientIdentityStatus", cIdentity.clientIdentityStatus.value);

                    if (cIdentity != null && cIdentity.clientCredential != null && cIdentity.clientCredential.ToList().Exists(x => x.credentialType.Equals("SHA256_V1", StringComparison.OrdinalIgnoreCase)))
                    {
                        roleAttributesList.Add("Password", cIdentity.clientCredential.ToList().Where(x => x.credentialType.Equals("SHA256_V1", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().credentialValue);
                    }

                    if (cIdentity != null && cIdentity.clientIdentityValidation != null && cIdentity.clientIdentityValidation.ToList().Exists(civ => civ.name.Equals("SALT_VALUE")))
                    {
                        roleAttributesList.Add("PWDHASHSALT", cIdentity.clientIdentityValidation.ToList().Where(civ => civ.name.Equals("SALT_VALUE")).FirstOrDefault().value);
                    }
                    roleAttributesList.Add("REASON", "ReinstateonTargetBAC");
                }
                else
                {
                    //Just Reactivate
                }
                if (!roleAttributesList.ContainsKey("REASON"))
                    roleAttributesList.Add("REASON", "ReactivateonTargetBAC");
                PrepareRoleAttributes(roleAttributesList, ref roleInstance);
            }
            else
            {
                throw new DnpException("Email id is not present at DnP end or dnp send null response ");
            }
        }

        public static void ReinstateonTargetBAC(string TargetBAC, string EmailName, ref BT.SaaS.Core.Shared.Entities.RoleInstance roleInstance, ref string emailSupplier)
        {
            Dictionary<string, string> roleAttributesList = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(TargetBAC))
            {
                //Todo Update GCP to GSUP to eliminate another GCP on Email(for affiliate scenarios)
                GetClientProfileV1Res bacGCPResponse = new GetClientProfileV1Res();
                bacGCPResponse = DnpWrapper.GetClientProfileV1(TargetBAC, BACID_IDENTIFER_NAMEPACE);

                if (bacGCPResponse != null && bacGCPResponse.clientProfileV1 != null && bacGCPResponse.clientProfileV1.client != null
                    && bacGCPResponse.clientProfileV1.client.clientIdentity != null && bacGCPResponse.clientProfileV1.client.clientIdentity.Count() > 0
                    && bacGCPResponse.clientProfileV1.clientServiceInstanceV1 != null && bacGCPResponse.clientProfileV1.clientServiceInstanceV1.Count() > 0
                    && bacGCPResponse.clientProfileV1.clientServiceInstanceV1.ToList().Exists(csi => csi.clientServiceInstanceIdentifier.value.Equals(BTEMAIL_SERVICECODE_NAMEPACE) && csi.serviceIdentity.ToList().Exists(si => si.domain.Equals(BACID_IDENTIFER_NAMEPACE) && si.value.Equals(TargetBAC)) && csi.clientServiceInstanceStatus.value.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase)))
                {
                    //Do Nothing
                    //var serviceInst = bacGCPResponse.clientProfileV1.clientServiceInstanceV1.ToList().Where(csi => csi.clientServiceInstanceIdentifier.value.Equals(BTEMAIL_SERVICECODE_NAMEPACE) && csi.serviceIdentity.ToList().Exists(si => si.domain.Equals(BACID_IDENTIFER_NAMEPACE) && si.value.Equals(BAC))).FirstOrDefault();
                    //GetEmailSupplier(true, ref emailSupplier, serviceInst);
                    //if (!roleAttributesList.ContainsKey("BillAccountNUmber"))
                    //    roleAttributesList.Add("BillAccountNUmber", TargetBAC);
                }
                else
                    throw new DnpException("BillingAccountnumber doesn't have email service/Active email service in DnP profile");

            }

            GetClientProfileV1Res emailGCPResponse = new GetClientProfileV1Res();
            emailGCPResponse = DnpWrapper.GetClientProfileV1(EmailName, "BTIEMAILID");

            if (emailGCPResponse != null && emailGCPResponse.clientProfileV1 != null && emailGCPResponse.clientProfileV1.client != null
                && emailGCPResponse.clientProfileV1.client.clientIdentity != null && emailGCPResponse.clientProfileV1.client.clientIdentity.Count() > 0)
            {
                string PrimaryEmail = string.Empty;
                string SIBACNumber = string.Empty;

                if (emailGCPResponse.clientProfileV1.clientServiceInstanceV1 != null && emailGCPResponse.clientProfileV1.clientServiceInstanceV1.Count() > 0)
                {
                    var serviceInst = (from si in emailGCPResponse.clientProfileV1.clientServiceInstanceV1
                                       where (si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase) && si.clientServiceRole != null && si.clientServiceRole.ToList().Exists(sr => sr.clientIdentity != null && sr.clientIdentity.ToList().Exists(sri => sri.value.Equals(EmailName, StringComparison.OrdinalIgnoreCase))))
                                       select si).FirstOrDefault();

                    if (serviceInst != null && serviceInst.serviceIdentity != null && serviceInst.serviceIdentity.Count() > 0 && serviceInst.serviceIdentity.ToList().Exists(si => si.domain.Equals("BTIEMAILID", StringComparison.OrdinalIgnoreCase)))
                    {
                        PrimaryEmail = serviceInst.serviceIdentity.ToList().Where(si => si.domain.Equals("BTIEMAILID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                        roleAttributesList.Add("PrimaryEmail", PrimaryEmail);
                    }
                    if (serviceInst != null && serviceInst.serviceIdentity != null && serviceInst.serviceIdentity.Count() > 0 && serviceInst.serviceIdentity.ToList().Exists(si => si.domain.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase)))
                    {
                        SIBACNumber = serviceInst.serviceIdentity.ToList().Where(si => si.domain.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                        roleAttributesList.Add("SIBACNUMBER", SIBACNumber);
                    }
                    roleAttributesList.Add("ISLASTEMAILID", reactivateTargetBACcase(SIBACNumber, PrimaryEmail) ? "true" : "false");
                }
                // Need to remove this check for expired mailboxes read pwd/pwdsalt and use it for mailbox creation for rest of the statues check for product model launched conditions
                if (emailGCPResponse.clientProfileV1.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BTIEMAILID") && ci.value.Equals(EmailName) && (ci.clientIdentityStatus.value.Equals("EXPIRED", StringComparison.OrdinalIgnoreCase) || ci.clientIdentityStatus.value.Equals("CEASE-INACTIVE", StringComparison.OrdinalIgnoreCase))))
                {
                    ClientIdentity cIdentity = emailGCPResponse.clientProfileV1.client.clientIdentity.ToList().Where(ci => ci.value.Equals(EmailName) && ci.clientCredential != null).FirstOrDefault();
                    roleAttributesList.Add("clientIdentityStatus", cIdentity.clientIdentityStatus.value);

                    if (cIdentity != null && cIdentity.clientCredential != null && cIdentity.clientCredential.ToList().Exists(x => x.credentialType.Equals("SHA256_V1", StringComparison.OrdinalIgnoreCase)))
                    {
                        roleAttributesList.Add("Password", cIdentity.clientCredential.ToList().Where(x => x.credentialType.Equals("SHA256_V1", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().credentialValue);
                    }

                    if (cIdentity != null && cIdentity.clientIdentityValidation != null && cIdentity.clientIdentityValidation.ToList().Exists(civ => civ.name.Equals("SALT_VALUE")))
                    {
                        roleAttributesList.Add("PWDHASHSALT", cIdentity.clientIdentityValidation.ToList().Where(civ => civ.name.Equals("SALT_VALUE")).FirstOrDefault().value);
                    }
                }
                else
                {

                    throw new DnpException("Email id is not in pending_cease or expired state in DnP");
                }
                if (!roleAttributesList.ContainsKey("REASON"))
                    roleAttributesList.Add("REASON", "ReinstateonTargetBAC");
                PrepareRoleAttributes(roleAttributesList, ref roleInstance);
            }
            else
            {
                throw new DnpException("Email id is not present at DnP end or dnp send null response ");
            }
        }

        public static void PrepareRoleAttributes(Dictionary<string, string> roleAttributesList, ref BT.SaaS.Core.Shared.Entities.RoleInstance roleInstance)
        {
            foreach (KeyValuePair<string, string> attr in roleAttributesList)
            {                
                BT.SaaS.Core.Shared.Entities.Attribute primaryemail = new BT.SaaS.Core.Shared.Entities.Attribute();
                primaryemail.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                primaryemail.Name = attr.Key;
                primaryemail.Value = attr.Value;
                roleInstance.Attributes.Add(primaryemail);
            }
        }

        public static bool ReinstateEmailbox(string EmailName)
        {
            manageClientProfileV1Response1 profileResponse;
            manageClientProfileV1Request1 manageClientProfileCeaseRequest = new manageClientProfileV1Request1();
            manageClientProfileCeaseRequest.manageClientProfileV1Request = new ManageClientProfileV1Request();
            BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock headerBlock = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
            headerBlock.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();
            headerBlock.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
            headerBlock.serviceAddressing.from = "http://www.profile.com?SAASMSEO";

            ManageClientProfileV1Req manageClientProfileReq1 = new ManageClientProfileV1Req();
            manageClientProfileReq1.clientProfileV1 = new ClientProfileV1();
            manageClientProfileReq1.clientProfileV1.client = new Client();
            manageClientProfileReq1.clientProfileV1.client.action = ACTION_UPDATE;

            ClientIdentity[] clientIdentity = new ClientIdentity[2];

            clientIdentity[0] = new ClientIdentity();
            clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
            clientIdentity[0].managedIdentifierDomain.value = "BTIEMAILID";
            clientIdentity[0].value = EmailName;
            clientIdentity[0].action = ACTION_SEARCH;

            clientIdentity[1] = new ClientIdentity();
            clientIdentity[1].managedIdentifierDomain = new ManagedIdentifierDomain();
            clientIdentity[1].managedIdentifierDomain.value = "BTIEMAILID";
            clientIdentity[1].value = EmailName;
            clientIdentity[1].clientIdentityStatus = new ClientIdentityStatus();
            clientIdentity[1].clientIdentityStatus.value = "PENDING";
            clientIdentity[1].action = ACTION_UPDATE;

            List<ClientIdentityValidation> listIdentityValidation = new List<ClientIdentityValidation>();
            ClientIdentityValidation identityValidation = null;

            identityValidation = new ClientIdentityValidation();
            identityValidation.action = ACTION_FORCE_INSERT;
            identityValidation.name = "CEASE_REASON";
            //if (!string.IsNullOrEmpty(reason) && reason.Equals("Reinstate", StringComparison.OrdinalIgnoreCase))
            identityValidation.value = "awaiting reinstatement order fulfilment";
            //else
            //    identityValidation.value = "awaiting basicemail reactivate order fulfilment";
            listIdentityValidation.Add(identityValidation);

            clientIdentity[1].clientIdentityValidation = listIdentityValidation.ToArray();

            manageClientProfileReq1.clientProfileV1.client.clientIdentity = clientIdentity;

            manageClientProfileCeaseRequest.manageClientProfileV1Request.standardHeader = headerBlock;
            manageClientProfileCeaseRequest.manageClientProfileV1Request.manageClientProfileV1Req = manageClientProfileReq1;


            #region Call DnP
            profileResponse = DnpWrapper.manageClientProfileV1Thomas(manageClientProfileCeaseRequest, EmailName);

            if (profileResponse != null
                && profileResponse.manageClientProfileV1Response != null
                && profileResponse.manageClientProfileV1Response.standardHeader != null
                && profileResponse.manageClientProfileV1Response.standardHeader.serviceState != null
                && profileResponse.manageClientProfileV1Response.standardHeader.serviceState.stateCode != null
                && profileResponse.manageClientProfileV1Response.standardHeader.serviceState.stateCode == "0")
            {
                return true;
            }
            else
            {
                return false;
            }
            #endregion
        }
    }
}
