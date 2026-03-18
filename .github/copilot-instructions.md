# Copilot Instructions

## Project Guidelines
- For AO UI work, prefer a mobile-first design that scales to web, with a desktop-app-like shell where content scrolls within the viewport. The web host should mimic the .NET MAUI shell: main content should use 100% available screen width, and all scrolling should stay inside the viewport-bound workspace rather than the page body. Use compact spacing, configurable light/dark themes, and modal/dialog patterns aligned to UI.md.

## Architecture Guidelines
- For AO CRM backend work, prefer vertical slice architecture and place CRM functionality under the AO.Core Features folder.