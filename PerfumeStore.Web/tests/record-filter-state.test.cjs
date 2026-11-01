const {test}=require('node:test');const assert=require('node:assert/strict');const vm=require('node:vm');const fs=require('node:fs');const path=require('node:path');
const source=fs.readFileSync(path.join(__dirname,'../assets/record-filters.js'),'utf8');
function setup({pathname='/SalesInvoices/',search='',saved=null,blocked=false}={}){
 const storage=new Map(saved?[['perfume:filters:SalesInvoices',JSON.stringify(saved)]]:[]),events={},nodes={searchInput:{value:''},fromDateInput:{value:''},toDateInput:{value:''}};let address,loads=0;
 const c=vm.createContext({location:{pathname,search,origin:'https://example.test',href:'https://example.test'+pathname+search},URL,URLSearchParams,
  sessionStorage:{getItem:k=>{if(blocked)throw Error('blocked');return storage.get(k)},setItem:(k,v)=>storage.set(k,v)},
  window:{history:{replaceState:(_,__,value)=>address=value},loadList:()=>loads++},
  document:{getElementById:id=>nodes[id],querySelectorAll:()=>[],addEventListener:(name,fn)=>events[name]=fn}});
 vm.runInContext(source,c);return {filters:vm.runInContext('RecordFilters',c),nodes,storage,events,address:()=>address,loads:()=>loads};
}
test('returning from View/Edit restores a folder URL and search without resetting later typing',async()=>{
 const s=setup({saved:{returning:true,query:'search=old&fromDate=2026-10-20&toDate=2026-10-31'}});await s.filters.ready;
 assert.match(s.address(),/fromDate=2026-10-20/);s.filters.restoreLegacy();assert.equal(s.nodes.searchInput.value,'old');
 s.nodes.searchInput.value='new';s.filters.restoreLegacy();assert.equal(s.nodes.searchInput.value,'new');
});
test('explicit URL filters override a pending return state',async()=>{
 const s=setup({search:'?search=explicit',saved:{returning:true,query:'search=old'}});await s.filters.ready;s.filters.restoreLegacy();assert.equal(s.nodes.searchInput.value,'explicit');
});
test('opening View/Edit marks stored filters for a one-time return',async()=>{
 const s=setup();await s.filters.ready;s.filters.remember(new URLSearchParams('search=rent&brandId=2'));
 s.events.click({target:{closest:()=>({href:'https://example.test/SalesInvoices/edit.html?id=7'})}});
 const saved=JSON.parse(s.storage.get('perfume:filters:SalesInvoices'));assert.equal(saved.returning,true);assert.match(saved.query,/brandId=2/);
});
test('Clear filters resets dates to this month and search but does not save records',async()=>{
 const s=setup();await s.filters.ready;s.nodes.searchInput.value='rent';s.filters.clear();
 assert.equal(s.nodes.searchInput.value,'');assert.equal(s.nodes.fromDateInput.value.slice(-2),'01');assert.equal(s.loads(),1);
});
test('unavailable session storage does not prevent filtering',async()=>{
 const s=setup({blocked:true});await s.filters.ready;s.filters.append(new URLSearchParams('search=test'));assert.match(s.address(),/search=test/);
});
