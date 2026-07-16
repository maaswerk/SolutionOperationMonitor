# 🚀 Solution Operation Monitor

**Watch solution import / upgrade / uninstall happen live in Microsoft Dataverse — progress %, ETA, a live chart and the full solution history at a glance.**

![Dataverse](https://img.shields.io/badge/Dataverse-Integration-blue)
![XrmToolBox](https://img.shields.io/badge/XrmToolBox-Plugin-green)
![Themes](https://img.shields.io/badge/Light%20%26%20Dark-mode-blueviolet)

---

## 🔍 Project description

**Solution Operation Monitor** is an XrmToolBox tool that shows the **live status of running solution operations** (import / upgrade / uninstall) in a Microsoft Dataverse environment — including progress in %, elapsed time and estimated remaining time (ETA) — plus the **complete solution history** at a glance.

It helps developers and administrators **see exactly what a deployment is doing right now**, how long it will still take, and whether a run is progressing normally or is stuck — instead of staring at a spinner that never moves.

---

## ✨ Main functions

### 1. Live operations
- Active import / upgrade / uninstall shown live (auto-refresh 3 / 5 / 10 / 30 / 60 s)
- Solution name, version, operation and sub-operation
- Progress bar + percentage, "running for" and estimated remaining time (ETA)

### 2. Progress chart per operation
- **Actual curve** of the real progress samples (blue)
- **Projection line** to 100 % based on the current rate (green dashed, with ETA clock)
- **Average reference line** from history (grey dotted) — a flatter actual curve means the run is slower than usual
- **"now" marker** (orange)

### 3. Realistic ETA
- Remaining time is based on the sliding progress rate of the last 2 minutes (not a naive extrapolation from the start), with exponential smoothing against jumpy values
- When the platform progress is stuck (typical: import jumps to ~90 % and stays there), the tool honestly shows **"progress stalled"** instead of an ever-growing fantasy ETA
- No fake percentages: without comparable history the bar runs in marquee mode instead of freezing at ~90 %

### 4. Solution history
- Last 200 operations as a sortable and filterable grid
- Operation, sub-operation, status, result, start, end, duration, error message
- Running rows highlighted, failed rows flagged
- **Completion notification** as soon as a running operation finishes (incl. result)

### 5. Light & dark mode
- "Theme" switch in the toolbar (light / dark), Windows app mode auto-detected on start
- Consistently themed across toolbar, cards, chart and history grid
- Switchable without an active connection and applied instantly

### 6. Bilingual (EN / DE)
- Language auto-detected from Windows, switchable in the toolbar

---

## 🎯 Possible applications

| Role | Benefits |
|-------------------|------------------------------------------------------|
| 👨‍💻 Developers | See live how long a deployment still takes and whether it is stuck |
| 🧑‍💼 Administrators | Monitor import / upgrade / uninstall and spot failed runs at a glance |
| 📊 Project managers | Transparent status of solution deployments incl. full history |

---

## 📊 How progress is measured

| Operation | Source | Accuracy |
|---|---|---|
| **Import** | `importjob.progress` (0–100 %, real platform value) | Exact, ETA from the measured rate |
| **Upgrade / Uninstall** | Dataverse provides **no percentage** here. The tool uses the average duration of the last (max. 5) comparable operations of the same solution from `msdyn_solutionhistory` | Estimate (flagged orange) |
| **History** | `msdyn_solutionhistory` (start/end time, operation, sub-operation, status, result, exception) | Exact |

> ℹ️ `msdyn_solutionhistory` entries are **deleted by the platform after 180 days**, so the history never reaches further back than that.

---

## ⚙️ Prerequisites

- A working **Microsoft Dataverse / Dynamics 365** environment
- Installation of the **XrmToolBox** as the plugin platform
- The connected user needs **read** access to:
  - `msdyn_solutionhistory` (solution history)
  - `importjob` (import job) — *optional*; without it everything works except the exact import percentage

---

## 🧭 Roadmap / Ideas

- [x] Light & dark mode
- [ ] "System" theme option that follows the Windows mode at runtime
- [ ] Windows toast notification on completion
- [ ] Per-component progress by parsing the `importjob.data` XML

---

## 📎 License

This project is licensed under the MIT license.