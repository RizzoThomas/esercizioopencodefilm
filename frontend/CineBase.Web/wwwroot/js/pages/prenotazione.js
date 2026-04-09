// prenotazione.js - Selezione posti e conferma prenotazione

let bookingState = {
  proiezioneId: null,
  disponibilita: null,
  selectedSeats: new Set(),
  maxPrenotabili: 0,
  isSubmitting: false
};

document.addEventListener('DOMContentLoaded', async function () {
  if (!Auth.isAuthenticated()) {
    window.location.href = '/login.html?redirect=' + encodeURIComponent(window.location.pathname + window.location.search);
    return;
  }

  await loadComponent('navbar-container', '/components/navbar-landing.html');
  await loadComponent('footer-container', '/components/footer-landing.html');
  if (typeof updateNavbarForAuth === 'function') {
    updateNavbarForAuth();
  }

  const proiezioneId = getProiezioneIdFromUrl();
  if (!proiezioneId) {
    showBookingError('ID proiezione mancante o non valido');
    return;
  }

  bookingState.proiezioneId = proiezioneId;

  document.getElementById('confirm-booking-btn')?.addEventListener('click', handleConfirmBooking);

  await loadDisponibilita(proiezioneId);
});

function getProiezioneIdFromUrl() {
  const params = new URLSearchParams(window.location.search);
  const id = Number(params.get('proiezioneId'));
  return Number.isInteger(id) && id > 0 ? id : null;
}

async function loadDisponibilita(proiezioneId) {
  setLoadingState(true);
  try {
    const data = await API.getPrenotazioneDisponibilita(proiezioneId);
    bookingState.disponibilita = data;
    bookingState.maxPrenotabili = Math.max(0, Number(data.maxPostiPrenotabili || 0));
    bookingState.selectedSeats = new Set();

    renderBookingHeader(data);
    renderSeatGrid(data);
    updateSummary();

    document.getElementById('booking-loading')?.classList.add('hidden');
    document.getElementById('booking-error')?.classList.add('hidden');
    document.getElementById('booking-content')?.classList.remove('hidden');
  } catch (error) {
    handleApiError(error);
    showBookingError('Impossibile caricare la disponibilita posti per questa proiezione');
  } finally {
    setLoadingState(false);
  }
}

function showBookingError(message) {
  document.getElementById('booking-loading')?.classList.add('hidden');
  document.getElementById('booking-content')?.classList.add('hidden');
  document.getElementById('booking-error')?.classList.remove('hidden');
  const messageEl = document.getElementById('booking-error-message');
  if (messageEl) messageEl.textContent = message;
}

function renderBookingHeader(data) {
  const filmTitle = document.getElementById('film-title');
  const cinemaName = document.getElementById('cinema-name');
  const showtime = document.getElementById('showtime');
  const cover = document.getElementById('film-cover');

  if (filmTitle) filmTitle.textContent = data.filmTitolo || 'Film';
  if (cinemaName) cinemaName.textContent = `${data.cinemaNome || 'Cinema'}${data.cinemaCitta ? ', ' + data.cinemaCitta : ''}`;
  if (showtime) {
    const dateText = data.dataProiezione ? new Date(data.dataProiezione).toLocaleDateString('it-IT') : '--/--/----';
    const timeText = formatTimeFromTimespan(data.oraProiezione);
    showtime.textContent = `${dateText} - ${timeText}`;
  }
  if (cover) {
    cover.src = data.filmCopertinaPath || 'https://via.placeholder.com/300x450/1a1a2e/00f5ff?text=NO+IMAGE';
    cover.onerror = function () {
      this.src = 'https://via.placeholder.com/300x450/1a1a2e/00f5ff?text=NO+IMAGE';
    };
  }
}

function renderSeatGrid(data) {
  const seatGrid = document.getElementById('seat-grid');
  if (!seatGrid) return;

  const occupied = new Set((data.postiOccupati || []).map(normalizeSeat));
  const allSeats = (data.tuttiIPosti || []).map(normalizeSeat).filter(Boolean);

  seatGrid.innerHTML = allSeats.map((seat) => {
    const isOccupied = occupied.has(seat);
    const classes = isOccupied
      ? 'bg-pink-600/70 border-pink-400/50 text-pink-100 cursor-not-allowed'
      : 'bg-slate-800 border-slate-600 text-slate-200 hover:border-cyan-400/70 hover:text-cyan-300';

    return `<button type="button" data-seat="${seat}" ${isOccupied ? 'disabled' : ''} class="seat-btn h-9 rounded-md border text-[11px] font-mono transition-colors ${classes}">${seat}</button>`;
  }).join('');

  seatGrid.querySelectorAll('.seat-btn:not([disabled])').forEach((btn) => {
    btn.addEventListener('click', function () {
      toggleSeatSelection(String(this.dataset.seat || ''));
    });
  });
}

function toggleSeatSelection(seat) {
  const normalized = normalizeSeat(seat);
  if (!normalized) return;

  if (bookingState.selectedSeats.has(normalized)) {
    bookingState.selectedSeats.delete(normalized);
  } else {
    if (bookingState.selectedSeats.size >= bookingState.maxPrenotabili) {
      showToast(`Puoi selezionare massimo ${bookingState.maxPrenotabili} posti`, 'warning');
      return;
    }
    bookingState.selectedSeats.add(normalized);
  }

  updateSeatSelectionUI();
  updateSummary();
}

function updateSeatSelectionUI() {
  document.querySelectorAll('.seat-btn[data-seat]').forEach((btn) => {
    const seat = normalizeSeat(String(btn.dataset.seat || ''));
    const selected = bookingState.selectedSeats.has(seat);
    if (selected) {
      btn.classList.remove('bg-slate-800', 'border-slate-600', 'text-slate-200', 'hover:border-cyan-400/70', 'hover:text-cyan-300');
      btn.classList.add('bg-cyan-500/70', 'border-cyan-300/60', 'text-cyan-50');
    } else if (!btn.disabled) {
      btn.classList.remove('bg-cyan-500/70', 'border-cyan-300/60', 'text-cyan-50');
      btn.classList.add('bg-slate-800', 'border-slate-600', 'text-slate-200', 'hover:border-cyan-400/70', 'hover:text-cyan-300');
    }
  });
}

function updateSummary() {
  const data = bookingState.disponibilita;
  if (!data) return;

  const selectedCount = bookingState.selectedSeats.size;
  const unitPrice = 10;
  const total = selectedCount * unitPrice;

  const capienza = document.getElementById('summary-capienza');
  const occupati = document.getElementById('summary-occupati');
  const disponibili = document.getElementById('summary-disponibili');
  const selezionati = document.getElementById('summary-selezionati');
  const totale = document.getElementById('summary-totale');
  const selectedSeats = document.getElementById('selected-seats');
  const confirmBtn = document.getElementById('confirm-booking-btn');

  if (capienza) capienza.textContent = String(data.capienzaCinema || 0);
  if (occupati) occupati.textContent = String(data.postiPrenotati || 0);
  if (disponibili) disponibili.textContent = String(data.postiDisponibili || 0);
  if (selezionati) selezionati.textContent = String(selectedCount);
  if (totale) totale.textContent = `${total.toFixed(2)} EUR`;

  if (selectedSeats) {
    if (selectedCount === 0) {
      selectedSeats.textContent = 'Nessuno';
    } else {
      selectedSeats.textContent = Array.from(bookingState.selectedSeats).sort().join(', ');
    }
  }

  if (confirmBtn) {
    const canConfirm = selectedCount > 0 && !bookingState.isSubmitting;
    confirmBtn.disabled = !canConfirm;
  }
}

async function handleConfirmBooking() {
  if (!bookingState.disponibilita || bookingState.selectedSeats.size === 0) {
    return;
  }

  const confirmBtn = document.getElementById('confirm-booking-btn');
  bookingState.isSubmitting = true;
  if (confirmBtn) {
    confirmBtn.disabled = true;
    confirmBtn.innerHTML = '<i class="fa-solid fa-circle-notch fa-spin mr-1"></i> Conferma in corso...';
  }

  try {
    const posti = Array.from(bookingState.selectedSeats).sort();
    await API.createPrenotazione({
      proiezioneId: bookingState.proiezioneId,
      numeroPosti: posti.length,
      posti: posti
    });

    showToast('Prenotazione completata con successo', 'success');
    window.location.href = '/area-personale.html#prenotazioni';
  } catch (error) {
    handleApiError(error);
    await loadDisponibilita(bookingState.proiezioneId);
  } finally {
    bookingState.isSubmitting = false;
    if (confirmBtn) {
      confirmBtn.innerHTML = 'Conferma Prenotazione';
    }
    updateSummary();
  }
}

function setLoadingState(isLoading) {
  bookingState.isSubmitting = Boolean(isLoading);
  const confirmBtn = document.getElementById('confirm-booking-btn');
  if (confirmBtn) {
    confirmBtn.disabled = isLoading || bookingState.selectedSeats.size === 0;
  }
}

function normalizeSeat(seat) {
  return String(seat || '').trim().toUpperCase();
}

function formatTimeFromTimespan(value) {
  if (!value) return '--:--';
  if (typeof value === 'string') return value.slice(0, 5);
  if (typeof value === 'object' && value !== null) {
    const h = Number(value.hours ?? 0);
    const m = Number(value.minutes ?? 0);
    return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}`;
  }
  return '--:--';
}
