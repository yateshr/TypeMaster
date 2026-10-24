from PIL import Image
from pathlib import Path
im=Image.open('Asset/Branding/typemaster-icon.png')
im.save('Native/TypeMaster.ico',format='ICO',sizes=[(16,16),(24,24),(32,32),(48,48),(64,64),(128,128),(256,256)])
for f in Path('Asset/Branding').glob('typemaster-*'):
 import shutil
 shutil.copyfile(f,Path('output/branding')/f.name)
