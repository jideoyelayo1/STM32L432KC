#!/usr/bin/env python3
"""Run real assembled Thumb/DSP instructions on QEMU's Cortex-M4, independent of the interpreter."""
import argparse,json,pathlib,random,shutil,struct,subprocess
ROOT=pathlib.Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('--check',action='store_true');args=p.parse_args()
cache=ROOT/'artifacts/reference';cache.mkdir(parents=True,exist_ok=True)
forms=[]
def add(names,operands):
 for name in names.split():forms.append((name,operands))
add('ADC ADCS ADD ADDS AND ANDS BIC BICS EOR EORS ORN ORNS ORR ORRS RSB RSBS SBC SBCS SUB SUBS','r0,r1,r2')
add('ASR ASRS LSL LSLS LSR LSRS ROR RORS','r0,r1,r2')
add('MOV MOVS MVN MVNS CLZ RBIT REV REV16 REVSH RRX RRXS','r0,r1')
add('CMP CMN TST TEQ','r1,r2')
add('BFC','r0,#5,#17');add('BFI SBFX UBFX','r0,r1,#5,#17')
add('SSAT','r0,#12,r1');add('USAT','r0,#12,r1');add('SSAT16','r0,#8,r1');add('USAT16','r0,#8,r1')
add('SXTB SXTB16 SXTH UXTB UXTB16 UXTH','r0,r1');add('SXTAB SXTAB16 SXTAH UXTAB UXTAB16 UXTAH','r0,r1,r2')
add('PKHBT','r0,r1,r2,LSL #8');add('PKHTB','r0,r1,r2,ASR #8');add('SEL QADD QDADD QDSUB QSUB','r0,r1,r2')
for prefix in ['S','Q','SH','U','UQ','UH']:
 add(' '.join(prefix+s for s in ['ADD16','ADD8','ASX','SAX','SUB16','SUB8']),'r0,r1,r2')
add('MUL SDIV UDIV USAD8 SMMUL SMMULR SMUAD SMUADX SMUSD SMUSDX SMULBB SMULBT SMULTB SMULTT SMULWB SMULWT','r0,r1,r2')
add('MULS','r0,r0,r2')
add('MLA MLS USADA8 SMLABB SMLABT SMLATB SMLATT SMLAD SMLADX SMLAWB SMLAWT SMLSD SMLSDX SMMLA SMMLAR SMMLS SMMLSR','r0,r1,r2,r3')
add('UMULL UMLAL UMAAL SMULL SMLAL SMLALBB SMLALBT SMLALTB SMLALTT SMLALD SMLALDX SMLSLD SMLSLDX','r0,r1,r2,r3')
for special in ['APSR','IAPSR','EAPSR','XPSR','IPSR','EPSR','IEPSR']:
 add('MRS',f'r0,{special}')
rng=random.Random(432)
edge=[0,1,0xffffffff,0x80000000,0x7fffffff,0x80008000,0x7fff7fff,0x00ff80ff,0x01234567,0x89abcdef,31,32,33,255]
rows=[]
for name,operand in forms:
 for n in range(20):
  values=[rng.getrandbits(32) for _ in range(4)]+[0]*8
  if n<len(edge):values[1]=edge[n];values[2]=edge[-n-1];values[0]=edge[(n+2)%len(edge)];values[3]=edge[(n+5)%len(edge)]
  rows.append(dict(instruction=f'{name} {operand}',registers=values,apsr=rng.getrandbits(32)&0xf80f0000))
from qemu_helper import mov,run
snippets=[]
for row in rows:
 snippet=[]
 for r,value in enumerate(row['registers']):snippet+=mov(f'r{r}',value)
 snippet+=mov('r12',row['apsr'])+['msr APSR_nzcvq,r12','msr APSR_g,r12',row['instruction'],'ldr r12,=OUTPUT_ADDRESS','stmia r12!,{r0-r11}','mrs r0,APSR','str r0,[r12]'];snippets.append(snippet)
for row,values in zip(rows,run(snippets,13,'cpu-oracle')):row['expected']=list(values[:12]);row['expected_apsr']=values[12]&0xf80f0000
version=subprocess.check_output(['qemu-system-arm','--version'],text=True).splitlines()[0]
output=json.dumps(dict(reference='QEMU Cortex-M4 / mps2-an386',seed=432,vectors=rows),separators=(',',':'))+'\n'
target=ROOT/'tests/Stm32Emulator.Core.Tests/Fixtures/cpu-vectors.json'
if args.check:
 if target.read_text()!=output:raise SystemExit('Committed CPU fixtures differ from reference')
else:target.write_text(output)
print(f'Verified {len(rows)} instruction vectors on {version}')
