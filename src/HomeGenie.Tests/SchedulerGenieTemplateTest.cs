using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using Jint;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace HomeGenie.Tests;
[TestFixture]
public class SchedulerGenieTemplateTest {
 string template;
 readonly List<string> calls=new();
 [OneTimeSetUp] public void LoadShippedTemplate() {
  var root=new DirectoryInfo(TestContext.CurrentContext.TestDirectory);while(root!=null&&!File.Exists(Path.Combine(root.FullName,"HomeGenie.sln")))root=root.Parent;
  template=(string)JArray.Parse(File.ReadAllText(Path.Combine(root.FullName,"assets/build/all/app/assets/scheduler/preset-actions.json"))).OfType<JObject>().Single(t=>(string)t["id"]=="genie_command")["script"];
 }
 [SetUp] public void Reset()=>calls.Clear();
 Engine Engine(Func<string,string,object> call) {
  var engine=new Engine();engine.SetValue("apiCall",call);engine.Execute("var $$ = {api: {call: apiCall}};");return engine;
 }
 string Code(string provider="933")=>template.Replace("$request$",new JObject{["version"]=1,["providerId"]=provider,["prompt"]="Read state"}.ToString(Formatting.None));

 [TestCase(true)] [TestCase(false)] public void NativeWaitResponseAllowsFollowingCommandOnlyAfterCompletion(bool objects) {
  using var engine=Engine((route,body)=> {
   calls.Add(route);var request=JObject.Parse(body);
   Assert.That((bool)request["wait"],Is.True);Assert.That((int)request["timeoutSeconds"],Is.EqualTo(600));
   var result=new JObject{["version"]=1,["requestId"]="job",["state"]="completed"};return objects?result:result.ToString();
  });
  engine.Execute(Code()+"var nextCommand=true;");Assert.That(engine.GetValue("nextCommand").AsBoolean(),Is.True);Assert.That(calls.Count,Is.EqualTo(1));
 }
 [TestCase("failed")] [TestCase("cancelled")] [TestCase("busy")] [TestCase("output_limit")] [TestCase("context_limit")] [TestCase("not_found")] [TestCase("cancelling")]
 public void NonCompletedNativeResultStopsFollowingCommandsWithoutReplay(string state) {
  using var engine=Engine((route,body)=>{calls.Add(route);return new JObject{["version"]=1,["requestId"]="job",["state"]=state,["error"]="No completion"};});
  Assert.Catch(()=>engine.Execute(Code()+"var nextCommand=true;"));Assert.That(calls.Count,Is.EqualTo(1));Assert.That(engine.GetValue("nextCommand").IsUndefined(),Is.True);
 }
 [Test] public void NativeExceptionStopsFollowingCommandsWithoutReplay() {
  using var engine=Engine((route,body)=>{calls.Add(route);throw new ThreadInterruptedException();});
  Assert.Catch(()=>engine.Execute(Code()+"var nextCommand=true;"));Assert.That(calls.Count,Is.EqualTo(1));Assert.That(engine.GetValue("nextCommand").IsUndefined(),Is.True);
 }
 [Test] public void MultipleGenieActionsHaveIndependentScopesAndProviders() {
  var providers=new List<string>();
  using var engine=Engine((route,body)=> {providers.Add((string)JObject.Parse(body)["providerId"]);return new JObject{["version"]=1,["requestId"]="job",["state"]="completed"};});
  engine.Execute(Code()+Code("940"));Assert.That(providers,Is.EqualTo(new[]{"933","940"}));
 }
}
