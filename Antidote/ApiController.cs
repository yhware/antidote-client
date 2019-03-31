using System;
using RestSharp;

namespace Antidote
{
    static class ApiController
    {
        private static string API_HOST = AppState.DEV_MODE ? 
            Settings.Default.DEV_API_HOST : Settings.Default.PRD_API_HOST;
       
        private static String SessionCode = "";
        public static RestClient client = new RestClient(API_HOST);
        
        public static void setSessionCode(String sessionCode)
        {
            SessionCode = sessionCode;
        }

        public static RestRequest getLoginReq(String Username, String Password)
        {

            RestRequest req = new RestRequest("login", Method.POST);
            req.AddParameter("Username", Username);
            req.AddParameter("Password", Password);
            req.AddParameter("ComputerCode", AppState.ComputerCode);
            req.AddParameter("ComputerName", "");

            return req;
        }

        public static RestRequest getPwResetReq(String Username, String OldPw, String NewPw)
        {
            RestRequest req = new RestRequest("api/change_pw", Method.POST);
            req.AddParameter("Username", Username);
            req.AddParameter("OldPw", OldPw);
            req.AddParameter("NewPw", NewPw);
            req.AddParameter("ComputerCode", AppState.ComputerCode);

            return req;
        }

        public static RestRequest getAdminLoginReq(string Username, string Password)
        {
            RestRequest req = new RestRequest("api/login", Method.POST);
            req.AddParameter("Username", Username);
            req.AddParameter("Password", Password);

            return req;
        }


        public static RestRequest getComputerRegReq(string ComputerCode, string ComputerName,
            string ComputerLocation)
        {
            string authStr = "Bearer " + AppState.jwt;
            RestRequest req = new RestRequest("api/computer", Method.POST);
            req.AddHeader("Authorization", authStr);
            req.AddParameter("ComputerCode", ComputerCode);
            req.AddParameter("ComputerName", ComputerName);
            req.AddParameter("ComputerLocation", ComputerLocation);

            return req;
        }





    }
}
