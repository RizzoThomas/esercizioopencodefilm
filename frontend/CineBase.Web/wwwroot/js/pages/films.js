// Films Page JavaScript
let allFilms = [];
let allRegisti = [];

document.addEventListener('DOMContentLoaded', async () => {
  await Promise.all([loadFilms(), loadRegistiList()]);
  populateRegistiSelect();
  setupFilters();
  setupFormSubmit();
});

function normalizeCollection(data) {
  if (Array.isArray(data)) return data;
  if (Array.isArray(data?.$values)) return data.$values;
  if (Array.isArray(data?.items)) return data.items;
  return [];
}

async function loadFilms() {
  const tableBody = document.getElementById('films-table-body');
  if (!tableBody) return;
  
  try {
    const films = normalizeCollection(await API.getFilms());
    allFilms = films;
    renderFilms(films);
    updateStats(films);
  } catch (error) {
    handleApiError(error);
    tableBody.innerHTML = '<tr><td colspan="7" class="px-6 py-4 text-center text-red-500">Errore nel caricamento dei film</td></tr>';
  }
}

async function loadRegistiList() {
  try {
    allRegisti = normalizeCollection(await API.getRegisti());
  } catch (error) {
    console.error('Error loading registi:', error);
  }
}

function populateRegistiSelect() {
  const select = document.getElementById('regista-select');
  if (!select) return;

  select.innerHTML = '<option value="">Seleziona regista</option>';
  allRegisti.forEach(regista => {
    const option = document.createElement('option');
    option.value = String(regista.id);
    option.textContent = `${regista.nome} ${regista.cognome}`;
    select.appendChild(option);
  });
}

function renderFilms(films) {
  const tableBody = document.getElementById('films-table-body');
  if (!tableBody) return;
  
  if (!films.length) {
    tableBody.innerHTML = '<tr><td colspan="7" class="px-6 py-4 text-center text-gray-500">Nessun film trovato</td></tr>';
    return;
  }
  
  tableBody.innerHTML = films.map(film => `
    <tr class="hover:bg-slate-50 transition-colors">
      <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">${film.id}</td>
      <td class="px-6 py-4 whitespace-nowrap">
        <div class="h-10 w-8 flex-shrink-0 bg-slate-200 rounded overflow-hidden">
          <img class="h-full w-full object-cover" src="${film.locandina || '/assets/images/defaults/cover-default.jpg'}" alt="${film.titolo}">
        </div>
      </td>
      <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">${film.titolo}</td>
      <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">${formatDate(film.dataProduzione)}</td>
      <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">${getRegistaName(film.registaId)}</td>
      <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">${film.durata || '-'} min</td>
      <td class="px-6 py-4 whitespace-nowrap text-sm font-medium">
        <button onclick="editFilm(${film.id})" class="text-indigo-600 hover:text-indigo-900 mr-3">
          <i class="fa-solid fa-pencil"></i>
        </button>
        <button onclick="deleteFilm(${film.id}, '${film.titolo}')" class="text-red-600 hover:text-red-900">
          <i class="fa-solid fa-trash"></i>
        </button>
      </td>
    </tr>
  `).join('');
}

function getRegistaName(registaId) {
  const regista = allRegisti.find(r => Number(r.id) === Number(registaId));
  return regista ? `${regista.nome} ${regista.cognome}` : `ID ${registaId}`;
}

function updateStats(films) {
  const totalFilmsEl = document.getElementById('total-films');
  if (totalFilmsEl) totalFilmsEl.textContent = String(films.length);

  const newReleases = films.filter(f => f.dataUscita && new Date(f.dataUscita) > new Date(Date.now() - 90 * 24 * 60 * 60 * 1000)).length;
  const newReleasesEl = document.getElementById('new-releases');
  if (newReleasesEl) newReleasesEl.textContent = String(newReleases);
}

function setupFilters() {
  const searchInput = document.getElementById('search-input');
  const genreFilter = document.getElementById('genre-filter');
  
  searchInput?.addEventListener('input', (e) => filterFilms(e.target.value, genreFilter?.value));
  genreFilter?.addEventListener('change', (e) => filterFilms(searchInput?.value, e.target.value));
}

function filterFilms(search, genre) {
  let filtered = allFilms;
  
  if (search) {
    const searchLower = search.toLowerCase();
    filtered = filtered.filter(f => 
      f.titolo?.toLowerCase().includes(searchLower) ||
      getRegistaName(f.registaId).toLowerCase().includes(searchLower)
    );
  }
  
  if (genre && genre !== 'all') {
    filtered = filtered.filter(f => f.genere && f.genere === genre);
  }
  
  renderFilms(filtered);
}

function setupFormSubmit() {
  const form = document.getElementById('film-form');
  if (!form) return;
  
  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    
    const data = serializeForm('film-form');
    if (data.registaId) data.registaId = Number(data.registaId);
    if (data.durata) data.durata = Number(data.durata);

    const editId = form.dataset.editId;
    
    try {
      if (editId) {
        await API.updateFilm(editId, data);
        showToast('Film aggiornato con successo');
      } else {
        await API.createFilm(data);
        showToast('Film creato con successo');
      }
      
      closeModal('film-modal');
      loadFilms();
    } catch (error) {
      handleApiError(error);
    }
  });
}

async function editFilm(id) {
  try {
    const film = await API.getFilm(id);
    openModal('film-modal', 'Modifica Film');
    
    const form = document.getElementById('film-form');
    form.dataset.editId = id;
    
    form.querySelector('[name="titolo"]').value = film.titolo || '';
    form.querySelector('[name="dataProduzione"]').value = formatDateForInput(film.dataProduzione);
    form.querySelector('[name="durata"]').value = film.durata || '';
    form.querySelector('[name="copertinaPath"]').value = film.copertinaPath || '';
    form.querySelector('[name="filmatoPath"]').value = film.filmatoPath || '';
    form.querySelector('[name="registaId"]').value = film.registaId || '';
  } catch (error) {
    handleApiError(error);
  }
}

async function deleteFilm(id, title) {
  openDeleteModal(title, async () => {
    try {
      await API.deleteFilm(id);
      showToast('Film eliminato con successo');
      loadFilms();
    } catch (error) {
      handleApiError(error);
    }
  });
}
