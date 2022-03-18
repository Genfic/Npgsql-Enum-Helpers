namespace NpgsqlEnumHelper.Attributes;

[AttributeUsage(AttributeTargets.Enum)]
public class NpgsqlEnumAttribute : Attribute
{
    public string? Alias { get; set; }

    public NpgsqlEnumAttribute(string alias) => Alias = alias;
    public NpgsqlEnumAttribute() => Alias = null;
}