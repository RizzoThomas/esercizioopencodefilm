# Piano di Lavoro - Iterazione 3: Autenticazione, Autorizzazione, RBAC, Area Utente e Categorie

## 1) Panoramica

Questa iterazione implementa la sicurezza completa dell'applicazione CineBase con:
- **Autenticazione JWT** con access token (15 min) e refresh token (7 giorni)
- **RBAC (Role-Based Access Control)** con 3 ruoli gerarchici
- **Area personale utente** con salvataggio proiezioni e prenotazioni virtuali
- **Categorie film** con relazione many-to-many
- **Controllo accessi frontend** con re-direzione automatica

### 1.1 Ruoli e Permessi

| Ruolo | Permessi |
|-------|----------|
| **Admin** | Full access: CRUD su tutte le entità (Film, Registi, Proiezioni, Cinema, Categorie, Utenti) |
| **Power User** | CRUD su Film, Proiezioni, Registi. Solo READ su Cinema (no create/update/delete) |
| **User** | Vede proiezioni pubbliche, salva film nell'area personale, gestisce prenotazioni virtuali. NO accesso area admin |
| **Non autenticato** | Solo visualizzazione pagina pubblica (index.html) e lista proiezioni. Redirect a login se tenta prenotazione |

### 1.2 Flusso di Accesso

```
Utente non autenticato:
  ↓
index.html (accessibile) → Proiezioni (visualizzazione) → [Clicca Prenota] → redirect → login.html
  ↓
Login success → Riceve JWT tokens → redirect based on role

Admin → dashboard.html (nav completa)
Power User → dashboard.html (nav limitata: no gestione Cinema)
User → index.html (nav user: Home, Proiezioni, Area Personale)
```

---

## 2) Schema Dati Nuovi

### 2.1 Utente (User)
```csharp
public class User {
    public int Id { get; set; }
    public string Email { get; set; } = null!;          // Unique, usata come username
    public string PasswordHash { get; set; } = null!;    // BCrypt
    public string Nome { get; set; } = null!;
    public string Cognome { get; set; } = null!;
    public string? Telefono { get; set; }
    public DateTime? DataNascita { get; set; }
    public UserRole Ruolo { get; set; }                  // Enum: Admin, PowerUser, User
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; }
    
    // Navigation
    public ICollection<UserProiezione> ProiezioniSalvate { get; set; } = [];
    public ICollection<Prenotazione> Prenotazioni { get; set; } = [];
}

public enum UserRole {
    Admin = 0,
    PowerUser = 1,
    User = 2
}
```

### 2.2 RefreshToken
```csharp
public class RefreshToken {
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Token { get; set; } = null!;          // GUID string
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRevoked { get; set; }
    public string? ReplacedByToken { get; set; }
    
    public User User { get; set; } = null!;
}
```

### 2.3 Categoria (Categorie Film)
```csharp
public class Categoria {
    public int Id { get; set; }
    public string Nome { get; set; } = null!;           // Unique
    public string? Descrizione { get; set; }
    
    // Navigation
    public ICollection<Film> Films { get; set; } = [];
}

// Tabella di join per relazione many-to-many
public class FilmCategoria {
    public int FilmId { get; set; }
    public int CategoriaId { get; set; }
    public Film Film { get; set; } = null!;
    public Categoria Categoria { get; set; } = null!;
}
```

### 2.4 UserProiezione (Proiezioni Salvate)
```csharp
public class UserProiezione {
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ProiezioneId { get; set; }
    public DateTime SavedAt { get; set; }
    public string? Note { get; set; }
    
    public User User { get; set; } = null!;
    public Proiezione Proiezione { get; set; } = null!;
}
```

### 2.5 Prenotazione (Prenotazione Virtuale)
```csharp
public class Prenotazione {
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ProiezioneId { get; set; }
    public int NumeroPosti { get; set; }
    public DateTime CreatedAt { get; set; }
    public StatoPrenotazione Stato { get; set; }
    public string? CodicePrenotazione { get; set; }      // Codice univoco (ex: PRE-2024-XXXX)
    public decimal? PrezzoTotale { get; set; }
    
    public User User { get; set; } = null!;
    public Proiezione Proiezione { get; set; } = null!;
}

public enum StatoPrenotazione {
    InAttesa = 0,    // Prenotazione creata, pagamento da effettuare
    Confermata = 1,  // Pagamento completato (futuro)
    Annullata = 2
}
```

---

## 3) Fasi di Implementazione

### FASE 1: Setup Autenticazione Backend (Fondamentale)

#### 1.1 Aggiungere pacchetti NuGet
```bash
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package BCrypt.Net-Next
```

#### 1.2 Configurazione JWT in Program.cs
```csharp
// Aggiungere configurazione JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
            ),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
```

#### 1.3 Variabili ambiente (.env)
```
JWT_KEY=your-super-secret-key-min-32-chars-long!!
JWT_ISSUER=FilmAPI
JWT_AUDIENCE=CineBase.Web
JWT_ACCESS_TOKEN_EXPIRATION_MINUTES=15
JWT_REFRESH_TOKEN_EXPIRATION_DAYS=7
```

#### 1.4 Aggiornare FilmDbContext
```csharp
// Aggiungere DbSet
public DbSet<User> Users { get; set; }
public DbSet<RefreshToken> RefreshTokens { get; set; }
public DbSet<Categoria> Categorie { get; set; }
public DbSet<FilmCategoria> FilmCategorie { get; set; }
public DbSet<UserProiezione> UserProiezioni { get; set; }
public DbSet<Prenotazione> Prenotazioni { get; set; }

// Configurare relazioni e vincoli in OnModelCreating
```

---

### FASE 2: Modelli e DTO Autenticazione

#### 2.1 Creare cartelle struttura
```
backend/FilmAPI/
├── Model/
│   ├── User.cs
│   ├── RefreshToken.cs
│   ├── Categoria.cs
│   ├── FilmCategoria.cs
│   ├── UserProiezione.cs
│   └── Prenotazione.cs
├── DTO/
│   ├── Auth/
│   │   ├── LoginRequestDTO.cs
│   │   ├── LoginResponseDTO.cs
│   │   ├── RefreshTokenRequestDTO.cs
│   │   └── RegisterRequestDTO.cs
│   ├── User/
│   │   ├── UserDTO.cs
│   │   ├── UserCreateDTO.cs
│   │   ├── UserUpdateDTO.cs
│   │   └── ChangePasswordDTO.cs
│   ├── Categoria/
│   │   ├── CategoriaDTO.cs
│   │   └── CategoriaCreateDTO.cs
│   ├── UserProiezione/
│   │   ├── UserProiezioneDTO.cs
│   │   └── UserProiezioneCreateDTO.cs
│   └── Prenotazione/
│       ├── PrenotazioneDTO.cs
│       └── PrenotazioneCreateDTO.cs
└── Services/
    ├── IAuthService.cs
    ├── AuthService.cs
    ├── IJwtService.cs
    ├── JwtService.cs
    ├── IUserService.cs
    ├── UserService.cs
    ├── ICategoriaService.cs
    ├── CategoriaService.cs
    ├── IUserProiezioneService.cs
    ├── UserProiezioneService.cs
    ├── IPrenotazioneService.cs
    └── PrenotazioneService.cs
```

#### 2.2 DTO Autenticazione

**LoginRequestDTO.cs:**
```csharp
public record LoginRequestDTO(
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string Password
);
```

**LoginResponseDTO.cs:**
```csharp
public record LoginResponseDTO(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserDTO User
);
```

**RegisterRequestDTO.cs:**
```csharp
public record RegisterRequestDTO(
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string Password,
    [Required] string Nome,
    [Required] string Cognome,
    string? Telefono,
    DateTime? DataNascita
);
```

**UserDTO.cs:**
```csharp
public record UserDTO(
    int Id,
    string Email,
    string Nome,
    string Cognome,
    string? Telefono,
    DateTime? DataNascita,
    string Ruolo,
    DateTime CreatedAt
);
```

---

### FASE 3: Servizi Autenticazione

#### 3.1 IJwtService + JwtService
Responsabilità:
- Generare access token JWT
- Generare refresh token (GUID)
- Validare token
- Estrarre claims (UserId, Email, Role)

#### 3.2 IAuthService + AuthService
Responsabilità:
- Login (verifica credenziali, genera tokens)
- Register (hash password, crea utente)
- Refresh token (valida refresh, genera nuovi tokens)
- Revoke token (logout)
- Password hash/verify (BCrypt)

#### 3.3 IUserService + UserService
Responsabilità:
- CRUD utenti (admin only)
- Get current user
- Update profilo (user può modificare solo se stesso)
- Change password
- Soft delete (disattiva)

---

### FASE 4: Endpoints Autenticazione

#### 4.1 AuthEndpoints.cs

```csharp
public static class AuthEndpoints {
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app) {
        var group = app.MapGroup("/auth")
            .WithTags("Authentication")
            .WithOpenApi();
        
        // POST /auth/login - Pubblico
        group.MapPost("/login", async (LoginRequestDTO dto, IAuthService authService) => {
            var result = await authService.LoginAsync(dto);
            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : Results.Unauthorized();
        })
        .AllowAnonymous()
        .WithName("Login")
        .Produces<LoginResponseDTO>(200)
        .Produces(401);
        
        // POST /auth/register - Pubblico
        group.MapPost("/register", async (RegisterRequestDTO dto, IAuthService authService) => {
            var result = await authService.RegisterAsync(dto);
            return result.IsSuccess
                ? Results.Created($"/users/{result.Value.Id}", result.Value)
                : Results.BadRequest(result.Error);
        })
        .AllowAnonymous()
        .WithName("Register")
        .Produces<UserDTO>(201)
        .Produces(400);
        
        // POST /auth/refresh - Pubblico (richiede refresh token valido)
        group.MapPost("/refresh", async (RefreshTokenRequestDTO dto, IAuthService authService) => {
            var result = await authService.RefreshTokenAsync(dto);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Unauthorized();
        })
        .AllowAnonymous()
        .WithName("RefreshToken")
        .Produces<LoginResponseDTO>(200)
        .Produces(401);
        
        // POST /auth/logout - Richiede autenticazione
        group.MapPost("/logout", async (HttpContext httpContext, IAuthService authService) => {
            var userId = httpContext.User.GetUserId(); // Extension method
            await authService.LogoutAsync(userId);
            return Results.NoContent();
        })
        .RequireAuthorization()
        .WithName("Logout")
        .Produces(204);
        
        // GET /auth/me - Richiede autenticazione
        group.MapGet("/me", async (HttpContext httpContext, IUserService userService) => {
            var userId = httpContext.User.GetUserId();
            var user = await userService.GetByIdAsync(userId);
            return user is not null ? Results.Ok(user) : Results.NotFound();
        })
        .RequireAuthorization()
        .WithName("GetCurrentUser")
        .Produces<UserDTO>(200)
        .Produces(404);
        
        return app;
    }
}
```

---

### FASE 5: Middleware e Protezione Endpoint

#### 5.1 Custom Attributes

**RequireRoleAttribute:**
```csharp
public class RequireRoleAttribute : AuthorizeAttribute {
    public RequireRoleAttribute(params UserRole[] roles) {
        var roleNames = roles.Select(r => r.ToString()).ToArray();
        Roles = string.Join(",", roleNames);
    }
}
```

#### 5.2 Extension Methods per Claims

```csharp
public static class ClaimsPrincipalExtensions {
    public static int GetUserId(this ClaimsPrincipal user) {
        var claim = user.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null ? int.Parse(claim.Value) : throw new UnauthorizedAccessException();
    }
    
    public static string GetUserEmail(this ClaimsPrincipal user) {
        return user.FindFirst(ClaimTypes.Email)?.Value 
            ?? throw new UnauthorizedAccessException();
    }
    
    public static UserRole GetUserRole(this ClaimsPrincipal user) {
        var role = user.FindFirst(ClaimTypes.Role)?.Value;
        return Enum.Parse<UserRole>(role ?? "User");
    }
    
    public static bool IsInRole(this ClaimsPrincipal user, UserRole role) {
        return user.GetUserRole() == role || user.GetUserRole() == UserRole.Admin;
    }
}
```

#### 5.3 Proteggere endpoint esistenti

**RegistiEndpoints (esempio):**
```csharp
// GET - Pubblico (anche non autenticato)
group.MapGet("").AllowAnonymous();
group.MapGet("/{id}").AllowAnonymous();

// POST/PUT/DELETE - Richiede Admin o PowerUser
group.MapPost("").RequireAuthorization(new[] { UserRole.Admin, UserRole.PowerUser });
group.MapPut("/{id}").RequireAuthorization(new[] { UserRole.Admin, UserRole.PowerUser });
group.MapDelete("/{id}").RequireAuthorization(new[] { UserRole.Admin, UserRole.PowerUser });
```

**CinemasEndpoints (esempio):**
```csharp
// GET - Pubblico
group.MapGet("").AllowAnonymous();
group.MapGet("/{id}").AllowAnonymous();

// POST/PUT/DELETE - Solo Admin (PowerUser può solo leggere)
group.MapPost("").RequireAuthorization(new[] { UserRole.Admin });
group.MapPut("/{id}").RequireAuthorization(new[] { UserRole.Admin });
group.MapDelete("/{id}").RequireAuthorization(new[] { UserRole.Admin });
```

---

### FASE 6: Categorie Film

#### 6.1 Modifica Film model
Aggiungere:
```csharp
public ICollection<Categoria> Categorie { get; set; } = [];
```

#### 6.2 Endpoints Categorie

```csharp
// GET /categorie - Pubblico
// GET /categorie/{id} - Pubblico
// POST /categorie - Admin only
// PUT /categorie/{id} - Admin only
// DELETE /categorie/{id} - Admin only

// GET /films/{id}/categorie - Pubblico
// POST /films/{id}/categorie - Admin, PowerUser
// DELETE /films/{id}/categorie/{categoriaId} - Admin, PowerUser
```

#### 6.3 Seed Categorie Predefinite
In `DbSeeder.cs` creare categorie base:
- Azione, Avventura, Animazione, Commedia, Drammatico, Fantasy, Horror, Thriller, Sci-Fi, Documentario

---

### FASE 7: Area Personale API

#### 7.1 UserProiezioniEndpoints

```csharp
// GET /me/proiezioni - Proiezioni salvate dall'utente corrente
// POST /me/proiezioni - Salva una proiezione
// DELETE /me/proiezioni/{id} - Rimuove dai salvati

// GET /me/prenotazioni - Prenotazioni dell'utente
// POST /me/prenotazioni - Crea prenotazione virtuale
// PUT /me/prenotazioni/{id}/annulla - Annulla prenotazione
```

#### 7.2 DTO Area Personale

**UserProiezioneDTO:**
```csharp
public record UserProiezioneDTO(
    int Id,
    int ProiezioneId,
    FilmSummaryDTO Film,
    CinemaSummaryDTO Cinema,
    DateTime DataProiezione,
    TimeSpan OraProiezione,
    DateTime SavedAt,
    string? Note
);
```

**PrenotazioneDTO:**
```csharp
public record PrenotazioneDTO(
    int Id,
    string CodicePrenotazione,
    FilmSummaryDTO Film,
    CinemaSummaryDTO Cinema,
    DateTime DataProiezione,
    int NumeroPosti,
    decimal? PrezzoTotale,
    StatoPrenotazione Stato,
    DateTime CreatedAt
);
```

---

### FASE 8: Frontend - Autenticazione

#### 8.1 Nuovi file JS

**js/auth.js** - Gestione autenticazione:
```javascript
const Auth = {
    // Storage keys
    ACCESS_TOKEN_KEY: 'cinebase_access_token',
    REFRESH_TOKEN_KEY: 'cinebase_refresh_token',
    USER_KEY: 'cinebase_user',
    
    // Login
    async login(email, password) {
        const response = await apiFetch('/auth/login', {
            method: 'POST',
            body: JSON.stringify({ email, password })
        });
        
        this.setTokens(response.accessToken, response.refreshToken);
        this.setUser(response.user);
        
        return response.user;
    },
    
    // Logout
    async logout() {
        try {
            await apiFetch('/auth/logout', { method: 'POST' });
        } finally {
            this.clearAuth();
        }
    },
    
    // Refresh token
    async refreshToken() {
        const refreshToken = this.getRefreshToken();
        if (!refreshToken) throw new Error('No refresh token');
        
        const response = await apiFetch('/auth/refresh', {
            method: 'POST',
            body: JSON.stringify({ refreshToken })
        });
        
        this.setTokens(response.accessToken, response.refreshToken);
        this.setUser(response.user);
        
        return response.accessToken;
    },
    
    // Token management
    getAccessToken() { return localStorage.getItem(this.ACCESS_TOKEN_KEY); },
    getRefreshToken() { return localStorage.getItem(this.REFRESH_TOKEN_KEY); },
    getUser() { 
        const user = localStorage.getItem(this.USER_KEY);
        return user ? JSON.parse(user) : null;
    },
    getUserRole() {
        const user = this.getUser();
        return user?.ruolo || null;
    },
    
    setTokens(access, refresh) {
        localStorage.setItem(this.ACCESS_TOKEN_KEY, access);
        localStorage.setItem(this.REFRESH_TOKEN_KEY, refresh);
    },
    
    setUser(user) {
        localStorage.setItem(this.USER_KEY, JSON.stringify(user));
    },
    
    clearAuth() {
        localStorage.removeItem(this.ACCESS_TOKEN_KEY);
        localStorage.removeItem(this.REFRESH_TOKEN_KEY);
        localStorage.removeItem(this.USER_KEY);
    },
    
    isAuthenticated() {
        return !!this.getAccessToken();
    },
    
    isAdmin() { return this.getUserRole() === 'Admin'; },
    isPowerUser() { return this.getUserRole() === 'PowerUser'; },
    isUser() { return this.getUserRole() === 'User'; },
    
    // Can access admin area
    canAccessAdmin() {
        return this.isAdmin() || this.isPowerUser();
    },
    
    // Can manage cinemas (solo Admin)
    canManageCinemas() {
        return this.isAdmin();
    },
    
    // Can manage films/proiezioni/registi
    canManageContent() {
        return this.isAdmin() || this.isPowerUser();
    }
};
```

**js/api.js** - Aggiornamento con token:
```javascript
// Aggiungere header Authorization automaticamente
async function apiFetch(endpoint, options = {}) {
    const token = Auth.getAccessToken();
    
    const defaultOptions = {
        headers: {
            'Content-Type': 'application/json',
            ...(token && { 'Authorization': `Bearer ${token}` })
        }
    };
    
    // ... resto logica con gestione 401 -> refresh token
}
```

**js/router.js** - Controllo accessi pagine:
```javascript
const Router = {
    // Mappa ruoli -> pagine accessibili
    permissions: {
        'admin': ['*'],  // Tutte le pagine
        'poweruser': ['dashboard', 'films', 'registi', 'proiezioni', 'area-personale'],
        'user': ['index', 'proiezioni-pubblico', 'area-personale', 'login'],
        'guest': ['index', 'login', 'register']
    },
    
    // Verifica accesso alla pagina corrente
    checkAccess() {
        const currentPage = window.location.pathname;
        const role = Auth.getUserRole() || 'guest';
        
        // Definire quali pagine sono admin
        const adminPages = ['/dashboard.html', '/cinemas.html', '/registi.html', '/films.html', '/proiezioni.html'];
        const requiresAuth = adminPages.includes(currentPage) || currentPage === '/area-personale.html';
        
        // Non autenticato su pagina protetta -> redirect login
        if (requiresAuth && !Auth.isAuthenticated()) {
            window.location.href = '/login.html?redirect=' + encodeURIComponent(currentPage);
            return false;
        }
        
        // User su pagina admin -> redirect home
        if (Auth.isUser() && adminPages.includes(currentPage)) {
            window.location.href = '/index.html';
            return false;
        }
        
        // PowerUser su cinemas.html -> redirect dashboard
        if (Auth.isPowerUser() && currentPage === '/cinemas.html') {
            window.location.href = '/dashboard.html';
            return false;
        }
        
        return true;
    }
};

// Eseguire check all'avvio
document.addEventListener('DOMContentLoaded', () => {
    Router.checkAccess();
});
```

#### 8.2 Nuove pagine HTML

**login.html**:
```html
<!DOCTYPE html>
<html lang="it">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Login - CineBase</title>
    <!-- Tailwind CSS, Fonts, Icons -->
</head>
<body class="bg-brand-dark min-h-screen flex items-center justify-center">
    <div class="bg-brand-dark-lighter p-8 rounded-2xl border border-white/10 w-full max-w-md">
        <div class="text-center mb-8">
            <h1 class="text-3xl font-bold text-brand-orange">🎬 CineBase</h1>
            <p class="text-gray-400 mt-2">Accedi al tuo account</p>
        </div>
        
        <form id="login-form" class="space-y-6">
            <div>
                <label class="block text-sm font-medium text-gray-300 mb-2">Email</label>
                <input type="email" name="email" required
                    class="w-full bg-brand-dark border border-white/10 rounded-lg px-4 py-3 text-white focus:ring-2 focus:ring-brand-orange">
            </div>
            
            <div>
                <label class="block text-sm font-medium text-gray-300 mb-2">Password</label>
                <input type="password" name="password" required minlength="6"
                    class="w-full bg-brand-dark border border-white/10 rounded-lg px-4 py-3 text-white focus:ring-2 focus:ring-brand-orange">
            </div>
            
            <button type="submit" 
                class="w-full bg-brand-orange hover:bg-brand-orange-dark text-white font-semibold py-3 rounded-lg transition-colors">
                Accedi
            </button>
        </form>
        
        <div class="mt-6 text-center">
            <p class="text-gray-400">Non hai un account? 
                <a href="/register.html" class="text-brand-orange hover:underline">Registrati</a>
            </p>
        </div>
    </div>
    
    <script src="/js/utils.js"></script>
    <script src="/js/api.js"></script>
    <script src="/js/auth.js"></script>
    <script src="/js/pages/login.js"></script>
</body>
</html>
```

**register.html** - Form registrazione con campi: email, password, nome, cognome, telefono (opz), data nascita (opz)

**area-personale.html** - Pagina area utente con:
- Dati profilo (modificabili)
- Proiezioni salvate
- Prenotazioni effettuate
- Sezione "I miei film"

#### 8.3 Componenti Navbar Aggiornati

**navbar-landing.html** (versione user autenticato):
```html
<nav class="sticky top-0 z-50 bg-brand-dark/95 backdrop-blur-md border-b border-white/10">
    <div class="container mx-auto px-4 h-20 flex items-center justify-between">
        <a href="/index.html" class="text-2xl font-bold text-brand-orange">🎬 CineBase</a>
        
        <div class="hidden md:flex items-center gap-8">
            <a href="/index.html" class="text-white hover:text-brand-orange">Home</a>
            <a href="/proiezioni.html" class="text-white hover:text-brand-orange">Proiezioni</a>
            <a href="/area-personale.html" class="text-white hover:text-brand-orange">Area Personale</a>
        </div>
        
        <!-- User Dropdown -->
        <div class="relative" id="user-menu">
            <button id="user-dropdown-btn" class="flex items-center gap-2 text-white">
                <span id="user-name">Mario Rossi</span>
                <i class="fa-solid fa-chevron-down"></i>
            </button>
            <div id="user-dropdown" class="hidden absolute right-0 mt-2 w-48 bg-brand-dark-lighter rounded-lg border border-white/10">
                <a href="/area-personale.html" class="block px-4 py-2 text-white hover:bg-white/5">Profilo</a>
                <a href="/area-personale.html#prenotazioni" class="block px-4 py-2 text-white hover:bg-white/5">Prenotazioni</a>
                <hr class="border-white/10">
                <button id="logout-btn" class="w-full text-left px-4 py-2 text-red-400 hover:bg-white/5">
                    Logout
                </button>
            </div>
        </div>
    </div>
</nav>
```

**navbar-admin.html** (versione con ruoli):
- Admin: vede tutto (Dashboard, Films, Registi, Cinemas, Proiezioni)
- PowerUser: vede Dashboard, Films, Registi, Proiezioni (NO Cinemas)

```javascript
// In js/navbar.js - Logica per mostrare/nascondere voci menu
function updateNavbarForRole() {
    const user = Auth.getUser();
    if (!user) return;
    
    // Nascondi voci in base al ruolo
    if (Auth.isUser()) {
        // Nascondi tutta la nav admin
        document.getElementById('admin-nav')?.classList.add('hidden');
        document.getElementById('user-nav')?.classList.remove('hidden');
    } else if (Auth.isPowerUser()) {
        // Nascondi link Cinema
        document.getElementById('nav-cinemas')?.classList.add('hidden');
    }
}
```

---

### FASE 9: Migrations e Seed Dati

#### 9.1 Creare Migration
```bash
cd backend/FilmAPI
dotnet ef migrations add Iteration3AuthAndCategories
dotnet ef database update
```

#### 9.2 Seed Utenti Test
In `DbSeeder.cs` aggiungere:
```csharp
// Admin: admin@cinebase.it / admin123
// PowerUser: power@cinebase.it / power123
// User: user@cinebase.it / user123
```

---

### FASE 10: Test e Verifica

#### 10.1 Test Backend
```bash
dotnet test tests/backend/FilmAPI.Tests.csproj
```

Nuovi test da implementare:
- **Unit Test**: JwtService, AuthService, Password hashing
- **Integration Test**: 
  - Login con credenziali corrette → 200 + tokens
  - Login con credenziali errate → 401
  - Accesso endpoint protetto senza token → 401
  - Accesso endpoint con token scaduto → 401
  - Accesso endpoint admin con ruolo User → 403
  - Refresh token valido → nuovi tokens
  - Refresh token scaduto → 401

#### 10.2 Test Manuale Frontend
Checklist:
- [ ] Login con credenziali valide → redirect corretto
- [ ] Login con credenziali errate → messaggio errore
- [ ] Accesso pagina admin senza login → redirect login
- [ ] Utente autenticato su cinemas.html → redirect dashboard
- [ ] Token scaduto → auto-refresh e retry richiesta
- [ ] Logout → pulizia storage e redirect home
- [ ] PowerUser vede menu ridotto (no cinemas)
- [ ] User vede solo menu pubblico + area personale

#### 10.3 Test Flusso Completo
```
1. Utente non autenticato:
   - Apre index.html ✓
   - Vede proiezioni ✓
   - Clicca "Prenota" → redirect login.html ✓

2. Registrazione nuovo utente:
   - Compila form register.html ✓
   - Riceve conferma → redirect login ✓

3. Login User:
   - Accede con credenziali ✓
   - Redirect index.html ✓
   - Vede "Area Personale" nel menu ✓
   - Non vede link admin ✓
   - Prova ad accedere a /dashboard.html → redirect index ✓

4. Login PowerUser:
   - Accede con credenziali ✓
   - Redirect dashboard.html ✓
   - Vede menu: Dashboard, Films, Registi, Proiezioni ✓
   - NON vede Cinemas ✓
   - Può creare Film ✓
   - Prova cinemas.html → redirect dashboard ✓

5. Login Admin:
   - Accede con credenziali ✓
   - Redirect dashboard.html ✓
   - Vede menu completo ✓
   - Può gestire utenti ✓

6. Area Personale:
   - Salva proiezione ✓
   - Vede in "Proiezioni Salvate" ✓
   - Crea prenotazione ✓
   - Vede codice prenotazione ✓
```

---

## 4) Categorie - Implementazione Dettagliata

### 4.1 Backend

**CategoriaService** - Operazioni CRUD e gestione associazioni

**Endpoints Categorie:**
```csharp
// GET /api/categorie - Lista tutte le categorie
// GET /api/categorie/{id} - Dettaglio categoria con film associati
// POST /api/categorie - Crea nuova categoria (Admin)
// PUT /api/categorie/{id} - Aggiorna categoria (Admin)
// DELETE /api/categorie/{id} - Elimina categoria (Admin)
```

**Endpoints Film-Categorie:**
```csharp
// POST /api/films/{filmId}/categorie - Aggiunge categorie a film
// DELETE /api/films/{filmId}/categorie/{categoriaId} - Rimuove categoria da film
// GET /api/films?categoria={nome} - Filtra film per categoria
```

### 4.2 Frontend

**Modifica films.html:**
- Aggiungere colonna "Categorie" nella tabella
- Nel modal create/edit: multi-select per categorie

**js/pages/films.js:**
```javascript
// Carica categorie disponibili
async function loadCategorie() {
    const categorie = await API.getCategorie();
    // Popola multi-select
}

// Al salvataggio film, invia array categorieId
async function saveFilm(data) {
    const film = await API.createFilm(data);
    if (data.categorieIds?.length) {
        await API.addCategorieToFilm(film.id, data.categorieIds);
    }
}
```

---

## 5) Struttura File Finale

```
repo-root/
├── backend/
│   └── FilmAPI/
│       ├── Model/
│       │   ├── User.cs
│       │   ├── RefreshToken.cs
│       │   ├── Categoria.cs
│       │   ├── FilmCategoria.cs
│       │   ├── UserProiezione.cs
│       │   └── Prenotazione.cs
│       ├── DTO/
│       │   ├── Auth/
│       │   ├── User/
│       │   ├── Categoria/
│       │   ├── UserProiezione/
│       │   └── Prenotazione/
│       ├── Services/
│       │   ├── IAuthService.cs
│       │   ├── AuthService.cs
│       │   ├── IJwtService.cs
│       │   ├── JwtService.cs
│       │   ├── IUserService.cs
│       │   ├── UserService.cs
│       │   ├── ICategoriaService.cs
│       │   ├── CategoriaService.cs
│       │   ├── IUserProiezioneService.cs
│       │   ├── UserProiezioneService.cs
│       │   ├── IPrenotazioneService.cs
│       │   └── PrenotazioneService.cs
│       ├── Endpoints/
│       │   ├── AuthEndpoints.cs
│       │   ├── UserEndpoints.cs
│       │   ├── CategorieEndpoints.cs
│       │   ├── UserProiezioniEndpoints.cs
│       │   └── PrenotazioniEndpoints.cs
│       └── Extensions/
│           └── ClaimsPrincipalExtensions.cs
├── frontend/
│   └── CineBase.Web/
│       └── wwwroot/
│           ├── login.html
│           ├── register.html
│           ├── area-personale.html
│           ├── js/
│           │   ├── auth.js
│           │   ├── router.js
│           │   ├── pages/
│           │   │   ├── login.js
│           │   │   ├── register.js
│           │   │   └── area-personale.js
│           └── components/
│               ├── navbar-user.html
│               └── navbar-public.html
└── tests/
    └── backend/
        └── (test auth, categorie, area personale)
```

---

## 6) Prompt per AI Implementation

```
Implementa Iterazione 3 del progetto CineBase seguendo il piano in docs/project/dev_iteration/3/PianoDiLavoro.md.

PRIORITÀ:
1. FASE 1-3: Setup JWT, modelli auth, servizi base
2. FASE 4-5: Endpoints auth e protezione esistenti
3. FASE 6: Categorie film
4. FASE 7: Area personale API
5. FASE 8: Frontend login e controllo accessi

REQUISITI TECNICI:
- Access token: 15 minuti, Refresh token: 7 giorni
- Password: BCrypt con salt
- Ruoli: Admin (0), PowerUser (1), User (2)
- Storage token frontend: localStorage
- Categorie: seed con 10 categorie predefinite
- Relazione Film-Categorie: many-to-many

VALIDAZIONI:
- Email: formato valido, unique
- Password: min 6 caratteri
- Registrazione: ruolo default = User

RE-DIREZIONI FRONTEND:
- Non autenticato → area protetta → /login.html
- User → area admin → /index.html
- PowerUser → /cinemas.html → /dashboard.html

TEST:
- Aggiungere test integration per auth
- Verificare protezione endpoint esistenti
- Eseguire: dotnet test tests/backend/FilmAPI.Tests.csproj

Non modificare la struttura esistente in backend/FilmAPI (usa cartelle nuove dove indicato).
```

---

## 7) Checklist Completamento

### Backend
- [ ] Pacchetti JWT e BCrypt installati
- [ ] Configurazione JWT in Program.cs
- [ ] Modelli User, RefreshToken, Categoria, etc.
- [ ] Migration creata e applicata
- [ ] Servizi Auth, JWT, User implementati
- [ ] Endpoints /auth/login, /auth/register, /auth/refresh, /auth/logout, /auth/me
- [ ] Endpoint protetti con [Authorize] e ruoli
- [ ] Categorie CRUD endpoint
- [ ] Area personale endpoints
- [ ] Seed utenti test in DbSeeder
- [ ] Test backend passano

### Frontend
- [ ] auth.js con gestione token
- [ ] router.js con controllo accessi
- [ ] api.js aggiornato con header Authorization
- [ ] login.html e login.js
- [ ] register.html e register.js
- [ ] area-personale.html e area-personale.js
- [ ] Navbar dinamica per ruoli
- [ ] Re-direzione automatica pagine protette
- [ ] Gestione token scaduto (auto-refresh)

### Integrazione
- [ ] Login funziona correttamente
- [ ] Ruoli rispettati nelle pagine
- [ ] Categorie associate ai film
- [ ] Area personale funzionante
- [ ] Logout pulisce i dati
- [ ] Test end-to-end completati

---

## 8) Note Implementative

### Sicurezza
- Non loggare mai password o token
- Validare sempre input utente
- Usare HTTPS in produzione
- Refresh token rotation opzionale (implementare se c'è tempo)

### Performance
- Aggiungere indici su User.Email e RefreshToken.Token
- Cache ruoli in claim JWT (evita query DB)

### UX
- Mostrare spinner durante login
- Messaggi errore chiari ("Email o password errati", non "401 Unauthorized")
- Redirect alla pagina originale dopo login (parametro ?redirect=)

---

**Data creazione piano:** 2026-04-01  
**Iterazione:** 3  
**Stato:** Pronto per implementazione
