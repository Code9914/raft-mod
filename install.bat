@echo off
setlocal enabledelayedexpansion
chcp 65001 >nul 2>nul

REM ═══════════════════════════════════════════════════════════════
REM  RaftMod - Installateur automatique
REM ═══════════════════════════════════════════════════════════════
REM
REM  Ce script télécharge et installe RaftMod proprement.
REM
REM  1. Te demande le dossier d'installation de Raft
REM  2. Installe BepInEx 5.4.23.5 si absent
REM  3. Télécharge le mod depuis GitHub Releases
REM  4. Copie le .dll dans BepInEx\plugins\RaftMod\
REM
REM  Utilisation : double-clique sur install.bat
REM ═══════════════════════════════════════════════════════════════

REM ╔══════════════════════════════════════════════════╗
REM ║  CONFIGURATION — Modifie ces 2 lignes           ║
REM ╚══════════════════════════════════════════════════╝

set "GITHUB_USER=Code9914"
set "GITHUB_REPO=raft-mod"

REM ───────────────────────────────────────────
REM  Fin de la configuration
REM ───────────────────────────────────────────

set "BEPINEX_VERSION=5.4.23.5"
set "BEPINEX_URL=https://github.com/BepInEx/BepInEx/releases/download/v%BEPINEX_VERSION%/BepInEx_x64_%BEPINEX_VERSION%.zip"
set "MOD_URL=https://github.com/%GITHUB_USER%/%GITHUB_REPO%/releases/latest/download/RaftMod.dll"
set "CONFIG_FILE=%USERPROFILE%\.raftmod_install_path"

:MAIN
cls
echo.
echo  ╔══════════════════════════════════════════╗
echo  ║        RaftMod - Installateur            ║
echo  ╚══════════════════════════════════════════╝
echo.
echo  1. Installer / Mettre à jour le mod
echo  2. Modifier le dossier de Raft
echo  3. Quitter
echo.
set /p "CHOIX=Choix (1-3) : "
if "%CHOIX%"=="1" goto INSTALL
if "%CHOIX%"=="2" goto ASK_PATH
if "%CHOIX%"=="3" exit /b
goto MAIN

:ASK_PATH
cls
echo.
echo  ─── Dossier de Raft ───
echo.
echo  Indique le dossier où se trouve Raft.exe
echo.

REM Charger l'ancien chemin si existant
set "RAFT_PATH="
if exist "%CONFIG_FILE%" set /p RAFT_PATH=<"%CONFIG_FILE%"
if defined RAFT_PATH echo  Ancien chemin : !RAFT_PATH!
echo.
set /p "RAFT_PATH=Chemin complet (ex: D:\Games\Raft) : "
echo.
echo  Vérification...
if not exist "!RAFT_PATH!\Raft.exe" (
    echo  [ERREUR] Raft.exe introuvable dans ce dossier.
    pause
    goto ASK_PATH
)

echo !RAFT_PATH! > "%CONFIG_FILE%"
echo  [OK] Chemin sauvegardé.
timeout /t 2 >nul
goto MAIN

REM ══════════════════════════════════════════════════
REM  INSTALLATION
REM ══════════════════════════════════════════════════

:INSTALL
cls

REM Charger le chemin
set "RAFT_PATH="
if exist "%CONFIG_FILE%" set /p RAFT_PATH=<"%CONFIG_FILE%"
if not defined RAFT_PATH goto ASK_PATH

if not exist "!RAFT_PATH!\Raft.exe" (
    echo  [ERREUR] Chemin invalide, merci de le corriger.
    pause
    goto ASK_PATH
)

set "BEPINEX_DIR=!RAFT_PATH!\BepInEx"
set "PLUGIN_DIR=!BEPINEX_DIR!\plugins\RaftMod"

cls
echo.
echo  ╔══════════════════════════════════════════╗
echo  ║     Installation de RaftMod             ║
echo  ╚══════════════════════════════════════════╝
echo.
echo  Raft  : !RAFT_PATH!
echo  Cible : !PLUGIN_DIR!
echo.

REM ── Étape 1 : BepInEx ──
echo  [1/4] BepInEx...
if exist "!BEPINEX_DIR!\core\BepInEx.dll" (
    echo    Déjà installé, OK.
) else (
    echo    Non trouvé — téléchargement requis.
    echo.
    set /p "OKB=Continuer ? (O/N) : "
    if /i not "!OKB!"=="O" (
        echo  [ANNULÉ] BepInEx est requis.
        pause
        goto MAIN
    )

    echo    Téléchargement...
    powershell -NoProfile -ExecutionPolicy Bypass -Command "$wc = New-Object Net.WebClient; $wc.Headers['User-Agent']='RaftMod-Installer'; try { $wc.DownloadFile('%BEPINEX_URL%', '%TEMP%\BepInEx.zip'); Write-Host 'OK' } catch { Write-Host $_.Exception.Message; exit 1 }"
    if !ERRORLEVEL! neq 0 (
        echo    [ERREUR] Échec du téléchargement BepInEx.
        pause
        goto MAIN
    )

    echo    Extraction...
    powershell -NoProfile -ExecutionPolicy Bypass -Command "Add-Type -AssemblyName IO.Compression.FileSystem; [IO.Compression.ZipFile]::ExtractToDirectory('%TEMP%\BepInEx.zip', '!RAFT_PATH!')"
    if !ERRORLEVEL! neq 0 (
        echo    [ERREUR] Échec de l'extraction.
        pause
        goto MAIN
    )
    del "%TEMP%\BepInEx.zip" 2>nul
    echo    [OK] BepInEx installé
)
echo.

REM ── Étape 2 : Dossier plugin ──
echo  [2/4] Dossier plugin...
if not exist "!PLUGIN_DIR!" mkdir "!PLUGIN_DIR!"
echo    [OK] !PLUGIN_DIR!
echo.

REM ── Étape 3 : Téléchargement du mod ──
echo  [3/4] Téléchargement du mod...
echo    Source : %MOD_URL%
echo.
powershell -NoProfile -ExecutionPolicy Bypass -Command "$wc = New-Object Net.WebClient; $wc.Headers['User-Agent']='RaftMod-Installer'; try { $wc.DownloadFile('%MOD_URL%', '%TEMP%\RaftMod.dll'); Write-Host 'OK' } catch { Write-Host $_.Exception.Message; exit 1 }"
if !ERRORLEVEL! neq 0 (
    echo    [ERREUR] Échec du téléchargement.
    echo.
    echo    Vérifie que l'URL est correcte :
    echo    %MOD_URL%
    pause
    goto MAIN
)

copy /Y "%TEMP%\RaftMod.dll" "!PLUGIN_DIR!\RaftMod.dll" >nul
del "%TEMP%\RaftMod.dll" 2>nul
echo    [OK] Mod téléchargé et installé
echo.

REM ── Étape 4 : Vérification ──
echo  [4/4] Vérification...
if exist "!PLUGIN_DIR!\RaftMod.dll" (
    for %%F in ("!PLUGIN_DIR!\RaftMod.dll") do echo    [OK] RaftMod.dll (%%~zF octets)
    echo.
    echo  ╔══════════════════════════════════════════╗
    echo  ║      Installation terminée !             ║
    echo  ╚══════════════════════════════════════════╝
    echo.
    echo  Lance le jeu et appuie sur F5 pour ouvrir le menu.
) else (
    echo    [ERREUR] Fichier introuvable après copie.
)

echo.
pause
goto MAIN
