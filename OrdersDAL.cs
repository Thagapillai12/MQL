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
using System.Configuration;


namespace BT.SaaS.MSEOAdapter
{
    public class OrdersDAL
    {
        public static List<OrderComplete> DeQueueOrderCompleteOrders(int maxRowCount, string source)
        {
            List<OrderComplete> orders = new List<OrderComplete>();
            Order order = new Order();
            Database db = DatabaseFactory.CreateDatabase();

            using (DbCommand dbCmd = db.GetStoredProcCommand("GetOrderCompleteFromArchieve"))
            {
                db.AddInParameter(dbCmd, "@MaxCount", DbType.Int32, maxRowCount);
                db.AddInParameter(dbCmd, "@ServiceProviderID", DbType.Int32, Convert.ToInt32(Settings1.Default.ConsumerServiceProviderId));
                db.AddInParameter(dbCmd, "@Source", DbType.String, source);
                using (IDataReader dr = db.ExecuteReader(dbCmd))
                {
                    if (dr != null)
                    {
                        while (dr.Read())
                        {
                            OrderComplete orderComplete = new OrderComplete();
                            orderComplete.Order = DeCompressToString((byte[])dr["OrderXML"]);
                            orderComplete.From = dr["SourceName"].ToString();
                            orderComplete.ExceptionString = dr["ErrorDescription"].ToString();
                            orderComplete.ErrorCode = dr["ErrorCode"].ToString();
                            if (Convert.ToInt32(dr["OrderStatusID"]) == 5)
                            {
                                orderComplete.Status = false;
                            }
                            else
                            {
                                orderComplete.Status = true;
                            }
                            orders.Add(orderComplete);
                        }
                    }
                }
            }

            return orders;
        }

        public static List<OrderComplete> DeQueueBulkOrderCompleteOrders(int maxRowCount, string source)
        {
            List<OrderComplete> orders = new List<OrderComplete>();
            Order order = new Order();
            Database db = DatabaseFactory.CreateDatabase();

            using (DbCommand dbCmd = db.GetStoredProcCommand("GetBulkOrderCompleteFromArchieve"))
            {
                db.AddInParameter(dbCmd, "@MaxCount", DbType.Int32, maxRowCount);
                db.AddInParameter(dbCmd, "@ServiceProviderID", DbType.Int32, Convert.ToInt32(Settings1.Default.ConsumerServiceProviderId));
                db.AddInParameter(dbCmd, "@Source", DbType.String, source);
                using (IDataReader dr = db.ExecuteReader(dbCmd))
                {
                    if (dr != null)
                    {
                        while (dr.Read())
                        {
                            OrderComplete orderComplete = new OrderComplete();
                            orderComplete.Order = DeCompressToString((byte[])dr["OrderXML"]);
                            orderComplete.From = dr["SourceName"].ToString();
                            orderComplete.ExceptionString = dr["ErrorDescription"].ToString();
                            orderComplete.ErrorCode = dr["ErrorCode"].ToString();
                            if (Convert.ToInt32(dr["OrderStatusID"]) == 5)
                            {
                                orderComplete.Status = false;
                            }
                            else
                            {
                                orderComplete.Status = true;
                            }
                            orders.Add(orderComplete);
                        }
                    }
                }
            }

            return orders;
        }
        public static int GetBulkOrderCountFromOrders(string source)
        {
            int orderCount = 0;
            Database db = DatabaseFactory.CreateDatabase();

            using (DbCommand dbCmd = db.GetStoredProcCommand("GetBulkOrderCountFromOrders"))
            {
                db.AddInParameter(dbCmd, "@Source", DbType.String, source);
                db.AddOutParameter(dbCmd, "@ordersExist", DbType.Int32, 32);
                db.ExecuteNonQuery(dbCmd);
                orderCount=(int)db.GetParameterValue(dbCmd, "@ordersExist");
            }
            return orderCount;
        }

        public static int ReadAllBytesFromStream(Stream stream, byte[] buffer)
        {
            // Use this method is used to read all bytes from a stream.
            int offset = 0;
            int totalCount = 0;
            using (GZipStream writeStream = new GZipStream(stream, CompressionMode.Decompress))
            {
                while (true)
                {
                    int bytesRead = writeStream.Read(buffer, offset, 100);
                    if (bytesRead == 0)
                    {
                        break;
                    }
                    offset += bytesRead;
                    totalCount += bytesRead;
                }
            }
            return totalCount;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="compressedByte"></param>
        /// <returns></returns>
        static public string DeCompressToString(byte[] compressedByte)
        {

            byte[] decompressed;
            int i = 0;
            using (MemoryStream ms = new MemoryStream(compressedByte))
            {
                byte[] bufferWrite = new byte[4];
                ms.Position = (int)ms.Length - 4;
                ms.Read(bufferWrite, 0, 4);
                ms.Position = 0;
                int bufferLength = BitConverter.ToInt32(bufferWrite, 0);
                decompressed = new byte[bufferLength + 100];
                i = ReadAllBytesFromStream(ms, decompressed);

            }


            StringBuilder sB = new StringBuilder(i);
            for (int j = 0; j < i; j++)
            {
                sB.Append((char)decompressed[j]);
            }
            return sB.ToString();

        }


        //static public string DeCompressToString(byte[] compressedByte)
        //{
        //    byte[] decompressed = new byte[compressedByte.Length * 10];
        //    int i = 0;
        //    using (MemoryStream ms = new MemoryStream(compressedByte))
        //    {
        //        using (GZipStream writeStream = new GZipStream(ms, CompressionMode.Decompress))
        //        {
        //            i = writeStream.Read(decompressed, 0, compressedByte.Length * 10);
        //        }
        //    }
        //    StringBuilder sB = new StringBuilder(i);
        //    for (int j = 0; j < i; j++)
        //    {
        //        sB.Append((char)decompressed[j]);
        //    }
        //    return sB.ToString();
        //}
        //modified CON-73882 - start
        //public static bool InsertKillSessionFailures(string BBSID, string RBSID, string ErrorMessage, string ErrorType, string FailedAt, string OrderType, string OV_VOL_Id, string REASON)
        //{
        //    List<OrderComplete> orders = new List<OrderComplete>();
        //    Order order = new Order();
        //    try
        //    {
        //        Database db = DatabaseFactory.CreateDatabase();

        //        using (DbCommand dbCmd = db.GetStoredProcCommand("InsertKillSession_Failures"))
        //        {
        //            db.AddInParameter(dbCmd, "@BBSID", DbType.String, BBSID);
        //            db.AddInParameter(dbCmd, "@RBSID", DbType.String, RBSID);
        //            db.AddInParameter(dbCmd, "@Error_Message", DbType.String, ErrorMessage);
        //            db.AddInParameter(dbCmd, "@Error_Type", DbType.String, ErrorType);
        //            db.AddInParameter(dbCmd, "@Failed_At", DbType.String, FailedAt);
        //            db.AddInParameter(dbCmd, "@ORDER_TYPE", DbType.String, OrderType);
        //            db.AddInParameter(dbCmd, "@REASON", DbType.String, REASON);
        //            db.AddInParameter(dbCmd, "@OV_VOL_Id", DbType.String, OV_VOL_Id);
        //            db.ExecuteNonQuery(dbCmd);

        //        }
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        return false;
        //    }

        //}
    }
    //modified CON-73882 - end
    public class OrderComplete
    {
        public string Order { get; set; }
        public string From { get; set; }
        public bool Status { get; set; }
        public string ExceptionString { get; set; }
        public string ErrorCode { get; set; }
    }
}
