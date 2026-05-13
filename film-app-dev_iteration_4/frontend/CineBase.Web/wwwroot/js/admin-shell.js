(() => {
  const ADMIN_PATHS = new Set([
    '/dashboard.html', '/films.html', '/registi.html', '/cinemas.html',
    '/proiezioni.html', '/categorie.html', '/utenti.html', '/utenti-detail.html', '/validazione.html'
  ]);

  const PAGE_TITLES = {
    '/dashboard.html': 'Dashboard', '/films.html': 'Film', '/registi.html': 'Registi',
    '/cinemas.html': 'Cinema', '/proiezioni.html': 'Proiezioni', '/categorie.html': 'Categorie',
    '/utenti.html': 'Utenti', '/utenti-detail.html': 'Dettaglio Utente', '/validazione.html': 'Validazione'
  };

  const PAGE_ICONS = {
    '/dashboard.html': 'fa-gauge-high', '/films.html': 'fa-film', '/registi.html': 'fa-user',
    '/cinemas.html': 'fa-building', '/proiezioni.html': 'fa-clock', '/categorie.html': 'fa-tags',
    '/utenti.html': 'fa-users', '/validazione.html': 'fa-ticket-check'
  };

  function getUser() {
    return (typeof Auth !== 'undefined' && Auth?.getUser) ? Auth.getUser() : null;
  }

  function getUserRole() {
    const user = getUser();
    if (!user) return null;
    const r = String(user.ruolo || '').trim().toLowerCase();
    if (r === '2' || r === 'admin') return 'admin';
    if (r === '1' || r === 'poweruser') return 'poweruser';
    return 'user';
  }

  function isAdmin() { return getUserRole() === 'admin'; }

  function toggleSidebar() {
    const sb = document.getElementById('admin-sidebar');
    const bd = document.getElementById('admin-sidebar-backdrop');
    if (!sb) return;
    sb.classList.toggle('-translate-x-full');
    if (bd) bd.classList.toggle('hidden');
    // Hamburger animation
    document.querySelectorAll('#admin-sidebar-toggle').forEach(function(el) {
      el.classList.toggle('hamburger-open');
    });
  }

  function setActiveLinks() {
    const cp = window.location.pathname.toLowerCase();
    document.querySelectorAll('[data-admin-link]').forEach(el => {
      el.classList.toggle('active', (el.getAttribute('href') || '').toLowerCase() === cp);
    });
  }

  function updateUserUI() {
    const user = getUser();
    const elName = document.getElementById('admin-user-name');
    const elAvatar = document.getElementById('admin-user-avatar');
    if (elName) elName.textContent = user?.nome || user?.email || 'Admin';
    if (elAvatar) {
      elAvatar.textContent = ((user?.nome || 'A').charAt(0) + (user?.cognome || 'd').charAt(0)).toUpperCase();
    }
  }

  function bindActions() {
    document.getElementById('admin-sidebar-toggle')?.addEventListener('click', toggleSidebar);
    document.getElementById('admin-sidebar-backdrop')?.addEventListener('click', toggleSidebar);
    document.querySelectorAll('#admin-sidebar a[data-admin-link]').forEach(a => a.addEventListener('click', () => {
      if (window.innerWidth < 768) toggleSidebar();
    }));

    const userToggle = document.getElementById('admin-user-toggle');
    const userMenu = document.getElementById('admin-user-menu');
    if (userToggle && userMenu) {
      userToggle.addEventListener('click', () => userMenu.classList.toggle('hidden'));
      document.addEventListener('click', e => {
        if (!userToggle.contains(e.target) && !userMenu.contains(e.target)) userMenu.classList.add('hidden');
      });
    }

    document.getElementById('admin-logout-btn')?.addEventListener('click', () => {
      (Auth?.logout ? Auth.logout() : Promise.resolve()).finally(() => { window.location.href = '/index.html'; });
    });
  }

  function renderShell(main) {
    const cp = window.location.pathname.toLowerCase();
    const title = PAGE_TITLES[cp] || 'Admin';
    const icon = PAGE_ICONS[cp] || 'fa-gauge-high';

    const shell = document.createElement('div');
    shell.innerHTML = `
    <div id="admin-shell-root" class="flex h-screen overflow-hidden bg-[#f5f5f5]">
      <!-- Backdrop mobile -->
      <div id="admin-sidebar-backdrop" class="fixed inset-0 bg-black/60 z-40 hidden md:hidden backdrop-blur-sm"></div>

      <!-- Sidebar -->
      <aside id="admin-sidebar"
        class="w-64 flex-shrink-0 flex flex-col fixed md:relative inset-y-0 left-0 z-50
               -translate-x-full md:translate-x-0 transition-transform duration-300
               bg-[#fff] border-r border-[#e5e5e5]">
        
        <!-- Logo -->
        <div class="px-5 py-5 border-b border-[#e5e5e5]">
          <a href="/index.html" class="flex items-center gap-3">
            <div class="w-9 h-9 bg-ferrari-primary flex items-center justify-center">
              <i class="fa-solid fa-film text-white text-sm"></i>
            </div>
            <div>
              <span class="text-base font-bold text-[#111] tracking-tight">CineBase</span>
              <p class="text-[10px] text-[#888] uppercase tracking-widest">Admin</p>
            </div>
          </a>
        </div>

        <!-- Nav -->
        <nav class="flex-1 px-3 py-4 space-y-0.5 overflow-y-auto">
          <p class="px-3 mb-2 text-[10px] font-semibold uppercase tracking-widest text-[#999]">Menu</p>
          <a data-admin-link href="/dashboard.html" class="admin-link"><i class="fa-solid fa-gauge-high w-5"></i>Dashboard</a>
          <a data-admin-link href="/films.html" class="admin-link"><i class="fa-solid fa-film w-5"></i>Film</a>
          <a data-admin-link href="/registi.html" class="admin-link"><i class="fa-solid fa-user w-5"></i>Registi</a>
          <a data-admin-link href="/cinemas.html" class="admin-link"><i class="fa-solid fa-building w-5"></i>Cinema</a>
          <a data-admin-link href="/proiezioni.html" class="admin-link"><i class="fa-solid fa-clock w-5"></i>Proiezioni</a>
          <a data-admin-link href="/categorie.html" class="admin-link"><i class="fa-solid fa-tags w-5"></i>Categorie</a>
          <p class="px-3 mt-4 mb-2 text-[10px] font-semibold uppercase tracking-widest text-[#999]">Gestione</p>
          <a data-admin-link href="/utenti.html" class="admin-link" data-role-required="admin"><i class="fa-solid fa-users w-5"></i>Utenti</a>
          <a data-admin-link href="/validazione.html" class="admin-link"><i class="fa-solid fa-ticket-check w-5"></i>Validazione</a>
        </nav>

        <!-- Bottom -->
        <div class="p-3 border-t border-[#1f1f1f]">
          <a href="/profilo.html" class="admin-link mb-1"><i class="fa-solid fa-gear w-5"></i>Impostazioni</a>
        </div>
      </aside>

      <!-- Main content -->
      <div class="flex-1 flex flex-col min-w-0 overflow-hidden">
        <!-- Top bar -->
        <header class="flex-shrink-0 h-14 border-b border-[#e5e5e5] bg-white/90 backdrop-blur-md flex items-center px-4 lg:px-6 gap-4">
          <button id="admin-sidebar-toggle" class="md:hidden p-2 -ml-2 flex flex-col gap-[5px] text-[#555] hover:text-[#111]">
            <span class="hamburger-line"></span>
            <span class="hamburger-line"></span>
            <span class="hamburger-line"></span>
          </button>
          <div class="flex items-center gap-3 min-w-0">
            <i class="fa-solid ${icon} text-ferrari-primary text-sm hidden sm:block"></i>
            <h1 class="text-sm font-semibold text-[#111] truncate">${title}</h1>
          </div>
          <div class="flex-1"></div>
          
          <button id="admin-theme-toggle" class="p-2 text-[#555] hover:text-[#111] transition-colors" title="Cambia tema">
            <i class="fa-solid fa-moon text-sm"></i>
          </button>

          <div class="relative">
            <button id="admin-user-toggle" class="flex items-center gap-2 text-sm text-[#333] hover:text-[#111] transition-colors">
              <div id="admin-user-avatar" class="w-7 h-7 bg-ferrari-primary/20 flex items-center justify-center text-ferrari-primary font-bold text-xs">AD</div>
              <span id="admin-user-name" class="hidden sm:inline text-xs">Admin</span>
              <i class="fa-solid fa-chevron-down text-[10px] text-[#999]"></i>
            </button>
            <div id="admin-user-menu" class="hidden absolute right-0 top-full mt-2 w-52 bg-[#1a1a1a] border border-[#2a2a2a] py-1.5 z-50">
              <div class="px-4 py-2 border-b border-[#2a2a2a]">
                <p class="text-xs font-medium text-white" id="admin-user-menu-name">admin@cinebase.it</p>
              </div>
              <a href="/profilo.html" class="flex items-center gap-2 px-4 py-2 text-xs text-[#aaa] hover:text-white hover:bg-white/5"><i class="fa-solid fa-user w-4"></i>Profilo</a>
              <a href="/profilo.html#prenotazioni" class="flex items-center gap-2 px-4 py-2 text-xs text-[#aaa] hover:text-white hover:bg-white/5"><i class="fa-solid fa-ticket w-4"></i>Prenotazioni</a>
              <hr class="my-1 border-[#2a2a2a]">
              <button id="admin-logout-btn" class="w-full flex items-center gap-2 px-4 py-2 text-xs text-red-500 hover:bg-white/5 text-left"><i class="fa-solid fa-sign-out-alt w-4"></i>Logout</button>
            </div>
          </div>
        </header>

        <!-- Content -->
        <main class="flex-1 overflow-y-auto bg-[#f5f5f5]" id="admin-shell-content"></main>
      </div>
    </div>

    <style>
      .admin-link {
        display: flex; align-items: center; gap: 0.75rem;
        padding: 0.5rem 0.75rem; border-radius: 6px;
        font-size: 0.8125rem; font-weight: 500;
        color: #555; text-decoration: none;
        transition: all 0.15s ease;
      }
      .admin-link:hover { color: #111; background: rgba(0,0,0,0.04); }
      .admin-link.active { color: #111; background: rgba(218,41,28,0.08); }
      .admin-link.active i { color: var(--ferrari-primary, #da291c); }
      
      /* Light theme admin overrides */
      html.light #admin-shell-root { background: #f5f5f5; }
      html.light #admin-sidebar { background: #fff; border-color: #e5e5e5; }
      html.light #admin-sidebar .border-\[\#1f1f1f\] { border-color: #e5e5e5 !important; }
      html.light .admin-link { color: #666; }
      html.light .admin-link:hover { color: #111; background: rgba(0,0,0,0.03); }
      html.light .admin-link.active { color: #111; background: rgba(218,41,28,0.06); }
      html.light header.bg-white\/90 { background: rgba(255,255,255,0.9) !important; border-color: #e5e5e5 !important; }
      html.light #admin-shell-content { background: #f5f5f5; }
      html.light #admin-sidebar-backdrop { background: rgba(0,0,0,0.3); }
      html.light #admin-user-menu { background: #fff; border-color: #e5e5e5; }
      html.light #admin-user-menu .text-\[\#aaa\] { color: #666 !important; }
      html.light #admin-user-menu .border-\[\#2a2a2a\] { border-color: #e5e5e5 !important; }
    </style>
    `;

    document.body.prepend(shell);
    
    // Move main content into shell
    const target = document.getElementById('admin-shell-content');
    if (target) {
      main.classList.add('p-4', 'lg:p-6');
      target.appendChild(main);
    }
  }

  function initThemeToggle() {
    var btn = document.getElementById('admin-theme-toggle');
    if (!btn) return;
    btn.addEventListener('click', function() {
      var root = document.documentElement;
      var isLight = root.classList.contains('light');
      if (isLight) {
        root.classList.remove('light');
        try { localStorage.setItem('cinebase-theme', 'dark'); } catch(e) {}
      } else {
        root.classList.add('light');
        try { localStorage.setItem('cinebase-theme', 'light'); } catch(e) {}
      }
      // Update icon
      var icon = btn.querySelector('i');
      if (icon) icon.className = root.classList.contains('light') ? 'fa-solid fa-sun text-sm' : 'fa-solid fa-moon text-sm';
      // Sync landing toggle icon if present
      document.querySelectorAll('#theme-toggle i').forEach(function(el) {
        el.className = root.classList.contains('light') ? 'fa-solid fa-sun' : 'fa-solid fa-moon';
      });
    });
    // Sync icon on page load
    var icon = btn.querySelector('i');
    if (icon) icon.className = document.documentElement.classList.contains('light') ? 'fa-solid fa-sun text-sm' : 'fa-solid fa-moon text-sm';
  }

  document.addEventListener('DOMContentLoaded', () => {
    const cp = window.location.pathname.toLowerCase();
    if (!ADMIN_PATHS.has(cp)) return;

    const main = document.querySelector('main');
    if (!main) return;

    // Remove landing navbar/footer
    document.getElementById('navbar-container')?.remove();
    document.getElementById('footer-container')?.remove();

    renderShell(main);
    bindActions();
    setActiveLinks();
    updateUserUI();
    initThemeToggle();

    // Hide admin-only links for PowerUser
    if (!isAdmin()) {
      document.querySelectorAll('[data-role-required="admin"]').forEach(el => el.style.display = 'none');
    }
  });
})();
