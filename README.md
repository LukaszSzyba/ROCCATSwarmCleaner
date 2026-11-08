# ROCCAT Swarm Cleaner

Narzędzie konsolowe (.NET 10, Windows) do usuwania pozostałości po ROCCAT Swarm.

## Co robi

- Kończy procesy Swarm/ROCCAT.
- Zatrzymuje i usuwa usługi Swarm.
- Usuwa pliki i foldery, których nazwa zawiera `roccat` lub `swarm` (pierwszy poziom w Program Files, ProgramData, AppData, Dokumenty, Pulpit, Menu Start, Temp).
- Usuwa pasujące klucze rejestru (`HKCU\Software`, `HKLM\SOFTWARE`, `WOW6432Node`, `Services`).
- Usuwa wpisy autostartu (`Run`) i wpisy odinstalowania.

## Czego nie robi

- Nie uruchamia oficjalnego deinstalatora.
- Nie usuwa sterowników, zaplanowanych zadań, reguł zapory ani wpisów w `C:\Windows\Installer`.
- Nie przeszukuje zagnieżdżonych folderów.
- Nie tworzy kopii zapasowej.

## Ostrzeżenie

Dopasowanie po słowach `roccat` i `swarm` jest szerokie i może usunąć niepowiązane dane
(np. Docker Swarm). Używasz na własną odpowiedzialność.

## Użycie

1. Utwórz punkt przywracania systemu.
2. Odinstaluj Swarm w Ustawienia → Aplikacje.
3. Uruchom jako **Administrator** w trybie podglądu:

   ```powershell
   dotnet run --project ROCCATSwarmCleaner -- --dry-run
   ```

4. Jeśli lista jest poprawna, uruchom bez `--dry-run` i potwierdź `t`:

   ```powershell
   dotnet run --project ROCCATSwarmCleaner
   ```

5. Zrestartuj komputer i zainstaluj Swarm ponownie.

## Wymagania

- Windows
- .NET 10 SDK
- Uprawnienia administratora
