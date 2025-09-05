using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace BT.SaaS.MSEOAdapter
{
    [DataContract(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15", Name = "GetBTVPActivationCodeRequest")]
    public sealed class GetBTVPActivationCodeRequest
    {
        [DataMember(Order = 0, Name = "OrderNumber", IsRequired = true)]
        public Guid OrderNumber { get; set; }

        [DataMember(Order = 1, Name = "OrderDate", IsRequired = true)]
        public string OrderDate { get; set; }

        [DataMember(Order = 2, Name = "BTVPActivationCodeRequest", IsRequired = true)]
        public BTVPActivationCodeRequest BTVPActivationCode { get; set; }
    }

    [DataContract]
    public sealed class BTVPActivationCodeRequest
    {
        [DataMember(Order = 0, Name = "McAfeeQueryCustomer", IsRequired = true)]
        public McAfeeQueryCustomer Customer { get; set; }
    }

    [DataContract]
    public sealed class McAfeeQueryCustomer
    {
        [DataMember(Order = 0, Name = "CCID", IsRequired = true)]
        public string CCID { get; set; }

        [DataMember(Order = 1, Name = "SKU", IsRequired = true)]
        public string SKU { get; set; }
    }

    [DataContract(Namespace = "http://capabilities.nat.bt.com/xsd/ManageOrder/2007/03/15", Name = "GetBTVPActivationCodeResponse")]
    public sealed class GetBTVPActivationCodeResponse
    {
        [DataMember(Order = 0, Name = "Result")]
        public McAfeeQueryResult Result { get; set; }

        [DataMember(Order = 1, Name = "ActivationCode")]
        public BTVPActivationCode ActivationCode { get; set; }
    }

    [DataContract]
    public sealed class McAfeeQueryResult
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
    public sealed class BTVPActivationCode
    {
        [DataMember(Order = 0, Name = "Customer")]
        public QueryMcAfeeResponse Customer { get; set; }
    }

    [DataContract]
    public sealed class QueryMcAfeeResponse
    {
        [DataMember(Order = 0, Name = "CCID")]
        public string CCID { get; set; }

        [DataMember(Order = 1, Name = "ActivationPin")]
        public string ActivationPin { get; set; }

        [DataMember(Order = 2, Name = "ProductDownloadUrl")]
        public string ProductDownloadUrl { get; set; }
    }

}