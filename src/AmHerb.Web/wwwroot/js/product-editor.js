'use strict';
let detailsDirty=false;
const detailsForm=document.querySelector('[data-product-details]');
detailsForm?.addEventListener('input',()=>{detailsDirty=true;});
document.querySelectorAll('[data-image-file]').forEach(input=>input.addEventListener('change',()=>{
 const form=input.closest('form'),preview=form.querySelector('[data-local-preview]'),status=form.querySelector('.image-upload-status');
 if(preview.dataset.objectUrl){URL.revokeObjectURL(preview.dataset.objectUrl);delete preview.dataset.objectUrl;}
 preview.hidden=true;input.setCustomValidity('');status.textContent='';const file=input.files[0];if(!file)return;
 if(!['image/jpeg','image/png','image/webp'].includes(file.type)||file.size>5*1024*1024){input.setCustomValidity('เลือกไฟล์ JPEG, PNG หรือ WebP ขนาดไม่เกิน 5 MB');status.textContent=input.validationMessage;status.classList.add('text-danger');input.reportValidity();return;}
 status.classList.remove('text-danger');const url=URL.createObjectURL(file);preview.dataset.objectUrl=url;preview.src=url;preview.hidden=false;
 preview.onload=()=>{status.textContent=`${file.name} · ${(file.size/1024/1024).toFixed(2)} MB · ${preview.naturalWidth} × ${preview.naturalHeight} px · ยังไม่ได้บันทึก`;};
 preview.onerror=()=>{input.setCustomValidity('ไม่สามารถอ่านไฟล์ภาพนี้ได้');status.textContent=input.validationMessage;preview.hidden=true;};
}));
document.addEventListener('submit',event=>{
 const form=event.target;
 if(form.closest('.image-editor')&&detailsDirty&&!confirm('ข้อมูลสินค้าที่แก้ไขยังไม่ได้บันทึก ต้องการทำรายการภาพและละทิ้งการแก้ไขข้อมูลหรือไม่?')){event.preventDefault();return;}
 if(form.matches('[data-confirm-delete]')&&!confirm('ต้องการลบภาพนี้ออกจากสินค้าหรือไม่?')){event.preventDefault();return;}
});
