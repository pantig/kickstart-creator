# kickstart-creator

Webowy generator kickstartu RHEL 9 (CIS Level 2, minimal install, headless) w
.NET 10. **Nie dotyka i nie remasteruje licencjonowanego ISO RHEL.** Zamiast
tego generuje mały, dodatkowy nosnik z etykieta `OEMDRV` (ISO9660 + FAT
`.img`), ktory Anaconda wykrywa automatycznie i z ktorego czyta `ks.cfg` -
dolaczasz go jako DRUGI nosnik obok niezmienionego, oficjalnego ISO RHEL.

Bazowy szablon kickstartu: `RH9_KS.CFG` w katalogu glownym repo (dokument
referencyjny - nie jest uzywany w runtime; wersja uzywana przez aplikacje to
`src/KickstartCreator.Web/Templates/rhel9.ks.sbn`).

## Co generator robi

1. Formularz zbiera parametry wdrozenia: wybor dysku, siec (DHCP/static),
   dozwolone zrodlowe CIDR dla SSH, port SSH, dane konta administracyjnego,
   hasla (root/user/GRUB - recznie albo auto-generowane 16-znakowe), strefe
   czasowa, serwery NTP.

   **Wybor dysku instalacyjnego** ma dwa tryby:
   - **Interaktywnie podczas instalacji (domyslnie)** - w heterogenicznym
     srodowisku kolejnosc `/dev/sda`/`/dev/sdb`/LUN-ow/RAID nie jest
     przewidywalna, wiec zamiast zgadywac, wygenerowany kickstart pokazuje w
     `%pre` liste dostepnych dyskow na konsoli (z ostrzezeniem przy dyskach,
     ktore juz maja partycje) i czeka, az operator wpisze numer. Partycjonowanie
     (ten sam schemat CIS co w trybie recznym) generuje sie dynamicznie przez
     `%include`. **Wymaga kogos przy konsoli/wirtualnym KVM w momencie
     instalacji - nie nadaje sie do w pelni bezobslugowego PXE.**
   - **Podaj z gory (`by-id`)** - dysk wpisany w formularzu trafia od razu do
     `ignoredisk`/`part`/`part pv.01` (4 miejsca, podstawiane automatycznie z
     jednego pola). Instalacja w pelni automatyczna/bezobslugowa, ale wymaga,
     zeby operator znal poprawny identyfikator `by-id` z gory.
2. Hasla sa hashowane server-side: `rootpw`/`user --password` przez
   `openssl passwd -6 -stdin` (SHA-512 crypt), haslo GRUB przez natywny
   PBKDF2-HMAC-SHA512 w .NET (`Rfc2898DeriveBytes`) w formacie zgodnym z
   `grub2-mkpasswd-pbkdf2`. Plaintext nigdy nie trafia do pliku ani do loga.
3. Auto-generowane hasla sa pokazywane **raz** na stronie wyniku, przed
   pobraniem czegokolwiek - po pierwszym pobraniu jakiegokolwiek pliku dla
   danego generowania sa trwale czyszczone z pamieci serwera.
4. Szablon (Scriban) renderuje finalny `ks.cfg`.
5. `xorriso` buduje `OEMDRV.iso`, `mkfs.vfat`/`mtools` buduja `OEMDRV.img` -
   oba zawieraja tylko `ks.cfg`.
6. Pliki trafiaja na docker volume z automatycznym sprzataniem po
   `ArtifactRetention:Hours` (domyslnie 24h).

Po pierwszym zalogowaniu do zainstalowanego systemu wyswietla sie kolorowy
raport (generowany raz, przy pierwszym boocie): realny wynik `oscap xccdf
eval` dla profilu CIS oraz nasza wlasna checklista hardeningu (port SSH +
kontekst SELinux, firewalld, hidepid, banery, blokada roota, itd.).

## Uruchomienie (Docker)

### Wymagania

- Docker Engine z wtyczka Compose v2 (polecenie `docker compose`, nie starsze
  `docker-compose`). Nic wiecej - .NET SDK ani ktokolwiek z
  openssl/xorriso/mtools nie musza byc zainstalowane na hoscie, wszystko jest
  w obrazie kontenera.

### Start

Z katalogu glownego repo (tam, gdzie lezy `docker-compose.yml`):

```bash
docker compose up --build
```

Pierwsze uruchomienie zbuduje obraz (SDK etap kompiluje aplikacje, runtime
etap instaluje `xorriso`/`dosfstools`/`mtools`/`openssl`/`tzdata`) i wystartuje
kontener. Aplikacja bedzie dostepna pod `http://localhost:8080`.

Aby uruchomic w tle:

```bash
docker compose up --build -d
```

Sprawdzenie stanu / logow:

```bash
docker compose ps
docker compose logs -f kickstart-creator
```

Healthcheck kontenera odpytuje `GET /healthz` co 30s (widoczny w `docker
compose ps` jako `healthy`/`unhealthy`).

Zatrzymanie:

```bash
docker compose down
```

`docker compose down` **nie** kasuje wygenerowanych plikow - te siedza na
named volume `kickstart-artifacts` i przetrwaja restart kontenera (a i tak
zostana automatycznie posprzatane po uplywie okna retencji - domyslnie 24h).
Aby usunac rowniez ten volume: `docker compose down -v`.

### Konfiguracja

Wszystko konfigurowalne przez zmienne srodowiskowe w `docker-compose.yml`
(mapuja sie na `appsettings.json` przez konwencje `Sekcja__Klucz`):

| Zmienna | Domyslnie | Znaczenie |
|---|---|---|
| `ArtifactRetention__Hours` | `24` | Po ilu godzinach usuwac wygenerowane pliki |
| `ArtifactRetention__SweepIntervalMinutes` | `15` | Co ile sprawdzac, czy sa przeterminowane pliki |
| `ArtifactStorage__RootPath` | `/data/artifacts` | Gdzie w kontenerze zapisywac artefakty (montowane jako volume) |
| `GrubPbkdf2__Iterations` | `10000` | Liczba iteracji PBKDF2 dla hasha GRUB |
| `PasswordPolicy__GeneratedLength` | `16` | Dlugosc auto-generowanych hasel |
| `BasicAuth__Enabled` | `false` | Wlacza wbudowana bramke HTTP Basic Auth |
| `BASIC_AUTH_USER` / `BASIC_AUTH_PASSWORD` | - | Dane logowania gdy `BasicAuth__Enabled=true` |

Domyslnie brak logowania - narzedzie wewnetrzne, zalecane postawienie za
reverse proxy (Traefik/Caddy/nginx) z wlasnym auth/SSO. Wbudowany Basic Auth
to prosta bramka, nie system kont. Zeby go wlaczyc, odkomentuj w
`docker-compose.yml`:

```yaml
    environment:
      BasicAuth__Enabled: "true"
      BASIC_AUTH_USER: "admin"
      BASIC_AUTH_PASSWORD: "change-me"
```

i zrestartuj: `docker compose up -d`.

### Zmiana portu

Domyslnie `8080:8080` (host:kontener). Zeby wystawic na innym porcie hosta,
zmien lewa strone w `docker-compose.yml`, np. `"9090:8080"`.

### Podglad wygenerowanych plikow na hoscie

Pliki zyja wylacznie w named volume, nie w katalogu repo. Podejrzenie ich bez
wychodzenia z kontenera:

```bash
docker compose exec kickstart-creator ls -la /data/artifacts
```

## Rozwoj lokalny (bez Dockera)

Wymaga .NET 10 SDK oraz w PATH: `openssl`, `xorriso`, `mkfs.vfat`
(dosfstools), `mcopy` (mtools) - to samo, co instaluje `Dockerfile`.

```bash
dotnet build
dotnet test
dotnet run --project src/KickstartCreator.Web
```

## Struktura repo

```
RH9_KS.CFG                          # rekomendacja/dokument referencyjny (nieuzywany w runtime)
Dockerfile / docker-compose.yml
src/KickstartCreator.Web/           # aplikacja (Razor Pages + Minimal API)
  Templates/rhel9.ks.sbn            # szablon kickstartu (Scriban)
  Templates/_post-install-summary.sbn  # partial: first-boot CIS/best-practices summary
  Models/ Validation/ Services/ Endpoints/ Auth/ Pages/
tests/KickstartCreator.Tests/       # testy jednostkowe (xUnit)
```

## Znane ograniczenia / do zrobienia

- Kod powstal w srodowisku bez lokalnego .NET SDK i bez Dockera - **nigdy nie
  zostal skompilowany ani uruchomiony**. Wersje pakietow NuGet (Scriban,
  Microsoft.NET.Test.Sdk, xunit, xunit.runner.visualstudio, coverlet.collector)
  zostaly zweryfikowane jako realnie istniejace najnowsze stabilne wydania na
  NuGet.org (nie zgadywane), a kluczowe API Scriban uzywane w
  `ScribanKickstartTemplateRenderer` (`ITemplateLoader`, `ScriptObject.Import`,
  `Template.Parse/Render`, `TemplateContext`) zostalo zweryfikowane w
  zrodlach biblioteki - ale samego builda nie da sie zastapic tym przegladem.
  Pierwsze `docker compose up --build` (lub `dotnet build`) moze wciaz ujawnic
  drobne bledy kompilacji do poprawienia.
- Golden test dla hasha GRUB PBKDF2 sprawdza tylko *format* wyjscia (zgodny z
  `grub2-mkpasswd-pbkdf2`) i determinizm, nie porownuje z realnym
  wywolaniem `grub2-mkpasswd-pbkdf2` (niedostepnym w srodowisku, w ktorym to
  powstalo) - warto dopisac prawdziwy golden vector przed produkcyjnym uzyciem.
- Manualny smoke test (realna instalacja z `qemu-kvm`/BMC + obserwacja logow
  Anacondy) nie zostal wykonany - patrz sekcja Weryfikacja w planie
  (`~/.claude/plans/resilient-booping-waffle.md` w tej sesji).
