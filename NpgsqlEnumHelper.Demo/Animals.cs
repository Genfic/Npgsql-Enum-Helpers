using NpgsqlEnumHelper.Attributes;

namespace NpgsqlEnumHelper.Demo;

[NpgsqlEnum]
enum Animals
{
    Cat,
    Dog,
    Parrot,
    Tardigrade
}