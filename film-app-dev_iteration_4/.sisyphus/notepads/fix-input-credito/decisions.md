- Kept the fix minimal: reused existing Auth.isLoggedIn() and existing toast container on offerte.html rather than changing navbar or payment flow.
- Supported both redirect and returnUrl query params in login.js for compatibility with existing callers.

