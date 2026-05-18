// Variabile utentiData: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let utentiData = [];

// Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
document.addEventListener('DOMContentLoaded', async () => {
  await loadUtenti();
});

// Funzione loadUtenti: carica i dati iniziali o aggiorna il contenuto visibile della pagina. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function loadUtenti() {
  // Variabile tbody: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const tbody = document.getElementById('utenti-table-body');
  try {
    // Variabile data: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const data = await API.getUtenti();
    utentiData = normalizeCollection(data);
    renderUtenti(utentiData);
  } catch (error) {
    tbody.innerHTML = `<tr><td colspan="8" class="px-4 py-8 text-center text-red-400">Errore caricamento utenti: ${error.message || ''}</td></tr>`;
  }
}

// Funzione renderUtenti: costruisce markup o componenti UI a partire dai dati in ingresso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function renderUtenti(lista) {
  // Variabile tbody: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const tbody = document.getElementById('utenti-table-body');

  if (!lista.length) {
    tbody.innerHTML = `<tr><td colspan="8" class="px-4 py-8 text-center text-body">Nessun utente trovato</td></tr>`;
    return;
  }

  tbody.innerHTML = lista.map(u => {
    // Variabile ruoloBadge: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const ruoloBadge = getRuoloBadge(u.ruolo);
    // Variabile date: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const date = new Date(u.dataRegistrazione);
    // Variabile dateStr: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
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
          <a href="/utenti-detail.html?id=${u.id}" class="btn-tertiary text-xs px-2 py-1 inline-block" title="Visualizza e modifica utente">
            <i class="fa-solid fa-pen mr-1"></i>Modifica
          </a>
        </td>
      </tr>`;
  }).join('');
}

// Funzione getRuoloBadge: recupera un valore derivato e lo restituisce al chiamante. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
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

// Funzione filterUtenti: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function filterUtenti() {
  const q = document.getElementById('utenti-search').value.toLowerCase().trim();
  if (!q) {
    renderUtenti(utentiData);
    return;
  }
  // Variabile filtered: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const filtered = utentiData.filter(u =>
    (u.nome && u.nome.toLowerCase().includes(q)) ||
    (u.cognome && u.cognome.toLowerCase().includes(q)) ||
    (u.email && u.email.toLowerCase().includes(q))
  );
  renderUtenti(filtered);
}

// Funzione normalizeCollection: normalizza il valore in ingresso per confronti stabili. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function normalizeCollection(data) {
  if (Array.isArray(data)) return data;
  if (Array.isArray(data?.$values)) return data.$values;
  if (Array.isArray(data?.items)) return data.items;
  return [];
}
