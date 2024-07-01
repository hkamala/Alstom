using System;
using System.Collections.Generic;

namespace E2KService.ActiveMQ;

public class Channel : Tuple<ChannelType, string>, IEquatable<Channel?>
{
    public ChannelType ChannelType => Item1;
    public string ChannelName => Item2;

    public Channel(ChannelType type, string channelName) : base(type, channelName)
    {
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Channel);
    }

    public bool Equals(Channel? other)
    {
        return other != null &&
               base.Equals(other) &&
               Item1 == other.Item1 &&
               Item2 == other.Item2;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), Item1, Item2);
    }

    public static bool operator ==(Channel? left, Channel? right)
    {
        return EqualityComparer<Channel>.Default.Equals(left, right);
    }

    public static bool operator !=(Channel? left, Channel? right)
    {
        return !(left == right);
    }
}
