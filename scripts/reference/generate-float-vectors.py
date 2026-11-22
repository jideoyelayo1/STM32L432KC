#!/usr/bin/env python3
"""Generate independent numerical vectors; no reference code is shipped in the lab."""
import argparse, hashlib, io, json, pathlib, random, subprocess, tempfile, urllib.request, zipfile
ROOT=pathlib.Path(__file__).resolve().parents[2]
SHA='21130ce885d35c1fe73fc1e1bf2244178167e05c6747cad5f450cc991714c746'
URL='https://www.jhauser.us/arithmetic/SoftFloat-3e.zip'
p=argparse.ArgumentParser();p.add_argument('--softfloat',type=pathlib.Path);p.add_argument('--check',action='store_true');args=p.parse_args()
cache=ROOT/'artifacts/reference';cache.mkdir(parents=True,exist_ok=True)
if args.softfloat:source=args.softfloat.resolve()
else:
 archive=cache/'SoftFloat-3e.zip'
 if not archive.exists():archive.write_bytes(urllib.request.urlopen(URL,timeout=60).read())
 payload=archive.read_bytes()
 if hashlib.sha256(payload).hexdigest()!=SHA:raise SystemExit('SoftFloat archive digest mismatch')
 with zipfile.ZipFile(io.BytesIO(payload)) as z:
  if any(pathlib.PurePosixPath(n).is_absolute() or '..' in pathlib.PurePosixPath(n).parts for n in z.namelist()):raise SystemExit('Unsafe reference archive')
  z.extractall(cache)
 source=cache/'SoftFloat-3e'
build=source/'build/Linux-x86_64-GCC'
subprocess.run(['make','-s','-C',str(build),'SPECIALIZE_TYPE=ARM-VFPv2','-j4'],check=True)
exe=cache/'float-oracle'
subprocess.run(['cc','-std=c99','-I'+str(source/'source/include'),str(ROOT/'scripts/reference/softfloat-oracle.c'),str(build/'softfloat.a'),'-o',str(exe)],check=True)
rng=random.Random(432)
edge=[0,0x80000000,1,0x80000001,0x007fffff,0x00800000,0x00800001,0x3f000000,0x3f7fffff,0x3f800000,0x3f800001,0x3f800002,0xbf800000,0x40000000,0x40400000,0x7f7fffff,0xff7fffff,0x7f800000,0xff800000,0x7fc12345,0x7f812345,0xffc12345,0xff812345,0x4f000000,0x4f800000,0xcf000000]
rows=[]
for mode in range(4):
 for op in ['ADD','SUB','MUL','DIV','SQRT','FMA','I32','U32','FROM_I32','FROM_U32','HALF','FROM_HALF']:
  triples=[(x,y,0x3f800000) for x in edge for y in (edge if op in ['ADD','SUB','MUL','DIV','FMA'] else [0])]
  triples += [(rng.getrandbits(32),rng.getrandbits(32),rng.getrandbits(32)) for _ in range(256)]
  if op=='FROM_HALF':triples=[(x,0,0) for x in [0,1,0x3ff,0x400,0x3c00,0x7bff,0x7c00,0x7d00,0x7e00,0x8000,0xfc00,0xffff]+[rng.randrange(65536) for _ in range(256)]]
  for a,b,c in triples:rows.append(dict(op=op,a=a,b=b,c=c,mode=mode))
stdin=''.join(f'{r["op"]} {r["a"]:x} {r["b"]:x} {r["c"]:x} {r["mode"]}\n' for r in rows)
result=subprocess.run([str(exe)],input=stdin,text=True,stdout=subprocess.PIPE,check=True)
values=result.stdout.splitlines()
if len(values)!=len(rows):raise SystemExit('Incomplete oracle output')
for row,value in zip(rows,values):row['result'],row['flags']=map(int,value.split())
output=json.dumps(dict(reference='Berkeley SoftFloat 3e / ARM-VFPv2',archive_sha256=SHA,seed=432,vectors=rows),separators=(',',':'))+'\n'
target=ROOT/'tests/Stm32Emulator.Core.Tests/Fixtures/float-vectors.json'
if args.check:
 if target.read_text()!=output:raise SystemExit('Committed numerical fixtures differ from reference')
else:target.write_text(output)
print(f'Verified {len(rows)} independent float vectors')
