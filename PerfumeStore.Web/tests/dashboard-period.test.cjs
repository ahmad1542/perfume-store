const {test}=require('node:test');const assert=require('node:assert/strict');const fs=require('node:fs');const vm=require('node:vm');const path=require('node:path');
const source=fs.readFileSync(path.join(__dirname,'../Dashboard/dashboard.js'),'utf8').replace(/initializePeriodSelectors\(\);\s*wireActions\(\);\s*loadDashboard\(\);\s*$/,'');
const select=()=>({value:'',options:[],appendChild(o){this.options.push(o)},addEventListener(event,fn){this.change=fn}});
test('month and year controls start at January 2021 and request the selected period',async()=>{const month=select(),year=select();let url;const elements={dashboardMonth:month,dashboardYear:year};const c=vm.createContext({document:{getElementById:id=>elements[id],createElement:()=>({})},window:{APP_CONFIG:{API_BASE_URL:'/api'}},console,apiGetJson:async u=>{url=u;return {}}});vm.runInContext(source,c);c.bindDashboard=()=>{};c.initializePeriodSelectors();assert.equal(month.options.length,12);assert.equal(year.options.at(-1).value,2021);assert.equal(year.options[0].value,new Date().getFullYear());month.value=1;year.value=2021;await c.loadDashboard();assert.equal(url,'/api/Dashboard?year=2021&month=1');});
test('slower earlier dashboard response cannot replace a newer selected month',async()=>{let resolveFirst;const c=vm.createContext({document:{getElementById:()=>null},window:{APP_CONFIG:{API_BASE_URL:'/api'}},console,apiGetJson:()=>new Promise(r=>resolveFirst=r)});vm.runInContext(source,c);let bound;c.bindDashboard=data=>bound=data;const first=c.loadDashboard();c.apiGetJson=async()=>({month:'February'});await c.loadDashboard();resolveFirst({month:'January'});await first;assert.equal(bound.month,'February');});

test('profit summary binds the API amounts and daily charts use broad hover targets',()=>{
 const nodes={}; const c=vm.createContext({document:{getElementById:id=>nodes[id]??=({classList:{toggle(){}},addEventListener(){}})},console});
 vm.runInContext(source,c);c.renderAlerts=()=>{};c.renderExpenseBreakdown=()=>{};c.renderActivity=()=>{};c.renderCharts=()=>{};c.escapeHtml=String;
 c.bindDashboard({netProfit:99.5,salesRevenue:425,costOfGoodsSold:215.5,grossProfit:209.5,expenseTotal:110,kpis:{}});
 assert.equal(nodes.monthNetProfit.textContent,'₪99.5');assert.equal(nodes.profitRevenue.textContent,'₪425');assert.equal(nodes.profitCost.textContent,'₪215.5');assert.equal(nodes.expenseTotal.textContent,'₪110');
 const options=c.chartOptions(true);assert.equal(options.interaction.mode,'index');assert.equal(options.interaction.intersect,false);assert.equal(options.maintainAspectRatio,false);
 assert.equal(options.plugins.tooltip.callbacks.label({dataset:{label:'Sales'},parsed:{y:75}}),'Sales: ₪75');
});

test('brand chart compares sales and FIFO profit without stacking and preserves losses',()=>{
 const charts=[];const c=vm.createContext({document:{getElementById:id=>id.endsWith('Chart')?{id}:null},Chart:function(_,config){charts.push(config);},console});
 vm.runInContext(source,c);
 c.renderCharts({brands:[{name:'Brand',value:75,grossProfit:-5,isProfitEstimated:true}]});
 const brand=charts.at(-1);assert.equal(brand.type,'bar');
 assert.deepEqual(Array.from(brand.data.datasets[0].data),[75]);
 assert.deepEqual(Array.from(brand.data.datasets[1].data),[-5]);
 assert.notEqual(brand.data.datasets[0].backgroundColor,brand.data.datasets[1].backgroundColor);
 assert.ok(!brand.options.scales?.y?.stacked);
 assert.equal(brand.options.interaction.intersect,false);
 assert.match(brand.options.plugins.tooltip.callbacks.afterBody([{dataIndex:0}]),/estimated/);
});
