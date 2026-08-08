# Loop 0 — Checklist de validation manuelle

Prérequis : `docs/loop0-unity-setup.md` déroulé (APK installé, compagnon lancé), casque et PC sur le même réseau Wi-Fi (idéalement 5 GHz, même routeur).

## CA 1 — L'app démarre en passthrough

- [ ] `adb install -r AirPlayer.apk` réussit.
- [ ] L'app se lance sur le Quest 3S et affiche le **passthrough** (tu vois ta pièce).
- [ ] Le hand tracking est actif : tes mains sont détectées sans manettes.
- [ ] Le cube grabbable peut être saisi (pinch), déplacé et relâché.

## CA 2 — Découverte et connexion en < 5 s

- [ ] Compagnon lancé (`dotnet run --project src/AirPlayer.Companion`) **avant** l'app casque.
- [ ] Au lancement de l'app, le HUD passe de « Recherche du compagnon… » à « Connecté : AirPlayer Companion (…) » en **moins de 5 secondes**.
- [ ] Côté PC, le log affiche `Headset connected: 'Quest 3S' @ <ip du casque>`.

## CA 3 — État de connexion et latence affichés

- [ ] Le HUD affiche « Connecté » en vert avec le nom du compagnon.
- [ ] Après ~10 s, la latence affichée est une **moyenne sur 10 pings** (le HUD indique « moy. 10 pings »).
- [ ] Ordre de grandeur attendu en Wi-Fi 5 GHz local : 2–15 ms. Noter la valeur : ______ ms.

## CA 4 — Détection de déconnexion en < 4 s

- [ ] Couper le compagnon (Ctrl+C) pendant que le casque est connecté.
- [ ] Le HUD passe à « Déconnecté — connexion à … » en **moins de 4 secondes** (chronomètre en main).
- [ ] Relancer le compagnon : le casque se **reconnecte tout seul** (hello répété + nouvelle annonce mDNS) et le HUD repasse au vert.

## CA 5 — Tests unitaires verts

- [ ] `cd companion && dotnet test` : tous les tests passent (OSC roundtrip/malformés, machine à états de connexion, moteur compagnon, parsing mDNS).

## Tests complémentaires

- [ ] **Reconnexion après veille** : mettre le casque en veille 30 s, le réveiller → reconnexion automatique.
- [ ] **IP manuelle** : couper le mDNS (ou décocher `autoConnectToFirst` et vider la découverte), renseigner l'IP du PC dans `ManualIpConnect` et déclencher `Connect()` → la connexion s'établit sans mDNS.
- [ ] **Deux lancements successifs** de l'app casque → le compagnon accepte la nouvelle session (le hello ré-enregistre le client).

## Résultats

| Critère | OK / KO | Notes |
|---|---|---|
| CA 1 passthrough + hand tracking | | |
| CA 2 découverte < 5 s | | |
| CA 3 latence moyenne (valeur) | | |
| CA 4 déconnexion < 4 s | | |
| CA 5 dotnet test | | |

Une fois tout coché : mettre à jour la section « État actuel » de `CLAUDE.md` (cocher Loop 0, noter les écarts) et seulement ensuite attaquer la Loop 1.
