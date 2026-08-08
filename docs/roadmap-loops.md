# Roadmap — Loops de développement

Chaque loop est autonome, livre quelque chose de démontrable, et se termine par une validation humaine (checklist manuelle avec le casque quand requis). **Ne pas commencer une loop avant la validation de la précédente.**

Format : Objectif → Livrables → Critères d'acceptation (CA) → Tests.

---

## Loop 0 — Fondations et tuyauterie

**Objectif** : les deux projets compilent, le casque et le PC se parlent.

**Livrables**
- Monorepo initialisé : `quest-app/` (Unity 6, Meta XR SDK configuré, build APK qui se lance sur Quest), `companion/` (.NET 8 console).
- Découverte mDNS + fallback IP manuelle.
- Handshake `/airplayer/hello` → `/airplayer/welcome`, heartbeat ping/pong.
- Décisions documentées (ADR courts dans `docs/adr/`) : lib OSC, lib MIDI virtuel.
- Scène Unity minimale en passthrough avec un cube grabbable (valide que le hand tracking et le passthrough marchent).

**CA**
1. `adb install` de l'APK → l'app démarre en passthrough sur le Quest 3S.
2. Le compagnon lancé sur le PC apparaît dans la liste du casque en < 5 s ; la connexion s'établit.
3. Le casque affiche l'état de connexion et la latence aller-retour mesurée (moyenne sur 10 pings).
4. Couper le compagnon → le casque affiche « déconnecté » en < 4 s.
5. Tests xUnit verts sur : parsing/sérialisation OSC, machine à états de connexion.

**Tests manuels** : checklist fournie par Claude Code en fin de loop (connexion, déconnexion, reconnexion, IP manuelle).

---

## Loop 1 — Pads → notes MIDI ⚠️ loop de validation du concept

**Objectif** : frapper un pad du doigt joue un son dans Ableton. C'est ici qu'on découvre si le produit est viable.

**Livrables**
- Module `PadGrid` 4×4 : poke au doigt, feedback visuel (enfoncement + flash), vélocité dérivée de la vitesse du doigt.
- Envoi `/airplayer/note` avec redondance note-off ; compagnon → port MIDI virtuel.
- Grab du module (pinch à deux mains pour déplacer/redimensionner).
- Outil de mesure de latence : mode debug qui loggue timestamp casque → timestamp MIDI out compagnon.

**CA**
1. Un Drum Rack Ableton se joue au doigt ; les 16 pads déclenchent les bonnes notes (base C1, configurable en JSON pour l'instant).
2. Vélocité perceptible : frappe douce vs forte produit des vélocités distinctes (au moins 3 zones fiables).
3. Latence casque→MIDI-out mesurée et documentée dans `docs/latency-report.md`. Cible : < 15 ms sur ce segment.
4. Aucune note coincée après 10 minutes de jeu intensif.
5. Zéro allocation GC par frame dans le chemin input→réseau (vérifié au Profiler).

**Tests manuels** : session de finger drumming de 10 min ; jugement subjectif « est-ce que c'est jouable ? » documenté honnêtement. **Si la latence ou la précision est décevante, on s'arrête et on repense (prédiction de traversée, pads plus gros, pivot triggers) avant de continuer.**

---

## Loop 2 — Faders et knobs → CC

**Objectif** : mixer et moduler dans Ableton avec les mains.

**Livrables**
- `FaderBank` (pinch-drag vertical) et `KnobBank` (pinch-twist ou drag circulaire — prototyper les deux, garder le meilleur).
- Envoi `/airplayer/cc` avec throttling intelligent (max ~200 msg/s par contrôle, dernier état toujours envoyé).
- Configuration des mappings (canal, CC) via un panneau simple dans le casque.

**CA**
1. MIDI learn d'Ableton fonctionne : pincer un fader, l'assigner à un volume de piste en 2 clics côté PC.
2. Mouvement fluide sans stair-stepping audible sur un filtre balayé.
3. Le pinch ne « lâche » pas de façon intempestive pendant un drag lent (tolérance de tracking).
4. Tests xUnit sur le throttling CC.

---

## Loop 3 — Session view vivant

**Objectif** : voir et lancer les clips du projet Ableton réel.

**Livrables**
- Intégration AbletonOSC dans le compagnon derrière une interface `ISessionBackend` (+ doc d'installation du Remote Script pour l'utilisateur).
- Module `SessionGrid` : fenêtre 8×8 scrollable sur la grille de clips, couleurs réelles, états (vide/arrêté/lecture/déclenché), noms de pistes.
- Poke un clip = fire ; geste ou bouton stop par piste ; fire de scène.
- Sync complet à la connexion, deltas ensuite.

**CA**
1. Ouvrir un projet Ableton existant → la grille du casque reflète les clips (couleurs, noms) en < 2 s.
2. Lancer un clip depuis le casque → il joue, et son état visuel passe à « déclenché » puis « en lecture » en respectant la quantisation de Live.
3. Lancer un clip depuis le PC → le casque le reflète en < 500 ms.
4. Ableton fermé/rouvert → le compagnon se resynchronise sans redémarrage.
5. Tests xUnit sur `ISessionBackend` avec un backend simulé.

---

## Loop 4 — MR/VR et persistance spatiale

**Objectif** : l'app se comporte comme un vrai outil de studio, session après session.

**Livrables**
- Toggle passthrough MR ↔ environnement VR (skybox sobre).
- Ancres spatiales : les modules retrouvent leur place dans le studio au relancement.
- Sauvegarde/chargement de layouts nommés (JSON versionné).
- Layout par défaut soigné pour la première ouverture.

**CA**
1. Placer pads/faders/session grid autour de son bureau, fermer l'app, relancer → tout est à sa place (< 5 cm de dérive).
2. Le toggle MR/VR conserve les positions.
3. Trois layouts sauvegardés, rechargés, supprimés sans corruption.
4. Lisibilité validée en passthrough dans une pièce éclairée normalement ET faiblement.

---

## Loop 5 — Polish MVP

**Objectif** : utilisable par quelqu'un d'autre que le développeur.

**Livrables**
- Compagnon en tray app Windows (démarrage auto optionnel, statut, log).
- Onboarding casque : première connexion guidée (installer loopMIDI/AbletonOSC, choisir le compagnon).
- All-notes-off sur déconnexion et bouton panic.
- Doc utilisateur : setup réseau recommandé, installation, dépannage.
- Passe de performance : 72 fps stables avec les 4 modules actifs.

**CA**
1. Un utilisateur externe (Louis-Philippe ?) installe et joue en < 15 min avec seulement la doc.
2. Session d'une heure sans crash, sans note coincée, sans reconnexion manuelle.
3. Le critère de succès du MVP (vision.md) est atteint : une vraie session de production complète.

---

## Après le MVP (backlog non trié)

- Support macOS du compagnon
- Vélocité par prédiction de traversée si Loop 1 l'a jugée nécessaire mais non bloquante
- Modules additionnels : XY pad, arpégiateur visuel, mixeur 3D
- Mode DJ (l'angle marché identifié : jog wheels, crossfader, hot cues)
- Distribution Meta Store / App Lab
- MPE
