// Categorie Page JavaScript
let allCategorie = [];

document.addEventListener('DOMContentLoaded', async () => {
  setupFormSubmit();
  await loadCategorie();
});

function normalizeCollection(data) {
  if (Array.isArray(data)) return data;
  if (Array.isArray(data?.$values)) return data.$values;
  if (Array.isArray(data?.items)) return data.items;
  return [];
}

async function loadCategorie() {
  const tableBody = document.getElementById('categorie-table-body');
  if (!tableBody) return;

  try {
    allCategorie = normalizeCollection(await API.getCategorie());
    renderCategorie(allCategorie);
  } catch (error) {
    handleApiError(error);
    tableBody.innerHTML = '<tr><td colspan="3" class="px-6 py-4 text-center text-brand-error">Errore nel caricamento delle categorie</td></tr>';
  }
}

function renderCategorie(categorie) {
  const tableBody = document.getElementById('categorie-table-body');
  if (!tableBody) return;

  if (!categorie.length) {
    tableBody.innerHTML = '<tr><td colspan="3" class="px-6 py-4 text-center text-brand-on-surface-variant">Nessuna categoria trovata</td></tr>';
    return;
  }

  tableBody.innerHTML = categorie.map(categoria => `
    <tr class="row-hover">
      <td class="px-6 py-4 whitespace-nowrap text-sm text-brand-on-surface-variant">${categoria.id}</td>
      <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-brand-on-surface">${categoria.nome}</td>
      <td class="px-6 py-4 whitespace-nowrap text-sm font-medium">
        <button onclick="editCategoria(${categoria.id}, '${escapeHtml(categoria.nome)}')" class="text-brand-gold hover:text-brand-gold-dark mr-3">
          <i class="fa-solid fa-pencil"></i>
        </button>
        <button onclick="deleteCategoria(${categoria.id}, '${escapeHtml(categoria.nome)}')" class="text-red-600 hover:text-red-900">
          <i class="fa-solid fa-trash"></i>
        </button>
      </td>
    </tr>
  `).join('');
}

function escapeHtml(text) {
  const div = document.createElement('div');
  div.textContent = text;
  return div.innerHTML.replace(/'/g, "\\'").replace(/"/g, '\\"');
}

function setupFormSubmit() {
  const form = document.getElementById('categoria-form');
  if (!form) return;

  form.addEventListener('submit', async (e) => {
    e.preventDefault();

    const formData = new FormData(form);
    const nome = formData.get('nome')?.toString().trim();
    const editId = form.dataset.editId;

    if (!nome) {
      showToast('Il nome della categoria e obbligatorio', 'error');
      return;
    }

    try {
      if (editId) {
        await API.updateCategoria(editId, { nome });
        showToast('Categoria aggiornata con successo');
      } else {
        await API.createCategoria({ nome });
        showToast('Categoria creata con successo');
      }

      closeModal('categoria-modal');
      await loadCategorie();
    } catch (error) {
      handleApiError(error);
    }
  });
}

async function editCategoria(id, nome) {
  openModal('categoria-modal', 'Modifica Categoria');
  const form = document.getElementById('categoria-form');
  form.dataset.editId = id;
  form.querySelector('[name="nome"]').value = nome;
}

async function deleteCategoria(id, nome) {
  openDeleteModal(nome, async () => {
    try {
      await API.deleteCategoria(id);
      showToast('Categoria eliminata con successo');
      await loadCategorie();
    } catch (error) {
      handleApiError(error);
    }
  });
}
