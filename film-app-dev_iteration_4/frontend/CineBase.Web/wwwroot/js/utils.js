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
  const toastContainer = document.getElementById('toast-container');
  if (!toastContainer) return;
  
  const colors = {
    success: 'bg-emerald-500',
    danger: 'bg-red-500',
    warning: 'bg-amber-500',
    info: 'bg-blue-500'
  };
  
  const toastId = 'toast-' + Date.now();
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
    const toast = document.getElementById(toastId);
    if (toast) toast.remove();
  }, 3000);
}

// Conferma eliminazione
function confirmDelete(itemName, callback) {
  const confirmed = confirm(`Sei sicuro di voler eliminare "${itemName}"?`);
  if (confirmed) callback();
}

// Formatta importo in EUR
function formatCurrency(amount) {
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
