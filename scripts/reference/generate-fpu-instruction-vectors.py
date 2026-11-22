#!/usr/bin/env python3
"""ARM-specific FPU instruction and FPSCR oracle, including DN/FZ/AHP and fixed-point forms."""
import argparse,json,pathlib,random
from qemu_helper import ROOT,mov,run
p=argparse.ArgumentParser();p.add_argument('--check',action='store_true');args=p.parse_args()
forms=[]
for op in ['VADD','VSUB','VMUL','VDIV','VMLA','VMLS','VNMLA','VNMLS','VNMUL','VFMA','VFMS','VFNMA','VFNMS']:forms.append(op+'.F32 s0,s1,s2')
for op in ['VABS','VNEG','VSQRT']:forms.append(op+'.F32 s0,s1')
for op in ['VCMP','VCMPE']:forms.append(op+'.F32 s0,s1')
for op in ['VCVT','VCVTR']:
 for t in ['S32','U32']:forms.append(f'{op}.{t}.F32 s0,s1')
for t in ['S32','U32']:forms.append(f'VCVT.F32.{t} s0,s1')
for t in ['S16','U16','S32','U32']:
 forms += [f'VCVT.{t}.F32 s0,s0,#8',f'VCVT.F32.{t} s0,s0,#8']
for op in ['VCVTB','VCVTT']:forms += [f'{op}.F32.F16 s0,s1',f'{op}.F16.F32 s0,s1']
rng=random.Random(432);edge=[0,0x80000000,1,0x80000001,0x007fffff,0x00800000,0x3f000000,0x3f800001,0xbf800000,0x40000000,0x7f7fffff,0x7f800000,0xff800000,0x7fc12345,0x7f812345,0xffc12345,0xff812345,0x00007c00,0x00007d01,0x7e01fc01,0x4f000000,0xcf000000]
rows=[];snippets=[]
for instruction in forms:
 for mode in range(4):
  for variant in range(8):
   status=(mode<<22)|((variant&1)<<24)|((variant>>1&1)<<25)|((variant>>2&1)<<26)
   for n in range(len(edge)+8):
    regs=[edge[n] if n<len(edge) else rng.getrandbits(32), edge[(n+7)%len(edge)] if n<len(edge) else rng.getrandbits(32),edge[(n+11)%len(edge)] if n<len(edge) else rng.getrandbits(32)]
    row=dict(instruction=instruction,registers=regs,fpscr=status);rows.append(row);snippet=[]
    for j,value in enumerate(regs):snippet+=mov('r0',value)+[f'vmov s{j},r0']
    snippet+=mov('r0',status)+['vmsr fpscr,r0',instruction,'ldr r12,=OUTPUT_ADDRESS','vmov r0,s0','str r0,[r12]','vmrs r0,fpscr','str r0,[r12,#4]'];snippets.append(snippet)
values=run(snippets,2,'fpu-oracle',True)
for row,expected in zip(rows,values):row['expected'],row['expected_fpscr']=expected
output=json.dumps(dict(reference='QEMU Cortex-M4 FPv4-SP-D16',seed=432,vectors=rows),separators=(',',':'))+'\n';target=ROOT/'tests/Stm32Emulator.Core.Tests/Fixtures/fpu-instruction-vectors.json'
if args.check:
 if target.read_text()!=output:raise SystemExit('FPU instruction fixtures differ from QEMU')
else:target.write_text(output)
print(f'Verified {len(rows)} ARM FPU instruction cases')
