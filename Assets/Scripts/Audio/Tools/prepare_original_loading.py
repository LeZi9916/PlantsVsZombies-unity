"""Prepare exact local title assets, merging the original JPG alpha mask only."""
import argparse,hashlib,json,shutil
from pathlib import Path
from PIL import Image

def main():
 p=argparse.ArgumentParser();p.add_argument('--source',required=True);p.add_argument('--output',required=True);a=p.parse_args();src=Path(a.source);out=Path(a.output);out.mkdir(parents=True,exist_ok=True)
 files={'title':'images/titlescreen.jpg','popcap':'images/PopCap_Logo.jpg','grass':'images/LoadBar_grass.png','dirt':'images/LoadBar_dirt.png','sodroll':'reanim/SodRollCap.png'}
 provenance=[]
 for key,name in files.items():
  f=src/name;im=Image.open(f);im.save(out/(key+'.png'));provenance.append(dict(key=key,source=name,sha256=hashlib.sha256(f.read_bytes()).hexdigest(),width=im.width,height=im.height))
 logo=Image.open(src/'images/PvZ_Logo.jpg').convert('RGBA');mask=Image.open(src/'images/PvZ_Logo_.png').convert('L');logo.putalpha(mask);logo.save(out/'logo.png')
 provenance.append(dict(key='logo',source='images/PvZ_Logo.jpg',alphaMask='images/PvZ_Logo_.png',sha256=hashlib.sha256((src/'images/PvZ_Logo.jpg').read_bytes()).hexdigest()))
 for f in out.glob('*.png'):
  # Deterministic identity; texture defaults preserve the source pixels and alpha.
  f.with_suffix('.png.meta').write_text('fileFormatVersion: 2\nguid: '+hashlib.md5(('PvZOriginalLoading:'+f.stem).encode()).hexdigest()+'\nTextureImporter:\n  externalObjects: {}\n  serializedVersion: 13\n  mipmaps:\n    enableMipMap: 0\n  textureSettings:\n    serializedVersion: 2\n    filterMode: 1\n    aniso: 1\n    mipBias: 0\n    wrapU: 1\n    wrapV: 1\n    wrapW: 1\n  maxTextureSize: 2048\n  textureFormat: 1\n  textureCompression: 0\n  alphaIsTransparency: 1\n  sRGBTexture: 1\n  textureType: 0\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n',encoding='utf-8')
 (out/'provenance.json').write_text(json.dumps(provenance,ensure_ascii=False,indent=2),encoding='utf-8')
 print('Prepared',len(provenance),'original loading assets')
if __name__=='__main__':main()
