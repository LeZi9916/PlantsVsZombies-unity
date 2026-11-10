"""Prepare the additional board images used by the Adventure UI, preserving originals."""
from pathlib import Path
from io import BytesIO
import argparse, json, xml.etree.ElementTree as ET
from PIL import Image

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--source',type=Path,default=Path(__file__).resolve().parents[2]/'extracted_assets'/'raw')
    parser.add_argument('--destination',type=Path,default=Path(__file__).resolve().parents[1]/'Assets'/'StreamingAssets'/'PvZ')
    args=parser.parse_args(); source=args.source.resolve(); destination=args.destination.resolve(); planned={}
    regular=['background2.jpg','background3.jpg','background4.jpg','background5.jpg','background6boss.jpg',
             'ConveyorBelt.png','ConveyorBelt_backdrop.png','Scary_Pot.png','crater.png','crater_water_day.png',
             'crater_water_night.png','crater_roof_center.png','ice.png','pool.jpg','pool_night.jpg',
             'ProjectileCactus.png','Projectile_star.png','CobCannon_target.png','CobCannon_target_shadow.png']
    for name in regular:planned['images/'+name]=(source/'images'/name).read_bytes()
    for name in ['PuffShroom_puff1.png','PuffShroom_puff2.png']:
        planned['particles/'+name]=(source/'particles'/name).read_bytes()
    for name in ['Tombstones','fog']:
        image=Image.open(source/'images'/(name+'.jpg')).convert('RGBA')
        mask_path=next(p for p in (source/'images').iterdir() if p.stem.lower()==name.lower()+'_' and p.suffix.lower() in ('.png','.jpg'))
        mask=Image.open(mask_path).convert('RGB').getchannel('R')
        if image.size!=mask.size:raise ValueError('Alpha mask dimensions differ: '+name)
        image.putalpha(mask);out=BytesIO();image.save(out,format='PNG');planned['images/'+name+'.png']=out.getvalue()
    sod=source.parent/'converted'/'rgba'/'images'/'sod3row.png';planned['images/sod3row.png']=sod.read_bytes()
    for name in ['PoolCleaner','RoofCleaner','Zombie_boss_fireball','Zombie_boss_iceball','Coin_silver','Coin_gold','Diamond','FirePea']:
        relative='reanim/'+name+'.reanim'; data=(source/relative).read_bytes();planned[relative]=data
        doc=ET.fromstring('<root>'+data.decode('utf-8-sig')+'</root>')
        files={p.stem.upper():p for p in (source/'reanim').iterdir() if p.suffix.lower() in ('.png','.jpg','.gif')}
        for image in doc.findall('.//i'):
            key=image.text.strip().removeprefix('IMAGE_REANIM_')
            texture=files.get(key.upper())
            if texture is None:raise ValueError('Missing adventure animation texture: '+key)
            planned['reanim/'+texture.name]=texture.read_bytes()
            mask=files.get((texture.stem+'_').upper())
            if texture.suffix.lower()=='.jpg' and mask:planned['reanim/'+mask.name]=mask.read_bytes()
    for relative,data in planned.items():
        target=destination/relative
        if target.exists() and target.read_bytes()!=data:raise ValueError('Destination differs; refusing to overwrite '+str(target))
    for relative,data in planned.items():
        target=destination/relative;target.parent.mkdir(parents=True,exist_ok=True)
        if not target.exists():target.write_bytes(data)
    print(json.dumps({'PreparedFiles':len(planned),'Source':str(source),'Destination':str(destination)},ensure_ascii=False))
if __name__=='__main__':main()
