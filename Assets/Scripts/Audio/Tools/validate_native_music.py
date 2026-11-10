"""Bounded original x86 music-state oracle. Does not start Unity or the original game.
Uses the existing local Unicorn dependency under analysis/ida/waves/linux-deps (Linux/WSL).
Only external position/count/audio-interface calls are stubbed; 0x469530 and its fade helpers execute from the original PE.
"""
import argparse,bisect,csv,hashlib,json,struct
from pathlib import Path
from unicorn import Uc,UC_ARCH_X86,UC_MODE_32,UC_HOOK_CODE
from unicorn.x86_const import UC_X86_REG_EAX,UC_X86_REG_EBX,UC_X86_REG_ECX,UC_X86_REG_EDX,UC_X86_REG_ESI,UC_X86_REG_EDI,UC_X86_REG_ESP,UC_X86_REG_EBP,UC_X86_REG_EIP

MUSIC,APP,IFACE,VTABLE,BASS,VOICE,JUMP,STACK,STOP=0x10000000,0x10010000,0x10020000,0x10021000,0x10022000,0x10023000,0x10023010,0x11000000,0x12000000
EXPECTED='e1511201ec2ae9a7e30259f04bc2ec7272badb2574a2e9b405d0e916ae5c082c'
def u32(b,p):return struct.unpack_from('<I',b,p)[0]
class Oracle:
 def __init__(self,image):
  pe=u32(image,0x3c);opt=pe+24;count=struct.unpack_from('<H',image,pe+6)[0];size=struct.unpack_from('<H',image,pe+20)[0]
  self.uc=Uc(UC_ARCH_X86,UC_MODE_32);self.uc.mem_map(0x400000,(u32(image,opt+56)+4095)&~4095);self.uc.mem_write(0x400000,image[:u32(image,opt+60)])
  for i in range(count):
   p=opt+size+i*40;rva,raw,offset=u32(image,p+12),u32(image,p+16),u32(image,p+20)
   if raw:self.uc.mem_write(0x400000+rva,image[offset:offset+raw])
  self.uc.mem_map(MUSIC,0x40000);self.uc.mem_map(STACK,0x10000);self.uc.mem_map(STOP,0x1000)
  self.write(MUSIC,APP);self.write(MUSIC+4,IFACE);self.write(IFACE,VTABLE);self.write(APP+0x3a8,IFACE);self.write(APP+0x868,1);self.write(APP+0x91c,0)
  self.write(VTABLE+0x38,VOICE);self.write(0x755f50,BASS);self.write(BASS+0x50,JUMP)
  for address,pop in ((0x640b30,4),(0x414940,0),(0x468fa0,4),(VOICE,12),(JUMP,12)):
   self.uc.mem_write(address,b'\xc2'+struct.pack('<H',pop));self.uc.hook_add(UC_HOOK_CODE,self.hook,begin=address,end=address)
  self.position=self.zombies=0;self.volumes={1:1.0,2:0.0,3:0.0};self.jump=-1
 def read(self,p):return u32(self.uc.mem_read(p,4),0)
 def write(self,p,v):self.uc.mem_write(p,struct.pack('<I',v&0xffffffff))
 def hook(self,uc,ea,size,user):
  if ea==0x640b30:uc.reg_write(UC_X86_REG_EAX,self.position)
  elif ea==0x414940:uc.reg_write(UC_X86_REG_EAX,self.zombies)
  elif ea==0x468fa0:uc.reg_write(UC_X86_REG_EAX,2)
  elif ea==VOICE:
   esp=uc.reg_read(UC_X86_REG_ESP);self.volumes[self.read(esp+4)]=struct.unpack('<d',uc.mem_read(esp+8,8))[0]
  elif ea==JUMP:self.jump=self.read(uc.reg_read(UC_X86_REG_ESP)+8)&65535
 def call(self,ea,regs):
  for r in (UC_X86_REG_EAX,UC_X86_REG_EBX,UC_X86_REG_ECX,UC_X86_REG_EDX,UC_X86_REG_ESI,UC_X86_REG_EDI,UC_X86_REG_EBP):self.uc.reg_write(r,0)
  self.uc.reg_write(UC_X86_REG_ESP,STACK+0xf000);self.uc.mem_write(STACK+0xf000,struct.pack('<I',STOP))
  for r,v in regs.items():self.uc.reg_write(r,v)
  self.uc.emu_start(ea,STOP,count=10000)
  if self.uc.reg_read(UC_X86_REG_EIP)!=STOP:raise RuntimeError('Native oracle did not return')
 def reset(self,night):
  for o in (24,36,40,44,48,52):self.write(MUSIC+o,0)
  self.write(MUSIC+24,-1);self.write(MUSIC+8,2 if night else 1)
  self.write(MUSIC+12,1);self.write(MUSIC+16,2);self.write(MUSIC+20,3)
 def tick(self,position,zombies,force):
  self.position=position;self.zombies=zombies;self.jump=-1;self.volumes={1:1.0,2:0.0,3:0.0}
  if force:self.call(0x469510,{UC_X86_REG_EAX:MUSIC})
  self.call(0x469530,{UC_X86_REG_ESI:MUSIC})
  return [self.read(MUSIC+o) for o in (36,40,44,48,52)]+[self.volumes[i] for i in (1,2,3)]+[self.jump]

def position_at(clip,sample):
 if sample>=clip['frames']:sample=clip['loopStartSample']+(sample-clip['frames'])%(clip['frames']-clip['loopStartSample'])
 rows=clip['rowTimeMap'];i=max(0,bisect.bisect_right([r['sample'] for r in rows],sample)-1);row=rows[i];end=rows[i+1]['sample'] if i+1<len(rows) else clip['frames']
 fraction=min(3,max(0,(sample-row['sample'])*4//max(1,end-row['sample'])))
 return row['order']|((row['row']+fraction)<<16)

def main():
 p=argparse.ArgumentParser();p.add_argument('--exe',required=True);p.add_argument('--manifest',required=True);p.add_argument('--output',required=True);a=p.parse_args()
 image=Path(a.exe).read_bytes();assert hashlib.sha256(image).hexdigest()==EXPECTED
 clips={c['key']:c for c in json.loads(Path(a.manifest).read_text())['clips']};oracle=Oracle(image)
 with Path(a.output).open('w',newline='') as f:
  w=csv.writer(f);w.writerow(['night','tick','position','zombies','force','burst','counter','drumsState','recordedPosition','drumsCounter','baseGain','drumsGain','hatsGain','jump'])
  for night,key,length in ((False,'day_base',7000),(True,'night_base',11200)):
   oracle.reset(night);clip=clips[key]
   for tick in range(length):
    force=tick in (200,3500,6500,9500) if night else tick==6500
    zombies=0 if night else (10 if 100<=tick<1600 or 4000<=tick<5000 else 0)
    pos=position_at(clip,tick*clip['sampleRate']//100)
    w.writerow([int(night),tick,pos,zombies,int(force)]+oracle.tick(pos,zombies,force))
 print('Generated original x86 oracle:18200 music ticks from actual decoded rows')
if __name__=='__main__':main()
