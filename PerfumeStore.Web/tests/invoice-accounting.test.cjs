const {test}=require('node:test');const assert=require('node:assert/strict');const fs=require('node:fs');const path=require('node:path');const vm=require('node:vm');
const c=vm.createContext({});vm.runInContext(fs.readFileSync(path.join(__dirname,'../assets/invoice-accounting.js'),'utf8'),c);
for(const purchase of [false,true]){
 const body={Products:{1:1},[purchase?'UnitCosts':'UnitPrices']:{1:80},AmountPaid:75,HasDebt:false,DebtAmount:0};
 test((purchase?'Purchase':'Sales')+' rejects paid 75 for line total 80',()=>{assert.match(c.invoiceSettlementMessage(body,purchase),/₪80.00/);assert.match(c.invoiceSettlementMessage(body,purchase),/₪75.00/);});
 test((purchase?'Purchase':'Sales')+' accepts paid plus debt equal to total',()=>{assert.equal(c.invoiceSettlementMessage({...body,HasDebt:true,DebtAmount:5},purchase),null);assert.equal(c.invoiceSettlementMessage({...body,AmountPaid:0,HasDebt:true,DebtAmount:80},purchase),null);});
 test((purchase?'Purchase':'Sales')+' ignores unchecked debt and refuses overpayment',()=>{assert.ok(c.invoiceSettlementMessage({...body,DebtAmount:5},purchase));assert.ok(c.invoiceSettlementMessage({...body,AmountPaid:85},purchase));});
}
test('money is compared in cents rather than floating-point sums',()=>{assert.equal(c.invoiceSettlementMessage({Products:{1:3},UnitPrices:{1:0.1},AmountPaid:0.1,HasDebt:true,DebtAmount:0.2}),null);});

test('credit reminder dismisses without losing credit and returns when amounts change',()=>{
 const storage=new Map();const notices=[];
 const document={addEventListener(){},getElementById(){return {before(p){notices.push(p);}};},createElement(){return {children:[],setAttribute(){},appendChild(x){this.children.push(x);},addEventListener(_,fn){this.click=fn;},remove(){this.removed=true;}};}};
 const ctx=vm.createContext({document,localStorage:{getItem:k=>storage.get(k),setItem:(k,v)=>storage.set(k,v)}});
 vm.runInContext(fs.readFileSync(path.join(__dirname,'../assets/invoice-accounting.js'),'utf8'),ctx);
 const invoice={creditRefundDue:10,itemsTotal:60,appliedVoucherPayments:30};
 ctx.showInvoiceCreditNotice(invoice,'sales',1);
 assert.equal(notices.length,1);assert.match(notices[0].textContent,/₪10.00/);
 notices[0].children[0].click();assert.equal(notices[0].removed,true);
 ctx.showInvoiceCreditNotice(invoice,'sales',1);assert.equal(notices.length,1);
 assert.equal(invoice.creditRefundDue,10);
 ctx.showInvoiceCreditNotice({...invoice,creditRefundDue:15,itemsTotal:55},'sales',1);assert.equal(notices.length,2);
});
