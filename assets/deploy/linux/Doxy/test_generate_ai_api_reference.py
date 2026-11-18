import importlib.util
from pathlib import Path
import tempfile
import sys
import unittest
import xml.etree.ElementTree as ET

sys.dont_write_bytecode = True
spec = importlib.util.spec_from_file_location("generator", Path(__file__).with_name("generate-ai-api-reference.py"))
generator = importlib.util.module_from_spec(spec)
spec.loader.exec_module(generator)


class ReferenceGenerationTest(unittest.TestCase):
    def test_overloads_defaults_and_property_access_are_preserved(self):
        first = ET.fromstring('<memberdef kind="function"><type>ModuleHelper</type><name>Find</name><argsstring>(string name, bool exact=false)</argsstring></memberdef>')
        second = ET.fromstring('<memberdef kind="function"><type>ModuleHelper</type><name>Find</name><argsstring>(int address)</argsstring></memberdef>')
        self.assertEqual(generator.signature(first), 'ModuleHelper Find(string name, bool exact=false)')
        self.assertNotEqual(generator.signature(first), generator.signature(second))
        prop = ET.fromstring('<memberdef kind="property" gettable="yes" settable="no"><type>Module</type><name>Instance</name></memberdef>')
        self.assertEqual(generator.signature(prop), 'Module Instance { get; }')

    def test_examples_are_excluded_but_contract_notes_remain(self):
        node = ET.fromstring('<detaileddescription><para>Return true to stop.</para><para><programlisting>badExample();</programlisting></para><simplesect kind="note">Own the resource.</simplesect></detaileddescription>')
        result = generator.prose(node)
        self.assertIn('Return true to stop.', result)
        self.assertIn('Own the resource.', result)
        self.assertNotIn('badExample', result)

    def test_deprecation_in_csharp_source_listing_or_doxygen_metadata(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            (root / 'index.xml').write_text(
                '<doxygenindex><compound kind="file" refid="helper"><name>Helper.cs</name>'
                '</compound></doxygenindex>', encoding='utf-8')
            (root / 'helper.xml').write_text(
                '<doxygen><compounddef><programlisting>'
                '<codeline lineno="1"><highlight>[System.Obsolete(</highlight></codeline>'
                '<codeline lineno="2"><highlight>"Use<sp/>Handle<sp/>instead")]</highlight></codeline>'
                '<codeline lineno="3"><highlight>[AnotherAttribute]</highlight></codeline>'
                '<codeline lineno="4"><highlight>public<sp/>void<sp/>Legacy()</highlight></codeline>'
                '<codeline lineno="5"><highlight>{}</highlight></codeline>'
                '<codeline lineno="6"><highlight>public<sp/>void<sp/>Handle()</highlight></codeline>'
                '</programlisting><location file="Helper.cs"/></compounddef></doxygen>', encoding='utf-8')
            listings = generator.load_source_listings(root)
            legacy = ET.fromstring('<memberdef><location file="Helper.cs" line="4"/></memberdef>')
            handle = ET.fromstring('<memberdef><location file="Helper.cs" line="6"/></memberdef>')
            deprecated_doc = ET.fromstring(
                '<memberdef><detaileddescription><xrefsect><xreftitle>Deprecated</xreftitle>'
                '</xrefsect></detaileddescription></memberdef>')
            self.assertTrue(generator.is_deprecated(legacy, listings))
            self.assertTrue(generator.is_deprecated(deprecated_doc, listings))
            self.assertFalse(generator.is_deprecated(handle, listings))

    def test_generation_filters_internals_and_preserves_inheritance(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            index = ET.Element('doxygenindex')
            names = [name for _, group in generator.TOPICS.values() for name in group] + ['ScriptingHost']
            for name in names:
                entry = ET.SubElement(index, 'compound', kind='class', refid=name)
                ET.SubElement(entry, 'name').text = 'HomeGenie::Automation::Scripting::' + name
                (root / (name + '.xml')).write_text('<doxygen><compounddef><basecompoundref prot="public">Base</basecompoundref><sectiondef>'
                    '<memberdef kind="function" prot="public"><type></type><name>' + name + '</name><argsstring>()</argsstring></memberdef>'
                    '<memberdef kind="function" prot="private"><type>void</type><name>Secret</name><argsstring>()</argsstring></memberdef>'
                    '<memberdef kind="function" prot="public"><type>bool</type><name>Check</name><argsstring>()</argsstring></memberdef>'
                    '</sectiondef></compounddef></doxygen>', encoding='utf-8')
            ET.ElementTree(index).write(root / 'index.xml')
            result = generator.render(root)
            self.assertEqual(result, generator.render(root))
            self.assertEqual(len(result), 7)
            content = '\n'.join(result.values())
            self.assertIn('Inherits: Base', content)
            self.assertIn('bool Check()', content)
            self.assertNotIn('Secret', content)
            self.assertNotIn('ScriptingHost', content)
            self.assertNotIn('ModuleHelper()', content)
            (root / 'ModuleHelper.xml').unlink()
            with self.assertRaises(OSError):
                generator.render(root)


if __name__ == '__main__':
    unittest.main()
