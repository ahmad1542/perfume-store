const {test}=require('node:test');const assert=require('node:assert/strict');const fs=require('node:fs');const vm=require('node:vm');const path=require('node:path');
function setup(fail=false){
 const els={categoryId:{value:'2',options:[{value:'2',textContent:'Old category'}]},renameCategoryName:{value:'',focus(){}},btnSaveCategoryName:{},renameCategoryDialog:{showModal(){this.open=true},close(){this.open=false}}};let sent;
 const ctx=vm.createContext({document:{addEventListener(){}},$:id=>els[id],CATEGORIES_API:'/api/ProductCategories',setMsg(){},getFriendlyMessage:e=>e.message,apiGetJson:async()=>({id:2,name:'Old category',description:'Keep this description'}),apiSendJson:async(url,method,body)=>{if(fail)throw new Error('Duplicate name');sent={url,method,body};}});
 vm.runInContext(fs.readFileSync(path.join(__dirname,'../Products/category-rename.js'),'utf8'),ctx);
 return {ctx,els,sent:()=>sent};
}
test('rename prefills name, preserves description and category ID',async()=>{
 const {ctx,els,sent}=setup();await ctx.openCategoryRename();assert.equal(els.renameCategoryName.value,'Old category');
 els.renameCategoryName.value=' New category ';await ctx.saveCategoryName();
 assert.equal(sent().url,'/api/ProductCategories/2');assert.equal(sent().method,'PATCH');
 assert.equal(sent().body.name,'New category');assert.equal(sent().body.description,'Keep this description');
 assert.equal(els.categoryId.value,'2');assert.equal(els.categoryId.options[0].textContent,'New category');assert.equal(els.renameCategoryDialog.open,false);
});
test('failed rename keeps dialog open and existing option unchanged',async()=>{
 const {ctx,els}=setup(true);await ctx.openCategoryRename();els.renameCategoryName.value='Duplicate category';await ctx.saveCategoryName();
 assert.equal(els.renameCategoryDialog.open,true);assert.equal(els.categoryId.options[0].textContent,'Old category');assert.equal(els.btnSaveCategoryName.disabled,false);
});
