namespace E2KService.ActiveMQ;

public class Subscription : Tuple<Channel, string>, IEquatable<Subscription?>
{
    public Channel Channel => Item1;
    public string MessageType => Item2;

    public Subscription(Channel channel, string msgType) : base(channel, msgType)
    {
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Subscription);
    }

    public bool Equals(Subscription? other)
    {
        return other != null &&
               base.Equals(other) &&
               EqualityComparer<Channel>.Default.Equals(Item1, other.Item1) &&
               Item2 == other.Item2;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), Item1, Item2);
    }

    public static bool operator ==(Subscription? left, Subscription? right)
    {
        return EqualityComparer<Subscription>.Default.Equals(left, right);
    }

    public static bool operator !=(Subscription? left, Subscription? right)
    {
        return !(left == right);
    }
}
