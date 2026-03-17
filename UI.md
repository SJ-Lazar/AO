# UI Instructions

## Design Direction

Use the attached example as the visual direction for the CRM UI.

The interface should feel:

- modern
- clean
- compact
- data-focused
- soft and minimal rather than heavy or crowded

The layout should resemble a modern analytics-style CRM dashboard with:

- a compact sidebar navigation
- a lightweight top toolbar
- modular content cards
- rounded panels
- subtle shadows
- compact modal and dialog surfaces

## Theme Requirements

Support both:

- Light theme
- Dark theme

Use a configurable `60 / 30 / 10` color system:

- `60%` primary/base color for main background areas
- `30%` secondary/surface color for cards, sidebars, panels, dialogs, and modals
- `10%` accent color for highlights, active states, buttons, badges, and important metrics

All theme colors must be configurable through centralized theme variables or tokens.

### Theme Token Guidance

Define configurable tokens for at least:

- `--color-base`
- `--color-surface`
- `--color-accent`
- `--color-text-primary`
- `--color-text-muted`
- `--color-border`
- `--color-shadow`
- `--color-success`
- `--color-warning`
- `--color-danger`

### Light Theme Guidance

The light theme should use:

- a soft neutral base background
- slightly elevated white or near-white surfaces
- a strong accent color for key actions and highlights
- subtle borders and low-contrast separators

### Dark Theme Guidance

The dark theme should use:

- a deep muted base background
- slightly lighter elevated surfaces
- the same accent family adjusted for contrast
- text and chart colors that remain readable without harsh contrast

## Density and Spacing

The UI should be compact.

Use minimal padding and margin throughout.

Guidelines:

- prefer tight vertical rhythm
- reduce excessive whitespace
- use compact card padding
- keep toolbar height small
- keep sidebar navigation dense but readable
- use small gaps between cards and controls
- avoid oversized headers or oversized buttons

Spacing should feel intentional and efficient, similar to a professional CRM or analytics dashboard.

## Modal Requirements

Add support for compact modals styled to match the dashboard.

Modal requirements:

- rounded corners
- soft shadow
- subtle border
- compact header
- compact body spacing
- compact footer actions
- support for both light and dark themes
- consistent spacing with the rest of the dashboard
- accent color used sparingly for primary actions and status emphasis

Modal sizes should support:

- small
- medium
- large

Modals should be suitable for actions such as:

- creating a contact
- editing a deal
- confirming destructive actions
- viewing compact detail summaries

## Dialog Requirements

Add support for compact dialog panels or dialog-style containers inspired by the example image.

Dialog requirements:

- visually consistent with cards and modals
- suitable for inline details or focused workflows
- rounded edges
- minimal padding
- clean typography
- clear hierarchy between title, metadata, content, and actions
- support for both light and dark themes

Dialog types may include:

- centered dialog
- side panel dialog
- inline floating detail panel

## Visual Style Rules

Use the following visual rules:

- rounded corners, but not exaggerated
- subtle shadows only
- thin borders
- small pill badges for metrics and statuses
- compact filters and segmented controls
- dense but readable tables and lists
- modular cards with clear grouping
- soft background layering between page, surface, and elevated elements

Avoid:

- large empty spaces
- heavy gradients
- thick borders
- oversized icons
- overly bright accent overuse
- bulky dialog spacing

## Layout Guidance

The CRM UI should include:

- left navigation rail or sidebar
- top action/search/filter area
- summary metric cards
- data panels for contacts, deals, tasks, and reporting
- modal and dialog patterns that visually match the dashboard cards

The overall layout should look production-ready even in MVP form.

## Component Styling Notes

The following components should follow the same compact visual system:

- buttons
- input fields
- dropdowns
- tabs
- cards
- tables
- badges
- modals
- dialogs
- side panels

All components must respect the active theme and shared color tokens.

## Interaction Guidance

Use the accent color for:

- primary actions
- active navigation states
- selected filters
- KPI highlights
- important status markers

Hover, focus, and selected states should be visible but subtle.

Animations should be minimal and fast.

## Implementation Intent

When implementing the UI:

- keep it compact
- keep it themeable
- keep the color system configurable
- ensure both light and dark themes are supported from the start
- make modals and dialogs feel like a natural extension of the dashboard design
- align the final look closely with the attached reference image without copying it exactly
