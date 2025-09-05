using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BT.SaaS.EventServiceProxy
{   
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "3.0.0.0")]
    // [System.Runtime.Serialization.CollectionDataContractAttribute(Name = "activities", Namespace = "http://schemas.datacontract.org/2004/07/BT.SaaS.Core.Shared.Entities", ItemName = "activity")]
    [System.SerializableAttribute()]
    public class activities : System.Collections.Generic.List<int>
    {
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "3.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(ConfigurationName = "EventService.IEventService")]
    public interface IEventService
    {

        [System.ServiceModel.OperationContractAttribute(IsOneWay = true, Action = "http://tempuri.org/IEventService/RaiseEvent")]
        void RaiseEvent(BT.SaaS.Core.Shared.Entities.Event evt);

        [System.ServiceModel.OperationContractAttribute(IsOneWay = true, Action = "http://tempuri.org/IEventService/InsertRequestState")]
        void InsertRequestState(BT.SaaS.Core.Shared.Entities.Event evt);

        [System.ServiceModel.OperationContractAttribute(IsOneWay = true, Action = "http://tempuri.org/IEventService/UpdateRequestState")]
        void UpdateRequestState(BT.SaaS.Core.Shared.Entities.Event evt);

        [System.ServiceModel.OperationContractAttribute(Action = "http://tempuri.org/IEventService/GetEvents", ReplyAction = "http://tempuri.org/IEventService/GetEventsResponse")]
        BT.SaaS.Core.Shared.Entities.Event[] GetEvents(BT.SaaS.Core.Shared.Entities.EventFilter filter);

        [System.ServiceModel.OperationContractAttribute(Action = "http://tempuri.org/IEventService/Subscribe", ReplyAction = "http://tempuri.org/IEventService/SubscribeResponse")]
        BT.SaaS.Core.Shared.Entities.Subscription Subscribe(BT.SaaS.Core.Shared.Entities.Subscription subscription);

        [System.ServiceModel.OperationContractAttribute(Action = "http://tempuri.org/IEventService/UnSubscribe", ReplyAction = "http://tempuri.org/IEventService/UnSubscribeResponse")]
        void UnSubscribe(int subscriptionID);
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "3.0.0.0")]
    public interface IEventServiceChannel : IEventService, System.ServiceModel.IClientChannel
    {
    }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "3.0.0.0")]
    public partial class EventServiceClient : System.ServiceModel.ClientBase<IEventService>, IEventService
    {

        public EventServiceClient()
        {
        }

        public EventServiceClient(string endpointConfigurationName) :
            base(endpointConfigurationName)
        {
        }

        public EventServiceClient(string endpointConfigurationName, string remoteAddress) :
            base(endpointConfigurationName, remoteAddress)
        {
        }

        public EventServiceClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) :
            base(endpointConfigurationName, remoteAddress)
        {
        }

        public EventServiceClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) :
            base(binding, remoteAddress)
        {
        }

        public void RaiseEvent(BT.SaaS.Core.Shared.Entities.Event evt)
        {
            base.Channel.RaiseEvent(evt);
        }

        public void InsertRequestState(BT.SaaS.Core.Shared.Entities.Event evt)
        {
            base.Channel.InsertRequestState(evt);
        }

        public void UpdateRequestState(BT.SaaS.Core.Shared.Entities.Event evt)
        {
            base.Channel.UpdateRequestState(evt);
        }

        public BT.SaaS.Core.Shared.Entities.Event[] GetEvents(BT.SaaS.Core.Shared.Entities.EventFilter filter)
        {
            return base.Channel.GetEvents(filter);
        }

        public BT.SaaS.Core.Shared.Entities.Subscription Subscribe(BT.SaaS.Core.Shared.Entities.Subscription subscription)
        {
            return base.Channel.Subscribe(subscription);
        }

        public void UnSubscribe(int subscriptionID)
        {
            base.Channel.UnSubscribe(subscriptionID);
        }
    }
}
