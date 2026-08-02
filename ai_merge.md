# AI Merge Guide

## Remote Setup

```
origin   = git@github.com:credzba/TazUO.git        (your fork)
upstream = https://github.com/PlayTazUO/TazUO.git   (main repo)
```

## Branch Map

| Branch | Tracks | Status |
|--------|--------|--------|
| `dev` | `upstream/dev` | Synced + 15 custom commits |
| `legacy` | `upstream/legacy` | Synced + 17 custom commits |
| `backup-dev` | (frozen) | Old `dev` history before reset |
| `main` | `upstream/main` | Not yet synced |

## How to Sync Upstream Changes

### Future sync flow (no divergence)

Since both `dev` and `legacy` share a common ancestor with their upstream counterparts, future syncs are a simple merge:

```powershell
# Sync dev
git fetch upstream
git checkout dev
git merge upstream/dev
git push origin dev --force-with-lease

# Sync legacy
git checkout legacy
git merge upstream/legacy
git push origin legacy --force-with-lease
```

If upstream adopts a change you already cherry-picked, git will flag a conflict. At that point, skip the duplicate:

```powershell
# During conflict resolution
git checkout --theirs conflicted_file   # or --ours, depending on context
git add conflicted_file
git cherry-pick --continue
```

### Custom commits on dev (oldest to newest)

```
487e18459 add ARGS.md
c04a05f92 Add StatusGumpCredzba subclass
484e26ba3 Fix for large gumps shifting
5095a7ffe Downgrade packet log Warn->Trace
446d9b10a Simplify deploy workflows
6b94e5416 Remove Discord Notifier workflow
676ae1494 Add Discord notification (DISCORD_WEBHOOK_NET10)
a32e5d1a1 Fix net472-deploy.yml
cf1d5f0fd Remove net472-deploy.yml
78e4d3cf4 WinExe release / Exe debug / --console
298a7b9e0 Fix exclusive file locks
d64a248b6 Restore StatusGumpCredzba fill bars
f89a45495 Remove duplicate StatusGumpCredzba; ExternalImageLoader fixes
9fdc1725f Add backoff for reconnect
5db854f74 Disable auto build-test on push
```

### Custom commits on legacy (oldest to newest)

```
b41493397 Guard SetInScreen against negative clamp values
ef70c1c5d Add StatusGumpCredzba subclass
f49bb9044 Downgrade packet log
7cc53289c Simplify deploy workflows
d96299054 Remove duplicate tuo-deploy.yml
a876d525e Rename net9-deploy.yml -> tuo-deploy.yml
23001f5ef Remove Discord Notifier workflow
94e2eda97 Add Discord notification (DISCORD_WEBHOOK_NET472)
c4675ccb9 Make Build-Test workflow_dispatch only
4f9a1551f Remove Bootstrap build steps
1d4aaef3f Fix zip step for Windows runner
49aeb9fbf Suppress on-screen messages (journal)
ebf5047ed Suppress all on-screen messages (journal)
22d84dbe3 Suppress all on-screen messages (journal)
375bba78a Journal window open check
f545b3c83 WinExe release / Exe debug / --console
da16cb769 Fix exclusive file locks
```

## Key Constraints

- Push operations require manual confirmation
- `--force-with-lease` (not `--force`) for safety
- Submodules need updating after branch switches or resets: `git submodule update --init --recursive`
