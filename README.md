# Vođeni trening

Jednostavna one-page .NET MAUI aplikacija za iOS i Android. Trening se učitava iz
`Resources/Raw/vezbe.json`, a interfejs i odbrojavanje rade kroz MVVM state machine.

## Pokretanje u Rider-u

1. Otvorite `VodjeniTrening.csproj` ili ceo direktorijum u JetBrains Rider-u.
2. Sačekajte da Rider završi restore.
3. Izaberite iOS Simulator ili Android emulator kao run target.
4. Pokrenite konfiguraciju `VodjeniTrening` preko **Run** ili **Debug**.

Za komandnu liniju na macOS-u:

```bash
dotnet restore VodjeniTrening.csproj
dotnet build VodjeniTrening.csproj -f net10.0-ios
dotnet build VodjeniTrening.csproj -f net10.0-android -p:EnableAndroidTarget=true
```

Za Android je prvo potreban instaliran `maui-android` workload. Nakon instalacije
uključite Android target pomoću `-p:EnableAndroidTarget=true`, odnosno dodajte tu
MSBuild osobinu u Rider build konfiguraciju. Projekat ne instalira niti ažurira
razvojne alate automatski.

## Izmena treninga

Svaki objekat u `blokovi` predstavlja blok. Dodajte mu naziv, broj krugova, pauzu
između krugova, pauzu nakon bloka i najmanje jednu vežbu. Vežba tipa `vreme` koristi
`trajanjeSekundi`, a vežba tipa `ponavljanja` koristi `brojPonavljanja` i
`trajanjePonavljanjaSekundi`. Poslednje polje određuje koliko sekundi traje jedno
ponavljanje; prikaz se automatski odbrojava do nule.

Sve vrednosti trajanja su u sekundama. `pauzaNakonVezbeSekundi` važi samo kada u
istom krugu postoji sledeća vežba. Na kraju kruga koristi se
`pauzaIzmedjuKrugovaSekundi`, a na kraju bloka `pauzaNakonBlokaSekundi`.

## Platforme

- iOS minimum je 15.0, što je podržani minimum korišćenog .NET MAUI 10 šablona.
- Android minimum je API 21 (Android 5.0).

iOS 13 nije podržan ovim instaliranim MAUI 10 toolchain-om. Za taj zahtev potreban
je zasebno održavan stariji toolchain/framework ili promena minimalne verzije uređaja.

## Web Api
VodjeniTreningApi se nalazi na adresi http://85.215.209.4:8081/
