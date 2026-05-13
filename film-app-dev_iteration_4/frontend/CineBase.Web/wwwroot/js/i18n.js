/**
 * CineBase i18n — Sistema di traduzione IT/EN
 * Si auto-esegue su DOMContentLoaded
 */
(function () {
  var LANG = localStorage.getItem('cinebase-lang') || 'it';

  var EN = {
    // Navbar
    'Home': 'Home', 'Programmazione': 'Schedule', 'Cinema': 'Cinemas',
    'Film': 'Movies', 'Offerte': 'Offers', 'Profilo': 'Profile',
    'Watchlist': 'Watchlist', 'Admin': 'Admin', 'Area Admin': 'Admin Area',
    'Accedi': 'Log In', 'Registrati': 'Sign Up', 'Logout': 'Log Out',
    'Notifiche': 'Notifications', 'Cerca film': 'Search movies',
    'Cambia tema': 'Toggle theme', 'Lingua': 'Language',
    'Prenotazioni': 'Bookings', 'Menu': 'Menu',

    // Cinema page
    'I Nostri Cinema': 'Our Cinemas',
    'Caricamento...': 'Loading...',
    'Nessun cinema disponibile': 'No cinemas available',
    'Al momento non ci sono cinema nella rete': 'No cinemas in the network yet',

    // Watchlist
    'La Mia Watchlist': 'My Watchlist',
    'Nessun film salvato': 'No saved movies',
    'Salva i film che vuoi vedere dalla scheda film': 'Save movies you want to watch from the movie page',
    'Scopri i film': 'Browse movies',
    'Accesso richiesto': 'Login required',
    'Accedi per vedere la tua watchlist': 'Log in to view your watchlist',
    'Salvato il': 'Saved on',
    'Film salvato nella watchlist!': 'Movie saved to watchlist!',
    'Film rimosso dalla watchlist': 'Movie removed from watchlist',

    // Home
    'In Evidenza Questa Settimana': 'Featured This Week',
    'Una selezione dei titoli più rilevanti del momento': 'A selection of this week\'s most relevant titles',
    'Vai alla Programmazione': 'Go to Schedule',
    'Per Te': 'For You',
    'Nuove uscite': 'New Releases',
    'ogni settimana': 'every week',
    'Prenota': 'Book Now',
    'il tuo posto': 'your seat',

    // Programmazione / Detail
    'Regia': 'Directed by',
    'Accessibilità': 'Accessibility',
    'Sottotitoli': 'Subtitles',
    'Audio Descrizione': 'Audio Description',
    'Nessuno show disponibile': 'No shows available',
    'Non ci sono spettacoli per la data selezionata': 'No shows for the selected date',

    // Schedule filters
    'Suggerimenti basati sui tuoi gusti': 'Personalized recommendations',
    'Nessun suggerimento al momento': 'No recommendations yet',
    'Guarda qualche film per ricevere consigli personalizzati!': 'Watch some movies to get personalized tips!',

    // Map
    'Mappa': 'Map', 'Chiudi': 'Close',

    // Recommendations
    'L\'AI sta analizzando i tuoi gusti...': 'AI is analyzing your taste...',
    'Suggerimenti basati sui tuoi gusti cinematografici': 'Based on your movie taste',

    // Admin Dashboard
    'Dashboard': 'Dashboard', 'Gestione Cinema': 'Cinema Management',
    'Gestisci le sale cinematografiche': 'Manage cinema halls',
    'Aggiungi Cinema': 'Add Cinema', 'Modifica Cinema': 'Edit Cinema',
    'Conferma Eliminazione': 'Confirm Deletion',
    'Sei sicuro di voler eliminare': 'Are you sure you want to delete',
    'Annulla': 'Cancel', 'Elimina': 'Delete',
    'Nome': 'Name', 'Indirizzo': 'Address', 'Città': 'City',
    'Salva': 'Save', 'Azioni': 'Actions',
    'Nessun risultato': 'No results',
    'Pagina': 'Page', 'di': 'of',

    // Admin Films
    'Gestione Film': 'Movie Management',
    'Gestisci il catalogo film': 'Manage movie catalog',
    'Aggiungi Film': 'Add Movie', 'Modifica Film': 'Edit Movie',
    'Titolo': 'Title', 'Anno': 'Year', 'Durata': 'Duration',
    'Regista': 'Director', 'min': 'min',

    // Admin common
    'Utenti': 'Users', 'Validazione': 'Validation',
    'Categorie': 'Categories', 'Proiezioni': 'Screenings',
    'Impostazioni': 'Settings', 'Report Esportabili': 'Export Reports',
    'Scarica CSV': 'Download CSV',
    'Esporta i dati delle vendite in formato CSV': 'Export sales data as CSV',
    'Data da': 'From date', 'Data a': 'To date',
    'Errore download report': 'Download error',

    // Admin alerts
    'Alert Occupazione Sale': 'Hall Occupancy Alerts',
    'biglietti': 'tickets',

    // Auth
    'Email': 'Email', 'Password': 'Password',
    'Accedi al tuo account': 'Log in to your account',
    'Non hai un account?': 'Don\'t have an account?',
    'Hai già un account?': 'Already have an account?',
    'Crea account': 'Create account',

    // General
    'Errore caricamento': 'Loading error',
    'Errore nel caricamento': 'Error loading',
    'Caricamento': 'Loading',
    'Torna alla lista': 'Back to list',
    'Torna indietro': 'Go back',
    'minuti': 'minutes'
  };

  function translatePage() {
    if (LANG === 'it') return;

    var strings = Object.keys(EN);
    var values = Object.values(EN);

    // Translate all text nodes in body
    var walker = document.createTreeWalker(document.body, 4, null, false);
    var nodes = [];
    while (walker.nextNode()) nodes.push(walker.currentNode);

    nodes.forEach(function(node) {
      var text = node.nodeValue;
      if (!text || text.trim().length < 2) return;
      var p = node.parentElement;
      if (!p || p.tagName === 'SCRIPT' || p.tagName === 'STYLE' || p.tagName === 'TEXTAREA') return;

      for (var i = 0; i < strings.length; i++) {
        if (strings[i].length <= 2) continue; // Skip very short keys to avoid false matches
        if (text.indexOf(strings[i]) !== -1) {
          node.nodeValue = text.split(strings[i]).join(values[i]);
          text = node.nodeValue;
        }
      }
    });

    // Translate placeholder, title, aria-label attributes
    ['placeholder', 'title', 'aria-label'].forEach(function(attr) {
      document.querySelectorAll('[' + attr + ']').forEach(function(el) {
        var val = el.getAttribute(attr);
        if (!val) return;
        for (var i = 0; i < strings.length; i++) {
          if (strings[i].length <= 2) continue;
          if (val.indexOf(strings[i]) !== -1) {
            el.setAttribute(attr, val.split(strings[i]).join(values[i]));
            val = el.getAttribute(attr);
          }
        }
      });
    });
  }

  // Run on every DOM change — but guard against re-entrancy to avoid infinite loops
  var _translating = false;
  function safeTranslate() {
    if (_translating) return;
    _translating = true;
    try { translatePage(); } finally { _translating = false; }
  }

  if (document.body) safeTranslate();
  document.addEventListener('DOMContentLoaded', safeTranslate);
  var observer = new MutationObserver(function() {
    if (!_translating) safeTranslate();
  });
  if (document.body) {
    observer.observe(document.body, { childList: true, subtree: true });
  } else {
    document.addEventListener('DOMContentLoaded', function() {
      observer.observe(document.body, { childList: true, subtree: true });
    });
  }
})();
