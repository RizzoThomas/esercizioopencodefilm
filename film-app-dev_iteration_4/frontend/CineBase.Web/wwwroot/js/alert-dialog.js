/**
 * CineBase Alert Dialog — 21st.dev style confirm modal
 */
window.CineBaseAlert = function({ title, description, confirmText, cancelText, icon, onConfirm }) {
  // Variabile overlay: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const overlay = document.createElement('div');
  overlay.className = 'alert-overlay';
  
  // Variabile iconHtml: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const iconHtml = icon ? `<i class="fa-solid ${icon} text-ferrari-primary"></i>` : '';
  
  overlay.innerHTML = `
    <div class="alert-dialog">
      <div class="alert-dialog-title">${iconHtml} ${title || 'Conferma'}</div>
      <div class="alert-dialog-desc">${description || 'Sei sicuro?'}</div>
      <div class="alert-dialog-actions">
        <button class="btn-outline text-sm" id="alert-cancel">${cancelText || 'Annulla'}</button>
        <button class="btn-primary text-sm" id="alert-confirm">${confirmText || 'Conferma'}</button>
      </div>
    </div>
  `;
  
  document.body.appendChild(overlay);
  
  overlay.querySelector('#alert-cancel').onclick = () => overlay.remove();
  overlay.querySelector('#alert-confirm').onclick = () => {
    overlay.remove();
    if (onConfirm) onConfirm();
  };
  overlay.onclick = (e) => { if (e.target === overlay) overlay.remove(); };
  
  return { close: () => overlay.remove() };
};
