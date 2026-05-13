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
    'Da': 'From', 'A': 'To',
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
    if (LANG === 'it') return; // Nothing to translate

    var strings = Object.keys(EN);
    var values = Object.values(EN);

    // Translate all text nodes
    var walker = document.createTreeWalker(document.body, 4 /* NodeFilter.SHOW_TEXT */, null, false);
    var nodes = [];
    while (walker.nextNode()) nodes.push(walker.currentNode);

    nodes.forEach(function(node) {
      var text = node.nodeValue;
      if (!text || text.trim().length < 2) return;
      // Skip script/style content
      var p = node.parentElement;
      if (!p || p.tagName === 'SCRIPT' || p.tagName === 'STYLE') return;

      for (var i = 0; i < strings.length; i++) {
        if (text.indexOf(strings[i]) !== -1) {
          node.nodeValue = text.split(strings[i]).join(values[i]);
          text = node.nodeValue; // chain replacements
        }
      }
    });

    // Translate placeholder attributes
    document.querySelectorAll('[placeholder]').forEach(function(el) {
      for (var i = 0; i < strings.length; i++) {
        if (el.getAttribute('placeholder').indexOf(strings[i]) !== -1) {
          el.setAttribute('placeholder', el.getAttribute('placeholder').split(strings[i]).join(values[i]));
        }
      }
    });

    // Translate title attributes
    document.querySelectorAll('[title]').forEach(function(el) {
      for (var i = 0; i < strings.length; i++) {
        if (el.getAttribute('title') && el.getAttribute('title').indexOf(strings[i]) !== -1) {
          el.setAttribute('title', el.getAttribute('title').split(strings[i]).join(values[i]));
        }
      }
    });

    // Translate aria-label
    document.querySelectorAll('[aria-label]').forEach(function(el) {
      for (var i = 0; i < strings.length; i++) {
        if (el.getAttribute('aria-label') && el.getAttribute('aria-label').indexOf(strings[i]) !== -1) {
          el.setAttribute('aria-label', el.getAttribute('aria-label').split(strings[i]).join(values[i]));
        }
      }
    });
  }

  // Export API
  window.CineBaseLang = {
    lang: LANG,
    set: function(lang) {
      localStorage.setItem('cinebase-lang', lang);
      // Reload to apply translations cleanly
      window.location.reload();
    },
    t: function(key) {
      if (LANG === 'it') return key;
      return EN[key] || key;
    }
  };

  // Run on DOMContentLoaded and after dynamic content loads
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', translatePage);
  } else {
    translatePage();
  }
  // Also run after components are loaded
  document.addEventListener('components:loaded', translatePage);
  // And after any dynamic content change
  setTimeout(translatePage, 500);
})();
