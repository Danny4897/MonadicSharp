# Setup GitHub Profile e Repository per Danny4897 (aka Klexir)

Write-Host "🚀 Setup GitHub per Danny4897 (aka Klexir)" -ForegroundColor Green
Write-Host "=================================================" -ForegroundColor Green

Write-Host ""
Write-Host "📋 CHECKLIST SETUP GITHUB:" -ForegroundColor Yellow
Write-Host ""

Write-Host "1️⃣  REPOSITORY DEL PROGETTO (MonadicSharp)" -ForegroundColor Cyan
Write-Host "   ✅ README.md già aggiornato con badge Danny4897/Klexir"
Write-Host "   📁 Percorso: $PWD\README.md"
Write-Host ""

Write-Host "2️⃣  PROFILO GITHUB PERSONALE" -ForegroundColor Cyan
Write-Host "   📝 Crea repository: https://github.com/new"
Write-Host "   📛 Nome repository: Danny4897 (uguale al tuo username)"
Write-Host "   🌐 Deve essere PUBBLICO"
Write-Host "   ✅ Spunta 'Add a README file'"
Write-Host "   📁 Contenuto README: usa PROFILE_README.md"
Write-Host ""

Write-Host "3️⃣  FILE DA UTILIZZARE" -ForegroundColor Cyan
Write-Host "   📄 Per repository progetto: README.md (già aggiornato)"
Write-Host "   📄 Per profilo personale: PROFILE_README.md"
Write-Host "   📄 Riferimento badge: BADGES_GUIDE.md"
Write-Host ""

# Controlla se i file esistono
$files = @{
    "README.md"         = "Repository del progetto MonadicSharp"
    "PROFILE_README.md" = "Profilo GitHub personale"
    "BADGES_GUIDE.md"   = "Guida ai badge"
}

Write-Host "4️⃣  VERIFICA FILE" -ForegroundColor Cyan
foreach ($file in $files.Keys) {
    if (Test-Path $file) {
        Write-Host "   ✅ $file - $($files[$file])" -ForegroundColor Green
    }
    else {
        Write-Host "   ❌ $file - MANCANTE!" -ForegroundColor Red
    }
}
Write-Host ""

Write-Host "5️⃣  AZIONI DA FARE SU GITHUB.COM" -ForegroundColor Cyan
Write-Host ""
Write-Host "   A) REPOSITORY PROGETTO (se non esiste già):" -ForegroundColor White
Write-Host "      🌐 Vai su: https://github.com/new"
Write-Host "      📛 Nome: MonadicSharp"
Write-Host "      📝 Descrizione: A modern functional programming library for C#"
Write-Host "      🌐 Pubblico: SÌ"
Write-Host "      📄 README: usa il contenuto di README.md (già aggiornato)"
Write-Host ""

Write-Host "   B) PROFILO PERSONALE:" -ForegroundColor White
Write-Host "      🌐 Vai su: https://github.com/new"
Write-Host "      📛 Nome: Danny4897 (DEVE essere uguale al tuo username!)"
Write-Host "      📝 Descrizione: Profile README"
Write-Host "      🌐 Pubblico: SÌ"
Write-Host "      ✅ Spunta: 'Add a README file'"
Write-Host "      📄 README: copia il contenuto di PROFILE_README.md"
Write-Host ""

Write-Host "6️⃣  RISULTATO FINALE" -ForegroundColor Cyan
Write-Host "   🏠 Profilo: https://github.com/Danny4897 (mostrerà il README automaticamente)"
Write-Host "   📦 Progetto: https://github.com/Danny4897/MonadicSharp"
Write-Host "   🎯 NuGet: https://www.nuget.org/profiles/Klexir"
Write-Host ""

Write-Host "7️⃣  BADGE UTILIZZATI" -ForegroundColor Cyan
Write-Host "   🔵 GitHub: Danny4897"
Write-Host "   🟠 NuGet: Klexir"
Write-Host "   📦 Package: MonadicSharp"
Write-Host ""

# Apri i file per una verifica rapida
Write-Host "8️⃣  APERTURA FILE PER VERIFICA" -ForegroundColor Cyan
$choice = Read-Host "Vuoi aprire i file per verificare il contenuto? (y/n)"
if ($choice -eq 'y' -or $choice -eq 'Y') {
    if (Test-Path "README.md") {
        Write-Host "   📖 Aprendo README.md..." -ForegroundColor Green
        Start-Process notepad "README.md"
    }
    if (Test-Path "PROFILE_README.md") {
        Write-Host "   📖 Aprendo PROFILE_README.md..." -ForegroundColor Green
        Start-Process notepad "PROFILE_README.md"
    }
}

Write-Host ""
Write-Host "🎉 SETUP COMPLETATO!" -ForegroundColor Green
Write-Host "Ora vai su GitHub e segui i passaggi sopra!" -ForegroundColor Yellow
Write-Host ""
Write-Host "💡 SUGGERIMENTO:" -ForegroundColor Magenta
Write-Host "Il profilo GitHub Danny4897 mostrerà automaticamente che pubblichi su NuGet come Klexir!" -ForegroundColor Magenta
