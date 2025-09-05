using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using MSEO = BT.SaaS.MSEOAdapter;

namespace BT.SaaS.MSEOAdapter
{
    [DataContract]
    public sealed class ManageDNPServiceInstanceRequest
    {
        [DataMember(Order = 0, Name = "StandardHeader", IsRequired = true)]
        public StandardHeaderBlock StandardHeader { get; set; }

        [DataMember(Order = 1, Name = "OrderNumber", IsRequired = true)]
        public Guid OrderNumber { get; set; }

        [DataMember(Order = 2, Name = "ServiceInstance", IsRequired = true)]
        public SrvcInstance srvcInstance { get; set; }

    }

    [DataContract]
    public sealed class SrvcInstance
    {
        [DataMember(Order = 0, Name = "BillAccountNumber", IsRequired = true)]
        public string BillAccountNumber { get; set; }

        [DataMember(Order = 1, Name = "ServiceCode", IsRequired = true)]
        public string ServiceCode { get; set; }

        [DataMember(Order = 2, Name = "InstanceCharacteristics", IsRequired = true)]
        public Characteristic InstanceCharacteristics { get; set; }
    }

    [DataContract]
    public sealed class Characteristic
    {
        /// <remarks/>
        [DataMember(Order = 1, Name = "InstanceCharacteristic", IsRequired = true)]
        public InstanceCharcteristic[] InstanceCharacteristic { get; set; }
    }

    public partial class InstanceCharcteristic
    {

        /// <remarks/>
        private string name;

        /// <remarks/>
        private string value;

        /// <remarks/>
        private string previousValue;

        /// <remarks/>
        private string previousName;

        public InstanceCharcteristic()
        {
        }

        public InstanceCharcteristic(string name, string value, string previousValue, string previousName, InstanceAction action)
        {
            this.name = name;
            this.value = value;
            this.previousValue = previousValue;
            this.previousName = previousName;
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
    }

    [DataContract(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15", Name = "ManageDNPServiceInstanceResponse")]
    public sealed class ManageDNPServiceInstanceResponse
    {
        [DataMember(Order = 0, Name = "result")]
        public bool result { get; set; }

        [DataMember(Order = 1, Name = "errorCode")]
        public string errorcode { get; set; }

        [DataMember(Order = 2, Name = "errorDescritpion")]
        public string errorDescritpion { get; set; }
    }
}