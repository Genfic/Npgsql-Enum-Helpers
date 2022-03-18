namespace NpgsqlEnumHelper.Demo.Shims;

public interface INpgsqlTypeMapper
{
    public void MapEnum<TEnum>(INpgsqlNameTranslator? translator);
}