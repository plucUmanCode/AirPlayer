# Architecture — AirPlayer

## Vue d'ensemble

```
┌─────────────────────────┐         Wi-Fi (UDP)          ┌──────────────────────────────┐
│  QUEST 3S (Unity 6)     │                              │  PC WINDOWS                  │
│                         │  OSC :9000 (notes, CC,       │                              │
│  Hand tracking          │ ───────── commandes) ──────► │  ┌────────────────────────┐  │
│  (Interaction SDK)      │                              │  │ Compagnon .NET 10      │  │
│        │                │  OSC :9001 (états session,   │  │                        │  │
│  Modules UI 3D          │ ◄──────── heartbeat) ─────── │  │  OSC ⇄ MIDI virtuel ───┼──┼─► loopMIDI ─► Ableton
│  (pads, faders,         │                              │  │  OSC ⇄ AbletonOSC      │  │              (input MIDI)
│   grille session)       │                              │  └───────────┬────────────┘  │
│        │                │                              │              │ localhost UDP │
│  Client réseau          │                              │              │ 11000/11001   │
│  (thread dédié)         │                              │  ┌───────────▼────────────┐  │
└─────────────────────────┘                              │  │ AbletonOSC             │  │
                                                         │  │ (Remote Script Python  │  │
                                                         │  │  dans Ableton Live)    │  │
                                                         │  └────────────────────────┘  │
                                                         └──────────────────────────────┘
```

Trois processus, deux machines :

1. **App Quest (Unity 6)** — rendu MR, hand tracking, envoi/réception OSC.
2. **Compagnon (.NET 10)** — hub sur le PC. Convertit l'OSC du casque en MIDI virtuel (notes/CC) et relaie l'état du session view depuis AbletonOSC vers le casque.
3. **AbletonOSC** — Remote Script open source installé dans Ableton Live. Expose le Live Object Model en OSC sur localhost (clips, tracks, scènes, états de lecture).

## Pourquoi ce design

- **OSC/UDP plutôt que WebSocket** : pas de handshake TCP, pas de head-of-line blocking. Une note perdue vaut mieux qu'une note en retard. Les messages critiques (note-off !) ont un mécanisme de redondance (voir Fiabilité).
- **Compagnon comme hub unique** : le casque a une seule connexion à gérer. AbletonOSC reste sur localhost (pas besoin de l'exposer au réseau). La conversion MIDI se fait là où les ports MIDI virtuels existent.
- **MIDI virtuel pour notes/CC** : Ableton voit AirPlayer comme n'importe quel contrôleur physique. MIDI learn, mapping natif — tout marche sans code custom côté Ableton.
- **AbletonOSC pour le session view seulement** : le MIDI ne transporte pas l'état des clips (couleurs, lecture). AbletonOSC comble exactement ce trou.

## Budget latence (cible < 30 ms doigt→son)

| Étape | Budget |
|---|---|
| Détection hand tracking (Quest) | ~10 ms (incompressible, dépend du runtime Meta) |
| Logique app + sérialisation OSC | < 1 ms |
| Wi-Fi UDP (réseau local 5 GHz) | 2–8 ms |
| Compagnon : OSC → MIDI | < 1 ms |
| Ableton : MIDI in → audio out | 5–10 ms (buffer audio de l'utilisateur) |

Conditions requises côté utilisateur : Wi-Fi 5 GHz, idéalement casque et PC sur le même routeur (documenter la config recommandée). Mesurer réellement en Loop 1 avant d'optimiser.

Mitigation de la latence de détection : déclencher la note sur **prédiction de traversée** du plan du pad (vélocité du doigt) plutôt que sur contact confirmé, si les mesures montrent que c'est nécessaire.

## Protocole applicatif (OSC)

### Casque → compagnon (:9000)

```
/airplayer/note        (int channel, int note, int velocity)   # velocity 0 = note off
/airplayer/cc          (int channel, int cc, int value)
/airplayer/clip/fire   (int track, int scene)
/airplayer/clip/stop   (int track, int scene)
/airplayer/scene/fire  (int scene)
/airplayer/hello       (string deviceName, int protocolVersion)
/airplayer/ping        (int seq)
```

### Compagnon → casque (:9001)

```
/airplayer/welcome        (string companionVersion, bool abletonConnected)
/airplayer/incompatible   (int requiredVersion)   # réponse à un hello de version incompatible
/airplayer/pong           (int seq)
/airplayer/session/grid   (int tracks, int scenes)                    # dimensions
/airplayer/clip/state     (int track, int scene, int state, int rgb)  # state: 0=vide 1=arrêté 2=en lecture 3=déclenché
/airplayer/track/name     (int track, string name)
```

Versionner le protocole dès le jour 1 (`protocolVersion` dans le hello). Le compagnon refuse poliment un casque de version incompatible.

### Fiabilité sur UDP

- **Note-off** : envoyé 2 fois (rafale espacée de ~5 ms). Le compagnon déduplique par (channel, note) dans une fenêtre de 20 ms. Un note-off perdu = note qui sonne à l'infini, inacceptable.
- **Heartbeat** : ping/pong chaque seconde. 3 pongs manqués → le casque affiche « déconnecté » et le compagnon envoie all-notes-off au port MIDI.
- **États de clips** : le compagnon renvoie l'état complet de la grille à la connexion et sur demande (`/airplayer/session/sync`), puis en delta.

## Découverte réseau

mDNS : le compagnon annonce `_airplayer._udp.local`. Le casque liste les compagnons trouvés. Fallback : saisie IP manuelle (les réseaux qui bloquent le multicast existent).

## Stack et dépendances

### Quest
- Unity 6 LTS, URP, build Android
- Meta XR All-in-One SDK (Interaction SDK pour poke/pinch, Passthrough API)
- OSC : sérialisation maison (le format OSC est simple) ou lib légère — décision en Loop 0

### Compagnon
- .NET 10, console d'abord (tray app Windows en Loop 5)
- MIDI virtuel : DryWetMIDI + loopMIDI installé par l'utilisateur, OU teVirtualMIDI (port créé par programme, mais licence à vérifier pour distribution). Décision en Loop 0 avec justification écrite.
- OSC : Rug.Osc ou maison

### Ableton
- AbletonOSC (github.com/ideoforms/AbletonOSC), installé comme Remote Script. Documenter l'installation pour l'utilisateur.

## Modèle de données (casque)

Un **Layout** = liste de **Modules** placés dans l'espace (position/rotation/échelle relatives à un ancrage spatial).

Modules du MVP :
- `PadGrid` (n×m pads, note de départ, canal, couleur)
- `FaderBank` (n faders, CC de départ, canal)
- `KnobBank` (n knobs, CC de départ, canal)
- `SessionGrid` (fenêtre t×s sur la grille Ableton, avec offset scrollable)

Sérialisation JSON, sauvegarde locale sur le casque. Les layouts sont versionnés (champ `schemaVersion`).

## Risques techniques identifiés

| Risque | Impact | Mitigation |
|---|---|---|
| Latence/jitter hand tracking insuffisant pour du drumming serré | Fatal pour les pads | Mesurer tôt (Loop 1). Prédiction de traversée. Si vraiment insuffisant : repositionner les pads comme triggers de clips/one-shots plutôt que finger drumming de précision |
| Frappe dans le vide sans retour haptique = imprécis | UX dégradée | Feedback visuel fort (enfoncement, flash) + audio de confirmation optionnel ; pads volontairement larges |
| Wi-Fi du user pourri | Latence variable | Doc de setup (5 GHz, même routeur) + affichage latence mesurée dans l'app |
| AbletonOSC change ou casse avec une version de Live | Session view mort | Isoler derrière une interface `ISessionBackend` dans le compagnon ; le MIDI pur continue de marcher sans |
| Occlusion des mains (une main cache l'autre) | Notes ratées | Layouts recommandés qui écartent les zones de jeu |
