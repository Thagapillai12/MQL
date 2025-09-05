using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MSEO = BT.SaaS.MSEOAdapter;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;

namespace BT.SaaS.MSEOAdapter
{
    public class VASRequestValidator
    {
        #region ValidateVASEligibilityRequest

        public static bool ValidateVASEligibilityRequest(MSEO.VASServiceEligibilityRequest vasServiceEligibilityRequest, ref string validationException)
        {
            bool validationStatus = true;
            validationException = "success";
            try
            {
                //Validate VASClass List
                if (vasServiceEligibilityRequest.VASClassList == null)
                {
                    throw new Exception("VAS_Class_List_Null");
                }

                //Validate VASClass Name and Value
                foreach (VASClass vasClass in vasServiceEligibilityRequest.VASClassList)
                {
                    if (vasClass.Value == null)
                    {
                        throw new Exception("VASClass_Value_Null");
                    }
                }

                //Validate servicetype                
                if (string.IsNullOrEmpty(vasServiceEligibilityRequest.ServiceType)
                    || string.Equals(vasServiceEligibilityRequest.ServiceType, "all", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(vasServiceEligibilityRequest.ServiceType, "ACTIVATED", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(vasServiceEligibilityRequest.ServiceType, "NONACTIVATED", StringComparison.OrdinalIgnoreCase))
                {
                    if (vasServiceEligibilityRequest.ServiceType != null && (vasServiceEligibilityRequest.ServiceType.ToUpper() == "ACTIVATED" || vasServiceEligibilityRequest.ServiceType.ToUpper() == "NONACTIVATED"))
                    {
                        if (string.IsNullOrEmpty(vasServiceEligibilityRequest.BACID) && string.IsNullOrEmpty(vasServiceEligibilityRequest.CustomerID))
                        {
                            throw new Exception("DNP_Mandatory_Attributes_Null");
                        }
                    }
                }
                else
                {
                    throw new Exception("Invallid_ServiceType_Value");
                }
            }
            catch (Exception exception)
            {
                //Setting validation Status to false in case of Exception.
                validationStatus = false;
                validationException = exception.Message;
            }

            return validationStatus;
        }

        #endregion

        #region ValidateVASActivationRequest

        public static bool ValidateVASActivationRequest(MSEO.OrderRequest orderRequest, ref string validationException)
        {
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
                if (orderRequest.Order.OrderIdentifier == null || string.IsNullOrEmpty(orderRequest.Order.OrderIdentifier.Value))
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
                    if (!string.IsNullOrEmpty(orderRequest.Order.AlternativeIdentifier.Value))
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
                validationStatus = false;
                validationException = exception.Message.ToString();
            }
            return validationStatus;
        }

        #endregion

        #region ValidateVASRequest

        public static bool ValidateVASRequest(MSEO.OrderRequest orderRequest, ref string validationException)
        {
            bool validationStatus = true;
            validationException = "success";
            try
            {
                foreach (MSEO.OrderItem orderItem in orderRequest.Order.OrderItem)
                {
                    InstanceCharacteristic vasInstanceCharacteristic = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(instanceCharacteristic => instanceCharacteristic.Name.Equals("vasclass", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

                    if (vasInstanceCharacteristic != null)
                    {
                        if (vasInstanceCharacteristic.Name == null)
                        {
                            throw new Exception("VASInstanceChar_Name_Null");
                        }
                        else if (vasInstanceCharacteristic.Value == null)
                        {
                            throw new Exception("VASInstanceChar_Value_Null");
                        }
                    }
                    else
                    {
                        throw new Exception("VASClass_InstanceChar_Null");
                    }
                }
            }
            catch (Exception exception)
            {
                //Setting validation Status to false in case of Exception.
                validationStatus = false;
                validationException = exception.Message;
                BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter exception. VAS Request Validation failed : " + exception.ToString());
            }
            return validationStatus;
        }

        #endregion

        #region ValidateVASRegradeRequest
        /// <summary>
        /// Verify the list of Order items and check if the
        /// request contains atleast  one pair of Create-Cancel:
        /// If yes then it is a Regrade request
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static bool IsRegrade(OrderRequest request)
        {
            int cancelCount = 0;
            int createCount = 0;
            bool result;           

            foreach (OrderItem orderItem in request.Order.OrderItem)
            {
                if (orderItem.Action.Code.Equals("cancel", StringComparison.OrdinalIgnoreCase))
                {
                    cancelCount++;
                }
                else if (orderItem.Action.Code.Equals("create", StringComparison.OrdinalIgnoreCase))
                {
                    createCount++;
                }
            }           

            if (cancelCount >= 1 && createCount >= 1)
            {
                result = true;
            }
            else
            {
                result = false;
            }
            return result;
        }

        #endregion

        #region ValidateVASCHOPRegradeRequest
        /// <summary>
        /// Verify the list of Order items and check if the
        /// request contains atleast  one pair of Create-Cancel with service code BT_CONSUMER_BROADBAND:
        /// If yes then it is a CHOP Regrade request
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static bool IsCHOPRegrade(OrderRequest request)
        {
            int cancelCount = 0;
            int createCount = 0;
            bool result;
            foreach (OrderItem orderItem in request.Order.OrderItem)
            {
                if (orderItem.Action.Code.Equals("cancel", StringComparison.OrdinalIgnoreCase))
                {
                    if(orderItem.Instance[0] != null && orderItem.Instance[0].InstanceCharacteristic != null && orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(x => x.Name!= null && x.Name.Equals("ServiceCode",StringComparison.OrdinalIgnoreCase) && x.Value != null && x.Value.Equals("BT_CONSUMER_BROADBAND",StringComparison.OrdinalIgnoreCase)))
                        cancelCount++;
                }
                else if (orderItem.Action.Code.Equals("create", StringComparison.OrdinalIgnoreCase))
                {
                    if (orderItem.Instance[0] != null && orderItem.Instance[0].InstanceCharacteristic != null && orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(x => x.Name != null && x.Name.Equals("ServiceCode", StringComparison.OrdinalIgnoreCase) && x.Value != null && x.Value.Equals("BT_CONSUMER_BROADBAND", StringComparison.OrdinalIgnoreCase)))
                            createCount++;
                }
            }
            if (cancelCount >= 1 && createCount >= 1)
            {
                result = true;
            }
            else
            {
                result = false;
            }
            return result;
        }

        #endregion

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

        # region validateReservationID
        public static string validateReservationID(string input)
        {
            input = input.ToLower();
            //string givenDate = input.Substring(0, 8);
            string emailDataStore = "D&P";

            if ((input.Length == 14 && validateDate(input.Substring(0, 8)) && new Regex("^([0-9]{0,4})([a-z]{0,2})$").IsMatch(input.Substring(8, 6))) || new Regex("^(([0-9a-z])*)_to$").IsMatch(input))
            {
                emailDataStore = "D&P";
                
            }
                return emailDataStore;
            

        }
        # endregion

        #region validateDate
        private static bool validateDate(String givendate)
        {
            try
            {
                System.DateTime d = System.DateTime.ParseExact(givendate, "ddMMyyyy", CultureInfo.InvariantCulture);
                return true;
            }
            catch
            {
                return false;
            }
        }
        #endregion
    }
}
