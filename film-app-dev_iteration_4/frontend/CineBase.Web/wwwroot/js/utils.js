// API_BASE_URL declared in api.js — global reference

// Formattazione data ISO -> DD/MM/YYYY
function formatDate(isoDate) {
  if (!isoDate) return '-';
  // Estrai solo la parte data dall'ISO per evitare problemi timezone
  const datePart = isoDate.split('T')[0];
  const [year, month, day] = datePart.split('-');
  return `${day}/${month}/${year}`;
}

// Formattazione data per input date (YYYY-MM-DD)
function formatDateForInput(isoDate) {
  if (!isoDate) return '';
  return isoDate.split('T')[0];
}

// Formattazione ora (HH:MM)
function formatTime(timeString) {
  if (!timeString || timeString === '00:00:00') return '';
  // Se contiene 'T', estrai l'ora dalla parte dopo la T
  if (timeString.includes('T')) {
    return timeString.split('T')[1].substring(0, 5);
  }
  return timeString.substring(0, 5);
}

// Troncamento testo
function truncateText(text, maxLength = 50) {
  if (!text) return '';
  return text.length > maxLength ? text.substring(0, maxLength) + '...' : text;
}

// Gestione errori API
function handleApiError(error) {
  console.error('API Error:', error);
  
  // Variabile message: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  let message = 'Si è verificato un errore';

  if (error.status === 0) {
    message = error.message || 'Backend non raggiungibile';
    showToast(message, 'danger');
    return message;
  }
  
  switch (error.status) {
    case 400:
      message = error.errors ? Object.values(error.errors).flat().join(', ') : (error.message || 'Dati non validi');
      break;
    case 404:
      message = error.message || 'Elemento non trovato';
      break;
    case 409:
      message = error.message || 'Elemento già esistente (conflitto)';
      break;
    case 500:
      message = error.message || 'Errore del server';
      break;
    default:
      message = error.message || message;
      break;
  }
  
  showToast(message, 'danger');
  return message;
}

// Toast notification (Tailwind version)
function showToast(message, type = 'success') {
  // Variabile toastContainer: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const toastContainer = document.getElementById('toast-container');
  if (!toastContainer) return;
  
  // Variabile colors: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const colors = {
    success: 'bg-emerald-500',
    danger: 'bg-red-500',
    warning: 'bg-amber-500',
    info: 'bg-blue-500'
  };
  
  // Variabile toastId: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const toastId = 'toast-' + Date.now();
  // Variabile toastHtml: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const toastHtml = `
    <div id="${toastId}" class="${colors[type]} text-white px-6 py-3 shadow-lg flex items-center gap-3 animate-fade-in">
      <span>${message}</span>
      <button onclick="this.parentElement.remove()" class="hover:bg-white/20 rounded p-1">
        <i class="fa-solid fa-xmark"></i>
      </button>
    </div>
  `;
  
  toastContainer.insertAdjacentHTML('beforeend', toastHtml);
  
  // Auto-remove after 3 seconds
  setTimeout(() => {
    // Variabile toast: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const toast = document.getElementById(toastId);
    if (toast) toast.remove();
  }, 3000);
}

// Conferma eliminazione
function confirmDelete(itemName, callback) {
  // Variabile confirmed: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const confirmed = confirm(`Sei sicuro di voler eliminare "${itemName}"?`);
  if (confirmed) callback();
}

// Formatta importo in EUR
function formatCurrency(amount) {
  // Variabile val: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  var val = parseFloat(amount);
  if (isNaN(val)) return '0,00 \u20AC';
  return new Intl.NumberFormat('it-IT', { style: 'currency', currency: 'EUR' }).format(val);
}

// Ottiene URL copertina film con fallback
function getCoverImage(copertinaPath) {
  if (!copertinaPath) return '/assets/images/defaults/cover-default.svg';
  if (copertinaPath.startsWith('/media/')) {
    return `${API_BASE_URL}${copertinaPath}`;
  }
  if (!copertinaPath.includes('/') && !copertinaPath.startsWith('http')) {
    return `${API_BASE_URL}/media/${copertinaPath}`;
  }
  if (copertinaPath.startsWith('http')) {
    return copertinaPath;
  }
  return '/assets/images/defaults/cover-default.svg';
}

// Counter animation — anima un numero da 0 al target (21st.dev style)
function animateCounter(el, target, duration = 1200) {
  // Variabile start: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const start = 0;
  // Variabile startTime: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const startTime = performance.now();
  // Variabile isInt: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const isInt = Number.isInteger(target);
  // Funzione update: aggiorna lo stato o il DOM in base ai dati correnti. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
  function update(now) {
    // Variabile elapsed: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const elapsed = now - startTime;
    // Variabile progress: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const progress = Math.min(elapsed / duration, 1);
    // Easing out cubic
    const eased = 1 - Math.pow(1 - progress, 3);
    // Variabile current: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const current = start + (target - start) * eased;
    el.textContent = isInt ? Math.round(current).toLocaleString('it-IT') : current.toLocaleString('it-IT', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    el.classList.add('counted');
    if (progress < 1) requestAnimationFrame(update);
  }
  requestAnimationFrame(update);
}
