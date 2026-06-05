/**
 * CineBase — Aceternity UI Effects (vanilla JS)
 * Ferrari Design System — cinematic, premium, restrained
 * Adapted from https://ui.aceternity.com/components
 */
(function () {
  'use strict';

  // Variabile FX: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const FX = {
    prefersReducedMotion: window.matchMedia('(prefers-reduced-motion: reduce)').matches,

    /* ====================================================================
       UTILITY: Intersection Observer factory
       ==================================================================== */
    _observers: [],
    observe(selector, callback, options = { threshold: 0.15 }) {
      // Variabile els: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const els = document.querySelectorAll(selector);
      if (!els.length) return;
      if (this.prefersReducedMotion) {
        els.forEach(el => callback(el, true));
        return;
      }
      // Variabile obs: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const obs = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
          if (entry.isIntersecting) callback(entry.target, true);
        });
      }, options);
      els.forEach(el => obs.observe(el));
      this._observers.push(obs);
    },

    /* ====================================================================
       1. HERO PARALLAX — Multi-layer cinematic parallax
       Usage: <section class="hero-parallax"><div data-parallax-speed="0.3">...</div></section>
       ==================================================================== */
    heroParallax(selector = '.hero-parallax') {
      // Variabile hero: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const hero = document.querySelector(selector);
      if (!hero || this.prefersReducedMotion) return;

      // Variabile layers: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const layers = hero.querySelectorAll('[data-parallax-speed]');
      if (!layers.length) return;

      // Variabile ticking: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      let ticking = false;
      // Variabile/funzione update: supporto non ovvio per stato, callback o logica della pagina.
      const update = () => {
        // Variabile scrollY: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const scrollY = window.scrollY;
        // Variabile heroTop: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const heroTop = hero.offsetTop;
        // Variabile heroHeight: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const heroHeight = hero.offsetHeight;
        // Variabile viewportH: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const viewportH = window.innerHeight;
        // Variabile progress: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const progress = Math.max(0, Math.min(1, (scrollY - heroTop + viewportH) / (heroHeight + viewportH)));

        layers.forEach(layer => {
          // Variabile speed: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
          const speed = parseFloat(layer.dataset.parallaxSpeed) || 0.5;
          const y = (scrollY - heroTop) * speed;
          layer.style.transform = `translate3d(0, ${y}px, 0)`;
          if (layer.dataset.parallaxOpacity !== undefined) {
            // Variabile opSpeed: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
            const opSpeed = parseFloat(layer.dataset.parallaxOpacity);
            layer.style.opacity = Math.max(0, 1 - progress * opSpeed);
          }
        });
        ticking = false;
      };

      // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
      window.addEventListener('scroll', () => {
        if (!ticking) { requestAnimationFrame(update); ticking = true; }
      }, { passive: true });
      update();
    },

    /* ====================================================================
       2. CARD SPOTLIGHT — Radial gradient follows mouse
       Usage: <div class="card-spotlight">...</div>
       Ferrari: Rosso Corsa tint on hover, sharp corners
       ==================================================================== */
    cardSpotlight(selector = '.card-spotlight') {
      document.querySelectorAll(selector).forEach(card => {
        // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
        card.addEventListener('mousemove', (e) => {
          // Variabile rect: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
          const rect = card.getBoundingClientRect();
          const x = ((e.clientX - rect.left) / rect.width) * 100;
          const y = ((e.clientY - rect.top) / rect.height) * 100;
          card.style.setProperty('--spot-x', `${x}%`);
          card.style.setProperty('--spot-y', `${y}%`);
          card.classList.add('card-spotlight-active');
        });
        // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
        card.addEventListener('mouseleave', () => {
          card.classList.remove('card-spotlight-active');
        });
      });
    },

    /* ====================================================================
       3. FOCUS CARDS — Blur non-hovered cards in a grid
       Usage: <div class="focus-cards-container"><div class="focus-card">...</div></div>
       ==================================================================== */
    focusCards(selector = '.focus-cards-container') {
      document.querySelectorAll(selector).forEach(container => {
        // Variabile cards: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const cards = container.querySelectorAll('.focus-card');
        if (!cards.length) return;

        // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
        container.addEventListener('mouseenter', () => {
          cards.forEach(c => c.classList.add('focus-card-blur'));
        });
        // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
        container.addEventListener('mouseleave', () => {
          cards.forEach(c => c.classList.remove('focus-card-blur', 'focus-card-active'));
        });

        cards.forEach(card => {
          // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
          card.addEventListener('mouseenter', () => {
            card.classList.remove('focus-card-blur');
            card.classList.add('focus-card-active');
          });
          // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
          card.addEventListener('mouseleave', () => {
            card.classList.add('focus-card-blur');
            card.classList.remove('focus-card-active');
          });
        });
      });
    },

    /* ====================================================================
       4. ANIMATED TABS — Sliding indicator
       Usage: <div class="animated-tabs"><button class="animated-tab active">...</button></div>
       ==================================================================== */
    animatedTabs(selector = '.animated-tabs') {
      document.querySelectorAll(selector).forEach(tabContainer => {
        // Variabile indicator: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const indicator = document.createElement('div');
        indicator.className = 'animated-tab-indicator';
        tabContainer.appendChild(indicator);

        // Variabile tabs: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const tabs = tabContainer.querySelectorAll('.animated-tab');
        // Variabile/funzione updateIndicator: supporto non ovvio per stato, callback o logica della pagina.
        const updateIndicator = (activeTab) => {
          // Variabile rect: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
          const rect = activeTab.getBoundingClientRect();
          // Variabile parentRect: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
          const parentRect = tabContainer.getBoundingClientRect();
          indicator.style.width = `${rect.width}px`;
          indicator.style.transform = `translateX(${rect.left - parentRect.left}px)`;
          indicator.style.opacity = '1';
        };

        tabs.forEach(tab => {
          // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
          tab.addEventListener('click', () => {
            tabs.forEach(t => t.classList.remove('active'));
            tab.classList.add('active');
            updateIndicator(tab);
            // Variabile target: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
            const target = tab.dataset.tabTarget;
            if (target) {
              document.querySelectorAll('.animated-tab-panel').forEach(p => p.classList.add('hidden'));
              document.getElementById(target)?.classList.remove('hidden');
            }
          });
        });

        // Variabile activeTab: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const activeTab = tabContainer.querySelector('.animated-tab.active');
        if (activeTab) {
          requestAnimationFrame(() => updateIndicator(activeTab));
        }

        // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
        window.addEventListener('resize', () => {
          // Variabile currentActive: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
          const currentActive = tabContainer.querySelector('.animated-tab.active');
          if (currentActive) updateIndicator(currentActive);
        });
      });
    },

    /* ====================================================================
       5. STICKY SCROLL REVEAL — Content reveals on scroll
       Usage: <div class="sticky-reveal-container"><div class="sticky-reveal-image">...</div><div class="sticky-reveal-text reveal-on-scroll">...</div></div>
       ==================================================================== */
    stickyScrollReveal(selector = '.sticky-reveal-container') {
      document.querySelectorAll(selector).forEach(container => {
        // Variabile texts: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const texts = container.querySelectorAll('.reveal-on-scroll');
        this.observeDirect(texts, (el, visible) => {
          if (visible) {
            el.classList.add('revealed');
            el.style.setProperty('--reveal-delay', el.dataset.revealDelay || '0ms');
          }
        }, { threshold: 0.2 });
      });
    },

    observeDirect(elements, callback, options = { threshold: 0.15 }) {
      if (!elements || (elements.length !== undefined && !elements.length)) return;
      // Variabile els: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const els = elements.length !== undefined ? elements : [elements];
      if (this.prefersReducedMotion) { els.forEach(el => callback(el, true)); return; }
      // Variabile obs: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const obs = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
          if (entry.isIntersecting) callback(entry.target, true);
        });
      }, options);
      els.forEach(el => obs.observe(el));
      this._observers.push(obs);
    },

    /* ====================================================================
       6. TEXT GENERATE EFFECT — Characters appear one by one
       Usage: <h1 class="text-generate" data-text="CineBase">CineBase</h1>
       ==================================================================== */
    textGenerate(selector = '.text-generate') {
      document.querySelectorAll(selector).forEach(el => {
        // Variabile fullText: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const fullText = el.dataset.text || el.textContent;
        if (!fullText) return;
        el.textContent = '';
        el.style.visibility = 'visible';

        if (this.prefersReducedMotion) {
          el.textContent = fullText;
          return;
        }

        let i = 0;
        // Variabile interval: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const interval = setInterval(() => {
          el.textContent += fullText[i];
          i++;
          if (i >= fullText.length) clearInterval(interval);
        }, 40 + Math.random() * 30);
      });
    },

    /* ====================================================================
       7. AURORA BACKGROUND — Canvas-based animated gradient
       Usage: <canvas class="aurora-bg" data-colors="#da291c,#181818,#303030"></canvas>
       ==================================================================== */
    auroraBackground(selector = '.aurora-bg') {
      document.querySelectorAll(selector).forEach(canvas => {
        if (this.prefersReducedMotion) return;
        // Variabile ctx: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const ctx = canvas.getContext('2d');
        // Variabile colors: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const colors = (canvas.dataset.colors || '#da291c,#181818,#4a0000').split(',');
        let w, h, time = 0;

        // Variabile/funzione resize: supporto non ovvio per stato, callback o logica della pagina.
        const resize = () => {
          // Variabile parent: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
          const parent = canvas.parentElement;
          w = canvas.width = parent.offsetWidth;
          h = canvas.height = parent.offsetHeight;
        };
        resize();
        // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
        window.addEventListener('resize', resize);

        // Variabile/funzione animate: supporto non ovvio per stato, callback o logica della pagina.
        const animate = () => {
          time += 0.005;
          ctx.clearRect(0, 0, w, h);

          // Three overlapping radial gradients that slowly move
          const gradients = [
            { x: w * 0.3 + Math.sin(time * 0.7) * w * 0.2, y: h * 0.5 + Math.cos(time * 0.5) * h * 0.3, r: w * 0.6, color: colors[0], alpha: 0.15 },
            { x: w * 0.7 + Math.cos(time * 0.6) * w * 0.2, y: h * 0.3 + Math.sin(time * 0.8) * h * 0.2, r: w * 0.5, color: colors[2] || colors[1], alpha: 0.12 },
            { x: w * 0.5 + Math.sin(time * 0.4) * w * 0.15, y: h * 0.7 + Math.cos(time * 0.7) * h * 0.15, r: w * 0.7, color: colors[1] || colors[0], alpha: 0.08 },
          ];

          ctx.globalCompositeOperation = 'screen';
          for (const g of gradients) {
            // Variabile grad: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
            const grad = ctx.createRadialGradient(g.x, g.y, 0, g.x, g.y, g.r);
            grad.addColorStop(0, hexToRgba(g.color, g.alpha));
            grad.addColorStop(1, hexToRgba(g.color, 0));
            ctx.fillStyle = grad;
            ctx.fillRect(0, 0, w, h);
          }
          ctx.globalCompositeOperation = 'source-over';

          requestAnimationFrame(animate);
        };
        animate();
      });

      // Funzione hexToRgba: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
      function hexToRgba(hex, alpha) {
        const r = parseInt(hex.slice(1, 3), 16);
        const g = parseInt(hex.slice(3, 5), 16);
        const b = parseInt(hex.slice(5, 7), 16);
        return `rgba(${r},${g},${b},${alpha})`;
      }
    },

    /* ====================================================================
       8. FLOATING NAVBAR — Hide on scroll down, reveal on scroll up
       Usage: <nav class="navbar-ferrari floating-navbar">...</nav>
       ==================================================================== */
    floatingNavbar(selector = '.floating-navbar') {
      // Variabile nav: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const nav = document.querySelector(selector);
      if (!nav || this.prefersReducedMotion) return;

      // Variabile lastScroll: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      let lastScroll = 0;
      // Variabile ticking: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      let ticking = false;
      // Variabile threshold: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const threshold = 10;

      // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
      window.addEventListener('scroll', () => {
        if (!ticking) {
          requestAnimationFrame(() => {
            // Variabile currentScroll: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
            const currentScroll = window.scrollY;
            if (Math.abs(currentScroll - lastScroll) < threshold) { ticking = false; return; }
            if (currentScroll > lastScroll && currentScroll > 100) {
              nav.style.transform = 'translateY(-100%)';
            } else {
              nav.style.transform = 'translateY(0)';
            }
            lastScroll = currentScroll;
            ticking = false;
          });
          ticking = true;
        }
      }, { passive: true });
    },

    /* ====================================================================
       9. HERO HIGHLIGHT — SVG text with animated spotlight
       Usage: <span class="hero-highlight-text" data-text="piattaforma completa">piattaforma completa</span>
       ==================================================================== */
    heroHighlight(selector = '.hero-highlight-text') {
      document.querySelectorAll(selector).forEach(el => {
        if (this.prefersReducedMotion) return;
        // Variabile text: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const text = el.dataset.text || el.textContent;
        // Create a subtle pulsing glow animation via CSS
        el.classList.add('hero-highlight-active');
        el.style.setProperty('--highlight-text', `"${text}"`);
      });
    },

    /* ====================================================================
       10. APPLE CARDS CAROUSEL — Cards scale based on scroll position
       Usage: <div class="apple-carousel"><div class="apple-carousel-track"><div class="apple-carousel-card">...</div></div></div>
       ==================================================================== */
    appleCarousel(selector = '.apple-carousel') {
      document.querySelectorAll(selector).forEach(carousel => {
        // Variabile track: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const track = carousel.querySelector('.apple-carousel-track');
        // Variabile cards: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const cards = track?.querySelectorAll('.apple-carousel-card');
        if (!cards?.length || this.prefersReducedMotion) return;

        // Variabile/funzione updateCards: supporto non ovvio per stato, callback o logica della pagina.
        const updateCards = () => {
          // Variabile trackRect: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
          const trackRect = track.getBoundingClientRect();
          // Variabile centerX: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
          const centerX = trackRect.left + trackRect.width / 2;

          cards.forEach(card => {
            // Variabile cardRect: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
            const cardRect = card.getBoundingClientRect();
            // Variabile cardCenterX: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
            const cardCenterX = cardRect.left + cardRect.width / 2;
            // Variabile dist: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
            const dist = Math.abs(centerX - cardCenterX);
            // Variabile maxDist: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
            const maxDist = trackRect.width / 2;
            // Variabile ratio: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
            const ratio = Math.max(0, 1 - dist / maxDist);

            // Variabile scale: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
            const scale = 0.82 + ratio * 0.18;
            // Variabile opacity: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
            const opacity = 0.4 + ratio * 0.6;
            card.style.transform = `scale(${scale})`;
            card.style.opacity = opacity;
            card.style.zIndex = ratio > 0.7 ? 2 : 1;
          });
        };

        // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
        track.addEventListener('scroll', () => requestAnimationFrame(updateCards), { passive: true });
        updateCards();
      });
    },

    /* ====================================================================
       11. LAMP EFFECT — Section header glows on scroll
       Usage: <h2 class="lamp-header"><span class="lamp-text">Section Title</span></h2>
       ==================================================================== */
    lampEffect(selector = '.lamp-header') {
      document.querySelectorAll(selector).forEach(header => {
        // Variabile text: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const text = header.querySelector('.lamp-text') || header;
        this.observeDirect(text, (el, visible) => {
          if (visible) {
            el.classList.add('lamp-lit');
          }
        }, { threshold: 0.3 });
      });
    },

    /* ====================================================================
       12. EXPANDABLE CARDS — Click to expand
       Usage: <div class="expandable-card" data-expandable><div class="expandable-preview">...</div><div class="expandable-detail">...</div></div>
       ==================================================================== */
    expandableCards(selector = '.expandable-card') {
      document.querySelectorAll(selector).forEach(card => {
        // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
        card.addEventListener('click', function () {
          // Variabile wasExpanded: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
          const wasExpanded = this.classList.contains('expanded');
          // Close all others in same container
          const container = this.closest('.expandable-cards-container');
          if (container) {
            container.querySelectorAll('.expandable-card.expanded').forEach(c => c.classList.remove('expanded'));
          }
          if (!wasExpanded) {
            this.classList.add('expanded');
          }
        });
      });
    },

    /* ====================================================================
       13. GLARE CARD — Subtle glare on hover (Linear-inspired)
       Usage: <div class="glare-card">...</div>
       ==================================================================== */
    glareCards(selector = '.glare-card') {
      if (this.prefersReducedMotion) return;
      document.querySelectorAll(selector).forEach(card => {
        // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
        card.addEventListener('mousemove', (e) => {
          // Variabile rect: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
          const rect = card.getBoundingClientRect();
          const x = ((e.clientX - rect.left) / rect.width) * 100;
          const y = ((e.clientY - rect.top) / rect.height) * 100;
          card.style.setProperty('--glare-x', `${x}%`);
          card.style.setProperty('--glare-y', `${y}%`);
        });
      });
    },

    /* ====================================================================
       14. CINEMATIC SCROLL REVEAL — Blur + fade + slide on scroll
       Usage: <div class="reveal-cinematic">...</div>
       ==================================================================== */
    cinematicScrollReveal(selector = '.reveal-cinematic') {
      // Variabile els: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const els = document.querySelectorAll(selector);
      if (!els.length) return;
      if (this.prefersReducedMotion) {
        els.forEach(el => el.classList.add('revealed'));
        return;
      }
      // Variabile obs: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const obs = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
          if (entry.isIntersecting) {
            entry.target.classList.add('revealed');
          } else {
            entry.target.classList.remove('revealed');
          }
        });
      }, { threshold: 0.1, rootMargin: '0px 0px -40px 0px' });
      els.forEach(el => obs.observe(el));
      this._observers.push(obs);
    },

    /* ====================================================================
       15. RIPPLE CLICK EFFECT — Material wave from click point
       Usage: <button class="ripple-container">...</button>
       ==================================================================== */
    rippleClick(selector = '.ripple-container') {
      document.querySelectorAll(selector).forEach(el => {
        // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
        el.addEventListener('click', (e) => {
          // Variabile ripple: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
          const ripple = document.createElement('span');
          // Variabile rect: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
          const rect = el.getBoundingClientRect();
          // Variabile size: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
          const size = Math.max(rect.width, rect.height) * 2;
          ripple.className = 'ripple-effect';
          ripple.style.left = `${e.clientX - rect.left - size / 2}px`;
          ripple.style.top  = `${e.clientY - rect.top - size / 2}px`;
          ripple.style.width = ripple.style.height = `${size}px`;
          el.appendChild(ripple);
          // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
          ripple.addEventListener('animationend', () => ripple.remove());
        });
      });
    },

    /* ====================================================================
       16. MAGNETIC HOVER — Button subtly follows cursor
       Usage: <button class="magnetic-btn">...</button>
       ==================================================================== */
    magneticHover(selector = '.magnetic-btn') {
      if (this.prefersReducedMotion) return;
      document.querySelectorAll(selector).forEach(btn => {
        // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
        btn.addEventListener('mousemove', (e) => {
          // Variabile rect: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
          const rect = btn.getBoundingClientRect();
          const x = (e.clientX - rect.left - rect.width / 2) * 0.15;
          const y = (e.clientY - rect.top - rect.height / 2) * 0.15;
          btn.style.transform = `translate(${x}px, ${y}px)`;
        });
        // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
        btn.addEventListener('mouseleave', () => {
          btn.style.transform = 'translate(0, 0)';
        });
      });
    },

    /* ====================================================================
       17. 3D TILT CARD — Card tilts toward cursor (21st.dev parallax style)
       Usage: <div class="tilt-card"><div class="tilt-card-inner">...</div><div class="tilt-card-shine"></div></div>
       ==================================================================== */
    tiltCards(selector = '.tilt-card') {
      if (this.prefersReducedMotion) return;
      document.querySelectorAll(selector).forEach(card => {
        // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
        card.addEventListener('mousemove', (e) => {
          // Variabile rect: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
          const rect = card.getBoundingClientRect();
          const x = e.clientX - rect.left;
          const y = e.clientY - rect.top;
          // Variabile centerX: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
          const centerX = rect.width / 2;
          // Variabile centerY: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
          const centerY = rect.height / 2;
          // Variabile rotateX: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
          const rotateX = ((y - centerY) / centerY) * -8;
          // Variabile rotateY: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
          const rotateY = ((x - centerX) / centerX) * 8;
          card.style.transform = `perspective(1000px) rotateX(${rotateX}deg) rotateY(${rotateY}deg)`;
          card.style.setProperty('--tilt-x', `${(x / rect.width) * 100}%`);
          card.style.setProperty('--tilt-y', `${(y / rect.height) * 100}%`);
        });
        // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
        card.addEventListener('mouseleave', () => {
          card.style.transform = 'perspective(1000px) rotateX(0) rotateY(0)';
          card.style.setProperty('--tilt-x', '50%');
          card.style.setProperty('--tilt-y', '50%');
        });
      });
    },

    /* ====================================================================
       18. DUST PARTICLES — Floating projector motes in hero
       ==================================================================== */
    dustParticles(selector = '#dust-layer') {
      if (this.prefersReducedMotion) return;
      document.querySelectorAll(selector).forEach(container => {
        // Variabile count: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const count = 50;
        for (let i = 0; i < count; i++) {
          // Variabile particle: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
          const particle = document.createElement('div');
          particle.className = 'dust-particle';
          // Variabile size: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
          const size = 3 + Math.random() * 8;
          particle.style.width = `${size}px`;
          particle.style.height = `${size}px`;
          particle.style.left = `${Math.random() * 100}%`;
          particle.style.top = `${30 + Math.random() * 70}%`;
          particle.style.animationDuration = `${5 + Math.random() * 15}s`;
          particle.style.animationDelay = `${Math.random() * 10}s`;
          container.appendChild(particle);
        }
      });
    },

    /* ====================================================================
       INIT ALL EFFECTS
       ==================================================================== */
    init() {
      console.log('%c🎬 CineBase FX %cinitializing...',
        'color:#da291c;font-weight:bold;', 'color:#969696;');

      // Always-active effects (no page-specific dependency)
      this.floatingNavbar('.floating-navbar');
      this.cardSpotlight('.card-spotlight');
      this.cardSpotlight('.card-spotlight-enhanced');
      this.focusCards('.focus-cards-container');
      this.glareCards('.glare-card');
      this.expandableCards('.expandable-card');
      this.rippleClick('.ripple-container');
      this.magneticHover('.magnetic-btn');
      this.tiltCards('.tilt-card');
      this.dustParticles('#dust-layer');

      // Scroll-triggered effects
      this.stickyScrollReveal('.sticky-reveal-container');
      this.cinematicScrollReveal('.reveal-cinematic');

      // DOMContentLoaded effects
      if (document.readyState === 'loading') {
        // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
        document.addEventListener('DOMContentLoaded', () => this._initDelayed());
      } else {
        this._initDelayed();
      }
    },

    _initDelayed() {
      // Variabile effects: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const effects = {
        heroParallax: '.hero-parallax',
        animatedTabs: '.animated-tabs',
        textGenerate: '.text-generate',
        auroraBackground: '.aurora-bg',
        heroHighlight: '.hero-highlight-text',
        appleCarousel: '.apple-carousel',
        lampEffect: '.lamp-header',
      };
      // Variabile active: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      let active = 0;
      for (const [name, sel] of Object.entries(effects)) {
        if (document.querySelector(sel)) {
          this[name](sel);
          active++;
        }
      }
      console.log(`%c🎬 CineBase FX %c${active} effects active on this page`,
        'color:#da291c;font-weight:bold;', 'color:#969696;');
    },

    destroy() {
      this._observers.forEach(o => o.disconnect());
      this._observers = [];
    },
  };

  // Export
  window.CineBaseFX = FX;

  // Auto-init
  FX.init();
})();
