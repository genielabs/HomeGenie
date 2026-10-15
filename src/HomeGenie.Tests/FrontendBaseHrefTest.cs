using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace HomeGenie.Tests
{
    [TestFixture]
    public class FrontendBaseHrefTest
    {
        private static bool HasHomeGenieBaseHref(string html)
        {
            // Comments and inline scripts must not count as real HTML elements.
            html = Regex.Replace(html, @"<!--.*?-->|<script\b[^>]*>.*?</script\s*>", "",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
            var tags = Regex.Matches(html, @"<base\b(?:[^>""']|""[^""]*""|'[^']*')*>",
                RegexOptions.IgnoreCase);
            if (tags.Count != 1) return false;
            var attributes = Regex.Matches(tags[0].Value,
                @"(?:^|\s)href\s*=\s*(?:""(?<value>[^""]*)""|'(?<value>[^']*)'|(?<value>[^\s>]+))",
                RegexOptions.IgnoreCase);
            return attributes.Count == 1 && attributes[0].Groups["value"].Value == "/app/";
        }

        [Test]
        public void BundledFrontendMustUseAppBaseHref()
        {
            var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (root != null && !File.Exists(Path.Combine(root.FullName, "HomeGenie.sln")))
                root = root.Parent;
            Assert.That(root, Is.Not.Null, "Cannot locate HomeGenie repository for frontend validation.");
            var path = Path.Combine(root.FullName, "assets", "build", "all", "app", "index.html");
            Assert.That(File.Exists(path), Is.True, "Bundled frontend index.html is missing: " + path);
            Assert.That(HasHomeGenieBaseHref(File.ReadAllText(path)), Is.True,
                "Release blocked: bundled index.html must contain exactly one <base href=\"/app/\">. " +
                "Rebuild YoT with --base-href /app/ and copy the output into assets/build/all/app.");
        }

        [TestCase("<base href=\"/app/\">", true)]
        [TestCase("<BASE href='/app/'>", true)]
        [TestCase("<base href=/app/>", true)]
        [TestCase("<html><head></head></html>", false)]
        [TestCase("<base href=\"/\">", false)]
        [TestCase("<base href=\"/app\">", false)]
        [TestCase("<base href=\"https://example.com/app/\">", false)]
        [TestCase("<base data-href=\"/app/\">", false)]
        [TestCase("<base>", false)]
        [TestCase("<!-- <base href=\"/app/\"> -->", false)]
        [TestCase("<script>const example = '<base href=\"/app/\">';</script>", false)]
        [TestCase("<base href=\"/app/\"><base href=\"/\">", false)]
        [TestCase("<base href=\"/app/\" href=\"/\">", false)]
        public void ValidateBaseHrefExamples(string html, bool expected)
        {
            Assert.That(HasHomeGenieBaseHref(html), Is.EqualTo(expected));
        }
    }
}
