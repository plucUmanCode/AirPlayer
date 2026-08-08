# CLAUDE.md — AirPlayer (contrôleur MIDI MR pour Ableton)

## Ce qu'est ce projet

App Meta Quest 3S (Unity 6) qui affiche des contrôles musicaux en réalité mixte (pads, faders, grille session view), manipulés au hand tracking, et qui pilote Ableton Live sur un PC Windows via Wi-Fi. Une app compagnon .NET 8 tourne sur le PC : elle reçoit l'OSC du casque, le convertit en MIDI virtuel, et fait le pont bidirectionnel avec AbletonOSC pour l'état du session view.

Lire `docs/vision.md` et `docs/architecture.md` avant tout travail. Le backlog est dans `docs/roadmap-loops.md` — travailler une loop à la fois, ne jamais commencer une loop suivante sans validation humaine de la précédente.

## Structure du repo

```
quest-app/        # Projet Unity 6 (C#)
companion/        # App .NET 8 (C#) — console d'abord, tray app plus tard
  src/
  tests/
shared/
  AirPlayer.Core/ # C# pur partagé (OSC, protocole, machine à états, mDNS) —
                  # package local Unity + sources compilées dans le compagnon
docs/             # Vision, architecture, roadmap
```

## Règles techniques

### Général
- Langue du code et des commentaires : anglais. Documentation utilisateur : français.
- Commits atomiques, messages en anglais, format `feat:`, `fix:`, `docs:`, `test:`.
- Ne pas ajouter de dépendance sans la justifier dans le commit.

### Quest / Unity
- Unity 6 LTS, build target Android (Quest), OpenXR + Meta XR All-in-One SDK.
- Hand tracking via Meta Interaction SDK : **poke** pour les pads, **pinch-grab** pour les faders/knobs.
- Passthrough activé par défaut (mode MR) ; toggle vers skybox VR.
- Viser 72 fps minimum sur Quest 3S. Pas d'allocation par frame dans les boucles chaudes (input, réseau).
- Le réseau (envoi OSC) roule sur un thread dédié, jamais sur le main thread Unity.
- Toute logique non-Unity (sérialisation OSC, modèle de layout, machine à états de connexion) va dans des classes pures C# testables sans le casque.

### Compagnon .NET
- .NET 8, Windows d'abord (macOS plus tard si demandé).
- MIDI virtuel : teVirtualMIDI (via wrapper) ou DryWetMIDI + loopMIDI. Vérifier les licences avant d'intégrer.
- OSC : implémentation légère maison ou lib éprouvée (ex. Rug.Osc) — justifier le choix.
- Tests unitaires xUnit sur : parsing OSC, mapping OSC→MIDI, machine à états de connexion.

### Protocole (voir architecture.md pour le détail)
- Quest → compagnon : OSC sur UDP, port 9000.
- Compagnon → Quest : OSC sur UDP, port 9001 (états session view, heartbeat).
- Compagnon ↔ AbletonOSC : localhost UDP 11000 (envoi) / 11001 (réception).
- Découverte : mDNS (`_airplayer._udp.local`), avec fallback IP manuelle dans l'UI du casque.

## Ce que Claude Code peut tester seul vs pas

- **Testable seul** : compagnon .NET au complet (unit tests + simulateur de messages OSC), classes pures C# du projet Unity, compilation du projet Unity en batch mode si dispo.
- **Non testable seul** : tout ce qui demande le casque (hand tracking, passthrough, latence perçue, confort). Pour ces critères, implémenter, puis produire une **checklist de test manuel** claire à la fin de la loop et s'arrêter.

## État actuel

- [ ] Loop 0 — Fondations — **code livré, validation humaine en attente** (`docs/loop0-checklist.md`). Fait : core partagé `shared/AirPlayer.Core` (OSC maison, protocole v1, machine à états de connexion, mDNS maison), compagnon .NET 8 (handshake, heartbeat, annonce mDNS), scripts Unity (transport threadé, découverte, HUD, IP manuelle), ADRs 001–003, tests xUnit. Reste : assembler la scène dans l'éditeur (`docs/loop0-unity-setup.md`), builder l'APK, dérouler la checklist, exécuter `dotnet test` sur le PC (impossible dans l'environnement distant, réseau NuGet bloqué). Écarts : ajout de `/airplayer/incompatible` au protocole ; mDNS implémenté maison plutôt qu'une lib (ADR 003) ; réponses mDNS unicast (bit QU) pour Android.
- [ ] Loop 1 — Pads → notes MIDI
- [ ] Loop 2 — Faders/knobs → CC
- [ ] Loop 3 — Session view
- [ ] Loop 4 — MR/VR + persistance spatiale
- [ ] Loop 5 — Polish MVP

_Mettre à jour cette section à la fin de chaque loop : cocher, noter les écarts par rapport au plan et les dettes techniques assumées._
