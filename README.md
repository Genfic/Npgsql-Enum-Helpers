# Npgsql Enum Helpers

Source generator that generates code used to register and map enums to
[NpgSQL](https://github.com/npgsql/npgsql)

## Usage

1. Add the `[NpgsqlEnum]` attribute to selected enums

```cs
[NpgsqlEnum]
enum Animals
{
    Cat,
    Dog,
    Parrot,
    Tardigrade
}
```

2. Call `MapPostgresEnums()` on ` NpgsqlConnection.GlobalTypeMapper`

```cs
public MyDbContext(DbContextOptions options) : base(options)
{
    NpgsqlConnection.GlobalTypeMapper.MapPostgresEnums(); 👈
}
```

3. Call `RegisterPostgresEnums()` on your `ModelBuilder`

```cs
protected override void OnModelCreating(ModelBuilder builder)
{
    base.OnModelCreating(builder);

    builder.RegisterPostgresEnums(); 👈
}
```