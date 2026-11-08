"""Build the bundled GeoNames catalog from the official dump files."""

import argparse
import gzip
import io
import unicodedata
from pathlib import Path
from zipfile import ZipFile


def search_key(value):
    result = []
    latin = False
    for character in unicodedata.normalize("NFD", value):
        if unicodedata.category(character) == "Mn":
            if not latin:
                result.append(character)
        else:
            character = character.lower()
            latin = "a" <= character <= "z"
            result.append(character)
    return "".join(result)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("dump", type=Path, help="Folder with cities500.zip, admin1CodesASCII.txt and alternateNamesV2.zip")
    parser.add_argument("output", type=Path, help="Path to cities.tsv.gz")
    args = parser.parse_args()

    with (args.dump / "admin1CodesASCII.txt").open(encoding="utf-8") as regions_file:
        regions = dict(line.split("\t", 2)[:2] for line in regions_file)

    cities = []
    with ZipFile(args.dump / "cities500.zip") as archive:
        with io.TextIOWrapper(archive.open("cities500.txt"), encoding="utf-8") as source:
            for line in source:
                fields = line.rstrip("\n").split("\t")
                city_id, name, ascii_name, _, lat, lng = fields[:6]
                country, admin1 = fields[8], fields[10]
                region = admin1 if country == "US" else regions.get(f"{country}.{admin1}", "")
                cities.append((int(fields[14] or 0), city_id,
                               (name, ascii_name, lat, lng, country, region)))

    city_ids = {city_id for _, city_id, _ in cities}
    aliases = {}
    with ZipFile(args.dump / "alternateNamesV2.zip") as archive:
        with io.TextIOWrapper(archive.open("alternateNamesV2.txt"), encoding="utf-8") as source:
            for line in source:
                fields = line.rstrip("\n").split("\t")
                _, city_id, language, name, preferred, _, colloquial, historic = fields[:8]
                if city_id not in city_ids or not (language.isalpha() and 2 <= len(language) <= 3):
                    continue
                if colloquial or historic or fields[9] or "," in name:
                    continue
                names = aliases.setdefault(city_id, {})
                if language not in names or preferred:
                    names[language] = name

    with args.output.open("wb") as output:
        with gzip.GzipFile(filename="", fileobj=output, mode="wb", mtime=0) as compressed:
            with io.TextIOWrapper(compressed, encoding="utf-8", newline="\n") as catalog:
                for _, city_id, city in sorted(cities, key=lambda row: (-row[0], int(row[1]))):
                    name, ascii_name, *location = city
                    key = search_key(name)
                    names = {search_key(alias) for alias in aliases.get(city_id, {}).values()}
                    names.add(search_key(ascii_name))
                    variants = "," + ",".join(sorted(names - {"", key})) + ","
                    catalog.write("\t".join((key, variants, name, *location)) + "\n")
    print(f"{len(cities):,} cities")


if __name__ == "__main__":
    main()
