using Chalmers.ILL.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chalmers.ILL.Tests.Configuration
{
    // appsettings.json + appsettings.{Environment}.json (fas 6, "Sätt upp appsettings.json +
    // miljöspecifika filer"). Loads the real files the same way WebApplication.CreateBuilder does
    // (base file, then an environment-named overlay) rather than mocking them, so a mistake like
    // dropping a key, a wrong "Chillin" section name, or a key that's missing from one of the
    // environment files (and therefore silently falls back to null) would fail here instead of
    // only showing up as a broken localhost/production run.
    [TestClass]
    public class AppSettingsEnvironmentFilesTest
    {
        private static IChillinConfiguration Load(string environmentName)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(System.AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
                .Build();

            return new DefaultChillinConfiguration(configuration, new StubWebHostEnvironment());
        }

        [TestMethod]
        public void Development_OverridesEnvironmentSpecificKeys_WithLocalhostValues()
        {
            var sut = Load("Development");

            Assert.AreEqual("http://localhost:5000/", sut.BaseUrl);
            Assert.AreEqual("localhost", sut.TestServer);
            Assert.AreEqual("127.0.0.1", sut.CronServerIpAddress);
        }

        [TestMethod]
        public void Production_OverridesEnvironmentSpecificKeys_ButNotWithDevelopmentValues()
        {
            var sut = Load("Production");

            Assert.AreNotEqual("http://localhost:5000/", sut.BaseUrl);
            Assert.AreNotEqual("localhost", sut.TestServer);
            Assert.AreNotEqual("127.0.0.1", sut.CronServerIpAddress);
        }

        [TestMethod]
        public void BothEnvironments_StillSeeKeysThatOnlyLiveInTheBaseFile()
        {
            // StorageConnectionString isn't environment-specific and stays in appsettings.json -
            // proves the environment overlay doesn't shadow the base file.
            StringAssert.Contains(Load("Development").StorageConnectionString, "devstoreaccount1");
            StringAssert.Contains(Load("Production").StorageConnectionString, "devstoreaccount1");
        }
    }
}
