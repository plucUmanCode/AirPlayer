# 002 — MIDI virtuel : DryWetMIDI + loopMIDI

**Date** : 2026-08-08
**Statut** : accepté

## Contexte

Le compagnon doit exposer un port MIDI virtuel que Ableton voit comme un contrôleur physique (architecture.md). Windows n'offre aucune API native pour créer un port MIDI virtuel ; il faut soit un driver tiers, soit un SDK propriétaire. La décision est requise en Loop 0 (l'intégration effective arrive en Loop 1).

## Décision

**DryWetMIDI** (lib C#, licence MIT) pour l'envoi MIDI, vers un port créé par **loopMIDI** (Tobias Erichsen, freeware) que l'utilisateur installe lui-même.

## Alternatives considérées

- **teVirtualMIDI SDK** (même auteur que loopMIDI) : crée le port par programme — meilleure UX (pas d'installation séparée) — mais la licence du SDK exige un accord écrit pour toute distribution. Incompatible avec un MVP open/sideload sans négociation.
- **virtualMIDI driver direct** : mêmes contraintes de licence que le SDK.
- **NAudio.Midi** : MIT aussi, mais l'API MIDI y est secondaire ; DryWetMIDI est dédiée MIDI, activement maintenue, mieux documentée.

## Conséquences

- Dépendance NuGet `Melanchall.DryWetMidi` à ajouter en Loop 1 (MIT, compatible).
- L'utilisateur installe loopMIDI et crée un port « AirPlayer » : étape d'onboarding à documenter (Loop 5). loopMIDI est gratuit pour usage privé/non commercial — correct pour le MVP sideload ; **à revisiter avant toute distribution commerciale** (licence commerciale teVirtualMIDI ou autre approche).
- L'abstraction côté code (`IMidiOut` prévue en Loop 1) isole ce choix : changer de backend MIDI ne touchera pas le reste du compagnon.
