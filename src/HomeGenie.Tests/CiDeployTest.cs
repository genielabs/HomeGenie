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
            Assert.Pass();
        }

        [Test]
        public void CheckDeployVersionTest()
        {
            string releaseFile = Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "HomeGenie", "release_info.xml");
            var releaseInfo = UpdateChecker.GetReleaseFile(releaseFile);
            Assert.That(releaseInfo, Is.Not.Null);
            // check for $TRAVIS_TAG or APPVEYOR_REPO_TAG_NAME
            // to determine if this is a new release, if so
            // then update the version string in `release_info.xml` file
            string releaseTag = Environment.GetEnvironmentVariable("RELEASE_VERSION");
            if (!string.IsNullOrEmpty(releaseTag))
            {
                Assert.That(releaseTag.StartsWith("v"),  Is.True);
                releaseInfo.Version = releaseTag;
                // add 15 minutes to prevent github release date
                // be greater than actual release build date
                releaseInfo.ReleaseDate = DateTime.UtcNow.AddHours(0.25);
                releaseInfo.Description = "HomeGenie " + releaseTag;
                XmlSerializer serializer = new XmlSerializer(typeof(ReleaseInfo));
                using (TextWriter writer = new StreamWriter(releaseFile))
                {
                    serializer.Serialize(writer, releaseInfo);
                }
            }
            Assert.Pass();
        }
    }
}
