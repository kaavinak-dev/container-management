# Docker Maintenance — C Drive Space Management

<!-- CLAUDE INSTRUCTIONS
When the user asks any of the following (or similar):
  - "reduce docker c drive usage"
  - "clean up docker"
  - "docker is using too much space"
  - "prune docker images"
  - "compact docker disk"
  - "free up docker space"

Do the following:
  1. Read this file fully before responding.
  2. Identify which section(s) apply based on the user's intent.
  3. Present the relevant commands to the user and explain what each one does.
  4. Ask for explicit permission before executing each command.
  5. Execute commands one at a time, waiting for confirmation between each step.
  6. Never run `docker system prune -a` or `Optimize-VHD` without explicit confirmation
     — these are destructive/irreversible operations.
-->

---

## One-Time Setup (run once, already applied if .wslconfig exists)

### 1. Cap WSL2 disk size
Add to `C:\Users\kaavi\.wslconfig`:
```ini
[wsl2]
diskSize=40GB
```
Then restart Docker Desktop.

### 2. Enable containerd image store
Docker Desktop → Settings → General → enable **"Use containerd for pulling and storing images"** → Apply & Restart.

---

## After Every Dev Session (light cleanup — safe, fast)

```bash
# Stop running containers
docker compose down

# Remove stopped containers, dangling image layers, unused networks
docker system prune -f
```

---

## Bi-Weekly Cleanup (deeper — removes all unused images)

> WARNING: You will need to re-pull images (minio, postgres, redis, clamav, pgadmin) next session.

```bash
docker system prune -a -f --volumes
```

---

## Monthly — Compact the VHDX (reclaim space from pruning)

> Run in an admin PowerShell. Requires Docker/WSL to be fully shut down first.
> This is the step that actually shrinks the `.vhdx` file on your C drive.

```powershell
# Step 1 — Shut down WSL
wsl --shutdown

# Step 2 — Compact the virtual disk
Optimize-VHD -Path "$env:LOCALAPPDATA\Docker\wsl\disk\docker_data.vhdx" -Mode Full
```

If the path above doesn't exist, find the correct `.vhdx` with:
```powershell
ls "$env:LOCALAPPDATA\Docker\wsl\"
```

---

## Quick Reference

| Frequency      | Command(s)                                       | Effect                                       |
|----------------|--------------------------------------------------|----------------------------------------------|
| Every session  | `docker compose down` + `docker system prune -f` | Removes stopped containers + dangling layers |
| Bi-weekly      | `docker system prune -a -f --volumes`            | Removes ALL unused images + volumes          |
| Monthly        | `wsl --shutdown` + `Optimize-VHD`                | Shrinks the `.vhdx` file on C drive          |

---

## Notes
- ClamAV downloads fresh virus definitions on each startup — always prune after sessions that run ClamAV.
- `docker system prune -f` (light) is always safe. `prune -a` (deep) means re-pulling all images next session.
- The VHDX grows but never auto-shrinks — `Optimize-VHD` is the only way to reclaim C drive space.
