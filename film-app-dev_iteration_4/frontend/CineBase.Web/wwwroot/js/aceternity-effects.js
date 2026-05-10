/**
 * CineBase — Aceternity UI Effects (vanilla JS)
 * Ferrari Design System — cinematic, premium, restrained
 * Adapted from https://ui.aceternity.com/components
 */
(function () {
  'use strict';

  const FX = {
    prefersReducedMotion: window.matchMedia('(prefers-reduced-motion: reduce)').matches,

    /* ====================================================================
       UTILITY: Intersection Observer factory
       ==================================================================== */
    _observers: [],
    observe(selector, callback, options = { threshold: 0.15 }) {
      const els = document.querySelectorAll(selector);
      if (!els.length) return;
      if (this.prefersReducedMotion) {
        els.forEach(el => callback(el, true));
        return;
      }
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
      const hero = document.querySelector(selector);
      if (!hero || this.prefersReducedMotion) return;

      const layers = hero.querySelectorAll('[data-parallax-speed]');
      if (!layers.length) return;

      let ticking = false;
      const update = () => {
        const scrollY = window.scrollY;
        const heroTop = hero.offsetTop;
        const heroHeight = hero.offsetHeight;
        const viewportH = window.innerHeight;
        const progress = Math.max(0, Math.min(1, (scrollY - heroTop + viewportH) / (heroHeight + viewportH)));

        layers.forEach(layer => {
          const speed = parseFloat(layer.dataset.parallaxSpeed) || 0.5;
          const y = (scrollY - heroTop) * speed;
          layer.style.transform = `translate3d(0, ${y}px, 0)`;
          if (layer.dataset.parallaxOpacity !== undefined) {
            const opSpeed = parseFloat(layer.dataset.parallaxOpacity);
            layer.style.opacity = Math.max(0, 1 - progress * opSpeed);
          }
        });
        ticking = false;
      };

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
        card.addEventListener('mousemove', (e) => {
          const rect = card.getBoundingClientRect();
          const x = ((e.clientX - rect.left) / rect.width) * 100;
          const y = ((e.clientY - rect.top) / rect.height) * 100;
          card.style.setProperty('--spot-x', `${x}%`);
          card.style.setProperty('--spot-y', `${y}%`);
          card.classList.add('card-spotlight-active');
        });
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
        const cards = container.querySelectorAll('.focus-card');
        if (!cards.length) return;

        container.addEventListener('mouseenter', () => {
          cards.forEach(c => c.classList.add('focus-card-blur'));
        });
        container.addEventListener('mouseleave', () => {
          cards.forEach(c => c.classList.remove('focus-card-blur', 'focus-card-active'));
        });

        cards.forEach(card => {
          card.addEventListener('mouseenter', () => {
            card.classList.remove('focus-card-blur');
            card.classList.add('focus-card-active');
          });
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
        const indicator = document.createElement('div');
        indicator.className = 'animated-tab-indicator';
        tabContainer.appendChild(indicator);

        const tabs = tabContainer.querySelectorAll('.animated-tab');
        const updateIndicator = (activeTab) => {
          const rect = activeTab.getBoundingClientRect();
          const parentRect = tabContainer.getBoundingClientRect();
          indicator.style.width = `${rect.width}px`;
          indicator.style.transform = `translateX(${rect.left - parentRect.left}px)`;
          indicator.style.opacity = '1';
        };

        tabs.forEach(tab => {
          tab.addEventListener('click', () => {
            tabs.forEach(t => t.classList.remove('active'));
            tab.classList.add('active');
            updateIndicator(tab);
            const target = tab.dataset.tabTarget;
            if (target) {
              document.querySelectorAll('.animated-tab-panel').forEach(p => p.classList.add('hidden'));
              document.getElementById(target)?.classList.remove('hidden');
            }
          });
        });

        const activeTab = tabContainer.querySelector('.animated-tab.active');
        if (activeTab) {
          requestAnimationFrame(() => updateIndicator(activeTab));
        }

        window.addEventListener('resize', () => {
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
      const els = elements.length !== undefined ? elements : [elements];
      if (this.prefersReducedMotion) { els.forEach(el => callback(el, true)); return; }
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
        const fullText = el.dataset.text || el.textContent;
        if (!fullText) return;
        el.textContent = '';
        el.style.visibility = 'visible';

        if (this.prefersReducedMotion) {
          el.textContent = fullText;
          return;
        }

        let i = 0;
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
        const ctx = canvas.getContext('2d');
        const colors = (canvas.dataset.colors || '#da291c,#181818,#4a0000').split(',');
        let w, h, time = 0;

        const resize = () => {
          const parent = canvas.parentElement;
          w = canvas.width = parent.offsetWidth;
          h = canvas.height = parent.offsetHeight;
        };
        resize();
        window.addEventListener('resize', resize);

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
      const nav = document.querySelector(selector);
      if (!nav || this.prefersReducedMotion) return;

      let lastScroll = 0;
      let ticking = false;
      const threshold = 10;

      window.addEventListener('scroll', () => {
        if (!ticking) {
          requestAnimationFrame(() => {
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
        const track = carousel.querySelector('.apple-carousel-track');
        const cards = track?.querySelectorAll('.apple-carousel-card');
        if (!cards?.length || this.prefersReducedMotion) return;

        const updateCards = () => {
          const trackRect = track.getBoundingClientRect();
          const centerX = trackRect.left + trackRect.width / 2;

          cards.forEach(card => {
            const cardRect = card.getBoundingClientRect();
            const cardCenterX = cardRect.left + cardRect.width / 2;
            const dist = Math.abs(centerX - cardCenterX);
            const maxDist = trackRect.width / 2;
            const ratio = Math.max(0, 1 - dist / maxDist);

            const scale = 0.82 + ratio * 0.18;
            const opacity = 0.4 + ratio * 0.6;
            card.style.transform = `scale(${scale})`;
            card.style.opacity = opacity;
            card.style.zIndex = ratio > 0.7 ? 2 : 1;
          });
        };

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
        card.addEventListener('click', function () {
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
        card.addEventListener('mousemove', (e) => {
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
      this.observe(selector, (el, visible) => {
        if (visible) el.classList.add('revealed');
      }, { threshold: 0.1, rootMargin: '0px 0px -40px 0px' });
    },

    /* ====================================================================
       15. RIPPLE CLICK EFFECT — Material wave from click point
       Usage: <button class="ripple-container">...</button>
       ==================================================================== */
    rippleClick(selector = '.ripple-container') {
      document.querySelectorAll(selector).forEach(el => {
        el.addEventListener('click', (e) => {
          const ripple = document.createElement('span');
          const rect = el.getBoundingClientRect();
          const size = Math.max(rect.width, rect.height) * 2;
          ripple.className = 'ripple-effect';
          ripple.style.left = `${e.clientX - rect.left - size / 2}px`;
          ripple.style.top  = `${e.clientY - rect.top - size / 2}px`;
          ripple.style.width = ripple.style.height = `${size}px`;
          el.appendChild(ripple);
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
        btn.addEventListener('mousemove', (e) => {
          const rect = btn.getBoundingClientRect();
          const x = (e.clientX - rect.left - rect.width / 2) * 0.15;
          const y = (e.clientY - rect.top - rect.height / 2) * 0.15;
          btn.style.transform = `translate(${x}px, ${y}px)`;
        });
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
        card.addEventListener('mousemove', (e) => {
          const rect = card.getBoundingClientRect();
          const x = e.clientX - rect.left;
          const y = e.clientY - rect.top;
          const centerX = rect.width / 2;
          const centerY = rect.height / 2;
          const rotateX = ((y - centerY) / centerY) * -8;
          const rotateY = ((x - centerX) / centerX) * 8;
          card.style.transform = `perspective(1000px) rotateX(${rotateX}deg) rotateY(${rotateY}deg)`;
          card.style.setProperty('--tilt-x', `${(x / rect.width) * 100}%`);
          card.style.setProperty('--tilt-y', `${(y / rect.height) * 100}%`);
        });
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
        const count = 50;
        for (let i = 0; i < count; i++) {
          const particle = document.createElement('div');
          particle.className = 'dust-particle';
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
        document.addEventListener('DOMContentLoaded', () => this._initDelayed());
      } else {
        this._initDelayed();
      }
    },

    _initDelayed() {
      const effects = {
        heroParallax: '.hero-parallax',
        animatedTabs: '.animated-tabs',
        textGenerate: '.text-generate',
        auroraBackground: '.aurora-bg',
        heroHighlight: '.hero-highlight-text',
        appleCarousel: '.apple-carousel',
        lampEffect: '.lamp-header',
      };
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
