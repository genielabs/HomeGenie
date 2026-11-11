using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using NUnit.Framework;
using HomeGenie.Service;

namespace HomeGenie.Tests;
[TestFixture]
public class WidgetCatalogTest
{
    [TestCase("class Panel extends ControllerInstance { settings = {defaultSize:'dashboard',sizeOptions:[]}; }", true)]
    [TestCase("class Panel extends ControllerInstance { settings = {'defaultSize':'dashboard'}; }", true)]
    [TestCase("class Panel extends ControllerInstance { settings = {defaultSize:'small'}; } // defaultSize: 'dashboard'", false)]
    [TestCase("const x={defaultSize:'dashboard'}; class Panel extends ControllerInstance {settings={defaultSize:'big'};}", false)]
    [TestCase("class Panel extends ControllerInstance {settings={defaultSize:choose()};}", false)]
    [TestCase("class Panel extends ControllerInstance {settings={defaultSize:'dashboard',...override};}", false)]
    [TestCase("class Panel extends ControllerInstance {static settings={defaultSize:'dashboard'};}", false)]
    [TestCase("class Panel extends ControllerInstance {get settings(){return {defaultSize:'dashboard'};}}", false)]
    [TestCase("thisFunctionMustNeverRun(); class Panel extends ControllerInstance {settings={defaultSize:'dashboard'};}", true)]
    [TestCase("class", false)]
    public void ReadsOnlyDeclarativeControllerSettings(string source, bool expected) =>
        Assert.That(WidgetCatalog.IsDashboard(source), Is.EqualTo(expected));

    [Test] public void ListsNestedCompleteDashboardWidgetsAndIgnoresOtherFiles()
    {
        string root=Path.Combine(Path.GetTempPath(),"hg-widget-catalog-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root,"goals","example"));
        try {
            string panel=Path.Combine(root,"goals","example","panel");
            File.WriteAllText(panel+".html","<div></div>");
            File.WriteAllText(panel+".js","class Panel extends ControllerInstance {settings={defaultSize:'dashboard'};}");
            File.WriteAllText(Path.Combine(root,"ordinary.html"),"<div></div>");
            File.WriteAllText(Path.Combine(root,"ordinary.js"),"class Clock extends ControllerInstance {settings={defaultSize:'small'};}");
            File.WriteAllText(Path.Combine(root,"missing.html"),"<div></div>");
            Assert.That(WidgetCatalog.ListDashboardWidgets(root),Is.EqualTo(new[]{"goals/example/panel"}));
        } finally {Directory.Delete(root,true);}
    }
    [Test]
    public void BundledDashboardsAndTheirThemeWidgetsHaveAllUiTranslations()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !Directory.Exists(Path.Combine(root.FullName, "assets", "build", "all")))
            root = root.Parent;
        Assert.That(root, Is.Not.Null, "HomeGenie source assets");
        var assets = Path.Combine(root.FullName, "assets", "build", "all");
        using var index = JsonDocument.Parse(File.ReadAllText(Path.Combine(assets, "app", "assets", "i18n", "index.json")));
        var languages = index.RootElement.EnumerateObject().Select(language => language.Name).ToArray();
        var widgets = Path.Combine(assets, "data", "widgets");
        var dashboards = WidgetCatalog.ListDashboardWidgets(widgets).ToArray();
        Assert.That(dashboards, Is.Not.Empty);
        var catalogs = dashboards.Select(dashboard => Path.Combine(widgets, dashboard + ".i18n.json"))
            .Concat(dashboards.Select(dashboard => Path.GetDirectoryName(Path.Combine(widgets, dashboard))).Distinct()
                .SelectMany(folder => Directory.EnumerateFiles(folder, "*.i18n.json")))
            .Append(Path.Combine(widgets, "assistants", "genie.i18n.json"))
            .Distinct();

        foreach (var file in catalogs)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(file));
            var entries = document.RootElement.GetProperty("en").EnumerateObject().ToArray();
            Assert.That(entries, Is.Not.Empty, file);
            foreach (var language in languages)
            {
                Assert.That(document.RootElement.TryGetProperty(language, out var catalog), Is.True, $"{file} / {language}");
                foreach (var entry in entries)
                {
                    var location = $"{file} / {language} / {entry.Name}";
                    Assert.That(catalog.TryGetProperty(entry.Name, out var value), Is.True, location);
                    var translation = value.GetString();
                    Assert.That(string.IsNullOrWhiteSpace(translation), Is.False, location);
                    Assert.That(Placeholders(translation), Is.EqualTo(Placeholders(entry.Value.GetString())), location);
                }
            }
        }

        static string[] Placeholders(string text) => Regex.Matches(text, @"\{\w+\}")
            .Select(match => match.Value).OrderBy(value => value, StringComparer.Ordinal).ToArray();
    }

}
