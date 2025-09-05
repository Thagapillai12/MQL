using System;
using System.Data;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml.Linq;
using System.Runtime.Serialization;
using BT.SaaS.Core.Shared.Exceptions;

namespace BT.SaaS.IspssAdapter
{
    [Serializable]
    public class CMPSException : SaaSBaseApplicationException
    {

        public CMPSException()
        {
        }

        public CMPSException(string message)
            : base(message)
        {
        }

        public CMPSException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected CMPSException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext)
        {
        }

    }
}

