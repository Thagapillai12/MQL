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
using MSEO = BT.SaaS.MSEOAdapter;
using System.Globalization;


namespace BT.SaaS.MSEOAdapter
{
    public class RequestValidator
    {
       

        public static bool ValidateMSEORequest(MSEO.OrderRequest orderRequest, ref string validationException)
        {
            BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("Validate Request Called");
            bool validationStatus = true;
            validationException = "success";
            try
            {
                if (orderRequest.StandardHeader == null)
                {
                    throw new Exception("Standard_Header_Null");
                }
                //Validate Order exists
                if (orderRequest.Order == null)
                {
                    throw new Exception("Order_Null");
                }
                //Validate OrderIdentifier 
                if (orderRequest.Order.OrderIdentifier == null || string.IsNullOrEmpty(orderRequest.Order.OrderIdentifier.Value) )
                {
                    throw new Exception("Order_Identifier_Null");
                }
                //Validate length of the OrderIdentifier 
                if ((orderRequest.Order.OrderIdentifier.Value != null) && (orderRequest.Order.OrderIdentifier.Value.Length > 40))
                {
                    throw new Exception("Order_Identifier_Length_Error");
                }
                //Validate length of the AlternativeIdentifier 
                if ((orderRequest.Order.AlternativeIdentifier != null))
                {
                    if (!string.IsNullOrEmpty(orderRequest.Order.AlternativeIdentifier.Value ))
                    {
                        if ((orderRequest.Order.AlternativeIdentifier.Value.Length > 40))
                        {
                            throw new Exception("Order_AlternateIdentifier_Length_Error");
                        }
                    }
                }
                //Validate OrderDate               
                if (orderRequest.Order.OrderDate != null)
                {
                    //Validate date format
                    if (orderRequest.Order.OrderDate.DateTime1 == null || !validateDateTime(orderRequest.Order.OrderDate.DateTime1.ToString()))
                    {
                        throw new Exception("Invalid_DateTime_Format");
                    }
                }
                else
                {
                    throw new Exception("Invalid_DateTime_Format");
                }
                
                //Validate OrderItem 
                if (orderRequest.Order.OrderItem == null)
                {
                    throw new Exception("Order_OrderItem_Null");
                }

                //OrderItem[] OrderItemArr = orderRequest.Order.OrderItem;
                foreach (OrderItem OrderItemArr in orderRequest.Order.OrderItem)
                {
                    if (OrderItemArr.Action == null)
                    {
                        throw new Exception("OrderItem_Action_Null");
                    }
                    else
                    {
                        //Only Create, Delete and Amend are expected values.
                        if (OrderItemArr.Action.Code == null)
                        {
                            throw new Exception("Invalid_OrderItem_Action");
                        }
                    }
                    //Validate OrderItem Identifier 
                    if (OrderItemArr.Identifier == null)
                    {
                        throw new Exception("OrderItem_Identifier_Null");
                    }

                    if (OrderItemArr.RequiredDateTime != null)
                    {
                        //Validate date format
                        if (OrderItemArr.RequiredDateTime.DateTime1 != null)
                        {
                            if (OrderItemArr.RequiredDateTime.DateTime1 != string.Empty && !validateDateTime(OrderItemArr.RequiredDateTime.DateTime1.ToString()))
                            {
                                throw new Exception("Invalid_DateTime_Format");
                            }
                        }
                    }

                    //Validate OrderItem Specification 

                    if (OrderItemArr.Specification == null)
                    {
                        throw new Exception("OrderItem_Specification_Null");
                    }
                    else
                    {
                        //Validate OrderItem Specification Name (Product SCode).

                        if (string.IsNullOrEmpty(OrderItemArr.Specification[0].Identifier.Value1))
                        {
                            throw new Exception("Invalid_OrderItem_Specification");
                        }
                        else if (OrderItemArr.Specification[0].Identifier.Value1.Trim().Length > 255)
                        {
                            throw new Exception("OrderItem_Specification_Length_Error");
                        }
                    }

                    //Validate Instance
                    if (OrderItemArr.Instance == null)
                    {
                        throw new Exception("OrderItem_Instance_Null");
                    }

                    //Get first Order Item Instance
                    //Instance[] InstanceArr = OrderItemArr[0].Instance;

                    foreach (Instance InstanceArr in OrderItemArr.Instance)
                    {
                        if (InstanceArr.InstanceIdentifier == null || InstanceArr.InstanceIdentifier.Value == null)
                        {
                            if (!(orderRequest.Order.OrderItem[0].Specification[0].Identifier.Value1.Equals(ConfigurationManager.AppSettings["BTSpringProductCode"].ToString())
                                && orderRequest.Order.OrderIdentifier.Value.StartsWith("EE", StringComparison.OrdinalIgnoreCase)))
                            {
                                throw new Exception("OrderItem_InstanceIdentifier_Null");
                            }
                        }
                        if (!(orderRequest.Order.OrderItem[0].Specification[0].Identifier.Value1.Equals(ConfigurationManager.AppSettings["BTSpringProductCode"].ToString())
                                && orderRequest.Order.OrderIdentifier.Value.StartsWith("EE", StringComparison.OrdinalIgnoreCase)))
                        {
                            if (InstanceArr.InstanceIdentifier.Value.Trim().Length > 30)
                            {
                                throw new Exception("OrderItem_InstanceIdentifier_Length_Error");
                            }
                        }
                        if (InstanceArr.InstanceAction != null)
                        {
                            if (InstanceArr.InstanceAction.Code == null)
                            {
                                throw new Exception("Invalid_OrderItem_Instance_Action");
                            }
                        }
                        
                        //Validate InstanceCharacteristic Name and Value
                        foreach (InstanceCharacteristic insChar in InstanceArr.InstanceCharacteristic)
                        {
                            if (insChar.Name == null)
                            {
                                throw new Exception("InstanceChar_Name_Null");
                            }
                            else if (insChar.Value == null)
                            {
                                throw new Exception("InstanceChar_Value_Null");
                            }
                            else
                            {
                                if (insChar.Name.Length > 260)
                                {
                                    throw new Exception("Invalid_InstanceChar_Name_Length");
                                }
                                if (insChar.Value.Length > 1300)
                                {
                                    throw new Exception("Invalid_InstanceChar_Value_Length");
                                }
                            }
                        }
                    }
                }

            }
           
            catch (Exception exception)
            {
                //Setting validation Status to false in case of Exception.
                validationStatus = false;
                validationException = exception.Message ;
                BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter exception. Validation failed : " + exception.ToString());
            }
           
            return validationStatus;
        }


        #region validateDateTime(string input,)
        /// <summary>
        /// This method is to validate date
        /// </summary>
        /// <param name="input">OrderDate in OrderRequest</param>
        /// <returns>bool</returns>
        private static bool validateDateTime(string input)
        {
          
            try
            {
                System.DateTime validObj = new System.DateTime();
                try
                {
                    System.DateTime winEpoch = new System.DateTime(1970, 01, 01);
                    long epochTime = (Convert.ToInt64(input) * 10000000) + winEpoch.Ticks;
                    validObj = new System.DateTime(epochTime);

                }
                catch
                {
                    validObj = System.DateTime.Parse(input);

                }
                if (validObj != null)
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
            return false;
        }
        #endregion 

        public static bool ValidateESBROBTORequest(MSEO.OrderRequest orderRequest, ref string validationException)
        {
            BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("Validate Request Called");
            bool validationStatus = true;
            validationException = "success";
            try
            {
                //OrderItem[] OrderItemArr = orderRequest.Order.OrderItem;
                foreach (OrderItem OrderItemArr in orderRequest.Order.OrderItem)
                {
                    foreach (Instance InstanceArr in OrderItemArr.Instance)
                    {
                        if (!InstanceArr.InstanceCharacteristic.ToList().Exists(insChar => insChar.Name.Equals("EmailName", StringComparison.OrdinalIgnoreCase)))
                        {
                            throw new Exception("Ignored as EmailName is not present in request.");
                        }
                        else if (!InstanceArr.InstanceCharacteristic.ToList().Exists(insChar => insChar.Name.Equals("Reason", StringComparison.OrdinalIgnoreCase)))
                        {
                            throw new Exception("Ignored as Reason is not present in request.");
                        }
                        else if (!InstanceArr.InstanceCharacteristic.ToList().Exists(insChar => insChar.Name.Equals("EmailDomain", StringComparison.OrdinalIgnoreCase)))
                        {
                            throw new Exception("Ignored as EmailDomain is not present in request.");
                        }
                        if (InstanceArr.InstanceCharacteristic.ToList().Exists(insChar => insChar.Name.Equals("CompromisedDate", StringComparison.OrdinalIgnoreCase)))
                        {
                            string str = InstanceArr.InstanceCharacteristic.ToList().Where(insChar => insChar.Name.Equals("CompromisedDate", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                            System.DateTime dateTime;
                            if (!System.DateTime.TryParseExact(str, @"dd/MM/yyyy HH:mm:ss",
                                            CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime))
                            {
                                throw new Exception("Ignored as Invalid_CompromisedDate_Format");
                            }
                        }
                        else
                        {
                            throw new Exception("Ignored as CompromisedDate is not present in request.");
                        }

                    }
                }

            }

            catch (Exception exception)
            {
                //Setting validation Status to false in case of Exception.
                validationStatus = false;
                validationException = exception.Message;
                BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter exception. Validation failed : " + exception.ToString());
            }

            return validationStatus;
        }
    }
}
