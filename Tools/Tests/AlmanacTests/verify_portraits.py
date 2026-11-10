"""Compare GPU native portraits with an independent source-pixel rasterization.
This verifies transform/crop/layer correctness, not an original-game screenshot match.
"""
from pathlib import Path
import argparse, json, math, xml.etree.ElementTree as ET
import numpy as np
from PIL import Image, ImageFilter, ImageDraw

FIELDS={'x':0.,'y':0.,'kx':0.,'ky':0.,'sx':1.,'sy':1.,'a':1.,'f':0.,'i':None}

def load(root,name):
    doc=ET.fromstring('<root>'+ (root/'reanim'/(name+'.reanim')).read_text(encoding='utf-8-sig')+'</root>')
    tracks=[]
    for t in doc.findall('track'):
        state=dict(FIELDS); frames=[]
        for f in t.findall('t'):
            for v in f: state[v.tag]=v.text if v.tag=='i' else float(v.text)
            frames.append(dict(state))
        tracks.append((t.findtext('name'),frames))
    return float(doc.findtext('fps')),tracks

def matrix(p):
    kx,ky=math.radians(p['kx']),math.radians(p['ky'])
    return np.array([[p['sx']*math.cos(kx),-p['sy']*math.sin(ky),p['x']],
                     [p['sx']*math.sin(kx), p['sy']*math.cos(ky),p['y']], [0,0,1.]])

def pose(asset,clip,seconds):
    fps,tracks=asset; marker=next((f for n,f in tracks if n.lower()==clip.lower()),None)
    if marker:
        visible=[i for i,f in enumerate(marker) if f['f']>=0];start=visible[0];count=1
        while start+count<len(marker) and marker[start+count]['f']>=0:count+=1
    else:start=0;count=max(len(f) for _,f in tracks)
    time=seconds*fps%count;relative=int(time);fraction=time-relative
    result=[]
    for name,frames in tracks:
        a=frames[min(start+relative,len(frames)-1)]
        b=frames[min(start+(relative+1)%count,len(frames)-1)]
        p={k:(a[k]+(b[k]-a[k])*fraction if k not in ('i','f') else a[k]) for k in FIELDS}
        result.append((name,p))
    return result

def visible(name,flag):
    n=name.lower()
    if n in {'anim_cone','anim_bucket','anim_screendoor','zombie_outerarm_screendoor',
             'zombie_innerarm_screendoor_hand','zombie_duckytube','zombie_whitewater',
             'zombie_whitewater2','zombie_mustache','anim_tongue'}:return False
    if n=='zombie_flaghand':return flag
    if n=='zombie_innerarm_screendoor':return flag
    if n.startswith('anim_innerarm'):return not flag
    return True

class Raster:
    def __init__(self,root):
        self.root=root;self.textures={}
        self.paths={p.stem.upper():p for p in (root/'reanim').iterdir() if p.suffix.lower() in ('.png','.jpg')}
    def image(self,key):
        key=key.removeprefix('IMAGE_REANIM_').upper()
        if key not in self.textures:self.textures[key]=Image.open(self.paths[key]).convert('RGBA')
        return self.textures[key]
    def draw(self,canvas,p,transform):
        if p['f']<0 or not p['i'] or p['a']<=0:return
        image=self.image(p['i']); inv=np.linalg.inv(transform@matrix(p))
        layer=image.transform(canvas.size,Image.Transform.AFFINE,tuple(inv[:2,:].flatten()),Image.Resampling.BILINEAR)
        if p['a']<1:layer.putalpha(layer.getchannel('A').point(lambda a:round(a*p['a'])))
        canvas.alpha_composite(layer)

def expected(root,raster,zombie,seconds,thumb):
    flag=zombie==1
    if thumb:
        origin=(19+(2 if flag else -8 if zombie==24 else 0),12+(10 if flag else -7 if zombie==24 else 0))
        scale=.5; size=(72,72);seconds=0.
    else:origin=(42,51);scale=1.;size=(197,187)
    parent=np.array([[scale,0,origin[0]],[0,scale,origin[1]],[0,0,1.]])
    canvas=Image.new('RGBA',size)
    asset=load(root,'Zombie_imp' if zombie==24 else 'Zombie')
    sampled=pose(asset,'anim_walk' if zombie==24 else 'anim_idle',seconds)
    if flag:
        flagpose=pose(load(root,'Zombie_flagpole'),'',seconds)
        base=next(frames[0] for name,frames in asset[1] if name=='Zombie_flaghand')
        if thumb:
            for _,p in flagpose:raster.draw(canvas,p,parent)
    for name,p in sampled:
        if zombie!=24 and not visible(name,flag):continue
        if flag and not thumb and name=='Zombie_flaghand':
            attachment=parent@matrix(p)@np.linalg.inv(matrix(base))
            for _,fp in flagpose:raster.draw(canvas,fp,attachment)
        raster.draw(canvas,p,parent)
    return canvas

def compare(reference,actual):
    exp=np.asarray(reference,dtype=float);got=np.asarray(actual.convert('RGBA'),dtype=float)
    em=Image.fromarray(np.uint8(exp[:,:,3]>128)*255);gm=Image.fromarray(np.uint8(got[:,:,3]>128)*255)
    e=np.asarray(em)>0;g=np.asarray(gm)>0
    ed=np.asarray(em.filter(ImageFilter.MaxFilter(5)))>0;gd=np.asarray(gm.filter(ImageFilter.MaxFilter(5)))>0
    mismatch=int((e & ~gd).sum()+(g & ~ed).sum());population=max(1,int(e.sum()+g.sum()))
    interior=(exp[:,:,3]>254)&(got[:,:,3]>254)
    mae=float(np.abs(exp[:,:,:3]-got[:,:,:3])[interior].mean()) if interior.any() else 255.
    return {'MaskMismatchFraction':mismatch/population,'OpaqueColorMeanError':mae,
            'Passed':mismatch/population<.015 and mae<8 and bool(interior.any())}

def main():
    parser=argparse.ArgumentParser();parser.add_argument('root',type=Path);parser.add_argument('output',type=Path)
    args=parser.parse_args();raster=Raster(args.root);results=[]
    refs=args.output/'portraits'/'reference';refs.mkdir(exist_ok=True)
    for zombie in (0,1,24):
        for frame,seconds in ((0,0.),(1,.75)):
            for thumb in (False,True):
                stem=('thumb' if thumb else 'preview')+f'-{zombie:02d}-{frame}'
                ref=expected(args.root,raster,zombie,seconds,thumb);ref.save(refs/(stem+'.png'))
                result={'Name':stem,**compare(ref,Image.open(args.output/'portraits'/(stem+'.png')))};results.append(result)
    report={'Passed':all(x['Passed'] for x in results),'NativeVisualComparison':False,'Checks':results}
    (args.output/'portrait-raster-results.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps(report,indent=2))
    raise SystemExit(0 if report['Passed'] else 1)
if __name__=='__main__':main()
