// DateRail - Reusable horizontal date rail component
// Uses LOCAL dates throughout (not UTC) to avoid timezone shift bugs.
// Usage:
//   const rail = DateRail.create('rail-container', { days: 14, onDateSelected: (date) => { ... } });

const DateRail = {
  create(containerId, options = {}) {
    const days = options.days || 14;
    const onDateSelected = options.onDateSelected || function() {};

    const container = document.getElementById(containerId);
    if (!container) return null;

    let selectedDate = null;
    const dates = [];
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    for (let i = 0; i < days; i++) {
      const d = new Date(today);
      d.setDate(today.getDate() + i);
      dates.push(d);
    }

    function localDateKey(date) {
      const y = date.getFullYear();
      const m = String(date.getMonth() + 1).padStart(2, '0');
      const d = String(date.getDate()).padStart(2, '0');
      return `${y}-${m}-${d}`;
    }

    function render() {
      const dayNames = ['Dom', 'Lun', 'Mar', 'Mer', 'Gio', 'Ven', 'Sab'];
      const monthNames = ['Gen', 'Feb', 'Mar', 'Apr', 'Mag', 'Giu', 'Lug', 'Ago', 'Set', 'Ott', 'Nov', 'Dic'];

      let html = '<div class="relative">';
      html += '<button id="date-rail-prev" class="date-rail-arrow date-rail-arrow-left" type="button" aria-label="Date precedenti">';
      html += '<i class="fa-solid fa-chevron-left"></i></button>';

      html += '<div class="date-rail-scroll" id="date-rail-scroll">';

      dates.forEach((d, idx) => {
        const dateKey = localDateKey(d);
        const isSelected = selectedDate && localDateKey(selectedDate) === dateKey;
        const isToday = idx === 0;
        const dayName = isToday ? 'Oggi' : dayNames[d.getDay()];
        const dayNum = d.getDate();
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
        btn.addEventListener('click', () => {
          const dateStr = btn.dataset.date;
          const parts = dateStr.split('-');
          selectedDate = new Date(parseInt(parts[0]), parseInt(parts[1]) - 1, parseInt(parts[2]));
          selectedDate.setHours(0, 0, 0, 0);
          render();
          if (onDateSelected) onDateSelected(selectedDate);
        });
      });

      const prevBtn = document.getElementById('date-rail-prev');
      const nextBtn = document.getElementById('date-rail-next');
      const scrollEl = document.getElementById('date-rail-scroll');

      if (prevBtn && scrollEl) {
        prevBtn.addEventListener('click', () => {
          scrollEl.scrollBy({ left: -200, behavior: 'smooth' });
        });
      }
      if (nextBtn && scrollEl) {
        nextBtn.addEventListener('click', () => {
          scrollEl.scrollBy({ left: 200, behavior: 'smooth' });
        });
      }
    }

    function setSelectedDate(date) {
      selectedDate = date;
      render();
    }

    function getSelectedDate() {
      return selectedDate;
    }

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
