const {test}=require('node:test');
const assert=require('node:assert/strict');
const fs=require('node:fs');
const path=require('node:path');
const vm=require('node:vm');
const read=p=>fs.readFileSync(path.join(__dirname,'..',p),'utf8');

test('dashboard preserves displayed data on error and clears the warning after a successful retry',async()=>{
 const message={hidden:true}; let fail=false;let bound=0;
 const context=vm.createContext({document:{getElementById:id=>id==='dashboardMsg'?message:null},window:{APP_CONFIG:{API_BASE_URL:'/api'}},console:{error(){}},apiGetJson:async()=>{if(fail)throw Error('HTTP 500');return {sales:123};}});
 vm.runInContext(read('Dashboard/dashboard.js').replace(/wireActions\(\);\s*loadDashboard\(\);\s*$/,''),context);
 context.bindDashboard=()=>bound++;
 await context.loadDashboard();assert.equal(bound,1);
 fail=true;await context.loadDashboard();assert.equal(bound,1);assert.equal(message.hidden,false);assert.match(message.textContent,/out of date/);
 fail=false;await context.loadDashboard();assert.equal(bound,2);assert.equal(message.hidden,true);
});

test('inventory batch details use the inventory table styles and show batch costs',async()=>{
 const details={innerHTML:'',appendChild(node){this.table=node;}};
 const context=vm.createContext({getQueryParam:()=>1,setMsg(){},API:'/api/Inventory',apiGetJson:async()=>({quantity:4,batches:[{remainingQuantity:4,unitCost:750,purchaseInvoiceID:2}]}),escapeHtml:String,$:()=>details,document:{createElement:()=>({})},console});
 vm.runInContext(read('Inventory/view.js'),context);await context.loadView();
 assert.match(details.table.innerHTML,/class="inventory-table"/);assert.match(details.table.innerHTML,/3000.00/);assert.match(details.table.innerHTML,/Purchase #2/);
});
