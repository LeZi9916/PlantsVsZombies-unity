"""Render local MO3 tracker orders/channels to Unity loop stems (no synthesized music).

Use a 64-bit BASS 2.4 decoder via --bass; the original EXE's BASS 2.0 DLL is x86.
The decoder is an offline tool only. Native tune/order/channel evidence: 0x469140 / 0x468AC0.
Position scale 4: 0x468CA9. Native row/boundary comparisons: 0x469787..0x469815.
Each output includes its actual decoded row timeline; 16 PCM frames per position probe.
"""
import argparse, ctypes as C, hashlib, json, os, subprocess, tempfile, wave
from pathlib import Path

PLANS = [
 ("title","mainmusic",152,"0-29"),("choose","mainmusic",122,"0-29"),
 ("day_base","mainmusic",0,"0-23"),("day_drums","mainmusic",0,"24-26"),("day_hihats","mainmusic_hihats",0,"27"),
 ("night_base","mainmusic",48,"0-29"),("night_burst","mainmusic",76,"0-29"),("night_burst77","mainmusic",77,"0-29"),("night_standby","mainmusic",92,"0-29"),
 ("pool_base","mainmusic",94,"0-17"),("pool_drums","mainmusic",94,"18-28"),("pool_hihats","mainmusic_hihats",94,"18-24,29"),
 ("fog_base","mainmusic",125,"0-15"),("fog_drums","mainmusic",125,"16-22"),("fog_hihats","mainmusic_hihats",125,"23"),
 ("roof_base","mainmusic",184,"0-17"),("roof_drums","mainmusic",184,"18-20"),("roof_hihats","mainmusic_hihats",184,"21"),
 ("boss","mainmusic",158,"0-29"),("minigame","mainmusic",166,"0-29"),("puzzle","mainmusic",177,"0-29"),
 ("conveyor","mainmusic",212,"0-29"),("garden","mainmusic",221,"0-29")]

def run():
 p=argparse.ArgumentParser();p.add_argument('--bass',required=True);p.add_argument('--source',required=True);p.add_argument('--output',required=True);p.add_argument('--evidence',required=True);a=p.parse_args()
 lib=C.WinDLL(a.bass) if os.name=='nt' else C.CDLL(a.bass)
 def fn(n,args,ret):
  f=getattr(lib,n);f.argtypes=args;f.restype=ret;return f
 init=fn('BASS_Init',[C.c_int,C.c_uint,C.c_uint,C.c_void_p,C.c_void_p],C.c_bool)
 load=fn('BASS_MusicLoad',[C.c_uint,C.c_void_p,C.c_uint64,C.c_uint,C.c_uint,C.c_uint],C.c_uint)
 attr=fn('BASS_ChannelSetAttribute',[C.c_uint,C.c_uint,C.c_float],C.c_bool)
 seek=fn('BASS_ChannelSetPosition',[C.c_uint,C.c_uint64,C.c_uint],C.c_bool)
 pos=fn('BASS_ChannelGetPosition',[C.c_uint,C.c_uint],C.c_uint64)
 read=fn('BASS_ChannelGetData',[C.c_uint,C.c_void_p,C.c_uint],C.c_uint)
 free=fn('BASS_MusicFree',[C.c_uint],C.c_bool)
 err=fn('BASS_ErrorGetCode',[],C.c_int)
 if not init(0,44100,0,None,None):raise RuntimeError(('BASS_Init',err()))
 out=Path(a.output);out.mkdir(parents=True,exist_ok=True);evidence=Path(a.evidence);evidence.mkdir(parents=True,exist_ok=True)
 clips=[];traces=[]
 with tempfile.TemporaryDirectory(prefix='pvz-tracker-') as tmp:
  for key,module,order,channel_range in PLANS:
   blob=(Path(a.source)/(module+'.mo3')).read_bytes();buf=C.create_string_buffer(blob)
   h=load(1,buf,0,len(blob),0x200000|0x20000|0x8204,44100)
   if not h:raise RuntimeError(('load',key,err()))
   enabled=set()
   for part in channel_range.split(','):
    v=list(map(int,part.split('-')));enabled.update(range(v[0],v[-1]+1))
   if not attr(h,0x102,4.0):raise RuntimeError(('native position scaler',key,err()))
   for c in range(30):
    if not attr(h,0x200+c,1.0 if c in enabled else 0.0):raise RuntimeError(('attribute',key,c,err()))
   if not seek(h,order,1):raise RuntimeError(('seek',key,order,err()))
   wav=Path(tmp)/(key+'.wav');visited={};sequence=[];frames=0;previous=-1;previous_position=-1;row_positions={};row_map=[];loop_start=0;returned=-1;block=C.create_string_buffer(64)
   with wave.open(str(wav),'wb') as w:
    w.setnchannels(2);w.setsampwidth(2);w.setframerate(44100)
    while frames<44100*360:
     position=pos(h,1);current=position&65535
     if position!=previous_position:
      if position in row_positions:
       loop_start=row_positions[position];returned=current;break
      row_positions[position]=frames;previous_position=position
      row_map.append(dict(sample=frames,order=int(current),row=int(position>>16)))
     if current!=previous:
      if previous>=0 and current in visited:
       loop_start=visited[current];returned=current;break
      if previous>=0 and current<order:
       returned=current;loop_start=0;break
      visited[current]=frames;sequence.append([current,frames]);previous=current
     got=read(h,block,64)
     if got in (0,0xffffffff):break
     w.writeframesraw(block.raw[:got]);frames+=got//4
   free(h)
   if frames<4410 or returned<0:raise RuntimeError(('No tracker loop',key,frames,sequence))
   target=out/(key+'.ogg')
   subprocess.run(['ffmpeg','-nostdin','-v','error','-y','-i',str(wav),'-c:a','libvorbis','-q:a','6',str(target)],check=True)
   clips.append(dict(key=key,resource='PvZMusic/'+key,loopStartSample=loop_start,frames=frames,sampleRate=44100,nativeOrder=order,positionScale=4,rowTimeMap=row_map))
   traces.append(dict(key=key,module=module,sourceSha256=hashlib.sha256(blob).hexdigest(),nativeOrder=order,channels=channel_range,orderSequence=sequence,returnOrder=returned,loopStartSample=loop_start,frames=frames,positionScale=4,rowPoints=len(row_map)))
   meta='fileFormatVersion: 2\nguid: {guid}\nAudioImporter:\n  externalObjects: {{}}\n  serializedVersion: 7\n  defaultSettings:\n    serializedVersion: 2\n    loadType: 1\n    sampleRateSetting: 0\n    sampleRateOverride: 44100\n    compressionFormat: 1\n    quality: 0.7\n    conversionMode: 0\n    preloadAudioData: 0\n  platformSettingOverrides: {{}}\n  forceToMono: 0\n  normalize: 1\n  loadInBackground: 1\n  ambisonic: 0\n  3D: 0\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n'
   target.with_suffix('.ogg.meta').write_text(meta.format(guid=hashlib.md5(('PvZOriginalMusic:'+key).encode()).hexdigest()),encoding='utf-8')
   print(f'{key}: order {order} -> {returned}; {frames/44100:.3f}s, loop {loop_start/44100:.3f}s',flush=True)
 (out/'native_music.json').write_text(json.dumps(dict(clips=clips),ensure_ascii=False,indent=2),encoding='utf-8')
 (evidence/'music_render.json').write_text(json.dumps(traces,ensure_ascii=False,indent=2),encoding='utf-8')
 lib.BASS_Free()
if __name__=='__main__':run()
