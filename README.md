# Vođeni trening

Repozitorijum sadrži dva .NET 10 projekta:

- `VodjeniTrening.csproj` — .NET MAUI aplikacija za iOS i Android.
- `VodjeniTreningApi/VodjeniTreningApi.csproj` — ASP.NET Core API koji sluša na portu 8081.

MAUI aplikacija se pri pokretanju prijavljuje na API, dobija privremeni Bearer token,
a zatim preuzima aktuelni trening preko `GetTraining` endpointa.

## API endpointi

### `POST /login`

Zahtev:

```json
{
  "userName": "vodjenitrening",
  "userPassword": "PromeniMe-8081!"
}
```

Uspešan odgovor sadrži token koji važi 12 sati:

```json
{
  "token": "...",
  "expiresAtUtc": "..."
}
```

### `GET /GetTraining`

Zahteva zaglavlje `Authorization: Bearer TOKEN` i vraća sadržaj fajla
`VodjeniTreningApi/Data/vezbe.json`.

### `GET /health`

Javna provera da li je servis pokrenut.

## Pokretanje API-ja

```bash
dotnet run --project VodjeniTreningApi/VodjeniTreningApi.csproj
```

API sluša na `http://0.0.0.0:8081`. Za objavljivanje:

```bash
dotnet publish VodjeniTreningApi/VodjeniTreningApi.csproj -c Release -o publish/VodjeniTreningApi
```

Na server treba kopirati sadržaj publish direktorijuma i pokrenuti:

```bash
dotnet VodjeniTreningApi.dll
```

Port 8081 mora biti dozvoljen u firewall-u servera. Pre javnog puštanja promenite
korisničko ime i lozinku pomoću server environment promenljivih:

```text
Authentication__UserName
Authentication__UserPassword
```

Iste vrednosti za ovu početnu verziju moraju biti upisane u `ApiSettings` u
`Services/WorkoutService.cs`, pa aplikacija mora biti ponovo build-ovana.

## Pokretanje MAUI aplikacije

1. Otvorite `VodjeniTrening.csproj` ili ceo direktorijum u JetBrains Rider-u.
2. Sačekajte da Rider završi restore.
3. Izaberite iOS Simulator na Mac-u ili Android uređaj/emulator na Windows-u.
4. Pokrenite konfiguraciju `VodjeniTrening` preko **Run** ili **Debug**.

Na Mac-u projekat automatski koristi `net10.0-ios`, a na Windows-u
`net10.0-android`.

## Izmena treninga

Aktuelni trening nalazi se samo u:

```text
VodjeniTreningApi/Data/vezbe.json
```

Svaki objekat u `blokovi` predstavlja blok. Vežba tipa `vreme` koristi
`trajanjeSekundi`, a vežba tipa `ponavljanja` koristi `brojPonavljanja` i
`trajanjePonavljanjaSekundi`. Sve vrednosti trajanja su u sekundama.

## Bezbednost

Trenutna javna adresa `http://85.215.209.4:8081/` koristi HTTP, pa se login podaci
i token prenose bez enkripcije. Android i iOS su privremeno podešeni da dozvole
ovu konekciju. Pre produkcijske upotrebe treba postaviti HTTPS reverse proxy i
vratiti zabranu nešifrovanog saobraćaja u mobilnoj aplikaciji.

## Platforme

- iOS minimum je 15.0, što je podržani minimum korišćenog .NET MAUI 10 šablona.
- Android minimum je API 21 (Android 5.0).

iOS 13 nije podržan ovim instaliranim MAUI 10 toolchain-om. Za taj zahtev potreban
je zasebno održavan stariji toolchain/framework ili promena minimalne verzije uređaja.
