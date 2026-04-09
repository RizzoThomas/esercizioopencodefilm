// navbar.js - Gestione navbar dinamica con autenticazione JWT

// Gestione stato attivo navbar
function setActiveNavLink() {
    const currentPath = window.location.pathname;

    const desktopLinks = document.querySelectorAll('nav .nav-link');
    desktopLinks.forEach(link => {
        const href = link.getAttribute('href');
        const isActive = href === currentPath || (currentPath === '/' && href === '/index.html');

        if (isActive) {
            link.classList.add('text-indigo-600', 'border-b-2', 'border-indigo-600');
            link.classList.remove('text-slate-500', 'hover:text-indigo-600');
        } else {
            link.classList.remove('text-indigo-600', 'border-b-2', 'border-indigo-600');
            link.classList.add('text-slate-500', 'hover:text-indigo-600');
        }
    });

    const mobileLinks = document.querySelectorAll('#mobile-menu a');
    mobileLinks.forEach(link => {
        const href = link.getAttribute('href');
        const isActive = href === currentPath || (currentPath === '/' && href === '/index.html');

        if (isActive) {
            link.classList.add('text-indigo-600', 'bg-indigo-50');
            link.classList.remove('text-slate-700', 'hover:bg-slate-50');
        } else {
            link.classList.remove('text-indigo-600', 'bg-indigo-50');
            link.classList.add('text-slate-700', 'hover:bg-slate-50');
        }
    });
}

// Mobile menu toggle
function setupMobileMenu() {
    const menuToggle = document.getElementById('mobile-menu-toggle');
    const mobileMenu = document.getElementById('mobile-menu');

    if (menuToggle && mobileMenu) {
        menuToggle.addEventListener('click', () => {
            mobileMenu.classList.toggle('hidden');
        });
    }
}

// Gestione navbar dinamica basata sull'autenticazione
function updateNavbarForAuth() {
	if (typeof Auth === 'undefined') return;

	const isAuthenticated = Auth.isAuthenticated();
	const user = Auth.getUser();
	const userRole = Auth.getUserRole();

	const navGuest = document.getElementById('nav-guest');
	const navUser = document.getElementById('nav-user');

	const mobileNavGuest = document.getElementById('mobile-nav-guest');
	const mobileNavUser = document.getElementById('mobile-nav-user');
	const mobileNavAdmin = document.getElementById('mobile-nav-admin');

	const dropdownAdminSection = document.getElementById('dropdown-admin-section');
	const userAvatarContainer = document.getElementById('user-avatar-container');
	const userAvatarIcon = document.getElementById('user-avatar-icon');

	function showDesktop(el) {
		if (!el) return;
		el.classList.remove('hidden');
		el.style.display = '';
	}

	function hideElement(el) {
		if (!el) return;
		el.classList.add('hidden');
	}

	hideElement(navGuest);
	hideElement(navUser);
	hideElement(mobileNavGuest);
	hideElement(mobileNavUser);
	hideElement(mobileNavAdmin);

	if (!isAuthenticated) {
		showDesktop(navGuest);
		hideElement(mobileNavGuest);
		mobileNavGuest?.classList.remove('hidden');
	} else if (userRole === 'Admin' || userRole === 'PowerUser') {
		showDesktop(navUser);
		mobileNavAdmin?.classList.remove('hidden');

		const userName = document.getElementById('user-name');
		if (userName && user) {
			userName.textContent = `${user.nome} ${user.cognome}`;
		}

		if (dropdownAdminSection) {
			dropdownAdminSection.classList.remove('hidden');
		}

		if (userAvatarContainer && userAvatarIcon) {
			userAvatarContainer.className = 'w-8 h-8 rounded-full bg-purple-400/20 border border-purple-400/40 flex items-center justify-center text-purple-400';
			userAvatarIcon.className = 'fa-solid fa-shield-halved text-xs';
		}

		const adminRole = document.getElementById('admin-role');
		if (adminRole) {
			adminRole.textContent = `Ruolo: ${userRole}`;
		}

		if (userRole === 'PowerUser') {
			document.getElementById('dropdown-cinema-link')?.classList.add('hidden');
			document.getElementById('mobile-nav-admin-cinemas')?.classList.add('hidden');
		}
	} else {
		showDesktop(navUser);
		mobileNavUser?.classList.remove('hidden');

		const userName = document.getElementById('user-name');
		if (userName && user) {
			userName.textContent = `${user.nome} ${user.cognome}`;
		}

		if (dropdownAdminSection) {
			dropdownAdminSection.classList.add('hidden');
		}

		if (userAvatarContainer && userAvatarIcon) {
			userAvatarContainer.className = 'w-8 h-8 rounded-full bg-cyan-400/20 border border-cyan-400/40 flex items-center justify-center text-cyan-400';
			userAvatarIcon.className = 'fa-solid fa-user text-xs';
		}
	}
}

// Logout function
async function logout() {
    if (typeof Auth === 'undefined') return;
    
    try {
        await Auth.logout();
    } catch (error) {
        console.error('Logout error:', error);
    }
    
    window.location.href = '/index.html';
}

// Inizializzazione navbar
function initializeNavbar() {
    setActiveNavLink();
    setupMobileMenu();
    updateNavbarForAuth();
}

// Event listeners
document.addEventListener('components:loaded', initializeNavbar);

document.addEventListener('DOMContentLoaded', () => {
    if (document.querySelector('nav')) {
        initializeNavbar();
    }
});

// Aggiorna navbar quando cambia l'autenticazione
window.addEventListener('storage', (e) => {
    if (e.key === 'cinebase_user') {
        updateNavbarForAuth();
    }
});

// Esponi funzioni globalmente
window.setActiveNavLink = setActiveNavLink;
window.setupMobileMenu = setupMobileMenu;
window.updateNavbarForAuth = updateNavbarForAuth;
window.logout = logout;
window.initializeNavbar = initializeNavbar;
