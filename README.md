# Vođeni trening

.NET MAUI aplikacija za iOS i Android. Pri pokretanju se prijavljuje na
`VodjeniTreningApi`, dobija privremeni Bearer token i preuzima aktuelni trening
preko `GetTraining` endpointa.

Web API se razvija u zasebnom repozitorijumu:
[djakas69/VodjeniTreningApi](https://github.com/djakas69/VodjeniTreningApi).

## Pokretanje MAUI aplikacije

1. Otvorite `VodjeniTrening.csproj` u JetBrains Rider-u.
2. Sačekajte da Rider završi restore.
3. Izaberite iOS Simulator na Mac-u ili Android uređaj/emulator na Windows-u.
4. Pokrenite konfiguraciju `VodjeniTrening` preko **Run** ili **Debug**.

Na Mac-u projekat automatski koristi `net10.0-ios`, a na Windows-u
`net10.0-android`.

Adresa API-ja i početni pristupni podaci nalaze se u `ApiSettings` u
`Services/WorkoutService.cs`. Trenutna API adresa je:

```text
http://85.215.209.4:8081/
```

Ako server privremeno nije dostupan, aplikacija prikazuje dugme „Pokušaj ponovo“.

## Bezbednost

Trenutna javna adresa koristi HTTP, pa se login podaci i token prenose bez
enkripcije. Android i iOS su privremeno podešeni da dozvole ovu konekciju.
Pre produkcijske upotrebe treba postaviti HTTPS reverse proxy i vratiti zabranu
nešifrovanog saobraćaja u mobilnoj aplikaciji.

## Platforme

- iOS minimum je 15.0, što je podržani minimum korišćenog .NET MAUI 10 šablona.
- Android minimum je API 21 (Android 5.0).

iOS 13 nije podržan ovim MAUI 10 toolchain-om. Za taj zahtev potreban je zasebno
održavan stariji toolchain/framework ili promena minimalne verzije uređaja.
