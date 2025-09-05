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
using com.bt.util.logging;

namespace BT.SaaS.MSEOAdapter
{
    public class VASRequestProcessor
    {
        public static bool isActiveServiceExist = false;
        #region VASRequestMapper

        public static SAASPE.Order VASRequestMapper(MSEO.OrderRequest orderRequest,ref E2ETransaction e2eData)
        {
            int orderItemWithVasCeaseCount = 0;
            int orderItemWithVasCreateCount = 0;
            int orderItemWithVasModifyCount = 0;
            string orderType = string.Empty;
            MSEOSaaSMapper requestMapper = null;
            PremiumEmail premiumEmail = null;
            ProductVasClass premiumProdVasClass = null;
            ClientServiceInstanceV1[] services = null;
            Dictionary<ProductVasClass, string> VASClassDic = new Dictionary<ProductVasClass, string>();
            string cfsid = string.Empty;
            string productCode = string.Empty;
            SAASPE.Order response = new BT.SaaS.Core.Shared.Entities.Order();

            try
            {
                var vasCancelAction = from orderItem in orderRequest.Order.OrderItem
                                      where orderItem.Action.Code.Equals("cancel", StringComparison.OrdinalIgnoreCase)
                                      select orderItem;

                orderItemWithVasCeaseCount = vasCancelAction.Count();

                var vasCreateAction = from orderItem in orderRequest.Order.OrderItem
                                      where orderItem.Action.Code.Equals("create", StringComparison.OrdinalIgnoreCase)
                                      select orderItem;

                orderItemWithVasCreateCount = vasCreateAction.Count();

                var vasModifyAction = from orderItem in orderRequest.Order.OrderItem
                                      where orderItem.Action.Code.Equals("modify", StringComparison.OrdinalIgnoreCase)
                                      select orderItem;

                orderItemWithVasModifyCount = vasModifyAction.Count();

                if (orderItemWithVasCreateCount == orderRequest.Order.OrderItem.Count())
                {
                    premiumEmail = new PremiumEmail(orderRequest);
                    if (premiumEmail.IsBBPremiumEmailOrder())
                    {
                        response = premiumEmail.MapBBPremiumEmailProvisionRequest(premiumProdVasClass);
                    }
                    else if (premiumEmail.IsPremiumEmailOrder(ref premiumProdVasClass)|| premiumEmail.IsBasicEmailOrder(ref premiumProdVasClass))
                    {
                        response = premiumEmail.MapPremiumEmailProvisionRequest(premiumProdVasClass);
                    }
                    else
                    {
                        orderType = "ofscreate";
                        OrderRequest acceptedOrderRequest = VASCreateRequest(orderRequest, ref orderType, ref VASClassDic, ref services, ref cfsid);

                        if (acceptedOrderRequest.Order.OrderItem.Length > 0)
                        {
                            //Activation || Reactivation of Email && wifi 
                            if (!String.IsNullOrEmpty(orderType) && orderType.Equals("Activation", StringComparison.OrdinalIgnoreCase))
                            {
                                response = MSEOSaaSMapper.MapActivation(acceptedOrderRequest, VASClassDic);
                            }
                            else if (!string.IsNullOrEmpty(orderType) && orderType.Equals("CFProvision", StringComparison.OrdinalIgnoreCase))
                            {
                                productCode = VASClassDic.Keys.ToList().Select(pvc => pvc.VasProductFamily).FirstOrDefault();
                                response = MSEOSaaSMapper.MapChangeRequest(acceptedOrderRequest, productCode, cfsid, string.Empty, string.Empty, string.Empty);
                            }
                            else
                            {
                                response = MSEOSaaSMapper.MapUpgradeRequest(acceptedOrderRequest, VASClassDic, services);
                            }
                        }
                        else
                        { //Preparing order object with empty Product Order Items so that Order will be ignored when it is submitted to PE.
                            response.ProductOrderItems = new List<BT.SaaS.Core.Shared.Entities.ProductOrderItem>();
                        }
                    }
                }

                else if (orderItemWithVasCeaseCount == orderRequest.Order.OrderItem.Count())
                {
                    response = MSEOSaaSMapper.MapCeaseRequest(orderRequest, ref e2eData);
                }
                else if (VASRequestValidator.IsRegrade(orderRequest))
                {
                    //ccp 78
                    string NayanRegrade = string.Empty;

                    if (orderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("IsNayanRegrade", StringComparison.OrdinalIgnoreCase)))
                    {
                        NayanRegrade = orderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("IsNayanRegrade", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                    }
                    if (ConfigurationManager.AppSettings["IsNayanRegrade"].ToString().Equals("true", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(NayanRegrade) && (NayanRegrade.Equals("Y")))
                    {                       
                         throw new DnpException("The request has been ignored since it is a nayan regrade request");                      
                    }                    
                    else
                    {
                        if (orderRequest.Order.OrderItem.ToList().Exists(oi => oi.Action.Code.Equals("Create", StringComparison.OrdinalIgnoreCase) && oi.Instance.ToList().Exists(inst => inst.InstanceCharacteristic.ToList().Exists(instchar => instchar.Name.Equals("BTWSID", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(instchar.Value))))
                        && orderRequest.Order.OrderItem.ToList().Exists(oi => oi.Action.Code.Equals("Create", StringComparison.OrdinalIgnoreCase) && oi.Instance.ToList().Exists(inst => inst.InstanceCharacteristic.ToList().Exists(instchar => instchar.Name.Equals("ServiceCode", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(instchar.Value) && instchar.Value.Equals("BT_CONSUMER_BROADBAND", StringComparison.OrdinalIgnoreCase)))))
                        {
                            BTCONSUMERBROADBANDProcessor choProcessor = new BTCONSUMERBROADBANDProcessor();
                            choProcessor.ModifyBBSIDMapper(orderRequest, ref e2eData);
                        }
                        if (orderRequest.Order.OrderItem.ToList().Exists(oi => oi.Action.Code.Equals("Create", StringComparison.OrdinalIgnoreCase) && oi.Instance.ToList().Exists(inst => inst.InstanceCharacteristic.ToList().Exists(instchar => instchar.Name.Equals("HCStatus", StringComparison.OrdinalIgnoreCase) && instchar.Value.Equals("Active", StringComparison.OrdinalIgnoreCase))))
                            && orderRequest.Order.OrderItem.ToList().Exists(oi => oi.Action.Code.Equals("Create", StringComparison.OrdinalIgnoreCase) && oi.Instance.ToList().Exists(inst => inst.InstanceCharacteristic.ToList().Exists(instchar => instchar.Name.Equals("ServiceCode", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(instchar.Value) && instchar.Value.Equals("BT_CONSUMER_BROADBAND", StringComparison.OrdinalIgnoreCase))))
                            && orderRequest.Order.OrderItem.ToList().Exists(oi => oi.Action.Code.Equals("Create", StringComparison.OrdinalIgnoreCase) && oi.Instance.ToList().Exists(inst => inst.InstanceCharacteristic.ToList().Exists(instchar => instchar.Name.Equals("RBSID", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(instchar.Value)))))
                        {
                            BTCONSUMERBROADBANDProcessor choProcessor = new BTCONSUMERBROADBANDProcessor();
                            choProcessor.ModifyRBSIDMapperforCHOP(orderRequest, ref e2eData);
                        }

                        response = MSEOSaaSMapper.MapRegradeRequest(orderRequest, ref e2eData);
                    }                    
                }
                else if (orderItemWithVasModifyCount == orderRequest.Order.OrderItem.Count())
                {
                    //Moving Homemove to Nominum Product processor(BTRCE-114308)
                    NominumProductProcessor nominumProcessor = new NominumProductProcessor();
                    response = nominumProcessor.MapHomeMoveRequest(orderRequest, ref e2eData);
                }
            }
            catch (DnpException DnPexception)
            {
                throw DnPexception;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                requestMapper = null;
                premiumEmail = null;
                VASClassDic = null;
                services = null;
                premiumProdVasClass = null;
            }

            return response;
        }

        #endregion

        #region VASCreateRequest

        public static OrderRequest VASCreateRequest(OrderRequest requestOrderRequest, ref string orderType, ref Dictionary<ProductVasClass, string> VASClassDic, ref ClientServiceInstanceV1[] services, ref string cfsid)
        {
            MSEOAdapter.OrderRequest orderRequest = null;
            //bool isCAActiveExist = false;
            //bool isWifiExist = false;
            string billAccNumber = string.Empty;
            string vasClassID = string.Empty;
            string dnpSrvcTier = string.Empty;
            string dnpWifiHubStatus = string.Empty;
            string dnpProductVasSupplierCode = string.Empty;
            string dnpVasProductId = string.Empty;
            List<string> vasClassList = new List<string>();
            List<ProductVasClass> maxPreferenceIndicatorVASClassList = new List<ProductVasClass>();
            List<ProductVasClass> vasDefinitionList = new List<ProductVasClass>();
            List<string> dnpProductList = new List<string>();
            Dictionary<ProductVasClass, string> upgradeVASClassDic = new Dictionary<ProductVasClass, string>();
            Dictionary<ProductVasClass, string> provisionVASClassDic = new Dictionary<ProductVasClass, string>();
            Dictionary<string, string> dnpProductDic = new Dictionary<string, string>();
            string[] upgradeProducts = null;
            string[] activationProducts = null;
            bool cfProvision = false;
            bool isCHOServiceExists = false;
            bool isBBSIDAlreadyExists = false;
            bool isBBSIDChangeRequired = false;
            string BBSID = string.Empty;            
            string identity = string.Empty;
            string identityDomain = string.Empty;
            string orderKey = string.Empty;
            string downStreamError = string.Empty;
            try
            {
                if (orderType.Equals("ofscreate"))
                {

                    if (requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(insChar => insChar.Name.Equals("billaccountnumber", StringComparison.OrdinalIgnoreCase)))
                    {
                        billAccNumber = requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(insChar => insChar.Name.Equals("billaccountnumber", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                    }
                    if (requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(inschar => inschar.Name.Equals("SyncContentFilteringWithMobile", StringComparison.OrdinalIgnoreCase)))
                    {
                        cfProvision = true;
                    }
                    foreach (OrderItem vasOrderItem in requestOrderRequest.Order.OrderItem)
                    {
                        vasClassID = vasOrderItem.Instance[0].InstanceCharacteristic.ToList().Where(insChar => insChar.Name.Equals("vasclass", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                        vasClassList.Add(vasClassID);
                    }
                    if ((vasClassList != null) && (vasClassList.Count > 0))
                    {
                        vasDefinitionList = MdmWrapper.getSaaSVASDefs(vasClassList);
                    }
                    if (vasDefinitionList.Count > 0)
                    {
                        string[] vasProductFamilyList = ConfigurationManager.AppSettings["VASProductFamily"].Split(new char[] { ';' });
                        string[] vasSubTypeList = ConfigurationManager.AppSettings["VASSubType"].Split(new char[] { ';' });

                        foreach (string vasProductFamily in vasProductFamilyList)
                        {
                            foreach (string vasSubType in vasSubTypeList)
                            {
                                ProductVasClass maxPreferenceIndicatorProductVASClass = (from vasDefinition in vasDefinitionList
                                                                                         where vasDefinition.VasProductFamily.Equals(vasProductFamily, StringComparison.OrdinalIgnoreCase)
                                                                                         && vasDefinition.vasSubType.Equals(vasSubType, StringComparison.OrdinalIgnoreCase)
                                                                                         orderby vasDefinition.PreferenceIndicator descending
                                                                                         select vasDefinition).FirstOrDefault();
                                if (maxPreferenceIndicatorProductVASClass != null)
                                {
                                    maxPreferenceIndicatorVASClassList.Add(maxPreferenceIndicatorProductVASClass);
                                }
                            }
                        }

                    }
                    ClientServiceInstanceV1[] res = DnpWrapper.getServiceInstanceV1(billAccNumber, "VAS_BILLINGACCOUNT_ID", "");
                    services = res;
                    //Get services linked to Security BillAccountNumber
                    ClientServiceInstanceV1[] migratedServices = DnpWrapper.GetVASClientServiceInstances(billAccNumber, "SECURITY_BAC");

                    if (res != null)
                    {
                        if (migratedServices != null)
                            res = res.Concat(migratedServices).ToArray();
                    }
                    else
                        res = migratedServices;

                    services = res;

                    if (res != null)
                    {
                        foreach (ClientServiceInstanceV1 srvcInstance in res)
                        {
                            // we are checking CHO service is present or not
                            if (srvcInstance.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["CHOIdentifier"], StringComparison.OrdinalIgnoreCase))
                            {
                                isCHOServiceExists = true;
                                // getting the existing client idetity details so based on this we can insert bbsid value to client.
                                if (srvcInstance.clientServiceRole != null && srvcInstance.clientServiceRole.Count() > 0 && srvcInstance.clientServiceRole.ToList().Exists(csr => csr.id.Equals("DEFAULT", StringComparison.OrdinalIgnoreCase) && csr.clientIdentity != null && csr.clientIdentity.Count() > 0))
                                {
                                    ClientServiceRole serviceRole = srvcInstance.clientServiceRole.ToList().Where(csr => csr.id.Equals("DEFAULT", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                                   //Order stuck Value can't be null
                                    if (serviceRole != null && serviceRole.clientIdentity != null)
                                    {
                                        identity = serviceRole.clientIdentity.FirstOrDefault().value.ToString();
                                        identityDomain = serviceRole.clientIdentity.FirstOrDefault().managedIdentifierDomain.value;
                                    }
                                }
                                //read bbsid & order key value from request.
                                if (!string.IsNullOrEmpty(requestOrderRequest.Order.OrderIdentifier.Value))
                                {
                                    orderKey = requestOrderRequest.Order.OrderIdentifier.Value;
                                }
                                if (requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(insChar => insChar.Name.Equals("BTWSID", StringComparison.OrdinalIgnoreCase)))
                                {
                                    BBSID = requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(insChar => insChar.Name.Equals("BTWSID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                                }
                                if (srvcInstance.clientServiceRole != null && srvcInstance.clientServiceRole.Count() > 0 && srvcInstance.clientServiceRole.ToList().Exists(csr => csr.id.Equals("DEFAULT", StringComparison.OrdinalIgnoreCase) && csr.clientIdentity != null && csr.clientIdentity.Count() > 0
                                && csr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BBSID", StringComparison.OrdinalIgnoreCase))))
                                {
                                    if (srvcInstance.clientServiceRole != null && srvcInstance.clientServiceRole.Count() > 0 && srvcInstance.clientServiceRole.ToList().Exists(csr => csr.id.Equals("DEFAULT", StringComparison.OrdinalIgnoreCase) && csr.clientIdentity != null && csr.clientIdentity.Count() > 0
                                        && csr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BBSID", StringComparison.OrdinalIgnoreCase) && ci.value.Equals(BBSID, StringComparison.OrdinalIgnoreCase))))
                                    {
                                        isBBSIDAlreadyExists = true;
                                    }
                                    else
                                    {
                                        isBBSIDChangeRequired = true;
                                    }
                                }
                            }
                            if (srvcInstance.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase))
                            {
                                //stuck order fix(ArgumentException: An item with the same key has already been added)
                                //dnpProductDic.Add(ConfigurationManager.AppSettings["DanteIdentifierInMDM"], srvcInstance.clientServiceInstanceStatus.value);
                                if (!dnpProductDic.ContainsKey(ConfigurationManager.AppSettings["DanteIdentifierInMDM"]))
                                {
                                    dnpProductDic.Add(ConfigurationManager.AppSettings["DanteIdentifierInMDM"], srvcInstance.clientServiceInstanceStatus.value);
                                    dnpProductList.Add(ConfigurationManager.AppSettings["DanteIdentifierInMDM"]);
                                }
                            }
                            else
                            {
                                if (srvcInstance.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["WifiIdentifier"], StringComparison.OrdinalIgnoreCase)
                                    && srvcInstance.clientServiceInstanceCharacteristic != null && srvcInstance.clientServiceInstanceCharacteristic.ToList().Exists(csi => csi.name.Equals("WIFI_SERVICE_TIER", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(csi.value))
                                    && srvcInstance.clientServiceInstanceCharacteristic != null && srvcInstance.clientServiceInstanceCharacteristic.ToList().Exists(csi => csi.name.Equals("HUB_WIFI_STATUS", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(csi.value)))
                                {
                                    isActiveServiceExist = true;
                                    dnpSrvcTier = srvcInstance.clientServiceInstanceCharacteristic.ToList().Where(sic => sic.name.Equals("WIFI_SERVICE_TIER", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                    dnpWifiHubStatus = srvcInstance.clientServiceInstanceCharacteristic.ToList().Where(sic => sic.name.Equals("HUB_WIFI_STATUS", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                }
                                else if (srvcInstance.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["CAService"], StringComparison.OrdinalIgnoreCase) && srvcInstance.clientServiceInstanceStatus.value.Equals("active", StringComparison.OrdinalIgnoreCase)
                                          && srvcInstance.clientServiceInstanceCharacteristic != null && srvcInstance.clientServiceInstanceCharacteristic.ToList().Exists(csi => csi.name.Equals("prev_supplier_code", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(csi.value)))
                                {
                                    isActiveServiceExist = true;
                                    dnpProductVasSupplierCode = srvcInstance.clientServiceInstanceCharacteristic.ToList().Where(si => si.name.Equals("prev_supplier_code", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                }
                                else if (srvcInstance.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["CFService"], StringComparison.OrdinalIgnoreCase) && srvcInstance.clientServiceInstanceStatus.value.Equals("active", StringComparison.OrdinalIgnoreCase)
                                    && srvcInstance.clientServiceInstanceCharacteristic != null && srvcInstance.clientServiceInstanceCharacteristic.ToList().Exists(csic => csic.name.Equals("cfsid", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(csic.value)))
                                {
                                    cfsid = srvcInstance.clientServiceInstanceCharacteristic.Where(csic => csic.name.Equals("cfsid", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                }
                                else if (srvcInstance.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["MMAService"], StringComparison.OrdinalIgnoreCase) && srvcInstance.clientServiceInstanceStatus.value.Equals("active", StringComparison.OrdinalIgnoreCase)
                                          && srvcInstance.clientServiceInstanceCharacteristic != null && srvcInstance.clientServiceInstanceCharacteristic.ToList().Exists(csi => csi.name.Equals("VAS_PRODUCT_ID", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(csi.value)))
                                {
                                    dnpVasProductId = srvcInstance.clientServiceInstanceCharacteristic.Where(csic => csic.name.Equals("VAS_PRODUCT_ID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                }
                                //stuck order fix
                                if (!dnpProductDic.ContainsKey(srvcInstance.clientServiceInstanceIdentifier.value))
                                {
                                    dnpProductDic.Add(srvcInstance.clientServiceInstanceIdentifier.value, srvcInstance.clientServiceInstanceStatus.value);
                                }
                                dnpProductList.Add(srvcInstance.clientServiceInstanceIdentifier.value);
                            }
                        }
                    }
                    if ((maxPreferenceIndicatorVASClassList.Count > 0))
                    {
                        activationProducts = ConfigurationManager.AppSettings["VASEarlyActivationProducts"].ToString().Split(';');
                        upgradeProducts = ConfigurationManager.AppSettings["VASUpgradeProducts"].ToString().Split(';');
                        for (int i = 0; i < maxPreferenceIndicatorVASClassList.Count; i++)
                        {
                            if (dnpProductDic.Count > 0)
                            {
                                foreach (string dnpProduct in dnpProductDic.Keys)
                                {
                                    //BBReactivate and email provision when PENDING
                                    if ((maxPreferenceIndicatorVASClassList[i].VasProductFamily + ":" + maxPreferenceIndicatorVASClassList[i].vasSubType).Equals(dnpProduct, StringComparison.OrdinalIgnoreCase) && new string[] { "pending-cease", "ceased", "pending" }.Contains(dnpProductDic[dnpProduct].ToLower()) && !cfProvision)
                                    {
                                        if (activationProducts.ToList().Contains(maxPreferenceIndicatorVASClassList[i].VasProductFamily))
                                        {
                                            //Reactivate wifi only for BB and not for Spring VAS request
                                            if (maxPreferenceIndicatorVASClassList[i].VasProductFamily.Equals(ConfigurationManager.AppSettings["WifiProdCode"], StringComparison.OrdinalIgnoreCase))
                                            {
                                                if (maxPreferenceIndicatorVASClassList[i].VasServiceTier.Equals(ConfigurationManager.AppSettings["BBWIFIServiceTier"], StringComparison.OrdinalIgnoreCase))
                                                {
                                                    provisionVASClassDic.Add(maxPreferenceIndicatorVASClassList[i], dnpProductDic[dnpProduct]);
                                                }
                                            }
                                            else
                                            {
                                                provisionVASClassDic.Add(maxPreferenceIndicatorVASClassList[i], dnpProductDic[dnpProduct]);
                                            }
                                        }
                                    }
                                    // New Provision
                                    else if (!dnpProductList.ToList().Exists(dp => dp.Equals(maxPreferenceIndicatorVASClassList[i].VasProductFamily + ":" + maxPreferenceIndicatorVASClassList[i].vasSubType, StringComparison.OrdinalIgnoreCase)))
                                    {
                                        if (activationProducts.ToList().Contains(maxPreferenceIndicatorVASClassList[i].VasProductFamily) && !(provisionVASClassDic.ContainsKey(maxPreferenceIndicatorVASClassList[i])) && !cfProvision)
                                        {
                                            provisionVASClassDic.Add(maxPreferenceIndicatorVASClassList[i], "");
                                        }
                                    }
                                    // Upgrade
                                    else if (((maxPreferenceIndicatorVASClassList[i].VasProductFamily + ":" + maxPreferenceIndicatorVASClassList[i].vasSubType).Equals(dnpProduct, StringComparison.OrdinalIgnoreCase)) && (upgradeProducts.ToList().Contains(dnpProduct)) && dnpProductDic[dnpProduct].ToLower().Equals("active") && !cfProvision)
                                    {
                                        if (dnpProduct.ToUpper().Equals(ConfigurationManager.AppSettings["WifiIdentifier"]))
                                        {
                                            //ignore wifi if service tier is same in both MDM and DnP
                                            if (maxPreferenceIndicatorVASClassList[i].VasServiceTier.Equals(dnpSrvcTier, StringComparison.OrdinalIgnoreCase))
                                            {
                                                if (maxPreferenceIndicatorVASClassList[i].VasServiceTier.Equals(ConfigurationManager.AppSettings["SpringWIFIServiceTier"], StringComparison.OrdinalIgnoreCase))
                                                {
                                                    throw new DnpException("Service is in active state and no change in service tier");
                                                }
                                            }
                                            else if (maxPreferenceIndicatorVASClassList[i].VasServiceTier.Equals(ConfigurationManager.AppSettings["BBWIFIServiceTier"], StringComparison.OrdinalIgnoreCase))
                                            {
                                                if (!dnpWifiHubStatus.ToUpper().Equals(ConfigurationManager.AppSettings["BBWIFIHubStatus"], StringComparison.OrdinalIgnoreCase))
                                                {
                                                    provisionVASClassDic.Add(maxPreferenceIndicatorVASClassList[i], dnpProductDic[dnpProduct]);
                                                }
                                            }
                                            else if (maxPreferenceIndicatorVASClassList[i].VasServiceTier.Equals(ConfigurationManager.AppSettings["SpringWIFIServiceTier"], StringComparison.OrdinalIgnoreCase))
                                            {
                                                upgradeVASClassDic.Add(maxPreferenceIndicatorVASClassList[i], dnpProduct);
                                            }
                                        }
                                        else if (dnpProduct.ToUpper().Equals(ConfigurationManager.AppSettings["CAService"]))
                                        {
                                            //stuck order fix
                                            if(!string.IsNullOrEmpty(dnpProductVasSupplierCode))
                                            {
                                                if (Convert.ToInt32(maxPreferenceIndicatorVASClassList[i].SupplierCode) > Convert.ToInt32(dnpProductVasSupplierCode.ToString()))
                                                {
                                                    upgradeVASClassDic.Add(maxPreferenceIndicatorVASClassList.Where(pvc => pvc.VasProductFamily.Equals(ConfigurationManager.AppSettings["VASContentAnyWhereProductFamily"])).FirstOrDefault(), "ACTIVE");
                                                }
                                            }
                                        }
                                        else if (dnpProduct.ToUpper().Equals(ConfigurationManager.AppSettings["MMAService"]))
                                        {
                                            int licenseCount = dnpVasProductId.EndsWith("015") ? 15 : 2 ;
                                            if (Convert.ToInt32(maxPreferenceIndicatorVASClassList[i].ActivationCardinality) > licenseCount)
                                            {
                                                upgradeVASClassDic.Add(maxPreferenceIndicatorVASClassList.Where(pvc => (pvc.VasProductFamily.Equals(ConfigurationManager.AppSettings["NPPService"],StringComparison.OrdinalIgnoreCase) && pvc.vasSubType.Equals("common",StringComparison.OrdinalIgnoreCase))).FirstOrDefault(), "ACTIVE");
                                            }
                                        }
                                    }
                                    else if (cfProvision && dnpProduct.ToUpper().Equals(ConfigurationManager.AppSettings["CFservice"]) && dnpProductDic[ConfigurationManager.AppSettings["CFservice"].ToString()].Equals("ACTIVE", StringComparison.OrdinalIgnoreCase)
                                        && (maxPreferenceIndicatorVASClassList[i].VasProductFamily + ":" + maxPreferenceIndicatorVASClassList[i].vasSubType).Equals(dnpProduct, StringComparison.OrdinalIgnoreCase))
                                    {
                                        provisionVASClassDic.Add(maxPreferenceIndicatorVASClassList[i], dnpProductDic[dnpProduct]);
                                        break;
                                    }
                                }
                            }
                            else if (activationProducts.ToList().Contains(maxPreferenceIndicatorVASClassList[i].VasProductFamily) && !cfProvision)
                            {
                                provisionVASClassDic.Add(maxPreferenceIndicatorVASClassList[i], "");
                            }
                        }
                    }
                    else
                    {
                        throw new MdmException("VAS products are not mapped in MDM for vas class(es) sent in the request");
                    }
                    //it's chop code to insert bbsid to chop service in any case. 
                    if (isCHOServiceExists && !string.IsNullOrEmpty(BBSID) && !isBBSIDAlreadyExists)
                    {
                        E2ETransaction e2eData = null;
                        MessagesFile Mf = new MessagesFile(ConfigurationManager.AppSettings["logMessagesFilename"].ToString());
                        if (requestOrderRequest.StandardHeader != null && requestOrderRequest.StandardHeader.E2e != null && requestOrderRequest.StandardHeader.E2e.E2EDATA != null && !String.IsNullOrEmpty(requestOrderRequest.StandardHeader.E2e.E2EDATA.ToString()))
                            e2eData = new E2ETransaction(requestOrderRequest.StandardHeader.E2e.E2EDATA.ToString(), ConfigurationManager.AppSettings["ProjectID"].ToString(), ConfigurationManager.AppSettings["Component"].ToString(), Mf);

                        BTCONSUMERBROADBANDProcessor choProcessor = new BTCONSUMERBROADBANDProcessor();
                        if (isBBSIDChangeRequired)
                        {
                            choProcessor.ModifyBBSIDMapper(requestOrderRequest, ref e2eData);
                        }
                        else
                        {
                            if (choProcessor.IsInsertBBSIDTOCHOServiceRole(requestOrderRequest, billAccNumber, BBSID, identity, identityDomain, orderKey, ref downStreamError, ref e2eData))
                            {
                                //ignore
                            }
                        }
                        // to insert/modify the RBSID value in CHOP service
                        if (requestOrderRequest.Order.OrderItem.ToList().Exists(oi => oi.Action.Code.Equals("Create", StringComparison.OrdinalIgnoreCase) && oi.Instance.ToList().Exists(inst => inst.InstanceCharacteristic.ToList().Exists(instchar => instchar.Name.Equals("HCStatus", StringComparison.OrdinalIgnoreCase) && instchar.Value.Equals("Active", StringComparison.OrdinalIgnoreCase))))
                             && requestOrderRequest.Order.OrderItem.ToList().Exists(oi => oi.Action.Code.Equals("Create", StringComparison.OrdinalIgnoreCase) && oi.Instance.ToList().Exists(inst => inst.InstanceCharacteristic.ToList().Exists(instchar => instchar.Name.Equals("ServiceCode", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(instchar.Value) && instchar.Value.Equals("BT_CONSUMER_BROADBAND", StringComparison.OrdinalIgnoreCase))))
                             && requestOrderRequest.Order.OrderItem.ToList().Exists(oi => oi.Action.Code.Equals("Create", StringComparison.OrdinalIgnoreCase) && oi.Instance.ToList().Exists(inst => inst.InstanceCharacteristic.ToList().Exists(instchar => instchar.Name.Equals("RBSID", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(instchar.Value)))))
                        {
                            // BTCONSUMERBROADBANDProcessor choProcessor = new BTCONSUMERBROADBANDProcessor();
                            choProcessor.ModifyRBSIDMapperforCHOP(requestOrderRequest, ref e2eData);
                        }
                    }
                    // to insert/modify the RBSID value in CHOP service
                    else if (isCHOServiceExists
                          && requestOrderRequest.Order.OrderItem.ToList().Exists(oi => oi.Action.Code.Equals("Create", StringComparison.OrdinalIgnoreCase) && oi.Instance.ToList().Exists(inst => inst.InstanceCharacteristic.ToList().Exists(instchar => instchar.Name.Equals("HCStatus", StringComparison.OrdinalIgnoreCase) && instchar.Value.Equals("Active", StringComparison.OrdinalIgnoreCase))))
                          && requestOrderRequest.Order.OrderItem.ToList().Exists(oi => oi.Action.Code.Equals("Create", StringComparison.OrdinalIgnoreCase) && oi.Instance.ToList().Exists(inst => inst.InstanceCharacteristic.ToList().Exists(instchar => instchar.Name.Equals("ServiceCode", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(instchar.Value) && instchar.Value.Equals("BT_CONSUMER_BROADBAND", StringComparison.OrdinalIgnoreCase))))
                          && requestOrderRequest.Order.OrderItem.ToList().Exists(oi => oi.Action.Code.Equals("Create", StringComparison.OrdinalIgnoreCase) && oi.Instance.ToList().Exists(inst => inst.InstanceCharacteristic.ToList().Exists(instchar => instchar.Name.Equals("RBSID", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(instchar.Value)))))
                    {
                        E2ETransaction e2eData = null;
                        MessagesFile Mf = new MessagesFile(ConfigurationManager.AppSettings["logMessagesFilename"].ToString());
                        if (requestOrderRequest.StandardHeader != null && requestOrderRequest.StandardHeader.E2e != null && requestOrderRequest.StandardHeader.E2e.E2EDATA != null && !String.IsNullOrEmpty(requestOrderRequest.StandardHeader.E2e.E2EDATA.ToString()))
                            e2eData = new E2ETransaction(requestOrderRequest.StandardHeader.E2e.E2EDATA.ToString(), ConfigurationManager.AppSettings["ProjectID"].ToString(), ConfigurationManager.AppSettings["Component"].ToString(), Mf);

                        BTCONSUMERBROADBANDProcessor choProcessor = new BTCONSUMERBROADBANDProcessor();
                        choProcessor.ModifyRBSIDMapperforCHOP(requestOrderRequest, ref e2eData);                        
                    }
                    // CA Bolt ON || WIFI Upgrade
                    if ((upgradeVASClassDic.Count > 0) && (upgradeVASClassDic != null))
                    {
                        VASClassDic = upgradeVASClassDic;
                        foreach (OrderItem orderItem in requestOrderRequest.Order.OrderItem)
                        {
                            foreach (ProductVasClass pvc in upgradeVASClassDic.Keys)
                            {
                                if (pvc.VasClass.Equals(orderItem.Instance[0].InstanceCharacteristic.ToList().Where(insChar => insChar.Name.Equals("vasclass", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value, StringComparison.OrdinalIgnoreCase))
                                {
                                    orderItem.Status = Settings1.Default.AcceptedStatus;
                                }
                            }
                        }
                    }
                    else if (((provisionVASClassDic.Count > 0) && (provisionVASClassDic != null)))
                    {
                        VASClassDic = provisionVASClassDic;
                        requestOrderRequest = VASActivation(requestOrderRequest, provisionVASClassDic, ref orderType);
                    }
                    else
                    {                        
                        if (cfProvision)
                        {
                            if (!maxPreferenceIndicatorVASClassList.ToList().Exists(maxcls => maxcls.VasProductFamily.Equals(ConfigurationManager.AppSettings["CFProdCode"], StringComparison.OrdinalIgnoreCase)))
                            {
                                throw new MdmException("ContentFiltering is not mapped in MDM for vas class(es) sent in the request");
                            }
                            else if (!dnpProductDic.ContainsKey(ConfigurationManager.AppSettings["CFService"]))
                            {
                                throw new DnpException("Ignored as ContentFiltering Service is not present in DnP for the given BillAccountNumber");
                            }
                            else if (!dnpProductDic[ConfigurationManager.AppSettings["CFService"].ToString()].Equals("Active", StringComparison.OrdinalIgnoreCase))
                            {
                                throw new DnpException("Ignored as ContentFiltering Service is not in Active state for the given BillAccountNumber");
                            }
                        }
                        else if (maxPreferenceIndicatorVASClassList.ToList().Exists(maxcls => maxcls.VasProductFamily.Equals(ConfigurationManager.AppSettings["WifiProdCode"], StringComparison.OrdinalIgnoreCase) || maxcls.VasProductFamily.Equals(ConfigurationManager.AppSettings["DanteEMailProdCode"], StringComparison.OrdinalIgnoreCase))
                            && ((dnpProductDic.ContainsKey(ConfigurationManager.AppSettings["DanteIdentifierInMDM"]) && dnpProductDic[ConfigurationManager.AppSettings["DanteIdentifierInMDM"]].Equals("ACTIVE", StringComparison.OrdinalIgnoreCase)) || (dnpProductDic.ContainsKey(ConfigurationManager.AppSettings["WifiIdentifier"]) && dnpProductDic[ConfigurationManager.AppSettings["WifiIdentifier"]].Equals("ACTIVE", StringComparison.OrdinalIgnoreCase))))
                        {
                            throw new DnpException("VAS Early Activation product(s) are already ACTIVE in DnP for the given BillAccountNumber");
                        }
                        else
                        {
                            throw new DnpException("No Services exist to Upgrade/Provision for given VasClasse's");
                        }
                    }
                }
                else
                {
                    foreach (OrderItem orderItem in requestOrderRequest.Order.OrderItem)
                    {
                        string billAccountID = string.Empty;
                        ClientServiceInstanceV1[] serviceInstances = null;
                        List<ProductVasClass> productVasClassList = null;
                        List<string> CMPSCodes = null;
                        E2ETransaction e2eData = null;
                        billAccountID = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(insChar => insChar.Name.Equals("billaccountnumber", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                        vasClassID = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(insChar => insChar.Name.Equals("vasclass", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                        vasClassList.Add(vasClassID);

                        productVasClassList = MSEOSaaSMapper.FindAllActiveInactiveProductVasClass(vasClassList, billAccountID, "VAS_BILLINGACCOUNT_ID", "ACTIVE", ref serviceInstances, ref CMPSCodes,ref e2eData);

                        if (productVasClassList.Count > 0)
                        {
                            orderItem.Status = Settings1.Default.AcceptedStatus;
                        }
                        else
                        {
                            orderItem.Status = Settings1.Default.IgnoredStatus;
                        }
                    }
                }

                orderRequest = new OrderRequest();
                orderRequest.StandardHeader = requestOrderRequest.StandardHeader;
                orderRequest.Order = new BT.SaaS.MSEOAdapter.Order();
                orderRequest.Order.Action = requestOrderRequest.Order.Action;
                orderRequest.Order.OrderDate = requestOrderRequest.Order.OrderDate;
                orderRequest.Order.OrderIdentifier = requestOrderRequest.Order.OrderIdentifier;

                List<OrderItem> orderItemList = new List<OrderItem>(); ;
                foreach (OrderItem orderItem in requestOrderRequest.Order.OrderItem)
                {
                    if (orderItem.Status != null && orderItem.Status.Equals(Settings1.Default.AcceptedStatus, StringComparison.OrdinalIgnoreCase))
                    {
                        orderItemList.Add(orderItem);

                    }
                }
                orderRequest.Order.OrderItem = orderItemList.ToArray();
                orderRequest.Order.Status = requestOrderRequest.Order.Status;
            }
            catch (MdmException Mdmexception)
            {
                throw Mdmexception;
            }
            catch (DnpException DnPexception)
            {
                throw DnPexception;
            }
            finally
            {
                vasClassList = null;
                vasDefinitionList = null;
                dnpProductList = null;
                maxPreferenceIndicatorVASClassList = null;
                dnpProductDic.Clear();
            }
            return orderRequest;
        }

        #endregion

        #region VASEarlyActivation

        public static OrderRequest VASActivation(OrderRequest requestOrderRequest, Dictionary<ProductVasClass, string> provisionVASDic, ref string orderType)
        {
            bool isEmailNotExist = false;
            bool isReservationIDNotFormat = false;
            bool isSwitchLegacy = false;
            try
            {
                if ((provisionVASDic != null) && (provisionVASDic.Count > 0))
                {
                    foreach (ProductVasClass pvc in provisionVASDic.Keys)
                    {
                        if (pvc.VasProductFamily.Equals(ConfigurationManager.AppSettings["WifiProdCode"], StringComparison.OrdinalIgnoreCase))
                        {
                            if (ConfigurationManager.AppSettings["WifiSwitch"].Equals("ON", StringComparison.OrdinalIgnoreCase))
                            {
                                orderType = "Activation";
                                foreach (OrderItem ordrItem in requestOrderRequest.Order.OrderItem)
                                {
                                    ordrItem.Status = Settings1.Default.AcceptedStatus;
                                }
                            }
                        }
                        if (pvc.VasProductFamily.Equals(ConfigurationManager.AppSettings["BondedWifiProdCode"], StringComparison.OrdinalIgnoreCase))
                        {
                            orderType = "Activation";
                            foreach (OrderItem ordrItem in requestOrderRequest.Order.OrderItem)
                            {
                                ordrItem.Status = Settings1.Default.AcceptedStatus;
                            }
                        }                        

                        if (requestOrderRequest.Order.OrderItem[0].Status == null)
                        {
                            if (pvc.VasProductFamily.Equals(ConfigurationManager.AppSettings["DanteEMailProdCode"], StringComparison.OrdinalIgnoreCase))
                            {
                                foreach (OrderItem ordrItem in requestOrderRequest.Order.OrderItem)
                                {
                                    //emailname check for Email activation
                                    if (string.IsNullOrEmpty(provisionVASDic[pvc]) || (!string.IsNullOrEmpty(provisionVASDic[pvc]) && provisionVASDic[pvc].Equals("pending", StringComparison.OrdinalIgnoreCase)))
                                    {
                                        if (ordrItem.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("emailname", StringComparison.OrdinalIgnoreCase)))
                                        {
                                            isEmailNotExist = false;
                                            orderType = MSEOSaaSMapper.AcceptedEmailOrder(ordrItem, pvc.VasProductName.ToString());
                                            if (!String.IsNullOrEmpty(orderType) && (orderType.Equals("Activation") || orderType.Equals("PremiumEmail")))
                                            {
                                                foreach (OrderItem ordItem in requestOrderRequest.Order.OrderItem)
                                                {
                                                    ordItem.Status = Settings1.Default.AcceptedStatus;
                                                }
                                            }
                                            else
                                            {
                                                if (!String.IsNullOrEmpty(orderType) && (orderType.Equals("switchInLegacy")))
                                                {
                                                    isSwitchLegacy = true;
                                                }
                                                else if (!String.IsNullOrEmpty(orderType) && (orderType.Equals("ReservationIDNotFormat")))
                                                {
                                                    isReservationIDNotFormat = true;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            isEmailNotExist = true;
                                        }
                                    }
                                    //Reactivate: no check for emailanme in request
                                    else
                                    {
                                        orderType = MSEOSaaSMapper.AcceptedEmailOrder(ordrItem, pvc.VasProductName.ToString());
                                        if (!String.IsNullOrEmpty(orderType) && (orderType.Equals("Activation")))
                                            //|| orderType.Equals("PremiumEmail")))
                                        {
                                            foreach (OrderItem ordItem in requestOrderRequest.Order.OrderItem)
                                            {
                                                ordItem.Status = Settings1.Default.AcceptedStatus;
                                            }
                                        }
                                        else
                                        {
                                            if (!String.IsNullOrEmpty(orderType) && (orderType.Equals("switchInLegacy")))
                                            {
                                                isSwitchLegacy = true;
                                            }
                                            else if (!String.IsNullOrEmpty(orderType) && (orderType.Equals("ReservationIDNotFormat")))
                                            {
                                                isReservationIDNotFormat = true;
                                            }
                                        }
                                    }
                                }
                            }
                            else if ((pvc.VasProductFamily + ":" + pvc.vasSubType).Equals(ConfigurationManager.AppSettings["CFService"].ToString(), StringComparison.OrdinalIgnoreCase))
                            {
                                orderType = "CFProvision";
                                foreach (OrderItem ordrItem in requestOrderRequest.Order.OrderItem)
                                {
                                    ordrItem.Status = Settings1.Default.AcceptedStatus;
                                }
                            }
                        }
                    }
                    if (requestOrderRequest.Order.OrderItem[0].Status == null)
                    {
                        if (isActiveServiceExist)
                        {
                            throw new DnpException("No Services exist to Upgrade/Provision for given VasClasse's");
                        }
                        else
                        {
                            if (isReservationIDNotFormat)
                            {
                                throw new DnpException("Ignored as the EmaildataStore value is D&P based on Reservation ID format");
                            }
                            else if (isEmailNotExist)
                            {
                                throw new DnpException("Ignored as the EmailName is not present in the request");
                            }
                            else if (isSwitchLegacy)
                            {
                                throw new DnpException("Ignored as the VAS Migration Swtich Vlaue is LEGACY");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return requestOrderRequest;
        }
        #endregion

        #region Bulk Cloud Activation
        public static SAASPE.Order BulkActivationCloud(string BAC, string btoneid, string CustomerId, ref E2ETransaction e2eData)
        {
            CustomerAssetsResponse assetResponse = null;
            List<ProductVasClass> cmpsProdVasClassList = new List<ProductVasClass>();
            List<ProductVasClass> CAProdVasClassList = new List<ProductVasClass>();
            ProductVasClass maxProductClass = new ProductVasClass();
            //SAASPE.Order response = new BT.SaaS.Core.Shared.Entities.Order();
            SAASPE.Order productOrder = new BT.SaaS.Core.Shared.Entities.Order();

            try
            {
                List<string> CMPSCodes = new List<string>();
                //Todo : Add logging Changes for GCA response
                assetResponse = CmpsWrapper.GetCustomerAssets(BAC, ref e2eData);

                if (assetResponse != null)
                {
                    cmpsProdVasClassList = CmpsWrapper.ProductVasClassList(assetResponse, ref CMPSCodes);
                }
                else
                {
                    e2eData.logMessage("Scodes Returned from CMPS", "No asset details retuned from CMPS");
                    throw new DnpException("No asset details retuned from CMPS");
                }

                if (cmpsProdVasClassList.ToList().Exists(prod => prod.VasProductFamily.Equals(ConfigurationManager.AppSettings["VASContentAnyWhereProductFamily"].ToString(), StringComparison.OrdinalIgnoreCase)))
                {
                    maxProductClass = (from vasDefinition in cmpsProdVasClassList
                                       where vasDefinition.VasProductFamily.Equals(ConfigurationManager.AppSettings["VASContentAnyWhereProductFamily"].ToString(), StringComparison.OrdinalIgnoreCase)
                                       orderby vasDefinition.PreferenceIndicator descending
                                       select vasDefinition).FirstOrDefault();
                    if (maxProductClass != null)
                        productOrder = PrepareProductOrderItems(maxProductClass, BAC, CustomerId, btoneid);
                    else
                        throw new DnpException("There are no CA products in MDM for the following Scodes returned from CMPS " + CMPSCodes[0].ToString());
                }
                else
                {
                    throw new DnpException("There are no CA products in MDM for the following Scodes returned from CMPS " + CMPSCodes[0].ToString());
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }

            return productOrder;
        }
        public static SAASPE.Order PrepareProductOrderItems(ProductVasClass productVasClass, string bac, string customerId, string btoneid)
        {
            int productorderItemCount = 0;
            Dictionary<string, string> productNameStatus = new Dictionary<string, string>();
            string Status = string.Empty;
            SAASPE.Order productOrder = new Core.Shared.Entities.Order();

            try
            {
                productOrder.Header.OrderKey = Guid.NewGuid().ToString();
                productOrder.Header.ServiceProviderID = Settings1.Default.ConsumerServiceProviderId;
                productOrder.Header.EffectiveDateTime = System.DateTime.Now;

                productOrder.Header.OrderDateTime = System.DateTime.Now;

                productOrder.Header.Status = BT.SaaS.Core.Shared.Entities.OrderStatusEnum.@new;

                productOrder.Header.Customer = new BT.SaaS.Core.Shared.Entities.Customer();
                productOrder.Header.Customer.Status = BT.SaaS.Core.Shared.Entities.DataStatusEnum.done;

                productOrder.Header.Users = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.User>(1);
                SAASPE.User user = new BT.SaaS.Core.Shared.Entities.User();
                user.Status = BT.SaaS.Core.Shared.Entities.DataStatusEnum.done;
                productOrder.Header.Users.Add(user);

                productOrder.Header.Action = BT.SaaS.Core.Shared.Entities.OrderActionEnum.amend;
                SAASPE.ProductOrderItem productOrderItem = new Core.Shared.Entities.ProductOrderItem();

                productOrderItem = new BT.SaaS.Core.Shared.Entities.ProductOrderItem();
                productOrderItem.Header.Status = BT.SaaS.Core.Shared.Entities.DataStatusEnum.notDone;
                productOrderItem.Header.Quantity = "1";
                productOrderItem.XmlIdKey = "PO-" + productorderItemCount.ToString();

                productOrderItem.Header.OrderItemKey = productorderItemCount.ToString();
                productOrderItem.Header.OrderItemId = productorderItemCount.ToString();

                productOrderItem.Header.ProductCode = productVasClass.VasProductFamily;

                if (productNameStatus.ContainsKey(productOrderItem.Header.ProductCode.ToLower()))
                {
                    Status = productNameStatus[productOrderItem.Header.ProductCode.ToLower()];
                }

                System.Collections.Generic.List<string> inputProductDefinitionList = new System.Collections.Generic.List<string>();
                inputProductDefinitionList.Add(productOrderItem.Header.ProductCode);

                System.Collections.Generic.List<ProductDefinition> productDefinition = MdmWrapper.getSaaSProductDefs(inputProductDefinitionList, Settings1.Default.ConsumerServiceProviderId);

                if (productDefinition.Count == 0)
                {
                    throw new DnpException("No product Definition in MDM");
                }
                else
                {
                    productOrderItem.RoleInstances = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.RoleInstance>(1);
                    productOrderItem.ServiceInstances = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.ServiceInstance>();
                    SAASPE.RoleInstance roleInstance = new BT.SaaS.Core.Shared.Entities.RoleInstance();

                    roleInstance = new BT.SaaS.Core.Shared.Entities.RoleInstance();
                    roleInstance.RoleType = "ADMIN";
                    roleInstance.XmlIdKey = "RI-" + productorderItemCount.ToString();
                    roleInstance.Action = BT.SaaS.Core.Shared.Entities.RoleInstanceActionEnum.add;

                    SAASPE.ServiceInstance serviceInstance = new BT.SaaS.Core.Shared.Entities.ServiceInstance();
                    serviceInstance.ServiceRoles = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.ServiceRole>();
                    SAASPE.ServiceRole serviceRole = new BT.SaaS.Core.Shared.Entities.ServiceRole();
                    serviceRole.roleAttributes = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.Attribute>();

                    roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "VasSubType", Value = productVasClass.vasSubType, action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });
                    roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "Status", Value = Status, action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });
                    roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "PREVIOUSSUPPLIERCODE", Value = productVasClass.SupplierCode, action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });
                    roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "SUPPLIERCODE", Value = productVasClass.SupplierCode, action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });
                    roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "VasProductId", Value = productVasClass.VasProductId, action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });
                    //roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "PromotionIntegrationId", Value = productVasClass.PromotionScode, action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });
                    roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "BillAccountNumber", Value = bac, action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });
                    roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "BtOneId", Value = btoneid, action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });
                    roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "CustomerId", Value = customerId, action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });
                    roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "SERVICETYPE", Value = "VAS", action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });
                    roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "PRODUCTFAMILY", Value = productVasClass.VasProductFamily, action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });
                    roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "ISEXISTINGACCOUNT", Value = true.ToString(), action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });

                    serviceInstance.ServiceRoles.Add(serviceRole);
                    productOrderItem.ServiceInstances.Add(serviceInstance);
                    productOrderItem.RoleInstances.Add(roleInstance);
                }
                productOrder.ProductOrderItems.Add(productOrderItem);
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return productOrder;
        }
        #endregion
    }

}
