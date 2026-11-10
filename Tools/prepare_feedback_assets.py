from pathlib import Path
import hashlib,json,shutil,xml.etree.ElementTree as ET
root=Path(__file__).resolve().parents[2];raw=root/'extracted_assets/raw';dst=root/'PvZUnity/Assets/StreamingAssets/PvZ'
records=[]
def copy(relative):
 source=raw/relative;target=dst/relative
 if relative.parent.name=='particles' and relative.suffix.lower() in ['.png','.jpg']:target=dst/'images'/relative.name
 if not source.is_file():raise FileNotFoundError(source)
 target.parent.mkdir(parents=True,exist_ok=True)
 if not target.exists() or source.read_bytes()!=target.read_bytes():shutil.copyfile(source,target)
 records.append({'file':str(relative).replace('\\','/'),'sha256':hashlib.sha256(source.read_bytes()).hexdigest()})
images={}
for group in ET.parse(raw/'properties/resources.xml').getroot():
 folder='';prefix=''
 for node in group:
  if node.tag=='SetDefaults':folder=node.get('path','');prefix=node.get('idprefix','')
  elif node.tag=='Image':
   stem=raw/folder/node.get('path','')
   for suffix in ['.png','.jpg']:
    source=stem.with_suffix(suffix)
    if source.exists():images[prefix+node.get('id','')]=source.relative_to(raw);break
for folder in ['particles','images']:
 for source in (raw/folder).iterdir():
  if source.suffix.lower() in ['.png','.jpg']:images.setdefault(('IMAGE_'+source.stem).upper(),source.relative_to(raw))
images={k.upper():v for k,v in images.items()}
for name in ['SeedPacket','Award','Starburst','AwardPickupArrow']:
 relative=Path('particles')/(name+'.xml');copy(relative)
 xml=ET.fromstring('<Particles>'+(raw/relative).read_text()+'</Particles>')
 for image in xml.findall('.//Image'):
  if image.text.upper() not in images:raise KeyError(image.text)
  copy(images[image.text.upper()])
for name in ['SeedPacket_Larger.png','AwardScreen_Back_new.jpg','Shovel_hi_res.png','Shovel.png','Almanac.png','CarKeys.png','WateringCan.png','Taco.png','ZombieNoteSmall.png','ZombieNote.jpg','ZombieNote_.png','ZombieNote1.png','ZombieNote2.png','ZombieNote3.png','ZombieNote4.png','Credits_ZombieNote.png','moneybag.png','moneybag_hi_res.png','Sunflower_trophy.png','SeedChooser_Button.png','Almanac_CloseButton.png']:
 copy(Path('images')/name)
copy(Path('images/ad/game_pass.jpg'))
for name in ['seedlift.ogg','tap2.ogg','lightfill.ogg','paper.ogg','shovel.ogg','buttonclick.ogg']:copy(Path('sounds')/name)
out=root/'analysis/ida/adventure_rewards/imported-assets.json';out.write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf-8');print('Copied or verified %d original asset references'%len(records))
