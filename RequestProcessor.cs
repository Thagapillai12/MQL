using System;
using System.Configuration;
using System.Linq;
using MSEO = BT.SaaS.MSEOAdapter;
using SaaSNS = BT.SaaS.Core.Shared.Entities;
using BT.SaaS.IspssAdapter;
using BT.SaaS.Core.MDMAPI.Entities.Product;
using System.Collections.Generic;
using BT.SaaS.IspssAdapter.Dnp;
using System.Globalization;
using com.bt.util.logging;

namespace BT.SaaS.MSEOAdapter
{
    public class RequestProcessor
    {
        OrderRequest orderRequest;
        const string BACID_IDENTIFER_NAMEPACE = "VAS_BILLINGACCOUNT_ID";
        const string BTIEMAILID = "BTIEMAILID";

        public static SaaSNS.Order RequestMapper(MSEO.OrderRequest orderRequest, ref E2ETransaction e2eData)
        {
            BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("Mapping BTPlus product request");
            SaaSNS.Order response = new SaaSNS.Order();
            System.DateTime orderDate = new System.DateTime();
            int productorderItemCount = 0;

            response.Header.CeaseReason = orderRequest.SerializeObject();

            response.Header.OrderKey = orderRequest.Order.OrderIdentifier.Value;
            response.Header.ServiceProviderID = Settings1.Default.ConsumerServiceProviderId;

            response.Header.EffectiveDateTime = System.DateTime.Now;
            response.Header.OrderDateTime = System.DateTime.Now;

            response.Header.Status = BT.SaaS.Core.Shared.Entities.OrderStatusEnum.@new;

            response.Header.Customer = new BT.SaaS.Core.Shared.Entities.Customer();
            response.Header.Customer.Status = BT.SaaS.Core.Shared.Entities.DataStatusEnum.done;

            response.Header.Users = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.User>(1);
            SaaSNS.User user = new BT.SaaS.Core.Shared.Entities.User();
            user.Status = BT.SaaS.Core.Shared.Entities.DataStatusEnum.done;
            response.Header.Users.Add(user);

            try
            {
                switch (orderRequest.Order.OrderItem[0].Action.Code.ToLower())
                {
                    case ("create"):
                    case ("add"):
                        response.Header.Action = BT.SaaS.Core.Shared.Entities.OrderActionEnum.provide;
                        break;
                    case ("cancel"):
                        response.Header.Action = BT.SaaS.Core.Shared.Entities.OrderActionEnum.cease;
                        break;
                    default:
                        throw new Exception("Action not supported");
                }

                response.ProductOrderItems = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.ProductOrderItem>(orderRequest.Order.OrderItem.Length);

                foreach (MSEO.OrderItem orderItem in orderRequest.Order.OrderItem)
                {
                    SaaSNS.ProductOrderItem productOrderItem = new SaaSNS.ProductOrderItem();
                    productOrderItem.Header.Status = SaaSNS.DataStatusEnum.notDone;
                    productOrderItem.Header.Quantity = "1";
                    productOrderItem.XmlIdKey = "PO-" + productorderItemCount.ToString();

                    productOrderItem.Header.OrderItemKey = orderItem.Identifier.Id;

                    productOrderItem.Header.ProductCode = orderRequest.Order.OrderItem[0].Specification[0].Identifier.Value1;

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
                            SaaSNS.Attribute attribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                            attribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                            attribute.Name = instanceCharacteristic.Name;
                            attribute.Value = instanceCharacteristic.Value;
                            roleInstance.Attributes.Add(attribute);
                        }
                    }

                    SaaSNS.Attribute BptmE2EDataAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                    BptmE2EDataAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                    BptmE2EDataAttribute.Name = "E2EDATA";
                    if (orderRequest.StandardHeader != null && orderRequest.StandardHeader.E2e != null && orderRequest.StandardHeader.E2e.E2EDATA != null && !String.IsNullOrEmpty(orderRequest.StandardHeader.E2e.E2EDATA.ToString()))
                        BptmE2EDataAttribute.Value = orderRequest.StandardHeader.E2e.E2EDATA.ToString();
                    else
                        BptmE2EDataAttribute.Value = string.Empty;
                    roleInstance.Attributes.Add(BptmE2EDataAttribute);

                    serviceInstance.ServiceRoles.Add(serviceRole);
                    productOrderItem.ServiceInstances.Add(serviceInstance);
                    productOrderItem.RoleInstances.Add(roleInstance);
                    productorderItemCount++;

                    response.ProductOrderItems.Add(productOrderItem);
                }
                return response;
            }
            catch (Exception ex)
            {
                BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter Mapping Premium Email Exception : " + ex.ToString());
                throw ex;
            }
            finally
            {
            }


        }
    }
}
