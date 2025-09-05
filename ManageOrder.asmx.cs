using System;
using System.Web.Services.Description;
using System.Collections;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.Xml.Linq;
using System.ServiceModel;
using System.Collections.Generic;
using BT.SaaS.IspssAdapter.PE;
using SAASPE = BT.SaaS.Core.Shared.Entities;
using BT.SaaS.IspssAdapter;
using MSEO = BT.SaaS.MSEOAdapter;
using BT.SaaS.Core.MDMAPI.Entities.Product;
using BT.SaaS.IspssAdapter.Dnp;
using System.ServiceModel.Channels;
using System.Text;
using opi11.datamodel.ws.fsecure.com_SubscriptionManagement;
using System.IO;
using com.bt.util.logging;
using BT.SaaS.Core.Shared.Entities;
using System.Net;
using System.Xml;

using System.Diagnostics;

namespace BT.SaaS.MSEOAdapter
{
    /// <summary>
    /// Summary description for MSEOService
    /// </summary>


    [WebServiceAttribute(Namespace = "http://capabilities.nat.bt.com/wsdl/ManageOrder")]
    [WebServiceBindingAttribute(Name = "ManageOrderBinding", Namespace = "http://capabilities.nat.bt.com/wsdl/ManageOrder")]

    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class ManageOrder : System.Web.Services.WebService, IManageOrder
    {
        #region Members

        private const string ACTIVE = "ACTIVE";
        private const string MseoReceivedRequest = "MseoReceivedRequest";
        private const string MseoReceivedNotificationSend = "MseoReceivedNotificationSend";
        private const string MseoGotResponseFromFSecure = "GotResponseFromFSecure";
        private const string ExceptionOccurred = "ExceptionOccurred";
        string BillAccountNumber = string.Empty;
        string BtoneId = string.Empty;
        string orderFrom = string.Empty;
        E2ETransaction Mye2etxn;
        private const string OrderStatus = "OrderStatus";
        #endregion

        [WebMethodAttribute()]
        [SoapDocumentMethodAttribute("http://capabilities.nat.bt.com/wsdl/ManageOrder#orderRequest", OneWay = true, Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Bare, Binding = "ManageOrderBinding")]
        public void orderRequest([System.Xml.Serialization.XmlElementAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15", ElementName = "requestOrderRequest")] OrderRequest requestOrderRequest)
        {
            MSEOOrderNotification notification = this.PrepareAndSubmitOrder(requestOrderRequest);
        }

        [WebMethod]
        public int GetOrderStatus(string orderKey)
        {
            LoggingFramework.initialise(ConfigurationManager.AppSettings["logHandlingFilename"].ToString());
            MessagesFile Mf = new MessagesFile(ConfigurationManager.AppSettings["logMessagesFilename"].ToString());
            Mye2etxn = new E2ETransaction(ConfigurationManager.AppSettings["E2EData"].ToString(), ConfigurationManager.AppSettings["ProjectID"].ToString(), ConfigurationManager.AppSettings["Component"].ToString(), Mf);
            Mye2etxn.logMessage(MseoReceivedRequest, "Order Key<" + orderKey + ">;");
            Mye2etxn.startInboundCall();
            int status = 0;
            if (orderKey != null)
                status = MSEOInboundQueueDAL.GetOrderStatus(orderKey);
            Mye2etxn.endInboundCall();
            Mye2etxn.logMessage(MseoReceivedNotificationSend, "Order Key<" + orderKey + ">; Status<" + status + ">;");

            return status;
        }

        [WebMethodAttribute()]
        [SoapDocumentMethodAttribute("http://capabilities.nat.bt.com/wsdl/ManageOrder#submitOrderRequest", Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Bare, Binding = "ManageOrderBinding")]
        public MSEO.OrderNotification submitOrderRequest([System.Xml.Serialization.XmlElementAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15", ElementName = "requestOrderRequest")] OrderRequest requestOrderRequest, string orderFrom)
        {
            BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("Order " + requestOrderRequest.Order.OrderIdentifier.Value + "reached to MSEOAdapter at: " + System.DateTime.Now);
            if (string.IsNullOrEmpty(requestOrderRequest.StandardHeader.ServiceAddressing.From))
            {
                if (!string.IsNullOrEmpty(orderFrom) && orderFrom.Equals(Settings1.Default.NativeClientString))
                    requestOrderRequest.StandardHeader.ServiceAddressing.From = ConfigurationManager.AppSettings["DEVAPIServiceAddress"].ToString();
                else
                    requestOrderRequest.StandardHeader.ServiceAddressing.From = Settings1.Default.BTComstring;
            }
            MSEOOrderNotification notification = this.PrepareAndSubmitOrder(requestOrderRequest);
            BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("Order " + requestOrderRequest.Order.OrderIdentifier.Value + "notification send at: " + System.DateTime.Now);
            return notification.NotificationResponse;
        }

        private MSEOOrderNotification PrepareAndSubmitOrder(OrderRequest requestOrderRequest)
        {
            LoggingFramework.initialise(ConfigurationManager.AppSettings["logHandlingFilename"].ToString());
            MessagesFile Mf = new MessagesFile(ConfigurationManager.AppSettings["logMessagesFilename"].ToString());

            if (requestOrderRequest.StandardHeader != null && requestOrderRequest.StandardHeader.E2e != null && requestOrderRequest.StandardHeader.E2e.E2EDATA != null && !String.IsNullOrEmpty(requestOrderRequest.StandardHeader.E2e.E2EDATA.ToString()))
                Mye2etxn = new E2ETransaction(requestOrderRequest.StandardHeader.E2e.E2EDATA.ToString(), ConfigurationManager.AppSettings["ProjectID"].ToString(), ConfigurationManager.AppSettings["Component"].ToString(), Mf);
            else
                Mye2etxn = new E2ETransaction(ConfigurationManager.AppSettings["E2EData"].ToString(), ConfigurationManager.AppSettings["ProjectID"].ToString(), ConfigurationManager.AppSettings["Component"].ToString(), Mf);

            Mye2etxn.startInboundCall();
            Mye2etxn.setApplicationInfo("Order Key : " + requestOrderRequest.Order.OrderIdentifier.Value);
            requestOrderRequest.StandardHeader.E2e = new E2E();
            requestOrderRequest.StandardHeader.E2e.E2EDATA = Mye2etxn.toString();
            if (requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("billaccountnumber", StringComparison.OrdinalIgnoreCase)) && !String.IsNullOrEmpty(requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("billaccountnumber", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value))
            {
                BillAccountNumber = requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("billaccountnumber", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
            }

            if (requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("btoneid", StringComparison.OrdinalIgnoreCase)) && !String.IsNullOrEmpty(requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("btoneid", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value))
            {
                BtoneId = requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("btoneid", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
            }

            if (requestOrderRequest.StandardHeader != null && requestOrderRequest.StandardHeader.ServiceAddressing != null && requestOrderRequest.StandardHeader.ServiceAddressing.From != null && !String.IsNullOrEmpty(requestOrderRequest.StandardHeader.ServiceAddressing.From))
                orderFrom = requestOrderRequest.StandardHeader.ServiceAddressing.From.ToString();


            const string INITIAL_XML_STRING = "INITIAL XML";
            const string BAD_REQUEST_XML_STRING = "NO ORDER NUMBER";

            if (requestOrderRequest.Order != null)
            {
                string orderKey = requestOrderRequest.Order.OrderIdentifier.Value;
                InboundQueueDAL.QueueRawXML(orderKey, 2, requestOrderRequest.SerializeObject(), INITIAL_XML_STRING, INITIAL_XML_STRING);
            }
            else
            {
                InboundQueueDAL.QueueRawXML("Bad Request from MSEO", 2, requestOrderRequest.SerializeObject(), INITIAL_XML_STRING, BAD_REQUEST_XML_STRING);
            }

            MSEOOrderNotification notification = null;
            SAASPE.StandardHeader standardHeader = new BT.SaaS.Core.Shared.Entities.StandardHeader();

            standardHeader.serviceAddressing = new BT.SaaS.Core.Shared.Entities.ServiceAddressing();
            standardHeader.serviceAddressing.from = requestOrderRequest.StandardHeader.ServiceAddressing.From;
            bool isAck = true;
            try
            {
                BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter : Request recived : " + requestOrderRequest.Order.OrderIdentifier.Value);

                string errorString = string.Empty;
                notification = new MSEOOrderNotification(requestOrderRequest);

                SAASPE.Order order = null;

                if (RequestValidator.ValidateMSEORequest(requestOrderRequest, ref errorString))
                {
                    Mye2etxn.logMessage(MseoReceivedRequest, "Product<" + requestOrderRequest.Order.OrderItem[0].Specification[0].Identifier.Value1 + ">; Order Key<" + requestOrderRequest.Order.OrderIdentifier.Value + ">; BAC<" + BillAccountNumber + ">; BTONEID<" + BtoneId + ">; Order From<" + orderFrom + ">;");

                    // Flow for Ordering Bolt-On's
                    if (VASRequestValidator.ValidateVASRequest(requestOrderRequest, ref errorString))
                    {
                        int orderItemWithVasCreateCount = 0;
                        string orderType = string.Empty;
                        ClientServiceInstanceV1[] services = null;
                        Dictionary<ProductVasClass, string> provisionVASClassDic = new Dictionary<ProductVasClass, string>();
                        var vasCreateAction = from orderItem in requestOrderRequest.Order.OrderItem
                                              where orderItem.Action.Code.Equals("create", StringComparison.OrdinalIgnoreCase)
                                              select orderItem;
                        string cfsid = string.Empty;

                        orderItemWithVasCreateCount = vasCreateAction.Count();

                        if (orderItemWithVasCreateCount == requestOrderRequest.Order.OrderItem.Count())
                        {
                            orderType = "ofscreate";
                            OrderRequest acceptedOrderRequest = VASRequestProcessor.VASCreateRequest(requestOrderRequest, ref orderType, ref provisionVASClassDic, ref services, ref cfsid);
                            if (acceptedOrderRequest.Order.OrderItem.Length > 0)
                            {
                                order = MSEOSaaSMapper.MapUpgradeRequest(acceptedOrderRequest, provisionVASClassDic, services);
                            }
                            else
                            {
                                //Preparing order object with empty Product Order Items so that Order will be ignored when it is submitted to PE.
                                order = new BT.SaaS.Core.Shared.Entities.Order();
                                order.ProductOrderItems = new List<BT.SaaS.Core.Shared.Entities.ProductOrderItem>();
                            }
                        }
                    }
                    else
                    {
                        order = MSEOSaaSMapper.MapRequest(requestOrderRequest);
                    }

                    if (order.ProductOrderItems.Count > 0)
                    {
                        using (OrderClient orderClient = new OrderClient())
                        {
                            SAASPE.OrderResponse response = orderClient.SubmitOrder(standardHeader, order);

                            if (response == null)
                            {
                                throw new Exception("Provisioning Engine returned a null response");
                            }
                            else
                            {
                                BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter : The Order " + response.order.Header.OrderID + " was placed successfully");
                                notification.sendNotification(response.result.result, isAck, string.Empty, string.Empty, ref Mye2etxn);
                            }
                        }
                    }
                    else
                    {
                        notification.sendNotification(false, isAck, Settings1.Default.InvalideRequestCode, "The Product(s) in the request are Ignored because there is no defination for this product in SaaS.", ref Mye2etxn);
                        BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter : The Order " + requestOrderRequest.Order.OrderIdentifier.Value + " do not have SaaS products");
                    }
                }
                else
                {
                    InboundQueueDAL.UpdateQueueRawXML(Settings1.Default.InvalideRequestCode, errorString, requestOrderRequest.Order.OrderIdentifier.Value);
                    notification.sendNotification(false, isAck, Settings1.Default.InvalideRequestCode, errorString, ref Mye2etxn);
                }
                Mye2etxn.logMessage(MseoReceivedNotificationSend, "Product<" + requestOrderRequest.Order.OrderItem[0].Specification[0].Identifier.Value1 + ">; Order Key<" + requestOrderRequest.Order.OrderIdentifier.Value + ">; BAC<" + BillAccountNumber + ">; BTONEID<" + BtoneId + ">; Order From<" + orderFrom + ">;");
            }

            catch (DnpException DnPexception)
            {
                InboundQueueDAL.UpdateQueueRawXML(Settings1.Default.IgnoredStatus, DnPexception.Message.ToString(), requestOrderRequest.Order.OrderIdentifier.Value);

                foreach (OrderItem orderItem in requestOrderRequest.Order.OrderItem)
                {
                    orderItem.Status = "Ignored";
                }
                Mye2etxn.systemError(ExceptionOccurred, "Product<" + requestOrderRequest.Order.OrderItem[0].Specification[0].Identifier.Value1 + ">; Order Key<" + requestOrderRequest.Order.OrderIdentifier.Value + ">; BAC<" + BillAccountNumber + ">; BTONEID<" + BtoneId + ">; Order From<" + orderFrom + ">; Root Cause <" + DnPexception.Message + ">;");
                notification.sendNotification(false, isAck, "001", DnPexception.Message.ToString(), ref Mye2etxn);
                BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEO-PrepareAndSubmitOrder DNP exception : " + DnPexception.ToString() + " for order identifier :" + requestOrderRequest.Order.OrderIdentifier.Value);
            }

            catch (Exception ex)
            {
                Mye2etxn.systemError(ExceptionOccurred, "Product<" + requestOrderRequest.Order.OrderItem[0].Specification[0].Identifier.Value1 + ">; Order Key<" + requestOrderRequest.Order.OrderIdentifier.Value + ">; BAC<" + BillAccountNumber + ">; BTONEID<" + BtoneId + ">; Order From<" + orderFrom + ">; Root Cause <" + ex.Message + ">;");
                BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter exception : " + ex.ToString());
            }
            notification.NotificationResponse.StandardHeader.E2e.E2EDATA = Mye2etxn.toString();
            Mye2etxn.endInboundCall();
            return notification;
        }

        [WebMethod]
        public void orderNotification(OrderNotification orderNotificationRequest)
        {

        }
        [WebMethodAttribute()]
        [SoapDocumentMethodAttribute("http://capabilities.nat.bt.com/wsdl/ManageOrder#AccountHolderTrust", Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Bare, Binding = "ManageOrderBinding")]
        public MSEO.OrderNotification AccountHolderTrust([System.Xml.Serialization.XmlElementAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15", ElementName = "requestOrderRequest")]OrderRequest requestOrderRequest)
        {
            LoggingFramework.initialise(ConfigurationManager.AppSettings["logHandlingFilename"].ToString());
            MessagesFile Mf = new MessagesFile(ConfigurationManager.AppSettings["logMessagesFilename"].ToString());
            MSEOOrderNotification notification = null;
            const string ExecuteRequest = "ExecuteRequest";
            const string INITIAL_XML_STRING = "INITIAL XML";
            const string BAD_REQUEST_XML_STRING = "NO ORDER NUMBER";
            string errorString = string.Empty;
            string AcctrustMethod = string.Empty;
            string orderKey = string.Empty;
            string product = string.Empty;
            bool isEECustomer = false;

            try
            {
                notification = new MSEOOrderNotification(requestOrderRequest);

                if (requestOrderRequest.StandardHeader != null && requestOrderRequest.StandardHeader.E2e != null && requestOrderRequest.StandardHeader.E2e.E2EDATA != null && !String.IsNullOrEmpty(requestOrderRequest.StandardHeader.E2e.E2EDATA.ToString()))
                    Mye2etxn = new E2ETransaction(requestOrderRequest.StandardHeader.E2e.E2EDATA.ToString(), ConfigurationManager.AppSettings["ProjectID"].ToString(), ConfigurationManager.AppSettings["Component"].ToString(), Mf);
                else
                    Mye2etxn = new E2ETransaction(ConfigurationManager.AppSettings["E2EData"].ToString(), ConfigurationManager.AppSettings["ProjectID"].ToString(), ConfigurationManager.AppSettings["Component"].ToString(), Mf);

                Mye2etxn.startInboundCall();

                Mye2etxn.setApplicationInfo("Order Key : " + requestOrderRequest.Order.OrderIdentifier.Value);

                System.DateTime ActivityStartTime = System.DateTime.Now;
                string bptmTxnId = BPTMHelper.GetTxnIdFromE2eData(Mye2etxn.toString());
                Guid guid = new Guid(requestOrderRequest.Order.OrderIdentifier.Value);

                requestOrderRequest.StandardHeader.E2e = new E2E();
                requestOrderRequest.StandardHeader.E2e.E2EDATA = Mye2etxn.toString();

                SAASPE.StandardHeader standardHeader = new BT.SaaS.Core.Shared.Entities.StandardHeader();
                standardHeader.serviceAddressing = new BT.SaaS.Core.Shared.Entities.ServiceAddressing();
                standardHeader.serviceAddressing.from = requestOrderRequest.StandardHeader.ServiceAddressing.From;

                foreach (InstanceCharacteristic inschar in requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic)
                {
                    if ((inschar.Name.Equals("BillAccountNumber", StringComparison.OrdinalIgnoreCase)) && !(string.IsNullOrEmpty(inschar.Value)))
                    {
                        BillAccountNumber = inschar.Value;
                    }
                    else if ((inschar.Name.Equals("btoneid", StringComparison.OrdinalIgnoreCase)) && !(string.IsNullOrEmpty(inschar.Value)))
                    {
                        BtoneId = inschar.Value;
                    }
                    else if ((inschar.Name.Equals("accounttrustmethod", StringComparison.OrdinalIgnoreCase)) && !(string.IsNullOrEmpty(inschar.Value)))
                    {
                        AcctrustMethod = inschar.Value;
                    }
                    else if ((inschar.Name.Equals("Organisation", StringComparison.OrdinalIgnoreCase)) && !(string.IsNullOrEmpty(inschar.Value)))
                    {
                        if (!string.IsNullOrEmpty(inschar.Value))
                            if (inschar.Value.Equals("EEConsumer", StringComparison.OrdinalIgnoreCase))
                                isEECustomer = true;
                    }
                }
                if (requestOrderRequest.StandardHeader != null && requestOrderRequest.StandardHeader.ServiceAddressing != null && requestOrderRequest.StandardHeader.ServiceAddressing.From != null && !String.IsNullOrEmpty(requestOrderRequest.StandardHeader.ServiceAddressing.From))
                {
                    orderFrom = requestOrderRequest.StandardHeader.ServiceAddressing.From.ToString();
                }
                if (requestOrderRequest.Order.OrderItem[0].Specification[0] != null && requestOrderRequest.Order.OrderItem[0].Specification[0].Identifier != null && !string.IsNullOrEmpty(requestOrderRequest.Order.OrderItem[0].Specification[0].Identifier.Value1))
                {
                    product = requestOrderRequest.Order.OrderItem[0].Specification[0].Identifier.Value1;
                }

                LogHelper.LogActivityDetails(bptmTxnId, guid, "Order reached to MSEO", System.DateTime.Now - ActivityStartTime, "AHTLogTrace", guid, requestOrderRequest.SerializeObject());

                if (requestOrderRequest.Order != null)
                {
                    orderKey = requestOrderRequest.Order.OrderIdentifier.Value;
                    InboundQueueDAL.QueueRawXML(orderKey, 2, requestOrderRequest.SerializeObject(), INITIAL_XML_STRING, INITIAL_XML_STRING);
                }
                else
                {
                    InboundQueueDAL.QueueRawXML("Bad Request to MSEO", 2, requestOrderRequest.SerializeObject(), INITIAL_XML_STRING, BAD_REQUEST_XML_STRING);
                }

                if (VASRequestValidator.ValidateVASActivationRequest(requestOrderRequest, ref errorString))
                {
                    Mye2etxn.logMessage(MseoReceivedRequest, "Product<" + product + ">; Order Key<" + orderKey + ">; BAC<" + BillAccountNumber + ">; BTONEID<" + BtoneId + ">; Order From<" + orderFrom + ">; AccountTrustMethod<" + AcctrustMethod + ">;");
                    EventMapper.RaiseEvnt(requestOrderRequest, ExecuteRequest, OrderStatusEnum.pending, string.Empty);
                    AHTRequestProcessor ahtProcessor = new AHTRequestProcessor();
                    ahtProcessor.AHTRequestMapper(requestOrderRequest, BillAccountNumber, BtoneId, AcctrustMethod, ref Mye2etxn, ref notification, isEECustomer, ActivityStartTime);
                }
                else
                {
                    InboundQueueDAL.UpdateQueueRawXML(Settings1.Default.InvalideRequestCode, errorString, requestOrderRequest.Order.OrderIdentifier.Value);
                    notification.sendNotification(false, false, Settings1.Default.InvalideRequestCode, errorString, ref Mye2etxn);
                }
            }
            catch (DnpException exp)
            {
                Mye2etxn.systemError(ExceptionOccurred, "Product<" + product + ">; Order Key<" + orderKey + ">; BAC<" + BillAccountNumber + ">; BTONEID<" + BtoneId + ">; Order From<" + orderFrom + ">; Root Cause <" + exp.Message + ">;");
                InboundQueueDAL.UpdateQueueRawXML(Settings1.Default.FailedStatus, exp.Message.ToString(), requestOrderRequest.Order.OrderIdentifier.Value);

                foreach (OrderItem orderItem in requestOrderRequest.Order.OrderItem)
                {
                    orderItem.Status = "777";
                }

                notification.sendNotification(false, false, "777", exp.Message, ref Mye2etxn);
            }
            catch (Exception ex)
            {
                Mye2etxn.systemError(ExceptionOccurred, "Product<" + product + ">; Order Key<" + orderKey + ">; BAC<" + BillAccountNumber + ">; BTONEID<" + BtoneId + ">; Order From<" + orderFrom + ">; Root Cause <" + ex.Message + ">;");
                InboundQueueDAL.UpdateQueueRawXML(Settings1.Default.FailedStatus, ex.Message.ToString(), requestOrderRequest.Order.OrderIdentifier.Value);
                //raise event
                EventMapper.RaiseEvnt(requestOrderRequest, ExceptionOccurred, OrderStatusEnum.failed, ex.Message);
                foreach (OrderItem orderItem in requestOrderRequest.Order.OrderItem)
                {
                    orderItem.Status = "002";
                }

                notification.sendNotification(false, false, "002", ex.Message, ref Mye2etxn);
            }
            BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("Order " + orderKey + "notification send at: " + System.DateTime.Now);
            Mye2etxn.endInboundCall();
            return notification.NotificationResponse;
        }


        [WebMethodAttribute()]
        [SoapDocumentMethodAttribute("http://capabilities.nat.bt.com/wsdl/ManageOrder#vasServiceEligibilityRequest", Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Bare, Binding = "ManageOrderBinding")]
        public VASServiceEligibilityResponse VASServiceEligibility([System.Xml.Serialization.XmlElementAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15", ElementName = "vasServiceEligibilityRequest")]VASServiceEligibilityRequest request)
        {
            VASServiceEligibilityResponse vasServiceEligibilityResponse = new VASServiceEligibilityResponse();
            vasServiceEligibilityResponse.StandardHeader = new StandardHeaderBlock();
            vasServiceEligibilityResponse.StandardHeader.E2e = new E2E();
            vasServiceEligibilityResponse.Result = new Result();
            string errorString = string.Empty;
            string serviceType = string.Empty;
            string OrderItemInfo = string.Empty; string BPTMTxnId = string.Empty;
            System.DateTime ActivityStartTime = System.DateTime.Now;
            Guid guid = Guid.NewGuid();
            LoggingFramework.initialise(ConfigurationManager.AppSettings["logHandlingFilename"].ToString());
            MessagesFile Mf = new MessagesFile(ConfigurationManager.AppSettings["logMessagesFilename"].ToString());

            if (request.standardHeader != null && request.standardHeader.E2e != null && request.standardHeader.E2e.E2EDATA != null && !String.IsNullOrEmpty(request.standardHeader.E2e.E2EDATA.ToString()))
                Mye2etxn = new E2ETransaction(request.standardHeader.E2e.E2EDATA.ToString(), ConfigurationManager.AppSettings["ProjectID"].ToString(), ConfigurationManager.AppSettings["Component"].ToString(), Mf);
            else
                Mye2etxn = new E2ETransaction(ConfigurationManager.AppSettings["E2EData"].ToString(), ConfigurationManager.AppSettings["ProjectID"].ToString(), ConfigurationManager.AppSettings["Component"].ToString(), Mf);

            BPTMTxnId = BPTMHelper.GetTxnIdFromE2eData(Mye2etxn.toString());
            LogHelper.LogWFEventDetails(BPTMTxnId, "VASSrvcEligibility", guid, "Request Received", System.DateTime.Now - ActivityStartTime, request.BACID);

            try
            {
                Mye2etxn.startInboundCall();
                if (request.BACID != null && string.IsNullOrEmpty(request.BACID))
                    BillAccountNumber = request.BACID;

                foreach (VASClass vsclass in request.VASClassList)
                {
                    if (vsclass != null && vsclass.Value != null)
                        OrderItemInfo += "Vas Class <" + vsclass.Value + ">;";
                }

                if (VASRequestValidator.ValidateVASEligibilityRequest(request, ref errorString))
                {
                    Mye2etxn.logMessage(MseoReceivedRequest, "BAC<" + BillAccountNumber + ">; " + OrderItemInfo);
                    List<string> vasClassIDList = new List<string>();

                    foreach (VASClass vasClass in request.VASClassList)
                    {
                        vasClassIDList.Add(vasClass.Value);
                    }
                    if (request.ServiceType != null)
                    {
                        if (request.ServiceType.Equals("ACTIVATED", StringComparison.OrdinalIgnoreCase))
                        {
                            serviceType = ACTIVE;
                        }
                        else
                        {
                            serviceType = request.ServiceType;
                        }
                    }
                    else
                    {
                        serviceType = string.Empty;
                    }

                    List<ProductVasClass> vasDefinitionList = FindAllActiveInactiveProductVasClass(vasClassIDList, request.BACID, "VAS_BILLINGACCOUNT_ID", serviceType, BPTMTxnId);

                    if (vasClassIDList.Count > 1)
                    {
                        if (vasDefinitionList != null && vasDefinitionList.Count > 0)
                        {
                            vasServiceEligibilityResponse.Result.result = true;
                            vasServiceEligibilityResponse.Result.errorCode = 0000;
                            vasServiceEligibilityResponse.Result.errorDescritpion = "Transaction Success";
                            vasServiceEligibilityResponse.VASServiceList = new List<VASService>();
                            VASService vasService = null;
                            List<ProductVasClass> productVASClassList = new List<ProductVasClass>();
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
                                        productVASClassList.Add(maxPreferenceIndicatorProductVASClass);
                                }
                            }
                            foreach (ProductVasClass productVasClass in productVASClassList)
                            {
                                vasService = new VASService();
                                vasService.VASClassID = productVasClass.VasClass;
                                vasService.VASProductID = productVasClass.VasProductId;
                                vasService.SCode = productVasClass.ProductCode;
                                vasService.ProductName = productVasClass.VasProductName;
                                vasService.SupplierID = productVasClass.VasSupplierId;
                                vasService.ServiceTier = productVasClass.VasServiceTier;
                                vasService.ActivationCardinality = productVasClass.ActivationCardinality.ToString();
                                if (productVasClass.VasProductFamily.Equals("BTWIFI", StringComparison.OrdinalIgnoreCase))
                                {
                                    vasService.VASProductFamily = "WIFI";
                                }
                                else
                                {
                                    vasService.VASProductFamily = productVasClass.VasProductFamily;
                                }
                                vasService.VASSubType = productVasClass.vasSubType;
                                vasService.SupplierCode = productVasClass.SupplierCode;
                                vasService.HighestTier = productVasClass.HighestTier;
                                vasService.ProductType = productVasClass.ProductType;
                                vasServiceEligibilityResponse.VASServiceList.Add(vasService);
                            }
                        }
                        else
                        {
                            vasServiceEligibilityResponse.Result.result = false;
                            vasServiceEligibilityResponse.Result.errorCode = 5000;
                            vasServiceEligibilityResponse.Result.errorDescritpion = "Unable to get VASClass Definition";
                        }
                    }
                    else
                    {
                        if (vasDefinitionList != null && vasDefinitionList.Count > 0)
                        {
                            vasServiceEligibilityResponse.Result.result = true;
                            vasServiceEligibilityResponse.Result.errorCode = 0000;
                            vasServiceEligibilityResponse.Result.errorDescritpion = "Transaction Success";
                            vasServiceEligibilityResponse.VASServiceList = new List<VASService>();
                            VASService vasService = null;
                            foreach (ProductVasClass productVasClass in vasDefinitionList)
                            {
                                vasService = new VASService();
                                vasService.VASClassID = productVasClass.VasClass;
                                vasService.VASProductID = productVasClass.VasProductId;
                                vasService.SCode = productVasClass.ProductCode;
                                vasService.ProductName = productVasClass.VasProductName;
                                vasService.SupplierID = productVasClass.VasSupplierId;
                                vasService.ServiceTier = productVasClass.VasServiceTier;
                                vasService.ActivationCardinality = productVasClass.ActivationCardinality.ToString();
                                if (productVasClass.VasProductFamily.Equals("BTWIFI", StringComparison.OrdinalIgnoreCase))
                                {
                                    vasService.VASProductFamily = "WIFI";
                                }
                                else
                                {
                                    vasService.VASProductFamily = productVasClass.VasProductFamily;
                                }
                                vasService.VASSubType = productVasClass.vasSubType;
                                vasService.SupplierCode = productVasClass.SupplierCode;
                                vasService.HighestTier = productVasClass.HighestTier;
                                vasService.ProductType = productVasClass.ProductType;
                                vasServiceEligibilityResponse.VASServiceList.Add(vasService);
                            }
                        }
                        else
                        {
                            vasServiceEligibilityResponse.Result.result = false;
                            vasServiceEligibilityResponse.Result.errorCode = 5000;
                            vasServiceEligibilityResponse.Result.errorDescritpion = "Unable to get VASClass Definition";
                        }
                    }

                }
                else
                {
                    vasServiceEligibilityResponse.Result.result = false;
                    vasServiceEligibilityResponse.Result.errorCode = 5000;
                    vasServiceEligibilityResponse.Result.errorDescritpion = errorString;
                }
                Mye2etxn.logMessage(MseoReceivedNotificationSend, "BAC<" + BillAccountNumber + ">; " + OrderItemInfo);
                vasServiceEligibilityResponse.StandardHeader.E2e.E2EDATA = Mye2etxn.toString();
                Mye2etxn.endInboundCall();
            }
            catch (Exception ex)
            {
                vasServiceEligibilityResponse.Result.result = false;
                vasServiceEligibilityResponse.Result.errorCode = 5000;
                vasServiceEligibilityResponse.Result.errorDescritpion = ex.Message.ToString();
                Mye2etxn.logMessage(ExceptionOccurred, "BAC<" + BillAccountNumber + ">; " + "Root Cause<" + ex.Message.ToString() + ">;");
                vasServiceEligibilityResponse.StandardHeader.E2e.E2EDATA = Mye2etxn.toString();
                Mye2etxn.endInboundCall();
            }
            finally
            {
                LogHelper.LogWFEventDetails(BPTMTxnId, "VASSrvcEligibility", guid, "Reponse Sent", System.DateTime.Now - ActivityStartTime, request.BACID);
            }

            return vasServiceEligibilityResponse;
        }

        [WebMethodAttribute()]
        [SoapDocumentMethodAttribute("http://capabilities.nat.bt.com/wsdl/ManageOrder#vasServiceActivationRequest", Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Bare, Binding = "ManageOrderBinding")]
        public MSEO.OrderNotification VASServiceActivation([System.Xml.Serialization.XmlElementAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15", ElementName = "requestOrderRequest")]OrderRequest requestOrderRequest, string orderFrom)
        {
            string CustomerId = string.Empty;
            LoggingFramework.initialise(ConfigurationManager.AppSettings["logHandlingFilename"].ToString());
            MessagesFile Mf = new MessagesFile(ConfigurationManager.AppSettings["logMessagesFilename"].ToString());
            const string INITIAL_XML_STRING = "INITIAL XML";
            const string BAD_REQUEST_XML_STRING = "NO ORDER NUMBER";
            bool isChopReq = false;


            BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("Order " + requestOrderRequest.Order.OrderIdentifier.Value + " reached to MSEOAdapter at: " + System.DateTime.Now);
            if (string.IsNullOrEmpty(requestOrderRequest.StandardHeader.ServiceAddressing.From))
            {
                if (!string.IsNullOrEmpty(orderFrom) && orderFrom.Equals(Settings1.Default.NativeClientString))
                    requestOrderRequest.StandardHeader.ServiceAddressing.From = ConfigurationManager.AppSettings["DEVAPIServiceAddress"].ToString();
                else
                    requestOrderRequest.StandardHeader.ServiceAddressing.From = Settings1.Default.BTComstring;
            }

            if (requestOrderRequest.Order != null)
            {
                string orderKey = requestOrderRequest.Order.OrderIdentifier.Value;
                InboundQueueDAL.QueueRawXML(orderKey, 2, requestOrderRequest.SerializeObject(), INITIAL_XML_STRING, INITIAL_XML_STRING);
            }
            else
            {
                InboundQueueDAL.QueueRawXML("Bad Request from MSEO", 2, requestOrderRequest.SerializeObject(), INITIAL_XML_STRING, BAD_REQUEST_XML_STRING);
            }

            MSEOOrderNotification notification = null; SAASPE.Order order = null;
            SAASPE.StandardHeader standardHeader = new BT.SaaS.Core.Shared.Entities.StandardHeader();

            standardHeader.serviceAddressing = new BT.SaaS.Core.Shared.Entities.ServiceAddressing();
            standardHeader.serviceAddressing.from = requestOrderRequest.StandardHeader.ServiceAddressing.From;
            try
            {
                BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter : Request recived : " + requestOrderRequest.Order.OrderIdentifier.Value);
                string errorString = string.Empty;
                notification = new MSEOOrderNotification(requestOrderRequest);
                bool isAck = true;
                if (requestOrderRequest.StandardHeader != null && requestOrderRequest.StandardHeader.E2e != null && requestOrderRequest.StandardHeader.E2e.E2EDATA != null && !String.IsNullOrEmpty(requestOrderRequest.StandardHeader.E2e.E2EDATA.ToString()))
                    Mye2etxn = new E2ETransaction(requestOrderRequest.StandardHeader.E2e.E2EDATA.ToString(), ConfigurationManager.AppSettings["ProjectID"].ToString(), ConfigurationManager.AppSettings["Component"].ToString(), Mf);
                else
                    Mye2etxn = new E2ETransaction(ConfigurationManager.AppSettings["E2EData"].ToString(), ConfigurationManager.AppSettings["ProjectID"].ToString(), ConfigurationManager.AppSettings["Component"].ToString(), Mf);

                Mye2etxn.startInboundCall();
                Mye2etxn.setApplicationInfo("Order Key : " + requestOrderRequest.Order.OrderIdentifier.Value);
                requestOrderRequest.StandardHeader.E2e = new E2E();
                requestOrderRequest.StandardHeader.E2e.E2EDATA = Mye2etxn.toString();
                if (Convert.ToBoolean(ConfigurationManager.AppSettings["IsSAASRETAIL2748enabled"]) && requestOrderRequest != null && requestOrderRequest.Order != null && requestOrderRequest.Order.OrderItem[0] != null && requestOrderRequest.Order.OrderItem[0].Specification != null
                    && requestOrderRequest.Order.OrderItem[0].Specification[0].Identifier != null && !string.IsNullOrEmpty(requestOrderRequest.Order.OrderItem[0].Specification[0].Identifier.Value1)
                    && (requestOrderRequest.Order.OrderItem[0].Specification[0].Identifier.Value1.Equals("AdvancedSecurity", StringComparison.OrdinalIgnoreCase) || requestOrderRequest.Order.OrderItem[0].Specification[0].Identifier.Value1.Equals("BasicSecurity", StringComparison.OrdinalIgnoreCase)))
                {
                    throw new DnpException("Ignored as received Virus product request from upstream");
                } else if(Convert.ToBoolean(ConfigurationManager.AppSettings["CLOUD_STOP_SELL_SWITCH"]) && requestOrderRequest != null && requestOrderRequest.Order != null && requestOrderRequest.Order.OrderItem[0] != null && requestOrderRequest.Order.OrderItem[0].Specification != null
                    && requestOrderRequest.Order.OrderItem[0].Specification[0].Identifier != null && !string.IsNullOrEmpty(requestOrderRequest.Order.OrderItem[0].Specification[0].Identifier.Value1)
                    && requestOrderRequest.Order.OrderItem[0].Specification[0].Identifier.Value1.Equals("ContentAnywhere", StringComparison.OrdinalIgnoreCase)
                    && !requestOrderRequest.Order.OrderItem.ToList().Exists(oi => oi.Instance != null && oi.Instance.ToList().Exists(inst => inst.InstanceCharacteristic != null && inst.InstanceCharacteristic.ToList().Exists(instchar => instchar.Name.Equals("isBulkActivation", StringComparison.OrdinalIgnoreCase) && instchar.Value.Equals("true", StringComparison.OrdinalIgnoreCase)))))
                {
                    throw new DnpException("Ignored as received Cloud product request from upstream");
                }
                else
                {
                    if (requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("billaccountnumber", StringComparison.OrdinalIgnoreCase)) && !String.IsNullOrEmpty(requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("billaccountnumber", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value))
                    {
                        BillAccountNumber = requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("billaccountnumber", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                    }

                    if (requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("btoneid", StringComparison.OrdinalIgnoreCase)) && !String.IsNullOrEmpty(requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("btoneid", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value))
                    {
                        BtoneId = requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("btoneid", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                    }
                    if (requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("CustomerId", StringComparison.OrdinalIgnoreCase)) && !String.IsNullOrEmpty(requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("CustomerId", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value))
                    {
                        CustomerId = requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("CustomerId", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                    }
                    if (requestOrderRequest.StandardHeader != null && requestOrderRequest.StandardHeader.ServiceAddressing != null && requestOrderRequest.StandardHeader.ServiceAddressing.From != null && !String.IsNullOrEmpty(requestOrderRequest.StandardHeader.ServiceAddressing.From))
                        orderFrom = requestOrderRequest.StandardHeader.ServiceAddressing.From.ToString();

                    if (VASRequestValidator.ValidateVASActivationRequest(requestOrderRequest, ref errorString))
                    {
                        Mye2etxn.logMessage(MseoReceivedRequest, "Product<" + requestOrderRequest.Order.OrderItem[0].Specification[0].Identifier.Value1 + ">; Order Key<" + requestOrderRequest.Order.OrderIdentifier.Value + ">; BAC<" + BillAccountNumber + ">; BTONEID<" + BtoneId + ">; Order From<" + orderFrom + ">;");

                        if (requestOrderRequest.Order.OrderItem.ToList().Exists(oi => oi.Instance != null && oi.Instance.ToList().Exists(inst => inst.InstanceCharacteristic != null && inst.InstanceCharacteristic.ToList().Exists(instchar => instchar.Name.Equals("ServiceCode", StringComparison.OrdinalIgnoreCase) && instchar.Value.Equals("BT_CONSUMER_BROADBAND", StringComparison.OrdinalIgnoreCase)))))
                        {   
                            isChopReq = true;
                            Guid guid = Guid.NewGuid();
                            System.DateTime ActivityStartTime = System.DateTime.Now;

                            BTCONSUMERBROADBANDProcessor BTConsumerBroadBandProcessor = new BTCONSUMERBROADBANDProcessor();
                            BTConsumerBroadBandProcessor.BTCONSUMERBROADBANDReqeustMapper(requestOrderRequest, guid, ActivityStartTime, ref Mye2etxn);
                            //order = VASRequestProcessor.BulkActivationCloud(BillAccountNumber, BtoneId, CustomerId, ref Mye2etxn);

                            notification.sendNotification(true, isAck, string.Empty, string.Empty, ref Mye2etxn);
                        }
                        else
                        {
                            if (requestOrderRequest.Order.OrderItem.ToList().Exists(oi => oi.Instance != null && oi.Instance.ToList().Exists(inst => inst.InstanceCharacteristic != null && inst.InstanceCharacteristic.ToList().Exists(instchar => instchar.Name.Equals("isBulkActivation", StringComparison.OrdinalIgnoreCase) && instchar.Value.Equals("true", StringComparison.OrdinalIgnoreCase)))))
                            {
                                if (Convert.ToBoolean(ConfigurationManager.AppSettings["CLOUD_BULK_ACTIVATION_SWITCH"]))
                                {
                                    order = VASRequestProcessor.BulkActivationCloud(BillAccountNumber, BtoneId, CustomerId, ref Mye2etxn);
                                } else
                                {
                                    throw new DnpException("Cloud Bulk Activation functionality is not allowed currently.");
                                }
                                
                            }
                            else
                            {
                                order = MSEOSaaSMapper.MapRequest(requestOrderRequest);
                            }

                            if (order.ProductOrderItems.Count > 0)
                            {
                                using (OrderClient orderClient = new OrderClient())
                                {
                                    SAASPE.OrderResponse response = orderClient.SubmitOrder(standardHeader, order);

                                    if (response == null)
                                    {
                                        throw new Exception("Provisioning Engine returned a null response");
                                    }
                                    else
                                    {
                                        BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter : The Order " + response.order.Header.OrderID + " was placed successfully");
                                        notification.sendNotification(response.result.result, isAck, string.Empty, string.Empty, ref Mye2etxn);

                                        if (order.Header.Action.Equals(BT.SaaS.Core.Shared.Entities.OrderActionEnum.cease) && requestOrderRequest != null && requestOrderRequest.Order != null && requestOrderRequest.Order.OrderItem.Count() > 0 && requestOrderRequest.Order.OrderItem[0].Action != null
                                           && !string.IsNullOrEmpty(requestOrderRequest.Order.OrderItem[0].Action.Reason) && requestOrderRequest.Order.OrderItem[0].Action.Reason.Equals("hard_delete", StringComparison.OrdinalIgnoreCase)
                                           && order.ProductOrderItems != null && order.ProductOrderItems[0].RoleInstances != null && order.ProductOrderItems[0].RoleInstances[0].Attributes != null
                                           && order.ProductOrderItems[0].RoleInstances[0].Attributes.ToList().Exists(attr => attr.Name.Equals("isBACCeaseRequired", StringComparison.OrdinalIgnoreCase)) && order.ProductOrderItems[0].RoleInstances[0].Attributes.ToList().Where(attr => attr.Name.Equals("isBACCeaseRequired", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value.Equals("true", StringComparison.OrdinalIgnoreCase))
                                        {
                                            string emailName = string.Empty; string VasProductId = string.Empty; string billaccountnumber = string.Empty;
                                            if (order.ProductOrderItems[0].RoleInstances[0].Attributes.ToList().Exists(ic => ic.Name.Equals("emailname", StringComparison.OrdinalIgnoreCase)) && !String.IsNullOrEmpty(order.ProductOrderItems[0].RoleInstances[0].Attributes.ToList().Where(ic => ic.Name.Equals("emailname", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value))
                                                emailName = order.ProductOrderItems[0].RoleInstances[0].Attributes.ToList().Where(ic => ic.Name.Equals("emailname", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                                            if (order.ProductOrderItems[0].RoleInstances[0].Attributes.ToList().Exists(ic => ic.Name.Equals("VasProductId", StringComparison.OrdinalIgnoreCase)) && !String.IsNullOrEmpty(order.ProductOrderItems[0].RoleInstances[0].Attributes.ToList().Where(ic => ic.Name.Equals("VasProductId", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value))
                                                VasProductId = order.ProductOrderItems[0].RoleInstances[0].Attributes.ToList().Where(ic => ic.Name.Equals("VasProductId", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                                            if (order.ProductOrderItems[0].RoleInstances[0].Attributes.ToList().Exists(ic => ic.Name.Equals("billaccountnumber", StringComparison.OrdinalIgnoreCase)) && !String.IsNullOrEmpty(order.ProductOrderItems[0].RoleInstances[0].Attributes.ToList().Where(ic => ic.Name.Equals("billaccountnumber", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value))
                                                billaccountnumber = order.ProductOrderItems[0].RoleInstances[0].Attributes.ToList().Where(ic => ic.Name.Equals("billaccountnumber", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                                            if (order.ProductOrderItems[0].RoleInstances[0].Attributes.ToList().Exists(ic => ic.Name.Equals("AccountholderBtOneId")) && !string.IsNullOrEmpty(order.ProductOrderItems[0].RoleInstances[0].Attributes.ToList().Where(ic => ic.Name.Equals("AccountholderBtOneId")).FirstOrDefault().Value))
                                                BtoneId = order.ProductOrderItems[0].RoleInstances[0].Attributes.ToList().Where(ic => ic.Name.Equals("AccountholderBtOneId")).FirstOrDefault().Value;

                                            System.Threading.Tasks.Task.Run(() => DanteRequestProcessor.CeaseBasicorPremiumMail(BtoneId, emailName, billaccountnumber, VasProductId));
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (order.Header.Action.Equals(BT.SaaS.Core.Shared.Entities.OrderActionEnum.reActivate) && requestOrderRequest.Order.OrderItem.ToList().Exists(s => s.Specification.ToList().Exists(i => i.Identifier.Value1.Equals("Email", StringComparison.OrdinalIgnoreCase))))
                                {
                                    notification.sendNotification(true, isAck, string.Empty, string.Empty, ref Mye2etxn);

                                    string emailName = string.Empty;
                                    if (requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("emailname", StringComparison.OrdinalIgnoreCase)) && !String.IsNullOrEmpty(requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("emailname", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value))
                                        emailName = requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("emailname", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                                    if (string.IsNullOrEmpty(BtoneId))
                                    {
                                        if (requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("BTID")) && !string.IsNullOrEmpty(requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("BTID")).FirstOrDefault().Value))
                                            BtoneId = requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("BTID")).FirstOrDefault().Value;
                                    }
                                    System.Threading.Tasks.Task.Run(() => DanteRequestProcessor.ReinstateAsBasicMail(BtoneId, emailName, ref emailName, ref Mye2etxn));
                                }
                                else
                                {
                                    notification.sendNotification(false, isAck, Settings1.Default.InvalideRequestCode, "The Product(s) in the request are Ignored because there is no defination for this product in SaaS.", ref Mye2etxn);
                                    BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter : The Order " + requestOrderRequest.Order.OrderIdentifier.Value + " do not have SaaS products");
                                }
                            }
                        }
                    }
                    else
                    {
                        InboundQueueDAL.UpdateQueueRawXML(Settings1.Default.InvalideRequestCode, errorString, requestOrderRequest.Order.OrderIdentifier.Value);
                        notification.sendNotification(false, isAck, Settings1.Default.InvalideRequestCode, errorString, ref Mye2etxn);
                    }
                }
                Mye2etxn.logMessage(MseoReceivedNotificationSend, "Product<" + requestOrderRequest.Order.OrderItem[0].Specification[0].Identifier.Value1 + ">; Order Key<" + requestOrderRequest.Order.OrderIdentifier.Value + ">; BAC<" + BillAccountNumber + ">; BTONEID<" + BtoneId + ">; Order From<" + orderFrom + ">;");
            }
            catch (DnpException DnPexception)
            {
                Mye2etxn.systemError(ExceptionOccurred, "Product<" + requestOrderRequest.Order.OrderItem[0].Specification[0].Identifier.Value1 + ">; Order Key<" + requestOrderRequest.Order.OrderIdentifier.Value + ">; BAC<" + BillAccountNumber + ">; BTONEID<" + BtoneId + ">; Order From<" + orderFrom + ">; Root Cause <" + DnPexception.Message + ">;");
                InboundQueueDAL.UpdateQueueRawXML(Settings1.Default.IgnoredStatus, DnPexception.Message.ToString(), requestOrderRequest.Order.OrderIdentifier.Value);

                foreach (OrderItem orderItem in requestOrderRequest.Order.OrderItem)
                {
                    if (!string.IsNullOrEmpty(DnPexception.Message) && DnPexception.Message.StartsWith("Rejected", StringComparison.OrdinalIgnoreCase))
                        orderItem.Status = "Rejected";
                    else if (isChopReq)
                        orderItem.Status = "Failed";
                    else
                        orderItem.Status = "Ignored";
                }
                if (!string.IsNullOrEmpty(DnPexception.Message) && DnPexception.Message.StartsWith("Rejected", StringComparison.OrdinalIgnoreCase))
                {
                    notification.sendNotification(false, true, "002", DnPexception.Message, ref Mye2etxn);
                    BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter DNP exception : " + DnPexception.ToString() + " for order identifier :" + requestOrderRequest.Order.OrderIdentifier.Value);
                }
                else if (isChopReq)
                {
                    notification.sendNotification(false, true, "777", DnPexception.Message, ref Mye2etxn);
                    BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter DNP exception : " + DnPexception.ToString() + " for order identifier :" + requestOrderRequest.Order.OrderIdentifier.Value);
                }
                else
                {
                    notification.sendNotification(false, true, "001", DnPexception.Message, ref Mye2etxn);
                    BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter DNP exception : " + DnPexception.ToString() + " for order identifier :" + requestOrderRequest.Order.OrderIdentifier.Value);
                }
            }
            catch (Exception ex)
            {
                Mye2etxn.systemError(ExceptionOccurred, "Product<" + requestOrderRequest.Order.OrderItem[0].Specification[0].Identifier.Value1 + ">; Order Key<" + requestOrderRequest.Order.OrderIdentifier.Value + ">; BAC<" + BillAccountNumber + ">; BTONEID<" + BtoneId + ">; Order From<" + orderFrom + ">; Root Cause <" + ex.Message + ">;");
                InboundQueueDAL.UpdateQueueRawXML(Settings1.Default.IgnoredStatus, ex.Message.ToString(), requestOrderRequest.Order.OrderIdentifier.Value);
                if (isChopReq)
                {
                    foreach (OrderItem orderItem in requestOrderRequest.Order.OrderItem)
                    {
                        orderItem.Status = "Failed";
                    }

                    notification.sendNotification(false, true, "777", ex.Message, ref Mye2etxn);
                    BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter exception : " + ex.ToString());
                }
                else
                {
                    foreach (OrderItem orderItem in requestOrderRequest.Order.OrderItem)
                    {
                        orderItem.Status = "Ignored";
                    }

                    notification.sendNotification(false, true, "001", ex.Message, ref Mye2etxn);
                    BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter exception : " + ex.ToString());
                }
            }


            BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("Order " + requestOrderRequest.Order.OrderIdentifier.Value + "notification send at: " + System.DateTime.Now);
            notification.NotificationResponse.StandardHeader.E2e.E2EDATA = Mye2etxn.toString();
            Mye2etxn.endInboundCall();
            return notification.NotificationResponse;
        }

        private List<ProductVasClass> FindAllActiveInactiveProductVasClass(List<string> vasClassList, string identity, string identityDomain, string serviceType)
        {
            List<ProductVasClass> response = new List<ProductVasClass>();
            List<ProductVasClass> mdmProductVasClassList = new List<ProductVasClass>();
            List<ProductVasClass> activeProductVasClassList = new List<ProductVasClass>();
            List<string> dnpProductVasClassList = new List<string>();

            ClientServiceInstance[] serviceInstances = null;

            System.DateTime ActivityStartTime = System.DateTime.Now; Guid guid = Guid.NewGuid();
            TimeSpan TimeDuration = System.DateTime.Now - ActivityStartTime;

            if (string.Equals(serviceType, "ACTIVE", StringComparison.OrdinalIgnoreCase) || string.Equals(serviceType, "NONACTIVATED", StringComparison.OrdinalIgnoreCase))
            {
                LogHelper.LogWFEventDetails("BPTMTxnId", "VASSrvcEligibility", guid, "Started MDM call", System.DateTime.Now - ActivityStartTime, identity);
                mdmProductVasClassList = MdmWrapper.getSaaSVASDefs(vasClassList);
                LogHelper.LogWFEventDetails("BPTMTxnId", "VASSrvcEligibility", guid, "Got Response from MDM", System.DateTime.Now - ActivityStartTime, identity);

                LogHelper.LogWFEventDetails("BPTMTxnId", "VASSrvcEligibility", guid, "Started DnP call", System.DateTime.Now - ActivityStartTime, identity);
                serviceInstances = DnpWrapper.GetClientServiceInstances(identity, identityDomain);
                LogHelper.LogWFEventDetails("BPTMTxnId", "VASSrvcEligibility", guid, "Got Response from DnP", System.DateTime.Now - ActivityStartTime, identity);

                foreach (ClientServiceInstance serviceIntance in serviceInstances)
                {
                    if (serviceIntance.clientServiceInstanceStatus.value.Equals(ACTIVE, StringComparison.OrdinalIgnoreCase))
                    {
                        dnpProductVasClassList.Add(serviceIntance.clientServiceInstanceIdentifier.value);
                    }
                }
                foreach (string dnpVasProductID in dnpProductVasClassList)
                {
                    for (int i = 0; i < mdmProductVasClassList.Count; i++)
                    {
                        if (dnpVasProductID.Equals(mdmProductVasClassList[i].VasProductId, StringComparison.OrdinalIgnoreCase))
                        {
                            activeProductVasClassList.Add(mdmProductVasClassList[i]);
                        }
                    }
                }
                if (string.Equals(serviceType, "NONACTIVATED", StringComparison.OrdinalIgnoreCase))
                {
                    for (int i = 0; i < activeProductVasClassList.Count; i++)
                    {
                        mdmProductVasClassList.Remove(activeProductVasClassList[i]);
                    }
                    response = mdmProductVasClassList;
                }
                else if (string.Equals(serviceType, "ACTIVE", StringComparison.OrdinalIgnoreCase))
                {
                    response = activeProductVasClassList;
                }
            }
            else
            {
                LogHelper.LogWFEventDetails("BPTMTxnId", "VASSrvcEligibility", guid, "Started MDM call", System.DateTime.Now - ActivityStartTime, identity);
                response = MdmWrapper.getSaaSVASDefs(vasClassList);
                LogHelper.LogWFEventDetails("BPTMTxnId", "VASSrvcEligibility", guid, "Got Response from MDM", System.DateTime.Now - ActivityStartTime, identity);
            }

            return response;
        }

        private List<ProductVasClass> FindAllActiveInactiveProductVasClass(List<string> vasClassList, string identity, string identityDomain, string serviceType, string bptmtxnId)
        {
            List<ProductVasClass> response = new List<ProductVasClass>();
            System.DateTime ActivityStartTime = System.DateTime.Now;
            TimeSpan TimeDuration = System.DateTime.Now - ActivityStartTime;
            LogHelper.LogWFEventDetails(bptmtxnId, "VASSrvcEligibility", Guid.NewGuid(), "Started MDM DnP call", TimeDuration, identity);
            response = FindAllActiveInactiveProductVasClass(vasClassList, identity, "VAS_BILLINGACCOUNT_ID", serviceType);
            TimeDuration = System.DateTime.Now - ActivityStartTime;
            LogHelper.LogWFEventDetails(bptmtxnId, "VASSrvcEligibility", Guid.NewGuid(), "Got Response from MDM DnP", TimeDuration, identity);
            return response;
        }

        [WebMethod]
        public QueryCloudDataResponse QueryCloudData(QueryCloudDataRequest queryCloudDataRequest)
        {
            LoggingFramework.initialise(ConfigurationManager.AppSettings["logHandlingFilename"].ToString());
            MessagesFile Mf = new MessagesFile(ConfigurationManager.AppSettings["logMessagesFilename"].ToString());

            const double BYTE_VALUE = 1073741824.00;
            double canQuota = 0.0;
            string syncQuota = string.Empty;
            string CCID = string.Empty;
            subscriptionQueryResponse1 response = null;

            QueryCloudDataResponse queryCloudDataResponse = new QueryCloudDataResponse();
            queryCloudDataResponse.Result = new QueryResult();
            queryCloudDataResponse.Result.OrderDate = System.DateTime.Now.Ticks.ToString();
            queryCloudDataResponse.Result.OrderNumber = queryCloudDataRequest.OrderNumber;
            queryCloudDataResponse.Result.result = true;
            queryCloudDataResponse.Result.errorcode = 0;
            queryCloudDataResponse.Result.errorDescritpion = "Transaction Success";

            CloudDataResponse cloudResponse = new CloudDataResponse();
            cloudResponse.Customer = new CustomerResponse();
            try
            {
                Mye2etxn = new E2ETransaction(ConfigurationManager.AppSettings["E2EData"].ToString(), ConfigurationManager.AppSettings["ProjectID"].ToString(), ConfigurationManager.AppSettings["Component"].ToString(), Mf);
                Mye2etxn.startInboundCall();
                Mye2etxn.setApplicationInfo("QueryCloudData : Order Number <" + queryCloudDataRequest.OrderNumber + ">");
                if (!String.IsNullOrEmpty(queryCloudDataRequest.CloudData.Customer.Value))
                    CCID = queryCloudDataRequest.CloudData.Customer.Value;
                Mye2etxn.logMessage(MseoReceivedRequest, "CCID<" + CCID + ">;");

                subscriptionQueryRequest1 request = new subscriptionQueryRequest1();
                request.subscriptionQueryRequest = new subscriptionQueryRequest();
                request.operatorId = Settings1.Default.FSecureOperatorID;
                request.subscriptionQueryRequest.customer = new opi11.datamodel.ws.fsecure.com_SubscriptionManagement.Identity();
                request.subscriptionQueryRequest.customer.Item = queryCloudDataRequest.CloudData.Customer.Value;
                request.subscriptionQueryRequest.customer.ItemElementName = ItemChoiceType.externalReference;

                Mye2etxn.startOutboundCall(ConfigurationManager.AppSettings["destinationSystemFsecure"].ToString(), E2ETransaction.vREQ, "Query Cloud Data");

                if (Convert.ToBoolean(ConfigurationManager.AppSettings["isProxyEnabledforGetCalls"]))
                {
                    CustomBinding binding = new CustomBinding(@"SubscriptionManagementHttpBinding");
                    binding.Elements.RemoveAt(2);

                    HttpsTransportBindingElement tbe = new HttpsTransportBindingElement();
                    tbe.ProxyAddress = new Uri(ConfigurationManager.AppSettings["ProxyforGetCalls"]);
                    tbe.MaxBufferSize = 65536;
                    // passing TLS1.2 in the request.
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                    tbe.UseDefaultWebProxy = false;
                    binding.Elements.Add(tbe);

                    EndpointAddress address = new EndpointAddress(new Uri(ConfigurationManager.AppSettings["FSecureUrl"]));
                    //binding.GetType().GetProperty("HttpsTransportBindingElement").SetValue(binding, tbe, null);
                    using (ChannelFactory<SubscriptionManagement> factory = new ChannelFactory<SubscriptionManagement>(binding, address))
                    {
                        factory.Credentials.UserName.UserName = Settings1.Default.FSecureUserName;
                        factory.Credentials.UserName.Password = Settings1.Default.FSecurePassword;

                        SubscriptionManagement service = factory.CreateChannel();
                        response = service.subscriptionQuery(request);
                    }
                }
                else
                {
                    using (ChannelFactory<SubscriptionManagement> factory = new ChannelFactory<SubscriptionManagement>(@"SubscriptionManagementHttpPort"))
                    {
                        factory.Credentials.UserName.UserName = Settings1.Default.FSecureUserName;
                        factory.Credentials.UserName.Password = Settings1.Default.FSecurePassword;

                        SubscriptionManagement service = factory.CreateChannel();
                        response = service.subscriptionQuery(request);
                    }
                }

                cloudResponse.Customer.externalReference = queryCloudDataRequest.CloudData.Customer.Value;

                if (response != null
                        && response.subscriptionQueryResponse != null
                        && response.subscriptionQueryResponse.subscriptionDetail != null
                        && response.subscriptionQueryResponse.subscriptionDetail.Count() > 0)
                {
                    SubscriptionDetails subDetail = response.subscriptionQueryResponse.subscriptionDetail.ToList().Where(subcriptionDetail => subcriptionDetail.validToDate.Equals(Convert.ToDateTime(ConfigurationManager.AppSettings["FsecureValidToDate"])) && subcriptionDetail.attribute.ToList().Exists(a => a.name.Equals("can.quota"))).FirstOrDefault();
                    if (subDetail != null && subDetail.attribute.ToList().Exists(a => a.name.Equals("can.quota", StringComparison.OrdinalIgnoreCase)))
                    {
                        canQuota = Convert.ToDouble(subDetail.attribute.ToList().Where(a => a.name.Equals("can.quota", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value);
                        cloudResponse.Customer.syncquota = Convert.ToString(canQuota / BYTE_VALUE);
                        cloudResponse.Customer.status = subDetail.status.ToString();
                        cloudResponse.Customer.validToDate = subDetail.validToDate;//new System.DateTime(2030, 12, 20);
                    }
                    else
                    {
                        Dictionary<System.DateTime, string> validToDateList = new Dictionary<System.DateTime, string>();
                        for (int i = 0; i < response.subscriptionQueryResponse.subscriptionDetail.Count(); i = i + 1)
                        {
                            if (response.subscriptionQueryResponse.subscriptionDetail[i].attribute.ToList().Exists(a => a.name.Equals("can.quota", StringComparison.OrdinalIgnoreCase)))
                            {
                                if (!validToDateList.ContainsKey(response.subscriptionQueryResponse.subscriptionDetail[i].validToDate))
                                {
                                    validToDateList.Add(response.subscriptionQueryResponse.subscriptionDetail[i].validToDate, (Convert.ToDouble(response.subscriptionQueryResponse.subscriptionDetail[i].attribute.ToList().Where(a => a.name.Equals("can.quota", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value) / BYTE_VALUE).ToString());
                                }
                            }
                        }
                        if (validToDateList.Count > 0)
                        {
                            cloudResponse.Customer.validToDate = validToDateList.OrderByDescending(ic => ic.Key).FirstOrDefault().Key;
                            syncQuota = validToDateList.OrderByDescending(ic => ic.Key).FirstOrDefault().Value;
                        }
                        cloudResponse.Customer.syncquota = syncQuota;
                        cloudResponse.Customer.status = response.subscriptionQueryResponse.subscriptionDetail[0].status.ToString();//"disabled";

                    }
                    Mye2etxn.logMessage(MseoGotResponseFromFSecure, "CCID<" + CCID + ">; CAN QUOTA<" + canQuota + ">;");
                }
            }
            catch (FaultException<SmiFaultInfo> faultException)
            {
                queryCloudDataResponse.Result.result = false;
                queryCloudDataResponse.Result.errorcode = 001;
                queryCloudDataResponse.Result.errorDescritpion = "Transaction Failure";
                Mye2etxn.downstreamError(ExceptionOccurred, "CCID<" + CCID + ">; Root Cause <" + faultException.Detail.errorMessage + ">;");
            }
            catch (Exception Exception)
            {
                queryCloudDataResponse.Result.result = false;
                queryCloudDataResponse.Result.errorcode = 001;
                queryCloudDataResponse.Result.errorDescritpion = "Transaction Failure";
                Mye2etxn.downstreamError(ExceptionOccurred, "CCID<" + CCID + ">; Root Cause <" + Exception.Message + ">;");
            }
            finally
            {
                Mye2etxn.endOutboundCall(E2ETransaction.getSimulatedReply(Mye2etxn.toString(), E2ETransaction.vRES, null));
                Mye2etxn.endInboundCall();

            }

            queryCloudDataResponse.CloudData = cloudResponse;

            return queryCloudDataResponse;
        }

        [WebMethod]
        public GetBTVPActivationCodeResponse BTVPActivationCodeRequest(GetBTVPActivationCodeRequest btvpPActivationCodeRequest)
        {
            #region members
            string requestXML = string.Empty, responseXml;
            HttpWebRequest webrequest;
            string xmlVersion = "<?xml version=\"1.0\" encoding=\"utf-16\"?>";
            string xmlNamespace = "<PARTNERCONTEXT xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">";
            Guid guid = new Guid();
            guid = Guid.NewGuid();
            System.DateTime ActivityStartTime = System.DateTime.Now;
            string bptmTxnId = string.Empty;
            #endregion

            LoggingFramework.initialise(ConfigurationManager.AppSettings["logHandlingFilename"].ToString());
            MessagesFile Mf = new MessagesFile(ConfigurationManager.AppSettings["logMessagesFilename"].ToString());
            Mye2etxn = new E2ETransaction(ConfigurationManager.AppSettings["E2EData"].ToString(), ConfigurationManager.AppSettings["ProjectID"].ToString(), ConfigurationManager.AppSettings["Component"].ToString(), Mf);
            Mye2etxn.logMessage(MseoReceivedRequest, "Order Number<" + btvpPActivationCodeRequest.OrderNumber + ">;");
            Mye2etxn.startInboundCall();

            LogHelper.LogActivityDetails(btvpPActivationCodeRequest.OrderNumber.ToString(), guid, "Received BTVPActivationCodeRequest from upstream ", System.DateTime.Now - ActivityStartTime, "BTVPActivationTrace", btvpPActivationCodeRequest.SerializeObject());

            GetBTVPActivationCodeResponse queryMcAfeeResponse = new GetBTVPActivationCodeResponse();
            queryMcAfeeResponse.Result = new McAfeeQueryResult();
            queryMcAfeeResponse.Result.OrderDate = System.DateTime.Now.Ticks.ToString();
            queryMcAfeeResponse.Result.OrderNumber = btvpPActivationCodeRequest.OrderNumber;
            if (Convert.ToBoolean(ConfigurationManager.AppSettings["IsSAASRETAIL2748enabled"]))
            {
                queryMcAfeeResponse.Result.result = false;
                queryMcAfeeResponse.Result.errorcode = 001;
                queryMcAfeeResponse.Result.errorDescritpion = "Ignored as bad request received";

                return queryMcAfeeResponse;
            }
            else
            {
                queryMcAfeeResponse.Result.result = true;
                queryMcAfeeResponse.Result.errorcode = 0;
                queryMcAfeeResponse.Result.errorDescritpion = "Transaction Success";


                BTVPActivationCode mcAFeeActivationCode = new BTVPActivationCode();
                mcAFeeActivationCode.Customer = new QueryMcAfeeResponse();

                try
                {
                    #region ConstructRequestXml
                    Subscription subscription = new Subscription();

                    Header h = new Header();
                    HederChild child = new HederChild();
                    //Change it to read from Settings
                    child.partner_id = ConfigurationManager.AppSettings["partnerid"];
                    h.Partner = child;
                    subscription.header = h;

                    Account account = new Account();
                    PreferenceAttributes preferenceAttribute = new PreferenceAttributes();
                    Preference preferences = new Preference();

                    CustomerContextAttributes customerContextAttributes = new CustomerContextAttributes();
                    customerContextAttributes.Id = btvpPActivationCodeRequest.BTVPActivationCode.Customer.CCID;
                    customerContextAttributes.RequestType = "PRODUCTDOWNLOAD";

                    Data data = new Data();
                    data.attributes = customerContextAttributes;

                    SourceSKU sSku = null;

                    OrderAttributes orderAttributes = new OrderAttributes();
                    ItemAttributes itemAttributes = new ItemAttributes();

                    Item item = new Item();
                    item.itemAttributes = itemAttributes;
                    itemAttributes.sku = ConfigurationManager.AppSettings["mmasuppliercode"];
                    itemAttributes.additionalInfo = "ACTIVATIONCODE";
                    itemAttributes.additionalInfoSpecified = true;
                    itemAttributes.deviceType = "Phone";
                    itemAttributes.deviceTypeSpecified = true;

                    orderAttributes.items = item;
                    customerContextAttributes.orderAttributes = orderAttributes;

                    subscription.data = data;
                    #endregion

                    Mye2etxn.startOutboundCall(ConfigurationManager.AppSettings["destinationSystemMcAfee"].ToString(), E2ETransaction.vREQ, "Get BTVP Activation Code");

                    #region CalMcAfee
                    Serialization serialize = new Serialization();
                    requestXML = serialize.SerializeObject(subscription);
                    requestXML = requestXML.Replace(xmlVersion, "");
                    requestXML = requestXML.Replace(xmlNamespace, "<PARTNERCONTEXT>");

                    LogHelper.LogActivityDetails(btvpPActivationCodeRequest.OrderNumber.ToString(), guid, "Creating a subscription to activate btvpcode requestxml: ", System.DateTime.Now - ActivityStartTime, "BTVPActivationTrace", requestXML);
                    //send a request to the McAfee by HTTP Post            
                    webrequest = (HttpWebRequest)WebRequest.Create(ConfigurationManager.AppSettings["McAfeeURL"]);
                    webrequest.Method = "POST";
                    webrequest.ContentType = "text/xml";
                    webrequest.Timeout = 60000;
                    ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
                    UTF8Encoding encoding = new UTF8Encoding();
                    byte[] postBytes = encoding.GetBytes(requestXML);
                    webrequest.ContentLength = postBytes.Length;

                    Stream postStream = webrequest.GetRequestStream();
                    postStream.Write(postBytes, 0, postBytes.Length);
                    postStream.Close();
                    //Get the response from McAfee 
                    using (HttpWebResponse webResponse = (HttpWebResponse)webrequest.GetResponse())
                    {
                        using (StreamReader readStream = new StreamReader(webResponse.GetResponseStream()))
                        {
                            responseXml = readStream.ReadToEnd();

                            LogHelper.LogActivityDetails(btvpPActivationCodeRequest.OrderNumber.ToString(), guid, "Creating a subscription to activate btvpcode response xml: ", System.DateTime.Now - ActivityStartTime, "BTVPActivationTrace", responseXml);
                        }
                    }
                    #endregion
                    #region ValidateResponsefrom McAfee
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(responseXml);

                    XmlNode partnerRef = xmlDoc.SelectSingleNode("PARTNERRESPONSECONTEXT/DATA/RESPONSECONTEXT/ORDER");
                    XmlNode returnCode = xmlDoc.SelectSingleNode("PARTNERRESPONSECONTEXT/DATA/RESPONSECONTEXT/RETURNCODE");
                    if (returnCode.InnerText == "1000")
                    {
                        //XmlNode downloadUrl = xmlDoc.SelectSingleNode("PARTNERRESPONSECONTEXT/DATA/RESPONSECONTEXT/PRODUCTDOWNLOADURL");
                        XmlNode activationPin = xmlDoc.SelectSingleNode("PARTNERRESPONSECONTEXT/DATA/RESPONSECONTEXT/PRODUCTACTIVATIONCODE");

                        //Do not pass downloadurl as part of Activation response
                        if (!string.IsNullOrEmpty(activationPin.InnerText))
                            mcAFeeActivationCode.Customer.ActivationPin = activationPin.InnerText;
                        else
                        {
                            queryMcAfeeResponse.Result.result = false;
                            queryMcAfeeResponse.Result.errorcode = 1;
                            queryMcAfeeResponse.Result.errorDescritpion = "Activation Pin is empty from McAfee";
                        }
                        //mcAFeeActivationCode.Customer.ProductDownloadUrl = downloadUrl.InnerText;

                        queryMcAfeeResponse.ActivationCode = mcAFeeActivationCode;
                    }
                    else if (returnCode.InnerText == "6009")
                    {
                        queryMcAfeeResponse.Result.result = false;
                        queryMcAfeeResponse.Result.errorcode = 6009;
                        queryMcAfeeResponse.Result.errorDescritpion = "Maximum Limit Reached";
                    }
                    else
                    {
                        XmlNode errDesc = xmlDoc.SelectSingleNode("PARTNERRESPONSECONTEXT/DATA/RESPONSECONTEXT/RETURNDESC");
                        queryMcAfeeResponse.Result.result = false;
                        queryMcAfeeResponse.Result.errorcode = 1;
                        queryMcAfeeResponse.Result.errorDescritpion = errDesc.InnerText;
                    }
                    xmlDoc = null;
                    #endregion
                }

                catch (Exception ex)
                {
                    queryMcAfeeResponse.Result.result = false;
                    queryMcAfeeResponse.Result.errorcode = 001;
                    queryMcAfeeResponse.Result.errorDescritpion = "Transaction Failure" + ex.Message;
                    Mye2etxn.downstreamError(ExceptionOccurred, "CCID<" + btvpPActivationCodeRequest.BTVPActivationCode.Customer.CCID + ">; Failure Cause <" + ex.Message + ">;");
                }
                finally
                {
                    Mye2etxn.endOutboundCall(E2ETransaction.getSimulatedReply(Mye2etxn.toString(), E2ETransaction.vRES, null));
                    Mye2etxn.endInboundCall();
                }

                LogHelper.LogActivityDetails(btvpPActivationCodeRequest.OrderNumber.ToString(), guid, "Creating a subscription to activate btvpcode response: ", System.DateTime.Now - ActivityStartTime, "BTVPActivationTrace", queryMcAfeeResponse.SerializeObject());

                return queryMcAfeeResponse;
            }
        }

        [WebMethod]
        public ManageDNPServiceInstanceResponse ManageDNPServiceInstance(ManageDNPServiceInstanceRequest manageServiceInstanceRequest)
        {
            LoggingFramework.initialise(ConfigurationManager.AppSettings["logHandlingFilename"].ToString());
            MessagesFile Mf = new MessagesFile(ConfigurationManager.AppSettings["logMessagesFilename"].ToString());
            const string INITIAL_XML_STRING = "INITIAL XML";
            const string BAD_REQUEST_XML_STRING = "NO ORDER NUMBER";

            BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("Order " + manageServiceInstanceRequest.OrderNumber + " reached to MSEOAdapter at: " + System.DateTime.Now);

            if (manageServiceInstanceRequest.StandardHeader != null && manageServiceInstanceRequest.StandardHeader.E2e != null && manageServiceInstanceRequest.StandardHeader.E2e.E2EDATA != null && !String.IsNullOrEmpty(manageServiceInstanceRequest.StandardHeader.E2e.E2EDATA.ToString()))
                Mye2etxn = new E2ETransaction(manageServiceInstanceRequest.StandardHeader.E2e.E2EDATA.ToString(), ConfigurationManager.AppSettings["ProjectID"].ToString(), ConfigurationManager.AppSettings["Component"].ToString(), Mf);
            else
                Mye2etxn = new E2ETransaction(ConfigurationManager.AppSettings["E2EData"].ToString(), ConfigurationManager.AppSettings["ProjectID"].ToString(), ConfigurationManager.AppSettings["Component"].ToString(), Mf);

            System.DateTime ActivityStartTime = System.DateTime.Now;
            string bptmTxnId = BPTMHelper.GetTxnIdFromE2eData(Mye2etxn.toString());
            Guid guid = Guid.NewGuid();
            string orderKey = string.Empty;

            if (manageServiceInstanceRequest != null && manageServiceInstanceRequest.OrderNumber != null && manageServiceInstanceRequest.srvcInstance != null)
            {
                orderKey = manageServiceInstanceRequest.OrderNumber.ToString();
                //InboundQueueDAL.QueueRawXML(orderKey, 2, manageServiceInstanceRequest.SerializeObject(), INITIAL_XML_STRING, INITIAL_XML_STRING);
                LogHelper.LogActivityDetails(bptmTxnId, guid, "ReachedtoMSEO", TimeSpan.Zero, "BTNetFlixTrace", orderKey, manageServiceInstanceRequest.SerializeObject());
            }
            else
            {
                //InboundQueueDAL.QueueRawXML("Bad Request from MSEO", 2, manageServiceInstanceRequest.SerializeObject(), INITIAL_XML_STRING, BAD_REQUEST_XML_STRING);
                LogHelper.LogActivityDetails(bptmTxnId, guid, "ReachedtoMSEO", TimeSpan.Zero, "BTNetFlixTrace", BAD_REQUEST_XML_STRING, manageServiceInstanceRequest.SerializeObject());
            }

            if (manageServiceInstanceRequest.StandardHeader != null && manageServiceInstanceRequest.StandardHeader.ServiceAddressing != null && manageServiceInstanceRequest.StandardHeader.ServiceAddressing.From != null && !String.IsNullOrEmpty(manageServiceInstanceRequest.StandardHeader.ServiceAddressing.From))
                orderFrom = manageServiceInstanceRequest.StandardHeader.ServiceAddressing.From.ToString();

            Mye2etxn.startInboundCall();

            ManageDNPServiceInstanceResponse MSIResposne = null;
            MSIResposne = MSEOSaaSMapper.MSICalltoDnP(manageServiceInstanceRequest, ref Mye2etxn, bptmTxnId, ActivityStartTime, guid, orderKey);

            Mye2etxn.endInboundCall();

            Mye2etxn.logMessage("MSEOSendResponse to OSCH as" + MSIResposne.SerializeObject(), "Product<" + manageServiceInstanceRequest.srvcInstance.ServiceCode + ">; Order Key<" + manageServiceInstanceRequest.OrderNumber + ">; BAC<" + manageServiceInstanceRequest.srvcInstance.BillAccountNumber + ">; Order From<" + orderFrom + ">;");

            LogHelper.LogActivityDetails(bptmTxnId, guid, "MSEOSendResponse to OSCH", System.DateTime.Now - ActivityStartTime, "BTNetFlixTrace", BAD_REQUEST_XML_STRING, MSIResposne.SerializeObject());

            return MSIResposne;
        }

        [WebMethodAttribute()]
        [SoapDocumentMethodAttribute("http://capabilities.nat.bt.com/wsdl/ManageOrder#ofs", Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Bare, Binding = "ManageOrderBinding")]
        public MSEO.OrderNotification ofs(OrderRequest requestOrderRequest)
        {
            MSEOOrderNotification notification = null;
            LoggingFramework.initialise(ConfigurationManager.AppSettings["logHandlingFilename"].ToString());
            MessagesFile Mf = new MessagesFile(ConfigurationManager.AppSettings["logMessagesFilename"].ToString());
            string BillAccountNumber = string.Empty, VasClass = string.Empty, BtoneId = string.Empty, orderFrom = string.Empty, OrderItemInfo = string.Empty;
            bool isAck = true;
            bool isSpringProvision = false;
            bool isThomasProvision = false;
            bool isHCSProvision = false;
            bool isBTPlusMarker = false;
            bool isAssociateBTPlusMarker = false;
            bool isVoInfinity = false;
            bool isEESpringProvision = false;
            bool isCONSUMERBROADBAND = false;
            const string New = "New";
            const string ReachedtoMSEO = "Order reached to MSEO";
            const string Ignored = "Ignored";
            const string IgnoredNotificationSent = "Ignored notification sent for the Order ";
            string date = System.DateTime.Now.ToString();
            List<string> Active_EmailList = new List<string>();
            List<string> BarredAbuse_EmailList = new List<string>();

            try
            {
                //orderCompleteMethod();
                notification = new MSEOOrderNotification(requestOrderRequest);
                if (requestOrderRequest.StandardHeader != null && requestOrderRequest.StandardHeader.E2e != null && requestOrderRequest.StandardHeader.E2e.E2EDATA != null && !String.IsNullOrEmpty(requestOrderRequest.StandardHeader.E2e.E2EDATA.ToString()))
                    Mye2etxn = new E2ETransaction(requestOrderRequest.StandardHeader.E2e.E2EDATA.ToString(), ConfigurationManager.AppSettings["ProjectID"].ToString(), ConfigurationManager.AppSettings["Component"].ToString(), Mf);
                else
                {
                    Mye2etxn = new E2ETransaction(ConfigurationManager.AppSettings["E2EData"].ToString(), ConfigurationManager.AppSettings["ProjectID"].ToString(), ConfigurationManager.AppSettings["Component"].ToString(), Mf);
                    // requestOrderRequest.StandardHeader.E2e.E2EDATA = Mye2etxn.toString();
                }

                Mye2etxn.startInboundCall();
                Mye2etxn.setApplicationInfo("Order Key : " + requestOrderRequest.Order.OrderIdentifier.Value);
                if (requestOrderRequest.StandardHeader != null && requestOrderRequest.StandardHeader.E2e != null)
                {
                    requestOrderRequest.StandardHeader.E2e = new BT.SaaS.MSEOAdapter.E2E();
                    requestOrderRequest.StandardHeader.E2e.E2EDATA = Mye2etxn.toString();
                }
                if (requestOrderRequest.StandardHeader != null && requestOrderRequest.StandardHeader.ServiceAddressing != null && requestOrderRequest.StandardHeader.ServiceAddressing.From != null && !String.IsNullOrEmpty(requestOrderRequest.StandardHeader.ServiceAddressing.From))
                    orderFrom = requestOrderRequest.StandardHeader.ServiceAddressing.From.ToString();
                if (requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("billaccountnumber", StringComparison.OrdinalIgnoreCase)))
                    BillAccountNumber = requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("billaccountnumber", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;

                try
                {
                    const string INITIAL_XML_STRING = "INITIAL XML";
                    const string BAD_REQUEST_XML_STRING = "NO ORDER NUMBER";

                    string bptmTxnId = BPTMHelper.GetTxnIdFromE2eData(Mye2etxn.toString());
                    Guid guid = Guid.NewGuid();
                    System.DateTime ActivityStartTime = System.DateTime.Now;

                    if (requestOrderRequest != null)
                    {
                        string productCode = string.Empty;
                        if (NewJourneyProvisonOrders(requestOrderRequest, ref productCode))
                        {
                            if (productCode.Equals(ConfigurationManager.AppSettings["BTSpringProductCode"].ToString(), StringComparison.OrdinalIgnoreCase))
                                isSpringProvision = true;
                            else
                                isThomasProvision = true;
                        }
                        //Voinfinity
                        if (requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToUpper() == "SERVICENAME" && (ic.Value.Equals("IP_Voice_Service", StringComparison.OrdinalIgnoreCase))))
                        {
                            isVoInfinity = true;
                        }
                        if (requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("ServiceCode", StringComparison.OrdinalIgnoreCase) && (ic.Value.Equals("BT_CONSUMER_BROADBAND", StringComparison.OrdinalIgnoreCase)))
                             && !requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("vasclass", StringComparison.OrdinalIgnoreCase)))
                        {
                            isCONSUMERBROADBAND = true;
                        }
                        if (productCode.Equals(ConfigurationManager.AppSettings["HCSProductCode"].ToString(), StringComparison.OrdinalIgnoreCase))
                        {
                            isHCSProvision = true;
                        }
                        if (productCode.Equals(ConfigurationManager.AppSettings["BTPlusMarkerProductCode"].ToString(), StringComparison.OrdinalIgnoreCase))
                        {
                            isBTPlusMarker = true;
                        }
                        if (productCode.Equals(ConfigurationManager.AppSettings["AssociateBTPlusMarkerProductCode"].ToString(), StringComparison.OrdinalIgnoreCase))
                        {
                            isAssociateBTPlusMarker = true;
                        }

                        string errorString = string.Empty;
                        string orderKey = requestOrderRequest.Order.OrderIdentifier.Value;
                        SAASPE.Order order;

                        if (!isThomasProvision && !isSpringProvision && !isVoInfinity && !isHCSProvision && !isBTPlusMarker && !isAssociateBTPlusMarker && !isCONSUMERBROADBAND)
                            InboundQueueDAL.QueueRawXML(orderKey, 2, requestOrderRequest.SerializeObject(), INITIAL_XML_STRING, INITIAL_XML_STRING);
                        else if (isThomasProvision)
                        {
                            Logger.Write(orderKey + "," + New + "," + ReachedtoMSEO, Logger.TypeEnum.MessageTrace);
                            MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.MQ_IN_LOG, orderKey, requestOrderRequest.SerializeObject(), productCode);
                        }
                        else if (isSpringProvision)
                        {
                            Logger.Write(orderKey + "," + New + "," + ReachedtoMSEO, Logger.TypeEnum.SpringMessageTrace);
                            MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.MQ_IN_LOG, orderKey, requestOrderRequest.SerializeObject(), productCode);
                        }
                        else if (isVoInfinity)
                        {
                            Logger.Write(orderKey + "," + New + "," + ReachedtoMSEO, Logger.TypeEnum.VoInfinityTrace);
                            MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.MQ_IN_LOG, orderKey, requestOrderRequest.SerializeObject(), productCode);
                        }
                        else if (isCONSUMERBROADBAND)
                        {
                            //Logger.Write(orderKey + "," + New + "," + ReachedtoMSEO, Logger.TypeEnum.CONSUMERBROADBANDMessageTrace);
                            LogHelper.LogActivityDetails(bptmTxnId, guid, New + " " + ReachedtoMSEO, TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", requestOrderRequest.SerializeObject());
                            //MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.MQ_IN_LOG, orderKey, requestOrderRequest.SerializeObject(), productCode);                                            
                        }
                        else if (isHCSProvision)
                        {
                            Logger.Write(orderKey + "," + New + "," + ReachedtoMSEO, Logger.TypeEnum.HCSWarrantyMessageTrace);
                            MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.MQ_IN_LOG, orderKey, requestOrderRequest.SerializeObject(), productCode);
                        }
                        else if (isBTPlusMarker)
                        {
                            //here using only kibana logs 
                            LogHelper.LogActivityDetails(bptmTxnId, guid, New + " " + ReachedtoMSEO, TimeSpan.Zero, " BTPlusMarkerTrace ", requestOrderRequest.SerializeObject());
                            //Logger.Write(orderKey + "," + New + "," + ReachedtoMSEO, Logger.TypeEnum.BTPlusMarkerMessageTrace);                                            
                            MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.MQ_IN_LOG, orderKey, requestOrderRequest.SerializeObject(), productCode);
                        }
                        else if (isAssociateBTPlusMarker)
                        {
                            //here using only kibana logs 
                            LogHelper.LogActivityDetails(bptmTxnId, guid, New + " " + ReachedtoMSEO, TimeSpan.Zero, " BTPlusMarkerTrace ", requestOrderRequest.SerializeObject());
                            //Logger.Write(orderKey + "," + New + "," + ReachedtoMSEO, Logger.TypeEnum.BTPlusMarkerMessageTrace);                                            
                            MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.MQ_IN_LOG, orderKey, requestOrderRequest.SerializeObject(), productCode);
                        }

                        if (RequestValidator.ValidateMSEORequest(requestOrderRequest, ref errorString))
                        {
                            foreach (OrderItem Oi in requestOrderRequest.Order.OrderItem)
                            {
                                if (Oi.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("vasclass", StringComparison.OrdinalIgnoreCase)))
                                    VasClass = Oi.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("vasclass", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                                OrderItemInfo += "Order Item < Action:" + Oi.Action.Code + ", Product Code:" + Oi.Specification[0].Identifier.Value1 + ", VAS CLASS:" + VasClass + ">;";
                            }
                            Mye2etxn.logMessage(MseoReceivedRequest, "Order Key<" + requestOrderRequest.Order.OrderIdentifier.Value + ">; " + OrderItemInfo + "BAC<" + BillAccountNumber + ">; Order From<" + orderFrom + ">;");
                            // Voinfinity - changes Start
                            // create a factory that will decide and call appropriate request mapper - open/close - no more changes to this function
                            ProductFactory objfac = new ProductFactory();
                            if (objfac.Mapperfactory(requestOrderRequest, ref Mye2etxn))
                            {
                            }
                            //end
                            else if (isThomasProvision)
                            {
                                ThomasRequestProcessor thomasProcessor = new ThomasRequestProcessor();
                                Guid ad = new Guid();
                                System.DateTime b = System.DateTime.Now;
                                thomasProcessor.ThomasRequestMapper(requestOrderRequest, ad, b, ref Mye2etxn);
                            }
                            else if (isHCSProvision)
                            {
                                HCSRequestProcessor hcsProcessor = new HCSRequestProcessor();
                                hcsProcessor.HCSRequestMapper(requestOrderRequest, ref Mye2etxn);
                            }
                            else if (isSpringProvision)
                            {
                                SpringRequestProcessor springProcessor = new SpringRequestProcessor();
                                springProcessor.SpringRequestMapper(requestOrderRequest, ref Mye2etxn);
                            }
                            else if (isBTPlusMarker)
                            {
                                BTPlusMarkerProcessor BTPlusMarkerProcessor = new BTPlusMarkerProcessor();
                                BTPlusMarkerProcessor.BTPlusMarkerReqeustMapper(requestOrderRequest, guid, ActivityStartTime, ref Mye2etxn);
                            }
                            else if (isAssociateBTPlusMarker)
                            {
                                BTPlusMarkerProcessor BTPlusMarkerProcessor = new BTPlusMarkerProcessor();
                                BTPlusMarkerProcessor.AssociateBTPlusMarkerRequestMapper(requestOrderRequest, guid, ActivityStartTime, ref Mye2etxn);
                            }
                            else if (isCONSUMERBROADBAND)
                            {
                                BTCONSUMERBROADBANDProcessor BTConsumerBroadBandProcessor = new BTCONSUMERBROADBANDProcessor();
                                BTConsumerBroadBandProcessor.BTCONSUMERBROADBANDReqeustMapper(requestOrderRequest, guid, ActivityStartTime, ref Mye2etxn);
                            }
                            else if (VASRequestValidator.ValidateVASRequest(requestOrderRequest, ref errorString))
                            {
                                order = VASRequestProcessor.VASRequestMapper(requestOrderRequest, ref Mye2etxn);
                                MSEOSaaSMapper.SubmitOrder(order, requestOrderRequest, ref Mye2etxn);
                            }
                            else
                            {
                                if (requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToUpper() == "SERVICENAME" && (ic.Value.Equals("FP", StringComparison.OrdinalIgnoreCase) || ic.Value.Equals("NPPLUS2", StringComparison.OrdinalIgnoreCase))) && requestOrderRequest.Order.OrderItem[0].Action.Code.Equals("cancel", StringComparison.OrdinalIgnoreCase))
                                {
                                    bool isFirstCall = true;
                                    string[] FPItemSku = ConfigurationManager.AppSettings["FPITEMSKU"].Split(new char[] { ';' });
                                    string[] NPPItemSku = ConfigurationManager.AppSettings["NPPITEMSKU"].Split(new char[] { ';' });
                                    BT.SaaS.IspssAdapter.Dnp.GetClientProfileResponse profile = new BT.SaaS.IspssAdapter.Dnp.GetClientProfileResponse();
                                    List<string> itemSkuList = new List<string>();
                                    List<BT.SaaS.IspssAdapter.Dnp.ClientIdentity[]> linkedClientList = new List<BT.SaaS.IspssAdapter.Dnp.ClientIdentity[]>();
                                    foreach (OrderItem orderItem in requestOrderRequest.Order.OrderItem)
                                    {
                                        string productcodeWithServiceName = orderItem.Specification[0].Identifier.Value1 + "-" + orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToUpper() == "SERVICENAME").FirstOrDefault().Value;

                                        if (isFirstCall)
                                        {
                                            string serviceEmail = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("serviceemail", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                                            if (!serviceEmail.Contains("@btinternet.com"))
                                                serviceEmail = serviceEmail + "@btinternet.com";

                                            if (DnpWrapper.getProfileLinkedClients(serviceEmail, "ISPSERVICEID", ref profile))
                                            {
                                                if (profile.getClientProfileRes.linkedClientProfile != null && profile.getClientProfileRes.linkedClientProfile.Count() > 0)
                                                {
                                                    var linkedClients = from linkedclient in profile.getClientProfileRes.linkedClientProfile
                                                                        where linkedclient.client.clientIdentity.ToList().Exists(id => id.managedIdentifierDomain.value.ToUpper() == "GUARD" && id.clientIdentityStatus.value.ToUpper() == "ACTIVE")
                                                                        select linkedclient.client.clientIdentity;
                                                    linkedClientList = linkedClients.ToList();
                                                    foreach (BT.SaaS.IspssAdapter.Dnp.ClientIdentity[] clientIdentity in linkedClientList)
                                                    {
                                                        if (clientIdentity != null)
                                                        {
                                                            List<BT.SaaS.IspssAdapter.Dnp.ClientIdentityValidation> clientIdentityCharacteristics = DnpWrapper.GetClientIdentityCharacterstics(clientIdentity.FirstOrDefault().value, "GUARD");
                                                            if (clientIdentityCharacteristics != null && clientIdentityCharacteristics.Count > 0)
                                                            {
                                                                string itemsku_value = clientIdentityCharacteristics.Where(ic => ic.name.ToUpper() == "ITEMSKU").FirstOrDefault().value;
                                                                itemSkuList.Add(itemsku_value);
                                                            }
                                                        }
                                                    }
                                                    isFirstCall = false;
                                                }
                                                else
                                                {
                                                    throw new DnpException("Getting Linked Profile as NULL");
                                                }
                                            }
                                        }

                                        if (productcodeWithServiceName.Equals("S0220584-FP"))
                                        {
                                            if (itemSkuList.Exists(isku => FPItemSku.Any(iskufp => iskufp.Equals(isku, StringComparison.OrdinalIgnoreCase))))
                                            {
                                                orderItem.Status = Settings1.Default.AcceptedStatus;
                                            }
                                            else
                                            {
                                                orderItem.Status = Settings1.Default.IgnoredStatus;
                                            }
                                        }
                                        else if (productcodeWithServiceName.Equals("S0162974-NPPLUS2"))
                                        {
                                            if (itemSkuList.Exists(ic => NPPItemSku.Any(isku => isku.Equals(ic, StringComparison.OrdinalIgnoreCase))))
                                            {
                                                orderItem.Status = Settings1.Default.AcceptedStatus;
                                            }
                                            else
                                            {
                                                orderItem.Status = Settings1.Default.IgnoredStatus;
                                            }
                                        }

                                    }

                                    // Prepareing order request which having orderItem status is "ACCEPTED"
                                    MSEOAdapter.OrderRequest orderRequest = new OrderRequest();
                                    orderRequest.StandardHeader = requestOrderRequest.StandardHeader;
                                    orderRequest.Order = new BT.SaaS.MSEOAdapter.Order();
                                    orderRequest.Order.Action = requestOrderRequest.Order.Action;
                                    orderRequest.Order.OrderDate = requestOrderRequest.Order.OrderDate;
                                    orderRequest.Order.OrderIdentifier = requestOrderRequest.Order.OrderIdentifier;

                                    List<OrderItem> orderItemList = new List<OrderItem>(); ;
                                    foreach (OrderItem orderItem in requestOrderRequest.Order.OrderItem)
                                    {
                                        if (orderItem.Status.Equals(Settings1.Default.AcceptedStatus, StringComparison.OrdinalIgnoreCase))
                                        {
                                            orderItemList.Add(orderItem);
                                        }
                                    }
                                    orderRequest.Order.OrderItem = orderItemList.ToArray();
                                    orderRequest.Order.Status = requestOrderRequest.Order.Status;
                                    if (orderRequest.Order.OrderItem.Count() > 0)
                                    {
                                        order = MSEOSaaSMapper.MapRequest(orderRequest);
                                        MSEOSaaSMapper.SubmitOrder(order, requestOrderRequest, ref Mye2etxn);
                                    }
                                    else
                                    {
                                        order = new BT.SaaS.Core.Shared.Entities.Order();
                                        order.ProductOrderItems = new List<BT.SaaS.Core.Shared.Entities.ProductOrderItem>();
                                        MSEOSaaSMapper.SubmitOrder(order, requestOrderRequest, ref Mye2etxn);
                                    }
                                }
                                else if (ConfigurationManager.AppSettings["EmailScodes"].Split(',').Contains(requestOrderRequest.Order.OrderItem[0].Specification[0].Identifier.Value1) && !requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(inst => inst.Name.ToLower().Equals("ispserviceclass") && inst.Value.ToUpper().Equals("ISP_SC_PM"))
                                         && requestOrderRequest.Order.OrderItem[0].Action.Code.Equals("Create", StringComparison.OrdinalIgnoreCase))
                                {
                                    string emailDataStore = "CISP";
                                    if (ConfigurationManager.AppSettings["MigrationSwitch"].Equals("ON", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToLower() == "reservationid"))
                                        {
                                            string reservationID = requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("reservationid")).FirstOrDefault().Value;

                                            emailDataStore = VASRequestValidator.validateReservationID(reservationID);
                                            requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("emaildatastore")).FirstOrDefault().Value = emailDataStore;
                                            if (emailDataStore == "D&P")
                                            {
                                                order = MSEOSaaSMapper.MapRequest(requestOrderRequest);
                                                if (order.ProductOrderItems[0].Header.Status != BT.SaaS.Core.Shared.Entities.DataStatusEnum.done)
                                                {
                                                    MSEOSaaSMapper.SubmitOrder(order, requestOrderRequest, ref Mye2etxn);
                                                }
                                                else
                                                {
                                                    InboundQueueDAL.UpdateQueueRawXML(Settings1.Default.InvalideRequestCode, "Ignored as the EmaildataStore value is D&P based on Emailstatus in DNP", requestOrderRequest.Order.OrderIdentifier.Value);
                                                    foreach (OrderItem orderItem in requestOrderRequest.Order.OrderItem)
                                                    {
                                                        orderItem.Status = "Ignored";
                                                    }
                                                    BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter : The Order " + requestOrderRequest.Order.OrderIdentifier.Value + " was ignored by MSEO as the EmailDatastore is " + emailDataStore);
                                                    notification.sendNotification(false, false, "001", "Ignored as the EmaildataStore value is D&P based on Emailstatus in DNP", ref Mye2etxn, true);
                                                }
                                            }
                                            else if (emailDataStore == "CISP")
                                            {
                                                InboundQueueDAL.UpdateQueueRawXML(Settings1.Default.InvalideRequestCode, "Ignored as EmailDataStore value  is CISP based ReservationID Format", requestOrderRequest.Order.OrderIdentifier.Value);
                                                foreach (OrderItem orderItem in requestOrderRequest.Order.OrderItem)
                                                {
                                                    orderItem.Status = "Ignored";
                                                }
                                                BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter : The Order " + requestOrderRequest.Order.OrderIdentifier.Value + " was ignored by MSEO as the EmailDatastore is " + emailDataStore);
                                                notification.sendNotification(false, false, "001", "Ignored as the EmaildataStore value is CISP based on Reservation ID format", ref Mye2etxn, true);
                                            }
                                        }

                                        else
                                        {
                                            requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("emaildatastore")).FirstOrDefault().Value = "CISP";
                                            InboundQueueDAL.UpdateQueueRawXML(Settings1.Default.IgnoredStatus, "Product in the request are Ignored as ReservationID is not present in the request", requestOrderRequest.Order.OrderIdentifier.Value);

                                            foreach (OrderItem orderItem in requestOrderRequest.Order.OrderItem)
                                            {
                                                orderItem.Status = "Ignored";
                                            }
                                            notification.sendNotification(false, false, "001", "Ignored as the ReservationID is not present still Migration Swtich Vlaue is ON ", ref Mye2etxn, true);
                                            BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter : The Order " + requestOrderRequest.Order.OrderIdentifier.Value + " ignored as the Migation Switch Value equals to ON and ReservationID is not present in Request");
                                        }

                                    }
                                    else if (ConfigurationManager.AppSettings["MigrationSwitch"].Equals("OFF", StringComparison.OrdinalIgnoreCase))
                                    {
                                        requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("emaildatastore", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value = "D&P";
                                        //if (requestOrderRequest.Order.OrderItem[0].Action.Code.Equals("create", StringComparison.OrdinalIgnoreCase))
                                        //{


                                        order = MSEOSaaSMapper.MapRequest(requestOrderRequest);
                                        if (order.ProductOrderItems[0].Header.Status != BT.SaaS.Core.Shared.Entities.DataStatusEnum.done)
                                        {
                                            MSEOSaaSMapper.SubmitOrder(order, requestOrderRequest, ref Mye2etxn);
                                        }
                                        else
                                        {
                                            InboundQueueDAL.UpdateQueueRawXML(Settings1.Default.IgnoredStatus, "Ignored as the EmaildataStore value is D&P based on Emailstatus in DNP", requestOrderRequest.Order.OrderIdentifier.Value);
                                            foreach (OrderItem orderItem in requestOrderRequest.Order.OrderItem)
                                            {
                                                orderItem.Status = "Ignored";
                                            }
                                            BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter : The Order " + requestOrderRequest.Order.OrderIdentifier.Value + " was ignored by MSEO as the EmailDatastore is " + emailDataStore);
                                            notification.sendNotification(false, false, "001", "Ignored as the EmaildataStore value is D&P based on Emailstatus in DNP", ref Mye2etxn, true);
                                        }
                                        //}

                                    }
                                    else if (ConfigurationManager.AppSettings["MigrationSwitch"].Equals("LEGACY", StringComparison.OrdinalIgnoreCase))
                                    {
                                        requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("emaildatastore")).FirstOrDefault().Value = "CISP";
                                        InboundQueueDAL.UpdateQueueRawXML(Settings1.Default.IgnoredStatus, "Product in the request are Ignored as the Migration Switch value is LEGACY", requestOrderRequest.Order.OrderIdentifier.Value);

                                        foreach (OrderItem orderItem in requestOrderRequest.Order.OrderItem)
                                        {
                                            orderItem.Status = "Ignored";
                                        }
                                        notification.sendNotification(false, false, "001", "Ignored as the Migration Swtich Vlaue is LEGACY", ref Mye2etxn, true);
                                        BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter : The Order " + requestOrderRequest.Order.OrderIdentifier.Value + " ignored as the Migation Switch Value equals to Legacy");
                                    }

                                }
                                //Modify Order Journey for VASRegrade and OCB orders
                                else if ((requestOrderRequest.Order.OrderItem[0].Action.Code.Equals("Modify", StringComparison.OrdinalIgnoreCase)
                                    && (ConfigurationManager.AppSettings["EmailScodes"].Split(',').Contains(requestOrderRequest.Order.OrderItem[0].Specification[0].Identifier.Value1) || ConfigurationManager.AppSettings["PremiumEmailScodes"].Split(',').Contains(requestOrderRequest.Order.OrderItem[0].Specification[0].Identifier.Value1))) || (requestOrderRequest.Order.OrderItem[0].Action.Code.Equals("Modify", StringComparison.OrdinalIgnoreCase)
                                    && (requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(inst => inst.Name.ToLower().Equals("suspendtype")))))
                                {
                                    string BakId = string.Empty;
                                    string accountId = string.Empty;
                                    string emailDataStore = "CISP";
                                    string email_Supplier = ConfigurationManager.AppSettings["OWM_Email_Supplier"];

                                    if (requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToLower().Equals("billaccountnumber")) && !string.IsNullOrEmpty(requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("billaccountnumber")).FirstOrDefault().Value))
                                    {
                                        BakId = requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("billaccountnumber")).FirstOrDefault().Value;
                                    }
                                    ClientServiceInstanceV1[] serviceIntances = DnpWrapper.getServiceInstanceV1(BakId, "VAS_BILLINGACCOUNT_ID", ConfigurationManager.AppSettings["DanteIdentifier"]);
                                    if (serviceIntances != null)
                                    {
                                        if (serviceIntances[0].clientServiceInstanceStatus.value.Equals("active", StringComparison.OrdinalIgnoreCase))
                                        {
                                            var srvcChars = (from si in serviceIntances
                                                             where (si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase) && si.clientServiceInstanceCharacteristic != null)
                                                             select si).FirstOrDefault().clientServiceInstanceCharacteristic;

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
                                            }
                                            if (!String.IsNullOrEmpty(accountId.Trim()))
                                            {
                                                if (ConfigurationManager.AppSettings["DnPManagedEmailSupplier"].Split(',').Contains(email_Supplier, StringComparer.OrdinalIgnoreCase))
                                                {
                                                    accountId = string.Empty;
                                                }
                                                else
                                                {
                                                    requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("emaildatastore")).FirstOrDefault().Value = emailDataStore;
                                                    throw new DnpException("Ignored the request as EmailDataStore is CISP");
                                                }
                                            }
                                        }
                                        else
                                        {
                                            requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("emaildatastore")).FirstOrDefault().Value = emailDataStore;
                                            throw new DnpException("Profile doesn't have active BTIEMAIL service in DNP");
                                        }

                                        if (String.IsNullOrEmpty(accountId.Trim()))
                                        {
                                            emailDataStore = "D&P";
                                            requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("emaildatastore")).FirstOrDefault().Value = emailDataStore;

                                            if (requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(inst => inst.Name.ToLower().Equals("suspendtype")))
                                            {
                                                string suspend_Type = requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(inst => inst.Name.ToLower().Equals("suspendtype")).FirstOrDefault().Value;
                                                string suspend_Action = requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(inst => inst.Name.ToLower().Equals("suspendaction")).FirstOrDefault().Value;
                                                string ISPServiceClass = requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(inst => inst.Name.ToLower().Equals("ispserviceclass")).FirstOrDefault().Value;

                                                if (suspend_Type.Equals("S0145881", StringComparison.OrdinalIgnoreCase) && ISPServiceClass.Equals("ISP_SC_PM", StringComparison.OrdinalIgnoreCase))
                                                {

                                                    GetBatchProfileV1Res ProfileRes = DnpWrapper.GetServiceUserProfilesV1ForDante(BakId, "VAS_BILLINGACCOUNT_ID");

                                                    if (ProfileRes != null && ProfileRes.clientProfileV1 != null && ProfileRes.clientProfileV1.Count() > 0)
                                                    {
                                                        foreach (ClientProfileV1 clientProfile in ProfileRes.clientProfileV1)
                                                        {
                                                            if (clientProfile.client != null && clientProfile.client.clientIdentity != null && clientProfile.clientServiceInstanceV1 != null)
                                                            {
                                                                foreach (ClientServiceInstanceV1 CSI in clientProfile.clientServiceInstanceV1)
                                                                {
                                                                    if (CSI.clientServiceInstanceIdentifier.value.Equals("BTIEMAIL:DEFAULT") && (BakId.Equals(CSI.serviceIdentity.ToList().Where(si => si.domain.ToUpper().Equals("VAS_BILLINGACCOUNT_ID")).FirstOrDefault().value)))
                                                                    {
                                                                        foreach (BT.SaaS.IspssAdapter.Dnp.ClientIdentity clientidentity in clientProfile.client.clientIdentity)
                                                                        {
                                                                            if (clientidentity.managedIdentifierDomain.value.ToLower() == "btiemailid")
                                                                            {
                                                                                if (clientidentity.clientIdentityStatus.value.ToLower() == "active")
                                                                                {
                                                                                    Active_EmailList.Add(clientidentity.value);
                                                                                }
                                                                                else if (clientidentity.clientIdentityStatus.value.Equals("barred-abuse", StringComparison.OrdinalIgnoreCase)
                                                                                    && clientidentity.clientIdentityValidation.ToList().Exists(clval => clval.name.Equals("BARRED_REASON") && clval.value.Equals("PM_DebtBarred", StringComparison.OrdinalIgnoreCase)))
                                                                                {
                                                                                    BarredAbuse_EmailList.Add(clientidentity.value);
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                    if (suspend_Action.Equals("Add", StringComparison.OrdinalIgnoreCase) && Active_EmailList != null && Active_EmailList.Count > 0)
                                                    {
                                                        //preparing productorderxml...
                                                        order = MSEOSaaSMapper.MapRequest(requestOrderRequest);
                                                        // adding list of actvieemailids to the productorderxml
                                                        SAASPE.Attribute activeEmailsList = new BT.SaaS.Core.Shared.Entities.Attribute();
                                                        activeEmailsList.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                                        activeEmailsList.Name = "ListOfEmails";
                                                        activeEmailsList.Value = string.Join(";", Active_EmailList.ToArray());

                                                        order.ProductOrderItems[0].RoleInstances[0].Attributes.Add(activeEmailsList);
                                                        //modifing the email supplier name according to the profile.
                                                        if (order.ProductOrderItems[0].RoleInstances[0].Attributes.ToList().Exists(attr => attr.Name.Equals("EMAILSUPPLIER", StringComparison.OrdinalIgnoreCase)))
                                                        {
                                                            order.ProductOrderItems[0].RoleInstances[0].Attributes.ToList().Where(attr => attr.Name.Equals("EMAILSUPPLIER", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value = email_Supplier;
                                                        }

                                                        if (order.ProductOrderItems[0].Header.Status != BT.SaaS.Core.Shared.Entities.DataStatusEnum.done)
                                                        {
                                                            MSEOSaaSMapper.SubmitOrder(order, requestOrderRequest, ref Mye2etxn);
                                                        }
                                                    }
                                                    else if (suspend_Action.Equals("Delete", StringComparison.OrdinalIgnoreCase) && BarredAbuse_EmailList != null && BarredAbuse_EmailList.Count > 0)
                                                    {
                                                        //preparing productorderxml...
                                                        order = MSEOSaaSMapper.MapRequest(requestOrderRequest);
                                                        // adding list of barred-abuse emailids to the productorderxml
                                                        SAASPE.Attribute BarredAbuseEmailList = new BT.SaaS.Core.Shared.Entities.Attribute();
                                                        BarredAbuseEmailList.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                                        BarredAbuseEmailList.Name = "InactiveEmailList";
                                                        BarredAbuseEmailList.Value = string.Join(";", BarredAbuse_EmailList.ToArray());

                                                        order.ProductOrderItems[0].RoleInstances[0].Attributes.Add(BarredAbuseEmailList);
                                                        //modifing the email supplier name according to the profile.
                                                        if (order.ProductOrderItems[0].RoleInstances[0].Attributes.ToList().Exists(attr => attr.Name.Equals("EMAILSUPPLIER", StringComparison.OrdinalIgnoreCase)))
                                                        {
                                                            order.ProductOrderItems[0].RoleInstances[0].Attributes.ToList().Where(attr => attr.Name.Equals("EMAILSUPPLIER", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value = email_Supplier;
                                                        }

                                                        if (order.ProductOrderItems[0].Header.Status != BT.SaaS.Core.Shared.Entities.DataStatusEnum.done)
                                                        {
                                                            MSEOSaaSMapper.SubmitOrder(order, requestOrderRequest, ref Mye2etxn);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        string errMsg = suspend_Action.Equals("Delete", StringComparison.OrdinalIgnoreCase) ? "Request for OCB premium undo-barred-abuse was ignored as there are no barred-abuse emailidentities to activate" : "Request for OCB premium cease was ignored as there are no active emailidentities to barred-abuse";
                                                        InboundQueueDAL.UpdateQueueRawXML(Settings1.Default.IgnoredStatus, errMsg, requestOrderRequest.Order.OrderIdentifier.Value);
                                                        requestOrderRequest.Order.OrderItem[0].Status = "Ignored";
                                                        BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter : The Order " + requestOrderRequest.Order.OrderIdentifier.Value + errMsg);
                                                        notification.sendNotification(false, isAck, "001", errMsg, ref Mye2etxn);
                                                    }
                                                }
                                                else
                                                {
                                                    string errMsg = "Request for OCB premium barred or undo-barred-abuse was ignored as suspendType is not matching";
                                                    InboundQueueDAL.UpdateQueueRawXML(Settings1.Default.IgnoredStatus, errMsg, requestOrderRequest.Order.OrderIdentifier.Value);
                                                    requestOrderRequest.Order.OrderItem[0].Status = "Ignored";
                                                    BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter : The Order " + requestOrderRequest.Order.OrderIdentifier.Value + errMsg);
                                                    notification.sendNotification(false, isAck, "001", errMsg, ref Mye2etxn);
                                                }
                                            }
                                            else
                                            {
                                                requestOrderRequest.Order.OrderItem[0].Action.Reason = "VASRegrade";
                                                order = MSEOSaaSMapper.MapRequest(requestOrderRequest);
                                                // passing email_supplier value from service instance characterstics value
                                                order.ProductOrderItems[0].RoleInstances[0].Attributes.ToList().Where(attr => attr.Name.Equals("emailsupplier", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value = email_Supplier;
                                                if (order.ProductOrderItems[0].Header.Status != BT.SaaS.Core.Shared.Entities.DataStatusEnum.done)
                                                {
                                                    MSEOSaaSMapper.SubmitOrder(order, requestOrderRequest, ref Mye2etxn);
                                                }
                                                else
                                                {
                                                    InboundQueueDAL.UpdateQueueRawXML(Settings1.Default.IgnoredStatus, "Request for ISP Regrade was ignored with EmailDatastore as " + emailDataStore, requestOrderRequest.Order.OrderIdentifier.Value);
                                                    foreach (OrderItem orderItem in requestOrderRequest.Order.OrderItem)
                                                    {
                                                        orderItem.Status = "Ignored";
                                                    }
                                                    BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter : The Order " + requestOrderRequest.Order.OrderIdentifier.Value + " was ignored by MSEO as the EmailDatastore is " + emailDataStore);
                                                    notification.sendNotification(false, false, "001", "Request for ISP Regrade was ignored with EmailDatastore as " + emailDataStore, ref Mye2etxn, true);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("emaildatastore")).FirstOrDefault().Value = emailDataStore;
                                            throw new DnpException("Ignored the request as EmailDataStore is CISP");
                                        }
                                    }
                                    else
                                    {
                                        requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("emaildatastore")).FirstOrDefault().Value = emailDataStore;
                                        throw new DnpException("Profile doesn't have active services in DNP");
                                    }
                                }
                                // premium email OCB barredobuse scenario.
                                /*else if ((requestOrderRequest.Order.OrderItem[0].Action.Code.Equals("Cancel", StringComparison.OrdinalIgnoreCase)
                                    && (ConfigurationManager.AppSettings["PremiumEmailScodes"].Split(',').Contains(requestOrderRequest.Order.OrderItem[0].Specification[0].Identifier.Value1))
                                    && (requestOrderRequest.Order.OrderItem[0].Action.Reason.Equals("OCBPremiumEmailCease", StringComparison.OrdinalIgnoreCase))))
                                {
                                    string BakId = string.Empty;

                                    if (requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToLower().Equals("billaccountnumber")) && !string.IsNullOrEmpty(requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("billaccountnumber")).FirstOrDefault().Value))
                                    {
                                        BakId = requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("billaccountnumber")).FirstOrDefault().Value;
                                    }
                                    GetClientProfileV1Res ProfileRes = DnpWrapper.GetClientProfileV1(BakId, "VAS_BILLINGACCOUNT_ID");

                                    if (ProfileRes != null && ProfileRes.clientProfileV1 != null && ProfileRes.clientProfileV1.client != null && ProfileRes.clientProfileV1.client.clientIdentity != null)
                                    {
                                        foreach (BT.SaaS.IspssAdapter.Dnp.ClientIdentity clientidentity in ProfileRes.clientProfileV1.client.clientIdentity)
                                        {
                                            if (clientidentity.managedIdentifierDomain.value.ToLower() == "btiemailid" && clientidentity.clientIdentityStatus.value.ToLower() == "active")
                                            {
                                                Active_EmailList.Add(clientidentity.value);
                                            }
                                        }
                                    }
                                    if (Active_EmailList != null && Active_EmailList.Count > 0)
                                    {
                                        //preparing productorderxml...
                                        order = MSEOSaaSMapper.MapRequest(requestOrderRequest);
                                        // adding list of actvieemailids to the productorderxml
                                        SAASPE.Attribute activeEmailsList = new BT.SaaS.Core.Shared.Entities.Attribute();
                                        activeEmailsList.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                        activeEmailsList.Name = "ListOfEmails";
                                        activeEmailsList.Value = string.Join(";", Active_EmailList.ToArray());

                                        order.ProductOrderItems[0].RoleInstances[0].Attributes.Add(activeEmailsList);

                                        if (order.ProductOrderItems[0].Header.Status != BT.SaaS.Core.Shared.Entities.DataStatusEnum.done)
                                        {
                                            MSEOSaaSMapper.MSEOSaaSMapper.SubmitOrder(order, requestOrderRequest, ref Mye2etxn);
                                        }
                                    }
                                    else
                                    {
                                        InboundQueueDAL.UpdateQueueRawXML(Settings1.Default.IgnoredStatus, "Request for OCB premium cease was ignored as there are no active emailidentities to barred-abuse", requestOrderRequest.Order.OrderIdentifier.Value);
                                        requestOrderRequest.Order.OrderItem[0].Status = "Ignored";
                                        BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter : The Order " + requestOrderRequest.Order.OrderIdentifier.Value + " was ignored by MSEO as there are no active emailidentities to barred-abuse");
                                        notification.sendNotification(false, isAck, "001", "Request for OCB premium cease ignored as there are no active emailidentities to barred-abuse", ref Mye2etxn);
                                    }
                                }
                                // premium email OCB undo-barred-abuse scenario.
                                else if ((requestOrderRequest.Order.OrderItem[0].Action.Code.Equals("create", StringComparison.OrdinalIgnoreCase)
                    && (ConfigurationManager.AppSettings["PremiumEmailScodes"].Split(',').Contains(requestOrderRequest.Order.OrderItem[0].Specification[0].Identifier.Value1)
                    || (requestOrderRequest.Order.OrderItem[0].Action.Reason.Equals("OCBUndobar", StringComparison.OrdinalIgnoreCase)))))
                                {
                                    string BakId = string.Empty;
                                    string accountId = string.Empty;
                                    string email_Supplier = ConfigurationManager.AppSettings["OWM_Email_Supplier"];


                                    if (requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToLower().Equals("billaccountnumber")) && !string.IsNullOrEmpty(requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("billaccountnumber")).FirstOrDefault().Value))
                                    {
                                        BakId = requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("billaccountnumber")).FirstOrDefault().Value;
                                    }
                                    GetClientProfileV1Res ProfileRes = DnpWrapper.GetClientProfileV1(BakId, "VAS_BILLINGACCOUNT_ID");

                                    if (ProfileRes != null && ProfileRes.clientProfileV1 != null && ProfileRes.clientProfileV1.client != null && ProfileRes.clientProfileV1.client.clientIdentity != null)
                                    {
                                        foreach (BT.SaaS.IspssAdapter.Dnp.ClientIdentity clientidentity in ProfileRes.clientProfileV1.client.clientIdentity)
                                        {
                                            if (clientidentity.managedIdentifierDomain.value.ToLower() == "btiemailid" && clientidentity.clientIdentityStatus.value.Equals("barred-abuse", StringComparison.OrdinalIgnoreCase)
                                                && clientidentity.clientIdentityValidation.ToList().Exists(clval => clval.name.Equals("BARRED_REASON") && clval.value.Equals("PM_DebtBarred")))
                                            {
                                                BarredAbuse_EmailList.Add(clientidentity.value);
                                            }
                                        }
                                    }
                                    ClientServiceInstanceV1[] serviceIntances = DnpWrapper.getServiceInstanceV1(BakId, "VAS_BILLINGACCOUNT_ID", ConfigurationManager.AppSettings["DanteIdentifier"]);
                                    if (serviceIntances != null)
                                    {
                                        if (serviceIntances[0].clientServiceInstanceStatus.value.Equals("active", StringComparison.OrdinalIgnoreCase))
                                        {
                                            var srvcChars = (from si in serviceIntances
                                                             where (si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase) && si.clientServiceInstanceCharacteristic != null)
                                                             select si).FirstOrDefault().clientServiceInstanceCharacteristic;

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
                                            }
                                        }
                                        else
                                        {
                                            //requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("emaildatastore")).FirstOrDefault().Value = emailDataStore;
                                            throw new DnpException("Profile doesn't have active BTIEMAIL service in DNP");
                                        }
                                    }

                                    if (BarredAbuse_EmailList != null && BarredAbuse_EmailList.Count > 0)
                                    {
                                        //preparing productorderxml...
                                        order = MSEOSaaSMapper.MapRequest(requestOrderRequest);
                                        // adding list of barred-abuse emailids to the productorderxml
                                        SAASPE.Attribute BarredAbuseEmailList = new BT.SaaS.Core.Shared.Entities.Attribute();
                                        BarredAbuseEmailList.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                        BarredAbuseEmailList.Name = "InactiveEmailList";
                                        BarredAbuseEmailList.Value = string.Join(";", BarredAbuse_EmailList.ToArray());

                                        order.ProductOrderItems[0].RoleInstances[0].Attributes.Add(BarredAbuseEmailList);

                                        if (order.ProductOrderItems[0].RoleInstances[0].Attributes.ToList().Exists(attr => attr.Name.Equals("EMAILSUPPLIER", StringComparison.OrdinalIgnoreCase)))
                                        {
                                            order.ProductOrderItems[0].RoleInstances[0].Attributes.ToList().Where(attr => attr.Name.Equals("EMAILSUPPLIER", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value = email_Supplier;
                                        }

                                        if (order.ProductOrderItems[0].Header.Status != BT.SaaS.Core.Shared.Entities.DataStatusEnum.done)
                                        {
                                            MSEOSaaSMapper.MSEOSaaSMapper.SubmitOrder(order, requestOrderRequest, ref Mye2etxn);
                                        }
                                    }
                                    else
                                    {
                                        InboundQueueDAL.UpdateQueueRawXML(Settings1.Default.IgnoredStatus, "Request for OCB premium undo-barred-abuse was ignored as there are no barred-abuse emailidentities to activate", requestOrderRequest.Order.OrderIdentifier.Value);
                                        requestOrderRequest.Order.OrderItem[0].Status = "Ignored";
                                        BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter : The Order " + requestOrderRequest.Order.OrderIdentifier.Value + " was ignored by MSEO as there are barred-abuse emailidentities to activate");
                                        notification.sendNotification(false, isAck, "001", "Request for OCB premium undo-barred-abuse ignored as there are barred-abuse emailidentities to activate", ref Mye2etxn);
                                    }
                                }*/
                                //premium email isp leg
                                // else if (requestOrderRequest.Order.OrderItem[0].Specification[0].Identifier.Value1.Equals("S0200762") && requestOrderRequest.Order.OrderItem[0].Action.Code.Equals("Create", StringComparison.OrdinalIgnoreCase))
                                else if (requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(inst => inst.Name.ToLower().Equals("ispserviceclass") && inst.Value.ToUpper().Equals("ISP_SC_PM"))
                                    && requestOrderRequest.Order.OrderItem[0].Action.Code.Equals("Create", StringComparison.OrdinalIgnoreCase))
                                {
                                    string emailDataStore = string.Empty;
                                    string emailSupplier = "CriticalPath";
                                    string emailname = requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("emailname")).FirstOrDefault().Value;

                                    GetClientProfileV1Res emailprofile = new GetClientProfileV1Res();
                                    emailprofile = DnpWrapper.GetClientProfileV1(emailname, "BTIEMAILID");
                                    if (emailprofile != null && emailprofile.clientProfileV1 != null && emailprofile.clientProfileV1.clientServiceInstanceV1 != null)
                                    {
                                        emailDataStore = DanteRequestProcessor.GenerateEmailDataStore(emailprofile.clientProfileV1, "premiumemail", emailname, ref emailSupplier);
                                        if (emailDataStore == "D&P")
                                        {
                                            requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("emaildatastore")).FirstOrDefault().Value = "D&P";
                                            InboundQueueDAL.UpdateQueueRawXML(Settings1.Default.IgnoredStatus, "Ignored the ISP Leg of Premium Email Provision and EmailDataStore is DNP", requestOrderRequest.Order.OrderIdentifier.Value);

                                            foreach (OrderItem orderItem in requestOrderRequest.Order.OrderItem)
                                            {
                                                orderItem.Status = "Ignored";
                                            }
                                            notification.sendNotification(false, false, "001", "Ignored the ISP Leg of Premium Email Provision and EmailDataStore is DNP", ref Mye2etxn, true);
                                            BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter : The Order " + requestOrderRequest.Order.OrderIdentifier.Value + "Ignored the ISP Leg of Premium Email Provision , for the given emailname:" + emailname);
                                        }
                                        else
                                        {
                                            requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("emaildatastore")).FirstOrDefault().Value = "CISP";
                                            InboundQueueDAL.UpdateQueueRawXML(Settings1.Default.IgnoredStatus, "Ignored the ISP Leg of Premium Email Provision and EmailDataStore is CISP", requestOrderRequest.Order.OrderIdentifier.Value);

                                            foreach (OrderItem orderItem in requestOrderRequest.Order.OrderItem)
                                            {
                                                orderItem.Status = "Ignored";
                                            }
                                            notification.sendNotification(false, false, "001", "Ignored the ISP Leg of Premium Email Provision and EmailDataStore is CISP", ref Mye2etxn, true);
                                            BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter : The Order " + requestOrderRequest.Order.OrderIdentifier.Value + "Ignored the ISP Leg of Premium Email Provision , for the given emailname:" + emailname);

                                        }
                                    }
                                    else
                                    {
                                        requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("emaildatastore")).FirstOrDefault().Value = "CISP";
                                        InboundQueueDAL.UpdateQueueRawXML(Settings1.Default.IgnoredStatus, "Ignored the ISP Leg of Premium Email Provision and EmailDataStore is CISP", requestOrderRequest.Order.OrderIdentifier.Value);

                                        foreach (OrderItem orderItem in requestOrderRequest.Order.OrderItem)
                                        {
                                            orderItem.Status = "Ignored";
                                        }
                                        notification.sendNotification(false, false, "001", "Ignored the ISP Leg of Premium Email Provision and EmailDataStore is CISP", ref Mye2etxn, true);
                                        BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter : The Order " + requestOrderRequest.Order.OrderIdentifier.Value + "Ignored the ISP Leg of Premium Email Provision , for the given emailname:" + emailname);

                                    }
                                }
                                //ISP email or premium email cease
                                else if ((ConfigurationManager.AppSettings["EmailScodes"].Split(',').Contains(requestOrderRequest.Order.OrderItem[0].Specification[0].Identifier.Value1) || ConfigurationManager.AppSettings["PremiumEmailScodes"].Split(',').Contains(requestOrderRequest.Order.OrderItem[0].Specification[0].Identifier.Value1)) && requestOrderRequest.Order.OrderItem[0].Action.Code.Equals("Cancel", StringComparison.OrdinalIgnoreCase))
                                {
                                    if (ConfigurationManager.AppSettings["TransitionSwitch"].Equals("Strategic", StringComparison.OrdinalIgnoreCase))
                                    {
                                        InboundQueueDAL.UpdateQueueRawXML(Settings1.Default.IgnoredStatus, "Product in the request are Ignored as the Transition Switch value is STRATEGIC", requestOrderRequest.Order.OrderIdentifier.Value);
                                        //requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("emaildatastore")).FirstOrDefault().Value = "CISP";
                                        foreach (OrderItem orderItem in requestOrderRequest.Order.OrderItem)
                                        {
                                            orderItem.Status = "Ignored";
                                        }
                                        notification.sendNotification(false, false, "001", "Ignored as the Transition Swtich Vlaue is STRATEGIC", ref Mye2etxn, true);
                                        BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter : The Order " + requestOrderRequest.Order.OrderIdentifier.Value + " ignored as the Transition Switch Value equals to Strategic");
                                    }
                                    else if (ConfigurationManager.AppSettings["TransitionSwitch"].Equals("Transition-1", StringComparison.OrdinalIgnoreCase) || ConfigurationManager.AppSettings["TransitionSwitch"].Equals("Transition-2", StringComparison.OrdinalIgnoreCase))
                                    {
                                        try
                                        {
                                            requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("emaildatastore")).FirstOrDefault().Value = "D&P";
                                            string BACCeaseResult = string.Empty;

                                            order = MSEOSaaSMapper.MapRequest(requestOrderRequest);

                                            //here we are checking the BACCeaseResult attribute is present in the productorderitem or not 
                                            //based on that we are hitting this order to PE.
                                            if (order != null && order.ProductOrderItems != null && order.ProductOrderItems.Count() > 0
                                                && order.ProductOrderItems[0].RoleInstances != null && order.ProductOrderItems[0].RoleInstances[0].Attributes != null
                                                && order.ProductOrderItems[0].RoleInstances[0].Attributes.ToList().Exists(attr => attr.Name.Equals("BACCeaseResult", StringComparison.OrdinalIgnoreCase)))
                                            {
                                                BACCeaseResult = order.ProductOrderItems[0].RoleInstances[0].Attributes.ToList().Where(attr => attr.Name.Equals("BACCeaseResult", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;

                                                if (string.IsNullOrEmpty(BACCeaseResult))
                                                {
                                                    MSEOSaaSMapper.SubmitOrder(order, requestOrderRequest, ref Mye2etxn);
                                                }
                                                else
                                                {
                                                    //sending accept response 
                                                    notification.sendNotification(true, true, string.Empty, string.Empty, ref Mye2etxn);
                                                    BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOMQAdapter send AcceptedNotificationSent for order identifier :" + requestOrderRequest.Order.OrderIdentifier.Value);

                                                    if (BACCeaseResult.Equals("Success", StringComparison.OrdinalIgnoreCase))
                                                    {
                                                        notification.sendNotification(true, false, "004", string.Empty, ref Mye2etxn);
                                                        BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("The Order " + order.Header.OrderID + " was placed successfully");
                                                    }
                                                    else if (BACCeaseResult.Contains("failed"))
                                                    {
                                                        notification.sendNotification(false, isAck, "777", BACCeaseResult.ToString(), ref Mye2etxn);
                                                        BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOMQAdapter PM " + BACCeaseResult.ToString() + " for order identifier :" + requestOrderRequest.Order.OrderIdentifier.Value);
                                                    }
                                                }
                                            }
                                            else
                                                MSEOSaaSMapper.SubmitOrder(order, requestOrderRequest, ref Mye2etxn);
                                        }
                                        catch (DnpException DnPexception)
                                        {
                                            requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("emaildatastore")).FirstOrDefault().Value = "CISP";
                                            InboundQueueDAL.UpdateQueueRawXML(Settings1.Default.IgnoredStatus, DnPexception.Message.ToString(), requestOrderRequest.Order.OrderIdentifier.Value);

                                            foreach (OrderItem orderItem in requestOrderRequest.Order.OrderItem)
                                            {
                                                orderItem.Status = "Ignored";
                                            }

                                            notification.sendNotification(false, false, "001", DnPexception.Message.ToString(), ref Mye2etxn, true);
                                            BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOMQAdapter DNP exception : " + DnPexception.ToString() + " for order identifier :" + requestOrderRequest.Order.OrderIdentifier.Value);
                                        }
                                    }
                                }

                                //Nayan email changes
                                else if (ConfigurationManager.AppSettings["NayanEmailScodes"].Split(',').Contains(requestOrderRequest.Order.OrderItem[0].Specification[0].Identifier.Value1))
                                {
                                    if (requestOrderRequest.Order.OrderItem[0].Action.Code.Equals("Cancel", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (ConfigurationManager.AppSettings["TransitionSwitch"].Equals("Strategic", StringComparison.OrdinalIgnoreCase))
                                        {
                                            InboundQueueDAL.UpdateQueueRawXML(Settings1.Default.IgnoredStatus, "Product in the request are Ignored as the Transition Switch value is STRATEGIC", requestOrderRequest.Order.OrderIdentifier.Value);
                                            foreach (OrderItem orderItem in requestOrderRequest.Order.OrderItem)
                                            {
                                                orderItem.Status = "Ignored";
                                            }
                                            notification.sendNotification(false, false, "001", "Ignored as the Transition Swtich Vlaue is STRATEGIC", ref Mye2etxn, true);
                                            BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter : The Order " + requestOrderRequest.Order.OrderIdentifier.Value + " ignored as the Transition Switch Value equals to Strategic");
                                        }
                                        else if (ConfigurationManager.AppSettings["TransitionSwitch"].Equals("Transition-1", StringComparison.OrdinalIgnoreCase) || ConfigurationManager.AppSettings["TransitionSwitch"].Equals("Transition-2", StringComparison.OrdinalIgnoreCase))
                                        {
                                            try
                                            {
                                                string BACCeaseResult = string.Empty;

                                                order = MSEOSaaSMapper.MapRequest(requestOrderRequest);

                                                //here we are checking the BACCeaseResult attribute is present in the productorderitem or not 
                                                //based on that we are hitting this order to PE.
                                                if (order != null && order.ProductOrderItems != null && order.ProductOrderItems.Count() > 0
                                                    && order.ProductOrderItems[0].RoleInstances != null && order.ProductOrderItems[0].RoleInstances[0].Attributes != null
                                                    && order.ProductOrderItems[0].RoleInstances[0].Attributes.ToList().Exists(attr => attr.Name.Equals("BACCeaseResult", StringComparison.OrdinalIgnoreCase)))
                                                {
                                                    BACCeaseResult = order.ProductOrderItems[0].RoleInstances[0].Attributes.ToList().Where(attr => attr.Name.Equals("BACCeaseResult", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;

                                                    if (string.IsNullOrEmpty(BACCeaseResult))
                                                    {
                                                        MSEOSaaSMapper.SubmitOrder(order, requestOrderRequest, ref Mye2etxn);
                                                    }
                                                    else
                                                    {
                                                        //sending accept response 
                                                        notification.sendNotification(true, true, string.Empty, string.Empty, ref Mye2etxn);
                                                        BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOMQAdapter send AcceptedNotificationSent for order identifier :" + requestOrderRequest.Order.OrderIdentifier.Value);

                                                        if (BACCeaseResult.Equals("Success", StringComparison.OrdinalIgnoreCase))
                                                        {
                                                            notification.sendNotification(true, false, "004", string.Empty, ref Mye2etxn);
                                                            BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("The Order " + order.Header.OrderID + " was placed successfully");
                                                        }
                                                        else if (BACCeaseResult.Contains("failed"))
                                                        {
                                                            notification.sendNotification(false, isAck, "777", BACCeaseResult.ToString(), ref Mye2etxn);
                                                            BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOMQAdapter PM " + BACCeaseResult.ToString() + " for order identifier :" + requestOrderRequest.Order.OrderIdentifier.Value);
                                                        }
                                                    }
                                                }
                                                else
                                                    MSEOSaaSMapper.SubmitOrder(order, requestOrderRequest, ref Mye2etxn);
                                            }
                                            catch (DnpException DnPexception)
                                            {
                                                InboundQueueDAL.UpdateQueueRawXML(Settings1.Default.IgnoredStatus, DnPexception.Message.ToString(), requestOrderRequest.Order.OrderIdentifier.Value);

                                                foreach (OrderItem orderItem in requestOrderRequest.Order.OrderItem)
                                                {
                                                    orderItem.Status = "Ignored";
                                                }

                                                notification.sendNotification(false, false, "001", DnPexception.Message.ToString(), ref Mye2etxn, true);
                                                BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOMQAdapter DNP exception : " + DnPexception.ToString() + " for order identifier :" + requestOrderRequest.Order.OrderIdentifier.Value);
                                            }

                                        }

                                    }
                                    else if (requestOrderRequest.Order.OrderItem[0].Action.Code.Equals("modify", StringComparison.OrdinalIgnoreCase))
                                    {
                                        string accountId = string.Empty;
                                        string BakId = string.Empty;
                                        string email_Supplier = ConfigurationManager.AppSettings["OWM_Email_Supplier"];

                                        if (requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToLower().Equals("billaccountnumber")) && !string.IsNullOrEmpty(requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("billaccountnumber")).FirstOrDefault().Value))
                                        {
                                            BakId = requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("billaccountnumber")).FirstOrDefault().Value;
                                        }
                                        ClientServiceInstanceV1[] serviceIntances = DnpWrapper.getServiceInstanceV1(BakId, "VAS_BILLINGACCOUNT_ID", ConfigurationManager.AppSettings["DanteIdentifier"]);
                                        if (serviceIntances != null)
                                        {
                                            if (serviceIntances[0].clientServiceInstanceStatus.value.Equals("active", StringComparison.OrdinalIgnoreCase))
                                            {
                                                var srvcChars = (from si in serviceIntances
                                                                 where (si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase) && si.clientServiceInstanceCharacteristic != null)
                                                                 select si).FirstOrDefault().clientServiceInstanceCharacteristic;

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
                                                }
                                                if (!String.IsNullOrEmpty(accountId.Trim()))
                                                {
                                                    if (ConfigurationManager.AppSettings["DnPManagedEmailSupplier"].Split(',').Contains(email_Supplier, StringComparer.OrdinalIgnoreCase))
                                                    {
                                                        accountId = string.Empty;
                                                    }
                                                    else
                                                    {
                                                        throw new DnpException("Ignored the request as EmailDataStore is Other than DNP");
                                                    }
                                                }

                                            }
                                            else
                                            {
                                                throw new DnpException("Profile doesn't have active BTIEMAIL service in DNP");
                                            }

                                            if (String.IsNullOrEmpty(accountId.Trim()))
                                            {
                                                requestOrderRequest.Order.OrderItem[0].Action.Reason = "VASRegrade";
                                                order = MSEOSaaSMapper.MapRequest(requestOrderRequest);
                                                // passing email_supplier value from service instance characterstics value
                                                order.ProductOrderItems[0].RoleInstances[0].Attributes.ToList().Where(attr => attr.Name.Equals("emailsupplier", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value = "MX";
                                                if (order.ProductOrderItems[0].Header.Status != BT.SaaS.Core.Shared.Entities.DataStatusEnum.done)
                                                {
                                                    MSEOSaaSMapper.SubmitOrder(order, requestOrderRequest, ref Mye2etxn);
                                                }
                                                else
                                                {
                                                    InboundQueueDAL.UpdateQueueRawXML(Settings1.Default.IgnoredStatus, "Request for Nayan Regrade was ignored", requestOrderRequest.Order.OrderIdentifier.Value);
                                                    foreach (OrderItem orderItem in requestOrderRequest.Order.OrderItem)
                                                    {
                                                        orderItem.Status = "Ignored";
                                                    }
                                                    BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter : The Order " + requestOrderRequest.Order.OrderIdentifier.Value + " was ignored by MSEO as the EmailDatastore is ");
                                                    notification.sendNotification(false, false, "001", "Request for Nayan Regrade was ignored", ref Mye2etxn, true);
                                                }
                                            }
                                            else
                                            {
                                                throw new DnpException("Ignored the Nayan Regrade Request");
                                            }
                                        }
                                        else
                                        {
                                            throw new DnpException("Profile doesn't have active services in DNP");
                                        }
                                    }
                                }
                                else
                                {
                                    order = MSEOSaaSMapper.MapRequest(requestOrderRequest);
                                    MSEOSaaSMapper.SubmitOrder(order, requestOrderRequest, ref Mye2etxn);
                                }
                            }
                        }
                        else if (isThomasProvision)
                        {
                            notification.sendNotification(false, false, "002", errorString, ref Mye2etxn, true);
                            Logger.Write(orderKey + "," + Ignored + "," + IgnoredNotificationSent + "," + errorString, Logger.TypeEnum.MessageTrace);
                        }
                        else if (isSpringProvision)
                        {
                            notification.sendNotification(false, false, "002", errorString, ref Mye2etxn, true);
                            Logger.Write(orderKey + "," + Ignored + "," + IgnoredNotificationSent + "," + errorString, Logger.TypeEnum.SpringMessageTrace);
                        }
                        else if (isHCSProvision)
                        {
                            notification.sendNotification(false, false, "002", errorString, ref Mye2etxn, true);
                            Logger.Write(orderKey + "," + Ignored + "," + IgnoredNotificationSent + "," + errorString, Logger.TypeEnum.HCSWarrantyMessageTrace);
                        }
                        else if (isVoInfinity)
                        {
                            notification.sendNotification(false, false, "MSEO_malformedorder_ 002", errorString, ref Mye2etxn, true);
                            Logger.Write(orderKey + "," + Ignored + "," + IgnoredNotificationSent + "," + errorString, Logger.TypeEnum.HCSWarrantyMessageTrace);
                        }
                        else
                        {
                            InboundQueueDAL.UpdateQueueRawXML(Settings1.Default.InvalideRequestCode, errorString, requestOrderRequest.Order.OrderIdentifier.Value);
                            notification.sendNotification(false, false, "002", errorString, ref Mye2etxn, true);
                        }
                    }
                    else
                    {
                        InboundQueueDAL.QueueRawXML("Bad Request from MQService", 2, requestOrderRequest.SerializeObject(), INITIAL_XML_STRING, BAD_REQUEST_XML_STRING);
                    }
                }
                //if any exception thrown by DnP i,e when search by Invalid BTONEID or VAS_BILLINGACCOUNT_ID , Ignored notification will send
                catch (DnpException DnPexception)
                {
                    InboundQueueDAL.UpdateQueueRawXML(Settings1.Default.IgnoredStatus, DnPexception.Message.ToString(), requestOrderRequest.Order.OrderIdentifier.Value);

                    foreach (OrderItem orderItem in requestOrderRequest.Order.OrderItem)
                    {
                        orderItem.Status = "Ignored";
                    }

                    notification.sendNotification(false, isAck, "001", DnPexception.Message.ToString(), ref Mye2etxn);
                    BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOMQAdapter DNP exception : " + DnPexception.ToString() + " for order identifier :" + requestOrderRequest.Order.OrderIdentifier.Value);
                }
                //if any exception thrown by MDM i,e Invalid Product codes
                catch (MdmException Mdmexception)
                {
                    InboundQueueDAL.UpdateQueueRawXML(Settings1.Default.IgnoredStatus, Mdmexception.Message, requestOrderRequest.Order.OrderIdentifier.Value);

                    foreach (OrderItem orderItem in requestOrderRequest.Order.OrderItem)
                    {
                        orderItem.Status = "Ignored";
                    }
                    notification.sendNotification(false, isAck, "001", Mdmexception.Message, ref Mye2etxn);
                    BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOMQAdapter MDM exception : " + Mdmexception.Message + " for order identifier :" + requestOrderRequest.Order.OrderIdentifier.Value);
                }
                catch (CMPSException cmpsException)
                {
                    InboundQueueDAL.UpdateQueueRawXML(Settings1.Default.RejectedStatus, cmpsException.Message, requestOrderRequest.Order.OrderIdentifier.Value);
                    foreach (OrderItem orderItem in requestOrderRequest.Order.OrderItem)
                    {
                        orderItem.Status = "Rejected";
                    }
                    notification.sendNotification(false, isAck, "002", cmpsException.Message, ref Mye2etxn);
                    BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOMQAdapter CMPS exception : " + cmpsException.ToString() + " for order identifier :" + requestOrderRequest.Order.OrderIdentifier.Value);
                }
                catch (Exception serviceException)
                {
                    InboundQueueDAL.UpdateQueueRawXML("Fault@MSEO", serviceException.ToString(), requestOrderRequest.Order.OrderIdentifier.Value);
                    BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOMQAdapter exception : " + serviceException.ToString());
                }

                //isInTransProcess = true;

            }
            catch (Exception serviceException2)
            {
                #region Commented Old Code - For ref only
                //BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOMQAdapter exception : " + serviceException2.ToString());
                //if (serviceException2.ToString().ToLower().Trim().Contains("compcode: 2, reason: 2009"))
                //{
                //    BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOMQAdapter exception Enterd into the If block: ");
                //    //Set shutdownSignal to true because error in while getting the connection
                //    Logger.Write("reason : 2009 " + Process.GetCurrentProcess().ProcessName + " , Exception caught : " + serviceException2.Message + " at" + serviceException2.StackTrace + " , exception.tostring()" + serviceException2.ToString(), Logger.TypeEnum.ExceptionTrace);
                //    isConnectivityFailed = true;
                //    shutdownSignal = true;
                //    //BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOMQAdapter exception After isConnectivityFailed is setting: ");
                //}
                //// Added condition while max no.of channel from MSEO 
                //else if (serviceException2.ToString().ToLower().Trim().Contains("compcode: 2, reason: 2059"))
                //{
                //    isConnectivityFailed = true;
                //    shutdownSignal = true;
                //    Logger.Write(" reason: 2059 " + Process.GetCurrentProcess().ProcessName + " , Exception caught : " + serviceException2.Message + " at" + serviceException2.StackTrace + " , exception.tostring() : " + serviceException2.ToString(), Logger.TypeEnum.ExceptionTrace);

                //}
                #endregion

                Logger.Write("General Exception :: " + Process.GetCurrentProcess().ProcessName + " , Exception caught : " + serviceException2.Message + " at" + serviceException2.StackTrace + " , exception.tostring() : " + serviceException2.ToString(), Logger.TypeEnum.ExceptionTrace);
                //Logger.Write("General Exception :: " + Process.GetCurrentProcess().ProcessName + " , Exception caught : " + serviceException2.Message + " at" + serviceException2.StackTrace + " , exception.tostring() : " + serviceException2.ToString(), Logger.TypeEnum.SpringExceptionTrace);
                //shutdownSignal = true;
            }
            finally
            {
                Mye2etxn.endInboundCall();
            }
            return notification.NotificationResponse;
        }

        public bool NewJourneyProvisonOrders(OrderRequest request, ref string productCode)
        {
            bool value = false;

            if (request != null && request.Order != null && request.Order.OrderItem != null
                && request.Order.OrderItem[0] != null && request.Order.OrderItem[0].Action != null && !String.IsNullOrEmpty(request.Order.OrderItem[0].Action.Code)
                 && request.Order.OrderItem[0].Specification != null && request.Order.OrderItem[0].Specification[0] != null
                && request.Order.OrderItem[0].Specification[0].Identifier != null && !string.IsNullOrEmpty(request.Order.OrderItem[0].Specification[0].Identifier.Value1))
            {
                productCode = request.Order.OrderItem[0].Specification[0].Identifier.Value1;
                if (ConfigurationManager.AppSettings["NewJourneySCodes"].Split(',').Contains(productCode))
                {
                    if (productCode.Equals(ConfigurationManager.AppSettings["BTSportsProductCode"].ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        if (request.Order.OrderItem[0].Action.Code.ToLower() == "create" || request.Order.OrderItem[0].Action.Code.ToLower() == "modify")
                        {
                            if (ConfigurationManager.AppSettings["isThomasNewJourneySwitch"].ToLower() == "true")
                            {
                                value = true;
                            }
                        }
                    }
                    else if (productCode.Equals(ConfigurationManager.AppSettings["BTSpringProductCode"].ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        if (request.Order.OrderItem[0].Action.Code.ToLower() != "cancel")
                        {
                            if (ConfigurationManager.AppSettings["isSpringNewJourneySwitch"].ToLower() == "true")
                            {
                                value = true;
                            }
                        }
                    }
                }
            }
            return value;
        }

        [WebMethodAttribute()]
        [SoapDocumentMethodAttribute("http://capabilities.nat.bt.com/wsdl/ManageOrder#ov", Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Bare, Binding = "ManageOrderBinding")]
        public MSEO.OrderNotification OV(OrderRequest requestOrderRequest)
        {
            MSEOOrderNotification notification = null;
            string TargetBillAccountNumber = string.Empty, sourceBillAccountId = string.Empty, orderFrom = string.Empty, OrderItemInfo = string.Empty;
            bool isAck = true;
            try
            {
                const string INITIAL_XML_STRING = "INITIAL XML";
                const string BAD_REQUEST_XML_STRING = "NO ORDER NUMBER";
                string billAccountDomain = "VAS_BILLINGACCOUNT_ID";
                string securityBacDomain = "SECURITY_BAC";
                if (requestOrderRequest != null)
                {
                    string errorString = string.Empty;
                    string orderKey = requestOrderRequest.Order.OrderIdentifier.Value;
                    SAASPE.Order order;
                    InboundQueueDAL.QueueRawXML(orderKey, 2, requestOrderRequest.SerializeObject(), INITIAL_XML_STRING, INITIAL_XML_STRING);

                    if (RequestValidator.ValidateMSEORequest(requestOrderRequest, ref errorString))
                    {
                        if (requestOrderRequest.StandardHeader != null && requestOrderRequest.StandardHeader.ServiceAddressing != null && requestOrderRequest.StandardHeader.ServiceAddressing.From != null && !String.IsNullOrEmpty(requestOrderRequest.StandardHeader.ServiceAddressing.From))
                            orderFrom = requestOrderRequest.StandardHeader.ServiceAddressing.From.ToString();

                        foreach (OrderItem Oi in requestOrderRequest.Order.OrderItem)
                            OrderItemInfo += "Order Item <Action:" + Oi.Action.Code + ", Product Code:" + Oi.Specification[0].Identifier.Value1 + ">;";
                        if (requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("targetbillaccountnumber", StringComparison.OrdinalIgnoreCase)))
                            TargetBillAccountNumber = requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("targetbillaccountnumber", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                        if (requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("SourceBillAccountNumber", StringComparison.OrdinalIgnoreCase)))
                            sourceBillAccountId = requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("SourceBillAccountNumber", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;

                        Mye2etxn.logMessage(MseoReceivedRequest, "Order Key<" + requestOrderRequest.Order.OrderIdentifier.Value + ">; Source BAC<" + sourceBillAccountId + ">; Target BAC<" + TargetBillAccountNumber + ">; Order From<" + orderFrom + ">; " + OrderItemInfo);

                        bool isSpringOIExist = false;
                        bool isHomeAwayOItemExist = false;
                        bool isThomasOItemExist = false;
                        bool isMultiSim = false;
                        bool isTargetBACSpringExist = false;
                        bool isTargetBACWifiExist = false;
                        string rvsid = string.Empty;
                        string RoleKeyList = string.Empty;
                        string errorMessaeg = string.Empty;
                        string ota_device_id = string.Empty;
                        List<string> Role_Identity_List = null;
                        List<string> Target_Role_Key_List = null;
                        List<string> scodeList = new List<string>();
                        List<string> nonVASServicesList = new List<string>();
                        GetBatchProfileV1Response batchProfileResponse = null;
                        ClientServiceInstanceV1[] targetClientServicesVas = null;
                        ClientServiceInstanceV1[] targetClientServicesSecurity = null;
                        ClientServiceInstanceV1[] targetClientServices = null;
                        Dictionary<string, string> springParameters = new Dictionary<string, string>();
                        Dictionary<string, string> serviceIdentifierWithVasProdCode = new Dictionary<string, string>();
                        Dictionary<string, string> productNameStatus = new Dictionary<string, string>();

                        foreach (OrderItem ordItem in requestOrderRequest.Order.OrderItem)
                        {
                            if (ordItem.Specification[0].Identifier.Value1.Equals(ConfigurationManager.AppSettings["ThomasScode"], StringComparison.OrdinalIgnoreCase))
                            {
                                isThomasOItemExist = true;
                            }
                            else if (ordItem.Specification[0].Identifier.Value1.Equals(ConfigurationManager.AppSettings["HomeawayScode"], StringComparison.OrdinalIgnoreCase))
                            {
                                isHomeAwayOItemExist = true;
                            }
                            else if (ordItem.Specification[0].Identifier.Value1.Equals(ConfigurationManager.AppSettings["SpringScode"], StringComparison.OrdinalIgnoreCase))
                            {
                                isSpringOIExist = true;
                                if (ordItem.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToLower().Equals("rvsid") && !string.IsNullOrEmpty(ic.Value)))
                                    rvsid = ordItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("rvsid")).FirstOrDefault().Value;
                            }
                            else
                            {
                                scodeList.Add(ordItem.Specification[0].Identifier.Value1);
                            }
                        }
                        targetClientServicesVas = DnpWrapper.getServiceInstanceV1(TargetBillAccountNumber, billAccountDomain, "");
                        targetClientServicesSecurity = DnpWrapper.getServiceInstanceV1(TargetBillAccountNumber, securityBacDomain, "");
                        if (targetClientServicesVas != null)
                        {
                            targetClientServices = targetClientServicesVas;
                        }
                        if (targetClientServicesSecurity != null)
                        {
                            if (targetClientServices == null)
                                targetClientServices = targetClientServicesSecurity;
                            else
                                targetClientServices = targetClientServices.Concat(targetClientServicesSecurity).ToArray();
                        }
                        if (isThomasOItemExist)
                        {
                            ClientServiceInstanceV1[] clientServices = DnpWrapper.getServiceInstanceV1(sourceBillAccountId, billAccountDomain, ConfigurationManager.AppSettings["ThomasIdentifier"]);
                            if (clientServices != null && clientServices.Length > 0)
                            {
                                if (targetClientServices == null || !(targetClientServices.Length > 0 && targetClientServices.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["ThomasIdentifier"], StringComparison.OrdinalIgnoreCase))))
                                {
                                    nonVASServicesList.Add(ConfigurationManager.AppSettings["ThomasScode"].ToLower());
                                    if (clientServices[0].clientServiceRole != null)
                                        productNameStatus.Add(ConfigurationManager.AppSettings["ThomasScode"].ToLower(), clientServices[0].clientServiceRole.ToList().FirstOrDefault().clientServiceRoleStatus.value);
                                }
                            }
                        }
                        if (isHomeAwayOItemExist)
                        {
                            GetServiceUserProfilesV1Response response = DnpWrapper.GetServiceUserProfilesV1ForHomeAway(sourceBillAccountId);
                            if (response != null && response.getServiceUserProfilesV1Res != null && response.getServiceUserProfilesV1Res.clientProfileV1 != null)
                            {
                                nonVASServicesList.Add(ConfigurationManager.AppSettings["HomeawayScode"].ToLower());
                                productNameStatus.Add(ConfigurationManager.AppSettings["HomeawayScode"].ToLower(), "ACTIVE");//here ACTIVE value is never used it is give just to use the existing productNameStatus variable;
                            }
                        }

                        List<ProductVasClass> prodVASClassList = new List<ProductVasClass>();
                        List<ProductVasClass> vasDefinitionList = null;

                        if ((scodeList != null && scodeList.Count > 0) || isSpringOIExist)
                        {
                            vasDefinitionList = MdmWrapper.GetVasClassDetailsFromPromotions(scodeList).ToList();
                            if (vasDefinitionList != null && vasDefinitionList.Count > 0)
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
                                            prodVASClassList.Add(maxPreferenceIndicatorProductVASClass);
                                        }
                                    }
                                }
                            }
                            if (prodVASClassList != null && prodVASClassList.Count > 0)
                            {
                                if (DnpWrapper.getBatchProfileV1(sourceBillAccountId, billAccountDomain, sourceBillAccountId, securityBacDomain, TargetBillAccountNumber, billAccountDomain, ref batchProfileResponse))
                                {
                                    if (batchProfileResponse != null && batchProfileResponse.getBatchProfileV1Res != null && batchProfileResponse.getBatchProfileV1Res.clientProfileV1 != null)
                                    {
                                        for (int i = 0; i < 2; i++)
                                        {
                                            if (batchProfileResponse.getBatchProfileV1Res.clientProfileV1[i] != null && batchProfileResponse.getBatchProfileV1Res.clientProfileV1[i].clientServiceInstanceV1 != null && batchProfileResponse.getBatchProfileV1Res.clientProfileV1[i].clientServiceInstanceV1.Count() > 0 && batchProfileResponse.getBatchProfileV1Res.clientProfileV1[i].clientServiceInstanceV1.ToList().Where(csi => csi.clientServiceInstanceStatus.value.ToLower() != "pending").Count() > 0)
                                            {
                                                foreach (ClientServiceInstanceV1 serviceInstace in batchProfileResponse.getBatchProfileV1Res.clientProfileV1[i].clientServiceInstanceV1.ToList().Where(csi => csi.clientServiceInstanceStatus.value.ToLower() != "pending").ToArray())
                                                {
                                                    if (serviceInstace.serviceIdentity != null && serviceInstace.serviceIdentity.ToList().Exists(sID => sID.value.Equals(sourceBillAccountId, StringComparison.OrdinalIgnoreCase)))
                                                    {
                                                        if (serviceInstace.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["SpringIdentifier"]))
                                                        {
                                                            if (isSpringOIExist)
                                                            {
                                                                nonVASServicesList.Add(ConfigurationManager.AppSettings["SpringScode"].ToLower());
                                                                productNameStatus.Add(ConfigurationManager.AppSettings["SpringScode"].ToLower(), serviceInstace.clientServiceRole.ToList().FirstOrDefault().clientServiceRoleStatus.value);

                                                                if (serviceInstace.clientServiceRole != null)
                                                                {
                                                                    if (!isMultiSim && serviceInstace.clientServiceRole.ToList().Exists(sr => sr.id.Equals("DEFAULT")) && serviceInstace.clientServiceRole.ToList().Where(sr => sr.id.Equals("DEFAULT")).ToArray().Count() > 1)
                                                                    {
                                                                        isMultiSim = true;
                                                                    }
                                                                    serviceInstace.clientServiceRole.ToList().ForEach(x =>
                                                                    {
                                                                        if (x.id.ToLower().Equals("default") && x.clientIdentity != null && x.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.ToLower().Equals("rvsid") && ci.value.Equals(rvsid, StringComparison.OrdinalIgnoreCase)))
                                                                        {
                                                                            if (x.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("OTA_DEVICE_ID", StringComparison.OrdinalIgnoreCase)))
                                                                            {
                                                                                ota_device_id = x.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("OTA_DEVICE_ID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;

                                                                            }
                                                                            springParameters.Add("Role_Key_GSM", x.name);
                                                                        }
                                                                    });
                                                                }

                                                                if (targetClientServices != null && targetClientServices.ToList().Exists(tsrvc => tsrvc.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["SpringIdentifier"])))
                                                                    isTargetBACSpringExist = true;

                                                                springParameters.Add("OTA_DEVICE_ID", ota_device_id);
                                                                springParameters.Add("isMultiSim", isMultiSim.ToString());
                                                                springParameters.Add("isTargetBACSpringExist", isTargetBACSpringExist.ToString());
                                                            }
                                                        }
                                                        // As network security does not have VAS_PROD_ID in it's service instance charecterstics.
                                                        else if (serviceInstace.clientServiceInstanceCharacteristic != null && serviceInstace.clientServiceInstanceCharacteristic.Count() > 0 && serviceInstace.clientServiceInstanceCharacteristic.ToList().Exists(cis => cis.name.Equals("VAS_PRODUCT_ID", StringComparison.OrdinalIgnoreCase)) ||
                                                             serviceInstace.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["NetworkSecurityService"], StringComparison.OrdinalIgnoreCase))
                                                        {
                                                            if ((serviceInstace.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["WifiIdentifier"], StringComparison.OrdinalIgnoreCase)) || (serviceInstace.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["CFServiceCode"], StringComparison.OrdinalIgnoreCase)))
                                                            {
                                                                if (isSpringOIExist)
                                                                {
                                                                    if (batchProfileResponse.getBatchProfileV1Res.clientProfileV1[i].clientServiceInstanceV1.ToList().Exists(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["SpringIdentifier"]) && csi.serviceIdentity.ToList().Exists(sID => sID.value.Equals(sourceBillAccountId, StringComparison.OrdinalIgnoreCase))))
                                                                    {
                                                                        if (serviceInstace.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["WifiIdentifier"], StringComparison.OrdinalIgnoreCase))
                                                                        {
                                                                            if (targetClientServices != null && targetClientServices.ToList().Exists(tsrvc => tsrvc.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["WifiIdentifier"])))
                                                                            {
                                                                                isTargetBACWifiExist = true;
                                                                                Target_Role_Key_List = DanteRequestProcessor.GetListOfDefaultRoleIdentities(targetClientServices.ToList().Where(tsrvc => tsrvc.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["WifiIdentifier"])).FirstOrDefault());

                                                                                if (Target_Role_Key_List.Count > 0 && Target_Role_Key_List[0] != null)
                                                                                {
                                                                                    springParameters.Add("TargetRoleKeyList", Target_Role_Key_List[0].ToString());
                                                                                }
                                                                            }

                                                                            springParameters.Add("isTargetBACWifiExist", isTargetBACWifiExist.ToString());

                                                                            serviceIdentifierWithVasProdCode.Add(serviceInstace.clientServiceInstanceIdentifier.value.ToLower(), serviceInstace.clientServiceInstanceCharacteristic.ToList().Where(ic => ic.name.Equals("VAS_PRODUCT_ID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value.ToLower());

                                                                            if (serviceInstace.clientServiceRole != null)
                                                                                productNameStatus.Add(serviceInstace.clientServiceInstanceIdentifier.value.Substring(0, serviceInstace.clientServiceInstanceIdentifier.value.LastIndexOf(":")).ToLower(), serviceInstace.clientServiceRole.FirstOrDefault().clientServiceRoleStatus.value);

                                                                            Role_Identity_List = DanteRequestProcessor.GetListOfDefaultRoleIdentities(serviceInstace);

                                                                            if (Role_Identity_List.Count > 0 && Role_Identity_List[0] != null)
                                                                            {
                                                                                RoleKeyList = Role_Identity_List[0].ToString();
                                                                            }
                                                                        }
                                                                        else//Spring content filtering service.
                                                                        {
                                                                            if (targetClientServices == null || !targetClientServices.ToList().Exists(tsrvc => tsrvc.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["CFServiceCode"], StringComparison.OrdinalIgnoreCase)))
                                                                            {
                                                                                string SourceBACHasBroadBand = string.Empty;
                                                                                if (requestOrderRequest.Order.OrderItem.ToList().Exists(oi => oi.Instance[0].InstanceCharacteristic.ToList().Exists(ichar => ichar.Name.Equals("SourceBACHasBroadBand", StringComparison.OrdinalIgnoreCase))))
                                                                                    SourceBACHasBroadBand = requestOrderRequest.Order.OrderItem.ToList().Where(oi => oi.Instance[0].InstanceCharacteristic.ToList().Exists(ichar => ichar.Name.Equals("SourceBACHasBroadBand", StringComparison.OrdinalIgnoreCase))).FirstOrDefault().Instance[0].InstanceCharacteristic.ToList().Where(iChar => iChar.Name.Equals("SourceBACHasBroadBand", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                                                                                //To check spring has single sim or multiple sims
                                                                                if (!springParameters.ContainsKey("isMultiSim"))
                                                                                {
                                                                                    ClientServiceInstanceV1 springService = batchProfileResponse.getBatchProfileV1Res.clientProfileV1[i].clientServiceInstanceV1.ToList().Where(csi => csi.clientServiceInstanceStatus.value.ToLower() != "pending" && csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["SpringIdentifier"])).FirstOrDefault();
                                                                                    if (serviceInstace.clientServiceRole != null && serviceInstace.clientServiceRole.ToList().Exists(sr => sr.id.Equals("DEFAULT")) && serviceInstace.clientServiceRole.ToList().Where(sr => sr.id.Equals("DEFAULT")).ToArray().Count() > 1)
                                                                                    {
                                                                                        isMultiSim = true;
                                                                                    }
                                                                                }
                                                                                if (!isMultiSim && SourceBACHasBroadBand.Equals("False", StringComparison.OrdinalIgnoreCase))
                                                                                {
                                                                                    serviceIdentifierWithVasProdCode.Add(serviceInstace.clientServiceInstanceIdentifier.value.ToLower(), serviceInstace.clientServiceInstanceCharacteristic.ToList().Where(ic => ic.name.Equals("VAS_PRODUCT_ID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value.ToLower());
                                                                                    if (serviceInstace.clientServiceRole != null)
                                                                                        productNameStatus.Add(serviceInstace.clientServiceInstanceIdentifier.value.Substring(0, serviceInstace.clientServiceInstanceIdentifier.value.LastIndexOf(":")).ToLower(), serviceInstace.clientServiceRole.FirstOrDefault().clientServiceRoleStatus.value);
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                                else if ((targetClientServices == null || !targetClientServices.ToList().Exists(csi => csi.clientServiceInstanceIdentifier.value.Equals(serviceInstace.clientServiceInstanceIdentifier.value, StringComparison.OrdinalIgnoreCase))))
                                                                {
                                                                    serviceIdentifierWithVasProdCode.Add(serviceInstace.clientServiceInstanceIdentifier.value.ToLower(), serviceInstace.clientServiceInstanceCharacteristic.ToList().Where(ic => ic.name.Equals("VAS_PRODUCT_ID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value.ToLower());

                                                                    if (serviceInstace.clientServiceRole != null)
                                                                        productNameStatus.Add(serviceInstace.clientServiceInstanceIdentifier.value.Substring(0, serviceInstace.clientServiceInstanceIdentifier.value.LastIndexOf(":")).ToLower(), serviceInstace.clientServiceRole.FirstOrDefault().clientServiceRoleStatus.value);

                                                                    //preparing rolekeylist for only wifi service,this check will prevent overwriting of rolekeylist with CF service role key's.
                                                                    if ((serviceInstace.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["WifiIdentifier"], StringComparison.OrdinalIgnoreCase)))
                                                                    {
                                                                        Role_Identity_List = DanteRequestProcessor.GetListOfDefaultRoleIdentities(serviceInstace);

                                                                        if (Role_Identity_List.Count > 0 && Role_Identity_List[0] != null)
                                                                        {
                                                                            RoleKeyList = Role_Identity_List[0].ToString();
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                            else if ((targetClientServices == null || !targetClientServices.ToList().Exists(csi => csi.clientServiceInstanceIdentifier.value.Equals(serviceInstace.clientServiceInstanceIdentifier.value, StringComparison.OrdinalIgnoreCase))))
                                                            {
                                                                //Adding Dante Service Info as VASProduct Family and Subtype Format
                                                                if (serviceInstace.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase))
                                                                {
                                                                    serviceIdentifierWithVasProdCode.Add(ConfigurationManager.AppSettings["DanteIdentifierInMDM"].ToLower(), " ");
                                                                    if (serviceInstace.clientServiceRole != null)
                                                                        productNameStatus.Add(ConfigurationManager.AppSettings["DanteEMailProdCode"].ToLower(), serviceInstace.clientServiceRole.FirstOrDefault().clientServiceRoleStatus.value);
                                                                }
                                                                else
                                                                {
                                                                    if (serviceInstace.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["NetworkSecurityService"], StringComparison.OrdinalIgnoreCase))
                                                                    {
                                                                        serviceIdentifierWithVasProdCode.Add(ConfigurationManager.AppSettings["NetworkSecurityService"].ToLower(), " ");
                                                                    }
                                                                    else
                                                                    {
                                                                        serviceIdentifierWithVasProdCode.Add(serviceInstace.clientServiceInstanceIdentifier.value.ToLower(), serviceInstace.clientServiceInstanceCharacteristic.ToList().Where(ic => ic.name.Equals("VAS_PRODUCT_ID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value.ToLower());
                                                                    }
                                                                    if (serviceInstace.clientServiceRole != null)
                                                                        productNameStatus.Add(serviceInstace.clientServiceInstanceIdentifier.value.Substring(0, serviceInstace.clientServiceInstanceIdentifier.value.LastIndexOf(":")).ToLower(), serviceInstace.clientServiceRole.FirstOrDefault().clientServiceRoleStatus.value);
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    if (serviceIdentifierWithVasProdCode != null && serviceIdentifierWithVasProdCode.Count > 0)
                                    {
                                        foreach (ProductVasClass prodVasClass in prodVASClassList)
                                        {
                                            string serviceCode = prodVasClass.VasProductFamily.ToLower() + ":" + prodVasClass.vasSubType.ToLower();
                                            if (serviceIdentifierWithVasProdCode.ContainsKey(serviceCode))
                                            {
                                                if (serviceCode.Equals(ConfigurationManager.AppSettings["CAService"], StringComparison.OrdinalIgnoreCase))
                                                {
                                                    if (serviceIdentifierWithVasProdCode[serviceCode] == prodVasClass.VasProductId.ToLower())
                                                        prodVasClass.Notes = "active";
                                                    else
                                                    {
                                                        prodVasClass.Notes = "warning";
                                                        requestOrderRequest.Order.Error = new Error();
                                                        requestOrderRequest.Order.Error.Code = "005";
                                                        requestOrderRequest.Order.Error.Description = "Warning: Mismatch found for ContentAnywhere";
                                                    }
                                                }
                                                else
                                                {
                                                    prodVasClass.Notes = "active";
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        if ((prodVASClassList != null && prodVASClassList.Where(pv => !String.IsNullOrEmpty(pv.Notes)).Count() != 0) || (nonVASServicesList != null && nonVASServicesList.Count > 0))
                        {
                            order = MSEOSaaSMapper.PreparePEOrderForAssetTransfer(prodVASClassList, requestOrderRequest, nonVASServicesList, springParameters, RoleKeyList, productNameStatus, ref batchProfileResponse);
                            MSEOSaaSMapper.SubmitOrder(order, requestOrderRequest, ref Mye2etxn);
                        }
                        else
                        {
                            if (scodeList.Count == 0)
                            {
                                errorMessaeg = "invalid Scodes";
                            }
                            else if (prodVASClassList.Count == 0)
                            {
                                errorMessaeg = "No Products are mapped with the Promotions";
                            }
                            else
                            {
                                errorMessaeg = "No services exists in DnP for asset transfer from source BAC profile for given request";
                            }
                            InboundQueueDAL.UpdateQueueRawXML(Settings1.Default.IgnoredStatus, errorMessaeg, requestOrderRequest.Order.OrderIdentifier.Value);

                            foreach (OrderItem orderItem in requestOrderRequest.Order.OrderItem)
                            {
                                orderItem.Status = "Ignored";
                            }
                            notification.sendNotification(false, false, "001", errorMessaeg, ref Mye2etxn, true);
                            Trace.WriteLine(errorMessaeg);
                        }
                    }
                    else
                    {
                        InboundQueueDAL.UpdateQueueRawXML(Settings1.Default.InvalideRequestCode, errorString, requestOrderRequest.Order.OrderIdentifier.Value);
                        notification.sendNotification(false, false, "002", errorString, ref Mye2etxn, true);
                    }
                }
                else
                {
                    InboundQueueDAL.QueueRawXML("Bad Request from MQService", 2, requestOrderRequest.SerializeObject(), INITIAL_XML_STRING, BAD_REQUEST_XML_STRING);
                }
            }
            catch (DnpException DnPexception)
            {
                InboundQueueDAL.UpdateQueueRawXML(Settings1.Default.IgnoredStatus, DnPexception.Message.ToString(), requestOrderRequest.Order.OrderIdentifier.Value);

                foreach (OrderItem orderItem in requestOrderRequest.Order.OrderItem)
                {
                    orderItem.Status = "Ignored";
                }

                notification.sendNotification(false, false, "001", "Profile not found in DNP", ref Mye2etxn, true);
                Trace.WriteLine("MSEOOneViewMQAdapter DNP exception : " + DnPexception.ToString() + " for order identifier :" + requestOrderRequest.Order.OrderIdentifier.Value);
            }
            //if any exception thrown by MDM i,e Invalid Product codes
            catch (MdmException Mdmexception)
            {
                InboundQueueDAL.UpdateQueueRawXML(Settings1.Default.IgnoredStatus, Mdmexception.Message, requestOrderRequest.Order.OrderIdentifier.Value);

                foreach (OrderItem orderItem in requestOrderRequest.Order.OrderItem)
                {
                    orderItem.Status = "Ignored";
                }
                notification.sendNotification(false, false, "001", " The Product's in the request are Ignored because there is no defination for this product in SaaS", ref Mye2etxn, true);
                Trace.WriteLine("MSEOOneViewMQAdapter MDM exception : " + Mdmexception.Message + " for order identifier :" + requestOrderRequest.Order.OrderIdentifier.Value);
            }
            catch (Exception serviceException)
            {
                InboundQueueDAL.UpdateQueueRawXML("Fault@MSEO", serviceException.ToString(), requestOrderRequest.Order.OrderIdentifier.Value);
                Trace.WriteLine("MSEOOneViewMQAdapter exception : " + serviceException.ToString());
            }
            finally
            {
                Mye2etxn.endInboundCall();
            }
            return notification.NotificationResponse;
        }

        public static void orderCompleteMethod()
        {
            try
            {
                string from = string.Empty;
                MessagesFile Mf = new MessagesFile(ConfigurationManager.AppSettings["logMessagesFilename"].ToString());
                E2ETransaction Mye2etxn;
                Logger.Write("Executing stored procedure", Logger.TypeEnum.MQNotificationsExceptionTrace);
                List<OrderComplete> orderCompleteList = OrdersDAL.DeQueueOrderCompleteOrders(Convert.ToInt32(ConfigurationManager.AppSettings["RowCount"]), ConfigurationManager.AppSettings["bulkProcessSource"].ToString());
                Logger.Write("Stored procedure executed sucessfully and count: " + orderCompleteList.Count, Logger.TypeEnum.MQNotificationsExceptionTrace);

                foreach (OrderComplete orderNotification in orderCompleteList)
                {
                    try
                    {
                        string exceptionStartString = "<saasException>\r\n  <exception>BT.SaaS.Core.Shared.Exceptions.SaaSBaseApplicationException: PE:";
                        string exceptionEndString = "---&gt; &lt;saasException&gt;";
                        string errorMessage = string.Empty;
                        if (orderNotification != null && !String.IsNullOrEmpty(orderNotification.ExceptionString) && !orderNotification.ExceptionString.Equals("NULL", StringComparison.OrdinalIgnoreCase))
                        {
                            errorMessage = orderNotification.ExceptionString.Substring((orderNotification.ExceptionString.IndexOf(exceptionStartString) + exceptionStartString.Length), (orderNotification.ExceptionString.IndexOf(exceptionEndString) - (orderNotification.ExceptionString.IndexOf(exceptionStartString) + exceptionStartString.Length)));
                        }
                        BT.SaaS.Core.Shared.Entities.Order returnOrder = BT.SaaS.Core.Shared.Entities.Order.FromString(orderNotification.Order);
                        if (returnOrder.Header.CeaseReason != null)
                        {
                            BT.SaaS.MSEOAdapter.OrderRequest order = BT.SaaS.MSEOAdapter.OrderRequest.DeserializeObject(returnOrder.Header.CeaseReason);
                            MSEOSaaSMapper.MapResponse(returnOrder, ref order);
                            if (order.StandardHeader != null && order.StandardHeader.E2e != null && order.StandardHeader.E2e.E2EDATA != null && !String.IsNullOrEmpty(order.StandardHeader.E2e.E2EDATA.ToString()))
                                Mye2etxn = new E2ETransaction(order.StandardHeader.E2e.E2EDATA.ToString(), ConfigurationManager.AppSettings["ProjectID"].ToString(), ConfigurationManager.AppSettings["Component"].ToString(), Mf);
                            else
                                Mye2etxn = new E2ETransaction(ConfigurationManager.AppSettings["E2EData"].ToString(), ConfigurationManager.AppSettings["ProjectID"].ToString(), ConfigurationManager.AppSettings["Component"].ToString(), Mf);

                            MSEOOrderNotification notification = new MSEOOrderNotification(order);
                            if (!orderNotification.Status)
                            {
                                notification.sendNotification(orderNotification.Status, false, orderNotification.ErrorCode, errorMessage, ref Mye2etxn);
                            }
                            else
                            {
                                notification.sendNotification(orderNotification.Status, false, "004", errorMessage, ref Mye2etxn);
                            }
                        }
                    }
                    catch (Exception ex1)
                    {
                        Logger.Write(orderNotification.Order.ToString() + @", Order Complete Exception ex1- " + ex1.Message, Logger.TypeEnum.MQNotificationsExceptionTrace);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write("Order Complete Exception ex- " + ex.Message, Logger.TypeEnum.MQNotificationsExceptionTrace);
                Logger.Debug(ex.ToString());
            }
            finally
            {

            }
        }
    }
}
