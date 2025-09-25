namespace ITExam.ExternalModels.Auth
{
    namespace ITExam.ExternalModels.Auth
    {
        public class ApiLoginResponse
        {
            public ApiLoginData data { get; set; }
        }

        public class ApiCheckTokenData
        {

        }
        public class ApiLoginData
        {
            public Token token { get; set; }
            public UserInfo user_info { get; set; }
        }

        public class Token
        {
            public string platform { get; set; }
            public string access_token { get; set; }
            public string refresh_token { get; set; }
            public string token_chat { get; set; }
        }

        public class UserInfo
        {
            public int id { get; set; }
            public string username { get; set; }
            public string email { get; set; }
            public string nickname { get; set; }
            public string class_name { get; set; }
            public string major { get; set; }
            public string role { get; set; }
            public string faculty_name { get; set; }
        }
    }

}
