namespace BT.SaaS.IspssAdapter.CRM
{
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "3.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(Namespace = "http://capabilities.saas.bt.com/wsdl/CRM", ConfigurationName = "ManageCRMProviderPortType")]
    public interface ManageCRMProviderPortType
    {

        // CODEGEN: Generating message contract since the operation manageCustomerProductInstances is neither RPC nor document wrapped.
        [System.ServiceModel.OperationContractAttribute(Action = "http://capabilities.saas.bt.com/wsdl/CRM#manageCustomerProductInstances", ReplyAction = "*")]
        [System.ServiceModel.XmlSerializerFormatAttribute()]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(DebugEvent))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetBillingAccountsResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(SuggestIdentityResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(SubmitVasOrderResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(BillingEventResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ServiceEvent))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(CheckIdentityResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetCustomerOrdersResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetCustomerBillingSummaryResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(MatchCustomerRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetCustomerProblemsResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetCustomerOrdersRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(SubmitOrderRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(HealthRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetBillingAccountsRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(CheckIdentityRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(QueryOrderRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetCustomerProblemsRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetCustomerBillingSummaryRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ManageProductInstanceRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(BillingEventRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ManageUserRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(MatchCustomerResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(OperationsEvent))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ManageProductInstanceResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(QueryOrderResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(SuggestIdentityRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ManageBillingAccountsResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(MISEvent))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(CustomerEvent))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ManageBillingAccountsRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ManageUserResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ReportProblemResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ReportProblemRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(HealthResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(SubmitVasOrderRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(SubmitOrderResponse))]
        manageCustomerProductInstancesResponse manageCustomerProductInstances(manageCustomerProductInstances request);

        // CODEGEN: Generating message contract since the operation getCustomer is neither RPC nor document wrapped.
        [System.ServiceModel.OperationContractAttribute(Action = "http://capabilities.saas.bt.com/wsdl/CRM#getCustomer", ReplyAction = "*")]
        [System.ServiceModel.XmlSerializerFormatAttribute()]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(DebugEvent))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetBillingAccountsResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(SuggestIdentityResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(SubmitVasOrderResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(BillingEventResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ServiceEvent))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(CheckIdentityResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetCustomerOrdersResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetCustomerBillingSummaryResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(MatchCustomerRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetCustomerProblemsResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetCustomerOrdersRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(SubmitOrderRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(HealthRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetBillingAccountsRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(CheckIdentityRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(QueryOrderRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetCustomerProblemsRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetCustomerBillingSummaryRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ManageProductInstanceRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(BillingEventRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ManageUserRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(MatchCustomerResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(OperationsEvent))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ManageProductInstanceResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(QueryOrderResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(SuggestIdentityRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ManageBillingAccountsResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(MISEvent))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(CustomerEvent))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ManageBillingAccountsRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ManageUserResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ReportProblemResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ReportProblemRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(HealthResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(SubmitVasOrderRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(SubmitOrderResponse))]
        getCustomerResponse1 getCustomer(getCustomer request);

        // CODEGEN: Generating message contract since the operation orderEvent is neither RPC nor document wrapped.
        [System.ServiceModel.OperationContractAttribute(Action = "http://capabilities.saas.bt.com/wsdl/CRM#orderEvent", ReplyAction = "*")]
        [System.ServiceModel.XmlSerializerFormatAttribute()]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(DebugEvent))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetBillingAccountsResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(SuggestIdentityResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(SubmitVasOrderResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(BillingEventResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ServiceEvent))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(CheckIdentityResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetCustomerOrdersResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetCustomerBillingSummaryResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(MatchCustomerRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetCustomerProblemsResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetCustomerOrdersRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(SubmitOrderRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(HealthRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetBillingAccountsRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(CheckIdentityRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(QueryOrderRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetCustomerProblemsRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetCustomerBillingSummaryRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ManageProductInstanceRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(BillingEventRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ManageUserRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(MatchCustomerResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(OperationsEvent))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ManageProductInstanceResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(QueryOrderResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(SuggestIdentityRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ManageBillingAccountsResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(MISEvent))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(CustomerEvent))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ManageBillingAccountsRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ManageUserResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ReportProblemResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ReportProblemRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(HealthResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(SubmitVasOrderRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(SubmitOrderResponse))]
        orderEventResponse orderEvent(orderEvent1 request);

        // CODEGEN: Generating message contract since the operation manageCustomerBilling is neither RPC nor document wrapped.
        [System.ServiceModel.OperationContractAttribute(Action = "http://capabilities.saas.bt.com/wsdl/CRM#manageCustomerBilling", ReplyAction = "*")]
        [System.ServiceModel.XmlSerializerFormatAttribute()]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(DebugEvent))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetBillingAccountsResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(SuggestIdentityResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(SubmitVasOrderResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(BillingEventResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ServiceEvent))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(CheckIdentityResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetCustomerOrdersResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetCustomerBillingSummaryResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(MatchCustomerRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetCustomerProblemsResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetCustomerOrdersRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(SubmitOrderRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(HealthRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetBillingAccountsRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(CheckIdentityRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(QueryOrderRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetCustomerProblemsRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetCustomerBillingSummaryRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ManageProductInstanceRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(BillingEventRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ManageUserRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(MatchCustomerResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(OperationsEvent))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ManageProductInstanceResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(QueryOrderResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(SuggestIdentityRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ManageBillingAccountsResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(MISEvent))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(CustomerEvent))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ManageBillingAccountsRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ManageUserResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ReportProblemResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ReportProblemRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(HealthResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(SubmitVasOrderRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(SubmitOrderResponse))]
        manageCustomerBillingResponse manageCustomerBilling(manageCustomerBilling request);

        // CODEGEN: Generating message contract since the operation manageCustomer is neither RPC nor document wrapped.
        [System.ServiceModel.OperationContractAttribute(Action = "http://capabilities.saas.bt.com/wsdl/CRM#manageCustomer", ReplyAction = "*")]
        [System.ServiceModel.XmlSerializerFormatAttribute()]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(DebugEvent))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetBillingAccountsResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(SuggestIdentityResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(SubmitVasOrderResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(BillingEventResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ServiceEvent))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(CheckIdentityResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetCustomerOrdersResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetCustomerBillingSummaryResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(MatchCustomerRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetCustomerProblemsResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetCustomerOrdersRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(SubmitOrderRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(HealthRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetBillingAccountsRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(CheckIdentityRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(QueryOrderRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetCustomerProblemsRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetCustomerBillingSummaryRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ManageProductInstanceRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(BillingEventRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ManageUserRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(MatchCustomerResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(OperationsEvent))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ManageProductInstanceResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(QueryOrderResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(SuggestIdentityRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ManageBillingAccountsResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(MISEvent))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(CustomerEvent))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ManageBillingAccountsRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ManageUserResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ReportProblemResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ReportProblemRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(HealthResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(SubmitVasOrderRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(SubmitOrderResponse))]
        manageCustomerResponse1 manageCustomer(manageCustomer request);

        // CODEGEN: Generating message contract since the operation getContacts is neither RPC nor document wrapped.
        [System.ServiceModel.OperationContractAttribute(Action = "http://capabilities.saas.bt.com/wsdl/CRM#getContacts", ReplyAction = "*")]
        [System.ServiceModel.XmlSerializerFormatAttribute()]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(DebugEvent))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetBillingAccountsResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(SuggestIdentityResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(SubmitVasOrderResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(BillingEventResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ServiceEvent))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(CheckIdentityResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetCustomerOrdersResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetCustomerBillingSummaryResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(MatchCustomerRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetCustomerProblemsResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetCustomerOrdersRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(SubmitOrderRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(HealthRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetBillingAccountsRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(CheckIdentityRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(QueryOrderRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetCustomerProblemsRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetCustomerBillingSummaryRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ManageProductInstanceRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(BillingEventRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ManageUserRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(MatchCustomerResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(OperationsEvent))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ManageProductInstanceResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(QueryOrderResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(SuggestIdentityRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ManageBillingAccountsResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(MISEvent))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(CustomerEvent))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ManageBillingAccountsRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ManageUserResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ReportProblemResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ReportProblemRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(HealthResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(SubmitVasOrderRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(SubmitOrderResponse))]
        getContactsResponse1 getContacts(getContacts request);

        // CODEGEN: Generating message contract since the operation getProductInstances is neither RPC nor document wrapped.
        [System.ServiceModel.OperationContractAttribute(Action = "http://capabilities.saas.bt.com/wsdl/CRM#getProductInstances", ReplyAction = "*")]
        [System.ServiceModel.XmlSerializerFormatAttribute()]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(DebugEvent))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetBillingAccountsResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(SuggestIdentityResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(SubmitVasOrderResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(BillingEventResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ServiceEvent))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(CheckIdentityResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetCustomerOrdersResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetCustomerBillingSummaryResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(MatchCustomerRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetCustomerProblemsResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetCustomerOrdersRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(SubmitOrderRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(HealthRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetBillingAccountsRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(CheckIdentityRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(QueryOrderRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetCustomerProblemsRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetCustomerBillingSummaryRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ManageProductInstanceRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(BillingEventRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ManageUserRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(MatchCustomerResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(OperationsEvent))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ManageProductInstanceResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(QueryOrderResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(SuggestIdentityRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ManageBillingAccountsResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(MISEvent))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(CustomerEvent))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ManageBillingAccountsRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ManageUserResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ReportProblemResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ReportProblemRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(HealthResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(SubmitVasOrderRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(SubmitOrderResponse))]
        getProductInstancesResponse1 getProductInstances(getProductInstances request);

        // CODEGEN: Generating message contract since the operation searchCustomer is neither RPC nor document wrapped.
        [System.ServiceModel.OperationContractAttribute(Action = "http://capabilities.saas.bt.com/wsdl/CRM#searchCustomer", ReplyAction = "*")]
        [System.ServiceModel.XmlSerializerFormatAttribute()]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(DebugEvent))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetBillingAccountsResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(SuggestIdentityResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(SubmitVasOrderResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(BillingEventResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ServiceEvent))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(CheckIdentityResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetCustomerOrdersResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetCustomerBillingSummaryResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(MatchCustomerRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetCustomerProblemsResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetCustomerOrdersRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(SubmitOrderRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(HealthRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetBillingAccountsRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(CheckIdentityRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(QueryOrderRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetCustomerProblemsRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(GetCustomerBillingSummaryRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ManageProductInstanceRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(BillingEventRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ManageUserRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(MatchCustomerResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(OperationsEvent))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ManageProductInstanceResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(QueryOrderResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(SuggestIdentityRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ManageBillingAccountsResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(MISEvent))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(CustomerEvent))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ManageBillingAccountsRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ManageUserResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ReportProblemResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ReportProblemRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(HealthResponse))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(SubmitVasOrderRequest))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(SubmitOrderResponse))]
        searchCustomerResponse1 searchCustomer(searchCustomer request);
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class ManageCustomerRequest
    {

        private StandardHeaderBlock standardHeaderField;

        private Order orderField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", IsNullable = true, Order = 0)]
        public StandardHeaderBlock standardHeader
        {
            get
            {
                return this.standardHeaderField;
            }
            set
            {
                this.standardHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public Order order
        {
            get
            {
                return this.orderField;
            }
            set
            {
                this.orderField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/")]
    public partial class StandardHeaderBlock
    {

        private E2E e2eField;

        private ServiceState serviceStateField;

        private ServiceAddressing serviceAddressingField;

        private ServiceProperties servicePropertiesField;

        private ServiceSpecification serviceSpecificationField;

        private ServiceSecurity serviceSecurityField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public E2E e2e
        {
            get
            {
                return this.e2eField;
            }
            set
            {
                this.e2eField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public ServiceState serviceState
        {
            get
            {
                return this.serviceStateField;
            }
            set
            {
                this.serviceStateField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public ServiceAddressing serviceAddressing
        {
            get
            {
                return this.serviceAddressingField;
            }
            set
            {
                this.serviceAddressingField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 3)]
        public ServiceProperties serviceProperties
        {
            get
            {
                return this.servicePropertiesField;
            }
            set
            {
                this.servicePropertiesField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 4)]
        public ServiceSpecification serviceSpecification
        {
            get
            {
                return this.serviceSpecificationField;
            }
            set
            {
                this.serviceSpecificationField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 5)]
        public ServiceSecurity serviceSecurity
        {
            get
            {
                return this.serviceSecurityField;
            }
            set
            {
                this.serviceSecurityField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/")]
    public partial class E2E
    {

        private string e2EDATAField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public string E2EDATA
        {
            get
            {
                return this.e2EDATAField;
            }
            set
            {
                this.e2EDATAField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class DebugEvent
    {

        private StandardHeaderBlock standardHeaderField;

        private EventHeader eventHeaderField;

        private string debugMessageTextField;

        private logMessageType messageTypeField;

        private string debugLevelField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", IsNullable = true, Order = 0)]
        public StandardHeaderBlock standardHeader
        {
            get
            {
                return this.standardHeaderField;
            }
            set
            {
                this.standardHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public EventHeader eventHeader
        {
            get
            {
                return this.eventHeaderField;
            }
            set
            {
                this.eventHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public string debugMessageText
        {
            get
            {
                return this.debugMessageTextField;
            }
            set
            {
                this.debugMessageTextField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 3)]
        public logMessageType messageType
        {
            get
            {
                return this.messageTypeField;
            }
            set
            {
                this.messageTypeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType = "integer", Order = 4)]
        public string debugLevel
        {
            get
            {
                return this.debugLevelField;
            }
            set
            {
                this.debugLevelField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class EventHeader
    {

        private string idField;

        private string serviceProviderIdField;

        private string sequenceNoField;

        private string dateTimeField;

        private string eventTypeField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public string id
        {
            get
            {
                return this.idField;
            }
            set
            {
                this.idField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public string serviceProviderId
        {
            get
            {
                return this.serviceProviderIdField;
            }
            set
            {
                this.serviceProviderIdField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType = "integer", Order = 2)]
        public string sequenceNo
        {
            get
            {
                return this.sequenceNoField;
            }
            set
            {
                this.sequenceNoField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 3)]
        public string dateTime
        {
            get
            {
                return this.dateTimeField;
            }
            set
            {
                this.dateTimeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 4)]
        public string eventType
        {
            get
            {
                return this.eventTypeField;
            }
            set
            {
                this.eventTypeField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public enum logMessageType
    {

        /// <remarks/>
        WARNING,

        /// <remarks/>
        INFO,

        /// <remarks/>
        ERROR,

        /// <remarks/>
        DEBUG,
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class GetBillingAccountsResponse
    {

        private StandardHeaderBlock standardHeaderField;

        private BillingAccount[] billingAccountsField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", IsNullable = true, Order = 0)]
        public StandardHeaderBlock standardHeader
        {
            get
            {
                return this.standardHeaderField;
            }
            set
            {
                this.standardHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Order = 1)]
        [System.Xml.Serialization.XmlArrayItemAttribute("billingAccount", IsNullable = false)]
        public BillingAccount[] billingAccounts
        {
            get
            {
                return this.billingAccountsField;
            }
            set
            {
                this.billingAccountsField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class BillingAccount
    {

        private string xmlIdKeyField;

        private dataActionEnum actionField;

        private dataStatusEnum statusField;

        private string billingAccountKeyField;

        private string billingAccountTypeField;

        private Contact billingContactField;

        private Address billingAddressField;

        private BankAccountDetails bankAccountDetailsField;

        private CreditDebitCardDetails paymentCardDetailsField;

        private PayPalDetails payPalDetailsField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public string xmlIdKey
        {
            get
            {
                return this.xmlIdKeyField;
            }
            set
            {
                this.xmlIdKeyField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public dataActionEnum action
        {
            get
            {
                return this.actionField;
            }
            set
            {
                this.actionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public dataStatusEnum status
        {
            get
            {
                return this.statusField;
            }
            set
            {
                this.statusField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 3)]
        public string billingAccountKey
        {
            get
            {
                return this.billingAccountKeyField;
            }
            set
            {
                this.billingAccountKeyField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 4)]
        public string billingAccountType
        {
            get
            {
                return this.billingAccountTypeField;
            }
            set
            {
                this.billingAccountTypeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 5)]
        public Contact billingContact
        {
            get
            {
                return this.billingContactField;
            }
            set
            {
                this.billingContactField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 6)]
        public Address billingAddress
        {
            get
            {
                return this.billingAddressField;
            }
            set
            {
                this.billingAddressField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 7)]
        public BankAccountDetails bankAccountDetails
        {
            get
            {
                return this.bankAccountDetailsField;
            }
            set
            {
                this.bankAccountDetailsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 8)]
        public CreditDebitCardDetails paymentCardDetails
        {
            get
            {
                return this.paymentCardDetailsField;
            }
            set
            {
                this.paymentCardDetailsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 9)]
        public PayPalDetails payPalDetails
        {
            get
            {
                return this.payPalDetailsField;
            }
            set
            {
                this.payPalDetailsField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public enum dataActionEnum
    {

        /// <remarks/>
        none,

        /// <remarks/>
        del,

        /// <remarks/>
        change,

        /// <remarks/>
        add,
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public enum dataStatusEnum
    {

        /// <remarks/>
        notDone,

        /// <remarks/>
        failed,

        /// <remarks/>
        done,
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class Contact
    {

        private string xmlIdKeyField;

        private dataActionEnum actionField;

        private dataStatusEnum statusField;

        private string contactKeyField;

        private contactTypeEnum contactTypeField;

        private string jobTitleField;

        private string titleField;

        private string firstNameField;

        private string middleNameField;

        private string lastNameField;

        private string honoursField;

        private string preferredNameField;

        private TelephoneContact[] telephoneNumbersField;

        private EmailContact[] emailAddressesField;

        private Address addressField;

        private string dateOfBirthField;

        private string genderField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public string xmlIdKey
        {
            get
            {
                return this.xmlIdKeyField;
            }
            set
            {
                this.xmlIdKeyField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public dataActionEnum action
        {
            get
            {
                return this.actionField;
            }
            set
            {
                this.actionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public dataStatusEnum status
        {
            get
            {
                return this.statusField;
            }
            set
            {
                this.statusField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 3)]
        public string contactKey
        {
            get
            {
                return this.contactKeyField;
            }
            set
            {
                this.contactKeyField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 4)]
        public contactTypeEnum contactType
        {
            get
            {
                return this.contactTypeField;
            }
            set
            {
                this.contactTypeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 5)]
        public string jobTitle
        {
            get
            {
                return this.jobTitleField;
            }
            set
            {
                this.jobTitleField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 6)]
        public string title
        {
            get
            {
                return this.titleField;
            }
            set
            {
                this.titleField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 7)]
        public string firstName
        {
            get
            {
                return this.firstNameField;
            }
            set
            {
                this.firstNameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 8)]
        public string middleName
        {
            get
            {
                return this.middleNameField;
            }
            set
            {
                this.middleNameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 9)]
        public string lastName
        {
            get
            {
                return this.lastNameField;
            }
            set
            {
                this.lastNameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 10)]
        public string honours
        {
            get
            {
                return this.honoursField;
            }
            set
            {
                this.honoursField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 11)]
        public string preferredName
        {
            get
            {
                return this.preferredNameField;
            }
            set
            {
                this.preferredNameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Order = 12)]
        [System.Xml.Serialization.XmlArrayItemAttribute("telephoneContact", IsNullable = false)]
        public TelephoneContact[] telephoneNumbers
        {
            get
            {
                return this.telephoneNumbersField;
            }
            set
            {
                this.telephoneNumbersField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Order = 13)]
        [System.Xml.Serialization.XmlArrayItemAttribute("emailContact", IsNullable = false)]
        public EmailContact[] emailAddresses
        {
            get
            {
                return this.emailAddressesField;
            }
            set
            {
                this.emailAddressesField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 14)]
        public Address address
        {
            get
            {
                return this.addressField;
            }
            set
            {
                this.addressField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 15)]
        public string dateOfBirth
        {
            get
            {
                return this.dateOfBirthField;
            }
            set
            {
                this.dateOfBirthField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 16)]
        public string gender
        {
            get
            {
                return this.genderField;
            }
            set
            {
                this.genderField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public enum contactTypeEnum
    {

        /// <remarks/>
        user,

        /// <remarks/>
        customer,
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class TelephoneContact
    {

        private string numberField;

        private string extensionField;

        private string typeField;

        private bool isPreferredField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public string number
        {
            get
            {
                return this.numberField;
            }
            set
            {
                this.numberField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public string extension
        {
            get
            {
                return this.extensionField;
            }
            set
            {
                this.extensionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public string type
        {
            get
            {
                return this.typeField;
            }
            set
            {
                this.typeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 3)]
        public bool isPreferred
        {
            get
            {
                return this.isPreferredField;
            }
            set
            {
                this.isPreferredField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class EmailContact
    {

        private string typeField;

        private string emailAddressField;

        private bool isPreferredField;

        private bool suppressAllBTEmailsField;

        private bool suppressAllBTEmailsFieldSpecified;

        private bool suppressAll3rdPartyEmailsField;

        private bool suppressAll3rdPartyEmailsFieldSpecified;

        private bool suppressAllVendorEmailsField;

        private bool suppressAllVendorEmailsFieldSpecified;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public string type
        {
            get
            {
                return this.typeField;
            }
            set
            {
                this.typeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public string emailAddress
        {
            get
            {
                return this.emailAddressField;
            }
            set
            {
                this.emailAddressField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public bool isPreferred
        {
            get
            {
                return this.isPreferredField;
            }
            set
            {
                this.isPreferredField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 3)]
        public bool suppressAllBTEmails
        {
            get
            {
                return this.suppressAllBTEmailsField;
            }
            set
            {
                this.suppressAllBTEmailsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool suppressAllBTEmailsSpecified
        {
            get
            {
                return this.suppressAllBTEmailsFieldSpecified;
            }
            set
            {
                this.suppressAllBTEmailsFieldSpecified = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 4)]
        public bool suppressAll3rdPartyEmails
        {
            get
            {
                return this.suppressAll3rdPartyEmailsField;
            }
            set
            {
                this.suppressAll3rdPartyEmailsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool suppressAll3rdPartyEmailsSpecified
        {
            get
            {
                return this.suppressAll3rdPartyEmailsFieldSpecified;
            }
            set
            {
                this.suppressAll3rdPartyEmailsFieldSpecified = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 5)]
        public bool suppressAllVendorEmails
        {
            get
            {
                return this.suppressAllVendorEmailsField;
            }
            set
            {
                this.suppressAllVendorEmailsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool suppressAllVendorEmailsSpecified
        {
            get
            {
                return this.suppressAllVendorEmailsFieldSpecified;
            }
            set
            {
                this.suppressAllVendorEmailsFieldSpecified = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class Address
    {

        private UKAddress ukAddressField;

        private IntlAddress intlAddressField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public UKAddress ukAddress
        {
            get
            {
                return this.ukAddressField;
            }
            set
            {
                this.ukAddressField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public IntlAddress intlAddress
        {
            get
            {
                return this.intlAddressField;
            }
            set
            {
                this.intlAddressField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class UKAddress
    {

        private string organisationNameField;

        private string departmentNameField;

        private string subBuildingNameField;

        private string buildingNameField;

        private string buildingNumberField;

        private string dependentThoroughfareNameField;

        private string dependendentThoroughfareDescriptorField;

        private string thoroughfareNameField;

        private string thoroughfareDescriptorField;

        private string doubleDependentLocalityField;

        private string dependentLocalityField;

        private string postTownField;

        private string postcodeField;

        private string pOBoxField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public string organisationName
        {
            get
            {
                return this.organisationNameField;
            }
            set
            {
                this.organisationNameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public string departmentName
        {
            get
            {
                return this.departmentNameField;
            }
            set
            {
                this.departmentNameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public string subBuildingName
        {
            get
            {
                return this.subBuildingNameField;
            }
            set
            {
                this.subBuildingNameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 3)]
        public string buildingName
        {
            get
            {
                return this.buildingNameField;
            }
            set
            {
                this.buildingNameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 4)]
        public string buildingNumber
        {
            get
            {
                return this.buildingNumberField;
            }
            set
            {
                this.buildingNumberField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 5)]
        public string dependentThoroughfareName
        {
            get
            {
                return this.dependentThoroughfareNameField;
            }
            set
            {
                this.dependentThoroughfareNameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 6)]
        public string dependendentThoroughfareDescriptor
        {
            get
            {
                return this.dependendentThoroughfareDescriptorField;
            }
            set
            {
                this.dependendentThoroughfareDescriptorField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 7)]
        public string thoroughfareName
        {
            get
            {
                return this.thoroughfareNameField;
            }
            set
            {
                this.thoroughfareNameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 8)]
        public string thoroughfareDescriptor
        {
            get
            {
                return this.thoroughfareDescriptorField;
            }
            set
            {
                this.thoroughfareDescriptorField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 9)]
        public string doubleDependentLocality
        {
            get
            {
                return this.doubleDependentLocalityField;
            }
            set
            {
                this.doubleDependentLocalityField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 10)]
        public string dependentLocality
        {
            get
            {
                return this.dependentLocalityField;
            }
            set
            {
                this.dependentLocalityField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 11)]
        public string postTown
        {
            get
            {
                return this.postTownField;
            }
            set
            {
                this.postTownField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 12)]
        public string postcode
        {
            get
            {
                return this.postcodeField;
            }
            set
            {
                this.postcodeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 13)]
        public string POBox
        {
            get
            {
                return this.pOBoxField;
            }
            set
            {
                this.pOBoxField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class IntlAddress
    {

        private string[] addressLineField;

        private string postalCodeField;

        private string countryField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("addressLine", Order = 0)]
        public string[] addressLine
        {
            get
            {
                return this.addressLineField;
            }
            set
            {
                this.addressLineField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public string postalCode
        {
            get
            {
                return this.postalCodeField;
            }
            set
            {
                this.postalCodeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public string country
        {
            get
            {
                return this.countryField;
            }
            set
            {
                this.countryField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class BankAccountDetails
    {

        private string bankNameField;

        private string accountNumberField;

        private string sortCodeField;

        private string accountHolderNameField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public string bankName
        {
            get
            {
                return this.bankNameField;
            }
            set
            {
                this.bankNameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public string accountNumber
        {
            get
            {
                return this.accountNumberField;
            }
            set
            {
                this.accountNumberField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public string sortCode
        {
            get
            {
                return this.sortCodeField;
            }
            set
            {
                this.sortCodeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 3)]
        public string accountHolderName
        {
            get
            {
                return this.accountHolderNameField;
            }
            set
            {
                this.accountHolderNameField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class CreditDebitCardDetails
    {

        private string cardTypeField;

        private string cardNumberField;

        private string expiryDateField;

        private string startDateField;

        private string issueNoField;

        private string cVV2Field;

        private string cardHoldersNameField;

        private Address cardHoldersAddressField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public string cardType
        {
            get
            {
                return this.cardTypeField;
            }
            set
            {
                this.cardTypeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public string cardNumber
        {
            get
            {
                return this.cardNumberField;
            }
            set
            {
                this.cardNumberField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public string expiryDate
        {
            get
            {
                return this.expiryDateField;
            }
            set
            {
                this.expiryDateField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 3)]
        public string startDate
        {
            get
            {
                return this.startDateField;
            }
            set
            {
                this.startDateField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 4)]
        public string issueNo
        {
            get
            {
                return this.issueNoField;
            }
            set
            {
                this.issueNoField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 5)]
        public string CVV2
        {
            get
            {
                return this.cVV2Field;
            }
            set
            {
                this.cVV2Field = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 6)]
        public string cardHoldersName
        {
            get
            {
                return this.cardHoldersNameField;
            }
            set
            {
                this.cardHoldersNameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 7)]
        public Address cardHoldersAddress
        {
            get
            {
                return this.cardHoldersAddressField;
            }
            set
            {
                this.cardHoldersAddressField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class PayPalDetails
    {

        private IdentityCredential payPalIdentityField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public IdentityCredential payPalIdentity
        {
            get
            {
                return this.payPalIdentityField;
            }
            set
            {
                this.payPalIdentityField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class IdentityCredential
    {

        private string xmlIdKeyField;

        private ClientIdentity identityField;

        private ClientCredential credentialField;

        private ClientCredential newCredentialField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public string xmlIdKey
        {
            get
            {
                return this.xmlIdKeyField;
            }
            set
            {
                this.xmlIdKeyField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public ClientIdentity identity
        {
            get
            {
                return this.identityField;
            }
            set
            {
                this.identityField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public ClientCredential credential
        {
            get
            {
                return this.credentialField;
            }
            set
            {
                this.credentialField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 3)]
        public ClientCredential newCredential
        {
            get
            {
                return this.newCredentialField;
            }
            set
            {
                this.newCredentialField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class ClientIdentity
    {

        private string identityAliasField;

        private string identifierField;

        private string identiferNamepaceField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public string identityAlias
        {
            get
            {
                return this.identityAliasField;
            }
            set
            {
                this.identityAliasField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public string identifier
        {
            get
            {
                return this.identifierField;
            }
            set
            {
                this.identifierField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public string identiferNamepace
        {
            get
            {
                return this.identiferNamepaceField;
            }
            set
            {
                this.identiferNamepaceField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class ClientCredential
    {

        private string credentialField;

        private string credentialTypeField;

        private string secretQuestionField;

        private string secretAnswerField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public string credential
        {
            get
            {
                return this.credentialField;
            }
            set
            {
                this.credentialField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public string credentialType
        {
            get
            {
                return this.credentialTypeField;
            }
            set
            {
                this.credentialTypeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public string secretQuestion
        {
            get
            {
                return this.secretQuestionField;
            }
            set
            {
                this.secretQuestionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 3)]
        public string secretAnswer
        {
            get
            {
                return this.secretAnswerField;
            }
            set
            {
                this.secretAnswerField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class SuggestIdentityResponse
    {

        private StandardHeaderBlock standardHeaderField;

        private Identity[] identitiesField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", IsNullable = true, Order = 0)]
        public StandardHeaderBlock standardHeader
        {
            get
            {
                return this.standardHeaderField;
            }
            set
            {
                this.standardHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Order = 1)]
        [System.Xml.Serialization.XmlArrayItemAttribute("identity", IsNullable = false)]
        public Identity[] identities
        {
            get
            {
                return this.identitiesField;
            }
            set
            {
                this.identitiesField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class Identity
    {

        private string identifierField;

        private string namespaceField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public string identifier
        {
            get
            {
                return this.identifierField;
            }
            set
            {
                this.identifierField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public string @namespace
        {
            get
            {
                return this.namespaceField;
            }
            set
            {
                this.namespaceField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class SubmitVasOrderResponse
    {

        private VasOrderHeader headerField;

        private ServiceActionOrderItem[] serviceActionOrderItemsField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public VasOrderHeader header
        {
            get
            {
                return this.headerField;
            }
            set
            {
                this.headerField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Order = 1)]
        [System.Xml.Serialization.XmlArrayItemAttribute("serviceActionOrderItem", IsNullable = false)]
        public ServiceActionOrderItem[] serviceActionOrderItems
        {
            get
            {
                return this.serviceActionOrderItemsField;
            }
            set
            {
                this.serviceActionOrderItemsField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class VasOrderHeader
    {

        private string orderIdField;

        private string statusField;

        private string effectiveDateTimeField;

        private string orderDateTimeField;

        private string serviceProviderIdField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public string orderId
        {
            get
            {
                return this.orderIdField;
            }
            set
            {
                this.orderIdField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public string status
        {
            get
            {
                return this.statusField;
            }
            set
            {
                this.statusField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public string effectiveDateTime
        {
            get
            {
                return this.effectiveDateTimeField;
            }
            set
            {
                this.effectiveDateTimeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 3)]
        public string orderDateTime
        {
            get
            {
                return this.orderDateTimeField;
            }
            set
            {
                this.orderDateTimeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 4)]
        public string serviceProviderId
        {
            get
            {
                return this.serviceProviderIdField;
            }
            set
            {
                this.serviceProviderIdField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class ServiceActionOrderItem
    {

        private string xmlIdKeyField;

        private ServiceActionOrderItemHeader headerField;

        private VasResponse vasResponseField;

        private Attribute[] attributesField;

        private ServiceRole[] serviceRolesField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public string xmlIdKey
        {
            get
            {
                return this.xmlIdKeyField;
            }
            set
            {
                this.xmlIdKeyField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public ServiceActionOrderItemHeader header
        {
            get
            {
                return this.headerField;
            }
            set
            {
                this.headerField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public VasResponse vasResponse
        {
            get
            {
                return this.vasResponseField;
            }
            set
            {
                this.vasResponseField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Order = 3)]
        [System.Xml.Serialization.XmlArrayItemAttribute("attribute", IsNullable = false)]
        public Attribute[] attributes
        {
            get
            {
                return this.attributesField;
            }
            set
            {
                this.attributesField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Order = 4)]
        [System.Xml.Serialization.XmlArrayItemAttribute("serviceRole", IsNullable = false)]
        public ServiceRole[] serviceRoles
        {
            get
            {
                return this.serviceRolesField;
            }
            set
            {
                this.serviceRolesField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class ServiceActionOrderItemHeader
    {

        private serviceActionEnum serviceActionField;

        private serviceRequestedActionEnum requestActionField;

        private dataStatusEnum statusField;

        private string serviceCodeField;

        private string serviceIdField;

        private string serviceInstanceIdField;

        private string serviceInstanceKeyField;

        private string serviceEndPointUrlField;

        private string correlationIdField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public serviceActionEnum serviceAction
        {
            get
            {
                return this.serviceActionField;
            }
            set
            {
                this.serviceActionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public serviceRequestedActionEnum RequestAction
        {
            get
            {
                return this.requestActionField;
            }
            set
            {
                this.requestActionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public dataStatusEnum status
        {
            get
            {
                return this.statusField;
            }
            set
            {
                this.statusField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 3)]
        public string serviceCode
        {
            get
            {
                return this.serviceCodeField;
            }
            set
            {
                this.serviceCodeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 4)]
        public string serviceId
        {
            get
            {
                return this.serviceIdField;
            }
            set
            {
                this.serviceIdField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 5)]
        public string serviceInstanceId
        {
            get
            {
                return this.serviceInstanceIdField;
            }
            set
            {
                this.serviceInstanceIdField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 6)]
        public string serviceInstanceKey
        {
            get
            {
                return this.serviceInstanceKeyField;
            }
            set
            {
                this.serviceInstanceKeyField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 7)]
        public string serviceEndPointUrl
        {
            get
            {
                return this.serviceEndPointUrlField;
            }
            set
            {
                this.serviceEndPointUrlField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 8)]
        public string correlationId
        {
            get
            {
                return this.correlationIdField;
            }
            set
            {
                this.correlationIdField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public enum serviceActionEnum
    {

        /// <remarks/>
        query,

        /// <remarks/>
        notDefined,

        /// <remarks/>
        modifyClient,

        /// <remarks/>
        modifyAccount,

        /// <remarks/>
        deleteClient,

        /// <remarks/>
        createClient,

        /// <remarks/>
        createAccount,

        /// <remarks/>
        ceaseAccount,
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public enum serviceRequestedActionEnum
    {

        /// <remarks/>
        upgrade,

        /// <remarks/>
        provide,

        /// <remarks/>
        notDefined,

        /// <remarks/>
        moveUsers,

        /// <remarks/>
        modifyUserSetPassword,

        /// <remarks/>
        modifyUserNone,

        /// <remarks/>
        modifyUserDelete,

        /// <remarks/>
        modifyUserChangeSecretQuestion,

        /// <remarks/>
        modifyUserChangePassword,

        /// <remarks/>
        modifyUserChange,

        /// <remarks/>
        modifyUserAdd,

        /// <remarks/>
        modifyService,

        /// <remarks/>
        modifyCustomerSuspend,

        /// <remarks/>
        modifyCustomerResume,

        /// <remarks/>
        modifyCustomer,

        /// <remarks/>
        downgrade,

        /// <remarks/>
        deactivate,

        /// <remarks/>
        cease,

        /// <remarks/>
        activate,
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class VasResponse
    {

        private bool responseCodeField;

        private string errorCodeField;

        private string errorDescriptionField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public bool responseCode
        {
            get
            {
                return this.responseCodeField;
            }
            set
            {
                this.responseCodeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public string errorCode
        {
            get
            {
                return this.errorCodeField;
            }
            set
            {
                this.errorCodeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public string errorDescription
        {
            get
            {
                return this.errorDescriptionField;
            }
            set
            {
                this.errorDescriptionField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class Attribute
    {

        private dataActionEnum actionField;

        private string nameField;

        private string valueField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public dataActionEnum action
        {
            get
            {
                return this.actionField;
            }
            set
            {
                this.actionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public string name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public string value
        {
            get
            {
                return this.valueField;
            }
            set
            {
                this.valueField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class ServiceRole
    {

        private string roleInstanceXmlRefField;

        private string roleTypeField;

        private Attribute[] roleAttributesField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public string RoleInstanceXmlRef
        {
            get
            {
                return this.roleInstanceXmlRefField;
            }
            set
            {
                this.roleInstanceXmlRefField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public string RoleType
        {
            get
            {
                return this.roleTypeField;
            }
            set
            {
                this.roleTypeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Order = 2)]
        [System.Xml.Serialization.XmlArrayItemAttribute("attribute", IsNullable = false)]
        public Attribute[] roleAttributes
        {
            get
            {
                return this.roleAttributesField;
            }
            set
            {
                this.roleAttributesField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class BillingEventResponse
    {

        private StandardHeaderBlock standardHeaderField;

        private Order orderField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", IsNullable = true, Order = 0)]
        public StandardHeaderBlock standardHeader
        {
            get
            {
                return this.standardHeaderField;
            }
            set
            {
                this.standardHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public Order order
        {
            get
            {
                return this.orderField;
            }
            set
            {
                this.orderField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class Order
    {

        private OrderHeader orderHeaderField;

        private CreditCardAuthorisation[] creditCardAuthorisationsField;

        private OrderItem[] productOrderItemsField;

        private ServiceActionOrderItem[] serviceActionOrderItemsField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public OrderHeader orderHeader
        {
            get
            {
                return this.orderHeaderField;
            }
            set
            {
                this.orderHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Order = 1)]
        [System.Xml.Serialization.XmlArrayItemAttribute("creditCardAuthorisation", IsNullable = false)]
        public CreditCardAuthorisation[] creditCardAuthorisations
        {
            get
            {
                return this.creditCardAuthorisationsField;
            }
            set
            {
                this.creditCardAuthorisationsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Order = 2)]
        [System.Xml.Serialization.XmlArrayItemAttribute("productOrderItem", IsNullable = false)]
        public OrderItem[] productOrderItems
        {
            get
            {
                return this.productOrderItemsField;
            }
            set
            {
                this.productOrderItemsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Order = 3)]
        [System.Xml.Serialization.XmlArrayItemAttribute("serviceActionOrderItem", IsNullable = false)]
        public ServiceActionOrderItem[] serviceActionOrderItems
        {
            get
            {
                return this.serviceActionOrderItemsField;
            }
            set
            {
                this.serviceActionOrderItemsField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class OrderHeader
    {

        private orderActionEnum actionField;

        private orderStatusEnum statusField;

        private string orderKeyField;

        private string orderIdField;

        private string serviceProviderIdField;

        private string effectiveDateTimeField;

        private string orderDateTimeField;

        private Customer customerField;

        private Client[] usersField;

        private ClientIdentity identityPlacingOrderField;

        private bool holdToTermField;

        private bool holdToTermFieldSpecified;

        private string ceaseReasonField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public orderActionEnum action
        {
            get
            {
                return this.actionField;
            }
            set
            {
                this.actionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public orderStatusEnum status
        {
            get
            {
                return this.statusField;
            }
            set
            {
                this.statusField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public string orderKey
        {
            get
            {
                return this.orderKeyField;
            }
            set
            {
                this.orderKeyField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 3)]
        public string orderId
        {
            get
            {
                return this.orderIdField;
            }
            set
            {
                this.orderIdField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 4)]
        public string serviceProviderId
        {
            get
            {
                return this.serviceProviderIdField;
            }
            set
            {
                this.serviceProviderIdField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 5)]
        public string effectiveDateTime
        {
            get
            {
                return this.effectiveDateTimeField;
            }
            set
            {
                this.effectiveDateTimeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 6)]
        public string orderDateTime
        {
            get
            {
                return this.orderDateTimeField;
            }
            set
            {
                this.orderDateTimeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 7)]
        public Customer customer
        {
            get
            {
                return this.customerField;
            }
            set
            {
                this.customerField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Order = 8)]
        [System.Xml.Serialization.XmlArrayItemAttribute("user", IsNullable = false)]
        public Client[] users
        {
            get
            {
                return this.usersField;
            }
            set
            {
                this.usersField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 9)]
        public ClientIdentity identityPlacingOrder
        {
            get
            {
                return this.identityPlacingOrderField;
            }
            set
            {
                this.identityPlacingOrderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 10)]
        public bool holdToTerm
        {
            get
            {
                return this.holdToTermField;
            }
            set
            {
                this.holdToTermField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool holdToTermSpecified
        {
            get
            {
                return this.holdToTermFieldSpecified;
            }
            set
            {
                this.holdToTermFieldSpecified = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 11)]
        public string ceaseReason
        {
            get
            {
                return this.ceaseReasonField;
            }
            set
            {
                this.ceaseReasonField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public enum orderActionEnum
    {

        /// <remarks/>
        regrade,

        /// <remarks/>
        provide,

        /// <remarks/>
        notDefined,

        /// <remarks/>
        moveUsers,

        /// <remarks/>
        modifyUser,

        /// <remarks/>
        modifyService,

        /// <remarks/>
        modifyCustomer,

        /// <remarks/>
        deactivate,

        /// <remarks/>
        createUser,

        /// <remarks/>
        cease,

        /// <remarks/>
        activate,
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public enum orderStatusEnum
    {

        /// <remarks/>
        timedout,

        /// <remarks/>
        success,

        /// <remarks/>
        retry,

        /// <remarks/>
        pending,

        /// <remarks/>
        @new,

        /// <remarks/>
        failed,
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class Customer
    {

        private customerActionEnum actionField;

        private dataStatusEnum statusField;

        private string customerKeyField;

        private string customerIdField;

        private string customerStatusField;

        private Contact[] contactsField;

        private string companyNameField;

        private string tradingNameField;

        private BillingAccount[] billingAccountsField;

        private Attribute[] attributesField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public customerActionEnum action
        {
            get
            {
                return this.actionField;
            }
            set
            {
                this.actionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public dataStatusEnum status
        {
            get
            {
                return this.statusField;
            }
            set
            {
                this.statusField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public string customerKey
        {
            get
            {
                return this.customerKeyField;
            }
            set
            {
                this.customerKeyField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 3)]
        public string customerId
        {
            get
            {
                return this.customerIdField;
            }
            set
            {
                this.customerIdField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 4)]
        public string customerStatus
        {
            get
            {
                return this.customerStatusField;
            }
            set
            {
                this.customerStatusField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Order = 5)]
        [System.Xml.Serialization.XmlArrayItemAttribute("contact", IsNullable = false)]
        public Contact[] contacts
        {
            get
            {
                return this.contactsField;
            }
            set
            {
                this.contactsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 6)]
        public string companyName
        {
            get
            {
                return this.companyNameField;
            }
            set
            {
                this.companyNameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 7)]
        public string tradingName
        {
            get
            {
                return this.tradingNameField;
            }
            set
            {
                this.tradingNameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Order = 8)]
        [System.Xml.Serialization.XmlArrayItemAttribute("billingAccount", IsNullable = false)]
        public BillingAccount[] billingAccounts
        {
            get
            {
                return this.billingAccountsField;
            }
            set
            {
                this.billingAccountsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Order = 9)]
        [System.Xml.Serialization.XmlArrayItemAttribute("attribute", IsNullable = false)]
        public Attribute[] attributes
        {
            get
            {
                return this.attributesField;
            }
            set
            {
                this.attributesField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public enum customerActionEnum
    {

        /// <remarks/>
        suspend,

        /// <remarks/>
        resume,

        /// <remarks/>
        none,

        /// <remarks/>
        del,

        /// <remarks/>
        change,

        /// <remarks/>
        add,
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class Client
    {

        private string xmlIdKeyField;

        private clientActionEnum actionField;

        private dataStatusEnum statusField;

        private clientStatusEnum clientStatusField;

        private bool clientStatusFieldSpecified;

        private string clientIdField;

        private string typeField;

        private IdentityCredential[] identityCredentialsField;

        private Attribute[] attributesField;

        private string contactXmlRefField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public string xmlIdKey
        {
            get
            {
                return this.xmlIdKeyField;
            }
            set
            {
                this.xmlIdKeyField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public clientActionEnum action
        {
            get
            {
                return this.actionField;
            }
            set
            {
                this.actionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public dataStatusEnum status
        {
            get
            {
                return this.statusField;
            }
            set
            {
                this.statusField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 3)]
        public clientStatusEnum clientStatus
        {
            get
            {
                return this.clientStatusField;
            }
            set
            {
                this.clientStatusField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool clientStatusSpecified
        {
            get
            {
                return this.clientStatusFieldSpecified;
            }
            set
            {
                this.clientStatusFieldSpecified = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 4)]
        public string clientId
        {
            get
            {
                return this.clientIdField;
            }
            set
            {
                this.clientIdField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 5)]
        public string type
        {
            get
            {
                return this.typeField;
            }
            set
            {
                this.typeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Order = 6)]
        [System.Xml.Serialization.XmlArrayItemAttribute("identityCredential", IsNullable = false)]
        public IdentityCredential[] identityCredentials
        {
            get
            {
                return this.identityCredentialsField;
            }
            set
            {
                this.identityCredentialsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Order = 7)]
        [System.Xml.Serialization.XmlArrayItemAttribute("attribute", IsNullable = false)]
        public Attribute[] attributes
        {
            get
            {
                return this.attributesField;
            }
            set
            {
                this.attributesField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 8)]
        public string contactXmlRef
        {
            get
            {
                return this.contactXmlRefField;
            }
            set
            {
                this.contactXmlRefField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public enum clientActionEnum
    {

        /// <remarks/>
        setPassword,

        /// <remarks/>
        passwordForgot,

        /// <remarks/>
        none,

        /// <remarks/>
        del,

        /// <remarks/>
        deactivate,

        /// <remarks/>
        changeSecretQuestion,

        /// <remarks/>
        changePassword,

        /// <remarks/>
        change,

        /// <remarks/>
        add,

        /// <remarks/>
        activate,
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public enum clientStatusEnum
    {

        /// <remarks/>
        inactive,

        /// <remarks/>
        active,
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class CreditCardAuthorisation
    {

        private creditCardAuthorisationStatusEnum statusField;

        private string authCodeField;

        private string referenceNumberField;

        private Money amountAuthorisedField;

        private string dateTimeField;

        private string billingAccountXmlRefField;

        private string[] billedOrderItemsXmlRefsField;

        private string resultField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public creditCardAuthorisationStatusEnum status
        {
            get
            {
                return this.statusField;
            }
            set
            {
                this.statusField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public string authCode
        {
            get
            {
                return this.authCodeField;
            }
            set
            {
                this.authCodeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public string referenceNumber
        {
            get
            {
                return this.referenceNumberField;
            }
            set
            {
                this.referenceNumberField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 3)]
        public Money amountAuthorised
        {
            get
            {
                return this.amountAuthorisedField;
            }
            set
            {
                this.amountAuthorisedField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 4)]
        public string dateTime
        {
            get
            {
                return this.dateTimeField;
            }
            set
            {
                this.dateTimeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 5)]
        public string billingAccountXmlRef
        {
            get
            {
                return this.billingAccountXmlRefField;
            }
            set
            {
                this.billingAccountXmlRefField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Order = 6)]
        [System.Xml.Serialization.XmlArrayItemAttribute("billedOrderItemXmlRef", IsNullable = false)]
        public string[] billedOrderItemsXmlRefs
        {
            get
            {
                return this.billedOrderItemsXmlRefsField;
            }
            set
            {
                this.billedOrderItemsXmlRefsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 7)]
        public string result
        {
            get
            {
                return this.resultField;
            }
            set
            {
                this.resultField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public enum creditCardAuthorisationStatusEnum
    {

        /// <remarks/>
        none,

        /// <remarks/>
        fulfillledFailed,

        /// <remarks/>
        fulfilledOk,

        /// <remarks/>
        authorised,
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class Money
    {

        private float amountField;

        private string currencyField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public float amount
        {
            get
            {
                return this.amountField;
            }
            set
            {
                this.amountField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public string currency
        {
            get
            {
                return this.currencyField;
            }
            set
            {
                this.currencyField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class OrderItem
    {

        private string xmlIdKeyField;

        private OrderItemHeader orderItemHeaderField;

        private RoleInstance[] roleInstancesField;

        private SequencedServiceAction[] sequencedServiceActionsField;

        private ServiceInstance[] serviceInstancesField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public string xmlIdKey
        {
            get
            {
                return this.xmlIdKeyField;
            }
            set
            {
                this.xmlIdKeyField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public OrderItemHeader orderItemHeader
        {
            get
            {
                return this.orderItemHeaderField;
            }
            set
            {
                this.orderItemHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Order = 2)]
        [System.Xml.Serialization.XmlArrayItemAttribute("roleInstance", IsNullable = false)]
        public RoleInstance[] roleInstances
        {
            get
            {
                return this.roleInstancesField;
            }
            set
            {
                this.roleInstancesField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Order = 3)]
        [System.Xml.Serialization.XmlArrayItemAttribute("sequencedServiceAction", IsNullable = false)]
        public SequencedServiceAction[] sequencedServiceActions
        {
            get
            {
                return this.sequencedServiceActionsField;
            }
            set
            {
                this.sequencedServiceActionsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Order = 4)]
        [System.Xml.Serialization.XmlArrayItemAttribute("serviceInstance", IsNullable = false)]
        public ServiceInstance[] serviceInstances
        {
            get
            {
                return this.serviceInstancesField;
            }
            set
            {
                this.serviceInstancesField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class OrderItemHeader
    {

        private dataStatusEnum statusField;

        private string orderItemKeyField;

        private string orderItemIdField;

        private string productCodeField;

        private string productNameField;

        private string productInstanceKeyField;

        private string productInstanceIdField;

        private string parentProductInstanceKeyField;

        private string parentProductInstanceIdField;

        private string parentProductOrderItemXmlRefField;

        private string billingAccountXmlRefField;

        private string quantityField;

        private bool holdToTermField;

        private bool holdToTermFieldSpecified;

        private string ceaseReasonField;

        private string deliveryContactXmlRefField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public dataStatusEnum status
        {
            get
            {
                return this.statusField;
            }
            set
            {
                this.statusField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public string orderItemKey
        {
            get
            {
                return this.orderItemKeyField;
            }
            set
            {
                this.orderItemKeyField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public string orderItemId
        {
            get
            {
                return this.orderItemIdField;
            }
            set
            {
                this.orderItemIdField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 3)]
        public string productCode
        {
            get
            {
                return this.productCodeField;
            }
            set
            {
                this.productCodeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 4)]
        public string productName
        {
            get
            {
                return this.productNameField;
            }
            set
            {
                this.productNameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 5)]
        public string productInstanceKey
        {
            get
            {
                return this.productInstanceKeyField;
            }
            set
            {
                this.productInstanceKeyField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 6)]
        public string productInstanceId
        {
            get
            {
                return this.productInstanceIdField;
            }
            set
            {
                this.productInstanceIdField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 7)]
        public string parentProductInstanceKey
        {
            get
            {
                return this.parentProductInstanceKeyField;
            }
            set
            {
                this.parentProductInstanceKeyField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 8)]
        public string parentProductInstanceId
        {
            get
            {
                return this.parentProductInstanceIdField;
            }
            set
            {
                this.parentProductInstanceIdField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 9)]
        public string parentProductOrderItemXmlRef
        {
            get
            {
                return this.parentProductOrderItemXmlRefField;
            }
            set
            {
                this.parentProductOrderItemXmlRefField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 10)]
        public string billingAccountXmlRef
        {
            get
            {
                return this.billingAccountXmlRefField;
            }
            set
            {
                this.billingAccountXmlRefField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType = "integer", Order = 11)]
        public string quantity
        {
            get
            {
                return this.quantityField;
            }
            set
            {
                this.quantityField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 12)]
        public bool holdToTerm
        {
            get
            {
                return this.holdToTermField;
            }
            set
            {
                this.holdToTermField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool holdToTermSpecified
        {
            get
            {
                return this.holdToTermFieldSpecified;
            }
            set
            {
                this.holdToTermFieldSpecified = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 13)]
        public string ceaseReason
        {
            get
            {
                return this.ceaseReasonField;
            }
            set
            {
                this.ceaseReasonField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 14)]
        public string deliveryContactXmlRef
        {
            get
            {
                return this.deliveryContactXmlRefField;
            }
            set
            {
                this.deliveryContactXmlRefField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class RoleInstance
    {

        private string xmlIdKeyField;

        private roleInstanceActionEnum actionField;

        private provisioningActionEnum internalProvisioningActionField;

        private bool internalProvisioningActionFieldSpecified;

        private dataStatusEnum statusField;

        private int quantityToAddRemoveField;

        private bool quantityToAddRemoveFieldSpecified;

        private string roleTypeField;

        private string userXmlRefField;

        private Attribute[] attributesField;

        private string userIdentityCredentialXmlRefField;

        private string licenceKeyField;

        private string licenceExpiryField;

        private string receivingProductInstanceIdField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public string xmlIdKey
        {
            get
            {
                return this.xmlIdKeyField;
            }
            set
            {
                this.xmlIdKeyField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public roleInstanceActionEnum action
        {
            get
            {
                return this.actionField;
            }
            set
            {
                this.actionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public provisioningActionEnum internalProvisioningAction
        {
            get
            {
                return this.internalProvisioningActionField;
            }
            set
            {
                this.internalProvisioningActionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool internalProvisioningActionSpecified
        {
            get
            {
                return this.internalProvisioningActionFieldSpecified;
            }
            set
            {
                this.internalProvisioningActionFieldSpecified = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 3)]
        public dataStatusEnum status
        {
            get
            {
                return this.statusField;
            }
            set
            {
                this.statusField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 4)]
        public int quantityToAddRemove
        {
            get
            {
                return this.quantityToAddRemoveField;
            }
            set
            {
                this.quantityToAddRemoveField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool quantityToAddRemoveSpecified
        {
            get
            {
                return this.quantityToAddRemoveFieldSpecified;
            }
            set
            {
                this.quantityToAddRemoveFieldSpecified = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 5)]
        public string roleType
        {
            get
            {
                return this.roleTypeField;
            }
            set
            {
                this.roleTypeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 6)]
        public string userXmlRef
        {
            get
            {
                return this.userXmlRefField;
            }
            set
            {
                this.userXmlRefField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Order = 7)]
        [System.Xml.Serialization.XmlArrayItemAttribute("attribute", IsNullable = false)]
        public Attribute[] attributes
        {
            get
            {
                return this.attributesField;
            }
            set
            {
                this.attributesField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 8)]
        public string userIdentityCredentialXmlRef
        {
            get
            {
                return this.userIdentityCredentialXmlRefField;
            }
            set
            {
                this.userIdentityCredentialXmlRefField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 9)]
        public string licenceKey
        {
            get
            {
                return this.licenceKeyField;
            }
            set
            {
                this.licenceKeyField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 10)]
        public string licenceExpiry
        {
            get
            {
                return this.licenceExpiryField;
            }
            set
            {
                this.licenceExpiryField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 11)]
        public string receivingProductInstanceId
        {
            get
            {
                return this.receivingProductInstanceIdField;
            }
            set
            {
                this.receivingProductInstanceIdField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public enum roleInstanceActionEnum
    {

        /// <remarks/>
        none,

        /// <remarks/>
        move,

        /// <remarks/>
        del,

        /// <remarks/>
        change,

        /// <remarks/>
        add,
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public enum provisioningActionEnum
    {

        /// <remarks/>
        upgrade,

        /// <remarks/>
        provide,

        /// <remarks/>
        notDefined,

        /// <remarks/>
        moveUsers,

        /// <remarks/>
        modifyUser,

        /// <remarks/>
        modifyService,

        /// <remarks/>
        modifyCustomer,

        /// <remarks/>
        downgrade,

        /// <remarks/>
        deactivate,

        /// <remarks/>
        cease,

        /// <remarks/>
        activate,
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class SequencedServiceAction
    {

        private string serviceActionOrderItemXmlRefField;

        private string sequenceNumberField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public string serviceActionOrderItemXmlRef
        {
            get
            {
                return this.serviceActionOrderItemXmlRefField;
            }
            set
            {
                this.serviceActionOrderItemXmlRefField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType = "integer", Order = 1)]
        public string sequenceNumber
        {
            get
            {
                return this.sequenceNumberField;
            }
            set
            {
                this.sequenceNumberField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class ServiceInstance
    {

        private dataActionEnum actionField;

        private dataStatusEnum statusField;

        private string serviceCodeField;

        private string serviceInstanceIdField;

        private string serviceInstanceKeyField;

        private string licenceQuantityField;

        private ServiceRole[] serviceRolesField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public dataActionEnum action
        {
            get
            {
                return this.actionField;
            }
            set
            {
                this.actionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public dataStatusEnum status
        {
            get
            {
                return this.statusField;
            }
            set
            {
                this.statusField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public string serviceCode
        {
            get
            {
                return this.serviceCodeField;
            }
            set
            {
                this.serviceCodeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 3)]
        public string serviceInstanceId
        {
            get
            {
                return this.serviceInstanceIdField;
            }
            set
            {
                this.serviceInstanceIdField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 4)]
        public string serviceInstanceKey
        {
            get
            {
                return this.serviceInstanceKeyField;
            }
            set
            {
                this.serviceInstanceKeyField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType = "integer", Order = 5)]
        public string licenceQuantity
        {
            get
            {
                return this.licenceQuantityField;
            }
            set
            {
                this.licenceQuantityField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Order = 6)]
        [System.Xml.Serialization.XmlArrayItemAttribute("serviceRole", IsNullable = false)]
        public ServiceRole[] serviceRoles
        {
            get
            {
                return this.serviceRolesField;
            }
            set
            {
                this.serviceRolesField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class ServiceEvent
    {

        private StandardHeaderBlock standardHeaderField;

        private EventHeader eventHeaderField;

        private Problem problemField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", IsNullable = true, Order = 0)]
        public StandardHeaderBlock standardHeader
        {
            get
            {
                return this.standardHeaderField;
            }
            set
            {
                this.standardHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public EventHeader eventHeader
        {
            get
            {
                return this.eventHeaderField;
            }
            set
            {
                this.eventHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public Problem problem
        {
            get
            {
                return this.problemField;
            }
            set
            {
                this.problemField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class Problem
    {

        private dataActionEnum actionField;

        private string externalIdField;

        private string idField;

        private string cRMIdField;

        private string productInstanceKeyField;

        private string problemDescriptionField;

        private Note[] notesField;

        private Client clientReportingProblemField;

        private string priorityField;

        private string statusField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public dataActionEnum action
        {
            get
            {
                return this.actionField;
            }
            set
            {
                this.actionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public string externalId
        {
            get
            {
                return this.externalIdField;
            }
            set
            {
                this.externalIdField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public string id
        {
            get
            {
                return this.idField;
            }
            set
            {
                this.idField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 3)]
        public string CRMId
        {
            get
            {
                return this.cRMIdField;
            }
            set
            {
                this.cRMIdField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 4)]
        public string productInstanceKey
        {
            get
            {
                return this.productInstanceKeyField;
            }
            set
            {
                this.productInstanceKeyField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 5)]
        public string problemDescription
        {
            get
            {
                return this.problemDescriptionField;
            }
            set
            {
                this.problemDescriptionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Order = 6)]
        [System.Xml.Serialization.XmlArrayItemAttribute("note", IsNullable = false)]
        public Note[] notes
        {
            get
            {
                return this.notesField;
            }
            set
            {
                this.notesField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 7)]
        public Client clientReportingProblem
        {
            get
            {
                return this.clientReportingProblemField;
            }
            set
            {
                this.clientReportingProblemField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 8)]
        public string priority
        {
            get
            {
                return this.priorityField;
            }
            set
            {
                this.priorityField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 9)]
        public string status
        {
            get
            {
                return this.statusField;
            }
            set
            {
                this.statusField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class Note
    {

        private dataActionEnum actionField;

        private string dateTimeField;

        private string textField;

        private string userIdField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public dataActionEnum action
        {
            get
            {
                return this.actionField;
            }
            set
            {
                this.actionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public string dateTime
        {
            get
            {
                return this.dateTimeField;
            }
            set
            {
                this.dateTimeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public string text
        {
            get
            {
                return this.textField;
            }
            set
            {
                this.textField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 3)]
        public string userId
        {
            get
            {
                return this.userIdField;
            }
            set
            {
                this.userIdField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class CheckIdentityResponse
    {

        private StandardHeaderBlock standardHeaderField;

        private bool resultField;

        private bool resultFieldSpecified;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", IsNullable = true, Order = 0)]
        public StandardHeaderBlock standardHeader
        {
            get
            {
                return this.standardHeaderField;
            }
            set
            {
                this.standardHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public bool result
        {
            get
            {
                return this.resultField;
            }
            set
            {
                this.resultField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool resultSpecified
        {
            get
            {
                return this.resultFieldSpecified;
            }
            set
            {
                this.resultFieldSpecified = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class GetCustomerOrdersResponse
    {

        private StandardHeaderBlock standardHeaderField;

        private Order[] ordersField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", IsNullable = true, Order = 0)]
        public StandardHeaderBlock standardHeader
        {
            get
            {
                return this.standardHeaderField;
            }
            set
            {
                this.standardHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Order = 1)]
        [System.Xml.Serialization.XmlArrayItemAttribute("order", IsNullable = false)]
        public Order[] orders
        {
            get
            {
                return this.ordersField;
            }
            set
            {
                this.ordersField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class GetCustomerBillingSummaryResponse
    {

        private StandardHeaderBlock standardHeaderField;

        private BillingAccountSummary accountSummaryField;

        private InvoiceSummary[] invoiceSummariesField;

        private BillingTransaction[] billingTransactionsField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", IsNullable = true, Order = 0)]
        public StandardHeaderBlock standardHeader
        {
            get
            {
                return this.standardHeaderField;
            }
            set
            {
                this.standardHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public BillingAccountSummary accountSummary
        {
            get
            {
                return this.accountSummaryField;
            }
            set
            {
                this.accountSummaryField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Order = 2)]
        [System.Xml.Serialization.XmlArrayItemAttribute("invoiceSummary", IsNullable = false)]
        public InvoiceSummary[] invoiceSummaries
        {
            get
            {
                return this.invoiceSummariesField;
            }
            set
            {
                this.invoiceSummariesField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Order = 3)]
        [System.Xml.Serialization.XmlArrayItemAttribute(IsNullable = false)]
        public BillingTransaction[] billingTransactions
        {
            get
            {
                return this.billingTransactionsField;
            }
            set
            {
                this.billingTransactionsField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class BillingAccountSummary
    {

        private string billingAccountKeyField;

        private string nameField;

        private Address billingAddressField;

        private TelephoneAccount telephoneAccountField;

        private string accountTypeField;

        private string paymentMethodField;

        private string currentInvoiceField;

        private Money creditLimitField;

        private Money balanceField;

        private string billDateField;

        private Money unbilledUsageField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public string billingAccountKey
        {
            get
            {
                return this.billingAccountKeyField;
            }
            set
            {
                this.billingAccountKeyField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public string name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public Address billingAddress
        {
            get
            {
                return this.billingAddressField;
            }
            set
            {
                this.billingAddressField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 3)]
        public TelephoneAccount telephoneAccount
        {
            get
            {
                return this.telephoneAccountField;
            }
            set
            {
                this.telephoneAccountField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 4)]
        public string accountType
        {
            get
            {
                return this.accountTypeField;
            }
            set
            {
                this.accountTypeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 5)]
        public string paymentMethod
        {
            get
            {
                return this.paymentMethodField;
            }
            set
            {
                this.paymentMethodField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 6)]
        public string currentInvoice
        {
            get
            {
                return this.currentInvoiceField;
            }
            set
            {
                this.currentInvoiceField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 7)]
        public Money creditLimit
        {
            get
            {
                return this.creditLimitField;
            }
            set
            {
                this.creditLimitField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 8)]
        public Money balance
        {
            get
            {
                return this.balanceField;
            }
            set
            {
                this.balanceField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 9)]
        public string billDate
        {
            get
            {
                return this.billDateField;
            }
            set
            {
                this.billDateField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 10)]
        public Money unbilledUsage
        {
            get
            {
                return this.unbilledUsageField;
            }
            set
            {
                this.unbilledUsageField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class TelephoneAccount
    {

        private string telNoField;

        private string installedDateField;

        private string lineStatusField;

        private string directoryEntryTypeField;

        private string dutyRefField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public string telNo
        {
            get
            {
                return this.telNoField;
            }
            set
            {
                this.telNoField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public string installedDate
        {
            get
            {
                return this.installedDateField;
            }
            set
            {
                this.installedDateField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public string lineStatus
        {
            get
            {
                return this.lineStatusField;
            }
            set
            {
                this.lineStatusField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 3)]
        public string directoryEntryType
        {
            get
            {
                return this.directoryEntryTypeField;
            }
            set
            {
                this.directoryEntryTypeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 4)]
        public string dutyRef
        {
            get
            {
                return this.dutyRefField;
            }
            set
            {
                this.dutyRefField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class InvoiceSummary
    {

        private string invoiceIdField;

        private string invoiceDateField;

        private string invoiceVerField;

        private Money invoiceAmountField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public string invoiceId
        {
            get
            {
                return this.invoiceIdField;
            }
            set
            {
                this.invoiceIdField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public string invoiceDate
        {
            get
            {
                return this.invoiceDateField;
            }
            set
            {
                this.invoiceDateField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public string invoiceVer
        {
            get
            {
                return this.invoiceVerField;
            }
            set
            {
                this.invoiceVerField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 3)]
        public Money invoiceAmount
        {
            get
            {
                return this.invoiceAmountField;
            }
            set
            {
                this.invoiceAmountField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class BillingTransaction
    {

        private string typeField;

        private string paymentDescriptionField;

        private Money paymentAmountField;

        private string paymentDateTimeField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public string type
        {
            get
            {
                return this.typeField;
            }
            set
            {
                this.typeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public string paymentDescription
        {
            get
            {
                return this.paymentDescriptionField;
            }
            set
            {
                this.paymentDescriptionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public Money paymentAmount
        {
            get
            {
                return this.paymentAmountField;
            }
            set
            {
                this.paymentAmountField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 3)]
        public string paymentDateTime
        {
            get
            {
                return this.paymentDateTimeField;
            }
            set
            {
                this.paymentDateTimeField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class MatchCustomerRequest
    {

        private StandardHeaderBlock standardHeaderField;

        private Customer customerField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", IsNullable = true, Order = 0)]
        public StandardHeaderBlock standardHeader
        {
            get
            {
                return this.standardHeaderField;
            }
            set
            {
                this.standardHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public Customer customer
        {
            get
            {
                return this.customerField;
            }
            set
            {
                this.customerField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class GetCustomerProblemsResponse
    {

        private StandardHeaderBlock standardHeaderField;

        private Problem[] problemsField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", IsNullable = true, Order = 0)]
        public StandardHeaderBlock standardHeader
        {
            get
            {
                return this.standardHeaderField;
            }
            set
            {
                this.standardHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Order = 1)]
        [System.Xml.Serialization.XmlArrayItemAttribute("problem", IsNullable = false)]
        public Problem[] problems
        {
            get
            {
                return this.problemsField;
            }
            set
            {
                this.problemsField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class GetCustomerOrdersRequest
    {

        private StandardHeaderBlock standardHeaderField;

        private string customerIdField;

        private string customerKeyField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", IsNullable = true, Order = 0)]
        public StandardHeaderBlock standardHeader
        {
            get
            {
                return this.standardHeaderField;
            }
            set
            {
                this.standardHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public string customerId
        {
            get
            {
                return this.customerIdField;
            }
            set
            {
                this.customerIdField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public string customerKey
        {
            get
            {
                return this.customerKeyField;
            }
            set
            {
                this.customerKeyField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class SubmitOrderRequest
    {

        private StandardHeaderBlock standardHeaderField;

        private Order orderField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", IsNullable = true, Order = 0)]
        public StandardHeaderBlock standardHeader
        {
            get
            {
                return this.standardHeaderField;
            }
            set
            {
                this.standardHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public Order order
        {
            get
            {
                return this.orderField;
            }
            set
            {
                this.orderField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class HealthRequest
    {

        private StandardHeaderBlock standardHeaderField;

        private string levelField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", IsNullable = true, Order = 0)]
        public StandardHeaderBlock standardHeader
        {
            get
            {
                return this.standardHeaderField;
            }
            set
            {
                this.standardHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public string level
        {
            get
            {
                return this.levelField;
            }
            set
            {
                this.levelField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class GetBillingAccountsRequest
    {

        private StandardHeaderBlock standardHeaderField;

        private string[] billingAccountKeysField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", IsNullable = true, Order = 0)]
        public StandardHeaderBlock standardHeader
        {
            get
            {
                return this.standardHeaderField;
            }
            set
            {
                this.standardHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Order = 1)]
        [System.Xml.Serialization.XmlArrayItemAttribute("billingAccountKey", IsNullable = false)]
        public string[] billingAccountKeys
        {
            get
            {
                return this.billingAccountKeysField;
            }
            set
            {
                this.billingAccountKeysField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class CheckIdentityRequest
    {

        private StandardHeaderBlock standardHeaderField;

        private Identity identityField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", IsNullable = true, Order = 0)]
        public StandardHeaderBlock standardHeader
        {
            get
            {
                return this.standardHeaderField;
            }
            set
            {
                this.standardHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public Identity identity
        {
            get
            {
                return this.identityField;
            }
            set
            {
                this.identityField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class QueryOrder
    {

        private string orderIdField;

        private ExternalOrderQuery externalOrderQueryField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public string orderId
        {
            get
            {
                return this.orderIdField;
            }
            set
            {
                this.orderIdField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public ExternalOrderQuery externalOrderQuery
        {
            get
            {
                return this.externalOrderQueryField;
            }
            set
            {
                this.externalOrderQueryField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class ExternalOrderQuery
    {

        private string externalOrderIdField;

        private string serviceProviderIdField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public string externalOrderId
        {
            get
            {
                return this.externalOrderIdField;
            }
            set
            {
                this.externalOrderIdField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public string serviceProviderId
        {
            get
            {
                return this.serviceProviderIdField;
            }
            set
            {
                this.serviceProviderIdField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class QueryOrderRequest
    {

        private StandardHeaderBlock standardHeaderField;

        private QueryOrder queryOrderField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", IsNullable = true, Order = 0)]
        public StandardHeaderBlock standardHeader
        {
            get
            {
                return this.standardHeaderField;
            }
            set
            {
                this.standardHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public QueryOrder queryOrder
        {
            get
            {
                return this.queryOrderField;
            }
            set
            {
                this.queryOrderField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class GetCustomerProblemsRequest
    {

        private StandardHeaderBlock standardHeaderField;

        private string customerKeyField;

        private string customerIdField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", IsNullable = true, Order = 0)]
        public StandardHeaderBlock standardHeader
        {
            get
            {
                return this.standardHeaderField;
            }
            set
            {
                this.standardHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public string customerKey
        {
            get
            {
                return this.customerKeyField;
            }
            set
            {
                this.customerKeyField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public string customerId
        {
            get
            {
                return this.customerIdField;
            }
            set
            {
                this.customerIdField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class GetCustomerBillingSummaryRequest
    {

        private StandardHeaderBlock standardHeaderField;

        private string billingAccountKeyField;

        private string telNoField;

        private string startDateTimeField;

        private string endDateTimeField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", IsNullable = true, Order = 0)]
        public StandardHeaderBlock standardHeader
        {
            get
            {
                return this.standardHeaderField;
            }
            set
            {
                this.standardHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public string billingAccountKey
        {
            get
            {
                return this.billingAccountKeyField;
            }
            set
            {
                this.billingAccountKeyField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public string telNo
        {
            get
            {
                return this.telNoField;
            }
            set
            {
                this.telNoField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 3)]
        public string startDateTime
        {
            get
            {
                return this.startDateTimeField;
            }
            set
            {
                this.startDateTimeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 4)]
        public string endDateTime
        {
            get
            {
                return this.endDateTimeField;
            }
            set
            {
                this.endDateTimeField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class ManageProductInstanceRequest
    {

        private StandardHeaderBlock standardHeaderField;

        private OrderItem productInstanceAsOrderItemField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", IsNullable = true, Order = 0)]
        public StandardHeaderBlock standardHeader
        {
            get
            {
                return this.standardHeaderField;
            }
            set
            {
                this.standardHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public OrderItem productInstanceAsOrderItem
        {
            get
            {
                return this.productInstanceAsOrderItemField;
            }
            set
            {
                this.productInstanceAsOrderItemField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class BillingEventRequest
    {

        private StandardHeaderBlock standardHeaderField;

        private Order orderField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", IsNullable = true, Order = 0)]
        public StandardHeaderBlock standardHeader
        {
            get
            {
                return this.standardHeaderField;
            }
            set
            {
                this.standardHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public Order order
        {
            get
            {
                return this.orderField;
            }
            set
            {
                this.orderField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class ManageUserRequest
    {

        private StandardHeaderBlock standardHeaderField;

        private Client userField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", IsNullable = true, Order = 0)]
        public StandardHeaderBlock standardHeader
        {
            get
            {
                return this.standardHeaderField;
            }
            set
            {
                this.standardHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public Client user
        {
            get
            {
                return this.userField;
            }
            set
            {
                this.userField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class MatchCustomerResponse
    {

        private StandardHeaderBlock standardHeaderField;

        private Customer[] customersField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", IsNullable = true, Order = 0)]
        public StandardHeaderBlock standardHeader
        {
            get
            {
                return this.standardHeaderField;
            }
            set
            {
                this.standardHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Order = 1)]
        [System.Xml.Serialization.XmlArrayItemAttribute("customer", IsNullable = false)]
        public Customer[] customers
        {
            get
            {
                return this.customersField;
            }
            set
            {
                this.customersField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class OperationsEvent
    {

        private StandardHeaderBlock standardHeaderField;

        private EventHeader eventHeaderField;

        private string messageCodeField;

        private string messageTextField;

        private string relatedDataField;

        private logMessageType severityField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", IsNullable = true, Order = 0)]
        public StandardHeaderBlock standardHeader
        {
            get
            {
                return this.standardHeaderField;
            }
            set
            {
                this.standardHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public EventHeader eventHeader
        {
            get
            {
                return this.eventHeaderField;
            }
            set
            {
                this.eventHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public string messageCode
        {
            get
            {
                return this.messageCodeField;
            }
            set
            {
                this.messageCodeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 3)]
        public string messageText
        {
            get
            {
                return this.messageTextField;
            }
            set
            {
                this.messageTextField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 4)]
        public string relatedData
        {
            get
            {
                return this.relatedDataField;
            }
            set
            {
                this.relatedDataField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 5)]
        public logMessageType severity
        {
            get
            {
                return this.severityField;
            }
            set
            {
                this.severityField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class ManageProductInstanceResponse
    {

        private StandardHeaderBlock standardHeaderField;

        private OrderItem productInstanceAsOrderItemField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", IsNullable = true, Order = 0)]
        public StandardHeaderBlock standardHeader
        {
            get
            {
                return this.standardHeaderField;
            }
            set
            {
                this.standardHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public OrderItem productInstanceAsOrderItem
        {
            get
            {
                return this.productInstanceAsOrderItemField;
            }
            set
            {
                this.productInstanceAsOrderItemField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class QueryOrderResponse
    {

        private StandardHeaderBlock standardHeaderField;

        private Order orderField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", IsNullable = true, Order = 0)]
        public StandardHeaderBlock standardHeader
        {
            get
            {
                return this.standardHeaderField;
            }
            set
            {
                this.standardHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public Order order
        {
            get
            {
                return this.orderField;
            }
            set
            {
                this.orderField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class SuggestIdentityRequest
    {

        private StandardHeaderBlock standardHeaderField;

        private string namespaceField;

        private string numberOfSuggestionsField;

        private string[] inputStringsField;

        private string[] domainsField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", IsNullable = true, Order = 0)]
        public StandardHeaderBlock standardHeader
        {
            get
            {
                return this.standardHeaderField;
            }
            set
            {
                this.standardHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public string @namespace
        {
            get
            {
                return this.namespaceField;
            }
            set
            {
                this.namespaceField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType = "integer", Order = 2)]
        public string numberOfSuggestions
        {
            get
            {
                return this.numberOfSuggestionsField;
            }
            set
            {
                this.numberOfSuggestionsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Order = 3)]
        [System.Xml.Serialization.XmlArrayItemAttribute("inputString", IsNullable = false)]
        public string[] inputStrings
        {
            get
            {
                return this.inputStringsField;
            }
            set
            {
                this.inputStringsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Order = 4)]
        [System.Xml.Serialization.XmlArrayItemAttribute("domain", IsNullable = false)]
        public string[] domains
        {
            get
            {
                return this.domainsField;
            }
            set
            {
                this.domainsField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class ManageBillingAccountsResponse
    {

        private StandardHeaderBlock standardHeaderField;

        private Order orderField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", IsNullable = true, Order = 0)]
        public StandardHeaderBlock standardHeader
        {
            get
            {
                return this.standardHeaderField;
            }
            set
            {
                this.standardHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public Order order
        {
            get
            {
                return this.orderField;
            }
            set
            {
                this.orderField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class MISEvent
    {

        private StandardHeaderBlock standardHeaderField;

        private EventHeader eventHeaderField;

        private string misDataField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", IsNullable = true, Order = 0)]
        public StandardHeaderBlock standardHeader
        {
            get
            {
                return this.standardHeaderField;
            }
            set
            {
                this.standardHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public EventHeader eventHeader
        {
            get
            {
                return this.eventHeaderField;
            }
            set
            {
                this.eventHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public string misData
        {
            get
            {
                return this.misDataField;
            }
            set
            {
                this.misDataField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class CustomerEvent
    {

        private StandardHeaderBlock standardHeaderField;

        private EventHeader eventHeaderField;

        private Customer customerField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", IsNullable = true, Order = 0)]
        public StandardHeaderBlock standardHeader
        {
            get
            {
                return this.standardHeaderField;
            }
            set
            {
                this.standardHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public EventHeader eventHeader
        {
            get
            {
                return this.eventHeaderField;
            }
            set
            {
                this.eventHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public Customer customer
        {
            get
            {
                return this.customerField;
            }
            set
            {
                this.customerField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class ManageBillingAccountsRequest
    {

        private StandardHeaderBlock standardHeaderField;

        private Order orderField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", IsNullable = true, Order = 0)]
        public StandardHeaderBlock standardHeader
        {
            get
            {
                return this.standardHeaderField;
            }
            set
            {
                this.standardHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public Order order
        {
            get
            {
                return this.orderField;
            }
            set
            {
                this.orderField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class ManageUserResponse
    {

        private StandardHeaderBlock standardHeaderField;

        private Client userField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", IsNullable = true, Order = 0)]
        public StandardHeaderBlock standardHeader
        {
            get
            {
                return this.standardHeaderField;
            }
            set
            {
                this.standardHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public Client user
        {
            get
            {
                return this.userField;
            }
            set
            {
                this.userField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class ReportProblemResponse
    {

        private StandardHeaderBlock standardHeaderField;

        private string problemIdField;

        private string externalIdField;

        private string cRMIdField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", IsNullable = true, Order = 0)]
        public StandardHeaderBlock standardHeader
        {
            get
            {
                return this.standardHeaderField;
            }
            set
            {
                this.standardHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public string problemId
        {
            get
            {
                return this.problemIdField;
            }
            set
            {
                this.problemIdField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public string externalId
        {
            get
            {
                return this.externalIdField;
            }
            set
            {
                this.externalIdField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 3)]
        public string CRMId
        {
            get
            {
                return this.cRMIdField;
            }
            set
            {
                this.cRMIdField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class ReportProblemRequest
    {

        private StandardHeaderBlock standardHeaderField;

        private Problem problemField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", IsNullable = true, Order = 0)]
        public StandardHeaderBlock standardHeader
        {
            get
            {
                return this.standardHeaderField;
            }
            set
            {
                this.standardHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public Problem problem
        {
            get
            {
                return this.problemField;
            }
            set
            {
                this.problemField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class ComponentHealth
    {

        private string componentField;

        private bool resultField;

        private string detailedMessageField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public string component
        {
            get
            {
                return this.componentField;
            }
            set
            {
                this.componentField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public bool result
        {
            get
            {
                return this.resultField;
            }
            set
            {
                this.resultField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public string detailedMessage
        {
            get
            {
                return this.detailedMessageField;
            }
            set
            {
                this.detailedMessageField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class HealthResponse
    {

        private StandardHeaderBlock standardHeaderField;

        private ComponentHealth[] componentHealthsField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", IsNullable = true, Order = 0)]
        public StandardHeaderBlock standardHeader
        {
            get
            {
                return this.standardHeaderField;
            }
            set
            {
                this.standardHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Order = 1)]
        [System.Xml.Serialization.XmlArrayItemAttribute("componentHealth", IsNullable = false)]
        public ComponentHealth[] componentHealths
        {
            get
            {
                return this.componentHealthsField;
            }
            set
            {
                this.componentHealthsField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class SubmitVasOrderRequest
    {

        private VasOrderHeader headerField;

        private ServiceActionOrderItem serviceActionOrderItemField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public VasOrderHeader header
        {
            get
            {
                return this.headerField;
            }
            set
            {
                this.headerField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public ServiceActionOrderItem serviceActionOrderItem
        {
            get
            {
                return this.serviceActionOrderItemField;
            }
            set
            {
                this.serviceActionOrderItemField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class SubmitOrderResponse
    {

        private StandardHeaderBlock standardHeaderField;

        private Order orderField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", IsNullable = true, Order = 0)]
        public StandardHeaderBlock standardHeader
        {
            get
            {
                return this.standardHeaderField;
            }
            set
            {
                this.standardHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public Order order
        {
            get
            {
                return this.orderField;
            }
            set
            {
                this.orderField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class GetProductInstancesResponse
    {

        private StandardHeaderBlock standardHeaderField;

        private string customerKeyField;

        private ProductInstanceSummary[] productInstanceSummariesField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", IsNullable = true, Order = 0)]
        public StandardHeaderBlock standardHeader
        {
            get
            {
                return this.standardHeaderField;
            }
            set
            {
                this.standardHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public string customerKey
        {
            get
            {
                return this.customerKeyField;
            }
            set
            {
                this.customerKeyField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Order = 2)]
        [System.Xml.Serialization.XmlArrayItemAttribute("productInstanceSummary", IsNullable = false)]
        public ProductInstanceSummary[] productInstanceSummaries
        {
            get
            {
                return this.productInstanceSummariesField;
            }
            set
            {
                this.productInstanceSummariesField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class ProductInstanceSummary
    {

        private string quantityField;

        private string productCodeField;

        private string productNameField;

        private string productInstanceKeyField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType = "integer", Order = 0)]
        public string quantity
        {
            get
            {
                return this.quantityField;
            }
            set
            {
                this.quantityField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public string productCode
        {
            get
            {
                return this.productCodeField;
            }
            set
            {
                this.productCodeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public string productName
        {
            get
            {
                return this.productNameField;
            }
            set
            {
                this.productNameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 3)]
        public string productInstanceKey
        {
            get
            {
                return this.productInstanceKeyField;
            }
            set
            {
                this.productInstanceKeyField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class SearchCustomerRequest
    {

        private StandardHeaderBlock standardHeaderField;

        private string customerKeyField;

        private string customerNameField;

        private string billingAccountKeyField;

        private string contactFirstNameField;

        private string contactSecondNameField;

        private string contactEmailAddressField;

        private string[] contactKeysField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", IsNullable = true, Order = 0)]
        public StandardHeaderBlock standardHeader
        {
            get
            {
                return this.standardHeaderField;
            }
            set
            {
                this.standardHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public string customerKey
        {
            get
            {
                return this.customerKeyField;
            }
            set
            {
                this.customerKeyField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public string customerName
        {
            get
            {
                return this.customerNameField;
            }
            set
            {
                this.customerNameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 3)]
        public string billingAccountKey
        {
            get
            {
                return this.billingAccountKeyField;
            }
            set
            {
                this.billingAccountKeyField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 4)]
        public string contactFirstName
        {
            get
            {
                return this.contactFirstNameField;
            }
            set
            {
                this.contactFirstNameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 5)]
        public string contactSecondName
        {
            get
            {
                return this.contactSecondNameField;
            }
            set
            {
                this.contactSecondNameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 6)]
        public string contactEmailAddress
        {
            get
            {
                return this.contactEmailAddressField;
            }
            set
            {
                this.contactEmailAddressField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Order = 7)]
        [System.Xml.Serialization.XmlArrayItemAttribute("contactKey", IsNullable = false)]
        public string[] contactKeys
        {
            get
            {
                return this.contactKeysField;
            }
            set
            {
                this.contactKeysField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class GetProductInstancesRequest
    {

        private StandardHeaderBlock standardHeaderField;

        private string customerKeyField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", IsNullable = true, Order = 0)]
        public StandardHeaderBlock standardHeader
        {
            get
            {
                return this.standardHeaderField;
            }
            set
            {
                this.standardHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public string customerKey
        {
            get
            {
                return this.customerKeyField;
            }
            set
            {
                this.customerKeyField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class SearchCustomerResponse
    {

        private StandardHeaderBlock standardHeaderField;

        private Customer[] customersField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", IsNullable = true, Order = 0)]
        public StandardHeaderBlock standardHeader
        {
            get
            {
                return this.standardHeaderField;
            }
            set
            {
                this.standardHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Order = 1)]
        [System.Xml.Serialization.XmlArrayItemAttribute("customer", IsNullable = false)]
        public Customer[] customers
        {
            get
            {
                return this.customersField;
            }
            set
            {
                this.customersField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class GetContactsResponse
    {

        private StandardHeaderBlock standardHeaderField;

        private object customerKeyField;

        private Contact[] contactsField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", IsNullable = true, Order = 0)]
        public StandardHeaderBlock standardHeader
        {
            get
            {
                return this.standardHeaderField;
            }
            set
            {
                this.standardHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public object customerKey
        {
            get
            {
                return this.customerKeyField;
            }
            set
            {
                this.customerKeyField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Order = 2)]
        [System.Xml.Serialization.XmlArrayItemAttribute("contact", IsNullable = false)]
        public Contact[] contacts
        {
            get
            {
                return this.contactsField;
            }
            set
            {
                this.contactsField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class GetContactsRequest
    {

        private StandardHeaderBlock standardHeaderField;

        private string customerKeyField;

        private string[] contactKeysField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", IsNullable = true, Order = 0)]
        public StandardHeaderBlock standardHeader
        {
            get
            {
                return this.standardHeaderField;
            }
            set
            {
                this.standardHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public string customerKey
        {
            get
            {
                return this.customerKeyField;
            }
            set
            {
                this.customerKeyField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Order = 2)]
        [System.Xml.Serialization.XmlArrayItemAttribute("contactKey", IsNullable = false)]
        public string[] contactKeys
        {
            get
            {
                return this.contactKeysField;
            }
            set
            {
                this.contactKeysField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class EventResponse
    {

        private StandardHeaderBlock standardHeaderField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", IsNullable = true, Order = 0)]
        public StandardHeaderBlock standardHeader
        {
            get
            {
                return this.standardHeaderField;
            }
            set
            {
                this.standardHeaderField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class OrderEvent
    {

        private StandardHeaderBlock standardHeaderField;

        private EventHeader eventHeaderField;

        private Order orderField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", IsNullable = true, Order = 0)]
        public StandardHeaderBlock standardHeader
        {
            get
            {
                return this.standardHeaderField;
            }
            set
            {
                this.standardHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public EventHeader eventHeader
        {
            get
            {
                return this.eventHeaderField;
            }
            set
            {
                this.eventHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public Order order
        {
            get
            {
                return this.orderField;
            }
            set
            {
                this.orderField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class GetCustomerResponse
    {

        private StandardHeaderBlock standardHeaderField;

        private Customer customerField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", IsNullable = true, Order = 0)]
        public StandardHeaderBlock standardHeader
        {
            get
            {
                return this.standardHeaderField;
            }
            set
            {
                this.standardHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public Customer customer
        {
            get
            {
                return this.customerField;
            }
            set
            {
                this.customerField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class GetCustomerRequest
    {

        private StandardHeaderBlock standardHeaderField;

        private string customerKeyField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", IsNullable = true, Order = 0)]
        public StandardHeaderBlock standardHeader
        {
            get
            {
                return this.standardHeaderField;
            }
            set
            {
                this.standardHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public string customerKey
        {
            get
            {
                return this.customerKeyField;
            }
            set
            {
                this.customerKeyField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://saas.bt.com/v5")]
    public partial class ManageCustomerResponse
    {

        private StandardHeaderBlock standardHeaderField;

        private Order orderField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/", IsNullable = true, Order = 0)]
        public StandardHeaderBlock standardHeader
        {
            get
            {
                return this.standardHeaderField;
            }
            set
            {
                this.standardHeaderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public Order order
        {
            get
            {
                return this.orderField;
            }
            set
            {
                this.orderField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/")]
    public partial class ServiceSecurity
    {

        private string idField;

        private string roleField;

        private string typeField;

        private string authenticationLevelField;

        private string authenticationTokenField;

        private string userEntitlementsField;

        private string tokenExpiryField;

        private string callingApplicationField;

        private string callingApplicationCredentialsField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public string id
        {
            get
            {
                return this.idField;
            }
            set
            {
                this.idField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public string role
        {
            get
            {
                return this.roleField;
            }
            set
            {
                this.roleField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public string type
        {
            get
            {
                return this.typeField;
            }
            set
            {
                this.typeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 3)]
        public string authenticationLevel
        {
            get
            {
                return this.authenticationLevelField;
            }
            set
            {
                this.authenticationLevelField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 4)]
        public string authenticationToken
        {
            get
            {
                return this.authenticationTokenField;
            }
            set
            {
                this.authenticationTokenField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 5)]
        public string userEntitlements
        {
            get
            {
                return this.userEntitlementsField;
            }
            set
            {
                this.userEntitlementsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 6)]
        public string tokenExpiry
        {
            get
            {
                return this.tokenExpiryField;
            }
            set
            {
                this.tokenExpiryField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 7)]
        public string callingApplication
        {
            get
            {
                return this.callingApplicationField;
            }
            set
            {
                this.callingApplicationField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 8)]
        public string callingApplicationCredentials
        {
            get
            {
                return this.callingApplicationCredentialsField;
            }
            set
            {
                this.callingApplicationCredentialsField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/")]
    public partial class ServiceSpecification
    {

        private string payloadFormatField;

        private string versionField;

        private string revisionField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public string payloadFormat
        {
            get
            {
                return this.payloadFormatField;
            }
            set
            {
                this.payloadFormatField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public string version
        {
            get
            {
                return this.versionField;
            }
            set
            {
                this.versionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public string revision
        {
            get
            {
                return this.revisionField;
            }
            set
            {
                this.revisionField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/")]
    public partial class MessageDelivery
    {

        private string messagePersistenceField;

        private string messageRetriesField;

        private string messageRetryIntervalField;

        private string messageQoSField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public string messagePersistence
        {
            get
            {
                return this.messagePersistenceField;
            }
            set
            {
                this.messagePersistenceField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public string messageRetries
        {
            get
            {
                return this.messageRetriesField;
            }
            set
            {
                this.messageRetriesField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public string messageRetryInterval
        {
            get
            {
                return this.messageRetryIntervalField;
            }
            set
            {
                this.messageRetryIntervalField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 3)]
        public string messageQoS
        {
            get
            {
                return this.messageQoSField;
            }
            set
            {
                this.messageQoSField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/")]
    public partial class MessageExpiry
    {

        private string expiryTimeField;

        private string expiryActionField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public string expiryTime
        {
            get
            {
                return this.expiryTimeField;
            }
            set
            {
                this.expiryTimeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public string expiryAction
        {
            get
            {
                return this.expiryActionField;
            }
            set
            {
                this.expiryActionField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/")]
    public partial class ServiceProperties
    {

        private MessageExpiry messageExpiryField;

        private MessageDelivery messageDeliveryField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public MessageExpiry messageExpiry
        {
            get
            {
                return this.messageExpiryField;
            }
            set
            {
                this.messageExpiryField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public MessageDelivery messageDelivery
        {
            get
            {
                return this.messageDeliveryField;
            }
            set
            {
                this.messageDeliveryField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/")]
    public partial class ContextItem
    {

        private string contextIdField;

        private string contextNameField;

        private string valueField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string contextId
        {
            get
            {
                return this.contextIdField;
            }
            set
            {
                this.contextIdField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string contextName
        {
            get
            {
                return this.contextNameField;
            }
            set
            {
                this.contextNameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlTextAttribute()]
        public string Value
        {
            get
            {
                return this.valueField;
            }
            set
            {
                this.valueField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/")]
    public partial class AddressReference
    {

        private string addressField;

        private ContextItem[] contextItemListField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public string address
        {
            get
            {
                return this.addressField;
            }
            set
            {
                this.addressField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Order = 1)]
        [System.Xml.Serialization.XmlArrayItemAttribute("contextItem", IsNullable = false)]
        public ContextItem[] contextItemList
        {
            get
            {
                return this.contextItemListField;
            }
            set
            {
                this.contextItemListField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/")]
    public partial class ServiceAddressing
    {

        private string fromField;

        private AddressReference toField;

        private AddressReference replyToField;

        private string relatesToField;

        private AddressReference faultToField;

        private string messageIdField;

        private string serviceNameField;

        private string actionField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public string from
        {
            get
            {
                return this.fromField;
            }
            set
            {
                this.fromField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public AddressReference to
        {
            get
            {
                return this.toField;
            }
            set
            {
                this.toField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public AddressReference replyTo
        {
            get
            {
                return this.replyToField;
            }
            set
            {
                this.replyToField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 3)]
        public string relatesTo
        {
            get
            {
                return this.relatesToField;
            }
            set
            {
                this.relatesToField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 4)]
        public AddressReference faultTo
        {
            get
            {
                return this.faultToField;
            }
            set
            {
                this.faultToField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 5)]
        public string messageId
        {
            get
            {
                return this.messageIdField;
            }
            set
            {
                this.messageIdField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 6)]
        public string serviceName
        {
            get
            {
                return this.serviceNameField;
            }
            set
            {
                this.serviceNameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 7)]
        public string action
        {
            get
            {
                return this.actionField;
            }
            set
            {
                this.actionField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "3.0.4506.648")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://wsi.nat.bt.com/2005/06/StandardHeader/")]
    public partial class ServiceState
    {

        private string stateCodeField;

        private string errorCodeField;

        private string errorDescField;

        private string errorTextField;

        private string errorTraceField;

        private bool resendIndicatorField;

        private bool resendIndicatorFieldSpecified;

        private string retriesRemainingField;

        private string retryIntervalField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public string stateCode
        {
            get
            {
                return this.stateCodeField;
            }
            set
            {
                this.stateCodeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public string errorCode
        {
            get
            {
                return this.errorCodeField;
            }
            set
            {
                this.errorCodeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public string errorDesc
        {
            get
            {
                return this.errorDescField;
            }
            set
            {
                this.errorDescField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 3)]
        public string errorText
        {
            get
            {
                return this.errorTextField;
            }
            set
            {
                this.errorTextField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 4)]
        public string errorTrace
        {
            get
            {
                return this.errorTraceField;
            }
            set
            {
                this.errorTraceField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 5)]
        public bool resendIndicator
        {
            get
            {
                return this.resendIndicatorField;
            }
            set
            {
                this.resendIndicatorField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool resendIndicatorSpecified
        {
            get
            {
                return this.resendIndicatorFieldSpecified;
            }
            set
            {
                this.resendIndicatorFieldSpecified = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType = "integer", Order = 6)]
        public string retriesRemaining
        {
            get
            {
                return this.retriesRemainingField;
            }
            set
            {
                this.retriesRemainingField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType = "integer", Order = 7)]
        public string retryInterval
        {
            get
            {
                return this.retryIntervalField;
            }
            set
            {
                this.retryIntervalField = value;
            }
        }
    }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "3.0.0.0")]
    [System.ServiceModel.MessageContractAttribute(IsWrapped = false)]
    public partial class manageCustomerProductInstances
    {

        [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://saas.bt.com/v5", Order = 0)]
        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true)]
        public ManageCustomerRequest manageCustomerProductInstancesRequest;

        public manageCustomerProductInstances()
        {
        }

        public manageCustomerProductInstances(ManageCustomerRequest manageCustomerProductInstancesRequest)
        {
            this.manageCustomerProductInstancesRequest = manageCustomerProductInstancesRequest;
        }
    }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "3.0.0.0")]
    [System.ServiceModel.MessageContractAttribute(IsWrapped = false)]
    public partial class manageCustomerProductInstancesResponse
    {

        [System.ServiceModel.MessageBodyMemberAttribute(Name = "manageCustomerProductInstancesResponse", Namespace = "http://saas.bt.com/v5", Order = 0)]
        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true)]
        public ManageCustomerResponse manageCustomerProductInstancesResponse1;

        public manageCustomerProductInstancesResponse()
        {
        }

        public manageCustomerProductInstancesResponse(ManageCustomerResponse manageCustomerProductInstancesResponse1)
        {
            this.manageCustomerProductInstancesResponse1 = manageCustomerProductInstancesResponse1;
        }
    }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "3.0.0.0")]
    [System.ServiceModel.MessageContractAttribute(IsWrapped = false)]
    public partial class getCustomer
    {

        [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://saas.bt.com/v5", Order = 0)]
        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true)]
        public GetCustomerRequest getCustomerRequest;

        public getCustomer()
        {
        }

        public getCustomer(GetCustomerRequest getCustomerRequest)
        {
            this.getCustomerRequest = getCustomerRequest;
        }
    }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "3.0.0.0")]
    [System.ServiceModel.MessageContractAttribute(IsWrapped = false)]
    public partial class getCustomerResponse1
    {

        [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://saas.bt.com/v5", Order = 0)]
        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true)]
        public GetCustomerResponse getCustomerResponse;

        public getCustomerResponse1()
        {
        }

        public getCustomerResponse1(GetCustomerResponse getCustomerResponse)
        {
            this.getCustomerResponse = getCustomerResponse;
        }
    }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "3.0.0.0")]
    [System.ServiceModel.MessageContractAttribute(IsWrapped = false)]
    public partial class orderEvent1
    {

        [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://saas.bt.com/v5", Order = 0)]
        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true)]
        public OrderEvent orderEvent;

        public orderEvent1()
        {
        }

        public orderEvent1(OrderEvent orderEvent)
        {
            this.orderEvent = orderEvent;
        }
    }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "3.0.0.0")]
    [System.ServiceModel.MessageContractAttribute(IsWrapped = false)]
    public partial class orderEventResponse
    {

        [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://saas.bt.com/v5", Order = 0)]
        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true)]
        public EventResponse eventResponse;

        public orderEventResponse()
        {
        }

        public orderEventResponse(EventResponse eventResponse)
        {
            this.eventResponse = eventResponse;
        }
    }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "3.0.0.0")]
    [System.ServiceModel.MessageContractAttribute(IsWrapped = false)]
    public partial class manageCustomerBilling
    {

        [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://saas.bt.com/v5", Order = 0)]
        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true)]
        public ManageCustomerRequest manageCustomerBillingRequest;

        public manageCustomerBilling()
        {
        }

        public manageCustomerBilling(ManageCustomerRequest manageCustomerBillingRequest)
        {
            this.manageCustomerBillingRequest = manageCustomerBillingRequest;
        }
    }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "3.0.0.0")]
    [System.ServiceModel.MessageContractAttribute(IsWrapped = false)]
    public partial class manageCustomerBillingResponse
    {

        [System.ServiceModel.MessageBodyMemberAttribute(Name = "manageCustomerBillingResponse", Namespace = "http://saas.bt.com/v5", Order = 0)]
        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true)]
        public ManageCustomerResponse manageCustomerBillingResponse1;

        public manageCustomerBillingResponse()
        {
        }

        public manageCustomerBillingResponse(ManageCustomerResponse manageCustomerBillingResponse1)
        {
            this.manageCustomerBillingResponse1 = manageCustomerBillingResponse1;
        }
    }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "3.0.0.0")]
    [System.ServiceModel.MessageContractAttribute(IsWrapped = false)]
    public partial class manageCustomer
    {

        [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://saas.bt.com/v5", Order = 0)]
        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true)]
        public ManageCustomerRequest manageCustomerRequest;

        public manageCustomer()
        {
        }

        public manageCustomer(ManageCustomerRequest manageCustomerRequest)
        {
            this.manageCustomerRequest = manageCustomerRequest;
        }
    }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "3.0.0.0")]
    [System.ServiceModel.MessageContractAttribute(IsWrapped = false)]
    public partial class manageCustomerResponse1
    {

        [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://saas.bt.com/v5", Order = 0)]
        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true)]
        public ManageCustomerResponse manageCustomerResponse;

        public manageCustomerResponse1()
        {
        }

        public manageCustomerResponse1(ManageCustomerResponse manageCustomerResponse)
        {
            this.manageCustomerResponse = manageCustomerResponse;
        }
    }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "3.0.0.0")]
    [System.ServiceModel.MessageContractAttribute(IsWrapped = false)]
    public partial class getContacts
    {

        [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://saas.bt.com/v5", Order = 0)]
        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true)]
        public GetContactsRequest getContactsRequest;

        public getContacts()
        {
        }

        public getContacts(GetContactsRequest getContactsRequest)
        {
            this.getContactsRequest = getContactsRequest;
        }
    }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "3.0.0.0")]
    [System.ServiceModel.MessageContractAttribute(IsWrapped = false)]
    public partial class getContactsResponse1
    {

        [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://saas.bt.com/v5", Order = 0)]
        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true)]
        public GetContactsResponse getContactsResponse;

        public getContactsResponse1()
        {
        }

        public getContactsResponse1(GetContactsResponse getContactsResponse)
        {
            this.getContactsResponse = getContactsResponse;
        }
    }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "3.0.0.0")]
    [System.ServiceModel.MessageContractAttribute(IsWrapped = false)]
    public partial class getProductInstances
    {

        [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://saas.bt.com/v5", Order = 0)]
        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true)]
        public GetProductInstancesRequest getProductInstancesRequest;

        public getProductInstances()
        {
        }

        public getProductInstances(GetProductInstancesRequest getProductInstancesRequest)
        {
            this.getProductInstancesRequest = getProductInstancesRequest;
        }
    }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "3.0.0.0")]
    [System.ServiceModel.MessageContractAttribute(IsWrapped = false)]
    public partial class getProductInstancesResponse1
    {

        [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://saas.bt.com/v5", Order = 0)]
        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true)]
        public GetProductInstancesResponse getProductInstancesResponse;

        public getProductInstancesResponse1()
        {
        }

        public getProductInstancesResponse1(GetProductInstancesResponse getProductInstancesResponse)
        {
            this.getProductInstancesResponse = getProductInstancesResponse;
        }
    }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "3.0.0.0")]
    [System.ServiceModel.MessageContractAttribute(IsWrapped = false)]
    public partial class searchCustomer
    {

        [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://saas.bt.com/v5", Order = 0)]
        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true)]
        public SearchCustomerRequest searchCustomerRequest;

        public searchCustomer()
        {
        }

        public searchCustomer(SearchCustomerRequest searchCustomerRequest)
        {
            this.searchCustomerRequest = searchCustomerRequest;
        }
    }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "3.0.0.0")]
    [System.ServiceModel.MessageContractAttribute(IsWrapped = false)]
    public partial class searchCustomerResponse1
    {

        [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://saas.bt.com/v5", Order = 0)]
        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true)]
        public SearchCustomerResponse searchCustomerResponse;

        public searchCustomerResponse1()
        {
        }

        public searchCustomerResponse1(SearchCustomerResponse searchCustomerResponse)
        {
            this.searchCustomerResponse = searchCustomerResponse;
        }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "3.0.0.0")]
    public interface ManageCRMProviderPortTypeChannel : ManageCRMProviderPortType, System.ServiceModel.IClientChannel
    {
    }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "3.0.0.0")]
    public partial class ManageCRMProviderPortTypeClient : System.ServiceModel.ClientBase<ManageCRMProviderPortType>, ManageCRMProviderPortType
    {

        public ManageCRMProviderPortTypeClient()
        {
        }

        public ManageCRMProviderPortTypeClient(string endpointConfigurationName) :
            base(endpointConfigurationName)
        {
        }

        public ManageCRMProviderPortTypeClient(string endpointConfigurationName, string remoteAddress) :
            base(endpointConfigurationName, remoteAddress)
        {
        }

        public ManageCRMProviderPortTypeClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) :
            base(endpointConfigurationName, remoteAddress)
        {
        }

        public ManageCRMProviderPortTypeClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) :
            base(binding, remoteAddress)
        {
        }

        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        manageCustomerProductInstancesResponse ManageCRMProviderPortType.manageCustomerProductInstances(manageCustomerProductInstances request)
        {
            return base.Channel.manageCustomerProductInstances(request);
        }

        public ManageCustomerResponse manageCustomerProductInstances(ManageCustomerRequest manageCustomerProductInstancesRequest)
        {
            manageCustomerProductInstances inValue = new manageCustomerProductInstances();
            inValue.manageCustomerProductInstancesRequest = manageCustomerProductInstancesRequest;
            manageCustomerProductInstancesResponse retVal = ((ManageCRMProviderPortType)(this)).manageCustomerProductInstances(inValue);
            return retVal.manageCustomerProductInstancesResponse1;
        }

        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        getCustomerResponse1 ManageCRMProviderPortType.getCustomer(getCustomer request)
        {
            return base.Channel.getCustomer(request);
        }

        public GetCustomerResponse getCustomer(GetCustomerRequest getCustomerRequest)
        {
            getCustomer inValue = new getCustomer();
            inValue.getCustomerRequest = getCustomerRequest;
            getCustomerResponse1 retVal = ((ManageCRMProviderPortType)(this)).getCustomer(inValue);
            return retVal.getCustomerResponse;
        }

        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        orderEventResponse ManageCRMProviderPortType.orderEvent(orderEvent1 request)
        {
            return base.Channel.orderEvent(request);
        }

        public EventResponse orderEvent(OrderEvent orderEvent1)
        {
            orderEvent1 inValue = new orderEvent1();
            inValue.orderEvent = orderEvent1;
            orderEventResponse retVal = ((ManageCRMProviderPortType)(this)).orderEvent(inValue);
            return retVal.eventResponse;
        }

        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        manageCustomerBillingResponse ManageCRMProviderPortType.manageCustomerBilling(manageCustomerBilling request)
        {
            return base.Channel.manageCustomerBilling(request);
        }

        public ManageCustomerResponse manageCustomerBilling(ManageCustomerRequest manageCustomerBillingRequest)
        {
            manageCustomerBilling inValue = new manageCustomerBilling();
            inValue.manageCustomerBillingRequest = manageCustomerBillingRequest;
            manageCustomerBillingResponse retVal = ((ManageCRMProviderPortType)(this)).manageCustomerBilling(inValue);
            return retVal.manageCustomerBillingResponse1;
        }

        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        manageCustomerResponse1 ManageCRMProviderPortType.manageCustomer(manageCustomer request)
        {
            return base.Channel.manageCustomer(request);
        }

        public ManageCustomerResponse manageCustomer(ManageCustomerRequest manageCustomerRequest)
        {
            manageCustomer inValue = new manageCustomer();
            inValue.manageCustomerRequest = manageCustomerRequest;
            manageCustomerResponse1 retVal = ((ManageCRMProviderPortType)(this)).manageCustomer(inValue);
            return retVal.manageCustomerResponse;
        }

        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        getContactsResponse1 ManageCRMProviderPortType.getContacts(getContacts request)
        {
            return base.Channel.getContacts(request);
        }

        public GetContactsResponse getContacts(GetContactsRequest getContactsRequest)
        {
            getContacts inValue = new getContacts();
            inValue.getContactsRequest = getContactsRequest;
            getContactsResponse1 retVal = ((ManageCRMProviderPortType)(this)).getContacts(inValue);
            return retVal.getContactsResponse;
        }

        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        getProductInstancesResponse1 ManageCRMProviderPortType.getProductInstances(getProductInstances request)
        {
            return base.Channel.getProductInstances(request);
        }

        public GetProductInstancesResponse getProductInstances(GetProductInstancesRequest getProductInstancesRequest)
        {
            getProductInstances inValue = new getProductInstances();
            inValue.getProductInstancesRequest = getProductInstancesRequest;
            getProductInstancesResponse1 retVal = ((ManageCRMProviderPortType)(this)).getProductInstances(inValue);
            return retVal.getProductInstancesResponse;
        }

        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        searchCustomerResponse1 ManageCRMProviderPortType.searchCustomer(searchCustomer request)
        {
            return base.Channel.searchCustomer(request);
        }

        public SearchCustomerResponse searchCustomer(SearchCustomerRequest searchCustomerRequest)
        {
            searchCustomer inValue = new searchCustomer();
            inValue.searchCustomerRequest = searchCustomerRequest;
            searchCustomerResponse1 retVal = ((ManageCRMProviderPortType)(this)).searchCustomer(inValue);
            return retVal.searchCustomerResponse;
        }
    }

}