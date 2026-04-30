# Script di trasformazione Design Lamborghini per CineBase
# Applica automaticamente il design system alle pagine HTML

$transformations = @{
    # Colori
    'bg-brand-surface' = 'bg-black'
    'text-brand-on-surface' = 'text-white'
    'text-brand-on-surface-variant' = 'text-ash'
    'bg-brand-gold' = 'bg-gold'
    'text-brand-gold' = 'text-gold'
    'border-brand-gold' = 'border-gold'
    'hover:text-brand-gold' = 'hover:text-gold'
    'hover:bg-brand-gold' = 'hover:bg-gold'
    'bg-brand-surface-container' = 'bg-charcoal'
    'bg-brand-surface-container-high' = 'bg-charcoal'
    'bg-brand-surface-container-highest' = 'bg-charcoal'
    'bg-brand-surface-container-low' = 'bg-dark-iron'
    'bg-brand-surface-container-lowest' = 'bg-black'
    'border-brand-outline-variant' = 'border-white/10'
    'border-brand-outline' = 'border-graphite'
    
    # Bordi arrotondati → angoli netti
    'rounded-xl' = ''
    'rounded-2xl' = ''
    'rounded-lg' = ''
    'rounded-md' = ''
    'rounded-full' = ''
    'rounded' = ''
    'rounded-sm' = ''
    
    # Bottoni
    'btn-outline-brand' = 'btn btn-ghost'
    'cyber-btn-primary' = 'btn btn-gold'
    'btn-gold' = 'btn btn-gold'
    'btn-primary' = 'btn btn-gold'
    
    # Card e container
    'card-elevated' = 'bg-charcoal border border-white/10'
    'glass-panel' = 'bg-charcoal'
    'ghost-input' = 'input-dark'
    
    # Testo
    'font-bold' = 'font-normal'
    'font-semibold' = 'font-normal'
    'text-brand-error' = 'text-red-400'
    'bg-brand-error-container' = 'bg-red-900/20'
    'border-brand-error' = 'border-red-500'
}

# Funzione per applicare le trasformazioni
function Apply-LamborghiniTransform {
    param([string]$content)
    
    $newContent = $content
    
    foreach ($old in $transformations.Keys) {
        $new = $transformations[$old]
        if ($new -ne '') {
            $newContent = $newContent -replace [regex]::Escape($old), $new
        } else {
            # Rimuovi il pattern se il valore è vuoto
            $newContent = $newContent -replace "\s*$([regex]::Escape($old))", ''
        }
    }
    
    return $newContent
}

# Funzione per aggiornare il body tag
function Update-BodyTag {
    param([string]$content)
    
    # Aggiungi class dark e cambia le classi del body
    $content = $content -replace '<html lang="it-IT">', '<html lang="it-IT" class="dark">'
    $content = $content -replace 'class="bg-brand-surface text-brand-on-surface font-sans"', 'class="bg-black text-white font-sans"'
    $content = $content -replace "Inter:wght", "Roboto:wght"
    $content = $content -replace "Inter", "Roboto"
    
    return $content
}

# Elabora tutti i file HTML
$htmlFiles = Get-ChildItem -Path 'frontend/CineBase.Web/wwwroot' -Filter '*.html' -Recurse | Where-Object { 
    $_.Name -notin @('index.html', 'login.html', 'registrazione.html', 'navbar-landing.html', 'footer-landing.html')
}

foreach ($file in $htmlFiles) {
    Write-Host "Processing: $($file.Name)" -ForegroundColor Cyan
    
    $content = Get-Content $file.FullName -Raw -Encoding UTF8
    
    # Applica trasformazioni
    $content = Update-BodyTag -content $content
    $content = Apply-LamborghiniTransform -content $content
    
    # Salva il file
    Set-Content $file.FullName -Value $content -Encoding UTF8
    
    Write-Host "  ✓ Updated: $($file.Name)" -ForegroundColor Green
}

Write-Host "`nTutte le pagine sono state aggiornate con il Design Lamborghini!" -ForegroundColor Green
