'use strict';

const posForm = document.getElementById('pos-sale');
if (posForm) {
  const tiles = [...document.querySelectorAll('.pos-product')];
  const tendered = document.getElementById('tendered');
  const format = value => value.toLocaleString('en-US', {minimumFractionDigits:2, maximumFractionDigits:2});
  const total = () => tiles.reduce((sum,tile) => { const input=tile.querySelector('.pos-quantity'); return sum + (Number(input.value)||0)*Number(input.dataset.price); },0);
  const refresh = () => { document.getElementById('pos-total').textContent=format(total()); document.getElementById('pos-change').textContent=format(Math.max(0,(Number(tendered.value)||0)-total())); };
  const increase = tile => {const input=tile.querySelector('.pos-quantity');input.value=Math.min(Number(input.max), (Number(input.value)||0)+1);refresh();};
  tiles.forEach(tile => {
    const input=tile.querySelector('.pos-quantity');
    tile.querySelector('.pos-plus').addEventListener('click',()=>increase(tile));
    tile.querySelector('.pos-minus').addEventListener('click',()=>{input.value=Math.max(0,(Number(input.value)||0)-1);refresh();});
    input.addEventListener('input',refresh);
  });
  tendered.addEventListener('input',refresh);
  const search=document.getElementById('pos-search');
  search.addEventListener('input',()=>{const term=search.value.trim().toLowerCase();tiles.forEach(t=>t.hidden=!t.dataset.search.toLowerCase().includes(term));});
  search.addEventListener('keydown',event=>{if(event.key==='Enter'){event.preventDefault();const match=tiles.find(t=>(t.dataset.code.toLowerCase()===search.value.trim().toLowerCase() || t.dataset.barcode.toLowerCase()===search.value.trim().toLowerCase()));if(match){increase(match);search.value='';tiles.forEach(t=>t.hidden=false);}}});
  document.getElementById('method').addEventListener('change',event=>{tendered.disabled=event.target.value!=='Cash';});
  posForm.addEventListener('submit',event=>{if(total()<=0){event.preventDefault();search.focus();return;} if(posForm.checkValidity()){posForm.querySelector('button[type=submit]').disabled=true;}});
}
document.querySelectorAll('[data-back]').forEach(button => button.addEventListener('click', () => window.history.back()));


