# CJIT ServiceCore v3 Fix Notes

## Direct fixes from v2 feedback

1. **Workflow overlap fixed**
   - Rebuilt workflow layout using a dedicated `WorkflowForm` shell.
   - Header, step rail, content area, and footer are separated by docked `TableLayoutPanel` regions.
   - Every workflow content page is hosted in an auto-scroll panel.
   - Buttons are placed in a predictable footer instead of floating near content.

2. **DataGridView crash fixed**
   - v2 crashed when attempting to set `Columns["Id"].Width` after binding because the column reference could be null depending on binding timing/name mapping.
   - v3 uses `Theme.BindGrid()` which suspends layout, clears old binding, binds safely, clears selection, and never assumes a column exists.

3. **Duplicate/awkward "Guided" button labels removed**
   - Dashboard and navigation now use practical terms:
     - New Service Ticket
     - New Sale
     - Pickup Device
     - Daily Close
     - Process Center

4. **More U-Haul-style workflow behavior**
   - Counter workflows are now prompt-driven.
   - The user chooses a process, then steps through customer, device/order details, payment, review, and finish.
   - Each step has a clear instruction panel and validation before moving forward.

5. **Crash handling added**
   - Thread exceptions and unhandled exceptions are caught.
   - Logs are written to `%LOCALAPPDATA%\CJIT\ServiceCore\logs`.

## Recommended next build after v3

- Receipt printer formatting.
- Barcode scanner focused item lookup.
- Customer signature capture.
- Device intake photo attachment.
- Ticket labels.
- SMS/email notifications.
- Web dashboard/cloud sync.
