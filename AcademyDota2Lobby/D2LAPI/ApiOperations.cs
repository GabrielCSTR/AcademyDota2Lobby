using D2LAPI.Modal;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace D2LAPI
{
    public class ApiOperations
    {
        /**
         * Base Url @string
         */
        private string baseUrl;

        public ApiOperations()
        {
            this.baseUrl = "http://localhost:8000/v1/api/";
        }

        /**
         * Authenticate user with Web Api Endpoint
         * @param string username
         * @param string password
         */
        public User AuthenticateUser(string email, string password)
        {
            string endpoint = this.baseUrl + "login";
            string method = "POST";

            string json = GetJson(email, password);

            WebClient wc = new WebClient();
            wc.Headers["Content-Type"] = "application/json";
            try
            {
                var response = wc.UploadString(endpoint, method, json);
                return JsonConvert.DeserializeObject<User>(response);
            }
            catch (WebException ex)
            {
#if DEBUG
                //throw ex;
#endif
                // pegando responsta 
                string responseText = string.Empty;

                var responseStream = ex.Response?.GetResponseStream();

                if (responseStream != null)
                {
                    using (var reader = new StreamReader(responseStream))
                    {
                        responseText = reader.ReadToEnd();
                    }
                }

                if (responseText != string.Empty)
                {
                    // enviando resposta + status
                    var message = JsonConvert.DeserializeObject<httpResponseAPI>(responseText);

                    string responseAPI = JsonConvert.SerializeObject(new
                    {
                        Response = new
                        {
                            StatusCode = ((HttpWebResponse)ex.Response).StatusCode,
                            Message = message.Message,
                            Error = message.Error
                        },
                    });

                    // return 
                    return JsonConvert.DeserializeObject<User>(responseAPI);
                }
                else
                {
                    string responseAPI = JsonConvert.SerializeObject(new
                    {
                        Response = new
                        {
                            StatusCode = 500,
                            Error = "O servidor se encontra offline no momento, tente novamente mas tarde!"
                        },
                    });

                    // return 
                    return JsonConvert.DeserializeObject<User>(responseAPI);
                }


            }
        }

        private static string GetJson(string email, string password)
        {
            return JsonConvert.SerializeObject(new
            {
                email = email,
                password = password
            });
        }

        public List<BotD2> BotD2Handler(User _currentUser)
        {
            string endpoint = this.baseUrl + "botleague";
            string json = JsonConvert.SerializeObject(new
            {
                access_token = _currentUser.access_token
            });

            WebClient wc = new WebClient();
            wc.Headers["Content-Type"] = "application/json";
            wc.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + _currentUser.access_token);
            try
            {
                string response = wc.DownloadString(endpoint); // GET
                //string response = wc.UploadString(endpoint, method); // POST
                return JsonConvert.DeserializeObject<List<BotD2>>(response);
            }
            catch (WebException ex)
            {
                // pegando responsta 
                string responseText = string.Empty;

                var responseStream = ex.Response?.GetResponseStream();

                if (responseStream != null)
                {
                    using (var reader = new StreamReader(responseStream))
                    {
                        responseText = reader.ReadToEnd();
                    }
                }

                if (responseText != string.Empty)
                {
                    // enviando resposta + status
                    var message = JsonConvert.DeserializeObject<httpResponseAPI>(responseText);

                    string responseAPI = JsonConvert.SerializeObject(new
                    {
                        Response = new
                        {
                            StatusCode = ((HttpWebResponse)ex.Response).StatusCode,
                            Message = message.Message,
                            Error = message.Error
                        },
                    });

                    // return 
                    return JsonConvert.DeserializeObject<List<BotD2>>(responseAPI);
                }
                else
                {
                    string responseAPI = JsonConvert.SerializeObject(new
                    {
                        Response = new
                        {
                            StatusCode = 500,
                            Message = "O servidor se encontra offline no momento, tente novamente mas tarde!"
                        },
                    });

                    // return 
                    return JsonConvert.DeserializeObject<List<BotD2>>(responseAPI);
                }
            }

        }
    }
}
