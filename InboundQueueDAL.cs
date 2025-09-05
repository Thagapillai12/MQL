using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BT.SaaS.Core.Shared.Entities;
using System.Data.Common;
using Microsoft.Practices.EnterpriseLibrary.Data;
using System.Data;
using System.IO;
using System.IO.Compression;
using BT.SaaS.IspssAdapter.BOS_API;


namespace BT.SaaS.IspssAdapter
{
    public class InboundQueueDAL
    {
        public static bool IsOrderExists(string orderKey)
        {           
            Database db = DatabaseFactory.CreateDatabase();
            bool retVal = false;
            using (DbCommand dbCmd = db.GetStoredProcCommand("IsOrderExists"))
            {
                db.AddInParameter(dbCmd, "@OrderKey", DbType.String, orderKey);
                db.AddInParameter(dbCmd, "@Source", DbType.String, "BOS");
                db.AddOutParameter(dbCmd, "@isExists", DbType.Boolean, 10);
                db.ExecuteNonQuery(dbCmd);
                retVal = (bool)db.GetParameterValue(dbCmd, "isExists");
            }

            if (retVal)
            {
                Logger.Debug("ORN="+orderKey+" Duplicate order detected. Ignoring and returning success");
            }
            else
            {
                Logger.Debug("ORN=" + orderKey + " This is not a duplicate order");
            }

            return retVal;
        }

        public static bool IsOrderExists(string orderKey, string source)
        {
            Database db = DatabaseFactory.CreateDatabase();
            bool retVal = false;
            using (DbCommand dbCmd = db.GetStoredProcCommand("IsOrderExists"))
            {
                db.AddInParameter(dbCmd, "@OrderKey", DbType.String, orderKey);
                db.AddInParameter(dbCmd, "@Source", DbType.String, source);
                db.AddOutParameter(dbCmd, "@isExists", DbType.Boolean, 10);
                db.ExecuteNonQuery(dbCmd);
                retVal = (bool)db.GetParameterValue(dbCmd, "isExists");
            }

            if (retVal)
            {
                Logger.Debug("ORN=" + orderKey + " Duplicate order detected. Ignoring and returning success");
            }
            else
            {
                Logger.Debug("ORN=" + orderKey + " This is not a duplicate order");
            }

            return retVal;
        }

        public static bool GetResetPasswordData(string userData)
        {
            Database db = DatabaseFactory.CreateDatabase();
            bool retVal = false;
            using (DbCommand dbCmd = db.GetStoredProcCommand("GetResetPasswordData"))
            {
                db.AddInParameter(dbCmd, "@UserCredentialData", DbType.String, userData);
                db.AddOutParameter(dbCmd, "@DoDataExist", DbType.Boolean, 10);
                db.ExecuteNonQuery(dbCmd);
                retVal = (bool)db.GetParameterValue(dbCmd, "DoDataExist");
            }

            Logger.Debug("UserData Exist : " + retVal.ToString());            

            return retVal;
        }

        public static void QueueRawXML(string orderKey, int serviceProviderId, string payload, string errorcode, string errordescription)
        {
            Database db = DatabaseFactory.CreateDatabase();
            using (DbCommand dbCommand = db.GetStoredProcCommand("QueueRawXML"))
            {
                db.AddInParameter(dbCommand, "@OrderKey", DbType.String, orderKey);
                db.AddInParameter(dbCommand, "@ServiceProviderID", DbType.Int32, serviceProviderId);
                db.AddInParameter(dbCommand, "@XMLPayload", DbType.Xml, payload);
                db.AddInParameter(dbCommand, "@errorcode", DbType.String, errorcode);
                db.AddInParameter(dbCommand, "@errordescription", DbType.String, errordescription);
                db.ExecuteNonQuery(dbCommand);
            }
        }
        public static void UpdateQueueRawXML(string errorcode, string errordescription, string orderKey)
        {
            Database db = DatabaseFactory.CreateDatabase();
            using (DbCommand dbCommand = db.GetStoredProcCommand("sp_errorcodedescriptionupdate"))
            {

                db.AddInParameter(dbCommand, "@Errorcode", DbType.String, errorcode);
                db.AddInParameter(dbCommand, "@Errordescription", DbType.String, errordescription);
                db.AddInParameter(dbCommand, "@OrderKey", DbType.String, orderKey);
                db.ExecuteNonQuery(dbCommand);
            }
        }

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
                    retVal = -1;
                }
            }
            Logger.Debug("Order Status : " + retVal.ToString());
            return retVal;
        }
    }
}
