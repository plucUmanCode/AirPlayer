# Loop 0 — Guide Unity pas à pas (débutant)

Guide pour quelqu'un qui n'a **jamais ouvert Unity**. À la fin, l'app AirPlayer tourne sur ton Quest 3S et se connecte au compagnon sur ton PC. Compte **2 à 3 heures** la première fois, dont une bonne partie de téléchargements.

Plan : A. Installer Unity → B. Préparer le casque → C. Ouvrir le projet → D. Configurer → E. Créer la scène → F. Tester sans casque → G. Builder sur le casque → H. Dérouler la checklist.

---

## A. Installer Unity (≈ 45 min, surtout du téléchargement)

1. Télécharge **Unity Hub** : https://unity.com/download. Installe-le et ouvre-le.
2. Crée un compte **Unity ID** (gratuit) quand le Hub te le demande, et choisis la licence **Personal** (gratuite).
3. Dans le Hub, onglet **Installs** → **Install Editor** → choisis **Unity 6 LTS** (numéro de version `6000.x.xxf1`).
4. Sur l'écran des modules, coche :
   - ✅ **Android Build Support**, et dedans :
     - ✅ **OpenJDK**
     - ✅ **Android SDK & NDK Tools**
5. Lance l'installation et laisse tourner (~10 Go).

> Sans ces trois cases Android, impossible de builder pour le Quest. Si tu les as oubliées : Installs → roue dentée sur ta version → **Add modules**.

## B. Préparer le casque (≈ 15 min)

1. **Compte développeur Meta** : va sur https://developer.meta.com → connecte-toi avec ton compte Meta → crée une « organisation » (n'importe quel nom). C'est gratuit ; une vérification par téléphone ou carte peut être demandée.
2. **Mode développeur** : sur ton téléphone, app **Meta Horizon** → **Appareils** → ton Quest 3S → **Réglages du casque** → **Mode développeur** → active. Redémarre le casque.
3. Branche le casque au PC en USB-C. Mets le casque : une fenêtre **« Autoriser le débogage USB ? »** apparaît → **Toujours autoriser depuis cet ordinateur**. (Si tu la rates, elle reviendra au moment du build.)

## C. Ouvrir le projet (≈ 20 min de patience)

1. Assure-toi d'avoir la dernière version du repo : `git pull` dans ton dossier AirPlayer.
2. Unity Hub → onglet **Projects** → **Add** → **Add project from disk** → sélectionne le dossier **`quest-app`** (bien le sous-dossier, pas la racine du repo).
3. Clique sur le projet pour l'ouvrir. Si le Hub signale que la version du projet (6000.0.32f1) diffère de la tienne, choisis ta version installée et confirme — c'est prévu, Unity mettra le projet à niveau.
4. **Premier chargement long** (10-20 min) : Unity importe le projet. Il faut ensuite **installer le SDK Meta XR** (il n'est pas pré-installé) :
   - Dans ton **navigateur** : https://assetstore.unity.com → cherche **« Meta XR All-in-One SDK »** (gratuit, éditeur Meta) → connecte-toi avec ton compte Unity ID → **Add to My Assets**.
   - Dans Unity : **Window → Package Manager** → menu déroulant « Packages: » → **My Assets** → **Meta XR All-in-One SDK** → **Install** (gros téléchargement).
   - Si des fenêtres du SDK Meta apparaissent (télémétrie, mises à jour), réponds ce que tu veux — sans impact. Le menu **Meta** doit apparaître dans la barre de menus à la fin.
5. Repères de l'interface, tu n'as besoin que de ça :
   - **Hierarchy** (gauche) : la liste des objets de la scène.
   - **Scene / Game** (centre) : la vue 3D d'édition / ce que la caméra voit.
   - **Inspector** (droite) : les propriétés de l'objet sélectionné — c'est ici qu'on ajoute des **composants**.
   - **Project** (bas) : les fichiers du projet.
   - **Console** (onglet à côté de Project) : les logs et erreurs. **Ouvre-la et garde-la visible.**
6. Vérification : la Console ne doit montrer **aucune erreur rouge** (des warnings jaunes, c'est normal). En cas d'erreurs : Window → Package Manager → vérifie que **Meta XR All-in-One SDK** et **AirPlayer Core** (onglet « In Project ») sont bien là. Si la version du SDK Meta pose problème, sélectionne-le et prends la dernière version.

## D. Configurer le projet (≈ 15 min, une seule fois)

1. **Passer en Android** : menu **File → Build Profiles** (appelé **Build Settings** dans certaines versions) → sélectionne **Android** → bouton **Switch Platform** → attends la réimportation.
2. **Project Setup Tool de Meta** : menu **Meta → Tools → Project Setup Tool** (ou Edit → Project Settings → section **Meta XR**). Tu vois une liste de correctifs → clique **Fix All**, puis **Apply All** s'il reste des recommandations. Ça configure OpenXR, IL2CPP/ARM64, etc.
3. **Hand tracking** : Edit → Project Settings → **XR Plug-in Management** → sous-page **OpenXR** → onglet Android : dans les features, active **Meta Quest Support** et **Hand Tracking** (si le Project Setup Tool ne l'a pas déjà fait).
4. **Identité de l'app** : Edit → Project Settings → **Player** :
   - **Company Name** : `UmanCode` — **Product Name** : `AirPlayer` (c'est le nom affiché dans le casque).
   - Déplie **Other Settings** → **Internet Access** : **Require**. Vérifie le **Package Name** (ex. `com.UmanCode.AirPlayer`).
5. **Permission multicast (mDNS)** : menu **Meta → Tools → Update AndroidManifest.xml** (crée `Assets/Plugins/Android/AndroidManifest.xml`). Ouvre ce fichier (double-clic → éditeur de code) et ajoute cette ligne juste avant `</manifest>` :
   ```xml
   <uses-permission android:name="android.permission.CHANGE_WIFI_MULTICAST_STATE" />
   ```

## E. Créer la scène (≈ 30 min)

C'est l'étape la plus « Unity ». Deux gestes reviennent tout le temps :
- **Ajouter un composant** : sélectionne un objet dans la Hierarchy → bouton **Add Component** en bas de l'Inspector → tape le nom → Entrée.
- **Remplir un champ de référence** : glisse un objet **depuis la Hierarchy** jusque **sur le champ** dans l'Inspector.

Étapes :

1. **Nouvelle scène** : File → New Scene → **Basic (built-in)** → File → **Save As** → nomme-la `Main` dans un dossier `Assets/AirPlayer/Scenes` (crée le dossier dans la fenêtre de sauvegarde).
2. **Supprimer la caméra par défaut** : dans la Hierarchy, clic droit sur **Main Camera** → Delete. (Le rig Meta amène sa propre caméra.)
3. **Blocs Meta** : menu **Meta → Tools → Building Blocks**. Une fenêtre s'ouvre avec des tuiles. Ajoute (bouton **+** sur la tuile, ou glisse-la dans la Hierarchy) :
   - **Camera Rig**
   - **Passthrough**
   - **Hand Tracking** (selon la version du SDK : « Hands » ou « Synthetic Hands »)
   - **Grab Interaction** — crée un objet d'exemple saisissable. S'il te demande des dépendances, accepte qu'il les ajoute.
4. **Le cube** : si le bloc Grab Interaction a créé un objet saisissable, sélectionne-le et dans l'Inspector règle **Position** ≈ `X 0, Y 1.1, Z 0.5` et **Scale** ≈ `0.15, 0.15, 0.15`. (S'il n'y a pas d'objet : GameObject → 3D Object → Cube, mêmes position/échelle, puis Add Component → `Grabbable` et `Hand Grab Interactable` sur le cube.)
5. **L'objet AirPlayer** (le cerveau réseau) :
   - Clic droit dans la Hierarchy → **Create Empty** → renomme-le `AirPlayer`.
   - Add Component → **Connection Manager** (laisse `Device Name` = « Quest 3S »).
   - Add Component → **Companion Discovery** → glisse l'objet `AirPlayer` lui-même sur le champ **Connection Manager** → laisse **Auto Connect To First** coché.
6. **Le HUD** (l'affichage d'état) :
   - Clic droit sur `AirPlayer` → Create Empty → renomme `StatusHud`.
   - Position : `X 0, Y 1.3, Z 1.5` (≈ 1,5 m devant toi, hauteur des yeux assis).
   - Add Component → **Text Mesh** (le composant « legacy », pas TextMeshPro) : **Font Size** `48`, **Character Size** `0.01`, **Anchor** `Middle center`, **Alignment** `Center`.
   - Add Component → **Connection Hud** → glisse l'objet `AirPlayer` sur le champ **Connection Manager**.
7. **Fallback IP manuelle** (au cas où le mDNS ne passe pas sur ton réseau) :
   - Clic droit dans la Hierarchy → Create Empty → renomme `ManualIp`.
   - Add Component → **Manual Ip Connect** → glisse `AirPlayer` sur **Connection Manager** → dans **Ip Text**, tape l'IP de ton PC (celle que le compagnon affiche, ex. `192.168.100.129`).
8. **Sauvegarde** : Ctrl+S.
9. **Enregistrer la scène dans le build** : File → Build Profiles → **Scene List** (ou « Add Open Scenes » dans Build Settings) → vérifie que `Main` est cochée.

## F. Premier test — sans le casque (5 min)

Valide toute la connexion avant de builder :

1. Sur ton PC, lance le compagnon dans un terminal :
   ```powershell
   cd companion
   dotnet run --project src/AirPlayer.Companion
   ```
2. Dans Unity, appuie sur **▶ Play** (en haut au centre).
3. Attendu dans la **Console Unity** en < 5 s :
   ```
   [AirPlayer] Companion discovered: AirPlayer Companion (TON-PC) @ ...
   [AirPlayer] Connecting to ...
   ```
   et côté compagnon : `Headset connected: 'Quest 3S' @ ...`. Dans la vue **Game**, le texte du HUD est vert : « Connecté … Latence ».
4. Teste la coupure : **Ctrl+C sur le compagnon** → en < 4 s le HUD passe orange. Relance le compagnon → reconnexion seule.
5. Re-clique **▶** pour sortir du Play Mode. *(Piège Unity : les changements faits en Play Mode sont perdus en sortant.)*

Si rien n'est découvert ici, inutile d'aller sur le casque : vois le dépannage de `docs/loop0-test-unity.md` (pare-feu en tête).

## G. Builder et installer sur le casque (≈ 20 min la 1re fois)

1. Casque branché en USB, mode développeur actif (étape B).
2. File → **Build Profiles** (ou Build Settings) → plateforme Android → dans **Run Device**, ton Quest doit apparaître (bouton Refresh sinon — et vérifie la fenêtre d'autorisation USB dans le casque).
3. Clique **Build And Run**. Unity demande où sauver l'APK (`Builds/AirPlayer.apk`, crée le dossier). Le **premier build est long** (10-20 min, compilation IL2CPP) ; les suivants prennent 2-3 min.
4. À la fin, l'app **se lance toute seule dans le casque**. Pour la relancer plus tard : dans le casque, **Bibliothèque → filtre en haut à droite → Sources inconnues → AirPlayer**.

> Alternative sans câble une fois l'APK buildé : installe-le avec `adb install -r Builds/AirPlayer.apk`, ou via l'app **Meta Quest Developer Hub**.

## H. Le vrai test (checklist Loop 0)

1. Compagnon lancé sur le PC. Casque et PC sur le **même Wi-Fi** (5 GHz idéalement).
2. Mets le casque, lance AirPlayer : tu dois voir **ta pièce** (passthrough), **tes mains**, le **cube** saisissable, et le HUD qui passe au **vert tout seul en < 5 s** avec la latence mesurée.
3. Déroule **`docs/loop0-checklist.md`** point par point et remplis le tableau de résultats (latence notée, chrono de déconnexion, etc.).
4. Optionnel mais utile en cas de pépin : dans un terminal, `adb logcat -s Unity` affiche les logs `[AirPlayer]` du casque en direct.

Quand la checklist est verte : mets à jour la section « État actuel » de `CLAUDE.md`, commit — et la Loop 0 est officiellement close. 🎉

---

## Si ça coince

| Où | Réflexe |
|---|---|
| Erreurs rouges à l'ouverture du projet | Package Manager : SDK Meta XR et AirPlayer Core présents ? Redémarre Unity après résolution. |
| Le HUD reste « Recherche du compagnon… » | Pare-feu Windows (autoriser en réseau **privé**), isolation clients du routeur, ou mDNS bloqué → utilise `ManualIp` (son champ `Ip Text` + méthode `Connect`). Détails : `docs/loop0-test-unity.md`. |
| Le casque n'apparaît pas dans Run Device | Câble données (pas juste charge), autorisation USB dans le casque, mode développeur actif. |
| Passthrough noir dans le casque | Bloc **Passthrough** présent dans la scène ? Refais l'étape E-3. |
| Build échoue | Lis la **première** erreur rouge de la Console (les suivantes en découlent souvent) et envoie-la à Claude. |
