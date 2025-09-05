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
using System.Data.Common;
using Microsoft.Practices.EnterpriseLibrary.Data;
using BT.SaaS.IspssAdapter;

namespace BT.SaaS.MSEOAdapter
{
    public class MSEOInboundQueueDAL
    {
        //method need to be moved to InboundQueueDAL
        public static int GetOrderStatus(string orderKey)
        {
            Database db = DatabaseFactory.CreateDatabase();
            int retVal;
            using (DbCommand dbCommand = db.GetStoredProcCommand("GetOrderStatusByOrderKey"))
            {
                db.AddInParameter(dbCommand, "@OrderKey", DbType.String, orderKey);
                db.AddOutParameter(dbCommand, "@OrderStatus", DbType.Int32, 10);
                db.ExecuteNonQuery(dbCommand);
                if (db.GetParameterValue(dbCommand, "OrderStatus") != System.DBNull.Value)
                {
                    retVal = (int)db.GetParameterValue(dbCommand, "OrderStatus");
                }
                else
                {
                    retVal = 0;
                } 
            }
            Logger.Debug("Order Status for orderkey " + orderKey + " is : " + retVal.ToString());
            return retVal;
        }
    }
}
