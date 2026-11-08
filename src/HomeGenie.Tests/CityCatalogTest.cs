using System.Linq;
using System.Runtime.CompilerServices;
using HomeGenie.Service;
using MIG;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using ConfigHandler = HomeGenie.Service.Handlers.Config;

namespace HomeGenie.Tests
{
    [TestFixture]
    public class CityCatalogTest
    {
        [TestCase("Roma", "Rome", "IT")]
        [TestCase("Sao Paulo", "São Paulo", "BR")]
        [TestCase("東京", "Tokyo", "JP")]
        [TestCase("Pienza", "Pienza", "IT")]
        public void SearchesBundledNamesAndAliases(string query, string name, string country)
        {
            var city = CityCatalog.Search(query).First();

            Assert.Multiple(() =>
            {
                Assert.That(city.Name, Is.EqualTo(name));
                Assert.That(city.Country, Is.EqualTo(country));
                Assert.That(city.Latitude, Is.InRange(-90d, 90d));
                Assert.That(city.Longitude, Is.InRange(-180d, 180d));
                Assert.That(CityCatalog.Geocode(city.Description).First(), Is.EqualTo(city));
            });
        }

        [Test]
        public void KeepsHomonymsAndWeatherQueriesDistinct()
        {
            var cities = CityCatalog.Search("Springfield")
                .Where(city => city.Name == "Springfield" && city.Country == "US").ToArray();

            Assert.That(cities.Length, Is.GreaterThan(1));
            Assert.That(cities.Select(city => city.WeatherQuery).Distinct().Count(), Is.EqualTo(cities.Length));
            Assert.That(cities.All(city => city.WeatherQuery.EndsWith(", US")), Is.True);
        }

        [Test]
        public void LocationEndpointsWorkWithoutServerConfiguration()
        {
            var handler = (ConfigHandler)RuntimeHelpers.GetUninitializedObject(typeof(ConfigHandler));
            var search = new MigClientRequest(null, new MigInterfaceCommand(
                "HomeAutomation.HomeGenie/Config/System.Configure/Location.Search/Roma"));
            handler.ProcessRequest(search);

            var suggestion = JArray.FromObject(search.ResponseData).First;
            var geocode = new MigClientRequest(null, new MigInterfaceCommand(
                "HomeAutomation.HomeGenie/Config/System.Configure/Location.GeoCode/" + (string)suggestion["description"]));
            handler.ProcessRequest(geocode);
            var position = JArray.FromObject(geocode.ResponseData).First["geometry"]["location"];

            Assert.Multiple(() =>
            {
                Assert.That((string)suggestion["weatherQuery"], Is.EqualTo("Rome, IT"));
                Assert.That((double)position["lat"], Is.EqualTo((double)suggestion["latitude"]));
                Assert.That((double)position["lng"], Is.EqualTo((double)suggestion["longitude"]));
            });
        }

        [TestCase("41.89193,12.51133", "OK", "Rome, Lazio, IT")]
        [TestCase("0,0", "ZERO_RESULTS", null)]
        [TestCase("91,0", "INVALID_REQUEST", null)]
        [TestCase("0,NaN", "INVALID_REQUEST", null)]
        public void ReverseLookupWorksWithoutServerConfiguration(string coordinates, string status, string name)
        {
            var handler = (ConfigHandler)RuntimeHelpers.GetUninitializedObject(typeof(ConfigHandler));
            var request = new MigClientRequest(null, new MigInterfaceCommand(
                "HomeAutomation.HomeGenie/Config/System.Configure/Location.Lookup/" + coordinates));
            handler.ProcessRequest(request);

            var response = JObject.FromObject(request.ResponseData);
            Assert.Multiple(() =>
            {
                Assert.That((string)response["status"], Is.EqualTo(status));
                Assert.That((string)response["name"], Is.EqualTo(name));
            });
        }

        [TestCase("")]
        [TestCase("a")]
        [TestCase("zzzzznotacity")]
        public void ReturnsNoSuggestionsForShortOrUnknownQueries(string query)
        {
            Assert.That(CityCatalog.Search(query), Is.Empty);
        }
    }
}
