/**
 * TMDB Search Page JavaScript
 * Gestisce la ricerca e l'importazione di film da The Movie Database
 */

const TMDBSearch = {
    currentPage: 1,
    totalPages: 1,
    currentQuery: '',
    currentMode: 'search', // 'search', 'popular', 'upcoming', 'now_playing'

    init() {
        this.bindEvents();
        this.loadPopular(); // Carica film popolari all'avvio
    },

    bindEvents() {
        // Search
        document.getElementById('search-btn')?.addEventListener('click', () => this.handleSearch());
        document.getElementById('search-input')?.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') this.handleSearch();
        });

        // Filter buttons
        document.getElementById('btn-popular')?.addEventListener('click', () => this.loadPopular());
        document.getElementById('btn-upcoming')?.addEventListener('click', () => this.loadUpcoming());
        document.getElementById('btn-now-playing')?.addEventListener('click', () => this.loadNowPlaying());

        // Pagination
        document.getElementById('prev-page')?.addEventListener('click', () => this.prevPage());
        document.getElementById('next-page')?.addEventListener('click', () => this.nextPage());

        // Modal
        document.getElementById('close-modal')?.addEventListener('click', () => this.closeModal());
        document.getElementById('cancel-btn')?.addEventListener('click', () => this.closeModal());
        document.getElementById('import-btn')?.addEventListener('click', () => this.importMovie());
        document.getElementById('movie-modal')?.addEventListener('click', (e) => {
            if (e.target === document.getElementById('movie-modal')) this.closeModal();
        });

        // Success modal
        document.getElementById('continue-search')?.addEventListener('click', () => {
            document.getElementById('success-modal').classList.add('hidden');
        });
    },

    async handleSearch() {
        const query = document.getElementById('search-input').value.trim();
        if (!query) return;

        this.currentQuery = query;
        this.currentMode = 'search';
        this.currentPage = 1;
        await this.performSearch();
    },

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

            const response = await fetch(url, {
                headers: API.getAuthHeaders()
            });

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
        await this.performSearch();
    },

    async loadUpcoming() {
        this.currentMode = 'upcoming';
        this.currentPage = 1;
        document.getElementById('search-input').value = '';
        await this.performSearch();
    },

    async loadNowPlaying() {
        this.currentMode = 'now_playing';
        this.currentPage = 1;
        document.getElementById('search-input').value = '';
        await this.performSearch();
    },

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

        // Aggiungi event listeners ai pulsanti dettaglio
        document.querySelectorAll('.view-details-btn').forEach(btn => {
            btn.addEventListener('click', (e) => {
                const tmdbId = parseInt(e.currentTarget.dataset.id);
                this.loadMovieDetails(tmdbId);
            });
        });

        // Aggiungi event listeners ai pulsanti importa rapido
        document.querySelectorAll('.quick-import-btn').forEach(btn => {
            btn.addEventListener('click', (e) => {
                const tmdbId = parseInt(e.currentTarget.dataset.id);
                this.importMovie(tmdbId);
            });
        });

        // Aggiorna paginazione
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
                    <img src="${posterUrl}" alt="${movie.title}" class="movie-poster w-full">
                    <div class="absolute top-2 right-2">
                        <span class="badge-gold">${rating}</span>
                    </div>
                </div>
                <div class="p-4">
                    <h3 class="text-white font-normal text-lg mb-1 truncate-2">${movie.title}</h3>
                    <p class="text-ash text-sm mb-3">${year}</p>
                    <p class="text-steel text-xs truncate-2 mb-4">${movie.overview || 'Nessuna descrizione disponibile'}</p>
                    <div class="flex gap-2">
                        <button class="view-details-btn btn btn-ghost flex-1 text-xs py-2" data-id="${movie.id}">
                            <i class="fa-solid fa-eye mr-1"></i>Dettagli
                        </button>
                        <button class="quick-import-btn btn btn-gold text-xs py-2 px-3" data-id="${movie.id}" title="Importa rapidamente">
                            <i class="fa-solid fa-download"></i>
                        </button>
                    </div>
                </div>
            </div>
        `;
    },

    async loadMovieDetails(tmdbId) {
        try {
            const response = await fetch(`${API.baseUrl}/tmdb/movie/${tmdbId}`, {
                headers: API.getAuthHeaders()
            });

            if (!response.ok) throw new Error('Film non trovato');

            const movie = await response.json();
            this.currentMovie = movie; // Salva per l'importazione
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

        // Trailer
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

    closeModal() {
        document.getElementById('movie-modal').classList.add('hidden');
        document.body.style.overflow = '';
        this.currentMovie = null;
    },

    async importMovie(tmdbId = null) {
        const id = tmdbId || this.currentMovie?.id;
        if (!id) return;

        // Mostra loading
        document.getElementById('import-loading').classList.remove('hidden');

        try {
            const response = await fetch(`${API.baseUrl}/tmdb/import/${id}`, {
                method: 'POST',
                headers: API.getAuthHeaders()
            });

            if (response.status === 409) {
                // Film già esistente
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

    showSuccess(message, filmId) {
        this.closeModal();
        document.getElementById('success-message').textContent = message;
        document.getElementById('success-modal').classList.remove('hidden');
    },

    prevPage() {
        if (this.currentPage > 1) {
            this.currentPage--;
            this.performSearch();
        }
    },

    nextPage() {
        if (this.currentPage < this.totalPages) {
            this.currentPage++;
            this.performSearch();
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
    }
};

// Inizializza quando il DOM è pronto
document.addEventListener('DOMContentLoaded', () => {
    TMDBSearch.init();
});
