# CJIT ServiceCore Pro Edition v3
# VERSION: 3.0
# BUILD: 3.1.1
# REQUIREMENTS: Windows 10/11 + .NET SDK 8


## CJIT ServiceCore is a POS and service operations system for computer stores, phone repair shops, and service tech centers.

## Run
 Copy BUILD & RUN code to a new batch file (.bat) then save and then run as administrator.


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
