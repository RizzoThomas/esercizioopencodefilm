// Formattazione data ISO -> DD/MM/YYYY
function formatDate(isoDate) {
  if (!isoDate) return '-';
  const date = new Date(isoDate);
  return date.toLocaleDateString('it-IT');
}

// Formattazione data per input date (YYYY-MM-DD)
function formatDateForInput(isoDate) {
  if (!isoDate) return '';
  return isoDate.split('T')[0];
}

// Formattazione ora (HH:MM)
function formatTime(timeString) {
  if (!timeString) return '-';
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
  
  var message = 'Si è verificato un errore di sistema';

  if (error.status === 0) {
    message = error.message || 'Backend non raggiungibile su porta 5000';
  } else if (error.status === 400) {
    message = error.errors ? Object.values(error.errors).flat().join(', ') : (error.message || 'Dati non validi');
  } else if (error.status === 404) {
    message = error.message || 'Elemento non trovato';
  } else if (error.status === 409) {
    message = error.message || 'Conflitto: elemento già esistente';
  } else if (error.status === 500) {
    message = error.message || 'Errore interno del server';
  } else {
    message = error.message || message;
  }
  
  showToast(message, 'danger');
  return message;
}

// Toast notification (Cyberpunk version)
function showToast(message, type) {
  if (!type) type = 'success';
  const toastContainer = document.getElementById('toast-container');
  if (!toastContainer) return;
  
  var icon = '';
  if (type === 'success') icon = '<i class="fa-solid fa-check-circle text-green-400"></i>';
  else if (type === 'danger') icon = '<i class="fa-solid fa-circle-exclamation text-pink-400"></i>';
  else if (type === 'warning') icon = '<i class="fa-solid fa-triangle-exclamation text-yellow-400"></i>';
  else icon = '<i class="fa-solid fa-circle-info text-cyan-400"></i>';
  
  var toastId = 'toast-' + Date.now();
  var toastHtml = '<div id="' + toastId + '" class="cyber-toast toast-' + type + '">' + icon + '<span>' + message + '</span><button onclick="this.parentElement.remove()" class="hover:opacity-70 ml-2"><i class="fa-solid fa-xmark text-xs"></i></button></div>';
  
  toastContainer.insertAdjacentHTML('beforeend', toastHtml);
  
  setTimeout(function() {
    var toast = document.getElementById(toastId);
    if (toast) toast.remove();
  }, 3000);
}

// Conferma eliminazione
function confirmDelete(itemName, callback) {
  const confirmed = confirm(`Sei sicuro di voler eliminare "${itemName}"?`);
  if (confirmed) callback();
}
