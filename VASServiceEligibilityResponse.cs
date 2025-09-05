using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace BT.SaaS.MSEOAdapter
{
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15", TypeName = "vasServiceEligibilityResponse")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    [System.Xml.Serialization.XmlRoot(ElementName = "vasServiceEligibilityResponse", Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15")]
    public sealed class VASServiceEligibilityResponse
    {
        public VASServiceEligibilityResponse()
        {
            this.Result = new Result();
            this.VASServiceList = new List<VASService>();
            this.StandardHeader = new StandardHeaderBlock();
        }

        [DataMember(Order = 0, Name = "result")]
        public Result Result { get; set; }

        [DataMember(Order = 1, Name = "StandardHeader")]
        public StandardHeaderBlock StandardHeader { get; set; }

        [DataMember(Order = 2, Name = "vasServiceList")]
        public List<VASService> VASServiceList { get; set; }

    }

    [DataContract(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", Name = "result")]
    public sealed class Result
    {
        [DataMember(Order = 0, Name = "result")]
        public bool result { get; set; }

        [DataMember(Order = 1, Name = "errorCode")]
        public int errorCode { get; set; }

        [DataMember(Order = 2, Name = "errorDescritpion")]
        public string errorDescritpion { get; set; }

    }

    [DataContract(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15", Name = "vasService")]
    public sealed class VASService
    {
        [DataMember(Order = 0, Name = "vasClassID")]
        public string VASClassID { get; set; }

        [DataMember(Order = 1, Name = "vasProductID")]
        public string VASProductID { get; set; }

        [DataMember(Order = 2, Name = "sCode")]
        public string SCode { get; set; }

        [DataMember(Order = 3, Name = "productName")]
        public string ProductName { get; set; }

        [DataMember(Order = 4, Name = "supplierID")]
        public string SupplierID { get; set; }

        [DataMember(Order = 5, Name = "serviceTier")]
        public string ServiceTier { get; set; }

        [DataMember(Order = 6, Name = "activationCardinality")]
        public string ActivationCardinality { get; set; }

        [DataMember(Order = 7, Name = "vasProductFamily")]
        public string VASProductFamily { get; set; }

        [DataMember(Order = 8, Name = "vasSubType")]
        public string VASSubType { get; set; }

        [DataMember(Order = 9, Name = "supplierCode")]
        public string SupplierCode { get; set; }

        [DataMember(Order = 10, Name = "highestTier")]
        public string HighestTier { get; set; }

        [DataMember(Order = 11, Name = "productType")]
        public string ProductType { get; set; }
    }
}
