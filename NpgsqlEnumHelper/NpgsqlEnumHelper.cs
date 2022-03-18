using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using NpgsqlEnumHelper.Attributes;

namespace NpgsqlEnumHelper;

[Generator]
public class PostgresEnumSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<EnumDeclarationSyntax> enumDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (s, _) => IsSyntaxTargetForGeneration(s),
                static (ctx, _) => ctx.GetSemanticTargetForGeneration()
            )
            .Where(static m => m is not null)!;

        IncrementalValueProvider<(Compilation, ImmutableArray<EnumDeclarationSyntax>)> compilationAndEnums
            = context.CompilationProvider.Combine(enumDeclarations.Collect());
        
        context.RegisterSourceOutput(compilationAndEnums, static (spc, source) 
            => Execute(source.Item1, source.Item2, spc));
    }
    
    private static void Execute(Compilation compilation, ImmutableArray<EnumDeclarationSyntax> enums, SourceProductionContext context)
    {
        if (enums.IsDefaultOrEmpty) return;

        var enumsToGenerate = GetTypesToGenerate(compilation, enums.Distinct(), context.CancellationToken);

        if (enumsToGenerate.Count <= 0) return;
        
        var result = GenerateClass(enumsToGenerate);
        context.AddSource("NpgsqlEnumHelper.g.cs", SourceText.From(result, Encoding.UTF8));
    }

    private static string GenerateClass(List<EnumToGenerate> enumsToGenerate)
    {
        var sb = new StringBuilder();
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using Npgsql;");
        sb.AppendLine("using Npgsql.TypeMapping;");
        sb.AppendLine("namespace PostgresEnumHelpers.Generated;\n");
        sb.AppendLine("public static class PostgresEnumExtensions");
        sb.AppendLine("{");

        sb.AppendLine("\tpublic static INpgsqlTypeMapper MapPostgresEnums(this INpgsqlTypeMapper mapper, INpgsqlNameTranslator? translator = null)");
        sb.AppendLine("\t{");
        foreach (var e in enumsToGenerate)
        {
            sb.AppendLine($"\t\tmapper.MapEnum<{e.EnumName}>(translator);");
        }
        sb.AppendLine("\t}");

        sb.AppendLine("\tpublic static void RegisterPostgresEnums(this ModelBuilder builder, string? schema = null, INpgsqlNameTranslator? translator = null)");
        sb.AppendLine("\t{");
        foreach (var e in enumsToGenerate)
        {
            sb.AppendLine($"\t\tbuilder.HasPostgresEnum<{e.EnumName}>(schema, \"{e.Alias ?? "null"}\", translator);");
        }
        sb.AppendLine("\t}");

        sb.Append("}");

        return sb.ToString();
    }

    private static List<EnumToGenerate> GetTypesToGenerate(Compilation compilation, IEnumerable<EnumDeclarationSyntax> enums, CancellationToken ct)
    {
        var enumsToGenerate = new List<EnumToGenerate>();
        
        var enumAttribute = compilation.GetTypeByMetadataName(nameof(NpgsqlEnumAttribute));

        if (enumAttribute is null) return enumsToGenerate;

        foreach (var enumDeclarationSyntax in enums)
        {
            ct.ThrowIfCancellationRequested();

            var semanticModel = compilation.GetSemanticModel(enumDeclarationSyntax.SyntaxTree);
            if (semanticModel.GetDeclaredSymbol(enumDeclarationSyntax) is not INamedTypeSymbol enumSymbol) continue;

            var enumName = enumSymbol.ToString();

            string? alias = null;
            
            var pairs = enumSymbol.GetAttributes()
                .Where(attributeData => enumAttribute.Equals(attributeData.AttributeClass, SymbolEqualityComparer.Default))
                .SelectMany(attributeData => attributeData.NamedArguments);
            
            foreach (var argument in pairs)
            {
                if (argument.Key == nameof(NpgsqlEnumAttribute.Alias) && argument.Value.Value?.ToString() is { } n)
                {
                    alias = n;
                }
            }

            // Create an EnumToGenerate for use in the generation phase
            enumsToGenerate.Add(new EnumToGenerate(enumName, alias));
        }

        return enumsToGenerate;
    }


    private readonly struct EnumToGenerate
    {
        public readonly string EnumName;
        public readonly string? Alias;

        public EnumToGenerate(string enumName, string? alias)
        {
            EnumName = enumName;
            Alias = alias;
        }
    }


    private static bool IsSyntaxTargetForGeneration(SyntaxNode node) => node is EnumDeclarationSyntax { AttributeLists.Count: > 0 };

}