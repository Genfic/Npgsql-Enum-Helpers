using NpgsqlEnumHelper.Attributes;
using NpgsqlEnumHelper.Demo.Shims;

Console.WriteLine("Hello, World!");

var mapper = new NpgsqlTypeMapper();
// mapper.MapPostgresEnums();

[NpgsqlEnum]
enum Animals
{
    Cat,
    Dog,
    Parrot,
    Tardigrade
}