/**
 * Ricerca Film — TMDB (admin/poweruser) o Catalogo Locale (utenti normali)
 */
const TMDBSearch = {
    currentPage: 1,
    totalPages: 1,
    currentQuery: '',
    currentMode: 'search',
    isAdmin: false,
    _searchTimer: null,
    _activeFilterBtn: null,

    init() {
        const rawRole = (window.Auth?.getUserRole?.() || '');
        // Normalizza: stringhe 'admin'/'poweruser' o numeri 2/1
        const role = String(rawRole).trim().toLowerCase();
        this.isAdmin = role === 'admin' || role === 'poweruser' || role === '2' || role === '1';
        this.bindEvents();
        if (this.isAdmin) {
            this.loadPopular(); // TMDB popolari per admin
        } else {
            this.loadLocalPopular(); // catalogo locale per utenti
        }
    },

    bindEvents() {
        document.getElementById('search-btn')?.addEventListener('click', () => this.handleSearch());
        document.getElementById('search-input')?.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') this.handleSearch();
        });
        // Live search con debounce 300ms
        document.getElementById('search-input')?.addEventListener('input', (e) => {
            clearTimeout(this._searchTimer);
            this._searchTimer = setTimeout(() => {
                const query = e.target.value.trim();
                if (query.length >= 2 || query.length === 0) {
                    this.handleLiveSearch(query);
                }
            }, 300);
        });

        document.getElementById('btn-popular')?.addEventListener('click', () => {
            if (this.isAdmin) this.loadPopular(); else this.loadLocalPopular();
        });
        document.getElementById('btn-upcoming')?.addEventListener('click', () => {
            if (this.isAdmin) this.loadUpcoming(); else this.loadLocalUpcoming();
        });
        document.getElementById('btn-now-playing')?.addEventListener('click', () => {
            if (this.isAdmin) this.loadNowPlaying(); else this.loadLocalNowPlaying();
        });

        document.getElementById('prev-page')?.addEventListener('click', () => this.prevPage());
        document.getElementById('next-page')?.addEventListener('click', () => this.nextPage());

        document.getElementById('close-modal')?.addEventListener('click', () => this.closeModal());
        document.getElementById('cancel-btn')?.addEventListener('click', () => this.closeModal());
        document.getElementById('import-btn')?.addEventListener('click', () => this.importMovie());
        document.getElementById('movie-modal')?.addEventListener('click', (e) => {
            if (e.target === document.getElementById('movie-modal')) this.closeModal();
        });

        document.getElementById('continue-search')?.addEventListener('click', () => {
            document.getElementById('success-modal').classList.add('hidden');
        });
    },

    // ─── Ricerca (determina TMDB vs locale) ──────────────────────────

    async handleSearch() {
        const query = document.getElementById('search-input').value.trim();
        if (!query) return;
        this.currentQuery = query;
        this.currentMode = 'search';
        this.currentPage = 1;
        this.highlightFilterButton(null);
        if (this.isAdmin) {
            await this.performSearch();
        } else {
            await this.performLocalSearch();
        }
    },

    // Live search: chiamato ad ogni digitazione (con debounce)
    async handleLiveSearch(query) {
        this.currentQuery = query;
        this.currentPage = 1;
        if (!query) {
            // Torna alla modalità corrente (popolari / in uscita / al cinema)
            if (this.currentMode === 'popular') {
                if (this.isAdmin) await this.loadPopular();
                else await this.loadLocalPopular();
            } else if (this.currentMode === 'upcoming') {
                if (this.isAdmin) await this.loadUpcoming();
                else await this.loadLocalUpcoming();
            } else if (this.currentMode === 'now_playing') {
                if (this.isAdmin) await this.loadNowPlaying();
                else await this.loadLocalNowPlaying();
            }
            return;
        }
        this.currentMode = 'search';
        this.highlightFilterButton(null);
        if (this.isAdmin) {
            await this.performSearch();
        } else {
            await this.performLocalSearch();
        }
    },

    // Evidenzia il pulsante filtro attivo
    highlightFilterButton(mode) {
        this._activeFilterBtn = mode;
        const btns = ['btn-popular', 'btn-upcoming', 'btn-now-playing'];
        btns.forEach(id => {
            const btn = document.getElementById(id);
            if (!btn) return;
            if (mode && id === `btn-${mode === 'now_playing' ? 'now-playing' : mode}`) {
                btn.classList.add('text-gold');
            } else {
                btn.classList.remove('text-gold');
            }
        });
    },

    // ═══════════════════ TMDB (Admin/PowerUser) ═══════════════════════

    async performSearch() {
        this.showLoading();
        this.hideError();
        try {
            let url;
            switch (this.currentMode) {
                case 'popular':
                    url = `${API.baseUrl}/tmdb/popular?page=${this.currentPage}`;
                    break;
                case 'upcoming':
                    url = `${API.baseUrl}/tmdb/upcoming?page=${this.currentPage}`;
                    break;
                case 'now_playing':
                    url = `${API.baseUrl}/tmdb/now-playing?page=${this.currentPage}`;
                    break;
                default:
                    url = `${API.baseUrl}/tmdb/search?query=${encodeURIComponent(this.currentQuery)}&page=${this.currentPage}`;
            }
            const response = await fetch(url, { headers: API.getAuthHeaders() });
            if (!response.ok) throw new Error('Errore nella ricerca');
            const data = await response.json();
            this.renderResults(data);
        } catch (error) {
            console.error('Search error:', error);
            this.showError('Impossibile effettuare la ricerca. Riprova più tardi.');
        } finally {
            this.hideLoading();
        }
    },

    async loadPopular() {
        this.currentMode = 'popular';
        this.currentPage = 1;
        document.getElementById('search-input').value = '';
        this.highlightFilterButton('popular');
        await this.performSearch();
    },

    async loadUpcoming() {
        this.currentMode = 'upcoming';
        this.currentPage = 1;
        document.getElementById('search-input').value = '';
        this.highlightFilterButton('upcoming');
        await this.performSearch();
    },

    async loadNowPlaying() {
        this.currentMode = 'now_playing';
        this.currentPage = 1;
        document.getElementById('search-input').value = '';
        this.highlightFilterButton('now_playing');
        await this.performSearch();
    },

    // ═══════════════════ LOCALE (Utenti normali) ══════════════════════

    async performLocalSearch() {
        this.showLoading();
        this.hideError();
        try {
            let filter = null;
            if (this.currentMode === 'upcoming') filter = 'upcoming';
            else if (this.currentMode === 'now_playing') filter = 'now-playing';

            const params = { page: this.currentPage, pageSize: 12 };
            if (this.currentQuery) params.search = this.currentQuery;
            if (filter) params.filter = filter;

            let data;
            if (this.currentQuery || filter) {
                data = await API.getFilms(params);
            } else {
                data = await API.getFilms({ page: this.currentPage, pageSize: 12 });
            }

            const normalized = {
                results: (data.items || data).map(f => this.normalizeLocalFilm(f)),
                totalPages: data.totalPages || 1,
                page: data.page || 1
            };
            this.renderLocalResults(normalized);
        } catch (error) {
            console.error('Local search error:', error);
            this.showError('Impossibile caricare i film. Riprova più tardi.');
        } finally {
            this.hideLoading();
        }
    },

    async loadLocalPopular() {
        this.currentMode = 'popular';
        this.currentPage = 1;
        this.currentQuery = '';
        document.getElementById('search-input').value = '';
        this.highlightFilterButton('popular');
        await this.performLocalSearch();
    },

    async loadLocalUpcoming() {
        this.currentMode = 'upcoming';
        this.currentPage = 1;
        this.currentQuery = '';
        document.getElementById('search-input').value = '';
        this.highlightFilterButton('upcoming');
        await this.performLocalSearch();
    },

    async loadLocalNowPlaying() {
        this.currentMode = 'now_playing';
        this.currentPage = 1;
        this.currentQuery = '';
        document.getElementById('search-input').value = '';
        this.highlightFilterButton('now_playing');
        await this.performLocalSearch();
    },

    normalizeLocalFilm(film) {
        return {
            id: film.id,
            title: film.titolo || 'Senza titolo',
            overview: film.descrizioneLunga || '',
            posterPath: film.copertinaPath || null,
            releaseDate: film.dataRilascio || null,
            voteAverage: film.voteAverage || null,
            runtime: film.durata || null,
            registaNome: film.registaNome || '',
            registaCognome: film.registaCognome || '',
            cast: film.castText || '',
            categorie: film.categorie || [],
            // Flag per distinguere locale da TMDB
            _local: true
        };
    },

    // ═══════════════════ Rendering ════════════════════════════════════

    renderResults(data) {
        const grid = document.getElementById('results-grid');
        const noResults = document.getElementById('no-results');
        const pagination = document.getElementById('pagination');
        this.totalPages = data.totalPages || 1;

        if (!data.results || data.results.length === 0) {
            grid.innerHTML = '';
            noResults.classList.remove('hidden');
            pagination.classList.add('hidden');
            return;
        }
        noResults.classList.add('hidden');
        grid.innerHTML = data.results.map(movie => this.createMovieCard(movie)).join('');

        document.querySelectorAll('.view-details-btn').forEach(btn => {
            btn.addEventListener('click', (e) => {
                const id = parseInt(e.currentTarget.dataset.id);
                this.loadMovieDetails(id);
            });
        });
        document.querySelectorAll('.quick-import-btn').forEach(btn => {
            btn.addEventListener('click', (e) => {
                const tmdbId = parseInt(e.currentTarget.dataset.id);
                this.importMovie(tmdbId);
            });
        });
        this.updatePagination(pagination);
    },

    renderLocalResults(data) {
        const grid = document.getElementById('results-grid');
        const noResults = document.getElementById('no-results');
        const pagination = document.getElementById('pagination');
        this.totalPages = data.totalPages || 1;

        if (!data.results || data.results.length === 0) {
            grid.innerHTML = '';
            noResults.classList.remove('hidden');
            pagination.classList.add('hidden');
            return;
        }
        noResults.classList.add('hidden');
        grid.innerHTML = data.results.map(movie => this.createLocalMovieCard(movie)).join('');

        document.querySelectorAll('.view-details-btn').forEach(btn => {
            btn.addEventListener('click', (e) => {
                const id = parseInt(e.currentTarget.dataset.id);
                this.showLocalFilmModal(id);
            });
        });
        this.updatePagination(pagination);
    },

    updatePagination(pagination) {
        if (this.totalPages > 1) {
            pagination.classList.remove('hidden');
            document.getElementById('page-info').textContent = `Pagina ${this.currentPage} di ${this.totalPages}`;
            document.getElementById('prev-page').disabled = this.currentPage === 1;
            document.getElementById('next-page').disabled = this.currentPage >= this.totalPages;
        } else {
            pagination.classList.add('hidden');
        }
    },

    createMovieCard(movie) {
        const posterUrl = movie.posterPath
            ? `https://image.tmdb.org/t/p/w500${movie.posterPath}`
            : '/images/no-poster.jpg';
        const year = movie.releaseDate ? new Date(movie.releaseDate).getFullYear() : 'N/A';
        const rating = movie.voteAverage ? movie.voteAverage.toFixed(1) : 'N/A';

        return `
            <div class="bg-charcoal border border-white/10 overflow-hidden hover:border-gold/50 transition-colors group">
                <div class="relative">
                    <img src="${posterUrl}" alt="${this.escHtml(movie.title)}" class="movie-poster w-full">
                    <div class="absolute top-2 right-2">
                        <span class="badge-gold">${rating}</span>
                    </div>
                </div>
                <div class="p-4">
                    <h3 class="text-white font-normal text-lg mb-1 truncate-2">${this.escHtml(movie.title)}</h3>
                    <p class="text-ash text-sm mb-3">${year}</p>
                    <p class="text-steel text-xs truncate-2 mb-4">${this.escHtml(movie.overview || 'Nessuna descrizione disponibile')}</p>
                    <div class="flex gap-2">
                        <button class="view-details-btn btn btn-ghost flex-1 text-xs py-2" data-id="${movie.id}">
                            <i class="fa-solid fa-eye mr-1"></i>Dettagli
                        </button>
                        <button class="quick-import-btn btn btn-gold text-xs py-2 px-3" data-id="${movie.id}" title="Importa rapidamente">
                            <i class="fa-solid fa-download"></i>
                        </button>
                    </div>
                </div>
            </div>`;
    },

    createLocalMovieCard(movie) {
        const posterUrl = movie.posterPath && movie.posterPath.startsWith('http')
            ? movie.posterPath
            : movie.posterPath
                ? `/media/covers/${movie.posterPath}`
                : '/images/no-poster.jpg';
        const year = movie.releaseDate
            ? new Date(movie.releaseDate + 'T00:00:00').getFullYear()
            : 'N/A';
        const rating = movie.voteAverage ? movie.voteAverage.toFixed(1) : null;
        const director = movie.registaNome
            ? `${movie.registaNome} ${movie.registaCognome || ''}`.trim()
            : '';

        return `
            <div class="bg-charcoal border border-white/10 overflow-hidden hover:border-gold/50 transition-colors group">
                <div class="relative">
                    <img src="${posterUrl}" alt="${this.escHtml(movie.title)}" class="movie-poster w-full">
                    ${rating ? `<div class="absolute top-2 right-2"><span class="badge-gold">${rating}</span></div>` : ''}
                </div>
                <div class="p-4">
                    <h3 class="text-white font-normal text-lg mb-1 truncate-2">${this.escHtml(movie.title)}</h3>
                    <p class="text-ash text-sm mb-3">${year}${director ? ' • ' + director : ''}</p>
                    <p class="text-steel text-xs truncate-2 mb-4">${this.escHtml(movie.overview || 'Nessuna descrizione disponibile')}</p>
                    <div class="flex gap-2">
                        <button class="view-details-btn btn btn-ghost flex-1 text-xs py-2" data-id="${movie.id}">
                            <i class="fa-solid fa-eye mr-1"></i>Dettagli
                        </button>
                        <a href="/programmazione.html?filmId=${movie.id}" class="btn btn-ghost text-xs py-2 px-3 inline-flex items-center" title="Guarda proiezioni">
                            <i class="fa-solid fa-calendar-days"></i>
                        </a>
                    </div>
                </div>
            </div>`;
    },

    // ═══════════════════ Modal Dettaglio (TMDB) ════════════════════════

    async loadMovieDetails(tmdbId) {
        try {
            const response = await fetch(`${API.baseUrl}/tmdb/movie/${tmdbId}`, {
                headers: API.getAuthHeaders()
            });
            if (!response.ok) throw new Error('Film non trovato');
            const movie = await response.json();
            this.currentMovie = movie;
            // Mostra bottone importa solo per admin
            document.getElementById('import-btn').classList.toggle('hidden', !this.isAdmin);
            this.showModal(movie);
        } catch (error) {
            console.error('Error loading movie details:', error);
            this.showError('Impossibile caricare i dettagli del film');
        }
    },

    showModal(movie) {
        const posterUrl = movie.posterPath
            ? `https://image.tmdb.org/t/p/w500${movie.posterPath}`
            : '/images/no-poster.jpg';
        const year = movie.releaseDate ? new Date(movie.releaseDate).getFullYear() : 'N/A';
        const runtime = movie.runtime ? `${movie.runtime} min` : 'N/A';
        const rating = movie.voteAverage ? `${movie.voteAverage.toFixed(1)}/10` : 'N/A';
        const genres = movie.genres?.map(g => g.name).join(', ') || 'N/A';
        const director = movie.credits?.crew?.find(c => c.job === 'Director')?.name || 'N/A';
        const cast = movie.credits?.cast?.slice(0, 5).map(c => c.name).join(', ') || 'N/A';

        const trailer = movie.videos?.results?.find(v => v.type === 'Trailer' && v.site === 'YouTube');
        const trailerContainer = document.getElementById('modal-trailer');
        if (trailer) {
            trailerContainer.classList.remove('hidden');
            trailerContainer.querySelector('a').href = `https://www.youtube.com/watch?v=${trailer.key}`;
        } else {
            trailerContainer.classList.add('hidden');
        }

        document.getElementById('modal-poster').src = posterUrl;
        document.getElementById('modal-poster').alt = movie.title;
        document.getElementById('modal-movie-title').textContent = movie.title;
        document.getElementById('modal-year').textContent = year;
        document.getElementById('modal-runtime').textContent = runtime;
        document.getElementById('modal-rating').textContent = `⭐ ${rating}`;
        document.getElementById('modal-overview').textContent = movie.overview || 'Nessuna descrizione disponibile';
        document.getElementById('modal-genres').textContent = genres;
        document.getElementById('modal-director').textContent = director;
        document.getElementById('modal-cast').textContent = cast;

        document.getElementById('movie-modal').classList.remove('hidden');
        document.body.style.overflow = 'hidden';
    },

    // ═══════════════════ Modal Dettaglio (Locale) ══════════════════════

    async showLocalFilmModal(filmId) {
        try {
            const film = await API.getFilm(filmId);
            if (!film) throw new Error('Film non trovato');
            this.currentMovie = null; // non è TMDB
            document.getElementById('import-btn').classList.add('hidden'); // niente import per local

            const posterUrl = film.copertinaPath && film.copertinaPath.startsWith('http')
                ? film.copertinaPath
                : film.copertinaPath
                    ? `/media/covers/${film.copertinaPath}`
                    : '/images/no-poster.jpg';
            const year = film.dataRilascio
                ? new Date(film.dataRilascio + 'T00:00:00').getFullYear()
                : 'N/A';
            const runtime = film.durata ? `${film.durata} min` : 'N/A';
            const rating = film.voteAverage ? `${film.voteAverage.toFixed(1)}/10` : 'N/A';
            const genres = film.categorie?.map(c => c.nome).join(', ') || 'N/A';
            const director = film.registaNome
                ? `${film.registaNome} ${film.registaCognome || ''}`.trim()
                : 'N/A';

            document.getElementById('modal-trailer').classList.add('hidden');
            document.getElementById('modal-poster').src = posterUrl;
            document.getElementById('modal-poster').alt = film.titolo || '';
            document.getElementById('modal-movie-title').textContent = film.titolo || '';
            document.getElementById('modal-year').textContent = year;
            document.getElementById('modal-runtime').textContent = runtime;
            document.getElementById('modal-rating').textContent = rating !== 'N/A' ? `⭐ ${rating}` : '';
            document.getElementById('modal-overview').textContent = film.descrizioneLunga || 'Nessuna descrizione disponibile';
            document.getElementById('modal-genres').textContent = genres;
            document.getElementById('modal-director').textContent = director;
            document.getElementById('modal-cast').textContent = film.castText || 'N/A';

            document.getElementById('movie-modal').classList.remove('hidden');
            document.body.style.overflow = 'hidden';
        } catch (error) {
            console.error('Error loading local film:', error);
            this.showError('Impossibile caricare i dettagli del film');
        }
    },

    // ═══════════════════ Import (solo admin) ═══════════════════════════

    async importMovie(tmdbId = null) {
        if (!this.isAdmin) return;
        const id = tmdbId || this.currentMovie?.id;
        if (!id) return;
        document.getElementById('import-loading').classList.remove('hidden');
        try {
            const response = await fetch(`${API.baseUrl}/tmdb/import/${id}`, {
                method: 'POST',
                headers: API.getAuthHeaders()
            });
            if (response.status === 409) {
                const data = await response.json();
                this.showError(data.message || 'Film già importato');
                return;
            }
            if (!response.ok) throw new Error('Errore durante l\'importazione');
            const data = await response.json();
            this.showSuccess(data.message, data.filmId);
        } catch (error) {
            console.error('Import error:', error);
            this.showError('Impossibile importare il film. Riprova più tardi.');
        } finally {
            document.getElementById('import-loading').classList.add('hidden');
        }
    },

    // ═══════════════════ Utility ═══════════════════════════════════════

    closeModal() {
        document.getElementById('movie-modal').classList.add('hidden');
        document.body.style.overflow = '';
        this.currentMovie = null;
    },

    showSuccess(message, filmId) {
        this.closeModal();
        document.getElementById('success-message').textContent = message;
        document.getElementById('success-modal').classList.remove('hidden');
    },

    prevPage() {
        if (this.currentPage > 1) {
            this.currentPage--;
            if (this.isAdmin) this.performSearch();
            else this.performLocalSearch();
        }
    },

    nextPage() {
        if (this.currentPage < this.totalPages) {
            this.currentPage++;
            if (this.isAdmin) this.performSearch();
            else this.performLocalSearch();
        }
    },

    showLoading() {
        document.getElementById('loading-state').classList.remove('hidden');
        document.getElementById('results-grid').innerHTML = '';
        document.getElementById('no-results').classList.add('hidden');
    },

    hideLoading() {
        document.getElementById('loading-state').classList.add('hidden');
    },

    showError(message) {
        const errorEl = document.getElementById('error-state');
        document.getElementById('error-message').textContent = message;
        errorEl.classList.remove('hidden');
        setTimeout(() => errorEl.classList.add('hidden'), 5000);
    },

    hideError() {
        document.getElementById('error-state').classList.add('hidden');
    },

    escHtml(str) {
        if (!str) return '';
        const div = document.createElement('div');
        div.textContent = str;
        return div.innerHTML;
    }
};

document.addEventListener('DOMContentLoaded', () => {
    TMDBSearch.init();
});
