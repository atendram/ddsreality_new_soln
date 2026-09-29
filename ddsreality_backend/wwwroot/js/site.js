'use strict';
const toggle=document.querySelector('.menu-toggle');
if(toggle)toggle.addEventListener('click',()=>{const open=toggle.getAttribute('aria-expanded')!=='true';toggle.setAttribute('aria-expanded',String(open));document.querySelector('#main-nav').classList.toggle('open',open);});
document.querySelectorAll('[data-dismiss]').forEach(b=>b.addEventListener('click',()=>b.parentElement.remove()));
