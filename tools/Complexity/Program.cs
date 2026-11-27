using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// SCRUM-40 · HU-T05 (RNF-007): complejidad ciclomática (McCabe) de cada método, constructor, accesor y lambda
// de nivel superior en los proyectos indicados. Escribe un resumen en Markdown y termina con código 1 si el
// promedio de algún proyecto supera el umbral (10 por defecto).
// Uso: dotnet run --project tools/Complexity -- [--umbral 10] carpeta1 carpeta2 ...

var threshold = 10.0;
var folders = new List<string>();
for (var i = 0; i < args.Length; i++)
{
    if (args[i] == "--umbral") threshold = double.Parse(args[++i], CultureInfo.InvariantCulture);
    else folders.Add(args[i]);
}
if (folders.Count == 0)
{
    Console.Error.WriteLine("Indique las carpetas de los proyectos a analizar.");
    return 2;
}

var es = CultureInfo.GetCultureInfo("es-BO");
var failed = new List<string>();
var worst = new List<(string Project, string Method, int Value)>();
Console.WriteLine($"## Complejidad ciclomática (umbral {threshold.ToString("0.#", es)} por método)");
Console.WriteLine();
Console.WriteLine("| Proyecto | Métodos | Promedio | Máximo | RNF-007 |");
Console.WriteLine("| --- | ---: | ---: | ---: | --- |");
foreach (var folder in folders)
{
    var project = Path.GetFileName(Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar));
    var methods = Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories)
        .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                    && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                    && !f.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}"))
        .SelectMany(f => Measure(CSharpSyntaxTree.ParseText(File.ReadAllText(f)).GetRoot()))
        .ToList();
    if (methods.Count == 0) continue;
    var average = methods.Average(m => m.Value);
    var max = methods.MaxBy(m => m.Value);
    var ok = average <= threshold;
    if (!ok) failed.Add(project);
    worst.AddRange(methods.OrderByDescending(m => m.Value).Take(3).Select(m => (project, m.Name, m.Value)));
    Console.WriteLine($"| {project} | {methods.Count} | {average.ToString("0.00", es)} | {max.Value} ({max.Name}) | {(ok ? "✅" : "❌")} |");
}
Console.WriteLine();
Console.WriteLine("Métodos más complejos (para priorizar refactorizaciones):");
Console.WriteLine();
foreach (var (project, method, value) in worst.OrderByDescending(w => w.Value).Take(8))
    Console.WriteLine($"- `{project}` · `{method}`: {value}");

if (failed.Count > 0)
{
    Console.WriteLine();
    Console.WriteLine($"**Por encima del umbral:** {string.Join(", ", failed)}");
    return 1;
}
return 0;

static IEnumerable<(string Name, int Value)> Measure(SyntaxNode root)
{
    foreach (var node in root.DescendantNodes())
    {
        string? name = node switch
        {
            MethodDeclarationSyntax m => $"{Owner(m)}.{m.Identifier.Text}",
            ConstructorDeclarationSyntax c => $"{Owner(c)}..ctor",
            AccessorDeclarationSyntax a when a.Body is not null || a.ExpressionBody is not null =>
                $"{Owner(a)}.{(a.Parent?.Parent as PropertyDeclarationSyntax)?.Identifier.Text}.{a.Keyword.Text}",
            LocalFunctionStatementSyntax l => $"{Owner(l)}.{l.Identifier.Text}",
            // Las lambdas de los endpoints mínimos (MapGet, MapPost…) son los métodos del servicio.
            LambdaExpressionSyntax lambda when IsEndpoint(lambda) => $"{Owner(lambda)}.{Route(lambda)}",
            _ => null,
        };
        if (name is not null) yield return (name, 1 + Decisions(node));
    }
}

static int Decisions(SyntaxNode body) => body.DescendantNodes(n => n == body || !IsFunction(n)).Count(n => n switch
{
    IfStatementSyntax or WhileStatementSyntax or ForStatementSyntax or ForEachStatementSyntax
        or DoStatementSyntax or CatchClauseSyntax or ConditionalExpressionSyntax
        or CaseSwitchLabelSyntax or CasePatternSwitchLabelSyntax or SwitchExpressionArmSyntax => true,
    BinaryExpressionSyntax b => b.IsKind(SyntaxKind.LogicalAndExpression) || b.IsKind(SyntaxKind.LogicalOrExpression)
                                || b.IsKind(SyntaxKind.CoalesceExpression),
    BinaryPatternSyntax p => p.IsKind(SyntaxKind.AndPattern) || p.IsKind(SyntaxKind.OrPattern),
    _ => false,
});

static bool IsFunction(SyntaxNode n) => n is BaseMethodDeclarationSyntax or LocalFunctionStatementSyntax
    or AccessorDeclarationSyntax || (n is LambdaExpressionSyntax l && IsEndpoint(l));

// lambda → Argument → ArgumentList → Invocation (api.MapGet("/ruta", lambda))
static InvocationExpressionSyntax? EndpointCall(LambdaExpressionSyntax lambda) =>
    lambda.Parent?.Parent?.Parent is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax m } inv
    && m.Name.Identifier.Text.StartsWith("Map", StringComparison.Ordinal) ? inv : null;

static bool IsEndpoint(LambdaExpressionSyntax lambda) => EndpointCall(lambda) is not null;

static string Route(LambdaExpressionSyntax lambda) => EndpointCall(lambda) is { } inv
    ? $"{((MemberAccessExpressionSyntax)inv.Expression).Name.Identifier.Text}({inv.ArgumentList.Arguments.FirstOrDefault()})"
    : "lambda";

static string Owner(SyntaxNode node) =>
    node.Ancestors().OfType<BaseTypeDeclarationSyntax>().FirstOrDefault()?.Identifier.Text ?? "Program";
