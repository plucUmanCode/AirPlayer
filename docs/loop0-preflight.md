# Loop 0 — Pré-vol : vérifier que l'environnement est prêt

Série de **6 portes de validation** (G1 → G6) à passer dans l'ordre avant de dérouler la checklist casque et de clore la Loop 0. Chaque porte a un critère de réussite net et le correctif si ça bloque. Rejouable à tout moment (après une mise à jour du SDK, un changement de machine, etc.).

**Automatisation** : les portes G1 à G4 sont vérifiables par Claude Code branché sur l'éditeur via le serveur MCP `unity-mcp`. Prompt prêt à coller en fin de document.

---

## G1 — L'éditeur et les packages

| # | Vérification | Critère de réussite |
|---|---|---|
| 1.1 | Ouvrir le projet `quest-app` dans Unity | Aucune boîte de dialogue d'erreur au chargement |
| 1.2 | Console (Clear d'abord) | **0 erreur rouge** (warnings jaunes tolérés) |
| 1.3 | Package Manager → « In Project » | **Meta XR All-in-One SDK** présent — noter la version : ______ |
| 1.4 | Package Manager → « In Project » | **AirPlayer Core 0.1.0** présent (package local) |
| 1.5 | Menu **Meta** visible dans la barre de menus | Oui |

**Si ça bloque** : SDK absent → `docs/loop0-unity-pas-a-pas.md` étape C-4 (Asset Store → My Assets). AirPlayer Core absent → vérifier que le repo est complet (`shared/AirPlayer.Core/package.json` existe) et que `quest-app/Packages/manifest.json` contient `"com.airplayer.core": "file:../../shared/AirPlayer.Core"`. Erreurs rouges → traiter la **première** de la liste.

## G2 — La configuration projet

| # | Vérification | Critère de réussite |
|---|---|---|
| 2.1 | File → Build Profiles | Plateforme active = **Android** |
| 2.2 | Meta → Tools → **Project Setup Tool** | **0 élément « Required »** en attente (tout appliqué via Fix All) |
| 2.3 | Project Settings → XR Plug-in Management → onglet Android | **OpenXR** coché |
| 2.4 | → OpenXR → features Android | **Meta Quest Support** + **Hand Tracking** activés |
| 2.5 | Project Settings → Player → Other Settings | Internet Access = **Require**, Scripting Backend = **IL2CPP**, Target Architectures = **ARM64** |
| 2.6 | `Assets/Plugins/Android/AndroidManifest.xml` | Contient `android.permission.CHANGE_WIFI_MULTICAST_STATE` |

**Si ça bloque** : refaire l'étape D du guide pas-à-pas. Le Project Setup Tool règle 2.1 à 2.5 presque entièrement ; 2.6 est manuel.

## G3 — La scène Loop 0

| # | Vérification | Critère de réussite |
|---|---|---|
| 3.1 | `Assets/AirPlayer/Scenes/Main.unity` existe et est **dans la Scene List du build** | Oui, cochée |
| 3.2 | Hierarchy | Camera Rig Meta présent (pas de Main Camera par défaut restante), bloc Passthrough, hand tracking |
| 3.3 | Cube saisissable | Présent, ~0,5 m devant, échelle ~0,15 |
| 3.4 | GameObject `AirPlayer` | Composants **ConnectionManager** + **CompanionDiscovery**, champ Connection Manager **rempli** (pas « None ») |
| 3.5 | GameObject `StatusHud` | **TextMesh** + **ConnectionHud** (référence remplie), position ~(0, 1.3, 1.5) |
| 3.6 | GameObject `ManualIp` | **ManualIpConnect** (référence remplie), `Ip Text` = l'IP du PC affichée par le compagnon |

**Si ça bloque** : étape E du guide pas-à-pas. Un champ « None (…) » = référence à glisser depuis la Hierarchy.

## G4 — La connexion en Play Mode (sans casque)

Compagnon lancé sur le PC (`dotnet run --project src/AirPlayer.Companion` dans `companion/`), puis ▶ Play dans Unity.

| # | Vérification | Critère de réussite |
|---|---|---|
| 4.1 | Console Unity | `[AirPlayer] Companion discovered …` en **< 5 s** |
| 4.2 | Terminal compagnon | `Headset connected: 'Quest 3S' @ …` |
| 4.3 | HUD (vue Game) | Vert « Connecté », latence < 2 ms, « moy. 10 pings » après ~10 s |
| 4.4 | Ctrl+C sur le compagnon | HUD orange « Déconnecté » en **< 4 s** |
| 4.5 | Relancer le compagnon | Reconnexion automatique, HUD repasse vert |

**Si ça bloque** : dépannage de `docs/loop0-test-unity.md` (pare-feu Windows en premier suspect). Ne pas passer à G5 tant que G4 n'est pas vert : sur le casque, tout serait plus lent à déboguer.

## G5 — Le build sur le casque

| # | Vérification | Critère de réussite |
|---|---|---|
| 5.1 | Casque en USB, mode développeur actif | Visible dans Build Profiles → Run Device |
| 5.2 | **Build And Run** | Build sans erreur, l'app se lance dans le casque |
| 5.3 | Dans le casque | Passthrough visible, mains détectées, HUD lisible |

**Si ça bloque** : autorisation USB dans le casque, câble données, première erreur rouge de la Console.

## G6 — Go / No-Go

- **G1 à G5 verts** → tu es prêt : déroule **`docs/loop0-checklist.md`** (le test officiel avec chrono et tableau de résultats). Checklist verte → cocher Loop 0 dans `CLAUDE.md`, committer les fichiers générés par Unity (`.meta`, scène, manifest, ProjectVersion) → **feu vert Loop 1**.
- **Une porte rouge** → corriger avec le renvoi indiqué, re-passer la porte, ne pas sauter.

---

## Prompt pour Claude Code local (vérification automatique G1→G4)

À coller dans Claude Code (branché sur `unity-mcp`, Unity ouvert, compagnon **lancé** pour G4) :

> Lis docs/loop0-preflight.md. En utilisant les outils MCP unity-mcp, passe les portes G1 à G4 une par une : inspecte la Console, les packages installés, les réglages projet (plateforme, XR, Player, AndroidManifest), la scène Main (hiérarchie, composants, références sérialisées — signale tout champ à None), puis entre en Play Mode, observe la Console pour la découverte du compagnon, et ressors du Play Mode. Pour chaque vérification, donne ✅/❌ avec la valeur observée. Ce que tes outils ne permettent pas de vérifier, marque-le « à vérifier manuellement » sans deviner. Termine par un verdict : GO pour G5 (build casque) ou NO-GO avec la liste exacte des correctifs, dans l'ordre.

G5 et G6 restent manuels (il faut le casque sur la tête).
