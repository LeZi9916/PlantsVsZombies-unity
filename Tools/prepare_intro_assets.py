from pathlib import Path
import hashlib,json,shutil,xml.etree.ElementTree as E
root=Path(__file__).resolve().parents[2];raw=root/'extracted_assets/raw';dst=root/'PvZUnity/Assets/StreamingAssets/PvZ'
records=[]
def copy(source,target):
 target.parent.mkdir(parents=True,exist_ok=True)
 data=source.read_bytes()
 if target.exists() and target.read_bytes()!=data:raise RuntimeError('Original asset conflict '+str(target))
 if not target.exists():target.write_bytes(data)
 records.append({'Source':str(source.relative_to(root)),'Destination':str(target.relative_to(root)),'SHA256':hashlib.sha256(data).hexdigest()})
files={p.stem.upper():p for p in (raw/'reanim').iterdir() if p.suffix.lower() in ['.png','.jpg']}
for name in ['SodRoll','StartReadySetPlant','LoadBar_sprout','LoadBar_Zombiehead']:
 src=raw/'reanim'/(name+'.reanim');copy(src,dst/'reanim'/src.name)
 for node in E.fromstring('<root>'+src.read_text(encoding='utf-8-sig')+'</root>').findall('.//i'):
  key=node.text.strip().removeprefix('IMAGE_REANIM_').upper()
  if key not in files:raise KeyError(key)
  image=files[key];copy(image,dst/'reanim'/image.name)
  if key+'_' in files:copy(files[key+'_'],dst/'reanim'/files[key+'_'].name)
for name in ['SeedChooser_Background.png','SeedChooser_Button.png','SeedChooser_Button_Glow.png','SeedChooser_Button_Disabled.png','SeedChooser_ImitaterAddOn.png']:
 copy(raw/'images'/name,dst/'images'/name)
for name in ['sod1row','sod3row']:
 source=root/'extracted_assets/converted/rgba/images'/(name+'.png');target=dst/'images'/source.name
 if not target.exists():copy(source,target)
 else:records.append({'Source':str(source.relative_to(root)),'Destination':str(target.relative_to(root)),'SHA256':hashlib.sha256(target.read_bytes()).hexdigest()})
for name in ['readysetplant','roll_in','awooga','dirt_rise','seedlift']:
 src=raw/'sounds'/(name+'.ogg')
 if src.exists():copy(src,dst/'sounds'/src.name)
for name in ['SodRoll','SeedPacketFlash']:
 src=raw/'particles'/(name+'.xml');copy(src,dst/'particles'/src.name)
 images={p.stem.upper():p for d in ['images','particles'] for p in (raw/d).iterdir() if p.suffix.lower() in ['.png','.jpg']}
 for node in E.fromstring('<root>'+src.read_text()+'</root>').findall('.//Image'):
  image=images[node.text.removeprefix('IMAGE_').upper()];copy(image,dst/'images'/image.name)
folder=root/'analysis/ida/adventure_intro';folder.mkdir(parents=True,exist_ok=True);(folder/'imported-assets.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf-8');print('Verified %d original intro/card asset references'%len(records))
