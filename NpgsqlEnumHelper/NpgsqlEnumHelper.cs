using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

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
        // if (enums.IsDefaultOrEmpty) return;

        var enumsToGenerate = GetTypesToGenerate(compilation, enums.Distinct(), context.CancellationToken);

        // if (enumsToGenerate.Count <= 0) return;

        var result = GenerateClass(enumsToGenerate);
        context.AddSource("NpgsqlEnumHelper.g.cs", SourceText.From(result, Encoding.UTF8));
    }

    private static string GenerateClass((List<EnumToGenerate> Enums, List<string> Log) enumsToGenerate)
    {
        var sb = new StringBuilder();
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using Npgsql;");
        sb.AppendLine("using Npgsql.TypeMapping;");
        sb.AppendLine("namespace PostgresEnumHelpers.Generated;\n");
        sb.AppendLine("public static class PostgresEnumExtensions");
        sb.AppendLine("{");

        sb.AppendLine(
            "\tpublic static INpgsqlTypeMapper MapPostgresEnums(this INpgsqlTypeMapper mapper, INpgsqlNameTranslator? translator = null)");
        sb.AppendLine("\t{");
        foreach (var e in enumsToGenerate.Enums)
        {
            sb.AppendLine($"\t\tmapper.MapEnum<{e.EnumName}>(translator);");
        }

        sb.AppendLine("\t\treturn mapper;");
        sb.AppendLine("\t}");

        sb.AppendLine(
            "\tpublic static void RegisterPostgresEnums(this ModelBuilder builder, string? schema = null, INpgsqlNameTranslator? translator = null)");
        sb.AppendLine("\t{");
        foreach (var e in enumsToGenerate.Enums)
        {
            sb.AppendLine($"\t\tbuilder.HasPostgresEnum<{e.EnumName}>(schema, \"{e.Alias ?? "null"}\", translator);");
        }

        sb.AppendLine("\t}");

        sb.AppendLine("}");

        sb.AppendLine("/*");
        sb.AppendLine($"Generation time: {DateTime.Now}");
        foreach (var log in enumsToGenerate.Log)
        {
            sb.AppendLine(log);
        }

        sb.AppendLine("*/");

        return sb.ToString();
    }

    private static (List<EnumToGenerate> Enums, List<string> Log) GetTypesToGenerate(
        Compilation compilation,
        IEnumerable<EnumDeclarationSyntax> enums,
        CancellationToken ct)
    {
        var enumsToGenerate = new List<EnumToGenerate>();
        var enumDeclarationSyntaxes = enums as EnumDeclarationSyntax[] ?? enums.ToArray();

        var log = new List<string>
        {
            $"Found {enumDeclarationSyntaxes.Length} enums in total:",
            "\t" + string.Join("\n\t", enumDeclarationSyntaxes.Select(eds => eds.Identifier.ToString()))
        };


        foreach (var nts in compilation.GlobalNamespace.GetMembers().SelectMany(m => m.GetTypeMembers()))
        {
            log.Add($"Found symbol {nts.Name} — {nts.MetadataName}");
            log.Add($"\tHas attribute: {nts.GetAttributes().Any()}");
            log.Add("\t\t" + string.Join("\n\t\t\t", nts.GetAttributes().Select(a => a.GetType().Name)));
        }

        var enumAttribute = compilation.GlobalNamespace
            .GetMembers()
            .SelectMany(m => m.GetTypeMembers())
            .FirstOrDefault(m => m.Name == Helpers.AttributeName);

        if (enumAttribute is null)
        {
            log.Add(enumAttribute?.Name ?? "null");
            return (enumsToGenerate, log);
        }

        foreach (var enumDeclarationSyntax in enumDeclarationSyntaxes)
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
                if (argument.Key == "Alias" && argument.Value.Value?.ToString() is { } n)
                {
                    alias = n;
                }
            }

            // Create an EnumToGenerate for use in the generation phase
            enumsToGenerate.Add(new EnumToGenerate(enumName, alias));
        }

        return (enumsToGenerate, log);
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