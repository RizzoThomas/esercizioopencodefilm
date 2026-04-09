// Cinemas Page JavaScript
let allCinemas = [];

function normalizeCollection(data) {
  if (Array.isArray(data)) return data;
  if (Array.isArray(data?.$values)) return data.$values;
  if (Array.isArray(data?.items)) return data.items;
  return [];
}

document.addEventListener('DOMContentLoaded', async () => {
  await loadCinemas();
  setupFormSubmit();
});

async function loadCinemas() {
  const tableBody = document.getElementById('cinemas-table-body');
  if (!tableBody) return;
  
  try {
    const cinemas = normalizeCollection(await API.getCinemas());
    allCinemas = cinemas;
    renderCinemas(cinemas);
    updateStats(cinemas);
  } catch (error) {
    handleApiError(error);
    tableBody.innerHTML = '<tr><td colspan="5" class="px-6 py-4 text-center text-red-500">Errore nel caricamento dei cinema</td></tr>';
  }
}

function renderCinemas(cinemas) {
  const tableBody = document.getElementById('cinemas-table-body');
  if (!tableBody) return;
  
  if (!cinemas.length) {
    tableBody.innerHTML = '<tr><td colspan="5" class="px-6 py-4 text-center text-gray-500">Nessun cinema trovato</td></tr>';
    return;
  }
  
  tableBody.innerHTML = cinemas.map(cinema => `
    <tr class="hover:bg-slate-50 transition-colors">
      <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">${cinema.id}</td>
      <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">${cinema.nome}</td>
      <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">${cinema.indirizzo || '-'}</td>
      <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">${cinema.citta || '-'}</td>
      <td class="px-6 py-4 whitespace-nowrap text-sm font-medium">
        <button onclick="editCinema(${cinema.id})" class="text-indigo-600 hover:text-indigo-900 mr-3">
          <i class="fa-solid fa-pencil"></i>
        </button>
        <button onclick="deleteCinema(${cinema.id}, '${cinema.nome}')" class="text-red-600 hover:text-red-900">
          <i class="fa-solid fa-trash"></i>
        </button>
      </td>
    </tr>
  `).join('');
}

function updateStats(cinemas) {
  const totalCinemasEl = document.getElementById('total-cinemas');
  if (totalCinemasEl) totalCinemasEl.textContent = String(cinemas.length);
}

function setupFormSubmit() {
  const form = document.getElementById('cinema-form');
  if (!form) return;
  
  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    
    const data = serializeForm('cinema-form');
    delete data.telefono;
    const editId = form.dataset.editId;
    
    try {
      if (editId) {
        await API.updateCinema(editId, data);
        showToast('Cinema aggiornato con successo');
      } else {
        await API.createCinema(data);
        showToast('Cinema creato con successo');
      }
      
      closeModal('cinema-modal');
      loadCinemas();
    } catch (error) {
      handleApiError(error);
    }
  });
}

async function editCinema(id) {
  try {
    const cinema = await API.getCinema(id);
    openModal('cinema-modal', 'Modifica Cinema');
    
    const form = document.getElementById('cinema-form');
    form.dataset.editId = id;
    
    form.querySelector('[name="nome"]').value = cinema.nome || '';
    form.querySelector('[name="indirizzo"]').value = cinema.indirizzo || '';
    form.querySelector('[name="citta"]').value = cinema.citta || '';
    form.querySelector('[name="telefono"]').value = '';
  } catch (error) {
    handleApiError(error);
  }
}

async function deleteCinema(id, name) {
  openDeleteModal(name, async () => {
    try {
      await API.deleteCinema(id);
      showToast('Cinema eliminato con successo');
      loadCinemas();
    } catch (error) {
      handleApiError(error);
    }
  });
}
