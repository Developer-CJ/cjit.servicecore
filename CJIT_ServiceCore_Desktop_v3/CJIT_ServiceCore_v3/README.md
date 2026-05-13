# CJIT ServiceCore Desktop Edition v3

CJIT ServiceCore is a Windows desktop front-counter POS and service operations system for computer stores, phone repair shops, and service tech centers.

## Default login

- Username: `chris`
- Password: `1228`

## What v3 fixes

- Rebuilt workflow pages to eliminate overlapping controls.
- Removed generic/duplicate "Guided" wording.
- Fixed the DataGridView crash from v2 by removing unsafe column access and adding safe grid binding.
- Added a Process Center for U-Haul-style step-by-step counter workflows.
- Added global crash handling that writes crash logs instead of silently dying.
- Added cleaner responsive layouts with docked panels, table layouts, scroll-safe content, and large touch targets.

## Run

Double-click:

```bat
RUN_ME.bat
```

Or run manually:

```powershell
dotnet restore
dotnet build -c Release
dotnet run -c Release
```

## Keyboard shortcuts

| Key | Action |
| --- | --- |
| F1 | Dashboard |
| F2 | Customers |
| F3 | New Service Ticket |
| F4 | Ticket Queue |
| F5 | New Sale |
| F6 | Tech Bench |
| F7 | Inventory / Parts |
| F8 | Pickup Device |
| F9 | Daily Close |
| F10 | Admin Settings |

## Main workflows

1. New Service Ticket
2. New Sale
3. Pickup Device
4. Daily Close

Each workflow has a left-side step rail, big instructions, Back/Next/Finish buttons, and keyboard support.

## Database

The local SQLite database is created automatically under:

```text
%LOCALAPPDATA%\CJIT\ServiceCore\servicecore_v3.db
```

Crash logs are written under:

```text
%LOCALAPPDATA%\CJIT\ServiceCore\logs
```

## Notes

This is a strong v3 foundation and is intentionally offline-first. Payment processing is represented as local/manual payment records. Real card terminal integrations should be added later using a compliant payment provider.
