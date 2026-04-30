let utentiData = [];

document.addEventListener('DOMContentLoaded', async () => {
  await loadUtenti();
});

async function loadUtenti() {
  const tbody = document.getElementById('utenti-table-body');
  try {
    const data = await API.getUtenti();
    utentiData = normalizeCollection(data);
    renderUtenti(utentiData);
  } catch (error) {
    tbody.innerHTML = `<tr><td colspan="8" class="px-4 py-8 text-center text-red-400">Errore caricamento utenti: ${error.message || ''}</td></tr>`;
  }
}

function renderUtenti(lista) {
  const tbody = document.getElementById('utenti-table-body');

  if (!lista.length) {
    tbody.innerHTML = `<tr><td colspan="8" class="px-4 py-8 text-center text-body">Nessun utente trovato</td></tr>`;
    return;
  }

  tbody.innerHTML = lista.map(u => {
    const ruoloBadge = getRuoloBadge(u.ruolo);
    const date = new Date(u.dataRegistrazione);
    const dateStr = date.toLocaleDateString('it-IT', { day: 'numeric', month: 'short', year: 'numeric' });

    return `
      <tr class="hover:bg-canvas-elevated/30 transition-colors">
        <td class="px-4 py-3 whitespace-nowrap text-sm text-body">${u.id}</td>
        <td class="px-4 py-3 whitespace-nowrap text-sm text-ink">${u.email}</td>
        <td class="px-4 py-3 whitespace-nowrap text-sm text-ink">${u.nome || '-'}</td>
        <td class="px-4 py-3 whitespace-nowrap text-sm text-ink">${u.cognome || '-'}</td>
        <td class="px-4 py-3 whitespace-nowrap">${ruoloBadge}</td>
        <td class="px-4 py-3 whitespace-nowrap text-sm text-right font-semibold text-ferrari-primary">${formatCurrency(u.creditoResiduo)}</td>
        <td class="px-4 py-3 whitespace-nowrap text-sm text-body">${dateStr}</td>
        <td class="px-4 py-3 whitespace-nowrap text-center">
          <a href="/utenti-detail.html?id=${u.id}" class="btn-ghost text-xs px-2 py-1 inline-block" title="Visualizza e modifica utente">
            <i class="fa-solid fa-pen mr-1"></i>Modifica
          </a>
        </td>
      </tr>`;
  }).join('');
}

function getRuoloBadge(ruolo) {
  switch (ruolo) {
    case 'Admin':
      return '<span class="inline-flex items-center gap-1 px-2 py-0.5 text-xs font-semibold bg-ferrari-primary/15 text-ferrari-primary"><i class="fa-solid fa-crown"></i>Admin</span>';
    case 'PowerUser':
      return '<span class="inline-flex items-center gap-1 px-2 py-0.5 text-xs font-semibold bg-amber-500/15 text-amber-500"><i class="fa-solid fa-bolt"></i>PowerUser</span>';
    case 'User':
      return '<span class="inline-flex items-center gap-1 px-2 py-0.5 text-xs font-semibold bg-body/15 text-body"><i class="fa-solid fa-user"></i>User</span>';
    default:
      return `<span class="text-xs">${ruolo}</span>`;
  }
}

function filterUtenti() {
  const q = document.getElementById('utenti-search').value.toLowerCase().trim();
  if (!q) {
    renderUtenti(utentiData);
    return;
  }
  const filtered = utentiData.filter(u =>
    (u.nome && u.nome.toLowerCase().includes(q)) ||
    (u.cognome && u.cognome.toLowerCase().includes(q)) ||
    (u.email && u.email.toLowerCase().includes(q))
  );
  renderUtenti(filtered);
}

function normalizeCollection(data) {
  if (Array.isArray(data)) return data;
  if (Array.isArray(data?.$values)) return data.$values;
  if (Array.isArray(data?.items)) return data.items;
  return [];
}
