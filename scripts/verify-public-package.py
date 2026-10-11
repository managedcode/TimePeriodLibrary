from pathlib import Path
import hashlib
import json
import sys
import time
import urllib.error
import urllib.request
import xml.etree.ElementTree as ET
import zipfile

directory = Path(sys.argv[1])
version = ET.parse('Directory.Build.props').find('.//Version').text
filename = f'managedcode.timeperiodlibrary.{version}.nupkg'
url = f'https://api.nuget.org/v3-flatcontainer/managedcode.timeperiodlibrary/{version}/{filename}'
local = json.loads((directory / 'payload.json').read_text())
deadline = time.monotonic() + 900
while True:
    try:
        payload = urllib.request.urlopen(url, timeout=30).read()
        break
    except urllib.error.HTTPError as error:
        if error.code != 404 or time.monotonic() >= deadline:
            raise
        time.sleep(15)
public_path = directory / 'public.nupkg'
public_path.write_bytes(payload)
with zipfile.ZipFile(public_path) as archive:
    actual = {name: hashlib.sha256(archive.read(name)).hexdigest() for name in archive.namelist() if not name.startswith('_rels/') and not name.startswith('package/') and name != '[Content_Types].xml' and name != '.signature.p7s'}
assert actual == local, 'Public package differs from the exact qualified source payload'
(directory / 'public-feed.json').write_text(json.dumps({'url': url, 'version': version, 'payloadMatches': True, 'sha256': hashlib.sha256(payload).hexdigest()}, indent=2)+'\n')
print(f'Public NuGet payload verified: {url}')
