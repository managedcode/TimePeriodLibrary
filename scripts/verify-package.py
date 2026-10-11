from pathlib import Path
import hashlib
import sys
import subprocess
import xml.etree.ElementTree as ET
import zipfile

directory = Path(sys.argv[1])
version = ET.parse('Directory.Build.props').find('.//Version').text
path = directory / f'ManagedCode.TimePeriodLibrary.{version}.nupkg'
packages = list(directory.glob('*.nupkg'))
assert packages == [path], f'Unexpected package set: {packages}'
with zipfile.ZipFile(path) as archive:
    entries = archive.namelist()
    expected = 'lib/net10.0/ManagedCode.TimePeriodLibrary.dll'
    assert expected in entries and expected.replace('.dll', '.xml') in entries
    assert 'README.md' in entries and 'LICENSE.md' in entries
    assert all(name.startswith('lib/net10.0/') for name in entries if name.startswith('lib/'))
    nuspec = ET.fromstring(archive.read(next(name for name in entries if name.endswith('.nuspec'))))
    ns = {'n': nuspec.tag.partition('}')[0].lstrip('{')}
    assert nuspec.find('.//n:id', ns).text == 'ManagedCode.TimePeriodLibrary'
    assert nuspec.find('.//n:version', ns).text == version
    repository = nuspec.find('.//n:repository', ns)
    assert repository.attrib['url'] == 'https://github.com/managedcode/TimePeriodLibrary'
    source_commit = subprocess.check_output(['git', 'rev-parse', 'HEAD'], text=True).strip()
    assert repository.attrib.get('commit') == source_commit, 'Package repository commit differs from the qualified source'
    readme = archive.read('README.md').decode()
    assert 'Jani Giannoudis' in readme and 'Original authors and license' in readme
    manifest = {name: hashlib.sha256(archive.read(name)).hexdigest() for name in entries if not name.startswith('_rels/') and not name.startswith('package/') and name != '[Content_Types].xml' and name != '.signature.p7s'}
import json
(directory / 'payload.json').write_text(json.dumps(manifest, indent=2)+'\n')
(directory / 'package.sha256').write_text(hashlib.sha256(path.read_bytes()).hexdigest()+'  '+path.name+'\n')
print(f'Verified {path.name}: .NET10 DLL, XML, README, license and exact repository commit')
