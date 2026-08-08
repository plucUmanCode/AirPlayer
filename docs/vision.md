# Vision — AirPlayer

## Le problème

Les contrôleurs MIDI physiques sont figés : un layout, un nombre de pads, un prix. Les contrôleurs VR existants (Modulia Studio, MoveMusic) datent de l'ère Quest 1/2 : conçus pour les manettes, isolés en VR complète, peu maintenus. Aucun n'exploite ce que le Quest 3S rend possible : **le hand tracking précis dans son vrai studio en passthrough**.

## Le produit

Un contrôleur MIDI infiniment reconfigurable qui flotte dans le studio de l'utilisateur. Il voit son écran, son clavier, son café — et par-dessus, des pads qu'il frappe du doigt, des faders qu'il pince, et la grille de clips de son projet Ableton, en vrai temps réel.

## Utilisateur cible (MVP)

Producteur bedroom/home studio qui possède déjà un Quest et Ableton Live. Il produit assis à son bureau. Le casque est un *complément* à son setup, pas un remplacement — d'où la réalité mixte par défaut.

Persona secondaire (post-MVP) : performeur live qui veut un élément visuel sur scène.

## Différenciation

1. **Hand tracking natif** — zéro manette. La friction d'entrée est quasi nulle : mettre le casque, jouer.
2. **Réalité mixte par défaut** — l'utilisateur reste dans son studio, voit son écran Ableton. Les concurrents isolent.
3. **Session view vivant** — la grille de clips reflète l'état réel du projet (couleurs, clips en lecture) grâce à AbletonOSC. Pas juste des boutons aveugles qui envoient des notes.

## Principes de design

- **La latence est la feature #1.** Un pad qui répond en retard tue le produit. Budget total cible : < 30 ms du contact du doigt au son (voir architecture.md).
- **MR d'abord.** Toute décision visuelle assume le passthrough : contrôles lisibles sur fond réel variable, pas de dépendance à un environnement contrôlé.
- **Reconfigurable, pas complexe.** L'utilisateur place et redimensionne ses modules en les grabbant. Pas de menus profonds.
- **Ableton d'abord, MIDI pour le reste.** L'intégration profonde (session view) cible Ableton. Mais pads et faders émettent du MIDI standard : ça marche avec n'importe quel DAW dès le jour 1.

## Hors scope MVP (explicitement)

- Support des manettes
- macOS pour l'app compagnon
- Autres DAW en intégration profonde (FL Studio, Bitwig)
- Mode DJ (jog wheels, crossfader) — piste d'expansion notée, pas MVP
- Multijoueur / jam collaboratif
- MPE / expressivité avancée
- Distribution store (App Lab/Meta Store) — le MVP se sideload

## Critère de succès du MVP

Pier-Luc produit une session complète d'une heure dans Ableton en utilisant AirPlayer pour les drums (pads), le mixage (faders) et le lancement de clips — et il a envie de recommencer le lendemain.
