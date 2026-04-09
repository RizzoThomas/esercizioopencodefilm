// Proiezioni Page JavaScript
let allProiezioni = [];
let allCinemas = [];
let allFilms = [];

function normalizeCollection(data) {
  if (Array.isArray(data)) return data;
  if (Array.isArray(data?.$values)) return data.$values;
  if (Array.isArray(data?.items)) return data.items;
  return [];
}

document.addEventListener('DOMContentLoaded', async () => {
  await Promise.all([loadProiezioni(), loadCinemasList(), loadFilmsList()]);
  setupFormSubmit();
  populateSelects();
});

async function loadProiezioni() {
  const tableBody = document.getElementById('proiezioni-table-body');
  if (!tableBody) return;
  
  try {
    const proiezioni = normalizeCollection(await API.getProiezioni());
    allProiezioni = proiezioni;
    renderProiezioni(proiezioni);
    updateStats(proiezioni);
  } catch (error) {
    handleApiError(error);
    tableBody.innerHTML = '<tr><td colspan="7" class="px-6 py-4 text-center text-red-500">Errore nel caricamento delle proiezioni</td></tr>';
  }
}

async function loadCinemasList() {
  try {
    allCinemas = normalizeCollection(await API.getCinemas());
  } catch (error) {
    console.error('Error loading cinemas:', error);
  }
}

async function loadFilmsList() {
  try {
    allFilms = normalizeCollection(await API.getFilms());
  } catch (error) {
    console.error('Error loading films:', error);
  }
}

function populateSelects() {
  populateSelect('cinema-select', allCinemas, 'id', ['nome'], 'Seleziona Cinema');
  populateSelect('film-select', allFilms, 'id', ['titolo'], 'Seleziona Film');
}

function renderProiezioni(proiezioni) {
  const tableBody = document.getElementById('proiezioni-table-body');
  if (!tableBody) return;
  
  if (!proiezioni.length) {
    tableBody.innerHTML = '<tr><td colspan="7" class="px-6 py-4 text-center text-gray-500">Nessuna proiezione trovata</td></tr>';
    return;
  }
  
  tableBody.innerHTML = proiezioni.map(proiezione => `
    <tr class="hover:bg-slate-50 transition-colors">
      <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">${proiezione.id}</td>
      <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900">${getCinemaName(proiezione.cinemaId)}</td>
      <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900">${getFilmTitle(proiezione.filmId)}</td>
      <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">${formatDate(proiezione.data)}</td>
      <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">${formatTime(proiezione.ora)}</td>
      <td class="px-6 py-4 whitespace-nowrap">
        <span class="px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-800">
          Pianificata
        </span>
      </td>
      <td class="px-6 py-4 whitespace-nowrap text-sm font-medium">
        <button onclick="editProiezione(${proiezione.id})" class="text-indigo-600 hover:text-indigo-900 mr-3">
          <i class="fa-solid fa-pencil"></i>
        </button>
        <button onclick="deleteProiezione(${proiezione.id}, '${getFilmTitle(proiezione.filmId)}')" class="text-red-600 hover:text-red-900">
          <i class="fa-solid fa-trash"></i>
        </button>
      </td>
    </tr>
  `).join('');
}

function getCinemaName(cinemaId) {
  const cinema = allCinemas.find(c => Number(c.id) === Number(cinemaId));
  return cinema ? cinema.nome : `ID ${cinemaId}`;
}

function getFilmTitle(filmId) {
  const film = allFilms.find(f => Number(f.id) === Number(filmId));
  return film ? film.titolo : `ID ${filmId}`;
}

function updateStats(proiezioni) {
  const totalProiezioniEl = document.getElementById('total-proiezioni');
  if (totalProiezioniEl) totalProiezioniEl.textContent = String(proiezioni.length);
}

function setupFormSubmit() {
  const form = document.getElementById('proiezione-form');
  if (!form) return;
  
  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    
    const data = serializeForm('proiezione-form');
    delete data.postiTotali;
    if (data.cinemaId) data.cinemaId = Number(data.cinemaId);
    if (data.filmId) data.filmId = Number(data.filmId);

    if (data.data && data.ora) {
      data.ora = `${data.data}T${data.ora}:00`;
    }

    const editId = form.dataset.editId;
    
    try {
      if (editId) {
        await API.updateProiezione(editId, data);
        showToast('Proiezione aggiornata con successo');
      } else {
        await API.createProiezione(data);
        showToast('Proiezione creata con successo');
      }
      
      closeModal('proiezione-modal');
      loadProiezioni();
    } catch (error) {
      handleApiError(error);
    }
  });
}

async function editProiezione(id) {
  try {
    const proiezione = await API.getProiezione(id);
    openModal('proiezione-modal', 'Modifica Proiezione');
    
    const form = document.getElementById('proiezione-form');
    form.dataset.editId = id;
    
    form.querySelector('[name="cinemaId"]').value = proiezione.cinemaId || '';
    form.querySelector('[name="filmId"]').value = proiezione.filmId || '';
    form.querySelector('[name="data"]').value = formatDateForInput(proiezione.data);
    form.querySelector('[name="ora"]').value = formatTime(proiezione.ora);
    form.querySelector('[name="postiTotali"]').value = '100';
  } catch (error) {
    handleApiError(error);
  }
}

async function deleteProiezione(id, title) {
  openDeleteModal(title, async () => {
    try {
      await API.deleteProiezione(id);
      showToast('Proiezione eliminata con successo');
      loadProiezioni();
    } catch (error) {
      handleApiError(error);
    }
  });
}
