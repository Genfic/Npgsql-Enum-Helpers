using NpgsqlEnumHelper.Attributes;

namespace NpgsqlEnumHelper.Demo;

[NpgsqlEnum("Veggies")]
enum Vegetables
{
    Cat,
    Dog,
    Parrot,
    Tardigrade
}