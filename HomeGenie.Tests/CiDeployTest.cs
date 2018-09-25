using System;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;
using NUnit.Framework;

using HomeGenie.Service;

// Continuous Integration Deploy Tests (Travis / AppVeyor)

namespace HomeGenie.Tests
{
    [TestFixture]
    public class CiDeployTest
    {
        [Test]
        public void Test1()
        {
            Assert.True(true);
        }

        [Test]
        public void CheckDeployVersionTest()
        {
            string releaseFile = Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "HomeGenie", "bin", "Debug", "release_info.xml");
            var releaseInfo = UpdateChecker.GetReleaseFile(releaseFile);
            Assert.NotNull(releaseInfo);
            // check for $TRAVIS_TAG or APPVEYOR_REPO_TAG_NAME
            // to determine if this is a new release, if so
            // then update the version string in `release_info.xml` file
            string releaseTag = Environment.GetEnvironmentVariable("TRAVIS_TAG");
            if (releaseTag == null) releaseTag = Environment.GetEnvironmentVariable("APPVEYOR_REPO_TAG_NAME");
            if (!string.IsNullOrEmpty(releaseTag))
            {
                Assert.True(releaseTag.StartsWith("v"));
                releaseInfo.Version = releaseTag;
                releaseInfo.ReleaseDate = DateTime.UtcNow;
                releaseInfo.Description = "HomeGenie "+releaseTag;
                XmlSerializer serializer = new XmlSerializer(typeof(ReleaseInfo)); 
                using (TextWriter writer = new StreamWriter(releaseFile))
                {
                    serializer.Serialize(writer, releaseInfo); 
                }                 
            }
            Assert.True(true);
        }
    }
}
