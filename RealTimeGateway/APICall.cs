using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RealTimeGateway
{
    public enum Stack
    {
        cloud,
        access
    }
    public static class APICall
    {
        /// <summary>
        /// Queries the API to log the user in, Returns a string containing the Session ID, and a bool for if it was succesful
        /// </summary>
        public static string Login(string email, string password, Stack stack)
        {
            string url;
            if (stack == Stack.cloud)
            {
                url = "https://cloud.cimar.co.uk/api/v3/session/login/";
            }
            else
            {
                url = "https://access.cimar.co.uk/api/v3/session/login/";
            }

            var postData = "login=" + Uri.EscapeDataString(email);
            postData += "&password=" + Uri.EscapeDataString(password);

            var serverResponse = APIAndResponseString(postData, url);

            if (serverResponse != null)
            {
                return serverResponse["sid"];
            }
            return null; // is this crashing it when a study has failed and has this change fixed it? (need to turn off internet to test 
        }
        
        public static JObject ReturnStudy(string sid, string uid, Stack stack)
        {
            string url;

            if (stack == Stack.cloud)
            {
                url = "https://cloud.cimar.co.uk/api/v3/study/list";
            }
            else
            {
                url = "https://access.cimar.co.uk/api/v3/study/list";
            }

            string postData = "sid=" + Uri.EscapeDataString(sid);
            postData += "&filter.study_uid.equals=" + Uri.EscapeDataString(uid);
            JObject data = APIAndResponseJObject(postData, url);
            if(data != null)
            {
                return data;
            }

            return null;
        }

        /// <summary>
        /// Calls the API and returns a response as a string
        /// </summary>
        private static Dictionary<string, string> APIAndResponseString(string postData, string url)
        {
            HttpWebResponse response = API(postData, url);
            var serverResponse = ReadServerResponseString(response);

            return serverResponse;
        }
        /// <summary>
        /// Calls the API and returns a response as JSON object
        /// </summary>
        private static JObject APIAndResponseJObject(string postData, string url)
        {
            HttpWebResponse response = API(postData, url);
            var serverResponse = ReadServerResponseJObject(response);

            return serverResponse;
        }
        /// <summary>
        /// Creates the initial web response
        /// </summary>
        private static HttpWebResponse API(string postData, string url)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            try
            {
                var data = Encoding.ASCII.GetBytes(postData);
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = data.Length;

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }
                var response = (HttpWebResponse)request.GetResponse();
                return response;
            }
            catch (Exception ex)
            {
                Support.Print(ex.Message);
            }
            return null;
        }
        /// <summary>
        /// Reads and returns the server response as a string
        /// </summary>
        private static Dictionary<string, string> ReadServerResponseString(HttpWebResponse response)
        {
            if (response != null)
            {
                try
                {
                    using (StreamReader reader = new System.IO.StreamReader(response.GetResponseStream(), Encoding.ASCII))
                    {
                        string responseText = reader.ReadToEnd();
                        JsonConvert.SerializeObject(responseText);
                        var values = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseText);
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            return values;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Support.Print(ex.Message);
                }
            }
            return null;
        }
        /// <summary>
        /// Reads and returns the server response as a JSON Object
        /// </summary>
        private static JObject ReadServerResponseJObject(HttpWebResponse response)
        {
            if (response != null)
            {
                try
                {
                    using (StreamReader reader = new System.IO.StreamReader(response.GetResponseStream(), Encoding.ASCII))
                    {
                        string responseText = reader.ReadToEnd();
                        JsonConvert.SerializeObject(responseText);
                        JObject values = JsonConvert.DeserializeObject<JObject>(responseText);

                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            return values;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Support.Print(ex.Message);
                }
            }
            return null;
        }
    }
}
