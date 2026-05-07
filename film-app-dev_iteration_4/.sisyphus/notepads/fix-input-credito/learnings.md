- offerte.html now uses auth-aware subscription CTA: logged-out users go to /login.html?redirect=/offerte.html, logged-in users get an info toast instead of /registrazione.html.
- login flow already supports redirect; added returnUrl fallback in js/pages/login.js so post-login can return to offerte or other protected pages.

