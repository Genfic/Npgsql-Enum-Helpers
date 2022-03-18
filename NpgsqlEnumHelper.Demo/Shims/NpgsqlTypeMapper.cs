namespace NpgsqlEnumHelper.Demo.Shims;

public class NpgsqlTypeMapper : INpgsqlTypeMapper
{
    public void MapEnum<TEnum>(INpgsqlNameTranslator? translator)
    {
        Console.WriteLine($"Enum {nameof(TEnum)} registered");
        
        if (translator is null) return;
        
        Console.WriteLine("\t\tTranslator registered");
    }
}