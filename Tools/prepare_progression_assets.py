from pathlib import Path
import json, shutil
from PIL import Image
root=Path(__file__).resolve().parents[2]
raw=root/'extracted_assets/raw'; converted=root/'extracted_assets/converted/rgba'
out=root/'PvZUnity/Assets/StreamingAssets/PvZ'
names={'Background_Greenhouse.jpg','Background_greenhouse_overlay.jpg','Background_MushroomGarden.jpg','aquarium1.jpg','coinbank.png','WateringCan.png','WateringCanGold.png','Fertilizer.png','Phonograph.png','TreeFood.png','Rake.png','Present.png','PresentOpen.png','chocolate.png','icon_rake.png','icon_roofcleaner.png','icon_poolcleaner.png','bug_spray.png','brain.png'}
names.update(p.name for p in (raw/'images').glob('Store*') if not p.stem.endswith('_'))
names.update(p.name for p in (raw/'images').glob('Zen*') if not p.stem.endswith('_'))
count=0
for name in sorted(names):
 source=raw/'images'/name
 if not source.exists():continue
 rgba=converted/'images'/(source.stem+'.png')
 target=out/'images'/(source.stem+'.png' if source.suffix.lower()=='.jpg' and (raw/'images'/(source.stem+'_.png')).exists() else name)
 target.parent.mkdir(parents=True,exist_ok=True)
 if rgba.exists() and target.suffix=='.png':data=rgba.read_bytes()
 else:data=source.read_bytes()
 if target.exists() and target.read_bytes()!=data:raise RuntimeError('Asset conflict: '+str(target))
 target.write_bytes(data);count+=1
prop=out/'properties';prop.mkdir(parents=True,exist_ok=True)
shutil.copyfile(raw/'properties/LawnStrings.txt',prop/'LawnStrings.txt')
print(json.dumps({'prepared':count,'original_strings':True}))

# Original reanim tracks and their original texture dependencies.
import xml.etree.ElementTree as ET
files={p.stem.upper():p for p in (raw/'reanim').iterdir() if p.suffix.lower() in ('.jpg','.png','.gif')}
for animation in ['TreeOfWisdom','TreeOfWisdomClouds','TreeFood','ZenGarden_sprout','ZenGarden_wateringcan','ZenGarden_fertilizer','ZenGarden_bugspray','ZenGarden_phonograph','Stinky','Rake','CrazyDave','Portal_Circle','Portal_Square']:
 source=raw/'reanim'/(animation+'.reanim')
 if not source.exists():continue
 (out/'reanim').mkdir(parents=True,exist_ok=True)
 shutil.copyfile(source,out/'reanim'/source.name)
 document=ET.fromstring('<root>'+source.read_text(encoding='utf-8-sig')+'</root>')
 for image in document.findall('.//i'):
  key=(image.text or '').strip().removeprefix('IMAGE_REANIM_').upper()
  if key not in files:raise RuntimeError('Missing original texture '+key)
  texture=files[key];shutil.copyfile(texture,out/'reanim'/texture.name)
  mask=files.get(key+'_')
  if mask:shutil.copyfile(mask,out/'reanim'/mask.name)
