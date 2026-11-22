import { test, expect } from '@playwright/test';
import { mkdir, readFile } from 'node:fs/promises';
const errors = new WeakMap();
const register = (page,name) => page.locator('.registers:not(.floating-registers) > div').filter({has:page.getByText(name,{exact:true})}).locator('dd');
const single = (page,name) => page.locator('.floating-registers > div').filter({has:page.getByText(name,{exact:true})}).locator('dd');
async function sample(page,id) {
 await page.getByLabel('Start with an example',{exact:true}).selectOption(id);
 await expect(page.getByRole('status')).toContainText('Ready');
}
async function run(page) {
 await page.getByRole('button',{name:'Run',exact:true}).click();
 await expect(page.getByRole('status')).toContainText('Program completed.');
}
async function load(page,source) {
 await page.getByLabel('Assembly source code',{exact:true}).fill(source);
 await page.getByRole('button',{name:'Load',exact:true}).click();
 await expect(page.getByRole('status')).toContainText('Ready');
}
test.beforeEach(async({page})=>{
 const caught=[];errors.set(page,caught);page.on('pageerror',error=>caught.push(error.message));
 await page.goto('/demos/stm32');
 await expect(page.getByRole('status')).toContainText('Ready');
 await page.getByRole('combobox',{name:'Pace',exact:true}).selectOption('1000');
});
test.afterEach(async({page})=>{expect(errors.get(page)).toEqual([]);});
test('LED example completes with the expected output',async({page})=>{
 await run(page);await expect(register(page,'R0')).toHaveText('0x00000018');
 await expect(page.locator('.pin').filter({hasText:'PA3'})).toContainText('HIGH');
 await expect(page.locator('.pin').filter({hasText:'PA4'})).toContainText('HIGH');
});
test('function call restores the stack',async({page})=>{
 await sample(page,'calls');await run(page);await expect(register(page,'R0')).toHaveText('0x0000000C');await expect(register(page,'R13 / SP')).toHaveText('0x20010000');
});
test('DSP example exposes Q and GE',async({page})=>{
 await sample(page,'dsp');await run(page);await expect(register(page,'R2')).toHaveText('0x7FFF7FFF');await expect(register(page,'R3')).toHaveText('0x0000007F');
 await expect(page.locator('.flags .flag').filter({hasText:/^Q/})).toContainText('1');await expect(page.locator('.flags .flag').filter({hasText:/^GE/})).toContainText('F');
});
test('floating point inspector shows raw results and FPSCR',async({page},info)=>{
 await sample(page,'float');await run(page);await page.getByText('Floating-point registers',{exact:false}).click();
 await expect(single(page,'S2')).toContainText('0x40400000');await expect(single(page,'S3')).toContainText('0x00000002');await expect(single(page,'S4')).toContainText('0x00000001');
 await expect(page.locator('.processor-reading')).toContainText('0x00400010');
 await mkdir('artifacts/browser-proofs',{recursive:true});
 await page.locator('.processor-panel').screenshot({path:`artifacts/browser-proofs/${info.project.name}-fpu.png`});
});
test('event, interrupt and service-call stops are resumable',async({page})=>{
 await sample(page,'waits');await page.getByRole('button',{name:'Run',exact:true}).click();await expect(page.getByRole('status')).toContainText('Waiting for event.');
 await expect(page.getByRole('button',{name:'Run',exact:true})).toBeDisabled();await expect(page.getByRole('button',{name:'Step',exact:true})).toBeDisabled();
 await page.getByRole('button',{name:'Signal event',exact:true}).click();await expect(page.getByRole('status')).toContainText('Woken.');await expect(register(page,'R0')).toHaveText('0x00000000');
 await page.getByRole('button',{name:'Run',exact:true}).click();await expect(page.getByRole('status')).toContainText('Waiting for interrupt.');
 await page.getByRole('button',{name:'Signal interrupt',exact:true}).click();await page.getByRole('button',{name:'Run',exact:true}).click();await expect(page.getByRole('status')).toContainText('Service call #42.');await expect(register(page,'R0')).toHaveText('0x00000002');
 await run(page);await expect(register(page,'R0')).toHaveText('0x00000003');
});
test('invalid source is retained and cannot replace machine state',async({page})=>{
 await run(page);const source='UNKNOWN r0,#1';await page.getByLabel('Assembly source code',{exact:true}).fill(source);await page.getByRole('button',{name:'Load',exact:true}).click();
 await expect(page.getByRole('alert')).toContainText('Line 1');await expect(register(page,'R0')).toHaveText('0x00000018');await expect(page.getByLabel('Assembly source code',{exact:true})).toHaveValue(source);
 await page.getByRole('button',{name:'Reset',exact:true}).click();await expect(page.getByLabel('Assembly source code',{exact:true})).toHaveValue(source);await expect(register(page,'R0')).toHaveText('0x00000000');
});
test('GPIO input follows both toggle directions',async({page})=>{
 await sample(page,'buttons');await page.getByRole('button',{name:'Run',exact:true}).click();const input=page.getByRole('button',{name:'PB0 LOW',exact:true});await input.click();
 await expect(page.locator('.pin').filter({hasText:'PA3'})).toContainText('HIGH');await page.getByRole('button',{name:'PB0 HIGH',exact:true}).click();await expect(page.locator('.pin').filter({hasText:'PA3'})).toContainText('LOW');await page.getByRole('button',{name:'Pause',exact:true}).click();
});
test('timer example retains quarter duty',async({page})=>{
 await sample(page,'pwm');await page.getByRole('button',{name:'Run',exact:true}).click();await expect(page.locator('.pwm-heading')).toContainText('25');await page.getByRole('button',{name:'Pause',exact:true}).click();
});
test('WebAssembly agrees with native reference results across FPU forms',async({page})=>{
 const fixture=JSON.parse(await readFile(new URL('../Stm32Emulator.Core.Tests/Fixtures/fpu-instruction-vectors.json',import.meta.url),'utf8'));
 const byForm=new Map();for(const row of fixture.vectors)if(!byForm.has(row.instruction))byForm.set(row.instruction,row);
 const invalidFma=fixture.vectors.find(row=>row.instruction==='VFMA.F32 s0,s1,s2' && row.registers[0]===0x7fc11111 && row.registers[1]===0 && row.registers[2]===0x7f800000 && row.fpscr===0);
 const cases=[...byForm.values(),...fixture.vectors.filter((_,index)=>index%547===0).slice(0,48),invalidFma];
 const hex=value=>`0x${value.toString(16).padStart(8,'0')}`;
 const source=cases.map(row=>row.registers.map((value,index)=>`MOV r0,#${hex(value)}\nVMOV s${index},r0`).join('\n')+`\nMOV r0,#${hex(row.fpscr)}\nVMSR FPSCR,r0\n${row.instruction}\nVMOV r0,s0\nCMP r0,#${hex(row.expected)}\nBNE failure\nVMRS r0,FPSCR\nCMP r0,#${hex(row.expected_fpscr)}\nBNE failure`).join('\n')+'\nB passed\nfailure: MOV r9,#0xbad\nB finish\npassed: MOV r9,#0x600d\nfinish: NOP';
 await load(page,source);await run(page);await expect(register(page,'R9')).toHaveText('0x0000600D');
});
test('inspectors fit the viewport and controls remain touch sized',async({page},info)=>{
 await page.getByText('Floating-point registers',{exact:false}).click();await page.getByText('System registers',{exact:false}).click();
 const size=await page.evaluate(()=>({viewport:innerWidth,width:document.documentElement.scrollWidth}));expect(size.width).toBeLessThanOrEqual(size.viewport);
 for(const name of ['Run','Step','Reset','Signal event','Signal interrupt']){const rect=await page.getByRole('button',{name,exact:true}).boundingBox();expect(rect.height).toBeGreaterThanOrEqual(44);}
 await expect(page.getByRole('button',{name:'Signal event',exact:true})).toBeVisible();
 await mkdir('artifacts/browser-proofs',{recursive:true});await page.screenshot({path:`artifacts/browser-proofs/${info.project.name}-full.png`,fullPage:true});
});
