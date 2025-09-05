using System;
using System.Net;
using System.Configuration;
using System.IO;
using System.Xml.Serialization;
using BT.SaaS.IspssAdapter;
using System.Text;


namespace BT.SaaS.MSEOAdapter
{
    public sealed class DnpRestCallWrapper
    {
        public static serviceResponse GetInviterolesfromDNP(string urlKey)
        {
            serviceResponse serviceresponse;
            try
            {
                string responseXml;
                HttpWebRequest webrequest;

                webrequest = (HttpWebRequest)WebRequest.Create(urlKey);
                webrequest.Method = "GET";
                webrequest.ContentType = "text/xml";
                webrequest.Accept = "application/xml";
                webrequest.MediaType = "application/xml";
                webrequest.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["CMPSTimeOut"].ToString());
                webrequest.Headers.Add("SystemCd", "SAASMSEO");

                //CredentialCache cred = new CredentialCache();
                //NetworkCredential mycred = new NetworkCredential(ConfigurationManager.AppSettings["DnPUser"].ToString(), ConfigurationManager.AppSettings["DnPPassword"].ToString());
                //cred.Add(new Uri(urlKey), "Basic", mycred);
                //webrequest.Credentials = cred;
                
                NetworkCredential mycred = new NetworkCredential(ConfigurationManager.AppSettings["DnPUser"].ToString(), ConfigurationManager.AppSettings["DnPPassword"].ToString());
                if (mycred != null && !string.IsNullOrEmpty(mycred.UserName) && !string.IsNullOrEmpty(mycred.Password))
                {
                    webrequest.Headers.Add(HttpRequestHeader.Authorization, "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(mycred.UserName + ":" + mycred.Password)));
                }

                using (HttpWebResponse webResponse = (HttpWebResponse)webrequest.GetResponse())
                {
                    using (StreamReader readStream = new StreamReader(webResponse.GetResponseStream()))
                    {
                        responseXml = readStream.ReadToEnd();
                        readStream.Close();
                    }
                    webResponse.Close();
                }

                using (StringReader reader = new StringReader(responseXml))
                {
                    XmlSerializer serailizer = new XmlSerializer(typeof(serviceResponse));
                    serviceresponse = (serviceResponse)serailizer.Deserialize(reader);
                    reader.Close();
                }
            }
            catch (WebException DnpEx)
            {
                if (DnpEx.Response != null)
                {
                    string statusCode = string.Empty;
                    string responseXml = string.Empty;
                    using (HttpWebResponse errorResponse = (HttpWebResponse)DnpEx.Response)
                    {
                        statusCode = errorResponse.StatusCode.ToString();
                        using (StreamReader readStream = new StreamReader(errorResponse.GetResponseStream()))
                        {
                            responseXml = readStream.ReadToEnd();
                            readStream.Close();
                        }
                        errorResponse.Close();
                    }
                    if (DnpEx.Message.Contains("404") || responseXml.ToLower().StartsWith("no data") || responseXml.ToLower().StartsWith("no records"))
                        return null;
                    else
                        throw new DnpException("DNPException: " + statusCode + " : " + responseXml);
                }
                else
                {
                    throw new DnpException("DNPException: " + DnpEx.Message);
                }
            }
            catch (Exception dnpException)
            {

                throw new DnpException("DNPException: " + dnpException.ToString());
            }
            return serviceresponse;
        }

        public static string DoRESTCall(string method,
                                     string url, ref string updatedE2EDataValue,
                                     bool acceptXml,
                                     NetworkCredential networkCredentials,
                                     string requestXml,
                                     string systemCode)
        {
            string result = string.Empty;
            // Capture REST call start time
            var restCallStartTime = System.DateTime.Now;

            try
            {
                HttpWebRequest webrequest = (HttpWebRequest)WebRequest.Create(url);
                webrequest.Method = method;
                webrequest.ContentLength = 0;
                webrequest.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["RestCallTimeout"].ToString());
                // If Accept type is passed, then add to request, else not needed
                if (acceptXml)
                    webrequest.Accept = "application/xml";

                // If Network credentials are passed, then add to request, else not needed
                if (networkCredentials != null && !string.IsNullOrEmpty(networkCredentials.UserName) && !string.IsNullOrEmpty(networkCredentials.Password))
                {
                    webrequest.Headers.Add(HttpRequestHeader.Authorization, "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(networkCredentials.UserName + ":" + networkCredentials.Password)));
                }
                //Added this system as DNP is expecting a mandatory attribute from CCP 45
                if (!string.IsNullOrEmpty(systemCode) && systemCode.Trim().Length > 0)
                {
                    webrequest.Headers.Add("SystemCd", systemCode);
                }

                if ((!string.IsNullOrEmpty(systemCode)) && (systemCode.ToUpper() == "SAASMSEO"))//As DNP is expecting e2eData variable value as btE2EData, hence added check based on systemCode.
                {
                    webrequest.Headers.Add("btE2EData", updatedE2EDataValue);
                }
                else
                {
                    webrequest.Headers.Add("e2eData", updatedE2EDataValue);
                }
                // If RequestXml are passed, then add to request, else not needed
                if (!string.IsNullOrEmpty(requestXml))
                {
                    if (method.ToUpper() == "POST" || method.ToUpper() == "PUT")
                    {
                        webrequest.ContentType = "application/xml; charset=utf8";
                    }
                    byte[] requestBytes = System.Text.Encoding.UTF8.GetBytes(requestXml);
                    webrequest.ContentLength = requestBytes.Length;
                    using (Stream requestStream = webrequest.GetRequestStream())
                    {
                        requestStream.Write(requestBytes, 0, requestBytes.Length);
                        requestStream.Close();
                    }
                }

                using (var webResponse = (HttpWebResponse)webrequest.GetResponse())
                {
                    if (webResponse.StatusCode == HttpStatusCode.Accepted || webResponse.StatusCode == HttpStatusCode.OK || webResponse.StatusCode == HttpStatusCode.Created)//added webResponse.StatusCode == HttpStatusCode.Created, as esb sends 201 ok response for ManageServiceRoleRequest
                    {
                        // Logic to validate respose code
                        using (StreamReader readStream = new StreamReader(webResponse.GetResponseStream()))
                        {
                            if (readStream != null)
                            {
                                result = readStream.ReadToEnd();
                                if (string.IsNullOrEmpty(result))
                                {
                                    //Added this condition due to empty response from DNP for UPDATE calls. (As the response header is 200 ok with no response body)  
                                    result = "UPDATERESTCALLSUCCESS";
                                }
                            }
                            readStream.Close();
                            readStream.Dispose();
                        }

                        // Log REST call response
                        //LogHelper.LogRESTCallResponse(objRestLog, url, method, (int)webResponse.StatusCode, result, (DateTime.Now - restCallStartTime));
                    }
                    else
                    {
                        result = "RESTCALLFAILURE";
                        // Log REST call response
                        //LogHelper.LogRESTCallResponse(objRestLog, url, method, (int)webResponse.StatusCode, result, (DateTime.Now - restCallStartTime), System.Diagnostics.TraceEventType.Error);
                    }
                    webResponse.Close();
                }

            }
            catch (WebException webEx)
            {
                int statusCode = -1;

                if (webEx.Response != null)
                {
                    using (HttpWebResponse errorResponse = (HttpWebResponse)webEx.Response)
                    {
                        if (errorResponse != null)
                        {
                            statusCode = (int)errorResponse.StatusCode;
                            using (StreamReader readStream = new StreamReader(errorResponse.GetResponseStream()))
                            {
                                if (readStream != null)
                                {
                                    result = string.Concat(readStream.ReadToEnd(), statusCode, "RESTCALLFAILURE");
                                }
                                readStream.Close();
                            }
                            result = string.IsNullOrEmpty(result) ? string.Format("REST call failed with status code - {0}RESTCALLFAILURE", (int)errorResponse.StatusCode) : result;
                            // Log REST call response
                            //LogHelper.LogRESTCallResponse(objRestLog, url, method, (int)errorResponse.StatusCode, result, (DateTime.Now - restCallStartTime), System.Diagnostics.TraceEventType.Error);
                        }
                        else
                        {
                            // Log REST call response
                            //LogHelper.LogRESTCallResponse(objRestLog, url, method, statusCode, webEx.ToString(), (DateTime.Now - restCallStartTime), System.Diagnostics.TraceEventType.Error);
                        }
                        errorResponse.Close();
                    }
                }
                else
                {
                    // Log REST call response
                    result = string.Format("{0}RESTCALLFAILURE", webEx.Message);
                    //LogHelper.LogRESTCallResponse(objRestLog, url, method, statusCode, webEx.ToString(), (DateTime.Now - restCallStartTime), System.Diagnostics.TraceEventType.Error);
                }
            }
            catch (Exception ex)
            {
                result = string.Format("{0}RESTCALLFAILURE", ex.Message);

                //objRestLog.CallingFunctionName += "-Exception";
                // Log REST call response
                //LogHelper.LogRESTCallResponse(objRestLog, url, method, -1, ex.ToString(), (DateTime.Now - restCallStartTime), System.Diagnostics.TraceEventType.Error);
            }
            return result;
        }
    }
}
