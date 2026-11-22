"""Self-contained Cortex-M4 harness: assemble a relocation-free ELF section and run its raw image."""
import pathlib,struct,subprocess
ROOT=pathlib.Path(__file__).resolve().parents[2]
def mov(reg,value):return [f'movw {reg},#{value&65535}',f'movt {reg},#{value>>16}']
def run(snippets,words,name,enable_fpu=False):
 cache=ROOT/'artifacts/reference';cache.mkdir(parents=True,exist_ok=True)
 asm=['.syntax unified','.cpu cortex-m4','.fpu fpv4-sp-d16','.thumb','.section .text,"ax"','.word 0x20010000,9','.thumb_func','_start:']
 if enable_fpu:asm+=['ldr r0,=0xe000ed88','ldr r1,=0xf00000','str r1,[r0]','dsb','isb']
 base=0x20010000
 for n,snippet in enumerate(snippets):
  asm += [line.replace('OUTPUT_ADDRESS',f'0x{base+n*words*4:x}') for line in snippet]
  if n%8==7:asm += [f'b pool_{n}','.balign 4','.ltorg',f'pool_{n}:']
 size=len(snippets)*words*4
 asm += ['ldr r1,=0x20000000','adr r0,terminal','str r0,[r1]','movs r0,#4','str r0,[r1,#4]','movs r0,#3','str r0,[r1,#8]','movs r0,#1','bkpt #0xab','ldr r1,=0x20000000','str r0,[r1]',f'ldr r2,=0x{base:x}','str r2,[r1,#4]',f'ldr r2,={size}','str r2,[r1,#8]','movs r0,#5','bkpt #0xab','ldr r1,=0x20000000','ldr r2,=0x20026','str r2,[r1]','movs r2,#0','str r2,[r1,#4]','movs r0,#0x20','bkpt #0xab','b .','.balign 4','terminal: .asciz ":tt"','.balign 4','.ltorg']
 source=cache/(name+'.s');source.write_text('\n'.join(asm)+'\n');obj=cache/(name+'.o')
 subprocess.run(['clang','--target=arm-none-eabi','-mcpu=cortex-m4','-mthumb','-c',str(source),'-o',str(obj)],check=True)
 elf=obj.read_bytes()
 if elf[:6]!=b'\x7fELF\x01\x01':raise RuntimeError('Expected little-endian ELF32')
 shoff=struct.unpack_from('<I',elf,32)[0];entsize,count,namesindex=struct.unpack_from('<HHH',elf,46)
 sections=[struct.unpack_from('<10I',elf,shoff+n*entsize) for n in range(count)]
 names=sections[namesindex];strings=elf[names[4]:names[4]+names[5]]
 def section_name(s):return strings[s[0]:].split(b'\0',1)[0]
 if any(s[1] in [4,9] and s[5] for s in sections):raise RuntimeError('Unexpected unresolved reference-harness relocation')
 text=next(s for s in sections if section_name(s)==b'.text');image=cache/(name+'.bin');image.write_bytes(elf[text[4]:text[4]+text[5]])
 process=subprocess.run(['qemu-system-arm','-M','mps2-an386','-cpu','cortex-m4','-display','none','-monitor','none','-serial','none','-semihosting-config','enable=on,target=native','-kernel',str(image)],stdout=subprocess.PIPE,stderr=subprocess.PIPE,timeout=60,check=True)
 if len(process.stdout)!=size:raise RuntimeError(f'Incomplete QEMU output: {len(process.stdout)} != {size}; stderr: {process.stderr[:500]!r}')
 return [struct.unpack_from('<'+str(words)+'I',process.stdout,n*words*4) for n in range(len(snippets))]
