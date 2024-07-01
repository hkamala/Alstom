namespace ConflictManagementLibrary.Messages
{
    public interface IMessageJson
    {
        string ClassName { get; }
        string MessageName { get; }
        string MessageNumber { get;  }
        string MessageDescription { get; }

    }
}
