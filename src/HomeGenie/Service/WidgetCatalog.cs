using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Esprima;
using Esprima.Ast;

namespace HomeGenie.Service;

/// <summary>Reads declarative widget capabilities without executing controller code.</summary>
public static class WidgetCatalog
{
    public static bool IsDashboard(string source)
    {
        try
        {
            var controllers = new JavaScriptParser().ParseScript(source).Body.OfType<ClassDeclaration>()
                .Where(c => c.SuperClass is Identifier { Name: "ControllerInstance" }).ToArray();
            if (controllers.Length != 1) return false;
            var field = controllers[0].Body.Body.OfType<PropertyDefinition>()
                .LastOrDefault(p => !p.Static && !p.Computed && Key(p.Key) == "settings");
            if (field?.Value is not ObjectExpression settings ||
                settings.Properties.Any(p => p is not Property property || property.Computed)) return false;
            var size = settings.Properties.OfType<Property>().LastOrDefault(p => Key(p.Key) == "defaultSize");
            return size?.Value is Literal { Value: "dashboard" };
        }
        catch (ParserException) { return false; }
    }

    static string Key(Node node) => node is Identifier identifier ? identifier.Name :
        node is Literal { Value: string text } ? text : null;

    public static string[] ListDashboardWidgets(string root)
    {
        if (!Directory.Exists(root)) return Array.Empty<string>();
        var result = new List<string>();
        var options = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.ReparsePoint };
        foreach (string file in Directory.EnumerateFiles(root, "*.html", options))
        {
            string id = Path.GetRelativePath(root, file)[..^5].Replace('\\', '/');
            if (Path.GetFileName(file).StartsWith(".template.", StringComparison.Ordinal)) continue;
            try
            {
                PackagePathGuard.Contained(root, id + ".html");
                string controller = PackagePathGuard.Contained(root, id + ".js");
                if (File.Exists(controller) && new FileInfo(controller).Length <= 1024 * 1024 &&
                    IsDashboard(File.ReadAllText(controller))) result.Add(id);
            }
            catch (IOException) { } // A widget can be concurrently saved, removed or inaccessible.
            catch (UnauthorizedAccessException) { }
        }
        return result.OrderBy(id => id, StringComparer.Ordinal).ToArray();
    }
}
