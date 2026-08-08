# ADR — Architecture Decision Records

Chaque décision technique importante est documentée ici dans un fichier court : `NNN-titre.md`.

Format :

```markdown
# NNN — Titre de la décision

**Date** : YYYY-MM-DD
**Statut** : accepté | remplacé par NNN

## Contexte
Pourquoi la décision est nécessaire.

## Décision
Ce qu'on a choisi.

## Alternatives considérées
Ce qu'on a écarté et pourquoi.

## Conséquences
Ce que ça implique (dettes, contraintes, licences).
```

Décisions attendues dès la Loop 0 :
- `001-lib-osc.md` — sérialisation OSC (maison vs Rug.Osc vs autre)
- `002-midi-virtuel.md` — DryWetMIDI + loopMIDI vs teVirtualMIDI (attention aux licences)
