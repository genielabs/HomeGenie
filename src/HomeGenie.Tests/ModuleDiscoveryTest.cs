using System;
using System.Linq;
using HomeGenie.Automation;
using HomeGenie.Data;
using HomeGenie.Service;
using MIG.Interfaces.HomeAutomation.Commons;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace HomeGenie.Tests;

[TestFixture]
public class ModuleDiscoveryTest
{
    private static Module Light() => new() { Domain="Test.Lights", Address="1", Name="Porch Light", DeviceType=ModuleTypes.Light };
    private static ProgramBlock Program(bool enabled=true) => new() { Address=145, Name="Smart Device", IsEnabled=enabled,
        ScriptSource="PRIVATE SOURCE MUST NOT LEAK", Data="PRIVATE DATA MUST NOT LEAK",
        Features = new() { new ProgramFeature { ForDomains="", ForTypes="Switch,Light,Dimmer,Color", Property="HomeGenie.SmartLights.Enable", Description="Enable Smart Light Control", FieldType="checkbox" } } };

    [Test] public void DescribeDoesNotInitializeMissingFieldsOrSerializeValuesSourcesAndTimestamps()
    {
        var module=Light();module.Properties.Add(new ModuleParameter { Name="Sensor.Payload", Value=new string('x',100000), Description="Payload" });
        module.Properties.Add(new ModuleParameter { Name="ApiKey",Value="SECRET VALUE",FieldType="password" });
        string before=JsonConvert.SerializeObject(module);
        var description=ModuleDiscovery.Schema(module,new[]{Program()});string json=description.ToString();
        Assert.That(JsonConvert.SerializeObject(module),Is.EqualTo(before));
        Assert.That(description["features"].Any(p=>(string)p["key"]=="HomeGenie.SmartLights.Enable" && (bool)p["present"]==false),Is.True);
        Assert.That(json,Does.Not.Contain("SECRET VALUE").And.Not.Contain("PRIVATE SOURCE").And.Not.Contain("PRIVATE DATA").And.Not.Contain("timestamp").And.Not.Contain("UpdateTime").And.Not.Contain(new string('x',100)));
        Assert.That(description["features"].Single(p=>(string)p["key"]=="ApiKey")["sensitive"].Value<bool>(),Is.True);
    }
    [TestCase("Light",true)] [TestCase("Sensor",false)]
    public void TypeAndDomainFiltersMatchExistingUiRules(string type,bool expected)
    {
        var module=Light();module.DeviceType=Enum.Parse<ModuleTypes>(type);var feature=Program().Features[0];
        Assert.That(ModuleDiscovery.Applies(feature,module),Is.EqualTo(expected));
        feature.ForDomains="Other.Domain";Assert.That(ModuleDiscovery.Applies(feature,module),Is.False);
    }
    [Test] public void PropertyPresenceAndValueFiltersDoNotExposeTheValue()
    {
        var module=Light();var feature=Program().Features[0];feature.ForTypes="Light:Mode=Automatic";
        Assert.That(ModuleDiscovery.Applies(feature,module),Is.False);
        module.Properties.Add(new ModuleParameter{Name="Mode",Value="Manual"});Assert.That(ModuleDiscovery.Applies(feature,module),Is.False);
        module.Properties[0].Value="Automatic";Assert.That(ModuleDiscovery.Applies(feature,module),Is.True);
        var program=Program();program.Features[0]=feature;
        Assert.That(ModuleDiscovery.Schema(module,new[]{program})["properties"].Single(p=>(string)p["key"]=="Mode")["value"],Is.Null);
    }
    [Test] public void ExplicitVirtualFeaturesMatchAnyDeclaredPatternAndOverrideTypeFilters()
    {
        var module=Light();module.DeviceType=ModuleTypes.Sensor;var feature=Program().Features[0];
        Assert.That(ModuleDiscovery.Applies(feature,module,new[]{feature.Property,"Unrelated"}),Is.True);
        Assert.That(ModuleDiscovery.Applies(feature,module,new[]{"Unrelated",feature.Property}),Is.True);
        Assert.That(ModuleDiscovery.Applies(feature,module,new[]{"Unrelated"}),Is.False);
    }
    [Test] public void DisabledOwnerIsNotAnApplicableFeatureAndCatalogCanDescribeItsInactiveDefinition()
    {
        var owner=Program(false);Assert.That((int)ModuleDiscovery.Features(new[]{owner})["total"],Is.Zero);
        Assert.That((int)ModuleDiscovery.Schema(Light(),new[]{owner})["total"],Is.Zero);
        var result=ModuleDiscovery.Features(new[]{owner},includeDisabled:true);
        Assert.That((int)result["total"],Is.EqualTo(1));Assert.That((bool)result["items"][0]["program"]["enabled"],Is.False);
    }
    [Test] public void LiveRegistrationAndUpdatesAreReflectedWithoutASeparateCatalog()
    {
        var owner=Program();owner.Features.Add(new ProgramFeature{ForTypes="Light",Property="New.Feature",Description="New option",FieldType="slider:0:100:5"});
        Assert.That((int)ModuleDiscovery.Features(new[]{owner},Light())["total"],Is.EqualTo(2));
        owner.Features.RemoveAt(0);Assert.That((int)ModuleDiscovery.Features(new[]{owner},Light())["total"],Is.EqualTo(1));
    }
    [Test] public void PagingAndSearchAreDeterministicAndBounded()
    {
        var owner=Program();owner.Features.Add(new ProgramFeature{ForTypes="Light",Property="Another.Option",Description="Different option"});
        var page=ModuleDiscovery.Features(new[]{owner},offset:0,limit:1);Assert.That((int)page["total"],Is.EqualTo(2));Assert.That((int)page["nextOffset"],Is.EqualTo(1));
        Assert.That((string)page["items"][0]["property"],Is.EqualTo("Another.Option"));
        Assert.That((int)ModuleDiscovery.Features(new[]{owner},query:"different")["total"],Is.EqualTo(1));
        Assert.That((int)ModuleDiscovery.Features(new[]{owner},programId:999)["total"],Is.Zero);
        Assert.Throws<ArgumentException>(()=>ModuleDiscovery.Features(new[]{owner},limit:201));
        Assert.Throws<ArgumentException>(()=>ModuleDiscovery.Schema(Light(),new[]{owner},offset:-1));
    }
    [TestCase("ConfigureOptions.ApiKey","")] [TestCase("Auth.AccessToken","")] [TestCase("ArbitraryName","password")]
    public void SensitiveFieldsAreIdentified(string key,string type)=>Assert.That(ModuleDiscovery.Sensitive(key,type),Is.True);

    [Test] public void TargetedReadsAreBoundedAndSensitiveDefinitionsAreRespected()
    {
        var module=Light();var owner=Program();
        module.Properties.Add(new ModuleParameter{Name="Status.Level",Value="100"});
        module.Properties.Add(new ModuleParameter{Name="Large",Value=new string('x',4097)});
        module.Properties.Add(new ModuleParameter{Name="Hidden",Value="PRIVATE"});
        owner.Features.Add(new ProgramFeature{Property="Hidden",FieldType="password"});
        Assert.That((string)ModuleDiscovery.ReadParameter(module,new[]{owner},"Status.Level")["value"],Is.EqualTo("100"));
        Assert.Throws<InvalidOperationException>(()=>ModuleDiscovery.ReadParameter(module,new[]{owner},"Hidden"));
        Assert.Throws<InvalidOperationException>(()=>ModuleDiscovery.ReadParameter(module,new[]{owner},"Large"));
        int count=module.Properties.Count;
        Assert.That((bool)ModuleDiscovery.ReadParameter(module,new[]{owner},"Missing")["present"],Is.False);
        Assert.That(module.Properties.Count,Is.EqualTo(count));
    }
    [Test] public void SchemaClassifiesByFieldTypeAndNeverNestsFeaturesInsideProperties() {
        var module=Light();var owner=Program();
        module.Properties.Add(new ModuleParameter{Name="Status.Level",Value="42"});
        module.Properties.Add(new ModuleParameter{Name="Local.Setting",FieldType="text",Value="PRIVATE"});
        var second=Program();second.Address=146;
        owner.Features.Add(new ProgramFeature{Property="Declared.Property",FieldType="",ForTypes="Light"});
        var schema=ModuleDiscovery.Schema(module,new[]{owner,second});
        Assert.That(schema["properties"].All(p=>string.IsNullOrWhiteSpace((string)p["fieldType"]) && p["features"]==null),Is.True);
        Assert.That(schema["features"].All(p=>!string.IsNullOrWhiteSpace((string)p["fieldType"]) && p["features"]==null),Is.True);
        var feature=schema["features"].Single(p=>(string)p["key"]=="HomeGenie.SmartLights.Enable");
        Assert.That(feature["programs"].Count(),Is.EqualTo(2));
        Assert.That(schema["properties"].Select(p=>(string)p["key"]).Intersect(schema["features"].Select(p=>(string)p["key"])),Is.Empty);
        Assert.That(schema.ToString(),Does.Not.Contain("PRIVATE"));
        Assert.That((int)ModuleDiscovery.ProgramFeatures(new[]{owner},null,145)["total"],Is.EqualTo(1));
    }
    [Test] public void ProgramViewsSupportCatalogExactFeatureDisabledOwnersAndRejectUnknownTargets() {
        var owner=Program(false);
        Assert.That((int)ModuleDiscovery.ProgramFeatures(new[]{owner},null)["total"],Is.Zero);
        Assert.That((int)ModuleDiscovery.ProgramFeatures(new[]{owner},null,145)["total"],Is.EqualTo(1));
        Assert.That((int)ModuleDiscovery.ProgramFeatures(new[]{owner},null,145,"HomeGenie.SmartLights.Enable")["total"],Is.EqualTo(1));
        Assert.Throws<ArgumentException>(()=>ModuleDiscovery.ProgramFeatures(new[]{owner},null,999));
        Assert.Throws<ArgumentException>(()=>ModuleDiscovery.ProgramFeatures(new[]{owner},null,145,"Missing"));
        Assert.Throws<ArgumentException>(()=>ModuleDiscovery.ProgramFeatures(new[]{owner},"{\"source\":true}"));
        Assert.Throws<ArgumentException>(()=>ModuleDiscovery.ProgramFeatures(new[]{owner},"{\"programId\":146}",145));
    }
}
