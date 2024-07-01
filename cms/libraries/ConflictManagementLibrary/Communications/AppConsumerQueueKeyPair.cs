namespace ConflictManagementLibrary.Communications
{
    public class AppConsumerQueueKeyPair
    {
        public string MyQueueName { get; }
        public string MyKeyName { get; }

        private AppConsumerQueueKeyPair(string queueName, string keyName)
        {
            MyQueueName = queueName;
            MyKeyName = keyName;
        }

        public static AppConsumerQueueKeyPair CreateInstance(string queueName, string keyName)
        {
            return new AppConsumerQueueKeyPair(queueName, keyName);
        }
    }
}
