using System.Collections.Generic;
using System.Linq;
using System;

namespace ConflictManagementService.Model;

public class ElementPosition : IEquatable<ElementPosition?>
{
    public string ElementId => elementId;
    public uint Offset => offset;
    public long AdditionalPos => additionalPos;
    public string AdditionalName => additionalName;		// Platform, timing point etc.

    private readonly string elementId = "";
    private readonly uint offset = 0;
    private readonly string additionalName = "";
    private readonly long additionalPos = 0;

    public ElementPosition()
    {
    }

    public ElementPosition(string elementId, uint offset, long additionalPos, string additionalName = "")
    {
        this.elementId = elementId;
        this.offset = offset;
        this.additionalPos = additionalPos;
        this.additionalName = additionalName;
    }

    public bool IsValid()
    {
        return ElementId != "";
    }

    public override string ToString()
    {
        return string.Format($"ElementId = '{ElementId}', Offset = {Offset}, AdditionalPos = {AdditionalPos}, AdditionalName = '{AdditionalName}'");
    }

    public string GetEdgePosIdentifier()
    {
        return string.Format($"{ElementId}({AdditionalPos})");
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as ElementPosition);
    }

    public bool Equals(ElementPosition? other)
    {
        // Do not take AdditionalName into consideration
        return other is not null &&
               elementId == other.elementId &&
               offset == other.offset &&
               additionalPos == other.additionalPos;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ElementId, Offset, AdditionalPos);
    }

    public static bool operator ==(ElementPosition? left, ElementPosition? right)
    {
        return left is not null && left.Equals(right);
    }

    public static bool operator !=(ElementPosition? left, ElementPosition? right)
    {
        return !(left == right);
    }
}

////////////////////////////////////////////////////////////////////////////////

public class ElementExtension : IEquatable<ElementExtension?>
{
    public ElementPosition StartPos => startPos;
    public ElementPosition EndPos => endPos;
    public List<string> Elements => elements;

    private ElementPosition startPos = new();
    private ElementPosition endPos = new();
    private List<string> elements = new();

    public ElementExtension()
    {
    }

    public ElementExtension(ElementPosition startPos, ElementPosition endPos, List<string> elements)
    {
        this.startPos = startPos;
        this.endPos = endPos;
        this.elements = elements;

        if (!IsValid())
            throw new Exception(string.Format($"Invalid element extension: {this}"));
    }

    public bool IsValid()
    {
        return StartPos.IsValid() && EndPos.IsValid() && Elements.Count != 0 && Elements.First() == StartPos.ElementId && Elements.Last() == EndPos.ElementId;
    }

    public override string ToString()
    {
        string s = string.Format($"Start: [{StartPos}] End: [{EndPos}] Elements:");
        foreach (var edge in Elements)
            s += " " + edge;
        return s;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as ElementExtension);
    }

    public bool Equals(ElementExtension? other)
    {
        return other is not null &&
               startPos.Equals(other.startPos) &&
               endPos.Equals(other.endPos) &&
               elements.SequenceEqual(other.elements);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(StartPos, EndPos, Elements);
    }

    public static bool operator ==(ElementExtension? left, ElementExtension? right)
    {
        return left is not null && left.Equals(right);
    }

    public static bool operator !=(ElementExtension? left, ElementExtension? right)
    {
        return !(left == right);
    }
}
