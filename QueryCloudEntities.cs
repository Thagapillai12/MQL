using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace BT.SaaS.MSEOAdapter
{
    [DataContract(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15", Name = "QueryCloudDataRequest")]
    public sealed class QueryCloudDataRequest
    {
        [DataMember(Order = 0, Name = "OrderNumber", IsRequired = true)]
        public Guid OrderNumber { get; set; }

        [DataMember(Order = 1, Name = "OrderDate", IsRequired = true)]
        public string OrderDate { get; set; }

        [DataMember(Order = 2, Name = "CloudDataRequest", IsRequired = true)]
        public CloudDataRequest CloudData { get; set; }

    }

    [DataContract]
    public sealed class CloudDataRequest
    {
        [DataMember(Order = 0, Name = "Customer", IsRequired = true)]
        public Customer Customer { get; set; }
    }

    [DataContract]
    public sealed class Customer
    {
        [DataMember(Order = 0, Name = "Name", IsRequired = true)]
        public string Name { get; set; }

        [DataMember(Order = 1, Name = "Value")]
        public string Value { get; set; }
    }


    [DataContract(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15", Name = "QueryCloudDataResponse")]
    public sealed class QueryCloudDataResponse
    {
        [DataMember(Order = 0, Name = "Result")]
        public QueryResult Result { get; set; }

        [DataMember(Order = 0, Name = "CloudData")]
        public CloudDataResponse CloudData { get; set; }
    }

    [DataContract]
    public sealed class QueryResult
    {
        [DataMember(Order = 0, Name = "OrderNumber")]
        public Guid OrderNumber { get; set; }

        [DataMember(Order = 1, Name = "OrderDate")]
        public string OrderDate { get; set; }

        [DataMember(Order = 2, Name = "result")]
        public bool result { get; set; }

        [DataMember(Order = 3, Name = "errorCode")]
        public int errorcode { get; set; }

        [DataMember(Order = 4, Name = "errorDescritpion")]
        public string errorDescritpion { get; set; }

    }

    [DataContract]
    public sealed class CloudDataResponse
    {
        [DataMember(Order = 0, Name = "Customer")]
        public CustomerResponse Customer { get; set; }
    }

    [DataContract]
    public sealed class CustomerResponse
    {
        [DataMember(Order = 0, Name = "externalReference")]
        public string externalReference { get; set; }

        [DataMember(Order = 1, Name = "validToDate")]
        public System.DateTime validToDate { get; set; }

        [DataMember(Order = 2, Name = "status")]
        public string status { get; set; }

        [DataMember(Order = 3, Name = "syncquota")]
        public string syncquota { get; set; }

    }
}
