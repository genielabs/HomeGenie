using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace HomeGenie.Service
{
    internal static class CityCatalog
    {
        private const string ResourceName = "HomeGenie.CityCatalog.tsv.gz";
        private const int ResultLimit = 10;
        private static readonly Lazy<byte[]> Catalog = new Lazy<byte[]>(Load);

        internal static IReadOnlyList<City> Search(string input)
        {
            if (string.IsNullOrWhiteSpace(input) || input.Length > 256)
                return Array.Empty<City>();

            string query = Normalize(input.Trim());
            if (query.Length < 2)
                return Array.Empty<City>();

            // The catalog is ordered by population. Prefer exact matches before prefixes.
            var exact = new List<ReadOnlyMemory<byte>>(ResultLimit);
            var prefixes = new List<ReadOnlyMemory<byte>>(ResultLimit);
            var aliasPrefixes = new List<ReadOnlyMemory<byte>>(ResultLimit);
            byte[] key = Encoding.UTF8.GetBytes(query);
            byte[] aliasPrefix = Encoding.UTF8.GetBytes("," + query);
            byte[] exactAlias = Encoding.UTF8.GetBytes("," + query + ",");
            foreach (var row in Rows())
            {
                var fields = row.Span;
                var name = ReadField(ref fields);
                var aliases = ReadField(ref fields);
                var data = row.Slice(row.Length - fields.Length);
                if (name.SequenceEqual(key) || aliases.IndexOf(exactAlias) >= 0)
                {
                    exact.Add(data);
                    if (exact.Count == ResultLimit)
                        break;
                }
                else if (name.StartsWith(key))
                {
                    if (prefixes.Count < ResultLimit)
                        prefixes.Add(data);
                }
                else if (aliasPrefixes.Count < ResultLimit && aliases.IndexOf(aliasPrefix) >= 0)
                {
                    aliasPrefixes.Add(data);
                }
            }
            return exact.Concat(prefixes).Concat(aliasPrefixes).Take(ResultLimit)
                .Select(data => ParseCity(data.Span)).ToArray();
        }

        internal static IReadOnlyList<City> Geocode(string input)
        {
            if (string.IsNullOrWhiteSpace(input) || input.Length > 256)
                return Array.Empty<City>();

            string query = input.Trim();
            int separator = query.IndexOf(',');
            if (separator < 0)
                return Search(query);

            byte[] name = Encoding.UTF8.GetBytes(Normalize(query.Substring(0, separator).Trim()));
            string description = Normalize(query);
            var matches = new List<City>(ResultLimit);
            foreach (var row in Rows())
            {
                var fields = row.Span;
                if (!ReadField(ref fields).SequenceEqual(name))
                    continue;

                ReadField(ref fields);
                var city = ParseCity(fields);
                if (Normalize(city.Description) == description || Normalize(city.WeatherQuery) == description)
                {
                    matches.Add(city);
                    if (matches.Count == ResultLimit)
                        break;
                }
            }
            return matches;
        }

        internal static City FindNearby(double latitude, double longitude)
        {
            const double radians = Math.PI / 180;
            const double earthRadiusKm = 6371;
            const double searchRadiusKm = 50;
            double latitudeCosine = Math.Cos(latitude * radians);
            double bestDistance = Math.Pow(Math.Sin(searchRadiusKm / (2 * earthRadiusKm)), 2);
            ReadOnlyMemory<byte> bestMatch = default;
            foreach (var row in Rows())
            {
                var fields = row.Span;
                ReadField(ref fields); // search name
                ReadField(ref fields); // aliases
                var data = row.Slice(row.Length - fields.Length);
                ReadField(ref fields); // display name
                Utf8Parser.TryParse(ReadField(ref fields), out double cityLatitude, out _);
                double latitudeDelta = Math.Sin((cityLatitude - latitude) * radians / 2);
                double distance = latitudeDelta * latitudeDelta;
                if (distance >= bestDistance)
                    continue;

                Utf8Parser.TryParse(ReadField(ref fields), out double cityLongitude, out _);
                double longitudeDelta = Math.Sin((cityLongitude - longitude) * radians / 2);
                distance += latitudeCosine * Math.Cos(cityLatitude * radians) * longitudeDelta * longitudeDelta;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestMatch = data;
                }
            }
            return bestMatch.IsEmpty ? null : ParseCity(bestMatch.Span);
        }

        private static byte[] Load()
        {
            using var stream = typeof(CityCatalog).Assembly.GetManifestResourceStream(ResourceName);
            using var compressed = new GZipStream(stream, CompressionMode.Decompress);
            using var buffer = new MemoryStream();
            compressed.CopyTo(buffer);
            return buffer.ToArray();
        }

        // Keep the packed UTF-8 catalog in memory and decode only the results.
        private static IEnumerable<ReadOnlyMemory<byte>> Rows()
        {
            ReadOnlyMemory<byte> remaining = Catalog.Value;
            while (!remaining.IsEmpty)
            {
                int end = remaining.Span.IndexOf((byte)'\n');
                yield return remaining.Slice(0, end);
                remaining = remaining.Slice(end + 1);
            }
        }

        private static ReadOnlySpan<byte> ReadField(ref ReadOnlySpan<byte> row)
        {
            int end = row.IndexOf((byte)'\t');
            var field = row.Slice(0, end);
            row = row.Slice(end + 1);
            return field;
        }

        private static City ParseCity(ReadOnlySpan<byte> data)
        {
            string[] fields = Encoding.UTF8.GetString(data).Split('\t');
            return new City(fields[0], double.Parse(fields[1], CultureInfo.InvariantCulture),
                double.Parse(fields[2], CultureInfo.InvariantCulture), fields[3], fields[4]);
        }

        private static string Normalize(string value)
        {
            // Fold Latin accents, but keep marks that are part of other writing systems.
            var result = new StringBuilder(value.Length);
            bool latin = false;
            foreach (char character in value.Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
                {
                    if (!latin)
                        result.Append(character);
                }
                else
                {
                    char lower = char.ToLowerInvariant(character);
                    latin = lower is >= 'a' and <= 'z';
                    result.Append(lower);
                }
            }
            return result.ToString();
        }

        internal sealed record City(string Name, double Latitude, double Longitude, string Country, string Region)
        {
            internal string Description => string.IsNullOrEmpty(Region) ? $"{Name}, {Country}" : $"{Name}, {Region}, {Country}";
            internal string WeatherQuery => Country == "US" ? Description : $"{Name}, {Country}";
        }
    }
}
