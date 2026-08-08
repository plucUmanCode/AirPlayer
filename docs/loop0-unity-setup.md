# Loop 0 — Assemblage du projet Unity dans l'éditeur

Le code C# du casque est livré dans le repo (`quest-app/Assets/AirPlayer/Runtime/` + le package local `shared/AirPlayer.Core`). Ce qui suit doit être fait **une fois dans l'éditeur Unity** sur ta machine : création de la scène, configuration XR et build. Compte 30–45 minutes.

## 1. Prérequis

- **Unity 6 LTS** (via Unity Hub) avec les modules **Android Build Support**, **OpenJDK** et **Android SDK & NDK Tools**.
- Un compte développeur Meta et le **mode développeur activé** sur le Quest 3S (app Meta Horizon sur téléphone → casque → Réglages → Mode développeur).
- Câble USB-C et `adb` fonctionnel (`adb devices` doit lister le casque).

## 2. Ouvrir le projet

1. Unity Hub → **Add** → sélectionner le dossier `quest-app/`.
2. Ouvrir avec ta version Unity 6 LTS (le fichier `ProjectSettings/ProjectVersion.txt` indique 6000.0.32f1 ; toute 6000.0.x plus récente convient, Unity mettra le fichier à jour).
3. À l'ouverture, Unity résout les packages du `manifest.json`, dont le **Meta XR All-in-One SDK** via le registre scoped `npm.developer.oculus.com`. Si la version `78.0.0` n'existe plus, ouvre Window → Package Manager et prends la dernière version du package `com.meta.xr.sdk.all`.
4. Vérifier que le package local **AirPlayer Core** apparaît dans Package Manager (In Project → AirPlayer Core). Les scripts `AirPlayer.Runtime` doivent compiler sans erreur.
5. **Committer les fichiers `.meta`** générés par Unity au premier import (ils fixent les GUIDs des assets).

## 3. Configuration projet (une fois)

1. **File → Build Settings** → plateforme **Android** → Switch Platform.
2. **Meta → Project Setup Tool** : appliquer tous les correctifs recommandés (« Fix All »). Ça règle : OpenXR comme runtime, ARM64/IL2CPP, Vulkan/GLES, permissions de base, version Android minimale.
3. **Edit → Project Settings → XR Plug-in Management** → onglet Android : cocher **OpenXR**, puis dans OpenXR → Interaction Profiles / Features : activer **Meta Quest Support** et **Hand Tracking**.
4. **Player Settings → Other Settings** :
   - Internet Access : **Require**.
   - Scripting Backend IL2CPP, Target Architectures ARM64 (le Project Setup Tool l'a normalement déjà fait).
5. Permissions Android : le SDK Meta injecte le manifeste. Pour le mDNS, ajouter la permission multicast : **Meta → Tools → Update AndroidManifest.xml** (génère `Assets/Plugins/Android/AndroidManifest.xml`), puis y ajouter dans `<manifest>` :
   ```xml
   <uses-permission android:name="android.permission.CHANGE_WIFI_MULTICAST_STATE" />
   ```
   (`INTERNET` et `ACCESS_NETWORK_STATE` y sont déjà.)

## 4. Scène minimale (Loop 0)

Créer une scène `Assets/AirPlayer/Scenes/Main.unity` :

1. Supprimer la `Main Camera` par défaut.
2. **Meta → Building Blocks** : ajouter les blocs **Camera Rig**, **Passthrough**, et **Hand Tracking** (bloc « Synthetic Hands » ou « Hands »). La skybox de la caméra doit être en **Solid Color** alpha 0 (le bloc Passthrough le configure).
3. **Cube grabbable** (valide poke/grab + passthrough) : Building Blocks → **Grab Interaction** (crée un cube avec `Grabbable` + `HandGrabInteractable`). Le placer à ~0,5 m devant la caméra, échelle ~0,15.
4. **GameObject vide `AirPlayer`** avec les composants :
   - `ConnectionManager` (laisser `deviceName` = « Quest 3S »),
   - `CompanionDiscovery` → glisser le `ConnectionManager` dans son champ, laisser `autoConnectToFirst` coché.
5. **HUD** : GameObject enfant `StatusHud` avec un composant **TextMesh** (taille de police ~48, character size ~0.01), positionné à ~1 m devant la caméra, légèrement sous la ligne d'horizon. Ajouter le composant `ConnectionHud` → glisser le `ConnectionManager`.
6. **Fallback IP manuelle** : GameObject `ManualIp` avec `ManualIpConnect` → glisser le `ConnectionManager` et un second `TextMesh` pour l'affichage. Pour la Loop 0, saisir l'IP directement dans le champ inspector `ipText` avant le build suffit ; le clavier 3D à boutons pokables viendra avec les modules UI (Loop 1+). Prévoir un bouton poke simple (Building Blocks → Poke Interaction sur un petit cube) qui appelle `ManualIpConnect.Connect()` via un `UnityEvent` si tu veux tester le fallback sans rebuild.
7. File → Build Settings → **Add Open Scenes**.

## 5. Build et installation

```
File → Build Settings → Build  (génère AirPlayer.apk)
adb install -r AirPlayer.apk
```

Lancer l'app depuis la bibliothèque du casque (catégorie « Sources inconnues »).

## 6. Compagnon côté PC

Sur le PC Windows (avec le SDK .NET 10 installé) :

```
cd companion
dotnet test                                # les tests xUnit doivent être verts
dotnet run --project src/AirPlayer.Companion
```

Le compagnon logge son IP locale, l'écoute OSC sur udp/9000 et l'annonce mDNS. Autoriser l'app dans le pare-feu Windows (réseau **privé**) à la première exécution — ports UDP 9000 et 5353 entrants.

Ensuite dérouler `docs/loop0-checklist.md`.
