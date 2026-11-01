# Vođeni trening

.NET MAUI aplikacija za iOS i Android. Korisnik može da kreira nalog i da se
prijavi na `VodjeniTreningApi`. Nakon prijave aplikacija dobija privremeni Bearer
token i preuzima trening dodeljen tom korisniku za današnji datum. Posle
završenog treninga korisnik može da pošalje komentar ili da ga preskoči.

Web API se razvija u zasebnom repozitorijumu:
[djakas69/VodjeniTreningApi](https://github.com/djakas69/VodjeniTreningApi).

## Pokretanje MAUI aplikacije

1. Otvorite `VodjeniTrening.csproj` u JetBrains Rider-u.
2. Sačekajte da Rider završi restore.
3. Izaberite iOS Simulator na Mac-u ili Android uređaj/emulator na Windows-u.
4. Pokrenite konfiguraciju `VodjeniTrening` preko **Run** ili **Debug**.

Na Mac-u projekat automatski koristi `net10.0-ios`, a na Windows-u
`net10.0-android`.

Adresa API-ja nalazi se u `ApiSettings` u `Services/WorkoutService.cs`.
Korisnički podaci nisu ugrađeni u mobilnu aplikaciju. Trenutna API adresa je:

```text
https://api.darkojakas.de/
```

Ako server privremeno nije dostupan, aplikacija prikazuje dugme „Pokušaj ponovo“.

## Bezbednost

Komunikacija sa API-jem koristi HTTPS. Android eksplicitno zabranjuje cleartext
saobraćaj, a iOS više nema izuzetak koji bi dozvolio nešifrovane HTTP zahteve.

## Platforme

- iOS minimum je 15.0, što je podržani minimum korišćenog .NET MAUI 10 šablona.
- Android minimum je API 21 (Android 5.0).

iOS 13 nije podržan ovim MAUI 10 toolchain-om. Za taj zahtev potreban je zasebno
održavan stariji toolchain/framework ili promena minimalne verzije uređaja.
