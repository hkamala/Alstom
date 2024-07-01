namespace ConflictManagementLibrary.Communications
{
    public class AppExchangeSettings
    {
        public string MyHostName { get; }
        public int MyHostPort { get; set; }
        public string MyUserName { get; }
        public string MyPassword { get; }
        public bool MyUseAutoRecovery { get; }
        public int MyIntervalAutoRecovery { get; }
        public static AppExchangeSettings CreateInstance(string hostName, string port, string userName, string password, bool useAutoRecovery = true, int intervalAutoRecovery = 5)
        {
            return new AppExchangeSettings(hostName, port, userName, password, useAutoRecovery, intervalAutoRecovery);
        }
        private AppExchangeSettings(string hostName, string port, string userName, string password, bool useAutoRecovery = true, int intervalAutoRecovery = 5)
        {
            MyHostName = hostName;
            int.TryParse(port, out int result);
            MyHostPort = result;
            MyUserName = userName;
            MyPassword = password;
            MyUseAutoRecovery = useAutoRecovery;
            MyIntervalAutoRecovery = intervalAutoRecovery;
        }

    }
}
