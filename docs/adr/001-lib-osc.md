# 001 — Sérialisation OSC : implémentation maison

**Date** : 2026-08-08
**Statut** : accepté

## Contexte

Le casque et le compagnon échangent tout leur trafic en OSC sur UDP. Il faut une implémentation utilisable des deux côtés : compilable par Unity 6 (netstandard 2.1, C# 9) et par .NET 10, testable en xUnit, et sans allocation dans le chemin chaud input→réseau (règle CLAUDE.md, critique pour la Loop 1).

## Décision

Implémentation maison minimale dans `shared/AirPlayer.Core/Osc/` : `OscWriter`, `OscReader`, `OscMessage`, `OscArg`. Sous-ensemble OSC 1.0 couvrant exactement le protocole AirPlayer : type tags `i`, `f`, `s`, `T`, `F`. Pas de bundles, pas de pattern matching d'adresses (nos adresses sont des constantes comparées littéralement).

## Alternatives considérées

- **Rug.Osc** : mature, mais plus maintenue depuis des années, pensée .NET Framework, API orientée objets alloués — pas de contrôle zéro-alloc, et il faudrait l'embarquer dans Unity à la main.
- **OscCore / extOSC (Unity)** : bien pour Unity, mais inutilisables côté compagnon .NET 10 → deux implémentations à maintenir, deux comportements à tester.
- **OSC via lib côté compagnon + maison côté Unity** : casse la symétrie ; un seul code partagé élimine toute une classe de bugs d'interop.

## Conséquences

- Le format OSC émis/parsé est borné à notre protocole ; tout type tag inconnu fait rejeter le paquet (sécurité par défaut).
- `OscWriter.Write(message, buffer)` écrit dans un buffer fourni par l'appelant : le chemin pads→notes de la Loop 1 pourra être zéro-alloc.
- On maintient ~300 lignes de sérialisation, couvertes par des tests de roundtrip et de rejet de paquets malformés.
- Si un besoin futur dépasse le sous-ensemble (bundles avec timestamps, par ex.), la décision sera révisée.
