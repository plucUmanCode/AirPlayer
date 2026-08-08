# 003 — Découverte réseau : mDNS/DNS-SD maison

**Date** : 2026-08-08
**Statut** : accepté

## Contexte

Le casque doit trouver le compagnon sans saisie d'IP (architecture.md : `_airplayer._udp.local`, fallback IP manuelle). Il faut un annonceur côté PC et un résolveur côté Quest (Android/Unity, IL2CPP).

## Décision

Implémentation mDNS/DNS-SD minimale maison dans `shared/AirPlayer.Core/Discovery/` : encodage/décodage DNS (`MdnsMessages`), résolveur côté casque (`MdnsClient`), répondeur côté compagnon (`MdnsResponderService`). Enregistrements PTR + SRV + A, TTL 120 s, pas de compression de noms à l'émission (mais le parseur suit les pointeurs de compression pour rester robuste).

Particularité Android : les requêtes portent le bit **QU** (réponse unicast demandée) et le répondeur répond en unicast **et** en multicast. Le casque acquiert aussi un `MulticastLock` Android ; si le lock échoue, les réponses unicast passent quand même.

## Alternatives considérées

- **Makaretu.Dns.Multicast** (MIT) : complet, mais peu maintenu, et tire une grappe de dépendances dans le compagnon *et* dans Unity (où l'embarquer est pénible).
- **Zeroconf** (NuGet) : browse seulement, pas d'annonce — il aurait fallu autre chose côté PC.
- **NsdManager Android** : interop Java depuis Unity, asymétrique (rien côté PC), et réputé capricieux.
- **Broadcast UDP maison (non-mDNS)** : plus simple, mais hors spec architecture.md et invisible pour les outils mDNS standard (dns-sd, avahi) qui servent au débogage.

## Conséquences

- Zéro dépendance externe ; le parsing DNS est couvert par des tests xUnit (roundtrip, pointeurs de compression, boucles de pointeurs, paquets tronqués).
- Les deux extrémités étant à nous, seul notre sous-ensemble DNS est supporté ; un répondeur mDNS tiers exotique pourrait ne pas être parsé — sans impact, on ne cherche que notre service.
- Risque connu : cohabitation sur le port 5353 avec les stacks mDNS des OS (Windows, dnssd). `SO_REUSEADDR` est posé des deux côtés ; si un réseau/OS bloque quand même, le fallback IP manuelle couvre (CA Loop 0).
- Le service est annoncé au démarrage du compagnon (2 annonces non sollicitées) puis à chaque requête reçue.
