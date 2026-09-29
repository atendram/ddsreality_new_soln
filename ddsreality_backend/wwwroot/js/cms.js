'use strict';
document.querySelectorAll('[data-dismiss]').forEach(b=>b.addEventListener('click',()=>b.parentElement.remove()));
const dialog=document.querySelector('#confirm-dialog');
let pending=null;
document.querySelectorAll('form[data-confirm]').forEach(form=>form.addEventListener('submit',event=>{
  if(form.dataset.confirmed==='true')return;
  if(!dialog || typeof dialog.showModal!=='function'){if(!window.confirm(form.dataset.confirm))event.preventDefault();return;}
  event.preventDefault();pending=form;dialog.querySelector('#confirm-message').textContent=form.dataset.confirm;dialog.showModal();
}));
if(dialog)dialog.addEventListener('close',()=>{if(dialog.returnValue==='confirm' && pending){pending.dataset.confirmed='true';pending.requestSubmit();}pending=null;});
let dirty=false;
document.querySelectorAll('form[data-dirty]').forEach(form=>{form.addEventListener('input',()=>dirty=true);form.addEventListener('submit',()=>dirty=false);});
window.addEventListener('beforeunload',event=>{if(dirty){event.preventDefault();event.returnValue='';}});
const current=new URL(location.href);
document.querySelectorAll('.sidebar nav a').forEach(a=>{const u=new URL(a.href);if(u.pathname===current.pathname && (!u.searchParams.has('kind') || u.searchParams.get('kind')===current.searchParams.get('kind')))a.classList.add('active');});
const titleInput=document.querySelector('#Title');
const slugInput=document.querySelector('#Slug');
if(titleInput && slugInput && !slugInput.value){let manual=false;slugInput.addEventListener('input',()=>manual=true);titleInput.addEventListener('input',()=>{if(!manual)slugInput.value=titleInput.value.toLowerCase().normalize('NFKD').replace(/[\u0300-\u036f]/g,'').replace(/[^a-z0-9]+/g,'-').replace(/^-+|-+$/g,'');});}
