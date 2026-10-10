const {test}=require('node:test');const assert=require('node:assert/strict');const fs=require('node:fs');const vm=require('node:vm');const path=require('node:path');
const input={value:''};const selected={dataset:{unitCost:'5'}};
const ctx=vm.createContext({$:id=>id==='AddedUnitCost'?input:{selectedOptions:[selected]}});
vm.runInContext(fs.readFileSync(path.join(__dirname,'../Inventory/unit-cost.js'),'utf8'),ctx);
test('new stock uses product default and retains a zero default cost',()=>{
 ctx.prefillProductUnitCost();assert.equal(input.value,'5');
 selected.dataset.unitCost='0';ctx.prefillProductUnitCost();assert.equal(input.value,'0');selected.dataset.unitCost='5';
});
test('edit prefills newest remaining batch and ignores depleted stock',()=>{
 assert.equal(ctx.existingInventoryUnitCost({batches:[
 {id:1,receivedAt:'2026-10-01',remainingQuantity:4,unitCost:750},
 {id:2,receivedAt:'2026-10-05',remainingQuantity:2,unitCost:780},
 {id:3,receivedAt:'2026-10-09',remainingQuantity:0,unitCost:800}]}),780);
 assert.equal(ctx.existingInventoryUnitCost({batches:[]}), '5');
 assert.equal(ctx.existingInventoryUnitCost({batches:[{id:1,remainingQuantity:1,unitCost:0}]}),0);
});
