# AirPlayer — Contrôleur MIDI en réalité mixte pour Ableton Live

> Nom de travail. À valider.

Contrôleur MIDI en réalité mixte pour Meta Quest 3S, piloté au **hand tracking** (sans manettes). L'utilisateur voit son vrai studio en passthrough et interagit avec des pads, faders et une grille de session view flottants pour contrôler Ableton Live sur son PC, via Wi-Fi.

## Contenu du package

| Fichier | Rôle |
|---|---|
| `CLAUDE.md` | Contexte et règles pour Claude Code (à placer à la racine du repo) |
| `docs/vision.md` | Vision produit, utilisateur cible, différenciation |
| `docs/architecture.md` | Architecture technique complète (Quest, compagnon PC, protocoles) |
| `docs/roadmap-loops.md` | Backlog découpé en 6 loops avec critères d'acceptation |

## Comment utiliser avec Claude Code

1. Créer le repo (monorepo recommandé : `quest-app/` + `companion/`).
2. Copier `CLAUDE.md` à la racine et `docs/` dedans.
3. Lancer Claude Code et travailler **une loop à la fois** : « Lis docs/roadmap-loops.md et implémente la Loop 0. Arrête-toi quand les critères d'acceptation sont remplis. »
4. Valider manuellement les critères de chaque loop (surtout ceux qui demandent le casque) avant de passer à la suivante.
5. Mettre à jour `CLAUDE.md` (section « État actuel ») à la fin de chaque loop.

## Décisions prises

- **Environnement** : réalité mixte (passthrough) par défaut, VR complète en option.
- **Scope MVP** : pads + faders/knobs + contrôle session view Ableton.
- **Transport** : OSC sur UDP entre Quest et app compagnon ; MIDI virtuel + AbletonOSC côté PC.
- **Stack** : Unity 6 (Quest), .NET 10 (compagnon Windows), AbletonOSC (Remote Script Python, open source).
- **Input** : hand tracking seulement pour le MVP (manettes hors scope).
