using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using IBM.WMQ;
using System.Threading;
using System.Collections;
using BT.SaaS.Core.Shared.ErrorManagement;
using BT.SaaS.MSEOAdapter;
using BT.SaaS.IspssAdapter.PE;
using SAASPE = BT.SaaS.Core.Shared.Entities;
using BT.SaaS.IspssAdapter;
using System.ServiceModel;
using System.Configuration;
using BT.SaaS.IspssAdapter.Dnp;
using System.IO;
using com.bt.util.logging;

namespace BT.SaaS.MSEO.MQService
{
    public partial class MSEOMqService : ServiceBase
    {

        #region Data Members

        private static ManualResetEvent threadShutdownSignal;
        private static bool shutdownSignal = false;
        private System.Timers.Timer timer;
        private static bool isInTransProcess = false;
        private MessagesFile Mf;
        private E2ETransaction Mye2etxn;
        private const string MseoReceivedRequest = "MseoReceivedRequest";
        private const string MseoReceivedNotificationSend = "MseoReceivedNotificationSend";
        private const string ExceptionOccurred = "ExceptionOccurred";
        private const string OrderStatus = "OrderStatus";

        //private bool isConnectivityFailed = false;

        #endregion

        public MSEOMqService()
        {
            InitializeComponent();
            try
            {
                LoggingFramework.initialise(ConfigurationManager.AppSettings["logHandlingFilename"].ToString());
                Mf = new MessagesFile(ConfigurationManager.AppSettings["logMessagesFilename"].ToString());

                threadShutdownSignal = new ManualResetEvent(false);
                timer = new System.Timers.Timer();
                timer.Elapsed += new System.Timers.ElapsedEventHandler(this.timer_Elapsed);
                timer.Interval = Convert.ToDouble(Settings1.Default.TimerIntervalInSeconds) * 1000;
                timer.Enabled = true;

            }
            catch (Exception ex)
            {
                BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOMQAdapter exception: " + ex.ToString());
            }
        }


        protected override void OnStart(string[] args)
        {

        }

        protected override void OnStop()
        {
            try
            {
                // Set Shutdown flag
                shutdownSignal = true;
                if (isInTransProcess)
                {
                    // Wait for 30 secs before closing the service Immediately.
                    threadShutdownSignal.WaitOne(Convert.ToInt32(Settings1.Default.TimerIntervalInSeconds) * 1000);
                }
                Logger.Write("MQ service is stopped", Logger.TypeEnum.MQNotificationsExceptionTrace);
            }
            catch (Exception threadStopException)
            {
                BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOMQAdapter exception: " + threadStopException.ToString());
                Logger.Write("MQ service is stopped", Logger.TypeEnum.MQNotificationsExceptionTrace);
            }
        }

        #region timer_Elapsed event (This method is never called)
        /// <summary>
        /// This event is raised after a specified time interval (Defined in App.Config).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer_Elapsed(object sender, EventArgs e)
        {
            timer.Enabled = false;
            int PERetryCount = 0;
            string orderNumber = String.Empty;
            const string New = "New";
            const string ReachedtoMSEO = "Order reached to MSEO";
            const string Ignored = "Ignored";
            const string IgnoredNotificationSent = "Ignored notification sent for the Order ";
            try
            {
                shutdownSignal = false;
                //isConnectivityFailed = false;
                string result = "";
                if (ConfigurationManager.AppSettings["MQRequestPort"] != string.Empty)
                {
                    MQEnvironment.Port = Convert.ToInt32(ConfigurationManager.AppSettings["MQRequestPort"]);
                }
                Hashtable props = new Hashtable();
                props.Add(MQC.HOST_NAME_PROPERTY, ConfigurationManager.AppSettings["MQRequestHostname"]);
                props.Add(MQC.CHANNEL_PROPERTY, ConfigurationManager.AppSettings["MQRequestChannel"]);
                if (ConfigurationManager.AppSettings["MQRequestUserName"] != string.Empty)
                {
                    props.Add(MQC.USER_ID_PROPERTY, ConfigurationManager.AppSettings["MQRequestUserName"]);
                }
                if (ConfigurationManager.AppSettings["MQRequestPassword"] != string.Empty)
                {
                    props.Add(MQC.PASSWORD_PROPERTY, ConfigurationManager.AppSettings["MQRequestPassword"]);
                }

                MQQueueManager qMan = new MQQueueManager(ConfigurationManager.AppSettings["MQRequestQueueManager"], props);
                try
                {

                    MQMessage mQMsg = new MQMessage();
                    MQQueue q;

                    int OutOpenOptions = MQC.MQOO_INPUT_AS_Q_DEF | MQC.MQOO_FAIL_IF_QUIESCING;

                    UTF8Encoding utfEnc = new UTF8Encoding();


                    MQGetMessageOptions gmo = new MQGetMessageOptions();
                    gmo.Options = MQC.MQGMO_FAIL_IF_QUIESCING | MQC.MQGMO_WAIT;
                    gmo.WaitInterval = MQC.MQWI_UNLIMITED;
                    gmo.MatchOptions = MQC.MQMO_MATCH_CORREL_ID;
                    q = qMan.AccessQueue(ConfigurationManager.AppSettings["MQRequestQueueName"], OutOpenOptions); // configure

                    try
                    {
                        while (!shutdownSignal)  //flag
                        {
                            OrderRequest requestOrderRequest = null;
                            MSEOOrderNotification notification = null;
                            string BillAccountNumber = string.Empty, VasClass = string.Empty, BtoneId = string.Empty, orderFrom = string.Empty, OrderItemInfo = string.Empty;
                            bool isAck = true;
                            bool isSpringProvision = false;
                            bool isThomasProvision = false;
                            //Voinfinity
                            bool isVoInfinity = false;
                            bool isHCSProvision = false;
                            bool isBTPlusMarker = false;
                            bool isAssociateBTPlusMarker = false;
                            bool isCONSUMERBROADBAND = false;

                            List<string> Active_EmailList = new List<string>();
                            List<string> BarredAbuse_EmailList = new List<string>();

                            try
                            {
                                q.Get(mQMsg, gmo);
                                result = mQMsg.ReadString(mQMsg.MessageLength);
                                BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOMQAdapter : MQ Request received :" + result);
                                requestOrderRequest = OrderRequest.DeserializeObject(result);
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
                                            &&!requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("vasclass", StringComparison.OrdinalIgnoreCase)))
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

                                        if (!isThomasProvision && !isSpringProvision && !isVoInfinity && !isHCSProvision && !isBTPlusMarker &&!isAssociateBTPlusMarker&&!isCONSUMERBROADBAND)
                                            InboundQueueDAL.QueueRawXML(orderKey, 2, requestOrderRequest.SerializeObject(), INITIAL_XML_STRING, INITIAL_XML_STRING);
                                        else if (isThomasProvision)
                                        {
                                            Logger.Write(orderKey + "," + New + "," + ReachedtoMSEO, Logger.TypeEnum.MessageTrace);
                                            LogHelper.LogActivityDetails(bptmTxnId, guid, New + " " + ReachedtoMSEO, TimeSpan.Zero, "ThomasTrace", requestOrderRequest.SerializeObject());
                                            //MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.MQ_IN_LOG, orderKey, requestOrderRequest.SerializeObject(), productCode);
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
                                            LogHelper.LogActivityDetails(bptmTxnId, guid, New + " " + ReachedtoMSEO, TimeSpan.Zero, "BTPlusMarkerTrace", requestOrderRequest.SerializeObject());
                                            //Logger.Write(orderKey + "," + New + "," + ReachedtoMSEO, Logger.TypeEnum.BTPlusMarkerMessageTrace);                                            
                                            MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.MQ_IN_LOG, orderKey, requestOrderRequest.SerializeObject(), productCode);
                                        }
                                        else if (isAssociateBTPlusMarker)
                                        {
                                            //here using only kibana logs 
                                            LogHelper.LogActivityDetails(bptmTxnId, guid, New + " " + ReachedtoMSEO, TimeSpan.Zero, "BTPlusMarkerTrace", requestOrderRequest.SerializeObject());
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
                                                thomasProcessor.ThomasRequestMapper(requestOrderRequest, guid, ActivityStartTime, ref Mye2etxn);
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
                                                SubmitOrder(order, requestOrderRequest, ref Mye2etxn, PERetryCount);
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
                                                        SubmitOrder(order, requestOrderRequest, ref Mye2etxn, PERetryCount);
                                                    }
                                                    else
                                                    {
                                                        order = new BT.SaaS.Core.Shared.Entities.Order();
                                                        order.ProductOrderItems = new List<BT.SaaS.Core.Shared.Entities.ProductOrderItem>();
                                                        SubmitOrder(order, requestOrderRequest, ref Mye2etxn, PERetryCount);
                                                    }
                                                }
                                                else if (ConfigurationManager.AppSettings["EmailScodes"].Split(',').Contains(requestOrderRequest.Order.OrderItem[0].Specification[0].Identifier.Value1) && !requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(inst => inst.Name.ToLower().Equals("ispserviceclass") && inst.Value.ToUpper().Equals("ISP_SC_PM"))
                                                         && requestOrderRequest.Order.OrderItem[0].Action.Code.Equals("Create", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    string emailDataStore = "D&P";
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
                                                                    SubmitOrder(order, requestOrderRequest, ref Mye2etxn, PERetryCount);
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
                                                            else if (emailDataStore == "D&P")
                                                            {
                                                                InboundQueueDAL.UpdateQueueRawXML(Settings1.Default.InvalideRequestCode, "Ignored as EmailDataStore value  is D&P based ReservationID Format", requestOrderRequest.Order.OrderIdentifier.Value);
                                                                foreach (OrderItem orderItem in requestOrderRequest.Order.OrderItem)
                                                                {
                                                                    orderItem.Status = "Ignored";
                                                                }
                                                                BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter : The Order " + requestOrderRequest.Order.OrderIdentifier.Value + " was ignored by MSEO as the EmailDatastore is " + emailDataStore);
                                                                notification.sendNotification(false, false, "001", "Ignored as the EmaildataStore value is D&P based on Reservation ID format", ref Mye2etxn, true);
                                                            }
                                                        }

                                                        else
                                                        {
                                                            requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("emaildatastore")).FirstOrDefault().Value = "D&P";
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
                                                            SubmitOrder(order, requestOrderRequest, ref Mye2etxn, PERetryCount);
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
                                                        requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("emaildatastore")).FirstOrDefault().Value = "D&P";
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
                                                    string emailDataStore = "D&P";
                                                    string email_Supplier = "MX";

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
                                                                    throw new DnpException("Ignored the request as EmailDataStore is D&P");
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("emaildatastore")).FirstOrDefault().Value = "D&P";                                                            
                                                            throw new DnpException("Profile doesn't have active BTIEMAIL service in DNP");
                                                        }

                                                        if (String.IsNullOrEmpty(accountId.Trim()))
                                                        {                        
                                                            emailDataStore = "D&P";
                                                            requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("emaildatastore")).FirstOrDefault().Value = emailDataStore;                                                            
                                                            
                                                            List<string> mxMailboxesList = new List<string>(); List<string> yahooMailboxesList = new List<string>(); List<string> cpmsMailboxesList = new List<string>();

                                                            if (requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(inst => inst.Name.ToLower().Equals("suspendtype")))
                                                            {
                                                                string suspend_Type = requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(inst => inst.Name.ToLower().Equals("suspendtype")).FirstOrDefault().Value;
                                                                string suspend_Action = requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(inst => inst.Name.ToLower().Equals("suspendaction")).FirstOrDefault().Value;
                                                                string ISPServiceClass = requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(inst => inst.Name.ToLower().Equals("ispserviceclass")).FirstOrDefault().Value;

                                                                if (suspend_Type.Equals("S0145881", StringComparison.OrdinalIgnoreCase) && ISPServiceClass.Equals("ISP_SC_PM",StringComparison.OrdinalIgnoreCase))
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

                                                                                                    var emailSupplier = clientidentity.clientIdentityValidation != null && clientidentity.clientIdentityValidation.ToList().Exists(civ => civ.name != null && civ.name.Equals("EMAIL_SUPPLIER", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(civ.value)) ? clientidentity.clientIdentityValidation.ToList().Where(civ => civ.name != null && civ.name.Equals("EMAIL_SUPPLIER", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value : email_Supplier;

                                                                                                    if (emailSupplier.Equals("MX", StringComparison.OrdinalIgnoreCase))
                                                                                                        mxMailboxesList.Add(clientidentity.value);
                                                                                                    else if (emailSupplier.Equals("CriticalPath", StringComparison.OrdinalIgnoreCase))
                                                                                                        cpmsMailboxesList.Add(clientidentity.value);
                                                                                                    else
                                                                                                        yahooMailboxesList.Add(clientidentity.value);
                                                                                                }
                                                                                                else if (clientidentity.clientIdentityStatus.value.Equals("barred-abuse", StringComparison.OrdinalIgnoreCase)
                                                                                                    && clientidentity.clientIdentityValidation.ToList().Exists(clval => clval.name.Equals("BARRED_REASON") && clval.value.Equals("PM_DebtBarred", StringComparison.OrdinalIgnoreCase)))
                                                                                                {
                                                                                                    BarredAbuse_EmailList.Add(clientidentity.value);

                                                                                                    var emailSupplier = clientidentity.clientIdentityValidation != null && clientidentity.clientIdentityValidation.ToList().Exists(civ => civ.name != null && civ.name.Equals("EMAIL_SUPPLIER", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(civ.value)) ? clientidentity.clientIdentityValidation.ToList().Where(civ => civ.name != null && civ.name.Equals("EMAIL_SUPPLIER", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value : email_Supplier;

                                                                                                    if (emailSupplier.Equals("MX", StringComparison.OrdinalIgnoreCase))
                                                                                                        mxMailboxesList.Add(clientidentity.value);
                                                                                                    else if (emailSupplier.Equals("CriticalPath", StringComparison.OrdinalIgnoreCase))
                                                                                                        cpmsMailboxesList.Add(clientidentity.value);
                                                                                                    else
                                                                                                        yahooMailboxesList.Add(clientidentity.value);
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

                                                                        order.ProductOrderItems[0].RoleInstances[0].Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "MXMailboxes", Value = string.Join(",", mxMailboxesList.ToArray()) });
                                                                        order.ProductOrderItems[0].RoleInstances[0].Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "Yahoomailboxes", Value = string.Join(",", yahooMailboxesList.ToArray()) });
                                                                        order.ProductOrderItems[0].RoleInstances[0].Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "CPMSMailboxes", Value = string.Join(",", cpmsMailboxesList.ToArray()) });

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

                                                                        order.ProductOrderItems[0].RoleInstances[0].Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "MXMailboxes", Value = string.Join(",", mxMailboxesList.ToArray()) });
                                                                        order.ProductOrderItems[0].RoleInstances[0].Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "Yahoomailboxes", Value = string.Join(",", yahooMailboxesList.ToArray()) });
                                                                        order.ProductOrderItems[0].RoleInstances[0].Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "CPMSMailboxes", Value = string.Join(",", cpmsMailboxesList.ToArray()) });

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
                                                                    SubmitOrder(order, requestOrderRequest, ref Mye2etxn, PERetryCount);
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
                                                            throw new DnpException("Ignored the request as EmailDataStore is D&P");
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
                                                            MSEOSaaSMapper.SubmitOrder(order, requestOrderRequest, ref Mye2etxn);
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
                                                            MSEOSaaSMapper.SubmitOrder(order, requestOrderRequest, ref Mye2etxn);
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
                                                    string emailSupplier = "MX";
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
                                                            requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("emaildatastore")).FirstOrDefault().Value = "D&P";
                                                            InboundQueueDAL.UpdateQueueRawXML(Settings1.Default.IgnoredStatus, "Ignored the ISP Leg of Premium Email Provision and EmailDataStore is DNP", requestOrderRequest.Order.OrderIdentifier.Value);

                                                            foreach (OrderItem orderItem in requestOrderRequest.Order.OrderItem)
                                                            {
                                                                orderItem.Status = "Ignored";
                                                            }
                                                            notification.sendNotification(false, false, "001", "Ignored the ISP Leg of Premium Email Provision and EmailDataStore is DNP", ref Mye2etxn, true);
                                                            BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter : The Order " + requestOrderRequest.Order.OrderIdentifier.Value + "Ignored the ISP Leg of Premium Email Provision , for the given emailname:" + emailname);

                                                        }
                                                    }
                                                    else
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
                                                }
                                                //ISP email or premium email cease
                                                else if ((ConfigurationManager.AppSettings["EmailScodes"].Split(',').Contains(requestOrderRequest.Order.OrderItem[0].Specification[0].Identifier.Value1) || ConfigurationManager.AppSettings["PremiumEmailScodes"].Split(',').Contains(requestOrderRequest.Order.OrderItem[0].Specification[0].Identifier.Value1)) && requestOrderRequest.Order.OrderItem[0].Action.Code.Equals("Cancel", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    if (ConfigurationManager.AppSettings["TransitionSwitch"].Equals("Strategic", StringComparison.OrdinalIgnoreCase))
                                                    {
                                                        InboundQueueDAL.UpdateQueueRawXML(Settings1.Default.IgnoredStatus, "Product in the request are Ignored as the Transition Switch value is STRATEGIC", requestOrderRequest.Order.OrderIdentifier.Value);
                                                        //requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("emaildatastore")).FirstOrDefault().Value = "D&P";
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
                                                                    SubmitOrder(order, requestOrderRequest, ref Mye2etxn, PERetryCount);
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
                                                                SubmitOrder(order, requestOrderRequest, ref Mye2etxn, PERetryCount);
                                                        }
                                                        catch (DnpException DnPexception)
                                                        {                                                           
                                                            requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("emaildatastore")).FirstOrDefault().Value = "D&P";
                                                            
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
                                                    SubmitOrder(order, requestOrderRequest, ref Mye2etxn, PERetryCount);
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
                                    if (DnPexception.Message.Contains("CHO"))
                                    {
                                        InboundQueueDAL.UpdateQueueRawXML(Settings1.Default.FailedStatus, DnPexception.Message.ToString(), requestOrderRequest.Order.OrderIdentifier.Value);

                                        foreach (OrderItem orderItem in requestOrderRequest.Order.OrderItem)
                                        {
                                            orderItem.Status = "Failed";
                                        }                                        
                                        notification.sendNotification(false, false, "777", DnPexception.Message.ToString(), ref Mye2etxn);
                                    }
                                    else
                                    {
                                        InboundQueueDAL.UpdateQueueRawXML(Settings1.Default.IgnoredStatus, DnPexception.Message.ToString(), requestOrderRequest.Order.OrderIdentifier.Value);

                                        foreach (OrderItem orderItem in requestOrderRequest.Order.OrderItem)
                                        {
                                            orderItem.Status = "Ignored";
                                        }
                                        notification.sendNotification(false, false, "001", DnPexception.Message.ToString(), ref Mye2etxn, true);                                        
                                    }
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
                                    notification.sendNotification(false, false, "001", Mdmexception.Message, ref Mye2etxn, true);
                                    BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOMQAdapter MDM exception : " + Mdmexception.Message + " for order identifier :" + requestOrderRequest.Order.OrderIdentifier.Value);
                                }
                                catch (CMPSException cmpsException)
                                {
                                    InboundQueueDAL.UpdateQueueRawXML(Settings1.Default.RejectedStatus, cmpsException.Message, requestOrderRequest.Order.OrderIdentifier.Value);
                                    foreach (OrderItem orderItem in requestOrderRequest.Order.OrderItem)
                                    {
                                        orderItem.Status = "Rejected";
                                    }
                                    notification.sendNotification(false, false, "002", cmpsException.Message, ref Mye2etxn, isAck ? false : true);
                                    BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOMQAdapter CMPS exception : " + cmpsException.ToString() + " for order identifier :" + requestOrderRequest.Order.OrderIdentifier.Value);
                                }
                                catch (Exception serviceException)
                                {
                                    InboundQueueDAL.UpdateQueueRawXML("Fault@MSEO", serviceException.ToString(), requestOrderRequest.Order.OrderIdentifier.Value);
                                    BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOMQAdapter exception : " + serviceException.ToString());
                                }

                                isInTransProcess = true;

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
                                shutdownSignal = true;
                            }
                            finally
                            {
                                //Mye2etxn.endInboundCall();
                                isInTransProcess = false;
                                if (shutdownSignal)
                                {
                                    timer.Enabled = true;
                                    threadShutdownSignal.Set();
                                }
                            }
                        }
                    }
                    finally
                    {
                        try
                        {
                            q.Close();
                        }
                        catch (Exception QueueCloseEx)
                        {
                            Logger.Write("Queue Close Exception :: " + Process.GetCurrentProcess().ProcessName + " , Exception caught : " + QueueCloseEx.Message + " at" + QueueCloseEx.StackTrace + " , exception.tostring() : " + QueueCloseEx.ToString(), Logger.TypeEnum.ExceptionTrace);
                        }
                    }
                }
                finally
                {
                    try
                    {
                        qMan.Disconnect();
                    }
                    catch (Exception QueueManagerCloseEx)
                    {
                        Logger.Write("QueueManager Close Exception :: " + Process.GetCurrentProcess().ProcessName + " , Exception caught : " + QueueManagerCloseEx.Message + " at" + QueueManagerCloseEx.StackTrace + " , exception.tostring() : " + QueueManagerCloseEx.ToString(), Logger.TypeEnum.ExceptionTrace);
                    }
                }
            }
            catch (Exception exception)
            {
                BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOMQAdapter exception: " + exception.ToString());
                timer.Enabled = true;
                Logger.Write("Prcoess Name " + Process.GetCurrentProcess().ProcessName + " , Exception caught : " + exception.Message + " at" + exception.StackTrace + " , exception.tostring() : " + exception.ToString(), Logger.TypeEnum.ExceptionTrace);
                //Logger.Write("Proces Name " + Process.GetCurrentProcess().ProcessName + " , Exception caught : " + exception.Message + " at" + exception.StackTrace + " , exception.tostring() : " + exception.ToString(), Logger.TypeEnum.EmailTrace);                
            }
            finally
            {
                timer.Enabled = true;
            }
        }

        #endregion

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
                        value = true;
                        //if (request.Order.OrderItem[0].Action.Code.ToLower() == "create" || request.Order.OrderItem[0].Action.Code.ToLower() == "modify")
                        //{
                        //    if (ConfigurationManager.AppSettings["isThomasNewJourneySwitch"].ToLower() == "true")
                        //    {
                        //        value = true;
                        //    }
                        //}
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

        private void SubmitOrder(SAASPE.Order order, OrderRequest requestOrderRequest, ref E2ETransaction Mye2etxn, int PERetryCount)
        {
            try
            {
                MSEOSaaSMapper.SubmitOrder(order, requestOrderRequest, ref Mye2etxn);
            }
            catch (System.TimeoutException ex)
            {
                if (ConfigurationManager.AppSettings.AllKeys.Contains("isPERetrySwitch"))
                {
                    if (ConfigurationManager.AppSettings["isPERetrySwitch"].ToLower() == "true")
                    {
                        PERetryCount++;
                        if (PERetryCount < Convert.ToInt32(ConfigurationManager.AppSettings["springRetryCount"]))
                            SubmitOrder(order, requestOrderRequest, ref Mye2etxn, PERetryCount);
                    }
                }
            }
            catch (CommunicationException ex)
            {
                if (ConfigurationManager.AppSettings.AllKeys.Contains("isPERetrySwitch"))
                {
                    if (ConfigurationManager.AppSettings["isPERetrySwitch"].ToLower() == "true")
                    {
                        PERetryCount++;
                        if (PERetryCount < Convert.ToInt32(ConfigurationManager.AppSettings["springRetryCount"]))
                            SubmitOrder(order, requestOrderRequest, ref Mye2etxn, PERetryCount);
                    }
                }
            }
        }

    }
}
