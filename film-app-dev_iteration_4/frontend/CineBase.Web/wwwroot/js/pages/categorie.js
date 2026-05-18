// Categorie Page JavaScript
let allCategorie = [];

// Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
document.addEventListener('DOMContentLoaded', async () => {
  setupFormSubmit();
  await loadCategorie();
});

// Funzione normalizeCollection: normalizza il valore in ingresso per confronti stabili. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function normalizeCollection(data) {
  if (Array.isArray(data)) return data;
  if (Array.isArray(data?.$values)) return data.$values;
  if (Array.isArray(data?.items)) return data.items;
  return [];
}

// Funzione loadCategorie: carica i dati iniziali o aggiorna il contenuto visibile della pagina. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function loadCategorie() {
  // Variabile tableBody: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const tableBody = document.getElementById('categorie-table-body');
  if (!tableBody) return;

  try {
    allCategorie = normalizeCollection(await API.getCategorie());
    renderCategorie(allCategorie);
  } catch (error) {
    handleApiError(error);
    tableBody.innerHTML = '<tr><td colspan="3" class="px-6 py-4 text-center text-ferrari-semantic-warning">Errore nel caricamento delle categorie</td></tr>';
  }
}

// Funzione renderCategorie: costruisce markup o componenti UI a partire dai dati in ingresso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function renderCategorie(categorie) {
  // Variabile tableBody: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const tableBody = document.getElementById('categorie-table-body');
  if (!tableBody) return;

  if (!categorie.length) {
    tableBody.innerHTML = '<tr><td colspan="3" class="px-6 py-4 text-center text-body">Nessuna categoria trovata</td></tr>';
    return;
  }

  tableBody.innerHTML = categorie.map(categoria => `
    <tr class="row-hover">
      <td class="px-6 py-4 whitespace-nowrap text-sm text-body">${categoria.id}</td>
      <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-ink">${categoria.nome}</td>
      <td class="px-6 py-4 whitespace-nowrap text-sm font-medium">
        <button onclick="editCategoria(${categoria.id}, '${escapeHtml(categoria.nome)}')" class="text-ferrari-primary hover:text-ferrari-primary-hover mr-3">
          <i class="fa-solid fa-pencil"></i>
        </button>
        <button onclick="deleteCategoria(${categoria.id}, '${escapeHtml(categoria.nome)}')" class="text-red-600 hover:text-red-900">
          <i class="fa-solid fa-trash"></i>
        </button>
      </td>
    </tr>
  `).join('');
}

// Funzione escapeHtml: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function escapeHtml(text) {
  // Variabile div: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const div = document.createElement('div');
  div.textContent = text;
  return div.innerHTML.replace(/'/g, "\\'").replace(/"/g, '\\"');
}

// Funzione setupFormSubmit: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function setupFormSubmit() {
  // Variabile form: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const form = document.getElementById('categoria-form');
  if (!form) return;

  // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
  form.addEventListener('submit', async (e) => {
    e.preventDefault();

    // Variabile formData: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const formData = new FormData(form);
    // Variabile nome: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const nome = formData.get('nome')?.toString().trim();
    // Variabile editId: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
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

// Funzione editCategoria: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function editCategoria(id, nome) {
  openModal('categoria-modal', 'Modifica Categoria');
  // Variabile form: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const form = document.getElementById('categoria-form');
  form.dataset.editId = id;
  form.querySelector('[name="nome"]').value = nome;
}

// Funzione deleteCategoria: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
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
