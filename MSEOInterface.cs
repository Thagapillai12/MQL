using System.Diagnostics;
using System.Web.Services;
using System.ComponentModel;
using System.Web.Services.Protocols;
using System;
using System.Xml.Serialization;
using System.Text;
using System.IO;
using System.Globalization;
using System.Resources;
using System.Xml;
using System.Net;


namespace BT.SaaS.MSEOAdapter
{
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15", TypeName = "OrderRequest")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    [XmlRoot(ElementName = "requestOrderRequest", Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15")]
    public partial class OrderRequest
    {

        /// <remarks/>
        private StandardHeaderBlock standardHeader;

        /// <remarks/>
        private Order order;

        public OrderRequest()
        {
        }

        public OrderRequest(StandardHeaderBlock standardHeader, Order order)
        {
            this.standardHeader = standardHeader;
            this.order = order;
        }

        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", ElementName = "standardHeader")]
        public StandardHeaderBlock StandardHeader
        {
            get
            {
                return this.standardHeader;
            }
            set
            {
                if ((value == null))
                {
                    throw new System.ArgumentNullException("StandardHeader");
                }
                if ((this.standardHeader != value))
                {
                    this.standardHeader = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "order")]
        public Order Order
        {
            get
            {
                return this.order;
            }
            set
            {
                if ((this.order != value))
                {
                    this.order = value;
                }
            }
        }


        #region SerializeObject
        /// <summary>
        /// This method returns an xml representation of the object
        /// </summary>
        /// <returns>The xml resprestation as Srting</returns>
        public string SerializeObject()
        {
            StringBuilder serialBuilder;
            XmlSerializer messageSerializer;
            try
            {
                serialBuilder = new StringBuilder(1000);
                using (StringWriter serialWriter = new StringWriter(serialBuilder, CultureInfo.InvariantCulture))
                {
                    messageSerializer = new XmlSerializer(this.GetType());
                    messageSerializer.Serialize(serialWriter, this);
                    return serialWriter.ToString();
                }
            }
            catch (Exception serializeObjectException)
            {
                throw serializeObjectException;
            }
            finally
            {
                serialBuilder = null;
                messageSerializer = null;
            }
        }


        #endregion

        #region DeserializeObject

        /// <summary>
        ///	This method deserializes the input string into a ModifyState Request object
        /// </summary>
        /// <param name="xmlInput">The string to be deserialized</param>
        /// <returns>Populated ModifyStateRequest object</returns>
        public static OrderRequest DeserializeObject(string xmlInput)
        {
            XmlSerializer messageDeserial;
            OrderRequest modifyStateRequest;
            try
            {

                //  deserialize
                using (StringReader messageStreamer = new StringReader(xmlInput))
                {
                    using (XmlReader messageReader = new XmlTextReader(messageStreamer))
                    {
                        messageDeserial = new XmlSerializer(typeof(OrderRequest));

                        //modifyStateRequest = (OrderRequest)messageDeserial.Deserialize(messageReader);

                        if (messageDeserial.CanDeserialize(messageReader))
                        {
                            modifyStateRequest = (OrderRequest)messageDeserial.Deserialize(messageReader);
                        }
                        else
                        {
                            XmlRootAttribute root = new XmlRootAttribute("requestOrderRequest");
                            root.Namespace = @"http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15";
                            messageDeserial = new XmlSerializer(typeof(OrderRequest), root);
                            modifyStateRequest = (OrderRequest)messageDeserial.Deserialize(messageReader);

                        }
                    }
                }


                return modifyStateRequest;
            }
            catch (Exception deserializeObjectException)
            {
                throw deserializeObjectException;
            }
            finally
            {
                messageDeserial = null;
            }

        }

        #endregion DeserializeObject
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", TypeName = "StandardHeaderBlock")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class StandardHeaderBlock
    {

        /// <remarks/>
        private E2E e2e;

        /// <remarks/>
        private ServiceState serviceState;

        /// <remarks/>
        private ServiceAddressing serviceAddressing;

        /// <remarks/>
        private ServiceProperties serviceProperties;

        /// <remarks/>
        private ServiceSpecification serviceSpecification;

        /// <remarks/>
        private ServiceSecurity serviceSecurity;

        public StandardHeaderBlock()
        {
        }

        public StandardHeaderBlock(E2E e2e, ServiceState serviceState, ServiceAddressing serviceAddressing, ServiceProperties serviceProperties, ServiceSpecification serviceSpecification, ServiceSecurity serviceSecurity)
        {
            this.e2e = e2e;
            this.serviceState = serviceState;
            this.serviceAddressing = serviceAddressing;
            this.serviceProperties = serviceProperties;
            this.serviceSpecification = serviceSpecification;
            this.serviceSecurity = serviceSecurity;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "e2e")]
        public E2E E2e
        {
            get
            {
                return this.e2e;
            }
            set
            {
                if ((this.e2e != value))
                {
                    this.e2e = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "serviceState")]
        public ServiceState ServiceState
        {
            get
            {
                return this.serviceState;
            }
            set
            {
                if ((value == null))
                {
                    throw new System.ArgumentNullException("ServiceState");
                }
                if ((this.serviceState != value))
                {
                    this.serviceState = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "serviceAddressing")]
        public ServiceAddressing ServiceAddressing
        {
            get
            {
                return this.serviceAddressing;
            }
            set
            {
                if ((value == null))
                {
                    throw new System.ArgumentNullException("ServiceAddressing");
                }
                if ((this.serviceAddressing != value))
                {
                    this.serviceAddressing = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "serviceProperties")]
        public ServiceProperties ServiceProperties
        {
            get
            {
                return this.serviceProperties;
            }
            set
            {
                if ((this.serviceProperties != value))
                {
                    this.serviceProperties = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "serviceSpecification")]
        public ServiceSpecification ServiceSpecification
        {
            get
            {
                return this.serviceSpecification;
            }
            set
            {
                if ((this.serviceSpecification != value))
                {
                    this.serviceSpecification = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "serviceSecurity")]
        public ServiceSecurity ServiceSecurity
        {
            get
            {
                return this.serviceSecurity;
            }
            set
            {
                if ((this.serviceSecurity != value))
                {
                    this.serviceSecurity = value;
                }
            }
        }


        #region SerializeObject
        /// <summary>
        /// This method returns an xml representation of the object
        /// </summary>
        /// <returns>The xml resprestation as Srting</returns>
        public string SerializeObject()
        {
            StringBuilder serialBuilder;
            StringWriter serialWriter;
            XmlSerializer messageSerializer;
            try
            {
                serialBuilder = new StringBuilder(1000);
                serialWriter =
                   new StringWriter(serialBuilder, CultureInfo.InvariantCulture);
                messageSerializer = new XmlSerializer(this.GetType());
                messageSerializer.Serialize(serialWriter, this);
                return serialWriter.ToString();
            }
            catch (Exception serializeObjectException)
            {
                throw serializeObjectException;
            }
            finally
            {
                serialBuilder = null;
                serialWriter = null;
                messageSerializer = null;
            }
        }


        #endregion
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", TypeName = "E2E")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class E2E
    {

        /// <remarks/>
        private string e2EDATA;

        public E2E()
        {
        }

        public E2E(string e2EDATA)
        {
            this.e2EDATA = e2EDATA;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "E2EDATA")]
        public string E2EDATA
        {
            get
            {
                return this.e2EDATA;
            }
            set
            {
                if ((this.e2EDATA != value))
                {
                    this.e2EDATA = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15", TypeName = "ActivityResponse")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class ActivityResponse
    {

        /// <remarks/>
        private StandardHeaderBlock standardHeader;

        /// <remarks/>
        private Order[] order;

        /// <remarks/>
        private ActivitySpecification activityType;

        public ActivityResponse()
        {
        }

        public ActivityResponse(StandardHeaderBlock standardHeader, Order[] order, ActivitySpecification activityType)
        {
            this.standardHeader = standardHeader;
            this.order = order;
            this.activityType = activityType;
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "standardHeader")]
        public StandardHeaderBlock StandardHeader
        {
            get
            {
                return this.standardHeader;
            }
            set
            {
                if ((value == null))
                {
                    throw new System.ArgumentNullException("StandardHeader");
                }
                if ((this.standardHeader != value))
                {
                    this.standardHeader = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute("order", IsNullable = true)]
        public Order[] Order
        {
            get
            {
                return this.order;
            }
            set
            {
                if ((this.order != value))
                {
                    this.order = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "activityType")]
        public ActivitySpecification ActivityType
        {
            get
            {
                return this.activityType;
            }
            set
            {
                if ((this.activityType != value))
                {
                    this.activityType = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Agreements", TypeName = "Order")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class Order
    {

        /// <remarks/>
        private string action;

        /// <remarks/>
        private bool actionSpecified;

        /// <remarks/>
        private string version;

        /// <remarks/>
        private OrderIdentifier orderIdentifier;

        /// <remarks/>
        private Party fromParty;

        /// <remarks/>
        private Party toParty;

        /// <remarks/>
        private Party thirdPartyChannel;

        /// <remarks/>
        private DateTime orderDate;

        /// <remarks/>
        private GlobalIdentifier alternativeIdentifier;

        /// <remarks/>
        private BillingAccount billingAccount;

        /// <remarks/>
        private OrderItem[] orderItem;

        /// <remarks/>
        private Money totalPrice;

        /// <remarks/>
        private string status;

        /// <remarks/>
        private string substatus;

        /// <remarks/>
        private OrderIdentifier parentOrderIdentifier;

        /// <remarks/>
        private Error error;

        /// <remarks/>
        private Note[] note;

        public Order()
        {
        }

        public Order(
                    string action,
                    bool actionSpecified,
                    string version,
                    OrderIdentifier orderIdentifier,
                    Party fromParty,
                    Party toParty,
                    Party thirdPartyChannel,
                    DateTime orderDate,
                    GlobalIdentifier alternativeIdentifier,
                    BillingAccount billingAccount,
                    OrderItem[] orderItem,
                    Money totalPrice,
                    string status,
                    string substatus,
                    OrderIdentifier parentOrderIdentifier,
                    Error error,
                    Note[] note)
        {
            this.action = action;
            this.actionSpecified = actionSpecified;
            this.version = version;
            this.orderIdentifier = orderIdentifier;
            this.fromParty = fromParty;
            this.toParty = toParty;
            this.thirdPartyChannel = thirdPartyChannel;
            this.orderDate = orderDate;
            this.alternativeIdentifier = alternativeIdentifier;
            this.billingAccount = billingAccount;
            this.orderItem = orderItem;
            this.totalPrice = totalPrice;
            this.status = status;
            this.substatus = substatus;
            this.parentOrderIdentifier = parentOrderIdentifier;
            this.error = error;
            this.note = note;
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "action")]
        public string Action
        {
            get
            {
                return this.action;
            }
            set
            {
                if ((this.action != value))
                {
                    this.action = value;
                    this.actionSpecified = true;
                }
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool ActionSpecified
        {
            get
            {
                return this.actionSpecified;
            }
            set
            {
                if ((this.actionSpecified != value))
                {
                    this.actionSpecified = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "version")]
        public string Version
        {
            get
            {
                return this.version;
            }
            set
            {
                if ((this.version != value))
                {
                    this.version = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "orderIdentifier")]
        public OrderIdentifier OrderIdentifier
        {
            get
            {
                return this.orderIdentifier;
            }
            set
            {
                if ((this.orderIdentifier != value))
                {
                    this.orderIdentifier = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "fromParty")]
        public Party FromParty
        {
            get
            {
                return this.fromParty;
            }
            set
            {
                if ((this.fromParty != value))
                {
                    this.fromParty = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "toParty")]
        public Party ToParty
        {
            get
            {
                return this.toParty;
            }
            set
            {
                if ((this.toParty != value))
                {
                    this.toParty = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "thirdPartyChannel")]
        public Party ThirdPartyChannel
        {
            get
            {
                return this.thirdPartyChannel;
            }
            set
            {
                if ((this.thirdPartyChannel != value))
                {
                    this.thirdPartyChannel = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "orderDate")]
        public DateTime OrderDate
        {
            get
            {
                return this.orderDate;
            }
            set
            {
                if ((this.orderDate != value))
                {
                    this.orderDate = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "alternativeIdentifier")]
        public GlobalIdentifier AlternativeIdentifier
        {
            get
            {
                return this.alternativeIdentifier;
            }
            set
            {
                if ((this.alternativeIdentifier != value))
                {
                    this.alternativeIdentifier = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "billingAccount")]
        public BillingAccount BillingAccount
        {
            get
            {
                return this.billingAccount;
            }
            set
            {
                if ((this.billingAccount != value))
                {
                    this.billingAccount = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute("orderItem", IsNullable = true)]
        public OrderItem[] OrderItem
        {
            get
            {
                return this.orderItem;
            }
            set
            {
                if ((this.orderItem != value))
                {
                    this.orderItem = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "totalPrice")]
        public Money TotalPrice
        {
            get
            {
                return this.totalPrice;
            }
            set
            {
                if ((this.totalPrice != value))
                {
                    this.totalPrice = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "status")]
        public string Status
        {
            get
            {
                return this.status;
            }
            set
            {
                if ((this.status != value))
                {
                    this.status = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "substatus")]
        public string Substatus
        {
            get
            {
                return this.substatus;
            }
            set
            {
                if ((this.substatus != value))
                {
                    this.substatus = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "parentOrderIdentifier")]
        public OrderIdentifier ParentOrderIdentifier
        {
            get
            {
                return this.parentOrderIdentifier;
            }
            set
            {
                if ((this.parentOrderIdentifier != value))
                {
                    this.parentOrderIdentifier = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "error")]
        public Error Error
        {
            get
            {
                return this.error;
            }
            set
            {
                if ((this.error != value))
                {
                    this.error = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute("note")]
        public Note[] Note
        {
            get
            {
                return this.note;
            }
            set
            {
                if ((this.note != value))
                {
                    this.note = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Agreements", TypeName = "actionOrderEnum")]
    public enum actionOrderEnum
    {

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute(Name = "Create")]
        Create,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute(Name = "Amend")]
        Amend,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute(Name = "Cancel")]
        Cancel,
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Agreements", TypeName = "OrderIdentifier")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class OrderIdentifier
    {

        /// <remarks/>
        private string value;

        public OrderIdentifier()
        {
        }

        public OrderIdentifier(string value)
        {
            this.value = value;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "value")]
        public string Value
        {
            get
            {
                return this.value;
            }
            set
            {
                if ((this.value != value))
                {
                    this.value = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Parties", TypeName = "Party")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class Party
    {

        /// <remarks/>
        private PartyName name;

        /// <remarks/>
        private string id;

        /// <remarks/>
        private Employment employee;

        /// <remarks/>
        private ContactDetails contactDetails;

        /// <remarks/>
        private Employment employer;

        public Party()
        {
        }

        public Party(PartyName name, string id, Employment employee, ContactDetails contactDetails, Employment employer)
        {
            this.name = name;
            this.id = id;
            this.employee = employee;
            this.contactDetails = contactDetails;
            this.employer = employer;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "name")]
        public PartyName Name
        {
            get
            {
                return this.name;
            }
            set
            {
                if ((this.name != value))
                {
                    this.name = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "id")]
        public string Id
        {
            get
            {
                return this.id;
            }
            set
            {
                if ((this.id != value))
                {
                    this.id = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "employee")]
        public Employment Employee
        {
            get
            {
                return this.employee;
            }
            set
            {
                if ((this.employee != value))
                {
                    this.employee = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "contactDetails")]
        public ContactDetails ContactDetails
        {
            get
            {
                return this.contactDetails;
            }
            set
            {
                if ((this.contactDetails != value))
                {
                    this.contactDetails = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "employer")]
        public Employment Employer
        {
            get
            {
                return this.employer;
            }
            set
            {
                if ((this.employer != value))
                {
                    this.employer = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Parties", TypeName = "PartyName")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class PartyName
    {

        /// <remarks/>
        private string nameElement;

        /// <remarks/>
        private string nameValue;

        public PartyName()
        {
        }

        public PartyName(string nameElement, string nameValue)
        {
            this.nameElement = nameElement;
            this.nameValue = nameValue;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "nameElement")]
        public string NameElement
        {
            get
            {
                return this.nameElement;
            }
            set
            {
                if ((this.nameElement != value))
                {
                    this.nameElement = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "nameValue")]
        public string NameValue
        {
            get
            {
                return this.nameValue;
            }
            set
            {
                if ((this.nameValue != value))
                {
                    this.nameValue = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/HR", TypeName = "Employment")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class Employment
    {

        /// <remarks/>
        private string identifier;

        /// <remarks/>
        private string jobTitle;

        /// <remarks/>
        private Party party;

        public Employment()
        {
        }

        public Employment(string identifier, string jobTitle, Party party)
        {
            this.identifier = identifier;
            this.jobTitle = jobTitle;
            this.party = party;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "identifier")]
        public string Identifier
        {
            get
            {
                return this.identifier;
            }
            set
            {
                if ((this.identifier != value))
                {
                    this.identifier = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "jobTitle")]
        public string JobTitle
        {
            get
            {
                return this.jobTitle;
            }
            set
            {
                if ((this.jobTitle != value))
                {
                    this.jobTitle = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "party")]
        public Party Party
        {
            get
            {
                return this.party;
            }
            set
            {
                if ((this.party != value))
                {
                    this.party = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Parties", TypeName = "ContactDetails")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class ContactDetails
    {

        /// <remarks/>
        private string facsimile;

        /// <remarks/>
        private string emailAddress;

        /// <remarks/>
        private string mobilePhoneNumber;

        /// <remarks/>
        private string phoneNumber;

        /// <remarks/>
        private string pager;

        /// <remarks/>
        private string contactRole;

        /// <remarks/>
        private string contactAvailability;

        /// <remarks/>
        private Person person;

        /// <remarks/>
        private string id;

        public ContactDetails()
        {
        }

        public ContactDetails(string facsimile, string emailAddress, string mobilePhoneNumber, string phoneNumber, string pager, string contactRole, string contactAvailability, Person person, string id)
        {
            this.facsimile = facsimile;
            this.emailAddress = emailAddress;
            this.mobilePhoneNumber = mobilePhoneNumber;
            this.phoneNumber = phoneNumber;
            this.pager = pager;
            this.contactRole = contactRole;
            this.contactAvailability = contactAvailability;
            this.person = person;
            this.id = id;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "facsimile")]
        public string Facsimile
        {
            get
            {
                return this.facsimile;
            }
            set
            {
                if ((this.facsimile != value))
                {
                    this.facsimile = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "emailAddress")]
        public string EmailAddress
        {
            get
            {
                return this.emailAddress;
            }
            set
            {
                if ((this.emailAddress != value))
                {
                    this.emailAddress = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "mobilePhoneNumber")]
        public string MobilePhoneNumber
        {
            get
            {
                return this.mobilePhoneNumber;
            }
            set
            {
                if ((this.mobilePhoneNumber != value))
                {
                    this.mobilePhoneNumber = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "phoneNumber")]
        public string PhoneNumber
        {
            get
            {
                return this.phoneNumber;
            }
            set
            {
                if ((this.phoneNumber != value))
                {
                    this.phoneNumber = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "pager")]
        public string Pager
        {
            get
            {
                return this.pager;
            }
            set
            {
                if ((this.pager != value))
                {
                    this.pager = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "contactRole")]
        public string ContactRole
        {
            get
            {
                return this.contactRole;
            }
            set
            {
                if ((this.contactRole != value))
                {
                    this.contactRole = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "contactAvailability")]
        public string ContactAvailability
        {
            get
            {
                return this.contactAvailability;
            }
            set
            {
                if ((this.contactAvailability != value))
                {
                    this.contactAvailability = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "person")]
        public Person Person
        {
            get
            {
                return this.person;
            }
            set
            {
                if ((this.person != value))
                {
                    this.person = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "id")]
        public string Id
        {
            get
            {
                return this.id;
            }
            set
            {
                if ((this.id != value))
                {
                    this.id = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Parties", TypeName = "Person")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class Person
    {

        /// <remarks/>
        private string gender;

        /// <remarks/>
        private PersonName personName;

        /// <remarks/>
        private string placeOfBirth;

        /// <remarks/>
        private string nationality;

        /// <remarks/>
        private string maritalStatus;

        /// <remarks/>
        private string dateOfBirth;

        public Person()
        {
        }

        public Person(string gender, PersonName personName, string placeOfBirth, string nationality, string maritalStatus, string dateOfBirth)
        {
            this.gender = gender;
            this.personName = personName;
            this.placeOfBirth = placeOfBirth;
            this.nationality = nationality;
            this.maritalStatus = maritalStatus;
            this.dateOfBirth = dateOfBirth;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "gender")]
        public string Gender
        {
            get
            {
                return this.gender;
            }
            set
            {
                if ((this.gender != value))
                {
                    this.gender = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "personName")]
        public PersonName PersonName
        {
            get
            {
                return this.personName;
            }
            set
            {
                if ((this.personName != value))
                {
                    this.personName = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "placeOfBirth")]
        public string PlaceOfBirth
        {
            get
            {
                return this.placeOfBirth;
            }
            set
            {
                if ((this.placeOfBirth != value))
                {
                    this.placeOfBirth = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "nationality")]
        public string Nationality
        {
            get
            {
                return this.nationality;
            }
            set
            {
                if ((this.nationality != value))
                {
                    this.nationality = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "maritalStatus")]
        public string MaritalStatus
        {
            get
            {
                return this.maritalStatus;
            }
            set
            {
                if ((this.maritalStatus != value))
                {
                    this.maritalStatus = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "dateOfBirth")]
        public string DateOfBirth
        {
            get
            {
                return this.dateOfBirth;
            }
            set
            {
                if ((this.dateOfBirth != value))
                {
                    this.dateOfBirth = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Parties", TypeName = "PersonName")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class PersonName
    {

        /// <remarks/>
        private string title;

        /// <remarks/>
        private string forename;

        /// <remarks/>
        private string surname;

        /// <remarks/>
        private string[] initials;

        /// <remarks/>
        private string honours;

        public PersonName()
        {
        }

        public PersonName(string title, string forename, string surname, string[] initials, string honours)
        {
            this.title = title;
            this.forename = forename;
            this.surname = surname;
            this.initials = initials;
            this.honours = honours;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "title")]
        public string Title
        {
            get
            {
                return this.title;
            }
            set
            {
                if ((this.title != value))
                {
                    this.title = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "forename")]
        public string Forename
        {
            get
            {
                return this.forename;
            }
            set
            {
                if ((this.forename != value))
                {
                    this.forename = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "surname")]
        public string Surname
        {
            get
            {
                return this.surname;
            }
            set
            {
                if ((this.surname != value))
                {
                    this.surname = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute("initials", IsNullable = true)]
        public string[] Initials
        {
            get
            {
                return this.initials;
            }
            set
            {
                if ((this.initials != value))
                {
                    this.initials = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "honours")]
        public string Honours
        {
            get
            {
                return this.honours;
            }
            set
            {
                if ((this.honours != value))
                {
                    this.honours = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/BaseTypes", TypeName = "DateTime")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class DateTime
    {

        /// <remarks/>
        private string dateTime;

        public DateTime()
        {
        }

        public DateTime(string dateTime)
        {
            this.dateTime = dateTime;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "dateTime")]
        public string DateTime1
        {
            get
            {
                return this.dateTime;
            }
            set
            {
                if ((this.dateTime != value))
                {
                    this.dateTime = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Identification", TypeName = "GlobalIdentifier")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class GlobalIdentifier
    {

        /// <remarks/>
        private string value;

        /// <remarks/>
        private string type;

        public GlobalIdentifier()
        {
        }

        public GlobalIdentifier(string value, string type)
        {
            this.value = value;
            this.type = type;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "value")]
        public string Value
        {
            get
            {
                return this.value;
            }
            set
            {
                if ((this.value != value))
                {
                    this.value = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "type")]
        public string Type
        {
            get
            {
                return this.type;
            }
            set
            {
                if ((this.type != value))
                {
                    this.type = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Finance", TypeName = "BillingAccount")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class BillingAccount
    {

        /// <remarks/>
        private string type;

        /// <remarks/>
        private Money creditLimit;

        /// <remarks/>
        private string accountNumber;

        /// <remarks/>
        private BudgetCentre budgetCentre;

        public BillingAccount()
        {
        }

        public BillingAccount(string type, Money creditLimit, string accountNumber, BudgetCentre budgetCentre)
        {
            this.type = type;
            this.creditLimit = creditLimit;
            this.accountNumber = accountNumber;
            this.budgetCentre = budgetCentre;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "type")]
        public string Type
        {
            get
            {
                return this.type;
            }
            set
            {
                if ((this.type != value))
                {
                    this.type = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "creditLimit")]
        public Money CreditLimit
        {
            get
            {
                return this.creditLimit;
            }
            set
            {
                if ((this.creditLimit != value))
                {
                    this.creditLimit = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "accountNumber")]
        public string AccountNumber
        {
            get
            {
                return this.accountNumber;
            }
            set
            {
                if ((this.accountNumber != value))
                {
                    this.accountNumber = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "budgetCentre")]
        public BudgetCentre BudgetCentre
        {
            get
            {
                return this.budgetCentre;
            }
            set
            {
                if ((this.budgetCentre != value))
                {
                    this.budgetCentre = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/BaseTypes", TypeName = "Money")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class Money
    {

        /// <remarks/>
        private CurrencyCode currencyCode;

        /// <remarks/>
        private System.Nullable<int> amount;

        public Money()
        {
        }

        public Money(CurrencyCode currencyCode, System.Nullable<int> amount)
        {
            this.currencyCode = currencyCode;
            this.amount = amount;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "currencyCode")]
        public CurrencyCode CurrencyCode
        {
            get
            {
                return this.currencyCode;
            }
            set
            {
                if ((this.currencyCode != value))
                {
                    this.currencyCode = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "amount")]
        public System.Nullable<int> Amount
        {
            get
            {
                return this.amount;
            }
            set
            {
                if ((this.amount != value))
                {
                    this.amount = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/BaseTypes", TypeName = "CurrencyCode")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class CurrencyCode
    {

        /// <remarks/>
        private string code;

        /// <remarks/>
        private string symblol;

        /// <remarks/>
        private string name;

        public CurrencyCode()
        {
        }

        public CurrencyCode(string code, string symblol, string name)
        {
            this.code = code;
            this.symblol = symblol;
            this.name = name;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "code")]
        public string Code
        {
            get
            {
                return this.code;
            }
            set
            {
                if ((this.code != value))
                {
                    this.code = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "symblol")]
        public string Symblol
        {
            get
            {
                return this.symblol;
            }
            set
            {
                if ((this.symblol != value))
                {
                    this.symblol = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "name")]
        public string Name
        {
            get
            {
                return this.name;
            }
            set
            {
                if ((this.name != value))
                {
                    this.name = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Finance", TypeName = "BudgetCentre")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class BudgetCentre
    {

        /// <remarks/>
        private BudgetCentreIdentifier budgetCentreIdentifier;

        public BudgetCentre()
        {
        }

        public BudgetCentre(BudgetCentreIdentifier budgetCentreIdentifier)
        {
            this.budgetCentreIdentifier = budgetCentreIdentifier;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "budgetCentreIdentifier")]
        public BudgetCentreIdentifier BudgetCentreIdentifier
        {
            get
            {
                return this.budgetCentreIdentifier;
            }
            set
            {
                if ((this.budgetCentreIdentifier != value))
                {
                    this.budgetCentreIdentifier = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Finance", TypeName = "BudgetCentreIdentifier")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class BudgetCentreIdentifier
    {

        /// <remarks/>
        private string id;

        public BudgetCentreIdentifier()
        {
        }

        public BudgetCentreIdentifier(string id)
        {
            this.id = id;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "id")]
        public string Id
        {
            get
            {
                return this.id;
            }
            set
            {
                if ((this.id != value))
                {
                    this.id = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Agreements", TypeName = "OrderItem")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class OrderItem
    {

        /// <remarks/>
        private DateTime proposedDateTime;

        /// <remarks/>
        private DateTime completedDateTime;

        /// <remarks/>
        private DateTime committedDateTime;

        /// <remarks/>
        private System.Nullable<int> quantity;

        /// <remarks/>
        private bool quantitySpecified;

        /// <remarks/>
        private DateTime requiredDateTime;

        /// <remarks/>
        private OrderItemAction action;

        /// <remarks/>
        private OrderItemIdentifier identifier;

        /// <remarks/>
        private Money totalPrice;

        /// <remarks/>
        private GlobalIdentifier[] alternativeIdentifer;

        /// <remarks/>
        private Money unitPrice;

        /// <remarks/>
        private Contract holdToTerm;

        /// <remarks/>
        private BillingAccount billingAccount;

        /// <remarks/>
        private OrderItemAssociation[] orderItemAssociation;

        /// <remarks/>
        private OrderItemMembership[] orderItemMembership;

        /// <remarks/>
        private Installation installation;

        /// <remarks/>
        private Location deliveryLocation;

        /// <remarks/>
        private Specification[] specification;

        /// <remarks/>
        private Instance[] instance;

        /// <remarks/>
        private Activity[] activity;

        /// <remarks/>
        private OrderItemOverride[] orderItemOverride;

        /// <remarks/>
        private OrderItemSplitCharging[] orderItemSplitCharging;

        /// <remarks/>
        private string status;

        /// <remarks/>
        private Note[] note;

        /// <remarks/>
        private Error error;

        /// <remarks/>
        private string fromReference;

        /// <remarks/>
        private string toReference;

        public OrderItem()
        {
        }

        public OrderItem(
                    DateTime proposedDateTime,
                    DateTime completedDateTime,
                    DateTime committedDateTime,
                    System.Nullable<int> quantity,
                    bool quantitySpecified,
                    DateTime requiredDateTime,
                    OrderItemAction action,
                    OrderItemIdentifier identifier,
                    Money totalPrice,
                    GlobalIdentifier[] alternativeIdentifer,
                    Money unitPrice,
                    Contract holdToTerm,
                    BillingAccount billingAccount,
                    OrderItemAssociation[] orderItemAssociation,
                    OrderItemMembership[] orderItemMembership,
                    Installation installation,
                    Location deliveryLocation,
                    Specification[] specification,
                    Instance[] instance,
                    Activity[] activity,
                    OrderItemOverride[] orderItemOverride,
                    OrderItemSplitCharging[] orderItemSplitCharging,
                    string status,
                    Note[] note,
                    Error error,
                    string fromReference,
                    string toReference)
        {
            this.proposedDateTime = proposedDateTime;
            this.completedDateTime = completedDateTime;
            this.committedDateTime = committedDateTime;
            this.quantity = quantity;
            this.quantitySpecified = quantitySpecified;
            this.requiredDateTime = requiredDateTime;
            this.action = action;
            this.identifier = identifier;
            this.totalPrice = totalPrice;
            this.alternativeIdentifer = alternativeIdentifer;
            this.unitPrice = unitPrice;
            this.holdToTerm = holdToTerm;
            this.billingAccount = billingAccount;
            this.orderItemAssociation = orderItemAssociation;
            this.orderItemMembership = orderItemMembership;
            this.installation = installation;
            this.deliveryLocation = deliveryLocation;
            this.specification = specification;
            this.instance = instance;
            this.activity = activity;
            this.orderItemOverride = orderItemOverride;
            this.orderItemSplitCharging = orderItemSplitCharging;
            this.status = status;
            this.note = note;
            this.error = error;
            this.fromReference = fromReference;
            this.toReference = toReference;
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "proposedDateTime")]
        public DateTime ProposedDateTime
        {
            get
            {
                return this.proposedDateTime;
            }
            set
            {
                if ((this.proposedDateTime != value))
                {
                    this.proposedDateTime = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "completedDateTime")]
        public DateTime CompletedDateTime
        {
            get
            {
                return this.completedDateTime;
            }
            set
            {
                if ((this.completedDateTime != value))
                {
                    this.completedDateTime = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "committedDateTime")]
        public DateTime CommittedDateTime
        {
            get
            {
                return this.committedDateTime;
            }
            set
            {
                if ((this.committedDateTime != value))
                {
                    this.committedDateTime = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "quantity")]
        public System.Nullable<int> Quantity
        {
            get
            {
                return this.quantity;
            }
            set
            {
                if ((this.quantity != value))
                {
                    this.quantity = value;
                    this.quantitySpecified = true;
                }
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool QuantitySpecified
        {
            get
            {
                return this.quantitySpecified;
            }
            set
            {
                if ((this.quantitySpecified != value))
                {
                    this.quantitySpecified = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "requiredDateTime")]
        public DateTime RequiredDateTime
        {
            get
            {
                return this.requiredDateTime;
            }
            set
            {
                if ((this.requiredDateTime != value))
                {
                    this.requiredDateTime = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "action")]
        public OrderItemAction Action
        {
            get
            {
                return this.action;
            }
            set
            {
                if ((this.action != value))
                {
                    this.action = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "identifier", Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Agreements")]
        public OrderItemIdentifier Identifier
        {
            get
            {
                return this.identifier;
            }
            set
            {
                if ((this.identifier != value))
                {
                    this.identifier = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "totalPrice")]
        public Money TotalPrice
        {
            get
            {
                return this.totalPrice;
            }
            set
            {
                if ((this.totalPrice != value))
                {
                    this.totalPrice = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute("alternativeIdentifer", IsNullable = true)]
        public GlobalIdentifier[] AlternativeIdentifer
        {
            get
            {
                return this.alternativeIdentifer;
            }
            set
            {
                if ((this.alternativeIdentifer != value))
                {
                    this.alternativeIdentifer = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "unitPrice")]
        public Money UnitPrice
        {
            get
            {
                return this.unitPrice;
            }
            set
            {
                if ((this.unitPrice != value))
                {
                    this.unitPrice = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "holdToTerm")]
        public Contract HoldToTerm
        {
            get
            {
                return this.holdToTerm;
            }
            set
            {
                if ((this.holdToTerm != value))
                {
                    this.holdToTerm = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "billingAccount")]
        public BillingAccount BillingAccount
        {
            get
            {
                return this.billingAccount;
            }
            set
            {
                if ((this.billingAccount != value))
                {
                    this.billingAccount = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute("orderItemAssociation")]
        public OrderItemAssociation[] OrderItemAssociation
        {
            get
            {
                return this.orderItemAssociation;
            }
            set
            {
                if ((this.orderItemAssociation != value))
                {
                    this.orderItemAssociation = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute("orderItemMembership")]
        public OrderItemMembership[] OrderItemMembership
        {
            get
            {
                return this.orderItemMembership;
            }
            set
            {
                if ((this.orderItemMembership != value))
                {
                    this.orderItemMembership = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "installation")]
        public Installation Installation
        {
            get
            {
                return this.installation;
            }
            set
            {
                if ((this.installation != value))
                {
                    this.installation = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "deliveryLocation")]
        public Location DeliveryLocation
        {
            get
            {
                return this.deliveryLocation;
            }
            set
            {
                if ((this.deliveryLocation != value))
                {
                    this.deliveryLocation = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute("specification", IsNullable = true)]
        public Specification[] Specification
        {
            get
            {
                return this.specification;
            }
            set
            {
                if ((this.specification != value))
                {
                    this.specification = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute("instance", IsNullable = true)]
        public Instance[] Instance
        {
            get
            {
                return this.instance;
            }
            set
            {
                if ((this.instance != value))
                {
                    this.instance = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute("activity")]
        public Activity[] Activity
        {
            get
            {
                return this.activity;
            }
            set
            {
                if ((this.activity != value))
                {
                    this.activity = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute("orderItemOverride")]
        public OrderItemOverride[] OrderItemOverride
        {
            get
            {
                return this.orderItemOverride;
            }
            set
            {
                if ((this.orderItemOverride != value))
                {
                    this.orderItemOverride = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute("orderItemSplitCharging")]
        public OrderItemSplitCharging[] OrderItemSplitCharging
        {
            get
            {
                return this.orderItemSplitCharging;
            }
            set
            {
                if ((this.orderItemSplitCharging != value))
                {
                    this.orderItemSplitCharging = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "status", Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Agreements")]
        public string Status
        {
            get
            {
                return this.status;
            }
            set
            {
                if ((this.status != value))
                {
                    this.status = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute("note")]
        public Note[] Note
        {
            get
            {
                return this.note;
            }
            set
            {
                if ((this.note != value))
                {
                    this.note = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "error", Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Agreements")]
        public Error Error
        {
            get
            {
                return this.error;
            }
            set
            {
                if ((this.error != value))
                {
                    this.error = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "fromReference")]
        public string FromReference
        {
            get
            {
                return this.fromReference;
            }
            set
            {
                if ((this.fromReference != value))
                {
                    this.fromReference = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "toReference")]
        public string ToReference
        {
            get
            {
                return this.toReference;
            }
            set
            {
                if ((this.toReference != value))
                {
                    this.toReference = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Agreements", TypeName = "OrderItemAction")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class OrderItemAction
    {

        /// <remarks/>
        private string code;

        /// <remarks/>
        private string reason;

        public OrderItemAction()
        {
        }

        public OrderItemAction(string code, string reason)
        {
            this.code = code;
            this.reason = reason;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "code")]
        public string Code
        {
            get
            {
                return this.code;
            }
            set
            {
                if ((this.code != value))
                {
                    this.code = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "reason")]
        public string Reason
        {
            get
            {
                return this.reason;
            }
            set
            {
                if ((this.reason != value))
                {
                    this.reason = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Agreements", TypeName = "codeOrderItemActionEnum")]
    public enum codeOrderItemActionEnum
    {

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute(Name = "Create")]
        Create,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute(Name = "Amend")]
        Amend,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute(Name = "Cancel")]
        Cancel,
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Agreements", TypeName = "OrderItemIdentifier")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class OrderItemIdentifier
    {

        /// <remarks/>
        private string id;

        public OrderItemIdentifier()
        {
        }

        public OrderItemIdentifier(string id)
        {
            this.id = id;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "id", Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Agreements")]
        public string Id
        {
            get
            {
                return this.id;
            }
            set
            {
                if ((this.id != value))
                {
                    this.id = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Agreements", TypeName = "Contract")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class Contract
    {

        /// <remarks/>
        private string reference;

        /// <remarks/>
        private string dateValidFrom;

        /// <remarks/>
        private string dateValidTo;

        /// <remarks/>
        private Party[] holderParty;

        /// <remarks/>
        private ContractTerm contractTerm;

        public Contract()
        {
        }

        public Contract(string reference, string dateValidFrom, string dateValidTo, Party[] holderParty, ContractTerm contractTerm)
        {
            this.reference = reference;
            this.dateValidFrom = dateValidFrom;
            this.dateValidTo = dateValidTo;
            this.holderParty = holderParty;
            this.contractTerm = contractTerm;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "reference")]
        public string Reference
        {
            get
            {
                return this.reference;
            }
            set
            {
                if ((this.reference != value))
                {
                    this.reference = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "dateValidFrom")]
        public string DateValidFrom
        {
            get
            {
                return this.dateValidFrom;
            }
            set
            {
                if ((this.dateValidFrom != value))
                {
                    this.dateValidFrom = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "dateValidTo")]
        public string DateValidTo
        {
            get
            {
                return this.dateValidTo;
            }
            set
            {
                if ((this.dateValidTo != value))
                {
                    this.dateValidTo = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute("holderParty", IsNullable = true)]
        public Party[] HolderParty
        {
            get
            {
                return this.holderParty;
            }
            set
            {
                if ((this.holderParty != value))
                {
                    this.holderParty = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "contractTerm")]
        public ContractTerm ContractTerm
        {
            get
            {
                return this.contractTerm;
            }
            set
            {
                if ((this.contractTerm != value))
                {
                    this.contractTerm = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Agreements", TypeName = "ContractTerm")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class ContractTerm
    {

        /// <remarks/>
        private ContractAction contractAction;

        /// <remarks/>
        private System.Nullable<bool> enforce;

        /// <remarks/>
        private bool enforceSpecified;

        /// <remarks/>
        private Duration term;

        /// <remarks/>
        private ContractType type;

        public ContractTerm()
        {
        }

        public ContractTerm(ContractAction contractAction, System.Nullable<bool> enforce, bool enforceSpecified, Duration term, ContractType type)
        {
            this.contractAction = contractAction;
            this.enforce = enforce;
            this.enforceSpecified = enforceSpecified;
            this.term = term;
            this.type = type;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "contractAction")]
        public ContractAction ContractAction
        {
            get
            {
                return this.contractAction;
            }
            set
            {
                if ((this.contractAction != value))
                {
                    this.contractAction = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "enforce")]
        public System.Nullable<bool> Enforce
        {
            get
            {
                return this.enforce;
            }
            set
            {
                if ((this.enforce != value))
                {
                    this.enforce = value;
                    this.enforceSpecified = true;
                }
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool EnforceSpecified
        {
            get
            {
                return this.enforceSpecified;
            }
            set
            {
                if ((this.enforceSpecified != value))
                {
                    this.enforceSpecified = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "term")]
        public Duration Term
        {
            get
            {
                return this.term;
            }
            set
            {
                if ((this.term != value))
                {
                    this.term = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "type")]
        public ContractType Type
        {
            get
            {
                return this.type;
            }
            set
            {
                if ((this.type != value))
                {
                    this.type = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Agreements", TypeName = "ContractAction")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class ContractAction
    {

        /// <remarks/>
        private string code;

        public ContractAction()
        {
        }

        public ContractAction(string code)
        {
            this.code = code;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "code")]
        public string Code
        {
            get
            {
                return this.code;
            }
            set
            {
                if ((this.code != value))
                {
                    this.code = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/BaseTypes", TypeName = "Duration")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class Duration
    {

        /// <remarks/>
        private string unitsTimeUnit;

        /// <remarks/>
        private System.Nullable<int> amount;

        /// <remarks/>
        private bool amountSpecified;

        public Duration()
        {
        }

        public Duration(string unitsTimeUnit, System.Nullable<int> amount, bool amountSpecified)
        {
            this.unitsTimeUnit = unitsTimeUnit;
            this.amount = amount;
            this.amountSpecified = amountSpecified;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "unitsTimeUnit")]
        public string UnitsTimeUnit
        {
            get
            {
                return this.unitsTimeUnit;
            }
            set
            {
                if ((this.unitsTimeUnit != value))
                {
                    this.unitsTimeUnit = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "amount")]
        public System.Nullable<int> Amount
        {
            get
            {
                return this.amount;
            }
            set
            {
                if ((this.amount != value))
                {
                    this.amount = value;
                    this.amountSpecified = true;
                }
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool AmountSpecified
        {
            get
            {
                return this.amountSpecified;
            }
            set
            {
                if ((this.amountSpecified != value))
                {
                    this.amountSpecified = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Agreements", TypeName = "ContractType")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class ContractType
    {

        /// <remarks/>
        private System.Nullable<codeContractTypeEnum> code;

        public ContractType()
        {
        }

        public ContractType(System.Nullable<codeContractTypeEnum> code)
        {
            this.code = code;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "code")]
        public System.Nullable<codeContractTypeEnum> Code
        {
            get
            {
                return this.code;
            }
            set
            {
                if ((this.code != value))
                {
                    this.code = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Agreements", TypeName = "codeContractTypeEnum")]
    public enum codeContractTypeEnum
    {

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute(Name = "Create")]
        Create,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute(Name = "Modify")]
        Modify,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute(Name = "Terminate")]
        Terminate,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute(Name = "Fixed")]
        Fixed,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute(Name = "Variable")]
        Variable,
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Agreements", TypeName = "OrderItemAssociation")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class OrderItemAssociation
    {

        /// <remarks/>
        private OrderItem orderItem;

        /// <remarks/>
        private OrderIdentifier[] orderIdentifier;

        /// <remarks/>
        private OrderItemIdentifier orderItemIdentifier;

        public OrderItemAssociation()
        {
        }

        public OrderItemAssociation(OrderItem orderItem, OrderIdentifier[] orderIdentifier, OrderItemIdentifier orderItemIdentifier)
        {
            this.orderItem = orderItem;
            this.orderIdentifier = orderIdentifier;
            this.orderItemIdentifier = orderItemIdentifier;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "orderItem")]
        public OrderItem OrderItem
        {
            get
            {
                return this.orderItem;
            }
            set
            {
                if ((this.orderItem != value))
                {
                    this.orderItem = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute("orderIdentifier", IsNullable = true)]
        public OrderIdentifier[] OrderIdentifier
        {
            get
            {
                return this.orderIdentifier;
            }
            set
            {
                if ((this.orderIdentifier != value))
                {
                    this.orderIdentifier = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "orderItemIdentifier")]
        public OrderItemIdentifier OrderItemIdentifier
        {
            get
            {
                return this.orderItemIdentifier;
            }
            set
            {
                if ((this.orderItemIdentifier != value))
                {
                    this.orderItemIdentifier = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Agreements", TypeName = "OrderItemMembership")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class OrderItemMembership
    {

        /// <remarks/>
        private OrderItem orderItem;

        /// <remarks/>
        private OrderItemIdentifier[] id;

        public OrderItemMembership()
        {
        }

        public OrderItemMembership(OrderItem orderItem, OrderItemIdentifier[] id)
        {
            this.orderItem = orderItem;
            this.id = id;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "orderItem")]
        public OrderItem OrderItem
        {
            get
            {
                return this.orderItem;
            }
            set
            {
                if ((this.orderItem != value))
                {
                    this.orderItem = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute("id", IsNullable = true)]
        public OrderItemIdentifier[] Id
        {
            get
            {
                return this.id;
            }
            set
            {
                if ((this.id != value))
                {
                    this.id = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Agreements", TypeName = "Installation")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class Installation
    {

        /// <remarks/>
        private Location installationLocation;

        /// <remarks/>
        private InstallationDetails[] installationDetails;

        public Installation()
        {
        }

        public Installation(Location installationLocation, InstallationDetails[] installationDetails)
        {
            this.installationLocation = installationLocation;
            this.installationDetails = installationDetails;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "installationLocation")]
        public Location InstallationLocation
        {
            get
            {
                return this.installationLocation;
            }
            set
            {
                if ((this.installationLocation != value))
                {
                    this.installationLocation = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute("installationDetails", IsNullable = true)]
        public InstallationDetails[] InstallationDetails
        {
            get
            {
                return this.installationDetails;
            }
            set
            {
                if ((this.installationDetails != value))
                {
                    this.installationDetails = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Places", TypeName = "Location")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class Location
    {

        /// <remarks/>
        private string type;

        /// <remarks/>
        private Address address;

        /// <remarks/>
        private SubLocation[] subLocation;

        /// <remarks/>
        private System.Nullable<int> hazardCode;

        /// <remarks/>
        private bool hazardCodeSpecified;

        /// <remarks/>
        private string hazardText;

        /// <remarks/>
        private AddressIdentifier addressIdentifier;

        /// <remarks/>
        private ContactDetails contactDetails;

        public Location()
        {
        }

        public Location(string type, Address address, SubLocation[] subLocation, System.Nullable<int> hazardCode, bool hazardCodeSpecified, string hazardText, AddressIdentifier addressIdentifier, ContactDetails contactDetails)
        {
            this.type = type;
            this.address = address;
            this.subLocation = subLocation;
            this.hazardCode = hazardCode;
            this.hazardCodeSpecified = hazardCodeSpecified;
            this.hazardText = hazardText;
            this.addressIdentifier = addressIdentifier;
            this.contactDetails = contactDetails;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "type")]
        public string Type
        {
            get
            {
                return this.type;
            }
            set
            {
                if ((this.type != value))
                {
                    this.type = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "address")]
        public Address Address
        {
            get
            {
                return this.address;
            }
            set
            {
                if ((value == null))
                {
                    throw new System.ArgumentNullException("Address");
                }
                if ((this.address != value))
                {
                    this.address = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute("subLocation", IsNullable = true)]
        public SubLocation[] SubLocation
        {
            get
            {
                return this.subLocation;
            }
            set
            {
                if ((this.subLocation != value))
                {
                    this.subLocation = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "hazardCode")]
        public System.Nullable<int> HazardCode
        {
            get
            {
                return this.hazardCode;
            }
            set
            {
                if ((this.hazardCode != value))
                {
                    this.hazardCode = value;
                    this.hazardCodeSpecified = true;
                }
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool HazardCodeSpecified
        {
            get
            {
                return this.hazardCodeSpecified;
            }
            set
            {
                if ((this.hazardCodeSpecified != value))
                {
                    this.hazardCodeSpecified = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "hazardText")]
        public string HazardText
        {
            get
            {
                return this.hazardText;
            }
            set
            {
                if ((this.hazardText != value))
                {
                    this.hazardText = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "addressIdentifier")]
        public AddressIdentifier AddressIdentifier
        {
            get
            {
                return this.addressIdentifier;
            }
            set
            {
                if ((this.addressIdentifier != value))
                {
                    this.addressIdentifier = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "contactDetails")]
        public ContactDetails ContactDetails
        {
            get
            {
                return this.contactDetails;
            }
            set
            {
                if ((this.contactDetails != value))
                {
                    this.contactDetails = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Places", TypeName = "Address")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class Address
    {

        /// <remarks/>
        private System.Nullable<bool> status;

        /// <remarks/>
        private bool statusSpecified;

        /// <remarks/>
        private System.Nullable<bool> addressMatched;

        /// <remarks/>
        private bool addressMatchedSpecified;

        /// <remarks/>
        private string format;

        /// <remarks/>
        private UKPostalAddress address;

        /// <remarks/>
        private AddressIdentifier addressIdentifier;

        /// <remarks/>
        private Country country;

        public Address()
        {
        }

        public Address(System.Nullable<bool> status, bool statusSpecified, System.Nullable<bool> addressMatched, bool addressMatchedSpecified, string format, UKPostalAddress address, AddressIdentifier addressIdentifier, Country country)
        {
            this.status = status;
            this.statusSpecified = statusSpecified;
            this.addressMatched = addressMatched;
            this.addressMatchedSpecified = addressMatchedSpecified;
            this.format = format;
            this.address = address;
            this.addressIdentifier = addressIdentifier;
            this.country = country;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "status")]
        public System.Nullable<bool> Status
        {
            get
            {
                return this.status;
            }
            set
            {
                if ((this.status != value))
                {
                    this.status = value;
                    this.statusSpecified = true;
                }
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool StatusSpecified
        {
            get
            {
                return this.statusSpecified;
            }
            set
            {
                if ((this.statusSpecified != value))
                {
                    this.statusSpecified = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "addressMatched")]
        public System.Nullable<bool> AddressMatched
        {
            get
            {
                return this.addressMatched;
            }
            set
            {
                if ((this.addressMatched != value))
                {
                    this.addressMatched = value;
                    this.addressMatchedSpecified = true;
                }
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool AddressMatchedSpecified
        {
            get
            {
                return this.addressMatchedSpecified;
            }
            set
            {
                if ((this.addressMatchedSpecified != value))
                {
                    this.addressMatchedSpecified = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "format")]
        public string Format
        {
            get
            {
                return this.format;
            }
            set
            {
                if ((this.format != value))
                {
                    this.format = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "address")]
        public UKPostalAddress Address1
        {
            get
            {
                return this.address;
            }
            set
            {
                if ((this.address != value))
                {
                    this.address = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "addressIdentifier")]
        public AddressIdentifier AddressIdentifier
        {
            get
            {
                return this.addressIdentifier;
            }
            set
            {
                if ((value == null))
                {
                    throw new System.ArgumentNullException("AddressIdentifier");
                }
                if ((this.addressIdentifier != value))
                {
                    this.addressIdentifier = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "country")]
        public Country Country
        {
            get
            {
                return this.country;
            }
            set
            {
                if ((this.country != value))
                {
                    this.country = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Places", TypeName = "UKPostalAddress")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class UKPostalAddress
    {

        /// <remarks/>
        private string subBuildingName;

        /// <remarks/>
        private string buildingName;

        /// <remarks/>
        private string buildingNumber;

        /// <remarks/>
        private string dependentThoroughfareName;

        /// <remarks/>
        private string dependentThoroughfareDescriptor;

        /// <remarks/>
        private string thoroughfareName;

        /// <remarks/>
        private string thoroughfareDescriptor;

        /// <remarks/>
        private string doubleDependentLocality;

        /// <remarks/>
        private string dependentLocality;

        /// <remarks/>
        private string postTown;

        /// <remarks/>
        private string county;

        /// <remarks/>
        private string pOBoxNumber;

        /// <remarks/>
        private string postalInCode;

        /// <remarks/>
        private string postalOutCode;

        /// <remarks/>
        private string cherishedName;

        /// <remarks/>
        private string thoroughfareNumber;

        public UKPostalAddress()
        {
        }

        public UKPostalAddress(
                    string subBuildingName,
                    string buildingName,
                    string buildingNumber,
                    string dependentThoroughfareName,
                    string dependentThoroughfareDescriptor,
                    string thoroughfareName,
                    string thoroughfareDescriptor,
                    string doubleDependentLocality,
                    string dependentLocality,
                    string postTown,
                    string county,
                    string pOBoxNumber,
                    string postalInCode,
                    string postalOutCode,
                    string cherishedName,
                    string thoroughfareNumber)
        {
            this.subBuildingName = subBuildingName;
            this.buildingName = buildingName;
            this.buildingNumber = buildingNumber;
            this.dependentThoroughfareName = dependentThoroughfareName;
            this.dependentThoroughfareDescriptor = dependentThoroughfareDescriptor;
            this.thoroughfareName = thoroughfareName;
            this.thoroughfareDescriptor = thoroughfareDescriptor;
            this.doubleDependentLocality = doubleDependentLocality;
            this.dependentLocality = dependentLocality;
            this.postTown = postTown;
            this.county = county;
            this.pOBoxNumber = pOBoxNumber;
            this.postalInCode = postalInCode;
            this.postalOutCode = postalOutCode;
            this.cherishedName = cherishedName;
            this.thoroughfareNumber = thoroughfareNumber;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "subBuildingName")]
        public string SubBuildingName
        {
            get
            {
                return this.subBuildingName;
            }
            set
            {
                if ((this.subBuildingName != value))
                {
                    this.subBuildingName = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "buildingName")]
        public string BuildingName
        {
            get
            {
                return this.buildingName;
            }
            set
            {
                if ((this.buildingName != value))
                {
                    this.buildingName = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "buildingNumber")]
        public string BuildingNumber
        {
            get
            {
                return this.buildingNumber;
            }
            set
            {
                if ((this.buildingNumber != value))
                {
                    this.buildingNumber = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "dependentThoroughfareName")]
        public string DependentThoroughfareName
        {
            get
            {
                return this.dependentThoroughfareName;
            }
            set
            {
                if ((this.dependentThoroughfareName != value))
                {
                    this.dependentThoroughfareName = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "dependentThoroughfareDescriptor")]
        public string DependentThoroughfareDescriptor
        {
            get
            {
                return this.dependentThoroughfareDescriptor;
            }
            set
            {
                if ((this.dependentThoroughfareDescriptor != value))
                {
                    this.dependentThoroughfareDescriptor = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "thoroughfareName")]
        public string ThoroughfareName
        {
            get
            {
                return this.thoroughfareName;
            }
            set
            {
                if ((this.thoroughfareName != value))
                {
                    this.thoroughfareName = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "thoroughfareDescriptor")]
        public string ThoroughfareDescriptor
        {
            get
            {
                return this.thoroughfareDescriptor;
            }
            set
            {
                if ((this.thoroughfareDescriptor != value))
                {
                    this.thoroughfareDescriptor = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "doubleDependentLocality")]
        public string DoubleDependentLocality
        {
            get
            {
                return this.doubleDependentLocality;
            }
            set
            {
                if ((this.doubleDependentLocality != value))
                {
                    this.doubleDependentLocality = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "dependentLocality")]
        public string DependentLocality
        {
            get
            {
                return this.dependentLocality;
            }
            set
            {
                if ((this.dependentLocality != value))
                {
                    this.dependentLocality = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "postTown")]
        public string PostTown
        {
            get
            {
                return this.postTown;
            }
            set
            {
                if ((this.postTown != value))
                {
                    this.postTown = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "county")]
        public string County
        {
            get
            {
                return this.county;
            }
            set
            {
                if ((this.county != value))
                {
                    this.county = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "pOBoxNumber")]
        public string POBoxNumber
        {
            get
            {
                return this.pOBoxNumber;
            }
            set
            {
                if ((this.pOBoxNumber != value))
                {
                    this.pOBoxNumber = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "postalInCode")]
        public string PostalInCode
        {
            get
            {
                return this.postalInCode;
            }
            set
            {
                if ((this.postalInCode != value))
                {
                    this.postalInCode = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "postalOutCode")]
        public string PostalOutCode
        {
            get
            {
                return this.postalOutCode;
            }
            set
            {
                if ((this.postalOutCode != value))
                {
                    this.postalOutCode = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "cherishedName")]
        public string CherishedName
        {
            get
            {
                return this.cherishedName;
            }
            set
            {
                if ((this.cherishedName != value))
                {
                    this.cherishedName = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "thoroughfareNumber")]
        public string ThoroughfareNumber
        {
            get
            {
                return this.thoroughfareNumber;
            }
            set
            {
                if ((this.thoroughfareNumber != value))
                {
                    this.thoroughfareNumber = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Places", TypeName = "AddressIdentifier")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class AddressIdentifier
    {

        /// <remarks/>
        private string id;

        /// <remarks/>
        private string name;

        /// <remarks/>
        private string value;

        public AddressIdentifier()
        {
        }

        public AddressIdentifier(string id, string name, string value)
        {
            this.id = id;
            this.name = name;
            this.value = value;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "id")]
        public string Id
        {
            get
            {
                return this.id;
            }
            set
            {
                if ((this.id != value))
                {
                    this.id = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "name")]
        public string Name
        {
            get
            {
                return this.name;
            }
            set
            {
                if ((this.name != value))
                {
                    this.name = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "value")]
        public string Value
        {
            get
            {
                return this.value;
            }
            set
            {
                if ((this.value != value))
                {
                    this.value = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Places", TypeName = "Country")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class Country
    {

        /// <remarks/>
        private string name;

        public Country()
        {
        }

        public Country(string name)
        {
            this.name = name;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "name")]
        public string Name
        {
            get
            {
                return this.name;
            }
            set
            {
                if ((this.name != value))
                {
                    this.name = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Places", TypeName = "SubLocation")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class SubLocation
    {

        /// <remarks/>
        private string type;

        /// <remarks/>
        private string description;

        /// <remarks/>
        private string name;

        public SubLocation()
        {
        }

        public SubLocation(string type, string description, string name)
        {
            this.type = type;
            this.description = description;
            this.name = name;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "type")]
        public string Type
        {
            get
            {
                return this.type;
            }
            set
            {
                if ((this.type != value))
                {
                    this.type = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "description")]
        public string Description
        {
            get
            {
                return this.description;
            }
            set
            {
                if ((this.description != value))
                {
                    this.description = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "name")]
        public string Name
        {
            get
            {
                return this.name;
            }
            set
            {
                if ((this.name != value))
                {
                    this.name = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Agreements", TypeName = "InstallationDetails")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class InstallationDetails
    {

        /// <remarks/>
        private string name;

        /// <remarks/>
        private string value;

        public InstallationDetails()
        {
        }

        public InstallationDetails(string name, string value)
        {
            this.name = name;
            this.value = value;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "name")]
        public string Name
        {
            get
            {
                return this.name;
            }
            set
            {
                if ((this.name != value))
                {
                    this.name = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "value")]
        public string Value
        {
            get
            {
                return this.value;
            }
            set
            {
                if ((this.value != value))
                {
                    this.value = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/CoreClasses/Specifi" +
        "cations", TypeName = "Specification")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class Specification
    {

        /// <remarks/>
        private SpecificationIdentifier identifier;

        /// <remarks/>
        private System.Nullable<int>[] name;

        /// <remarks/>
        private System.Nullable<int> description;

        /// <remarks/>
        private bool descriptionSpecified;

        /// <remarks/>
        private SpecCategory specCategory;

        /// <remarks/>
        private string type;

        public Specification()
        {
        }

        public Specification(SpecificationIdentifier identifier, System.Nullable<int>[] name, System.Nullable<int> description, bool descriptionSpecified, SpecCategory specCategory, string type)
        {
            this.identifier = identifier;
            this.name = name;
            this.description = description;
            this.descriptionSpecified = descriptionSpecified;
            this.specCategory = specCategory;
            this.type = type;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "identifier")]
        public SpecificationIdentifier Identifier
        {
            get
            {
                return this.identifier;
            }
            set
            {
                if ((this.identifier != value))
                {
                    this.identifier = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute("name")]
        public System.Nullable<int>[] Name
        {
            get
            {
                return this.name;
            }
            set
            {
                if ((this.name != value))
                {
                    this.name = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "description")]
        public System.Nullable<int> Description
        {
            get
            {
                return this.description;
            }
            set
            {
                if ((this.description != value))
                {
                    this.description = value;
                    this.descriptionSpecified = true;
                }
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool DescriptionSpecified
        {
            get
            {
                return this.descriptionSpecified;
            }
            set
            {
                if ((this.descriptionSpecified != value))
                {
                    this.descriptionSpecified = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "specCategory")]
        public SpecCategory SpecCategory
        {
            get
            {
                return this.specCategory;
            }
            set
            {
                if ((this.specCategory != value))
                {
                    this.specCategory = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "type")]
        public string Type
        {
            get
            {
                return this.type;
            }
            set
            {
                if ((this.type != value))
                {
                    this.type = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/CoreClasses/Specifi" +
        "cations", TypeName = "SpecificationIdentifier")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class SpecificationIdentifier : PrimaryIdentifier
    {

        /// <remarks/>
        private string name;
        private string value1;


        public SpecificationIdentifier()
        {

        }

        public SpecificationIdentifier(string name)
        {
            this.name = name;
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "name")]
        public string Name
        {
            get
            {
                return this.name;
            }
            set
            {
                if ((this.name != value))
                {
                    this.name = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "value")]
        public new string Value1
        {
            get
            {
                return this.value1;
            }
            set
            {
                if ((this.value1 != value))
                {
                    this.value1 = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(SpecificationIdentifier))]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Identification", TypeName = "PrimaryIdentifier")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class PrimaryIdentifier
    {

        /// <remarks/>
        private string value;

        public PrimaryIdentifier()
        {
        }

        public PrimaryIdentifier(string value)
        {
            this.value = value;
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "Value")]
        public string Value
        {
            get
            {
                return this.value;
            }
            set
            {
                if ((this.value != value))
                {
                    this.value = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/CoreClasses/Specifi" +
        "cations", TypeName = "SpecCategory")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class SpecCategory
    {

        /// <remarks/>
        private string name;

        public SpecCategory()
        {
        }

        public SpecCategory(string name)
        {
            this.name = name;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "name")]
        public string Name
        {
            get
            {
                return this.name;
            }
            set
            {
                if ((this.name != value))
                {
                    this.name = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/CoreClasses/Specifi" +
        "cations", TypeName = "Instance")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class Instance
    {

        /// <remarks/>
        private InstanceCharacteristic[] instanceCharacteristic;

        /// <remarks/>
        private Specification specification1;

        /// <remarks/>
        private string status;

        /// <remarks/>
        private string name;

        /// <remarks/>
        private InstanceAction instanceAction;

        /// <remarks/>
        private InstanceAssociation[] instanceAssociation;

        /// <remarks/>
        private InstanceIdentifier instanceIdentifier;

        /// <remarks/>
        private DateTime startDateTime;

        /// <remarks/>
        private DateTime endDateTime;

        public Instance()
        {
        }

        public Instance(InstanceCharacteristic[] instanceCharacteristic, Specification specification1, string status, string name, InstanceAction instanceAction, InstanceAssociation[] instanceAssociation, InstanceIdentifier instanceIdentifier, DateTime startDateTime, DateTime endDateTime)
        {
            this.instanceCharacteristic = instanceCharacteristic;
            this.specification1 = specification1;
            this.status = status;
            this.name = name;
            this.instanceAction = instanceAction;
            this.instanceAssociation = instanceAssociation;
            this.instanceIdentifier = instanceIdentifier;
            this.startDateTime = startDateTime;
            this.endDateTime = endDateTime;
        }

        [System.Xml.Serialization.XmlElementAttribute("instanceCharacteristic")]
        public InstanceCharacteristic[] InstanceCharacteristic
        {
            get
            {
                return this.instanceCharacteristic;
            }
            set
            {
                if ((this.instanceCharacteristic != value))
                {
                    this.instanceCharacteristic = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "specification1")]
        public Specification Specification1
        {
            get
            {
                return this.specification1;
            }
            set
            {
                if ((this.specification1 != value))
                {
                    this.specification1 = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "status")]
        public string Status
        {
            get
            {
                return this.status;
            }
            set
            {
                if ((this.status != value))
                {
                    this.status = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "name")]
        public string Name
        {
            get
            {
                return this.name;
            }
            set
            {
                if ((this.name != value))
                {
                    this.name = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "instanceAction")]
        public InstanceAction InstanceAction
        {
            get
            {
                return this.instanceAction;
            }
            set
            {
                if ((this.instanceAction != value))
                {
                    this.instanceAction = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute("instanceAssociation", IsNullable = true)]
        public InstanceAssociation[] InstanceAssociation
        {
            get
            {
                return this.instanceAssociation;
            }
            set
            {
                if ((this.instanceAssociation != value))
                {
                    this.instanceAssociation = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "instanceIdentifier")]
        public InstanceIdentifier InstanceIdentifier
        {
            get
            {
                return this.instanceIdentifier;
            }
            set
            {
                if ((this.instanceIdentifier != value))
                {
                    this.instanceIdentifier = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "startDateTime")]
        public DateTime StartDateTime
        {
            get
            {
                return this.startDateTime;
            }
            set
            {
                if ((this.startDateTime != value))
                {
                    this.startDateTime = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "endDateTime")]
        public DateTime EndDateTime
        {
            get
            {
                return this.endDateTime;
            }
            set
            {
                if ((this.endDateTime != value))
                {
                    this.endDateTime = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/CoreClasses/Specifi" +
        "cations", TypeName = "InstanceCharacteristic")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class InstanceCharacteristic
    {

        /// <remarks/>
        private string name;

        /// <remarks/>
        private string value;

        /// <remarks/>
        private string previousValue;

        /// <remarks/>
        private string previousName;

        /// <remarks/>
        private InstanceAction action;

        public InstanceCharacteristic()
        {
        }

        public InstanceCharacteristic(string name, string value, string previousValue, string previousName, InstanceAction action)
        {
            this.name = name;
            this.value = value;
            this.previousValue = previousValue;
            this.previousName = previousName;
            this.action = action;
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "name")]
        public string Name
        {
            get
            {
                return this.name;
            }
            set
            {
                if ((this.name != value))
                {
                    this.name = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "value")]
        public string Value
        {
            get
            {
                return this.value;
            }
            set
            {
                if ((this.value != value))
                {
                    this.value = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "previousValue")]
        public string PreviousValue
        {
            get
            {
                return this.previousValue;
            }
            set
            {
                if ((this.previousValue != value))
                {
                    this.previousValue = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "previousName")]
        public string PreviousName
        {
            get
            {
                return this.previousName;
            }
            set
            {
                if ((this.previousName != value))
                {
                    this.previousName = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "action")]
        public InstanceAction Action
        {
            get
            {
                return this.action;
            }
            set
            {
                if ((this.action != value))
                {
                    this.action = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/CoreClasses/Specifi" +
        "cations", TypeName = "InstanceAction")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class InstanceAction
    {

        /// <remarks/>
        private string code;

        public InstanceAction()
        {
        }

        public InstanceAction(string code)
        {
            this.code = code;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "code")]
        public string Code
        {
            get
            {
                return this.code;
            }
            set
            {
                if ((this.code != value))
                {
                    this.code = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/CoreClasses/Specifi" +
        "cations", TypeName = "codeInstanceActionEnum")]
    public enum codeInstanceActionEnum
    {

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute(Name = "Provide")]
        Provide,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute(Name = "Cease")]
        Cease,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute(Name = "Modify")]
        Modify,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute(Name = "Suspend")]
        Suspend,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute(Name = "Resume")]
        Resume,
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/CoreClasses/Specifi" +
        "cations", TypeName = "InstanceAssociation")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class InstanceAssociation
    {

        /// <remarks/>
        private string type;

        /// <remarks/>
        private InstanceIdentifier instance;

        public InstanceAssociation()
        {
        }

        public InstanceAssociation(string type, InstanceIdentifier instance)
        {
            this.type = type;
            this.instance = instance;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "type")]
        public string Type
        {
            get
            {
                return this.type;
            }
            set
            {
                if ((this.type != value))
                {
                    this.type = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "instance")]
        public InstanceIdentifier Instance
        {
            get
            {
                return this.instance;
            }
            set
            {
                if ((this.instance != value))
                {
                    this.instance = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/CoreClasses/Specifi" +
        "cations", TypeName = "InstanceIdentifier")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class InstanceIdentifier
    {

        /// <remarks/>
        private string value;

        /// <remarks/>
        private string name;

        public InstanceIdentifier()
        {
        }

        public InstanceIdentifier(string value, string name)
        {
            this.value = value;
            this.name = name;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "value")]
        public string Value
        {
            get
            {
                return this.value;
            }
            set
            {
                if ((this.value != value))
                {
                    this.value = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "name")]
        public string Name
        {
            get
            {
                return this.name;
            }
            set
            {
                if ((this.name != value))
                {
                    this.name = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Activities", TypeName = "Activity")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class Activity
    {

        /// <remarks/>
        private ActivityIdentifier id;

        /// <remarks/>
        private string name;

        /// <remarks/>
        private ActivitySpecification activitySpecification;

        /// <remarks/>
        private Appointment appointment;

        /// <remarks/>
        private ActivityStatus status;

        public Activity()
        {
        }

        public Activity(ActivityIdentifier id, string name, ActivitySpecification activitySpecification, Appointment appointment, ActivityStatus status)
        {
            this.id = id;
            this.name = name;
            this.activitySpecification = activitySpecification;
            this.appointment = appointment;
            this.status = status;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "id")]
        public ActivityIdentifier Id
        {
            get
            {
                return this.id;
            }
            set
            {
                if ((this.id != value))
                {
                    this.id = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "name")]
        public string Name
        {
            get
            {
                return this.name;
            }
            set
            {
                if ((this.name != value))
                {
                    this.name = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "activitySpecification")]
        public ActivitySpecification ActivitySpecification
        {
            get
            {
                return this.activitySpecification;
            }
            set
            {
                if ((this.activitySpecification != value))
                {
                    this.activitySpecification = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "appointment")]
        public Appointment Appointment
        {
            get
            {
                return this.appointment;
            }
            set
            {
                if ((this.appointment != value))
                {
                    this.appointment = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "status")]
        public ActivityStatus Status
        {
            get
            {
                return this.status;
            }
            set
            {
                if ((this.status != value))
                {
                    this.status = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Activities", TypeName = "ActivityIdentifier")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class ActivityIdentifier
    {

        /// <remarks/>
        private string value;

        public ActivityIdentifier()
        {
        }

        public ActivityIdentifier(string value)
        {
            this.value = value;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "value")]
        public string Value
        {
            get
            {
                return this.value;
            }
            set
            {
                if ((this.value != value))
                {
                    this.value = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Activities", TypeName = "ActivitySpecification")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class ActivitySpecification
    {

        /// <remarks/>
        private string id;

        /// <remarks/>
        private string name;

        public ActivitySpecification()
        {
        }

        public ActivitySpecification(string id, string name)
        {
            this.id = id;
            this.name = name;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "id")]
        public string Id
        {
            get
            {
                return this.id;
            }
            set
            {
                if ((this.id != value))
                {
                    this.id = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "name")]
        public string Name
        {
            get
            {
                return this.name;
            }
            set
            {
                if ((this.name != value))
                {
                    this.name = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Scheduling", TypeName = "Appointment")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class Appointment
    {

        /// <remarks/>
        private string duration;

        /// <remarks/>
        private string startDateTime;

        /// <remarks/>
        private AppointmentWindow appointmentWindow;

        /// <remarks/>
        private Party[] party;

        /// <remarks/>
        private Location location;

        /// <remarks/>
        private string appointmentId;

        /// <remarks/>
        private AppointmentSlot appointmentSlot;

        public Appointment()
        {
        }

        public Appointment(string duration, string startDateTime, AppointmentWindow appointmentWindow, Party[] party, Location location, string appointmentId, AppointmentSlot appointmentSlot)
        {
            this.duration = duration;
            this.startDateTime = startDateTime;
            this.appointmentWindow = appointmentWindow;
            this.party = party;
            this.location = location;
            this.appointmentId = appointmentId;
            this.appointmentSlot = appointmentSlot;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "duration")]
        public string Duration
        {
            get
            {
                return this.duration;
            }
            set
            {
                if ((this.duration != value))
                {
                    this.duration = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "startDateTime")]
        public string StartDateTime
        {
            get
            {
                return this.startDateTime;
            }
            set
            {
                if ((this.startDateTime != value))
                {
                    this.startDateTime = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "appointmentWindow")]
        public AppointmentWindow AppointmentWindow
        {
            get
            {
                return this.appointmentWindow;
            }
            set
            {
                if ((this.appointmentWindow != value))
                {
                    this.appointmentWindow = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute("party")]
        public Party[] Party
        {
            get
            {
                return this.party;
            }
            set
            {
                if ((value == null))
                {
                    throw new System.ArgumentNullException("Party");
                }
                if ((this.party != value))
                {
                    this.party = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "location")]
        public Location Location
        {
            get
            {
                return this.location;
            }
            set
            {
                if ((this.location != value))
                {
                    this.location = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "appointmentId")]
        public string AppointmentId
        {
            get
            {
                return this.appointmentId;
            }
            set
            {
                if ((this.appointmentId != value))
                {
                    this.appointmentId = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "appointmentSlot")]
        public AppointmentSlot AppointmentSlot
        {
            get
            {
                return this.appointmentSlot;
            }
            set
            {
                if ((this.appointmentSlot != value))
                {
                    this.appointmentSlot = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Scheduling", TypeName = "AppointmentWindow")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class AppointmentWindow
    {

        /// <remarks/>
        private System.Nullable<int> earliestDate;

        /// <remarks/>
        private System.Nullable<int> latestDateTime;

        public AppointmentWindow()
        {
        }

        public AppointmentWindow(System.Nullable<int> earliestDate, System.Nullable<int> latestDateTime)
        {
            this.earliestDate = earliestDate;
            this.latestDateTime = latestDateTime;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "earliestDate")]
        public System.Nullable<int> EarliestDate
        {
            get
            {
                return this.earliestDate;
            }
            set
            {
                if ((this.earliestDate != value))
                {
                    this.earliestDate = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "latestDateTime")]
        public System.Nullable<int> LatestDateTime
        {
            get
            {
                return this.latestDateTime;
            }
            set
            {
                if ((this.latestDateTime != value))
                {
                    this.latestDateTime = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Scheduling", TypeName = "AppointmentSlot")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class AppointmentSlot
    {

        /// <remarks/>
        private System.Nullable<int> startDate;

        /// <remarks/>
        private bool startDateSpecified;

        /// <remarks/>
        private System.Nullable<int> endDate;

        /// <remarks/>
        private bool endDateSpecified;

        /// <remarks/>
        private System.Nullable<int> status;

        /// <remarks/>
        private bool statusSpecified;

        /// <remarks/>
        private System.Nullable<int> duration;

        /// <remarks/>
        private bool durationSpecified;

        /// <remarks/>
        private string type;

        /// <remarks/>
        private string value;

        public AppointmentSlot()
        {
        }

        public AppointmentSlot(System.Nullable<int> startDate, bool startDateSpecified, System.Nullable<int> endDate, bool endDateSpecified, System.Nullable<int> status, bool statusSpecified, System.Nullable<int> duration, bool durationSpecified, string type, string value)
        {
            this.startDate = startDate;
            this.startDateSpecified = startDateSpecified;
            this.endDate = endDate;
            this.endDateSpecified = endDateSpecified;
            this.status = status;
            this.statusSpecified = statusSpecified;
            this.duration = duration;
            this.durationSpecified = durationSpecified;
            this.type = type;
            this.value = value;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "startDate")]
        public System.Nullable<int> StartDate
        {
            get
            {
                return this.startDate;
            }
            set
            {
                if ((this.startDate != value))
                {
                    this.startDate = value;
                    this.startDateSpecified = true;
                }
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool StartDateSpecified
        {
            get
            {
                return this.startDateSpecified;
            }
            set
            {
                if ((this.startDateSpecified != value))
                {
                    this.startDateSpecified = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "endDate")]
        public System.Nullable<int> EndDate
        {
            get
            {
                return this.endDate;
            }
            set
            {
                if ((this.endDate != value))
                {
                    this.endDate = value;
                    this.endDateSpecified = true;
                }
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool EndDateSpecified
        {
            get
            {
                return this.endDateSpecified;
            }
            set
            {
                if ((this.endDateSpecified != value))
                {
                    this.endDateSpecified = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "status")]
        public System.Nullable<int> Status
        {
            get
            {
                return this.status;
            }
            set
            {
                if ((this.status != value))
                {
                    this.status = value;
                    this.statusSpecified = true;
                }
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool StatusSpecified
        {
            get
            {
                return this.statusSpecified;
            }
            set
            {
                if ((this.statusSpecified != value))
                {
                    this.statusSpecified = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "duration")]
        public System.Nullable<int> Duration
        {
            get
            {
                return this.duration;
            }
            set
            {
                if ((this.duration != value))
                {
                    this.duration = value;
                    this.durationSpecified = true;
                }
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool DurationSpecified
        {
            get
            {
                return this.durationSpecified;
            }
            set
            {
                if ((this.durationSpecified != value))
                {
                    this.durationSpecified = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "type")]
        public string Type
        {
            get
            {
                return this.type;
            }
            set
            {
                if ((this.type != value))
                {
                    this.type = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "value")]
        public string Value
        {
            get
            {
                return this.value;
            }
            set
            {
                if ((this.value != value))
                {
                    this.value = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Activities", TypeName = "ActivityStatus")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class ActivityStatus
    {

        /// <remarks/>
        private string value;

        public ActivityStatus()
        {
        }

        public ActivityStatus(string value)
        {
            this.value = value;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "value")]
        public string Value
        {
            get
            {
                return this.value;
            }
            set
            {
                if ((this.value != value))
                {
                    this.value = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Agreements", TypeName = "OrderItemOverride")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class OrderItemOverride
    {

        /// <remarks/>
        private OrderItemAction action;

        /// <remarks/>
        private DateTime startDate;

        /// <remarks/>
        private DateTime endDate;

        /// <remarks/>
        private ChargeDetails chargeDetails;

        /// <remarks/>
        private string overrideNotes;

        public OrderItemOverride()
        {
        }

        public OrderItemOverride(OrderItemAction action, DateTime startDate, DateTime endDate, ChargeDetails chargeDetails, string overrideNotes)
        {
            this.action = action;
            this.startDate = startDate;
            this.endDate = endDate;
            this.chargeDetails = chargeDetails;
            this.overrideNotes = overrideNotes;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "action")]
        public OrderItemAction Action
        {
            get
            {
                return this.action;
            }
            set
            {
                if ((this.action != value))
                {
                    this.action = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "startDate")]
        public DateTime StartDate
        {
            get
            {
                return this.startDate;
            }
            set
            {
                if ((this.startDate != value))
                {
                    this.startDate = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "endDate")]
        public DateTime EndDate
        {
            get
            {
                return this.endDate;
            }
            set
            {
                if ((this.endDate != value))
                {
                    this.endDate = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "chargeDetails")]
        public ChargeDetails ChargeDetails
        {
            get
            {
                return this.chargeDetails;
            }
            set
            {
                if ((this.chargeDetails != value))
                {
                    this.chargeDetails = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "overrideNotes")]
        public string OverrideNotes
        {
            get
            {
                return this.overrideNotes;
            }
            set
            {
                if ((this.overrideNotes != value))
                {
                    this.overrideNotes = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Finance", TypeName = "ChargeDetails")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class ChargeDetails
    {

        /// <remarks/>
        private ChargeType chargeType;

        /// <remarks/>
        private Money price;

        /// <remarks/>
        private string priceType;

        public ChargeDetails()
        {
        }

        public ChargeDetails(ChargeType chargeType, Money price, string priceType)
        {
            this.chargeType = chargeType;
            this.price = price;
            this.priceType = priceType;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "chargeType")]
        public ChargeType ChargeType
        {
            get
            {
                return this.chargeType;
            }
            set
            {
                if ((this.chargeType != value))
                {
                    this.chargeType = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "price")]
        public Money Price
        {
            get
            {
                return this.price;
            }
            set
            {
                if ((this.price != value))
                {
                    this.price = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "priceType")]
        public string PriceType
        {
            get
            {
                return this.priceType;
            }
            set
            {
                if ((this.priceType != value))
                {
                    this.priceType = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Finance", TypeName = "ChargeType")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class ChargeType
    {

        /// <remarks/>
        private string code;

        public ChargeType()
        {
        }

        public ChargeType(string code)
        {
            this.code = code;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "code")]
        public string Code
        {
            get
            {
                return this.code;
            }
            set
            {
                if ((this.code != value))
                {
                    this.code = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Agreements", TypeName = "OrderItemSplitCharging")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class OrderItemSplitCharging
    {

        /// <remarks/>
        private OrderItemAction action;

        /// <remarks/>
        private ChargeDetails chargeDetails;

        /// <remarks/>
        private System.Nullable<System.DateTime> endDateTime;

        /// <remarks/>
        private bool endDateTimeSpecified;

        /// <remarks/>
        private System.Nullable<System.DateTime> startDateTime;

        /// <remarks/>
        private bool startDateTimeSpecified;

        public OrderItemSplitCharging()
        {
        }

        public OrderItemSplitCharging(OrderItemAction action, ChargeDetails chargeDetails, System.Nullable<System.DateTime> endDateTime, bool endDateTimeSpecified, System.Nullable<System.DateTime> startDateTime, bool startDateTimeSpecified)
        {
            this.action = action;
            this.chargeDetails = chargeDetails;
            this.endDateTime = endDateTime;
            this.endDateTimeSpecified = endDateTimeSpecified;
            this.startDateTime = startDateTime;
            this.startDateTimeSpecified = startDateTimeSpecified;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "action")]
        public OrderItemAction Action
        {
            get
            {
                return this.action;
            }
            set
            {
                if ((this.action != value))
                {
                    this.action = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "chargeDetails")]
        public ChargeDetails ChargeDetails
        {
            get
            {
                return this.chargeDetails;
            }
            set
            {
                if ((this.chargeDetails != value))
                {
                    this.chargeDetails = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "endDateTime")]
        public System.Nullable<System.DateTime> EndDateTime
        {
            get
            {
                return this.endDateTime;
            }
            set
            {
                if ((this.endDateTime != value))
                {
                    this.endDateTime = value;
                    this.endDateTimeSpecified = true;
                }
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool EndDateTimeSpecified
        {
            get
            {
                return this.endDateTimeSpecified;
            }
            set
            {
                if ((this.endDateTimeSpecified != value))
                {
                    this.endDateTimeSpecified = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "startDateTime")]
        public System.Nullable<System.DateTime> StartDateTime
        {
            get
            {
                return this.startDateTime;
            }
            set
            {
                if ((this.startDateTime != value))
                {
                    this.startDateTime = value;
                    this.startDateTimeSpecified = true;
                }
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool StartDateTimeSpecified
        {
            get
            {
                return this.startDateTimeSpecified;
            }
            set
            {
                if ((this.startDateTimeSpecified != value))
                {
                    this.startDateTimeSpecified = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Agreements", TypeName = "Note")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class Note
    {

        /// <remarks/>
        private string noteType;

        /// <remarks/>
        private string text;

        public Note()
        {
        }

        public Note(string noteType, string text)
        {
            this.noteType = noteType;
            this.text = text;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "noteType")]
        public string NoteType
        {
            get
            {
                return this.noteType;
            }
            set
            {
                if ((this.noteType != value))
                {
                    this.noteType = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "text")]
        public string Text
        {
            get
            {
                return this.text;
            }
            set
            {
                if ((this.text != value))
                {
                    this.text = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Agreements", TypeName = "Error")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class Error
    {

        /// <remarks/>
        private string code;

        /// <remarks/>
        private string description;

        public Error()
        {
        }

        public Error(string code, string description)
        {
            this.code = code;
            this.description = description;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "code")]
        public string Code
        {
            get
            {
                return this.code;
            }
            set
            {
                if ((this.code != value))
                {
                    this.code = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "description")]
        public string Description
        {
            get
            {
                return this.description;
            }
            set
            {
                if ((this.description != value))
                {
                    this.description = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15", TypeName = "ActivityRequest")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class ActivityRequest
    {

        /// <remarks/>
        private StandardHeaderBlock standardHeader;

        /// <remarks/>
        private Order[] order;

        /// <remarks/>
        private ActivitySpecification activityType;

        public ActivityRequest()
        {
        }

        public ActivityRequest(StandardHeaderBlock standardHeader, Order[] order, ActivitySpecification activityType)
        {
            this.standardHeader = standardHeader;
            this.order = order;
            this.activityType = activityType;
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "standardHeader")]
        public StandardHeaderBlock StandardHeader
        {
            get
            {
                return this.standardHeader;
            }
            set
            {
                if ((value == null))
                {
                    throw new System.ArgumentNullException("StandardHeader");
                }
                if ((this.standardHeader != value))
                {
                    this.standardHeader = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute("order", IsNullable = true)]
        public Order[] Order
        {
            get
            {
                return this.order;
            }
            set
            {
                if ((this.order != value))
                {
                    this.order = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "activityType")]
        public ActivitySpecification ActivityType
        {
            get
            {
                return this.activityType;
            }
            set
            {
                if ((this.activityType != value))
                {
                    this.activityType = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15", TypeName = "ProvideActivityResponse")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class ProvideActivityResponse
    {

        /// <remarks/>
        private StandardHeaderBlock standardHeader;

        /// <remarks/>
        private OrderItemIdentifier orderItemIdentifier;

        public ProvideActivityResponse()
        {
        }

        public ProvideActivityResponse(StandardHeaderBlock standardHeader, OrderItemIdentifier orderItemIdentifier)
        {
            this.standardHeader = standardHeader;
            this.orderItemIdentifier = orderItemIdentifier;
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "standardHeader")]
        public StandardHeaderBlock StandardHeader
        {
            get
            {
                return this.standardHeader;
            }
            set
            {
                if ((value == null))
                {
                    throw new System.ArgumentNullException("StandardHeader");
                }
                if ((this.standardHeader != value))
                {
                    this.standardHeader = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "orderItemIdentifier")]
        public OrderItemIdentifier OrderItemIdentifier
        {
            get
            {
                return this.orderItemIdentifier;
            }
            set
            {
                if ((this.orderItemIdentifier != value))
                {
                    this.orderItemIdentifier = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Agreements", TypeName = "OrderItemStatus")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class OrderItemStatus
    {

        /// <remarks/>
        private string code;

        public OrderItemStatus()
        {
        }

        public OrderItemStatus(string code)
        {
            this.code = code;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "code")]
        public string Code
        {
            get
            {
                return this.code;
            }
            set
            {
                if ((this.code != value))
                {
                    this.code = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Agreements", TypeName = "OrderStatus")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class OrderStatus
    {

        /// <remarks/>
        private string code;

        public OrderStatus()
        {
        }

        public OrderStatus(string code)
        {
            this.code = code;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "code")]
        public string Code
        {
            get
            {
                return this.code;
            }
            set
            {
                if ((this.code != value))
                {
                    this.code = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15", TypeName = "OrderStatusResponse")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class OrderStatusResponse
    {

        /// <remarks/>
        private StandardHeaderBlock standardHeader;

        /// <remarks/>
        private OrderStatus status;

        /// <remarks/>
        private Activity activityStatus;

        /// <remarks/>
        private OrderItemStatus orderItemStatus;

        public OrderStatusResponse()
        {
        }

        public OrderStatusResponse(StandardHeaderBlock standardHeader, OrderStatus status, Activity activityStatus, OrderItemStatus orderItemStatus)
        {
            this.standardHeader = standardHeader;
            this.status = status;
            this.activityStatus = activityStatus;
            this.orderItemStatus = orderItemStatus;
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "standardHeader")]
        public StandardHeaderBlock StandardHeader
        {
            get
            {
                return this.standardHeader;
            }
            set
            {
                if ((value == null))
                {
                    throw new System.ArgumentNullException("StandardHeader");
                }
                if ((this.standardHeader != value))
                {
                    this.standardHeader = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "status")]
        public OrderStatus Status
        {
            get
            {
                return this.status;
            }
            set
            {
                if ((this.status != value))
                {
                    this.status = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "activityStatus")]
        public Activity ActivityStatus
        {
            get
            {
                return this.activityStatus;
            }
            set
            {
                if ((this.activityStatus != value))
                {
                    this.activityStatus = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "orderItemStatus")]
        public OrderItemStatus OrderItemStatus
        {
            get
            {
                return this.orderItemStatus;
            }
            set
            {
                if ((this.orderItemStatus != value))
                {
                    this.orderItemStatus = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15", TypeName = "OrderStatusRequest")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class OrderStatusRequest
    {

        /// <remarks/>
        private StandardHeaderBlock standardHeader;

        /// <remarks/>
        private OrderIdentifier orderId;

        public OrderStatusRequest()
        {
        }

        public OrderStatusRequest(StandardHeaderBlock standardHeader, OrderIdentifier orderId)
        {
            this.standardHeader = standardHeader;
            this.orderId = orderId;
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "standardHeader")]
        public StandardHeaderBlock StandardHeader
        {
            get
            {
                return this.standardHeader;
            }
            set
            {
                if ((value == null))
                {
                    throw new System.ArgumentNullException("StandardHeader");
                }
                if ((this.standardHeader != value))
                {
                    this.standardHeader = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "orderId")]
        public OrderIdentifier OrderId
        {
            get
            {
                return this.orderId;
            }
            set
            {
                if ((this.orderId != value))
                {
                    this.orderId = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15", TypeName = "QueryOrderResponse")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class QueryOrderResponse
    {

        /// <remarks/>
        private StandardHeaderBlock standardHeader;

        /// <remarks/>
        private Order order;

        public QueryOrderResponse()
        {
        }

        public QueryOrderResponse(StandardHeaderBlock standardHeader, Order order)
        {
            this.standardHeader = standardHeader;
            this.order = order;
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "standardHeader")]
        public StandardHeaderBlock StandardHeader
        {
            get
            {
                return this.standardHeader;
            }
            set
            {
                if ((value == null))
                {
                    throw new System.ArgumentNullException("StandardHeader");
                }
                if ((this.standardHeader != value))
                {
                    this.standardHeader = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "order")]
        public Order Order
        {
            get
            {
                return this.order;
            }
            set
            {
                if ((this.order != value))
                {
                    this.order = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15", TypeName = "QueryOrderRequest")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class QueryOrderRequest
    {

        /// <remarks/>
        private StandardHeaderBlock standardHeader;

        /// <remarks/>
        private OrderIdentifier orderIdentifier;

        public QueryOrderRequest()
        {
        }

        public QueryOrderRequest(StandardHeaderBlock standardHeader, OrderIdentifier orderIdentifier)
        {
            this.standardHeader = standardHeader;
            this.orderIdentifier = orderIdentifier;
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "standardHeader")]
        public StandardHeaderBlock StandardHeader
        {
            get
            {
                return this.standardHeader;
            }
            set
            {
                if ((value == null))
                {
                    throw new System.ArgumentNullException("StandardHeader");
                }
                if ((this.standardHeader != value))
                {
                    this.standardHeader = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "orderIdentifier")]
        public OrderIdentifier OrderIdentifier
        {
            get
            {
                return this.orderIdentifier;
            }
            set
            {
                if ((this.orderIdentifier != value))
                {
                    this.orderIdentifier = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15", TypeName = "FindOrderResponse")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class FindOrderResponse
    {

        /// <remarks/>
        private StandardHeaderBlock standardHeader;

        /// <remarks/>
        private OrderIdentifier orderIdentifier;

        public FindOrderResponse()
        {
        }

        public FindOrderResponse(StandardHeaderBlock standardHeader, OrderIdentifier orderIdentifier)
        {
            this.standardHeader = standardHeader;
            this.orderIdentifier = orderIdentifier;
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "standardHeader")]
        public StandardHeaderBlock StandardHeader
        {
            get
            {
                return this.standardHeader;
            }
            set
            {
                if ((value == null))
                {
                    throw new System.ArgumentNullException("StandardHeader");
                }
                if ((this.standardHeader != value))
                {
                    this.standardHeader = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "orderIdentifier")]
        public OrderIdentifier OrderIdentifier
        {
            get
            {
                return this.orderIdentifier;
            }
            set
            {
                if ((this.orderIdentifier != value))
                {
                    this.orderIdentifier = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Services", TypeName = "ServiceInstance")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class ServiceInstance
    {

        /// <remarks/>
        private string id;

        public ServiceInstance()
        {
        }

        public ServiceInstance(string id)
        {
            this.id = id;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "id")]
        public string Id
        {
            get
            {
                return this.id;
            }
            set
            {
                if ((this.id != value))
                {
                    this.id = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/BaseTypes", TypeName = "Date")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class Date
    {

        /// <remarks/>
        private string date;

        public Date()
        {
        }

        public Date(string date)
        {
            this.date = date;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "date")]
        public string Date1
        {
            get
            {
                return this.date;
            }
            set
            {
                if ((this.date != value))
                {
                    this.date = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15", TypeName = "FindOrderRequest")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class FindOrderRequest
    {

        /// <remarks/>
        private StandardHeaderBlock standardHeader;

        /// <remarks/>
        private Date date;

        /// <remarks/>
        private Appointment appointment;

        /// <remarks/>
        private BillingAccount billingAccount;

        /// <remarks/>
        private Order order;

        /// <remarks/>
        private OrderItem orderItem;

        /// <remarks/>
        private ContactDetails contactDetail;

        /// <remarks/>
        private ServiceInstance serviceId;

        public FindOrderRequest()
        {
        }

        public FindOrderRequest(StandardHeaderBlock standardHeader, Date date, Appointment appointment, BillingAccount billingAccount, Order order, OrderItem orderItem, ContactDetails contactDetail, ServiceInstance serviceId)
        {
            this.standardHeader = standardHeader;
            this.date = date;
            this.appointment = appointment;
            this.billingAccount = billingAccount;
            this.order = order;
            this.orderItem = orderItem;
            this.contactDetail = contactDetail;
            this.serviceId = serviceId;
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "standardHeader")]
        public StandardHeaderBlock StandardHeader
        {
            get
            {
                return this.standardHeader;
            }
            set
            {
                if ((value == null))
                {
                    throw new System.ArgumentNullException("StandardHeader");
                }
                if ((this.standardHeader != value))
                {
                    this.standardHeader = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "date")]
        public Date Date
        {
            get
            {
                return this.date;
            }
            set
            {
                if ((this.date != value))
                {
                    this.date = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "appointment")]
        public Appointment Appointment
        {
            get
            {
                return this.appointment;
            }
            set
            {
                if ((this.appointment != value))
                {
                    this.appointment = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "billingAccount")]
        public BillingAccount BillingAccount
        {
            get
            {
                return this.billingAccount;
            }
            set
            {
                if ((this.billingAccount != value))
                {
                    this.billingAccount = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "order")]
        public Order Order
        {
            get
            {
                return this.order;
            }
            set
            {
                if ((this.order != value))
                {
                    this.order = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "orderItem")]
        public OrderItem OrderItem
        {
            get
            {
                return this.orderItem;
            }
            set
            {
                if ((this.orderItem != value))
                {
                    this.orderItem = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "contactDetail")]
        public ContactDetails ContactDetail
        {
            get
            {
                return this.contactDetail;
            }
            set
            {
                if ((this.contactDetail != value))
                {
                    this.contactDetail = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "serviceId")]
        public ServiceInstance ServiceId
        {
            get
            {
                return this.serviceId;
            }
            set
            {
                if ((this.serviceId != value))
                {
                    this.serviceId = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15/CCM/Interaction", TypeName = "Notification")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class Notification
    {

        /// <remarks/>
        private string validationResult;

        /// <remarks/>
        private string notificationMessage;

        /// <remarks/>
        private string code;

        /// <remarks/>
        private string name;

        /// <remarks/>
        private string notificationType;

        /// <remarks/>
        private string notificationMechanism;

        public Notification()
        {
        }

        public Notification(string validationResult, string notificationMessage, string code, string name, string notificationType, string notificationMechanism)
        {
            this.validationResult = validationResult;
            this.notificationMessage = notificationMessage;
            this.code = code;
            this.name = name;
            this.notificationType = notificationType;
            this.notificationMechanism = notificationMechanism;
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "validationResult")]
        public string ValidationResult
        {
            get
            {
                return this.validationResult;
            }
            set
            {
                if ((this.validationResult != value))
                {
                    this.validationResult = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "notificationMessage")]
        public string NotificationMessage
        {
            get
            {
                return this.notificationMessage;
            }
            set
            {
                if ((this.notificationMessage != value))
                {
                    this.notificationMessage = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "code")]
        public string Code
        {
            get
            {
                return this.code;
            }
            set
            {
                if ((this.code != value))
                {
                    this.code = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "name")]
        public string Name
        {
            get
            {
                return this.name;
            }
            set
            {
                if ((this.name != value))
                {
                    this.name = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "notificationType")]
        public string NotificationType
        {
            get
            {
                return this.notificationType;
            }
            set
            {
                if ((this.notificationType != value))
                {
                    this.notificationType = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "notificationMechanism")]
        public string NotificationMechanism
        {
            get
            {
                return this.notificationMechanism;
            }
            set
            {
                if ((this.notificationMechanism != value))
                {
                    this.notificationMechanism = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15", TypeName = "orderNotificationRequest")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    [System.Xml.Serialization.XmlRoot(ElementName = "orderNotificationRequest", Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15")]
    public partial class OrderNotification
    {

        /// <remarks/>
        private StandardHeaderBlock standardHeader;

        /// <remarks/>
        private Notification notification;

        /// <remarks/>
        private Order[] order;

        [System.Xml.Serialization.XmlAttribute(AttributeName = "schemaLocation", Namespace = "")]
        public string schemaLocation = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15 ManageOrder.xsd";


        public OrderNotification()
        {
        }

        public OrderNotification(StandardHeaderBlock standardHeader, Notification notification, Order[] order)
        {
            this.standardHeader = standardHeader;
            this.notification = notification;
            this.order = order;
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "standardHeader", Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", Order = 1)]
        public StandardHeaderBlock StandardHeader
        {
            get
            {
                return this.standardHeader;
            }
            set
            {
                if ((value == null))
                {
                    throw new System.ArgumentNullException("StandardHeader");
                }
                if ((this.standardHeader != value))
                {
                    this.standardHeader = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "notification", Order = 2)]
        public Notification Notification
        {
            get
            {
                return this.notification;
            }
            set
            {
                if ((this.notification != value))
                {
                    this.notification = value;
                }
            }
        }


        [System.Xml.Serialization.XmlElementAttribute("order", IsNullable = true, Order = 3)]
        public Order[] Order
        {
            get
            {
                return this.order;
            }
            set
            {
                if ((this.order != value))
                {
                    this.order = value;
                }
            }
        }



        #region SerializeObject
        /// <summary>
        /// This method returns an xml representation of the object
        /// </summary>
        /// <returns>The xml resprestation as Srting</returns>
        public string SerializeObject()
        {
            StringBuilder serialBuilder;
            XmlSerializer messageSerializer;
            try
            {
                serialBuilder = new StringBuilder(1000);
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("wsdl", "http://schemas.xmlsoap.org/wsdl/");
                ns.Add("xsi", "http://www.w3.org/2001/XMLSchema-instance");
                using (StringWriter serialWriter = new StringWriter(serialBuilder, CultureInfo.InvariantCulture))
                {
                    messageSerializer = new XmlSerializer(this.GetType());
                    messageSerializer.Serialize(serialWriter, this, ns);
                    return serialWriter.ToString();
                }
            }
            catch (Exception serializeObjectException)
            {
                throw serializeObjectException;
            }
            finally
            {
                serialBuilder = null;
                messageSerializer = null;
            }
        }


        #endregion

        #region DeserializeObject

        /// <summary>
        ///	This method deserializes the input string into a ModifyState Request object
        /// </summary>
        /// <param name="xmlInput">The string to be deserialized</param>
        /// <returns>Populated ModifyStateRequest object</returns>
        public static OrderNotification DeserializeObject(string xmlInput)
        {
            XmlSerializer messageDeserial;
            OrderNotification request;
            try
            {

                //  deserialize
                using (StringReader messageStreamer = new StringReader(xmlInput))
                {
                    using (XmlReader messageReader = new XmlTextReader(messageStreamer))
                    {
                        messageDeserial = new XmlSerializer(typeof(OrderNotification));
                        request = (OrderNotification)messageDeserial.Deserialize(messageReader);
                        return request;
                    }
                }
            }
            catch (Exception deserializeObjectException)
            {
                throw deserializeObjectException;
            }
            finally
            {
                messageDeserial = null;
            }

        }

        #endregion DeserializeObject

    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", TypeName = "ServiceSecurity")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class ServiceSecurity
    {

        /// <remarks/>
        private string id;

        /// <remarks/>
        private string role;

        /// <remarks/>
        private string type;

        /// <remarks/>
        private string authenticationLevel;

        /// <remarks/>
        private string authenticationToken;

        /// <remarks/>
        private string userEntitlements;

        /// <remarks/>
        private string tokenExpiry;

        /// <remarks/>
        private string callingApplication;

        /// <remarks/>
        private string callingApplicationCredentials;

        public ServiceSecurity()
        {
        }

        public ServiceSecurity(string id, string role, string type, string authenticationLevel, string authenticationToken, string userEntitlements, string tokenExpiry, string callingApplication, string callingApplicationCredentials)
        {
            this.id = id;
            this.role = role;
            this.type = type;
            this.authenticationLevel = authenticationLevel;
            this.authenticationToken = authenticationToken;
            this.userEntitlements = userEntitlements;
            this.tokenExpiry = tokenExpiry;
            this.callingApplication = callingApplication;
            this.callingApplicationCredentials = callingApplicationCredentials;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "id")]
        public string Id
        {
            get
            {
                return this.id;
            }
            set
            {
                if ((this.id != value))
                {
                    this.id = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "role")]
        public string Role
        {
            get
            {
                return this.role;
            }
            set
            {
                if ((this.role != value))
                {
                    this.role = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "type")]
        public string Type
        {
            get
            {
                return this.type;
            }
            set
            {
                if ((this.type != value))
                {
                    this.type = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "authenticationLevel")]
        public string AuthenticationLevel
        {
            get
            {
                return this.authenticationLevel;
            }
            set
            {
                if ((this.authenticationLevel != value))
                {
                    this.authenticationLevel = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "authenticationToken")]
        public string AuthenticationToken
        {
            get
            {
                return this.authenticationToken;
            }
            set
            {
                if ((this.authenticationToken != value))
                {
                    this.authenticationToken = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "userEntitlements")]
        public string UserEntitlements
        {
            get
            {
                return this.userEntitlements;
            }
            set
            {
                if ((this.userEntitlements != value))
                {
                    this.userEntitlements = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "tokenExpiry")]
        public string TokenExpiry
        {
            get
            {
                return this.tokenExpiry;
            }
            set
            {
                if ((this.tokenExpiry != value))
                {
                    this.tokenExpiry = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "callingApplication")]
        public string CallingApplication
        {
            get
            {
                return this.callingApplication;
            }
            set
            {
                if ((this.callingApplication != value))
                {
                    this.callingApplication = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "callingApplicationCredentials")]
        public string CallingApplicationCredentials
        {
            get
            {
                return this.callingApplicationCredentials;
            }
            set
            {
                if ((this.callingApplicationCredentials != value))
                {
                    this.callingApplicationCredentials = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", TypeName = "ServiceSpecification")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class ServiceSpecification
    {

        /// <remarks/>
        private string payloadFormat;

        /// <remarks/>
        private string version;

        /// <remarks/>
        private string revision;

        public ServiceSpecification()
        {
        }

        public ServiceSpecification(string payloadFormat, string version, string revision)
        {
            this.payloadFormat = payloadFormat;
            this.version = version;
            this.revision = revision;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "payloadFormat")]
        public string PayloadFormat
        {
            get
            {
                return this.payloadFormat;
            }
            set
            {
                if ((this.payloadFormat != value))
                {
                    this.payloadFormat = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "version")]
        public string Version
        {
            get
            {
                return this.version;
            }
            set
            {
                if ((this.version != value))
                {
                    this.version = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "revision")]
        public string Revision
        {
            get
            {
                return this.revision;
            }
            set
            {
                if ((this.revision != value))
                {
                    this.revision = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", TypeName = "MessageDelivery")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class MessageDelivery
    {

        /// <remarks/>
        private string messagePersistence;

        /// <remarks/>
        private string messageRetries;

        /// <remarks/>
        private string messageRetryInterval;

        /// <remarks/>
        private string messageQoS;

        public MessageDelivery()
        {
        }

        public MessageDelivery(string messagePersistence, string messageRetries, string messageRetryInterval, string messageQoS)
        {
            this.messagePersistence = messagePersistence;
            this.messageRetries = messageRetries;
            this.messageRetryInterval = messageRetryInterval;
            this.messageQoS = messageQoS;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "messagePersistence")]
        public string MessagePersistence
        {
            get
            {
                return this.messagePersistence;
            }
            set
            {
                if ((this.messagePersistence != value))
                {
                    this.messagePersistence = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "messageRetries")]
        public string MessageRetries
        {
            get
            {
                return this.messageRetries;
            }
            set
            {
                if ((this.messageRetries != value))
                {
                    this.messageRetries = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "messageRetryInterval")]
        public string MessageRetryInterval
        {
            get
            {
                return this.messageRetryInterval;
            }
            set
            {
                if ((this.messageRetryInterval != value))
                {
                    this.messageRetryInterval = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "messageQoS")]
        public string MessageQoS
        {
            get
            {
                return this.messageQoS;
            }
            set
            {
                if ((this.messageQoS != value))
                {
                    this.messageQoS = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", TypeName = "MessageExpiry")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class MessageExpiry
    {

        /// <remarks/>
        private string expiryTime;

        /// <remarks/>
        private string expiryAction;

        public MessageExpiry()
        {
        }

        public MessageExpiry(string expiryTime, string expiryAction)
        {
            this.expiryTime = expiryTime;
            this.expiryAction = expiryAction;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "expiryTime")]
        public string ExpiryTime
        {
            get
            {
                return this.expiryTime;
            }
            set
            {
                if ((this.expiryTime != value))
                {
                    this.expiryTime = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "expiryAction")]
        public string ExpiryAction
        {
            get
            {
                return this.expiryAction;
            }
            set
            {
                if ((this.expiryAction != value))
                {
                    this.expiryAction = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", TypeName = "ServiceProperties")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class ServiceProperties
    {

        /// <remarks/>
        private MessageExpiry messageExpiry;

        /// <remarks/>
        private MessageDelivery messageDelivery;

        public ServiceProperties()
        {
        }

        public ServiceProperties(MessageExpiry messageExpiry, MessageDelivery messageDelivery)
        {
            this.messageExpiry = messageExpiry;
            this.messageDelivery = messageDelivery;
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "messageExpiry")]
        public MessageExpiry MessageExpiry
        {
            get
            {
                return this.messageExpiry;
            }
            set
            {
                if ((this.messageExpiry != value))
                {
                    this.messageExpiry = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "messageDelivery")]
        public MessageDelivery MessageDelivery
        {
            get
            {
                return this.messageDelivery;
            }
            set
            {
                if ((this.messageDelivery != value))
                {
                    this.messageDelivery = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", TypeName = "ContextItem")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class ContextItem
    {

        /// <remarks/>
        private string _value;

        /// <remarks/>
        private string contextId;

        /// <remarks/>
        private string contextName;

        /// <remarks/>
        private string value;

        public ContextItem()
        {
        }

        public ContextItem(string _value, string contextId, string contextName, string value)
        {
            this._value = _value;
            this.contextId = contextId;
            this.contextName = contextName;
            this.value = value;
        }

        [System.Xml.Serialization.XmlAttributeAttribute(AttributeName = "_value")]
        public string _value1
        {
            get
            {
                return this._value;
            }
            set
            {
                if ((this._value != value))
                {
                    this._value = value;
                }
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute(AttributeName = "contextId")]
        public string ContextId
        {
            get
            {
                return this.contextId;
            }
            set
            {
                if ((this.contextId != value))
                {
                    this.contextId = value;
                }
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute(AttributeName = "contextName")]
        public string ContextName
        {
            get
            {
                return this.contextName;
            }
            set
            {
                if ((this.contextName != value))
                {
                    this.contextName = value;
                }
            }
        }

        [System.Xml.Serialization.XmlTextAttribute()]
        public string Value
        {
            get
            {
                return this.value;
            }
            set
            {
                if ((this.value != value))
                {
                    this.value = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", TypeName = "AddressReference")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class AddressReference
    {

        /// <remarks/>
        private string address;

        /// <remarks/>
        private ContextItem[] contextItemList;

        public AddressReference()
        {
        }

        public AddressReference(string address, ContextItem[] contextItemList)
        {
            this.address = address;
            this.contextItemList = contextItemList;
        }

        [System.Xml.Serialization.XmlElementAttribute(DataType = "anyURI", ElementName = "address")]
        public string Address
        {
            get
            {
                return this.address;
            }
            set
            {
                if ((this.address != value))
                {
                    this.address = value;
                }
            }
        }

        [System.Xml.Serialization.XmlArrayAttribute(IsNullable = false, ElementName = "contextItemList")]
        [System.Xml.Serialization.XmlArrayItemAttribute("item", IsNullable = false)]
        public ContextItem[] ContextItemList
        {
            get
            {
                return this.contextItemList;
            }
            set
            {
                if ((value == null))
                {
                    throw new System.ArgumentNullException("ContextItemList");
                }
                if ((this.contextItemList != value))
                {
                    this.contextItemList = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", TypeName = "ServiceAddressing")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class ServiceAddressing
    {

        /// <remarks/>
        private string from;

        /// <remarks/>
        private AddressReference to;

        /// <remarks/>
        private AddressReference replyTo;

        /// <remarks/>
        private string relatesTo;

        /// <remarks/>
        private AddressReference faultTo;

        /// <remarks/>
        private string messageId;

        /// <remarks/>
        private string serviceName;

        /// <remarks/>
        private string action;

        public ServiceAddressing()
        {
        }

        public ServiceAddressing(string from, AddressReference to, AddressReference replyTo, string relatesTo, AddressReference faultTo, string messageId, string serviceName, string action)
        {
            this.from = from;
            this.to = to;
            this.replyTo = replyTo;
            this.relatesTo = relatesTo;
            this.faultTo = faultTo;
            this.messageId = messageId;
            this.serviceName = serviceName;
            this.action = action;
        }

        [System.Xml.Serialization.XmlElementAttribute(DataType = "anyURI", IsNullable = true, ElementName = "from")]
        public string From
        {
            get
            {
                return this.from;
            }
            set
            {
                if ((this.from != value))
                {
                    this.from = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "to")]
        public AddressReference To
        {
            get
            {
                return this.to;
            }
            set
            {
                if ((this.to != value))
                {
                    this.to = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "replyTo")]
        public AddressReference ReplyTo
        {
            get
            {
                return this.replyTo;
            }
            set
            {
                if ((this.replyTo != value))
                {
                    this.replyTo = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "relatesTo")]
        public string RelatesTo
        {
            get
            {
                return this.relatesTo;
            }
            set
            {
                if ((this.relatesTo != value))
                {
                    this.relatesTo = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "faultTo")]
        public AddressReference FaultTo
        {
            get
            {
                return this.faultTo;
            }
            set
            {
                if ((this.faultTo != value))
                {
                    this.faultTo = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "messageId")]
        public string MessageId
        {
            get
            {
                return this.messageId;
            }
            set
            {
                if ((this.messageId != value))
                {
                    this.messageId = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(DataType = "anyURI", IsNullable = true, ElementName = "serviceName")]
        public string ServiceName
        {
            get
            {
                return this.serviceName;
            }
            set
            {
                if ((this.serviceName != value))
                {
                    this.serviceName = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(DataType = "anyURI", IsNullable = true, ElementName = "action")]
        public string Action
        {
            get
            {
                return this.action;
            }
            set
            {
                if ((this.action != value))
                {
                    this.action = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", TypeName = "ServiceState")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class ServiceState
    {

        /// <remarks/>
        private string stateCode;

        /// <remarks/>
        private string errorCode;

        /// <remarks/>
        private string errorDesc;

        /// <remarks/>
        private string errorText;

        /// <remarks/>
        private string errorTrace;

        /// <remarks/>
        private System.Nullable<bool> resendIndicator;

        /// <remarks/>
        private bool resendIndicatorSpecified;

        /// <remarks/>
        private string retriesRemaining;

        /// <remarks/>
        private string retryInterval;

        public ServiceState()
        {
        }

        public ServiceState(string stateCode, string errorCode, string errorDesc, string errorText, string errorTrace, System.Nullable<bool> resendIndicator, bool resendIndicatorSpecified, string retriesRemaining, string retryInterval)
        {
            this.stateCode = stateCode;
            this.errorCode = errorCode;
            this.errorDesc = errorDesc;
            this.errorText = errorText;
            this.errorTrace = errorTrace;
            this.resendIndicator = resendIndicator;
            this.resendIndicatorSpecified = resendIndicatorSpecified;
            this.retriesRemaining = retriesRemaining;
            this.retryInterval = retryInterval;
        }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "stateCode")]
        public string StateCode
        {
            get
            {
                return this.stateCode;
            }
            set
            {
                if ((this.stateCode != value))
                {
                    this.stateCode = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "errorCode")]
        public string ErrorCode
        {
            get
            {
                return this.errorCode;
            }
            set
            {
                if ((this.errorCode != value))
                {
                    this.errorCode = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "errorDesc")]
        public string ErrorDesc
        {
            get
            {
                return this.errorDesc;
            }
            set
            {
                if ((this.errorDesc != value))
                {
                    this.errorDesc = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "errorText")]
        public string ErrorText
        {
            get
            {
                return this.errorText;
            }
            set
            {
                if ((this.errorText != value))
                {
                    this.errorText = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "errorTrace")]
        public string ErrorTrace
        {
            get
            {
                return this.errorTrace;
            }
            set
            {
                if ((this.errorTrace != value))
                {
                    this.errorTrace = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, ElementName = "resendIndicator")]
        public System.Nullable<bool> ResendIndicator
        {
            get
            {
                return this.resendIndicator;
            }
            set
            {
                if ((this.resendIndicator != value))
                {
                    this.resendIndicator = value;
                    this.resendIndicatorSpecified = true;
                }
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool ResendIndicatorSpecified
        {
            get
            {
                return this.resendIndicatorSpecified;
            }
            set
            {
                if ((this.resendIndicatorSpecified != value))
                {
                    this.resendIndicatorSpecified = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(DataType = "integer", IsNullable = true, ElementName = "retriesRemaining")]
        public string RetriesRemaining
        {
            get
            {
                return this.retriesRemaining;
            }
            set
            {
                if ((this.retriesRemaining != value))
                {
                    this.retriesRemaining = value;
                }
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(DataType = "integer", IsNullable = true, ElementName = "retryInterval")]
        public string RetryInterval
        {
            get
            {
                return this.retryInterval;
            }
            set
            {
                if ((this.retryInterval != value))
                {
                    this.retryInterval = value;
                }
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "2.0.50727.42")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Web.Services.WebServiceBindingAttribute(Name = "ManageOrderPortSoapBinding", Namespace = "http://capabilities.nat.bt.com/wsdl/ManageOrder")]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(PrimaryIdentifier))]
    public partial class ManageOrderComplete : System.Web.Services.Protocols.SoapHttpClientProtocol, IManageOrder
    {

        /// <remarks/>
        public ManageOrderComplete(string serviceUrl)
        {
            this.Url = serviceUrl;
        }
        public ManageOrderComplete(string url, WebProxy proxy)
        {
            this.Url = url;
            this.Proxy = proxy;
        }

        /// <remarks/>
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://capabilities.nat.bt.com/wsdl/ManageOrder#orderRequest", Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Bare)]
        public void orderRequest([System.Xml.Serialization.XmlElementAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15", ElementName = "requestOrderRequest")] OrderRequest requestOrderRequest)
        {
            this.Invoke("orderRequest", new object[] {
                        requestOrderRequest});
        }

        /// <remarks/>
        public System.IAsyncResult BeginorderRequest(OrderRequest requestOrderRequest, System.AsyncCallback callback, object asyncState)
        {
            return this.BeginInvoke("orderRequest", new object[] {
                        requestOrderRequest}, callback, asyncState);
        }

        /// <remarks/>
        public void EndorderRequest(System.IAsyncResult asyncResult)
        {
            this.EndInvoke(asyncResult);
        }

        /// <remarks/>
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://capabilities.nat.bt.com/wsdl/ManageOrder#orderNotification", Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Bare)]
        public void orderNotification([System.Xml.Serialization.XmlElementAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15", ElementName = "orderNotificationRequest")] OrderNotification orderNotificationRequest)
        {
            this.Invoke("orderNotification", new object[] {
                        orderNotificationRequest});
        }

        /// <remarks/>
        public System.IAsyncResult BeginorderNotification(OrderNotification orderNotificationRequest, System.AsyncCallback callback, object asyncState)
        {
            return this.BeginInvoke("orderNotification", new object[] {
                        orderNotificationRequest}, callback, asyncState);
        }

        /// <remarks/>
        public void EndorderNotification(System.IAsyncResult asyncResult)
        {
            this.EndInvoke(asyncResult);
        }

        /// <remarks/>
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://capabilities.nat.bt.com/wsdl/ManageOrder#findOrder", Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Bare)]
        [return: System.Xml.Serialization.XmlElementAttribute("findOrderReturn", Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15")]
        public FindOrderResponse findOrder([System.Xml.Serialization.XmlElementAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15", ElementName = "findOrderRequest")] FindOrderRequest findOrderRequest)
        {
            object[] results = this.Invoke("findOrder", new object[] {
                        findOrderRequest});
            return ((FindOrderResponse)(results[0]));
        }

        /// <remarks/>
        public System.IAsyncResult BeginfindOrder(FindOrderRequest findOrderRequest, System.AsyncCallback callback, object asyncState)
        {
            return this.BeginInvoke("findOrder", new object[] {
                        findOrderRequest}, callback, asyncState);
        }

        /// <remarks/>
        public FindOrderResponse EndfindOrder(System.IAsyncResult asyncResult)
        {
            object[] results = this.EndInvoke(asyncResult);
            return ((FindOrderResponse)(results[0]));
        }

        /// <remarks/>
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://capabilities.nat.bt.com/wsdl/ManageOrder#queryOrder", Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Bare)]
        [return: System.Xml.Serialization.XmlElementAttribute("queryOrderReturn", Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15")]
        public QueryOrderResponse queryOrder([System.Xml.Serialization.XmlElementAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15", ElementName = "queryOrderRequest")] QueryOrderRequest queryOrderRequest)
        {
            object[] results = this.Invoke("queryOrder", new object[] {
                        queryOrderRequest});
            return ((QueryOrderResponse)(results[0]));
        }

        /// <remarks/>
        public System.IAsyncResult BeginqueryOrder(QueryOrderRequest queryOrderRequest, System.AsyncCallback callback, object asyncState)
        {
            return this.BeginInvoke("queryOrder", new object[] {
                        queryOrderRequest}, callback, asyncState);
        }

        /// <remarks/>
        public QueryOrderResponse EndqueryOrder(System.IAsyncResult asyncResult)
        {
            object[] results = this.EndInvoke(asyncResult);
            return ((QueryOrderResponse)(results[0]));
        }

        /// <remarks/>
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://capabilities.nat.bt.com/wsdl/ManageOrder#orderStatus", Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Bare)]
        [return: System.Xml.Serialization.XmlElementAttribute("orderStatusReturn", Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15")]
        public OrderStatusResponse orderStatus([System.Xml.Serialization.XmlElementAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15", ElementName = "requestOrderStatusRequest")] OrderStatusRequest requestOrderStatusRequest)
        {
            object[] results = this.Invoke("orderStatus", new object[] {
                        requestOrderStatusRequest});
            return ((OrderStatusResponse)(results[0]));
        }

        /// <remarks/>
        public System.IAsyncResult BeginorderStatus(OrderStatusRequest requestOrderStatusRequest, System.AsyncCallback callback, object asyncState)
        {
            return this.BeginInvoke("orderStatus", new object[] {
                        requestOrderStatusRequest}, callback, asyncState);
        }

        /// <remarks/>
        public OrderStatusResponse EndorderStatus(System.IAsyncResult asyncResult)
        {
            object[] results = this.EndInvoke(asyncResult);
            return ((OrderStatusResponse)(results[0]));
        }

        /// <remarks/>
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://capabilities.nat.bt.com/wsdl/ManageOrder#provideActivityResponse", Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Bare)]
        public void provideActivityResponse([System.Xml.Serialization.XmlElementAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15", ElementName = "provideActivityResponseRequest")] ProvideActivityResponse provideActivityResponseRequest)
        {
            this.Invoke("provideActivityResponse", new object[] {
                        provideActivityResponseRequest});
        }

        /// <remarks/>
        public System.IAsyncResult BeginprovideActivityResponse(ProvideActivityResponse provideActivityResponseRequest, System.AsyncCallback callback, object asyncState)
        {
            return this.BeginInvoke("provideActivityResponse", new object[] {
                        provideActivityResponseRequest}, callback, asyncState);
        }

        /// <remarks/>
        public void EndprovideActivityResponse(System.IAsyncResult asyncResult)
        {
            this.EndInvoke(asyncResult);
        }

        /// <remarks/>
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://capabilities.nat.bt.com/wsdl/ManageOrder#activityRequest", Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Bare)]
        [return: System.Xml.Serialization.XmlElementAttribute("activityRequestReturn", Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15")]
        public ActivityResponse activityRequest([System.Xml.Serialization.XmlElementAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15", ElementName = "requestActivityRequest")] ActivityRequest requestActivityRequest)
        {
            object[] results = this.Invoke("activityRequest", new object[] {
                        requestActivityRequest});
            return ((ActivityResponse)(results[0]));
        }

        /// <remarks/>
        public System.IAsyncResult BeginactivityRequest(ActivityRequest requestActivityRequest, System.AsyncCallback callback, object asyncState)
        {
            return this.BeginInvoke("activityRequest", new object[] {
                        requestActivityRequest}, callback, asyncState);
        }

        /// <remarks/>
        public ActivityResponse EndactivityRequest(System.IAsyncResult asyncResult)
        {
            object[] results = this.EndInvoke(asyncResult);
            return ((ActivityResponse)(results[0]));
        }

        /// <remarks/>
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("", Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Bare)]
        [return: System.Xml.Serialization.XmlElementAttribute("getManageOrderServiceReturn", Namespace = "http://capabilities.nat.bt.com/wsdl/ManageOrder")]
        public object getManageOrderService()
        {
            object[] results = this.Invoke("getManageOrderService", new object[0]);
            return ((object)(results[0]));
        }

        /// <remarks/>
        public System.IAsyncResult BegingetManageOrderService(System.AsyncCallback callback, object asyncState)
        {
            return this.BeginInvoke("getManageOrderService", new object[0], callback, asyncState);
        }

        /// <remarks/>
        public object EndgetManageOrderService(System.IAsyncResult asyncResult)
        {
            object[] results = this.EndInvoke(asyncResult);
            return ((object)(results[0]));
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "2.0.50727.42")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Web.Services.WebServiceBindingAttribute(Name = "OssAdapterBinding", Namespace = "http://tempuri.org/")]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(PrimaryIdentifier))]
    public partial class OssAdapter : System.Web.Services.Protocols.SoapHttpClientProtocol
    {

        private System.Threading.SendOrPostCallback UserRegistrationOperationCompleted;

        private System.Threading.SendOrPostCallback SubmitOrderResponseOperationCompleted;

        private System.Threading.SendOrPostCallback SubmitOrderCompleteOperationCompleted;

        private bool useDefaultCredentialsSetExplicitly;

        /// <remarks/>
        public OssAdapter(string url)
        {
            this.Url = url;
        }

        public new string Url
        {
            get
            {
                return base.Url;
            }
            set
            {
                if ((((this.IsLocalFileSystemWebService(base.Url) == true)
                            && (this.useDefaultCredentialsSetExplicitly == false))
                            && (this.IsLocalFileSystemWebService(value) == false)))
                {
                    base.UseDefaultCredentials = false;
                }
                base.Url = value;
            }
        }

        public new bool UseDefaultCredentials
        {
            get
            {
                return base.UseDefaultCredentials;
            }
            set
            {
                base.UseDefaultCredentials = value;
                this.useDefaultCredentialsSetExplicitly = true;
            }
        }

        /// <remarks/>
        public event UserRegistrationCompletedEventHandler UserRegistrationCompleted;

        /// <remarks/>
        public event SubmitOrderResponseCompletedEventHandler SubmitOrderResponseCompleted;

        /// <remarks/>
        public event SubmitOrderCompleteCompletedEventHandler SubmitOrderCompleteCompleted;

        /// <remarks/>
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://capabilities.nat.bt.com/wsdl/ManageOrder#orderRequest", OneWay = true, Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Bare)]
        public void UserRegistration([System.Xml.Serialization.XmlElementAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15", IsNullable = true)] OrderRequest OrderRequest)
        {
            this.Invoke("UserRegistration", new object[] {
                        OrderRequest});
        }

        /// <remarks/>
        public void UserRegistrationAsync(OrderRequest OrderRequest)
        {
            this.UserRegistrationAsync(OrderRequest, null);
        }

        /// <remarks/>
        public void UserRegistrationAsync(OrderRequest OrderRequest, object userState)
        {
            if ((this.UserRegistrationOperationCompleted == null))
            {
                this.UserRegistrationOperationCompleted = new System.Threading.SendOrPostCallback(this.OnUserRegistrationOperationCompleted);
            }
            this.InvokeAsync("UserRegistration", new object[] {
                        OrderRequest}, this.UserRegistrationOperationCompleted, userState);
        }

        private void OnUserRegistrationOperationCompleted(object arg)
        {
            if ((this.UserRegistrationCompleted != null))
            {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.UserRegistrationCompleted(this, new System.ComponentModel.AsyncCompletedEventArgs(invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }

        /// <remarks/>
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("SubmitOrderResponse", OneWay = true, Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Bare)]
        public void SubmitOrderResponse([System.Xml.Serialization.XmlElementAttribute("SubmitOrderResponse", Namespace = "http://www.microsoft.com/csf/20/Sbe")] OSSResponseReceiver SubmitOrderResponse1)
        {
            this.Invoke("SubmitOrderResponse", new object[] {
                        SubmitOrderResponse1});
        }

        /// <remarks/>
        public void SubmitOrderResponseAsync(OSSResponseReceiver SubmitOrderResponse1)
        {
            this.SubmitOrderResponseAsync(SubmitOrderResponse1, null);
        }

        /// <remarks/>
        public void SubmitOrderResponseAsync(OSSResponseReceiver SubmitOrderResponse1, object userState)
        {
            if ((this.SubmitOrderResponseOperationCompleted == null))
            {
                this.SubmitOrderResponseOperationCompleted = new System.Threading.SendOrPostCallback(this.OnSubmitOrderResponseOperationCompleted);
            }
            this.InvokeAsync("SubmitOrderResponse", new object[] {
                        SubmitOrderResponse1}, this.SubmitOrderResponseOperationCompleted, userState);
        }

        private void OnSubmitOrderResponseOperationCompleted(object arg)
        {
            if ((this.SubmitOrderResponseCompleted != null))
            {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.SubmitOrderResponseCompleted(this, new System.ComponentModel.AsyncCompletedEventArgs(invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }

        /// <remarks/>
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("SubmitOrderComplete", OneWay = true, Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Bare)]
        public void SubmitOrderComplete([System.Xml.Serialization.XmlElementAttribute("SubmitOrderComplete", Namespace = "http://www.microsoft.com/csf/20/Sbe")] OSSOrderComplete SubmitOrderComplete1)
        {
            this.Invoke("SubmitOrderComplete", new object[] {
                        SubmitOrderComplete1});
        }

        /// <remarks/>
        public void SubmitOrderCompleteAsync(OSSOrderComplete SubmitOrderComplete1)
        {
            this.SubmitOrderCompleteAsync(SubmitOrderComplete1, null);
        }

        /// <remarks/>
        public void SubmitOrderCompleteAsync(OSSOrderComplete SubmitOrderComplete1, object userState)
        {
            if ((this.SubmitOrderCompleteOperationCompleted == null))
            {
                this.SubmitOrderCompleteOperationCompleted = new System.Threading.SendOrPostCallback(this.OnSubmitOrderCompleteOperationCompleted);
            }
            this.InvokeAsync("SubmitOrderComplete", new object[] {
                        SubmitOrderComplete1}, this.SubmitOrderCompleteOperationCompleted, userState);
        }

        private void OnSubmitOrderCompleteOperationCompleted(object arg)
        {
            if ((this.SubmitOrderCompleteCompleted != null))
            {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.SubmitOrderCompleteCompleted(this, new System.ComponentModel.AsyncCompletedEventArgs(invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }

        /// <remarks/>
        public new void CancelAsync(object userState)
        {
            base.CancelAsync(userState);
        }

        private bool IsLocalFileSystemWebService(string url)
        {
            if (((url == null)
                        || (url == string.Empty)))
            {
                return false;
            }
            System.Uri wsUri = new System.Uri(url);
            if (((wsUri.Port >= 1024)
                        && (string.Compare(wsUri.Host, "localHost", System.StringComparison.OrdinalIgnoreCase) == 0)))
            {
                return true;
            }
            return false;
        }
    }
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "2.0.50727.42")]
    public delegate void UserRegistrationCompletedEventHandler(object sender, System.ComponentModel.AsyncCompletedEventArgs e);

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "2.0.50727.42")]
    public delegate void SubmitOrderResponseCompletedEventHandler(object sender, System.ComponentModel.AsyncCompletedEventArgs e);

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "2.0.50727.42")]
    public delegate void SubmitOrderCompleteCompletedEventHandler(object sender, System.ComponentModel.AsyncCompletedEventArgs e);

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.microsoft.com/csf/20/Sbe")]
    public partial class OSSResponseReceiver
    {

        private string interactionIDField;

        private string requestUUIDField;

        private string resultCodeField;

        private string errorMessageField;

        /// <remarks/>
        public string InteractionID
        {
            get
            {
                return this.interactionIDField;
            }
            set
            {
                this.interactionIDField = value;
            }
        }

        /// <remarks/>
        public string RequestUUID
        {
            get
            {
                return this.requestUUIDField;
            }
            set
            {
                this.requestUUIDField = value;
            }
        }

        /// <remarks/>
        public string ResultCode
        {
            get
            {
                return this.resultCodeField;
            }
            set
            {
                this.resultCodeField = value;
            }
        }

        /// <remarks/>
        public string ErrorMessage
        {
            get
            {
                return this.errorMessageField;
            }
            set
            {
                this.errorMessageField = value;
            }
        }
    }
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.microsoft.com/csf/20/Sbe")]
    public partial class OSSOrderComplete
    {

        private string interactionIDField;

        private string requestUUIDField;

        private string resultCodeField;

        private string errorMessageField;

        private string serviceLogicErrorMessageField;

        private string serviceLogicStatusMessageField;

        private string serviceLogicResultCodeField;

        private VASCallStatusParams[] vASCallStatusCollectionField;

        /// <remarks/>
        public string InteractionID
        {
            get
            {
                return this.interactionIDField;
            }
            set
            {
                this.interactionIDField = value;
            }
        }

        /// <remarks/>
        public string RequestUUID
        {
            get
            {
                return this.requestUUIDField;
            }
            set
            {
                this.requestUUIDField = value;
            }
        }

        /// <remarks/>
        public string ResultCode
        {
            get
            {
                return this.resultCodeField;
            }
            set
            {
                this.resultCodeField = value;
            }
        }

        /// <remarks/>
        public string ErrorMessage
        {
            get
            {
                return this.errorMessageField;
            }
            set
            {
                this.errorMessageField = value;
            }
        }

        /// <remarks/>
        public string ServiceLogicErrorMessage
        {
            get
            {
                return this.serviceLogicErrorMessageField;
            }
            set
            {
                this.serviceLogicErrorMessageField = value;
            }
        }

        /// <remarks/>
        public string ServiceLogicStatusMessage
        {
            get
            {
                return this.serviceLogicStatusMessageField;
            }
            set
            {
                this.serviceLogicStatusMessageField = value;
            }
        }

        /// <remarks/>
        public string ServiceLogicResultCode
        {
            get
            {
                return this.serviceLogicResultCodeField;
            }
            set
            {
                this.serviceLogicResultCodeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        [System.Xml.Serialization.XmlArrayItemAttribute("VASCallStatus", Form = System.Xml.Schema.XmlSchemaForm.Unqualified, IsNullable = false)]
        public VASCallStatusParams[] VASCallStatusCollection
        {
            get
            {
                return this.vASCallStatusCollectionField;
            }
            set
            {
                this.vASCallStatusCollectionField = value;
            }
        }
    }
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.microsoft.com/csf/20/Sbe")]
    public partial class VASCallStatusParams
    {

        private string vASIDField;

        private string requestUUIDField;

        private string resultCodeField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public string VASID
        {
            get
            {
                return this.vASIDField;
            }
            set
            {
                this.vASIDField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public string RequestUUID
        {
            get
            {
                return this.requestUUIDField;
            }
            set
            {
                this.requestUUIDField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public string ResultCode
        {
            get
            {
                return this.resultCodeField;
            }
            set
            {
                this.resultCodeField = value;
            }
        }
    }
}
