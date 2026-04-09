// Registi Page JavaScript
let allRegisti = [];

document.addEventListener('DOMContentLoaded', async () => {
  bindSearch();
  await loadRegisti();
  setupFormSubmit();
});

async function loadRegisti() {
  const tableBody = document.getElementById('registi-table-body');
  if (!tableBody) return;
  
  try {
    const registiResponse = await API.getRegisti();
    const registi = normalizeCollection(registiResponse);
    allRegisti = registi;
    renderRegisti(registi);
    updateStats(registi);
  } catch (error) {
    handleApiError(error);
    tableBody.innerHTML = '<tr><td colspan="6" class="px-6 py-4 text-center text-red-500">Errore nel caricamento dei registi</td></tr>';
  }
}

function normalizeCollection(data) {
  if (Array.isArray(data)) return data;
  if (Array.isArray(data?.$values)) return data.$values;
  if (Array.isArray(data?.items)) return data.items;
  return [];
}

function renderRegisti(registi) {
  const tableBody = document.getElementById('registi-table-body');
  if (!tableBody) return;
  
  if (!registi.length) {
    tableBody.innerHTML = '<tr><td colspan="6" class="px-6 py-4 text-center text-gray-500">Nessun regista trovato</td></tr>';
    return;
  }
  
  tableBody.innerHTML = registi.map(regista => `
    <tr class="hover:bg-slate-50 transition-colors">
      <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">${regista.id}</td>
      <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900">${regista.nome}</td>
      <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900">${regista.cognome}</td>
      <td class="px-6 py-4 whitespace-nowrap">
        <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-800">
          ${regista.nazionalita || '-'}
        </span>
      </td>
      <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">${regista.filmCount || 0}</td>
      <td class="px-6 py-4 whitespace-nowrap text-sm font-medium">
        <button onclick="editRegista(${regista.id})" class="text-indigo-600 hover:text-indigo-900 mr-3">
          <i class="fa-solid fa-pencil"></i>
        </button>
        <button onclick="deleteRegista(${regista.id}, '${regista.nome} ${regista.cognome}')" class="text-red-600 hover:text-red-900">
          <i class="fa-solid fa-trash"></i>
        </button>
      </td>
    </tr>
  `).join('');
}

function updateStats(registi) {
  const totalRegistiEl = document.getElementById('total-registi');
  if (totalRegistiEl) totalRegistiEl.textContent = String(registi.length);

  const nationalities = new Set(registi.map(r => r.nazionalita).filter(Boolean));
  const totalNationalitiesEl = document.getElementById('total-nationalities');
  if (totalNationalitiesEl) totalNationalitiesEl.textContent = String(nationalities.size);
}

function bindSearch() {
  const searchInput = document.getElementById('search-input');
  if (!searchInput) return;

  searchInput.addEventListener('input', (e) => {
    const query = e.target.value.trim().toLowerCase();
    if (!query) {
      renderRegisti(allRegisti);
      return;
    }

    const filtered = allRegisti.filter(r =>
      (r.nome || '').toLowerCase().includes(query) ||
      (r.cognome || '').toLowerCase().includes(query) ||
      (r.nazionalita || '').toLowerCase().includes(query)
    );

    renderRegisti(filtered);
  });
}

function setupFormSubmit() {
  const form = document.getElementById('regista-form');
  if (!form) return;
  
  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    
    const data = serializeForm('regista-form');
    const editId = form.dataset.editId;
    
    try {
      if (editId) {
        await API.updateRegista(editId, data);
        showToast('Regista aggiornato con successo');
      } else {
        await API.createRegista(data);
        showToast('Regista creato con successo');
      }
      
      closeModal('regista-modal');
      loadRegisti();
    } catch (error) {
      handleApiError(error);
    }
  });
}

async function editRegista(id) {
  try {
    const regista = await API.getRegista(id);
    openModal('regista-modal', 'Modifica Regista');
    
    const form = document.getElementById('regista-form');
    form.dataset.editId = id;
    
    form.querySelector('[name="nome"]').value = regista.nome || '';
    form.querySelector('[name="cognome"]').value = regista.cognome || '';
    form.querySelector('[name="nazionalita"]').value = regista.nazionalita || '';
  } catch (error) {
    handleApiError(error);
  }
}

async function deleteRegista(id, name) {
  openDeleteModal(name, async () => {
    try {
      await API.deleteRegista(id);
      showToast('Regista eliminato con successo');
      loadRegisti();
    } catch (error) {
      handleApiError(error);
    }
  });
}
