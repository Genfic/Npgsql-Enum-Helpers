using Npgsql;
using Npgsql.TypeMapping;
using PostgresEnumHelpers.Generated;

Console.WriteLine("Hello, World!");

INpgsqlTypeMapper mapper = new NpgsqlTypeMapper();
mapper.MapPostgresEnums();

var builder = new ModelBuilder();
builder.RegisterPostgresEnums();