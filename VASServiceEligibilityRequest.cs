using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace BT.SaaS.MSEOAdapter
{
    [DataContract(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15", Name = "vasServiceEligibilityRequest")]
    public sealed class VASServiceEligibilityRequest
    {
        public VASServiceEligibilityRequest()
        {
            this.standardHeader = new StandardHeaderBlock();
            this.VASClassList = new List<VASClass>();
        }

        [DataMember(Order = 0, Name = "standardHeader")]
        public StandardHeaderBlock standardHeader;

        [DataMember(Order = 1, Name = "customerID")]
        public string CustomerID { get; set; }

        [DataMember(Order = 2, Name = "bacID")]
        public string BACID { get; set; }

        [DataMember(Order = 3, Name = "VASClassList")]
        public List<VASClass> VASClassList { get; set; }

        [DataMember(Order = 4, Name = "serviceType")]
        public string ServiceType { get; set; }

        [DataMember(Order = 5, Name = "supplierID")]
        public string SupplierID { get; set; }

        [DataMember(Order = 6, Name = "productID")]
        public string ProductID { get; set; }
    }

    [DataContract(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15", Name = "VASClass")]
    public sealed class VASClass
    {
        [DataMember(Order = 0, Name = "name")]
        public string Name { get; set; }

        [DataMember(Order = 1, Name = "value")]
        public string Value { get; set; }
    }
}
