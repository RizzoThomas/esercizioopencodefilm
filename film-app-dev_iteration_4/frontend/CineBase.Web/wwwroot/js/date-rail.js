// DateRail - Reusable horizontal date rail component
// Uses LOCAL dates throughout (not UTC) to avoid timezone shift bugs.
// Usage:
//   const rail = DateRail.create('rail-container', { days: 14, onDateSelected: (date) => { ... } });

const DateRail = {
  create(containerId, options = {}) {
    // Variabile days: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const days = options.days || 14;
    // Variabile onDateSelected: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const onDateSelected = options.onDateSelected || function() {};

    // Variabile container: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const container = document.getElementById(containerId);
    if (!container) return null;

    // Variabile selectedDate: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    let selectedDate = null;
    // Variabile dates: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const dates = [];
    // Variabile today: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    for (let i = 0; i < days; i++) {
      const d = new Date(today);
      d.setDate(today.getDate() + i);
      dates.push(d);
    }

    // Funzione localDateKey: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
    function localDateKey(date) {
      const y = date.getFullYear();
      const m = String(date.getMonth() + 1).padStart(2, '0');
      const d = String(date.getDate()).padStart(2, '0');
      return `${y}-${m}-${d}`;
    }

    // Funzione render: costruisce markup o componenti UI a partire dai dati in ingresso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
    function render() {
      // Variabile dayNames: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const dayNames = ['Dom', 'Lun', 'Mar', 'Mer', 'Gio', 'Ven', 'Sab'];
      // Variabile monthNames: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const monthNames = ['Gen', 'Feb', 'Mar', 'Apr', 'Mag', 'Giu', 'Lug', 'Ago', 'Set', 'Ott', 'Nov', 'Dic'];

      // Variabile html: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      let html = '<div class="relative">';
      html += '<button id="date-rail-prev" class="date-rail-arrow date-rail-arrow-left" type="button" aria-label="Date precedenti">';
      html += '<i class="fa-solid fa-chevron-left"></i></button>';

      html += '<div class="date-rail-scroll" id="date-rail-scroll">';

      dates.forEach((d, idx) => {
        // Variabile dateKey: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const dateKey = localDateKey(d);
        // Variabile isSelected: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const isSelected = selectedDate && localDateKey(selectedDate) === dateKey;
        // Variabile isToday: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const isToday = idx === 0;
        // Variabile dayName: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const dayName = isToday ? 'Oggi' : dayNames[d.getDay()];
        // Variabile dayNum: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const dayNum = d.getDate();
        // Variabile monthName: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const monthName = monthNames[d.getMonth()];

        html += `<button class="date-rail-btn ${isSelected ? 'date-rail-btn-active' : ''}" data-date="${dateKey}" type="button">`;
        html += `<span class="date-rail-day-name">${dayName}</span>`;
        html += `<span class="date-rail-day-num">${dayNum}</span>`;
        html += `<span class="date-rail-month">${monthName}</span>`;
        html += '</button>';
      });

      html += '</div>';

      html += '<button id="date-rail-next" class="date-rail-arrow date-rail-arrow-right" type="button" aria-label="Date successive">';
      html += '<i class="fa-solid fa-chevron-right"></i></button>';
      html += '</div>';

      container.innerHTML = html;

      container.querySelectorAll('.date-rail-btn').forEach(btn => {
        // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
        btn.addEventListener('click', () => {
          // Variabile dateStr: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
          const dateStr = btn.dataset.date;
          // Variabile parts: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
          const parts = dateStr.split('-');
          selectedDate = new Date(parseInt(parts[0]), parseInt(parts[1]) - 1, parseInt(parts[2]));
          selectedDate.setHours(0, 0, 0, 0);
          render();
          if (onDateSelected) onDateSelected(selectedDate);
        });
      });

      // Variabile prevBtn: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const prevBtn = document.getElementById('date-rail-prev');
      // Variabile nextBtn: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const nextBtn = document.getElementById('date-rail-next');
      // Variabile scrollEl: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const scrollEl = document.getElementById('date-rail-scroll');

      if (prevBtn && scrollEl) {
        // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
        prevBtn.addEventListener('click', () => {
          scrollEl.scrollBy({ left: -200, behavior: 'smooth' });
        });
      }
      if (nextBtn && scrollEl) {
        // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
        nextBtn.addEventListener('click', () => {
          scrollEl.scrollBy({ left: 200, behavior: 'smooth' });
        });
      }
    }

    // Funzione setSelectedDate: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
    function setSelectedDate(date) {
      selectedDate = date;
      render();
    }

    // Funzione getSelectedDate: recupera un valore derivato e lo restituisce al chiamante. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
    function getSelectedDate() {
      return selectedDate;
    }

    // Funzione getDateKey: recupera un valore derivato e lo restituisce al chiamante. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
    function getDateKey(date) {
      return localDateKey(date);
    }

    selectedDate = dates[0];
    render();

    return {
      setSelectedDate,
      getSelectedDate,
      getDateKey,
      render
    };
  }
};
