# Loop 0 — Tester l'app Unity

Prérequis : la scène est assemblée (`docs/loop0-unity-setup.md`) et le compagnon tourne sur le PC (`dotnet run --project src/AirPlayer.Companion` dans `companion/`).

Il y a **trois niveaux de test**, du plus rapide au plus complet. Fais-les dans l'ordre : chaque niveau valide une couche de plus, et un problème attrapé au niveau 1 se débogue bien plus vite que sur le casque.

---

## Niveau 1 — Play Mode dans l'éditeur (5 min, sans casque)

Tout le code réseau (découverte mDNS, handshake, heartbeat, HUD) est du C# pur : il tourne dans l'éditeur, sur le même PC que le compagnon. C'est le moyen le plus rapide de valider la tuyauterie complète.

1. Lance le compagnon dans un terminal et laisse-le tourner.
2. Dans Unity, ouvre la scène `Main` et appuie sur **Play**.
3. Regarde la **Console Unity** :
   ```
   [AirPlayer] Companion discovered: AirPlayer Companion (TON-PC) @ 192.168.x.x:9000
   [AirPlayer] Connecting to AirPlayer Companion (TON-PC) (192.168.x.x)
   ```
4. Côté terminal compagnon :
   ```
   [HH:mm:ss] Headset connected: 'Quest 3S' @ 192.168.x.x
   ```
5. Le HUD (visible dans la vue Game si le `StatusHud` est devant la caméra) passe au vert : « Connecté », puis affiche la latence — en local, attends-toi à < 1–2 ms.

**Teste aussi la déconnexion sans quitter le Play Mode** : Ctrl+C sur le compagnon → le HUD doit repasser orange (« Déconnecté — connexion à … ») en moins de 4 s. Relance le compagnon → reconnexion automatique en 1–2 s. C'est les CA 2 et 4 de la checklist, version locale.

Ce que ce niveau **ne teste pas** : passthrough, hand tracking, cube grabbable, et le vrai Wi-Fi (tout passe en local). Les timings de latence n'ont donc aucune valeur représentative ici.

> Astuce : si la découverte ne se fait pas dans l'éditeur, le fallback se teste aussi : sélectionne le GameObject `ManualIp`, mets `127.0.0.1` dans `ipText`, et appelle `Connect()` (bouton droit sur le composant `ManualIpConnect` dans l'inspector → `Connect` n'apparaît pas par défaut ; le plus simple est de décocher `autoConnectToFirst` et d'appeler la méthode depuis un bouton UI ou un petit script de test).

---

## Niveau 2 — Quest Link : Play Mode dans le casque (itération rapide)

Avec le casque branché en **Quest Link** (câble USB-C ou Air Link), le Play Mode de l'éditeur s'affiche directement dans le casque — hand tracking réel, sans builder d'APK. Idéal pour itérer sur la scène (placement du HUD, taille du cube, interactions).

Configuration une fois :

1. Installer l'app **Meta Quest Link** sur le PC (anciennement Oculus PC).
2. Dans ses paramètres → **Général** : définir Meta Quest Link comme runtime OpenXR actif.
3. Paramètres → **Bêta** : activer **Developer Runtime Features**, puis **Passthrough over Meta Quest Link** (sinon le passthrough restera noir en Play Mode) et **Hand Tracking over Link**.
4. Brancher le casque, accepter Link dans le casque.

Ensuite : compagnon lancé, casque sur la tête, **Play** dans Unity. Tu dois voir le passthrough, tes mains, le cube grabbable, et le HUD qui se connecte comme au niveau 1.

Limites : l'app tourne toujours **sur le PC** — le trafic réseau reste local, donc ni la latence Wi-Fi réelle ni le comportement Android (multicast lock, permissions) ne sont testés. Les performances ne sont pas celles du Quest non plus.

---

## Niveau 3 — APK sur le casque (le test officiel de la checklist)

C'est le seul niveau qui valide les critères d'acceptation pour de vrai : app standalone sur le Quest, Wi-Fi réel entre le casque et le PC.

1. **Build** : File → Build Settings → **Build** → `AirPlayer.apk`.
2. **Installation** (casque branché, mode développeur actif) :
   ```
   adb devices          # le casque doit apparaître "device"
   adb install -r AirPlayer.apk
   ```
3. **Lancement** : dans le casque, Bibliothèque → menu déroulant en haut à droite → **Sources inconnues** → AirPlayer.
4. Compagnon lancé sur le PC, casque et PC sur le **même réseau Wi-Fi** (5 GHz de préférence).
5. Dérouler **`docs/loop0-checklist.md`** et remplir le tableau de résultats.

### Voir les logs du casque pendant le test

Indispensable pour comprendre ce qui se passe côté Quest :

```
adb logcat -s Unity
```

Tu y verras les `[AirPlayer]` : companion discovered, connecting, connection lost, multicast lock, etc. Lance cette commande **avant** d'ouvrir l'app pour ne rien rater du démarrage.

---

## Dépannage

| Symptôme | Piste |
|---|---|
| « Recherche du compagnon… » ne bouge pas (casque) | 1) Le compagnon tourne et logge bien `mDNS: announcing` ? 2) Pare-feu Windows : UDP entrant autorisé pour l'app (réseau **privé**) — au besoin, teste 30 s avec le pare-feu coupé pour isoler le problème. 3) Casque et PC sur le même réseau/VLAN ? L'« isolation AP/clients » de certains routeurs bloque tout trafic entre appareils — à désactiver. 4) Fallback : IP manuelle (`ipText` = IP du PC affichée par le compagnon, puis `Connect()`). |
| Découverte OK mais jamais « Connecté » | Le hello part vers udp/9000 mais la réponse revient sur udp/9001 : vérifie que le pare-feu n'en bloque pas un des deux. `adb logcat -s Unity` d'un côté, logs compagnon de l'autre : si le compagnon logge `Headset connected` mais que le HUD reste orange, c'est le chemin retour (9001) qui est bloqué. |
| Latence affichée élevée (> 30 ms) | Wi-Fi 2,4 GHz ou routeur surchargé. Passe en 5 GHz, rapproche le casque du routeur, même routeur pour les deux appareils. |
| Le HUD passe orange par intermittence | Pertes de pings — même cause Wi-Fi que ci-dessus. Note-le honnêtement dans la checklist, c'est un signal important avant la Loop 1. |
| Passthrough noir | Bloc Passthrough présent dans la scène ? Caméra en Solid Color alpha 0 ? En Link : « Passthrough over Meta Quest Link » activé dans les paramètres Bêta ? |
| Erreurs de compilation à l'ouverture du projet | Package `com.meta.xr.sdk.all` résolu ? (registre scoped Meta accessible). Package local **AirPlayer Core** visible dans Package Manager ? Les deux doivent être là avant que `AirPlayer.Runtime` compile. |
| `adb install` échoue (`INSTALL_FAILED_UPDATE_INCOMPATIBLE`) | Une version signée différemment est déjà installée : `adb uninstall com.<CompanyName>.<ProductName>` puis réinstalle. |
| HUD invisible dans le casque | `StatusHud` à ~1 m devant la caméra, character size ~0.01, police ~48. Vérifie aussi qu'il n'est pas derrière toi : il est fixe dans le monde, pas accroché à la tête. |

## Rappel

Le test qui compte pour clore la Loop 0, c'est le **niveau 3 + la checklist**. Les niveaux 1 et 2 servent à itérer vite et à isoler les problèmes (logique vs réseau vs Android). Une fois la checklist verte, mets à jour la section « État actuel » de `CLAUDE.md` — et on attaque la Loop 1.
