using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
namespace HomeGenie.Tests;
[TestFixture]
public class ChatAttachmentsTest {
    Type files, message, prompt;
    string root, folder;
    [OneTimeSetUp] public void CompileDistributedStorage() {
        var repo=new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while(repo!=null&&!File.Exists(Path.Combine(repo.FullName,"HomeGenie.sln")))repo=repo.Parent;
        string source=(string)XDocument.Load(Path.Combine(repo.FullName,"assets/build/all/data/packages/homegenie/homegenie-base/programs.xml"))
            .Root.Elements().Single(n=>(string)n.Element("Address")=="577").Element("ScriptContext");
        string shared=(string)XDocument.Load(Path.Combine(repo.FullName,"assets/build/all/data/packages/homegenie/homegenie-base/programs.xml"))
            .Root.Elements().Single(n=>(string)n.Element("Address")=="583").Element("ScriptContext");
        string validation=CSharpSyntaxTree.ParseText("class Holder {"+shared+"}").GetRoot().DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>().Single(n=>n.Identifier.Text=="WidgetContextRequest").ToFullString();
        string code="using System;using System.IO;using System.Linq;using System.Collections.Generic;using Newtonsoft.Json;using Newtonsoft.Json.Linq;"+source+validation;
        var paths=((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator)
            .Where(p=>Path.GetDirectoryName(p)==Path.GetDirectoryName(typeof(object).Assembly.Location))
            .Append(typeof(JsonConvert).Assembly.Location).Append(typeof(Image).Assembly.Location);
        var compilation=CSharpCompilation.Create("ChatFilesProbe"+Guid.NewGuid().ToString("N"),new[]{CSharpSyntaxTree.ParseText(code)},
            paths.Select(p=>MetadataReference.CreateFromFile(p)),new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream=new MemoryStream();var result=compilation.Emit(stream);
        Assert.That(result.Success,Is.True,string.Join("\n",result.Diagnostics));
        var asm=Assembly.Load(stream.ToArray());files=asm.GetType("ChatFiles");message=asm.GetType("ChatMessage");prompt=asm.GetType("WidgetContextRequest");
    }
    [SetUp] public void CreateFolders() {root=Path.Combine(Path.GetTempPath(),"hg-files-"+Guid.NewGuid().ToString("N"));folder=Path.Combine(root,"577");Directory.CreateDirectory(folder);}
    [TearDown] public void Cleanup(){if(Directory.Exists(root))Directory.Delete(root,true);}
    object Rows(JArray rows=null)=>JsonConvert.DeserializeObject((rows??new JArray()).ToString(),typeof(List<>).MakeGenericType(message));
    object Invoke(string method,params object[] args){try{return files.GetMethod(method).Invoke(null,args);}catch(TargetInvocationException ex){throw ex.InnerException;}}
    JObject Handle(string operation,JObject data,string store="933",JArray history=null)=>JObject.FromObject(Invoke("Handle",folder,store,operation,data,Rows(history)));
    static byte[] Png(){using var image=new Image<Rgba32>(2,2);using var stream=new MemoryStream();image.SaveAsPng(stream);return stream.ToArray();}
    JObject Upload(string store="933")=>(JObject)Handle("Upload",new JObject{["name"]="sfondo-தமிழ்.png",["data"]=Convert.ToBase64String(Png())},store)["attachment"];
    JArray History(JObject image)=>new JArray(new JObject{["role"]="user",["parts"]=new JArray(new JObject{["text"]="Describe"},new JObject{["attachment"]=image})});
    [Test] public void UploadUsesGuidAndServerDetectedFormatAndKeepsUnicodeName(){var a=Upload();Assert.That(Guid.TryParseExact((string)a["id"],"N",out _),Is.True);Assert.That((string)a["mimeType"],Is.EqualTo("image/png"));Assert.That((string)a["name"],Does.Contain("தமிழ்"));Assert.That((int)a["width"],Is.EqualTo(2));Assert.That(File.Exists(Path.Combine(root,"933","files",(string)a["id"])),Is.True);}
    [Test] public void BinaryImageCanBeReadAfterLosingAllClientState(){var a=Upload();var b=Handle("Get",new JObject{["id"]=a["id"]});Assert.That(Convert.FromBase64String((string)b["data"]),Is.EqualTo(Png()));}
    [Test] public void CorruptOrOversizedImagesNeverCreateFiles(){Assert.Throws<UnknownImageFormatException>(()=>Handle("Upload",new JObject{["data"]=Convert.ToBase64String(new byte[20])}));Assert.Throws<InvalidOperationException>(()=>Handle("Upload",new JObject{["data"]=new string('A',6*1024*1024)}));Assert.That(Directory.Exists(Path.Combine(root,"933","files")),Is.False);}
    [Test] public void ProviderCannotResolveAnotherProvidersImage(){var a=Upload();Assert.Throws<InvalidOperationException>(()=>Handle("Get",new JObject{["id"]=a["id"]},"940"));}
    [TestCase("../sentinel")] [TestCase("C:\\outside")] [TestCase("/tmp/outside")]
    public void FileIdsCannotBecomePaths(string id){string sentinel=Path.Combine(root,"sentinel");File.WriteAllText(sentinel,"safe");Assert.Throws<InvalidOperationException>(()=>Handle("Get",new JObject{["id"]=id}));Assert.That(File.ReadAllText(sentinel),Is.EqualTo("safe"));}
    [Test] public void ResolveMapsReferencesOnlyInProviderCopyAndPreservesTextAndRepeatedImages(){var a=Upload();var history=History(a);string before=history.ToString();var resolved=Handle("Resolve",new JObject{["messages"]=history});Assert.That((string)resolved["messages"][0]["parts"][0]["text"],Is.EqualTo("Describe"));Assert.That((string)resolved["messages"][0]["parts"][1]["attachment"]["mimeType"],Is.EqualTo("image/png"));Assert.That(history.ToString(),Is.EqualTo(before));Assert.That(history.ToString(),Does.Not.Contain("inlineData"));}
    [Test] public void UsedAttachmentsCannotBeDeletedAsDrafts(){var a=Upload();Assert.Throws<InvalidOperationException>(()=>Handle("Delete",new JObject{["id"]=a["id"]},"933",History(a)));Assert.That(Handle("Get",new JObject{["id"]=a["id"]})["data"],Is.Not.Null);}
    [Test] public void ExportImportIsPortableAndRemapsIdsWithoutEmbeddingBytesInHistory(){var a=Upload();var exported=(JArray)Invoke("Export",folder,"933",Rows(History(a)));Assert.That(exported.ToString(),Does.Contain("data"));var args=new object[]{folder,"940",exported.ToString(),null};var imported=files.GetMethod("Import").Invoke(null,args);var result=JArray.FromObject(imported);string id=(string)result[0]["parts"][1]["attachment"]["id"];Assert.That(id,Is.Not.EqualTo((string)a["id"]));Assert.That(result[0]["parts"][1]["attachment"]["data"],Is.Null);Assert.That(Handle("Get",new JObject{["id"]=id},"940")["data"],Is.Not.Null);}
    [Test] public void FailedImportCleansOnlyItsNewFiles(){var a=Upload();var exported=(JArray)Invoke("Export",folder,"933",Rows(History(a)));exported.Add(new JObject{["role"]="user",["parts"]=new JArray(new JObject{["attachment"]=new JObject{["id"]=Guid.NewGuid().ToString("N"),["data"]="invalid"}})});var args=new object[]{folder,"933",exported.ToString(),null};Assert.Throws<TargetInvocationException>(()=>files.GetMethod("Import").Invoke(null,args));Assert.That(Directory.GetFiles(Path.Combine(root,"933","files")).Length,Is.EqualTo(1));}
    [Test] public void ClearRemovesProviderFilesButPreservesOtherProvidersAndFiles(){Upload();var other=Upload("940");File.WriteAllText(Path.Combine(root,"933","keep.txt"),"safe");Invoke("Clear",folder,"933");Assert.That(Directory.Exists(Path.Combine(root,"933","files")),Is.False);Assert.That(File.ReadAllText(Path.Combine(root,"933","keep.txt")),Is.EqualTo("safe"));Assert.That(Handle("Get",new JObject{["id"]=other["id"]},"940")["data"],Is.Not.Null);}
    [Test] public void LegacyTextHistoryImportsUnchanged(){var args=new object[]{folder,"933",new JArray(new JObject{["role"]="user",["parts"]=new JArray(new JObject{["text"]="hello"})}).ToString(),null};var imported=files.GetMethod("Import").Invoke(null,args);Assert.That((string)JArray.FromObject(imported)[0]["parts"][0]["text"],Is.EqualTo("hello"));}
    [Test] public void DefaultLegacyTextStoreCanStillExportImportAndClear() {
        var legacy=new JArray(new JObject{["role"]="user",["parts"]=new JArray(new JObject{["text"]="old chat"})});
        var exported=(JArray)Invoke("Export",folder,"default",Rows(legacy));
        Assert.That((string)exported[0]["parts"][0]["text"],Is.EqualTo("old chat"));
        var args=new object[]{folder,"default",exported.ToString(),null};
        Assert.That(files.GetMethod("Import").Invoke(null,args),Is.Not.Null);
        Assert.DoesNotThrow(()=>Invoke("Clear",folder,"default"));
    }
    [Test] public void StructuredPromptAcceptsImageOnlyTurnsWithoutChangingTextContract() {
        var request=new JObject{["version"]=1,["message"]="",["attachments"]=new JArray(new JObject{["id"]=Guid.NewGuid().ToString("N")})};
        Assert.DoesNotThrow(()=>prompt.GetMethod("Validate").Invoke(null,new object[]{request}));
        request.Remove("attachments");
        Assert.That(Assert.Throws<TargetInvocationException>(()=>prompt.GetMethod("Validate").Invoke(null,new object[]{request})).InnerException,Is.TypeOf<InvalidOperationException>());
        request["message"]="plain text";
        Assert.DoesNotThrow(()=>prompt.GetMethod("Validate").Invoke(null,new object[]{request}));
    }
    [Test] public void StructuredPromptRejectsPathIdsAndTooManyAttachments() {
        var request=new JObject{["version"]=1,["message"]="photo",["attachments"]=new JArray(new JObject{["id"]="../outside"})};
        Assert.Throws<TargetInvocationException>(()=>prompt.GetMethod("Validate").Invoke(null,new object[]{request}));
        request["attachments"]=new JArray(Enumerable.Range(0,4).Select(_=>new JObject{["id"]=Guid.NewGuid().ToString("N")}));
        Assert.Throws<TargetInvocationException>(()=>prompt.GetMethod("Validate").Invoke(null,new object[]{request}));
    }
}
