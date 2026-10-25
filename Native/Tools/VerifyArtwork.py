"""Independent size/visibility and teaching-key checks for authored artwork."""
from pathlib import Path
import json
import xml.etree.ElementTree as ET
from PIL import Image

root=Path(__file__).resolve().parents[2]/'Asset'
art=root/'TypeMasterArtwork'
manifest=json.loads((art/'manifest.json').read_text(encoding='utf-8'))
assert len(manifest['legacy'])==92
assert len(manifest['extras'])==18
for name,spec in manifest['legacy'].items():
    with Image.open(root/name) as old,Image.open(art/spec['file']) as new:
        assert new.size==old.size==(spec['width'],spec['height']),name
        if min(new.size)>3:assert new.convert('RGBA').getchannel('A').getbbox(),name
    svg=ET.parse(art/(spec['file'][:-4]+'.svg')).getroot()
    assert svg.get('viewBox')==f"0 0 {spec['width']} {spec['height']}",name

def arrows(name):
    svg=ET.parse(art/(name+'.svg'))
    return {node.get('data-key'):int(node.get('data-finger')) for node in svg.iter() if node.get('data-key')}

for file,expected in {
    'keyboard_u.gif':{'u':5},
    'keyboard_bn.gif':{'b':4,'n':5},
    'keyboard_ei.gif':{'e':3,'i':6},
    'keyboard_shift_f.gif':{'f':4},
    'keyboard_home_row.gif':dict(zip('asdfjkl;',[1,2,3,4,5,6,7,8])),
    'keyboard_home_row_fr.gif':dict(zip('qsdfjklm',[1,2,3,4,5,6,7,8])),
    'keyboard_home_row_de.gif':dict(zip('asdfjklö',[1,2,3,4,5,6,7,8])),
    'keyboard_home_row_fi.gif':dict(zip('asdfjklö',[1,2,3,4,5,6,7,8])),
    'keyboard_home_row_se.gif':dict(zip('asdfjklö',[1,2,3,4,5,6,7,8])),
    'keyboard_home_row_es.gif':dict(zip('asdfjklñ',[1,2,3,4,5,6,7,8])),
}.items():
    assert arrows(file)==expected,(file,arrows(file),expected)

for name in ('logo_login.bmp.png','logo_menu.bmp.png'):
    with Image.open(art/name) as image:
        bounds=image.convert('RGBA').getchannel('A').getbbox()
        assert bounds[2]-bounds[0]>image.width*.7,'Wordmark is missing: '+name
print('92 image canvases, alpha visibility, SVG viewboxes, 10 teaching-key diagrams and both wordmarks passed.')
