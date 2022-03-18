// ReSharper disable once CheckNamespace
namespace Npgsql;

public interface INpgsqlTypeMapper
{
    public void MapEnum<TEnum>(INpgsqlNameTranslator? translator);
}