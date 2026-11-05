using System;
using System.IO;
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
}
