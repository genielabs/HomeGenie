using System.Runtime.CompilerServices;
using MIG;
using Newtonsoft.Json;
using NUnit.Framework;
using ConfigHandler = HomeGenie.Service.Handlers.Config;
using AutomationHandler = HomeGenie.Service.Handlers.Automation;

namespace HomeGenie.Tests;

[TestFixture]
public class LegacyEditorEndpointsTest
{
    [TestCase("Interface.Import")]
    [TestCase("Interface.Install")]
    public void LegacyInterfaceEndpointsRejectBeforeHostAccess(string command)
    {
        var handler = (ConfigHandler)RuntimeHelpers.GetUninitializedObject(typeof(ConfigHandler));
        var request = Request("Config", command);
        Assert.DoesNotThrow(() => handler.ProcessRequest(request));
        AssertDisabled(request);
    }

    [TestCase("Programs.Arduino.FileLoad")]
    [TestCase("Programs.Arduino.FileSave")]
    [TestCase("Programs.Arduino.FileAdd")]
    [TestCase("Programs.Arduino.FileDelete")]
    [TestCase("Programs.Arduino.FileList")]
    public void LegacyArduinoEndpointsRejectBeforeHostAccess(string command)
    {
        var handler = (AutomationHandler)RuntimeHelpers.GetUninitializedObject(typeof(AutomationHandler));
        var request = Request("Automation", command);
        Assert.DoesNotThrow(() => handler.ProcessRequest(request));
        AssertDisabled(request);
    }

    private static MigClientRequest Request(string address, string command)
    {
        return new MigClientRequest(null,
            new MigInterfaceCommand("HomeAutomation.HomeGenie/" + address + "/" + command));
    }

    private static void AssertDisabled(MigClientRequest request)
    {
        Assert.That(request.ResponseData, Is.Not.Null);
        string response = JsonConvert.SerializeObject(request.ResponseData);
        Assert.That(response, Does.Contain("disabled").And.Contain("YoT"));
    }
}
